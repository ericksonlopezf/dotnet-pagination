// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EricksonLopez.Pagination.Redis;

/// <summary>
/// Provides a distributed implementation of <see cref="ICursorReplayStore"/> backed by Redis using atomic SETNX operations.
/// </summary>
public sealed class RedisCursorReplayStore : ICursorReplayStore
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisCursorReplayStoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCursorReplayStore"/> class.
    /// </summary>
    /// <param name="connection">The Redis connection multiplexer.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public RedisCursorReplayStore(
        IConnectionMultiplexer connection,
        IOptions<RedisCursorReplayStoreOptions>? options = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _options = options?.Value ?? new RedisCursorReplayStoreOptions();
    }

    /// <inheritdoc />
    public bool TryAcquireNonce(string nonce, TimeSpan timeToLive)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        var db = GetDatabase();
        string key = _options.KeyPrefix + nonce;

        return db.StringSet(key, "1", expiry: timeToLive, keepTtl: false, when: When.NotExists, flags: CommandFlags.None);
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var db = GetDatabase();
        string key = _options.KeyPrefix + nonce;

        // Redis StringSetAsync with When.NotExists implements atomic SETNX with expiration
        bool acquired = await db.StringSetAsync(key, "1", expiry: timeToLive, keepTtl: false, when: When.NotExists, flags: CommandFlags.None).ConfigureAwait(false);
        return acquired;
    }

    private IDatabase GetDatabase()
    {
        return _options.DatabaseIndex.HasValue
            ? _connection.GetDatabase(_options.DatabaseIndex.Value)
            : _connection.GetDatabase();
    }
}
