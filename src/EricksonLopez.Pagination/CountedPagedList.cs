// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents an immutable paginated list with a known total count.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public sealed class CountedPagedList<T> : PagedList<T>, ICountedPagedList<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountedPagedList{T}"/> class.
    /// </summary>
    internal CountedPagedList(IReadOnlyList<T> items, long totalCount, int page, int pageSize)
        : base(items, totalCount, page, pageSize, hasNextPage: null, hasPreviousPage: null)
    {
    }

    /// <inheritdoc/>
    long ICountedPagedList.TotalCount => ExactTotalCount;

    /// <summary>
    /// Gets the exact total number of items across all pages.
    /// </summary>
    public long ExactTotalCount => TotalCount!.Value;

    /// <summary>
    /// Projects each element of the counted paginated list into a new form using the specified transform function, preserving the total count.
    /// </summary>
    /// <typeparam name="TResult">The type of elements in the resulting list.</typeparam>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new <see cref="CountedPagedList{TResult}"/> containing the transformed elements and preserved count metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
#pragma warning disable CA1061 // Do not hide base class methods — intentional: returns stronger type CountedPagedList<TResult>.
    public new CountedPagedList<TResult> Map<TResult>(Func<T, TResult> selector)
#pragma warning restore CA1061
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        var mapped = new TResult[Count];
        for (int i = 0; i < Count; i++)
        {
            mapped[i] = selector(this[i]);
        }

        return new CountedPagedList<TResult>(mapped, ExactTotalCount, Page, PageSize);
    }
}
