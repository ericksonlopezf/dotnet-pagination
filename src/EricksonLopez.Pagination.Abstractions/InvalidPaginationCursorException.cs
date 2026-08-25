// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents the exception thrown when a pagination cursor cannot be decoded because it is malformed, invalid, or tampered with.
/// </summary>
public class InvalidPaginationCursorException : Exception
{
    /// <summary>
    /// Gets the raw cursor string that failed decoding.
    /// </summary>
    public string? OpaqueCursor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPaginationCursorException"/> class with a specified error message and the invalid cursor value.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="opaqueCursor">The raw cursor value that failed decoding.</param>
    public InvalidPaginationCursorException(string message, string? opaqueCursor)
        : base(message)
    {
        OpaqueCursor = opaqueCursor;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPaginationCursorException"/> class with a specified error message, the invalid cursor value, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="opaqueCursor">The raw cursor value that failed decoding.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidPaginationCursorException(string message, string? opaqueCursor, Exception innerException)
        : base(message, innerException)
    {
        OpaqueCursor = opaqueCursor;
    }
}


