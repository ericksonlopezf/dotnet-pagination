// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a factory for creating cursor-based paginated list instances.
/// </summary>
public interface ICursorPagedListFactory
{
    /// <summary>
    /// Creates a new cursor-based paginated list containing the specified items and pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of elements in the page.</typeparam>
    /// <param name="items">The items on the current page.</param>
    /// <param name="totalCount">The optional total number of items across all pages.</param>
    /// <param name="startCursor">The opaque cursor of the first item in the page.</param>
    /// <param name="endCursor">The opaque cursor of the last item in the page.</param>
    /// <param name="hasPreviousPage">A value indicating whether a preceding page exists.</param>
    /// <param name="hasNextPage">A value indicating whether a subsequent page exists.</param>
    /// <returns>A new <see cref="ICursorPagedList{T}"/> instance containing the specified elements and metadata.</returns>
    ICursorPagedList<T> CreateCursorPagedList<T>(IReadOnlyList<T> items, long? totalCount, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage);
}



