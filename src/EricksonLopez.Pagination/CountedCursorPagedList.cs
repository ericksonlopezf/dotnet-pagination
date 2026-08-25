// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents an immutable paginated list with cursor-based metadata and an exact total count.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public sealed class CountedCursorPagedList<T> : CursorPagedList<T>, ICountedCursorPagedList<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountedCursorPagedList{T}"/> class with the specified items, total count, and cursor metadata.
    /// </summary>
    /// <param name="items">The materialized page of items.</param>
    /// <param name="totalCount">The exact total number of items across all pages.</param>
    /// <param name="startCursor">The encoded opaque cursor for the first item, or <see langword="null"/> if the page is empty.</param>
    /// <param name="endCursor">The encoded opaque cursor for the last item, or <see langword="null"/> if the page is empty.</param>
    /// <param name="hasPreviousPage"><see langword="true"/> if there is a page before this one; otherwise, <see langword="false"/>.</param>
    /// <param name="hasNextPage"><see langword="true"/> if there is a page after this one; otherwise, <see langword="false"/>.</param>
    public CountedCursorPagedList(
        IReadOnlyList<T> items,
        long totalCount,
        string? startCursor,
        string? endCursor,
        bool hasPreviousPage,
        bool hasNextPage)
        : base(items, startCursor, endCursor, hasPreviousPage, hasNextPage)
    {
        ExactTotalCount = totalCount;
    }

    /// <inheritdoc />
    public long ExactTotalCount { get; }

    /// <inheritdoc />
    public long? TotalCount => ExactTotalCount;

    /// <summary>
    /// Projects each element of the paginated list into a new form using the specified transform function, preserving cursor metadata and the total count.
    /// </summary>
    /// <typeparam name="TResult">The type of elements in the resulting list.</typeparam>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new <see cref="CountedCursorPagedList{TResult}"/> containing the transformed elements and preserved metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public new CountedCursorPagedList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        var count = Count;
        var mapped = new TResult[count];
        for (var i = 0; i < count; i++)
        {
            mapped[i] = selector(this[i]);
        }

        return new CountedCursorPagedList<TResult>(mapped, ExactTotalCount, StartCursor, EndCursor, HasPreviousPage, HasNextPage);
    }
}

