// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides an in-memory, thread-safe implementation of <see cref="ICursorReplayStore"/>.
/// </summary>
/// <remarks>
/// <b>Deployment Note:</b> This store operates entirely within the local process memory. It is suitable for
/// single-node deployments, integration tests, and local development. For multi-node or clustered environments
/// (e.g. Kubernetes with multiple replicas), use a distributed implementation of <see cref="ICursorReplayStore"/>
/// (such as Redis or a shared database store) to ensure nonces are synchronized across all running instances.
/// </remarks>
public sealed class InMemoryCursorReplayStore : ICursorReplayStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _nonces = new();
    private int _cleanupCounter;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="nonce"/> is <see langword="null"/></exception>
    public bool TryAcquireNonce(string nonce, TimeSpan timeToLive)
    {
        ArgumentNullException.ThrowIfNull(nonce);

        // Stryker disable all : Opportunistic periodic background cleanup optimization
        // Opportunistic cleanup every 256 acquisitions
        if ((Interlocked.Increment(ref _cleanupCounter) & 0xFF) == 0)
        {
            CleanupExpired();
        }
        // Stryker restore all

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(timeToLive);

        // If nonce already exists and hasn't expired, acquisition fails (replay detected)
        if (_nonces.TryGetValue(nonce, out var existingExpiry))
        {
            // Stryker disable once all : Clock tick equality boundary condition on DateTimeOffset.UtcNow
            if (existingExpiry > now)
            {
                return false;
            }

            // If it had expired, try updating with new expiry
            return _nonces.TryUpdate(nonce, expiresAt, existingExpiry);
        }

        return _nonces.TryAdd(nonce, expiresAt);
    }

    /// <inheritdoc/>
    public Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        return Task.FromResult(TryAcquireNonce(nonce, timeToLive));
    }

    // Stryker disable all : Opportunistic background cleanup
    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in _nonces)
        {
            if (pair.Value <= now)
            {
                _nonces.TryRemove(pair.Key, out _);
            }
        }
    }
    // Stryker restore all
}



