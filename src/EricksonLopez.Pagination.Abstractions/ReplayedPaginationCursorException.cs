// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents the exception thrown when a pagination cursor that has already been consumed is replayed in a replay-protected context.
/// </summary>
public class ReplayedPaginationCursorException : InvalidPaginationCursorException
{
    /// <summary>
    /// Gets the unique nonce associated with the replayed cursor.
    /// </summary>
    public string? Nonce { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayedPaginationCursorException"/> class with a specified error message, the replayed cursor value, and the nonce.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="cursor">The opaque cursor that was replayed.</param>
    /// <param name="nonce">The embedded nonce string.</param>
    public ReplayedPaginationCursorException(string message, string? cursor = null, string? nonce = null)
        : base(message, cursor)
    {
        Nonce = nonce;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayedPaginationCursorException"/> class with a specified error message, a reference to the inner exception that is the cause of this exception, the replayed cursor value, and the nonce.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="cursor">The opaque cursor that was replayed.</param>
    /// <param name="nonce">The embedded nonce string.</param>
    public ReplayedPaginationCursorException(string message, Exception innerException, string? cursor = null, string? nonce = null)
        : base(message, cursor, innerException)
    {
        Nonce = nonce;
    }
}

