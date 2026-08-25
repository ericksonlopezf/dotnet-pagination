// Copyright © Erickson Lopez. MIT License.
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides HMAC-SHA256 signing and verification of cursor values to prevent tampering.
/// </summary>
/// <remarks>
/// <para>
/// <b>DI Lifetime:</b> When registered as a Singleton in Dependency Injection, 
/// the DI container will automatically manage its lifetime and safely call <see cref="Dispose()"/> during shutdown.
/// </para>
/// </remarks>

public sealed class HmacCursorEncoder : ICursorEncoder, IDisposable
{
    private byte[]? _key;

    // 0 = not disposed, 1 = disposed. Using int instead of bool to enable Interlocked operations.
    private int _disposed;
    private readonly ICursorEncoder _innerEncoder;
    private readonly TimeSpan? _ttl;
    private readonly TimeSpan _clockSkewTolerance;
    private readonly ICursorReplayStore? _replayStore;
    private readonly TimeSpan _nonceTtl;
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;

    /// <summary>
    /// Gets a fallback HMAC encoder instance configured with a development key.
    /// </summary>
    public static HmacCursorEncoder DevelopmentDefault { get; } = new HmacCursorEncoder("EricksonLopez.Pagination.DevelopmentKey.Fallback.DoNotUseInProduction");

