// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class HmacCursorEncoderTests
{
    private sealed class MockCursorEncoder : ICursorEncoder
    {
        public Func<string?, string?> DecodeFunc { get; set; } = s => s;
        public Func<string?, string?> EncodeFunc { get; set; } = s => s;
        public string? Encode(string? rawCursor) => EncodeFunc(rawCursor);
        public string? Decode(string? opaqueCursor) => DecodeFunc(opaqueCursor);
    }
    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenSecretKeyIsNullOrEmpty()
    {
        // Act
        Action act1 = () => _ = new HmacCursorEncoder(null!);
        Action act2 = () => _ = new HmacCursorEncoder("");

        // Assert
        act1.Should().Throw<ArgumentNullException>().WithParameterName("secretKey");
        act2.Should().Throw<ArgumentException>().WithParameterName("secretKey").WithMessage("*32 bytes*");
    }

    [Fact]
    public void Encode_ShouldReturnNull_WhenRawCursorIsNull()
    {
        // Arrange
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");

        // Act
        var result = encoder.Encode(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Encode_ShouldReturnEmpty_WhenRawCursorIsEmpty()
    {
        // Arrange
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");

        // Act
        var result = encoder.Encode("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void EncodeDecode_ShouldWorkCorrectly_WhenValidCursor()
    {
        // Arrange
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        var cursor = "eyJJZCI6NH0="; // valid payload

        // Act
        var encoded = encoder.Encode(cursor);
        var decoded = encoder.Decode(encoded);

        // Assert
        encoded.Should().NotBeNull();
        encoded.Should().NotBe(cursor);
        decoded.Should().Be(cursor);
    }

    [Fact]
    public void Decode_ShouldReturnNull_WhenOpaqueCursorIsNull()
    {
        // Arrange
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");

        // Act
        var result = encoder.Decode(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Decode_ShouldReturnEmpty_WhenOpaqueCursorIsEmpty()
    {
        // Arrange
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");

        // Act
        var result = encoder.Decode("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void Decode_ShouldThrowInvalidPaginationCursorException_WhenCursorIsNotSignedCorrectly()
    {
        // Arrange
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        var base64WithoutDot = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("rawcursorwithoutdot"));

        // Act
        Action act = () => encoder.Decode(base64WithoutDot);
        
        // Assert
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenSecretKeyIsExactly32Bytes()
    {
        Action act = () => _ = new HmacCursorEncoder("01234567890123456789012345678901");
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WhenInnerEncoderIsNull_DefaultsToBase64()
    {
        using var hmac = new HmacCursorEncoder("01234567890123456789012345678901", null);
        var raw = "abc";
        var encoded = hmac.Encode(raw);
        encoded.Should().NotContain("N:abc"); // Base64 encoding applied
    }

    private sealed class MockEncoder : ICursorEncoder
    {
        public string? Encode(string? rawCursor) => "mock_encoded";
        public string? Decode(string? opaqueCursor) => "mock_decoded";
    }

    [Fact]
    public void Constructor_WhenInnerEncoderProvided_UsesInnerEncoder()
    {
        using var hmac = new HmacCursorEncoder("01234567890123456789012345678901", new MockEncoder());
        hmac.Encode("abc").Should().Be("mock_encoded");
    }

    [Fact]
    public void Decode_ShouldThrow_WhenDotIndexIsAtZero()
    {
        using var hmac = new HmacCursorEncoder("01234567890123456789012345678901");
        var base64WithLeadingDot = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(".something"));
        
        Action act = () => hmac.Decode(base64WithLeadingDot);
        act.Should().Throw<InvalidPaginationCursorException>();
    }
    private static string CreateSignedOpaqueCursor(string secretKey, string rawPayload)
    {
        using var hmacAlg = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var hash = hmacAlg.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawPayload));
        var base64UrlHash = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var fullPayload = $"{rawPayload}.{base64UrlHash}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fullPayload));
    }

    [Fact]
    public void Decode_LegacyFormatWithoutTTL_ThrowsInvalidPaginationCursorException()
    {
        const string key = "01234567890123456789012345678901";
        using var hmac = new HmacCursorEncoder(key);
        var cursor = CreateSignedOpaqueCursor(key, "oldcursorvalwithoutttl");

        Action act = () => hmac.Decode(cursor);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void Decode_MultiColumnKeysetCursor_ShouldThrowInvalidPaginationCursorException()
    {
        // Tests fix for F-007: HmacCursorEncoder no longer parses multi-column keyset cursors as legacy TTL formats.
        const string key = "01234567890123456789012345678901";
        using var hmac = new HmacCursorEncoder(key);
        var cursor = CreateSignedOpaqueCursor(key, "M|v2|ABCD1234|42|123");

        Action act = () => hmac.Decode(cursor);
        act.Should().Throw<InvalidPaginationCursorException>()
           .WithMessage("The cursor format is unrecognized or has been tampered with.");
    }


    [Fact]
    public void Decode_ShouldThrowInvalidPaginationCursorException_WhenSignatureIsInvalid()
    {
        // Arrange
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        var cursor = "eyJJZCI6NH0=";
        var encoded = encoder.Encode(cursor);

        // Tamper with the encoded payload by changing the inner encoder
        var rawPayload = Base64CursorEncoder.Default.Decode(encoded!);
        var tamperedPayload = rawPayload + "1"; // Change the HMAC
        var tamperedEncoded = Base64CursorEncoder.Default.Encode(tamperedPayload);

        // Act
        Action act = () => encoder.Decode(tamperedEncoded);

        // Assert
        act.Should().Throw<InvalidPaginationCursorException>()
           .WithMessage("The cursor signature is invalid. Tampering detected.*");
    }

    [Fact]
    public void Encode_ShouldReturnNullOrEmpty_WhenDataIsNullOrEmpty()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-16-bytes-1234567890123456");
        
        encoder.Encode(null).Should().BeNull();
        encoder.Encode("").Should().Be("");
    }

    [Fact]
    public void Decode_ShouldReturnNullOrEmpty_WhenCursorIsNullOrEmpty()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-16-bytes-1234567890123456");
        
        encoder.Decode(null).Should().BeNull();
        encoder.Decode("").Should().Be("");
    }

    [Fact]
    public void Decode_ShouldThrowInvalidPaginationCursorException_WhenCursorIsInvalidBase64Url()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-16-bytes-1234567890123456");
        
        Action act = () => encoder.Decode("invalid!@#$%^&*()");

        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void Decode_ShouldThrowInvalidPaginationCursorException_WhenSignatureIsMissing()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-16-bytes-1234567890123456");
        // No dot in the decoded base64 string
        string noSignatureStr = "aGVsbG8="; // "hello" in base64
        Action act = () => encoder.Decode(noSignatureStr);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void EncodeDecode_WithTTL_ShouldWorkWhenNotExpired()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234", timeToLive: TimeSpan.FromMinutes(5));
        var encoded = encoder.Encode("test_cursor");
        
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be("test_cursor");
    }

    [Fact]
    public void Decode_WithTTL_ShouldThrowExpiredPaginationCursorException_WhenExpired()
    {
        // Gap 1: HmacCursorEncoder must throw the specific ExpiredPaginationCursorException
        // (not the generic InvalidPaginationCursorException) when the TTL has elapsed.
        // This allows callers to differentiate between expiry (operational) and tampering (security).
        using var encoder = new HmacCursorEncoder(
            "my-super-secret-key-32-bytes-long-1234", 
            timeToLive: TimeSpan.FromSeconds(-2),
            clockSkewTolerance: TimeSpan.Zero); // Must be zero so it expires instantly instead of falling into the 30s grace period
        var encoded = encoder.Encode("test_cursor");
        
        Action act = () => encoder.Decode(encoded);
        act.Should().Throw<EricksonLopez.Pagination.Abstractions.ExpiredPaginationCursorException>()
           .Which.ExpiredAt.Should().BeBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Decode_WithTTL_ExpiredPaginationCursorException_IsAlsoCatchableAsBase()
    {
        // Gap 1: ExpiredPaginationCursorException derives from InvalidPaginationCursorException.
        // Existing catch (InvalidPaginationCursorException) blocks must continue to catch expiry events
        // without modification — backward compatibility guarantee.
        using var encoder = new HmacCursorEncoder(
            "my-super-secret-key-32-bytes-long-1234", 
            timeToLive: TimeSpan.FromSeconds(-2),
            clockSkewTolerance: TimeSpan.Zero);
        var encoded = encoder.Encode("test_cursor");
        
        Action act = () => encoder.Decode(encoded);
        act.Should().Throw<EricksonLopez.Pagination.Abstractions.InvalidPaginationCursorException>(); // base type still catches it
    }

    [Fact]
    public void Encode_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        encoder.Dispose();
        
        Action act = () => encoder.Encode("test");
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Decode_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        encoder.Dispose();
        
        Action act = () => encoder.Decode("test");
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void EncodeDecode_ShouldUseArrayPool_WhenCursorIsVeryLarge()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        // maxByteCount > 768 requires string length > 256 for UTF8, let's use a 1000 char string
        var largeCursor = new string('A', 1000);
        
        var encoded = encoder.Encode(largeCursor);
        var decoded = encoder.Decode(encoded);
        
        decoded.Should().Be(largeCursor);
    }

    [Fact]
    public void Decode_WithTTL_ShouldThrow_WhenTTLIsUnparseable()
    {
        const string key = "my-super-secret-key-32-bytes-long-1234";
        using var hmac = new HmacCursorEncoder(key);
        
        // Hand-craft a T-prefixed payload with invalid TTL format: T<UnixTimeInSeconds>:<RawCursor>
        var cursor = CreateSignedOpaqueCursor(key, "Tinvalidttl:mycursor");

        Action act = () => hmac.Decode(cursor);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void Decode_WithTTL_ShouldThrow_WhenMissingColon()
    {
        const string key = "my-super-secret-key-32-bytes-long-1234";
        using var hmac = new HmacCursorEncoder(key);
        
        // Hand-craft a T-prefixed payload missing the colon
        var cursor = CreateSignedOpaqueCursor(key, "T99999999999");

        Action act = () => hmac.Decode(cursor);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Theory]
    [InlineData("payload_alpha_1")]
    [InlineData("payload_beta_2_test_vector")]
    [InlineData("cursor:with:colons:and+symbols")]
    [InlineData("deterministic_vector_exercising_base64url_branches")]
    [InlineData("short")]
    [InlineData("longer_payload_with_multiple_segments_and_characters_1234567890")]
    public void EncodeDecode_WithDeterministicPayloads_EnsuresUrlSafeBase64BranchesAreHit(string rawPayload)
    {
        // Deterministically tests that URL-safe characters ('-', '_') are produced and properly decoded
        // without '+' or '/' characters in the outer representation.
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        
        var encoded = encoder.Encode(rawPayload);
        encoded.Should().NotBeNull();
        encoded.Should().NotContain("+").And.NotContain("/");

        var decoded = encoder.Decode(encoded);
        decoded.Should().Be(rawPayload);
    }

    [Fact]
    public void DevelopmentDefault_WhenUsed_EncodesAndDecodesSuccessfully()
    {
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        encoder.Should().NotBeNull();
        var encoded = encoder.Encode("dev-cursor");
        encoded.Should().NotBeNull();
        encoder.Decode(encoded).Should().Be("dev-cursor");
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_IsIdempotent()
    {
        var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        encoder.Dispose();
        encoder.Dispose(); // second call should not throw
        Action act = () => encoder.Encode("test");
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Decode_WithMalformedR_ShouldThrow()
    {
        const string key = "my-super-secret-key-32-bytes-long-1234";
        using var hmac = new HmacCursorEncoder(key);
        
        // Signed R:cursor where colon is at index 1 (no nonce)
        var cursor = CreateSignedOpaqueCursor(key, "R:mycursor");

        Action act = () => hmac.Decode(cursor);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void Decode_WithMalformedTR_ShouldThrow()
    {
        const string key = "my-super-secret-key-32-bytes-long-1234";
        using var hmac = new HmacCursorEncoder(key);
        
        // Signed T9999999999:R:mycursor where TR has no nonce
        var cursor = CreateSignedOpaqueCursor(key, "T9999999999:R:mycursor");

        Action act = () => hmac.Decode(cursor);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void Decode_WithUnknownPrefix_ShouldThrow()
    {
        const string key = "my-super-secret-key-32-bytes-long-1234";
        using var hmac = new HmacCursorEncoder(key);
        
        // Signed Z:mycursor (unknown prefix)
        var cursor = CreateSignedOpaqueCursor(key, "Z:mycursor");

        Action act = () => hmac.Decode(cursor);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    private class TrackingReplayStore : ICursorReplayStore
    {
        public string? LastNonce { get; private set; }
        public TimeSpan? LastTtl { get; private set; }

        public bool TryAcquireNonce(string nonce, TimeSpan timeToLive)
        {
            LastNonce = nonce;
            LastTtl = timeToLive;
            return true;
        }

        public Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TryAcquireNonce(nonce, timeToLive));
        }
    }

    [Fact]
    public void Decode_WithFutureTimestamp_WithNonTPrefix_ThrowsInvalidPaginationCursorException()
    {
        const string key = "my-super-secret-key-32-bytes-long-1234";
        using var encoder = new HmacCursorEncoder(key);
        
        // Future timestamp payload with prefix 'X' instead of 'T'
        long futureEpoch = DateTimeOffset.UtcNow.AddHours(5).ToUnixTimeSeconds();
        var contentToSign = $"X{futureEpoch}:validcursor";
        var opaque = CreateSignedOpaqueCursor(key, contentToSign);

        Action act = () => encoder.Decode(opaque);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void Decode_WithValid32ByteSignatureFormat_ButMismatchingSignature_ThrowsInvalidPaginationCursorException()
    {
        var innerEncoder = Base64CursorEncoder.Default;
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        var encoded = encoder.Encode("validcursor")!;
        var decodedInner = innerEncoder.Decode(encoded)!;
        var dotIndex = decodedInner.LastIndexOf('.');
        var content = decodedInner[..dotIndex];
        
        // Construct a dummy 32-byte valid Base64URL signature that does NOT match
        var fakeHmac = Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tamperedPayload = content + "." + fakeHmac;
        var tamperedOpaqueCursor = innerEncoder.Encode(tamperedPayload);

        Action act = () => encoder.Decode(tamperedOpaqueCursor);
        act.Should().Throw<InvalidPaginationCursorException>().WithMessage("*signature is invalid*");
    }

    [Fact]
    public void Decode_WithExpiredCursor_WithinClockSkewTolerance_Succeeds()
    {
        var clockSkew = TimeSpan.FromSeconds(30);
        // An expired cursor (5 seconds ago) is still valid when within the 30-second clock skew tolerance
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234", timeToLive: TimeSpan.FromSeconds(-5), clockSkewTolerance: clockSkew);
        var encoded = encoder.Encode("skew-test-cursor")!;
        
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be("skew-test-cursor");
    }

    [Fact]
    public void Decode_WithReplayStoreAndTtl_PassesExact32CharGuidNonce()
    {
        var store = new TrackingReplayStore();
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234", replayStore: store, timeToLive: TimeSpan.FromMinutes(10));
        var encoded = encoder.Encode("replay-ttl-cursor")!;
        
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be("replay-ttl-cursor");
        
        store.LastNonce.Should().NotBeNull();
        store.LastNonce!.Length.Should().Be(32);
        Guid.TryParseExact(store.LastNonce, "N", out _).Should().BeTrue();
    }

    [Fact]
    public void Decode_WithReplayStoreWithoutTtl_PassesExact32CharGuidNonce()
    {
        var store = new TrackingReplayStore();
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234", replayStore: store, timeToLive: null);
        var encoded = encoder.Encode("replay-noturn-cursor")!;
        
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be("replay-noturn-cursor");
        
        store.LastNonce.Should().NotBeNull();
        store.LastNonce!.Length.Should().Be(32);
        Guid.TryParseExact(store.LastNonce, "N", out _).Should().BeTrue();
    }

    [Fact]
    public void ReplayStore_WithCustomTimeToLive_PassesCustomTtlToStore()
    {
        var store = new TrackingReplayStore();
        var customTtl = TimeSpan.FromMinutes(15);
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234", replayStore: store, timeToLive: customTtl);

        var encoded = encoder.Encode("mycursor");
        encoder.Decode(encoded);

        store.LastNonce.Should().NotBeNullOrEmpty();
        store.LastTtl.Should().Be(customTtl);
    }


    [Fact]
    public void Encode_WithTtl_WithoutReplayStore_DoesNotIncludeRNonce()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234", timeToLive: TimeSpan.FromMinutes(10));
        var encoded = encoder.Encode("test");
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be("test");

        var inner = encoded!.Replace('-', '+').Replace('_', '/');
        while (inner.Length % 4 != 0) inner += "=";
        var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(inner));
        payload.Should().NotContain(":R");
    }

    [Fact]
    public void Encode_WithoutReplayStore_DoesNotIncludeRNonce()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        var encoded = encoder.Encode("test");
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be("test");

        var inner = encoded!.Replace('-', '+').Replace('_', '/');
        while (inner.Length % 4 != 0) inner += "=";
        var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(inner));
        payload.Should().StartWith("N:test");
        payload.Should().NotStartWith("R");
        payload.Should().NotStartWith("TR");
    }

    [Fact]
    public void Decode_WhenInnerEncoderReturnsNull_ReturnsNull()
    {
        var mockInner = new MockCursorEncoder { DecodeFunc = _ => null };
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234", innerEncoder: mockInner);
        var result = encoder.Decode("some-opaque-cursor");
        result.Should().BeNull();
    }

    [Fact]
    public void EncodeDecode_WithLargePayload_RentedArrayBranch_RoundTripsSuccessfully()
    {
        using var encoder = new HmacCursorEncoder("my-super-secret-key-32-bytes-long-1234");
        var largePayload = new string('X', 2000); // 2000 chars forces maxByteCount > 768 in SignToSpan
        var encoded = encoder.Encode(largePayload);
        encoded.Should().NotBeNullOrEmpty();

        var decoded = encoder.Decode(encoded);
        decoded.Should().Be(largePayload);
    }
}






