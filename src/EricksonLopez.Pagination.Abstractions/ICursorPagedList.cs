// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a generic cursor-paginated list returned by keyset pagination queries.
/// </summary>
/// <remarks>
/// Covariance (<c>out T</c>) allows assigning an instance to a variable typed with a less-derived element type for reference types.
/// </remarks>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public interface ICursorPagedList<out T> : IReadOnlyList<T>, ICursorPagedList
{
}

/// <summary>
/// Defines cursor-based pagination metadata for a paginated result set.
/// </summary>
public interface ICursorPagedList
{
    /// <summary>Gets the opaque cursor to the first item in the current page.</summary>
    string? StartCursor { get; }

    /// <summary>Gets the opaque cursor to the last item in the current page.</summary>
    string? EndCursor { get; }

    /// <summary>Gets a value indicating whether there is a page before the current one.</summary>
    bool HasPreviousPage { get; }

    /// <summary>Gets a value indicating whether there is a page after the current one.</summary>
    bool HasNextPage { get; }
}




