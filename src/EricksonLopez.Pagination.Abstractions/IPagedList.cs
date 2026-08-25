// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a generic paginated list with offset-based metadata.
/// </summary>
/// <remarks>
/// Covariance (<c>out T</c>) allows assigning an instance to a variable typed with a less-derived element type for reference types.
/// </remarks>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public interface IPagedList<out T> : IReadOnlyList<T>, IPagedList
{
}

/// <summary>
/// Defines offset-based metadata for a paginated list.
/// </summary>
public interface IPagedList
{
    /// <summary>Gets the total number of items across all pages, or <see langword="null"/> if not computed.</summary>
    long? TotalCount { get; }

    /// <summary>
    /// Gets the current page number (1-indexed).
    /// </summary>
    int Page { get; }

    /// <summary>Gets the number of items per page.</summary>
    int PageSize { get; }

    /// <summary>Gets the total number of pages, or <see langword="null"/> if the total count is not computed.</summary>
    long? TotalPages { get; }

    /// <summary>Gets a value indicating whether there is a page before the current one.</summary>
    bool HasPreviousPage { get; }

    /// <summary>Gets a value indicating whether there is a page after the current one.</summary>
    bool HasNextPage { get; }
}



