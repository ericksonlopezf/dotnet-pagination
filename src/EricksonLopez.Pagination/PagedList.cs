// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents an immutable paginated list providing offset-based pagination metadata.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public class PagedList<T> : IPagedList<T>
#pragma warning restore S4035
{
    private readonly IReadOnlyList<T> _items;
    private readonly bool? _hasPreviousPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedList{T}"/> class.
    /// </summary>
    /// <param name="items">The materialized page of items.</param>
    /// <param name="totalCount">The total number of items across all pages, or <see langword="null"/> when not computed.</param>
    /// <param name="page">The current 1-indexed page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="hasNextPage"><see langword="true"/> if there is a next page; otherwise, <see langword="false"/>.</param>
    /// <param name="hasPreviousPage"><see langword="true"/> if there is a previous page; otherwise, <see langword="false"/>.</param>
    internal PagedList(
        IReadOnlyList<T> items,
        long? totalCount,
        int page,
        int pageSize,
        bool? hasNextPage = null,
        bool? hasPreviousPage = null)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than or equal to 1.");
        if (totalCount.HasValue && totalCount.Value < 0) throw new ArgumentOutOfRangeException(nameof(totalCount), "TotalCount cannot be negative.");

        _items = items ?? Array.Empty<T>();
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = totalCount.HasValue ? (totalCount.Value + pageSize - 1L) / pageSize : null;

        if (totalCount.HasValue)
        {
            // When the total count is known to be zero, there cannot be a next page regardless of
            // the caller's hasNextPage override. Allowing HasNextPage=true when TotalCount=0 produces
            // a semantically impossible state that will confuse API consumers and GraphQL clients.
            if (totalCount.Value == 0)
            {
                HasNextPage = false;
            }
            else
            {
                HasNextPage = Page < TotalPages;
            }
        }
        else if (hasNextPage.HasValue)
        {
            HasNextPage = hasNextPage.Value;
        }

        _hasPreviousPage = hasPreviousPage;
    }

    /// <inheritdoc />
    public long? TotalCount { get; }

    /// <summary>
    /// Gets the current page number (1-indexed).
    /// </summary>
    public int Page { get; }

    /// <inheritdoc />
    public int PageSize { get; }

    /// <inheritdoc />
    public long? TotalPages { get; }


    /// <inheritdoc />
    public bool HasPreviousPage 
    {
        get
        {
            if (_hasPreviousPage.HasValue) return _hasPreviousPage.Value;
            return Page > 1;
        }
    }

    /// <inheritdoc />
    public bool HasNextPage { get; }

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public T this[int index] => _items[index];

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();



    // ─── Factory methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="CountedPagedList{T}"/> with the specified items, pagination parameters, and known total count.
    /// </summary>
    /// <param name="items">The materialized page of items.</param>
    /// <param name="parameters">The pagination parameters used to request the page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <returns>A new <see cref="CountedPagedList{T}"/> containing the specified elements and total count metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    [Pure]
    public static CountedPagedList<T> WithCount(
        IReadOnlyList<T> items,
        PaginationParameters parameters,
        long totalCount)
    {
        // Stryker disable all
        if (items is null) throw new ArgumentNullException(nameof(items));

        return new CountedPagedList<T>(items, totalCount, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// Creates a new <see cref="PagedList{T}"/> without an exact total count using forward-probe indicator metadata.
    /// </summary>
    /// <param name="items">The materialized page of items.</param>
    /// <param name="parameters">The pagination parameters used to request the page.</param>
    /// <param name="hasNextPage"><see langword="true"/> if a subsequent page exists; otherwise, <see langword="false"/>.</param>
    /// <param name="hasPreviousPage"><see langword="true"/> if a preceding page exists; otherwise, <see langword="false"/>.</param>
    /// <returns>A new <see cref="PagedList{T}"/> containing the specified elements and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    [Pure]
    public static PagedList<T> WithoutCount(
        IReadOnlyList<T> items,
        PaginationParameters parameters,
        bool hasNextPage,
        bool? hasPreviousPage = null)
    {
        // Stryker disable all
        if (items is null) throw new ArgumentNullException(nameof(items));

        return new PagedList<T>(items, null, parameters.Page, parameters.PageSize, hasNextPage, hasPreviousPage);
    }


    /// <summary>
    /// Creates an empty <see cref="CountedPagedList{T}"/> with a total count of zero.
    /// </summary>
    /// <param name="parameters">The pagination parameters specifying the page size and number.</param>
    /// <returns>An empty paginated list with zero elements and zero total count.</returns>
    [Pure]
    public static CountedPagedList<T> Empty(PaginationParameters parameters)
    {
        return new CountedPagedList<T>([], 0, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// Projects each element of the paginated list into a new form using the specified transform function.
    /// </summary>
    /// <typeparam name="TResult">The type of elements contained in the projected page.</typeparam>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new <see cref="PagedList{TResult}"/> containing the projected elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public PagedList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        // Stryker disable all
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        var mapped = new TResult[_items.Count];
        for (int i = 0; i < _items.Count; i++)
        {
            mapped[i] = selector(_items[i]);
        }

        return new PagedList<TResult>(mapped, TotalCount, Page, PageSize, HasNextPage, _hasPreviousPage);
    }
    // Stryker restore all
}

