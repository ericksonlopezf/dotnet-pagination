// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a cursor-paginated list with an explicit total count of elements.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public interface ICountedCursorPagedList<out T> : ICursorPagedList<T>, ICountedCursorPagedList
{
}

/// <summary>
/// Defines cursor-based pagination metadata with an explicit total count of elements.
/// </summary>
public interface ICountedCursorPagedList : ICursorPagedList
{
    /// <summary>Gets the exact total number of items across all pages.</summary>
    long ExactTotalCount { get; }

    /// <summary>Gets the total number of items across all pages, or <see langword="null"/> if not computed.</summary>
    long? TotalCount { get; }
}



