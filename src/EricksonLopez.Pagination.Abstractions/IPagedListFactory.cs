// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a factory for creating offset-based paginated list instances.
/// </summary>
public interface IPagedListFactory
{
    /// <summary>
    /// Creates a new offset-based paginated list containing the specified items and pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of elements in the page.</typeparam>
    /// <param name="items">The collection of items on the current page.</param>
    /// <param name="totalCount">The total number of items across all pages, or <see langword="null"/> if uncounted.</param>
    /// <param name="page">The current 1-indexed page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="hasNextPage">A value indicating whether a subsequent page exists, or <see langword="null"/> if derived from total count.</param>
    /// <returns>A new <see cref="IPagedList{T}"/> instance containing the specified elements and metadata.</returns>
    IPagedList<T> CreatePagedList<T>(IReadOnlyList<T> items, long? totalCount, int page, int pageSize, bool? hasNextPage);
}



