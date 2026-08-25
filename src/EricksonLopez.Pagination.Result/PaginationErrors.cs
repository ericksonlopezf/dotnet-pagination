// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Result;

namespace EricksonLopez.Pagination.Result;

/// <summary>
/// Provides standard <see cref="Error"/> definitions for pagination failures.
/// </summary>
public static class PaginationErrors
{
    /// <summary>
    /// Gets an error indicating the provided pagination cursor is invalid or malformed.
    /// </summary>
    public static readonly Error InvalidCursor = Error.Validation(
        "Pagination.InvalidCursor", 
        "The provided pagination cursor is invalid or malformed.");

    /// <summary>
    /// Gets an error indicating the provided pagination cursor has expired.
    /// </summary>
    public static readonly Error ExpiredCursor = Error.Validation(
        "Pagination.ExpiredCursor", 
        "The provided pagination cursor has expired.");

    /// <summary>
    /// Gets an error indicating the provided pagination cursor has already been used (replayed).
    /// </summary>
    public static readonly Error ReplayedCursor = Error.Validation(
        "Pagination.ReplayedCursor", 
        "The provided pagination cursor has already been used and cannot be replayed.");
}
