// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Redis;

/// <summary>
/// Provides configuration options for <see cref="RedisCursorReplayStore"/>.
/// </summary>
public sealed class RedisCursorReplayStoreOptions
{
    /// <summary>
    /// Gets or sets the key prefix used when storing nonce keys in Redis.
    /// Default is <c>"pagination:replay:"</c>.
    /// </summary>
    public string KeyPrefix { get; set; } = "pagination:replay:";

    /// <summary>
    /// Gets or sets the specific Redis database index to use, or <see langword="null"/> to use the default database.
    /// </summary>
    public int? DatabaseIndex { get; set; }
}
