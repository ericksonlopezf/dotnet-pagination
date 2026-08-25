// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents the error raised when a time-limited cursor has passed its expiration time.
/// </summary>
/// <remarks>
/// <para>
/// This exception is a subclass of <see cref="InvalidPaginationCursorException"/> so callers
/// can catch it specifically or via the parent type, depending on whether they need to distinguish
/// expiration from tampering.
/// </para>
/// </remarks>
public sealed class ExpiredPaginationCursorException : InvalidPaginationCursorException
{
    /// <summary>
    /// Gets the Coordinated Universal Time (UTC) timestamp at which the cursor expired.
    /// </summary>
    public DateTimeOffset ExpiredAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpiredPaginationCursorException"/> class with the expired cursor value and expiration timestamp.
    /// </summary>
    /// <param name="opaqueCursor">The raw cursor value that failed to decode.</param>
    /// <param name="expiredAt">The UTC timestamp at which the cursor expired.</param>
    public ExpiredPaginationCursorException(string? opaqueCursor, DateTimeOffset expiredAt)
        : base($"The cursor expired at {expiredAt:O}. Restart pagination from the first page.", opaqueCursor)
    {
        ExpiredAt = expiredAt;
    }
}

