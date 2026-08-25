// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a generic paginated list with total count metadata.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public interface ICountedPagedList<out T> : IPagedList<T>, ICountedPagedList
{
}

/// <summary>
/// Extends <see cref="IPagedList"/> with total count metadata for offset-based pagination.
/// </summary>
public interface ICountedPagedList : IPagedList
{
    /// <summary>
    /// Gets the exact total number of items across all pages.
    /// </summary>
    new long TotalCount { get; }

    /// <summary>Gets the exact total number of items across all pages.</summary>
    long ExactTotalCount { get; }
}
