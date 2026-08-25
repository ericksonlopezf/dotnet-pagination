// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Pagination;

// Stryker disable all : Structured logging events
/// <summary>
/// Provides structured logging event identifiers and helper methods for cursor pagination events.
/// </summary>
public static partial class PaginationLogEvents
{
    /// <summary>
    /// Represents an event indicating that a time-limited cursor has expired.
    /// </summary>
    public static readonly EventId CursorExpired = new(1001, nameof(CursorExpired));

    /// <summary>
    /// Represents an event indicating that a cursor failed signature validation or was tampered with.
    /// </summary>
    public static readonly EventId CursorTampered = new(1002, nameof(CursorTampered));

    /// <summary>
    /// Represents an event indicating that a cursor replay was detected.
    /// </summary>
    public static readonly EventId CursorReplayed = new(1003, nameof(CursorReplayed));

    /// <summary>
    /// Logs a warning when a pagination cursor has expired.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cursor">The expired opaque cursor string.</param>
    /// <param name="expiredAt">The timestamp at which the cursor expired.</param>
    public static void LogCursorExpired(ILogger? logger, string? cursor, System.DateTimeOffset expiredAt)
    {
        if (logger == null || !logger.IsEnabled(LogLevel.Warning)) return;
        logger.Log(LogLevel.Warning, CursorExpired, "Pagination cursor '{Cursor}' expired at {ExpiredAt:O}.", cursor, expiredAt);
    }

    /// <summary>
    /// Logs a warning when cursor tampering or an invalid signature is detected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cursor">The tampered opaque cursor string.</param>
    public static void LogCursorTampered(ILogger? logger, string? cursor)
    {
        if (logger == null || !logger.IsEnabled(LogLevel.Warning)) return;
        logger.Log(LogLevel.Warning, CursorTampered, "Pagination cursor '{Cursor}' failed HMAC signature validation. Possible tampering.", cursor);
    }

    /// <summary>
    /// Logs a warning when cursor replay is detected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cursor">The replayed opaque cursor string.</param>
    /// <param name="nonce">The nonce embedded in the cursor that was already consumed.</param>
    public static void LogCursorReplayed(ILogger? logger, string? cursor, string? nonce)
    {
        if (logger == null || !logger.IsEnabled(LogLevel.Warning)) return;
        logger.Log(LogLevel.Warning, CursorReplayed, "Pagination cursor '{Cursor}' with nonce '{Nonce}' has already been consumed (replay attack detected).", cursor, nonce);
    }
    // Stryker restore all
}