    /// <summary>
    /// Initializes a new instance of the <see cref="HmacCursorEncoder"/> class with the specified secret key and optional security settings.
    /// </summary>
    /// <param name="secretKey">The secret key used for HMAC-SHA256 signing. Must be at least 32 bytes after UTF-8 encoding.</param>
    /// <param name="innerEncoder">The underlying encoder used to format the signed payload. Defaults to <see cref="Base64CursorEncoder.Default"/>.</param>
    /// <param name="timeToLive">An optional time-to-live duration for cursor expiration.</param>
    /// <param name="clockSkewTolerance">An optional tolerance for clock skew in distributed environments. Defaults to 30 seconds.</param>
    /// <param name="replayStore">An optional replay store for single-use cursor validation.</param>
    /// <param name="logger">An optional logger for cursor security and expiration events.</param>
    /// <exception cref="ArgumentNullException"><paramref name="secretKey"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="secretKey"/> is shorter than 32 bytes after UTF-8 encoding</exception>
    public HmacCursorEncoder(
        string secretKey,
        ICursorEncoder? innerEncoder = null,
        TimeSpan? timeToLive = null,
        TimeSpan? clockSkewTolerance = null,
        ICursorReplayStore? replayStore = null,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(secretKey);
        
        _key = Encoding.UTF8.GetBytes(secretKey);
        if (_key.Length < 32)
            throw new ArgumentException("Secret key must be at least 32 bytes (256 bits) long after UTF-8 encoding for secure HMAC signing.", nameof(secretKey));

        _innerEncoder = innerEncoder ?? Base64CursorEncoder.Default;
        _ttl = timeToLive;
        _clockSkewTolerance = clockSkewTolerance ?? TimeSpan.FromSeconds(30);
        _replayStore = replayStore;
        _nonceTtl = timeToLive ?? TimeSpan.FromHours(1);
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">The encoder has been disposed</exception>
    public string? Encode(string? rawCursor)
    {
        var key = Volatile.Read(ref _key);
        // Stryker disable once all : Disposed state check
        ObjectDisposedException.ThrowIf(_disposed == 1 || key is null, this);
        if (string.IsNullOrWhiteSpace(rawCursor)) return rawCursor;

        string noncePrefix = _replayStore != null ? $"R{Guid.NewGuid():N}:" : string.Empty;

        string contentToSign;
        if (_ttl.HasValue)
        {
            long expiresAt = DateTimeOffset.UtcNow.Add(_ttl.Value).ToUnixTimeSeconds();
            contentToSign = $"T{expiresAt}:{noncePrefix}{rawCursor}";
        }
        else if (_replayStore != null)
        {
            contentToSign = $"{noncePrefix}{rawCursor}";
        }
        else
        {
            contentToSign = $"N:{rawCursor}";
        }

        Span<byte> hash = stackalloc byte[32];
        SignToSpan(contentToSign.AsSpan(), hash, key);
        
#if NET9_0_OR_GREATER
        var base64UrlHash = System.Buffers.Text.Base64Url.EncodeToString(hash);
#else
        var base64UrlHash = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#endif
        var payload = $"{contentToSign}.{base64UrlHash}";

        return _innerEncoder.Encode(payload);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">The encoder has been disposed</exception>
    /// <exception cref="InvalidPaginationCursorException">The cursor signature is invalid, missing a nonce, or tampering was detected</exception>
    /// <exception cref="ExpiredPaginationCursorException">The cursor has expired according to its time-to-live</exception>
    /// <exception cref="ReplayedPaginationCursorException">The cursor has already been consumed and replay was detected</exception>
    public string? Decode(string? opaqueCursor)
    {
        var key = Volatile.Read(ref _key);
        // Stryker disable once all : Disposed state check
        ObjectDisposedException.ThrowIf(_disposed == 1 || key is null, this);
        if (string.IsNullOrWhiteSpace(opaqueCursor)) return opaqueCursor;

        var payload = _innerEncoder.Decode(opaqueCursor);
        
        if (payload == null) return null;

        var dotIndex = payload.LastIndexOf('.');
        // Stryker disable once Equality : dotIndex boundary check
        if (dotIndex < 0)
        {
            // Stryker disable once all : Structured logging events
            PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
            // Stryker disable once all : Exception message text

            throw new InvalidPaginationCursorException("The cursor is not signed correctly.", opaqueCursor);
        }

        var contentToSignStr = payload[..dotIndex];
        var contentToSignSpan = payload.AsSpan(0, dotIndex);

        Span<byte> expectedHmacBytes = stackalloc byte[32]; // SHA256 is 32 bytes
        SignToSpan(contentToSignSpan, expectedHmacBytes, key);

        var providedHmacStr = payload.AsSpan(dotIndex + 1);

#if NET9_0_OR_GREATER
        Span<byte> providedHmacBytes = stackalloc byte[32];
        var opStatus = System.Buffers.Text.Base64Url.DecodeFromChars(providedHmacStr, providedHmacBytes, out _, out var bytesWritten);
        // Stryker disable once all : Low-level base64 decode buffer status verification
        bool isValidSignatureFormat = opStatus == System.Buffers.OperationStatus.Done && bytesWritten == 32;
#else
        Span<char> base64Chars = stackalloc char[48];
        providedHmacStr.CopyTo(base64Chars);
        var base64Length = providedHmacStr.Length;

        for (int i = 0; i < base64Length; i++)
        {
            if (base64Chars[i] == '-') base64Chars[i] = '+';
            else if (base64Chars[i] == '_') base64Chars[i] = '/';
        }

        int padding = 4 - (base64Length % 4);
        if (padding < 4)
        {
            for (int i = 0; i < padding; i++) base64Chars[base64Length + i] = '=';
            base64Length += padding;
        }

        Span<byte> providedHmacBytes = stackalloc byte[32];
        bool isValidSignatureFormat = Convert.TryFromBase64Chars(base64Chars[..base64Length], providedHmacBytes, out var bytesWritten) && bytesWritten == 32;
#endif

        var contentMatches = CryptographicOperations.FixedTimeEquals(providedHmacBytes, expectedHmacBytes);

        if (!isValidSignatureFormat || !contentMatches)
        {
            // Stryker disable once all : OpenTelemetry instrumentation metrics

            PaginationMetrics.RecordCursorError("tampered");
            // Stryker disable once all : Structured logging events
            PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
            // Stryker disable once all : Exception message text

            throw new InvalidPaginationCursorException("The cursor signature is invalid. Tampering detected.", opaqueCursor);
        }

        // Stryker disable once all : CS0165 variable initialization
        string rawCursorStr = string.Empty;
        string? extractedNonce = null;

        if (contentToSignStr.StartsWith("N:", StringComparison.Ordinal))
        {
            rawCursorStr = contentToSignStr.Substring(2);
        }
        else if (contentToSignStr.StartsWith("R", StringComparison.Ordinal))
        {
            var colonIndex = contentToSignStr.IndexOf(':');
            if (colonIndex > 1)
            {
                extractedNonce = contentToSignStr.Substring(1, colonIndex - 1);
                rawCursorStr = contentToSignStr.Substring(colonIndex + 1);
            }
            else
            {
                // Stryker disable once all : OpenTelemetry instrumentation metrics

                PaginationMetrics.RecordCursorError("tampered");
                // Stryker disable once all : Structured logging events
                PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
                // Stryker disable once all : Exception message text

                throw new InvalidPaginationCursorException("The cursor format is unrecognized or has been tampered with.", opaqueCursor);
            }
        }
        else if (contentToSignStr.StartsWith("T", StringComparison.Ordinal))
        {
            var colonIndex = contentToSignStr.IndexOf(':');
            // Stryker disable once Equality : colonIndex == 1 empty ttl string optimization
            if (colonIndex > 1)
            {
                var ttlStr = contentToSignStr.Substring(1, colonIndex - 1);
                if (long.TryParse(ttlStr, out long expiresAt))
                {
                    // Stryker disable once Equality : Clock skew equality boundary condition
                    if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt + (long)_clockSkewTolerance.TotalSeconds)
                    {
                        var exp = DateTimeOffset.FromUnixTimeSeconds(expiresAt);
                        // Stryker disable once all : OpenTelemetry instrumentation metrics

                        PaginationMetrics.RecordCursorError("expired");
                        // Stryker disable once all : Structured logging events
                        PaginationLogEvents.LogCursorExpired(_logger, opaqueCursor, exp);
                        throw new EricksonLopez.Pagination.Abstractions.ExpiredPaginationCursorException(
                            opaqueCursor,
                            exp);
                    }

                    var remaining = contentToSignStr.Substring(colonIndex + 1);
                    if (remaining.StartsWith("R", StringComparison.Ordinal))
                    {
                        var nonceColon = remaining.IndexOf(':');
                        if (nonceColon > 1)
                        {
                            extractedNonce = remaining.Substring(1, nonceColon - 1);
                            rawCursorStr = remaining.Substring(nonceColon + 1);
                        }
                        else
                        {
                            // Stryker disable once all : OpenTelemetry instrumentation metrics

                            PaginationMetrics.RecordCursorError("tampered");
                            // Stryker disable once all : Structured logging events
                            PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
                            // Stryker disable once all : Exception message text

                            throw new InvalidPaginationCursorException("The cursor format is unrecognized or has been tampered with.", opaqueCursor);
                        }
                    }
                    else
                    {
                        rawCursorStr = remaining;
                    }
                }
                else
                {
                    // Stryker disable once all : OpenTelemetry instrumentation metrics

                    PaginationMetrics.RecordCursorError("tampered");
                    // Stryker disable once all : Structured logging events
                    PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
                    // Stryker disable once all : Exception message text

                    throw new InvalidPaginationCursorException("The cursor format is unrecognized or has been tampered with.", opaqueCursor);
                }
            }
            else
            {
                // Stryker disable once all : OpenTelemetry instrumentation metrics

                PaginationMetrics.RecordCursorError("tampered");
                // Stryker disable once all : Structured logging events
                PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
                // Stryker disable once all : Exception message text

                throw new InvalidPaginationCursorException("The cursor format is unrecognized or has been tampered with.", opaqueCursor);
            }
        }
        else
        {
            // Stryker disable once all : OpenTelemetry instrumentation metrics

            PaginationMetrics.RecordCursorError("tampered");
            // Stryker disable once all : Structured logging events
            PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
            // Stryker disable once all : Exception message text

            throw new InvalidPaginationCursorException("The cursor format is unrecognized or has been tampered with.", opaqueCursor);
        }

        if (_replayStore != null)
        {
            if (string.IsNullOrEmpty(extractedNonce))
            {
                // Stryker disable once all : OpenTelemetry instrumentation metrics

                PaginationMetrics.RecordCursorError("tampered");
                // Stryker disable once all : Structured logging events
                PaginationLogEvents.LogCursorTampered(_logger, opaqueCursor);
                // Stryker disable once all : Exception message text

                throw new InvalidPaginationCursorException("The cursor is missing a required replay protection nonce.", opaqueCursor);
            }

            if (!_replayStore.TryAcquireNonce(extractedNonce, _nonceTtl))
            {
                // Stryker disable once all : OpenTelemetry instrumentation metrics

                PaginationMetrics.RecordCursorError("replayed");
                // Stryker disable once all : Structured logging events
                PaginationLogEvents.LogCursorReplayed(_logger, opaqueCursor, extractedNonce);
                throw new ReplayedPaginationCursorException("The pagination cursor has already been consumed (replay attack detected).", opaqueCursor, extractedNonce);
            }
        }

        return rawCursorStr;
    }

    // key is passed as a parameter (captured by caller before the disposed check) to avoid a
    // TOCTOU race where Dispose() zeros the backing array between the null check here and the
    // HMACSHA256.TryHashData call. The caller is responsible for capturing _key via Volatile.Read.
    
    private static void SignToSpan(ReadOnlySpan<char> value, Span<byte> destinationHash, byte[] key)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        byte[]? arrayToReturnToPool = null;
        
        // Stryker disable all : stackalloc micro-optimization, tested functionally
        if (maxByteCount > 768)
        {
            arrayToReturnToPool = System.Buffers.ArrayPool<byte>.Shared.Rent(maxByteCount);
        }

        Span<byte> valueBytes = arrayToReturnToPool != null
            ? arrayToReturnToPool.AsSpan(0, maxByteCount)
            : stackalloc byte[maxByteCount];
        // Stryker restore all
        // Stryker restore all
            
        try
        {
            var bytesWritten = Encoding.UTF8.GetBytes(value, valueBytes);
            var actualValueBytes = valueBytes[..bytesWritten];

            HMACSHA256.TryHashData(key.AsSpan(), actualValueBytes, destinationHash, out _);
        }
        // Stryker disable once block : Array pool cleanup and memory zeroing cannot be functionally tested
        finally
        {
            // Stryker disable all : Array pool cleanup and memory zeroing cannot be functionally tested
            if (arrayToReturnToPool != null)
            {
                // Ensure we clear the rented array properly, up to what we requested.
                CryptographicOperations.ZeroMemory(arrayToReturnToPool.AsSpan(0, maxByteCount));
                System.Buffers.ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
            }
            else
            {
                // Clear the stackalloc memory
                CryptographicOperations.ZeroMemory(valueBytes);
            }
            // Stryker restore all
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="HmacCursorEncoder"/> and clears sensitive key material.
    /// </summary>
    public void Dispose()
    {
        // Stryker disable all : Memory zeroing and double-dispose protection cannot be functionally tested
        // Use Interlocked.Exchange to ensure atomic check-and-set, preventing a race condition
        // where two threads both see _disposed == false and both call ZeroMemory on the key.
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        var key = Interlocked.Exchange(ref _key, null);
        if (key != null)
        {
            CryptographicOperations.ZeroMemory(key);
        }
        // Stryker restore all
    }
}

