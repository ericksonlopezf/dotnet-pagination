// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents an immutable paginated list with cursor-based metadata.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public class CursorPagedList<T> : ICursorPagedList<T>
#pragma warning restore S4035
{
    private readonly IReadOnlyList<T> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorPagedList{T}"/> class with the specified items and cursor metadata.
    /// </summary>
    /// <param name="items">The collection of elements contained in the current page.</param>
    /// <param name="startCursor">The encoded opaque cursor pointing to the first item in the page, or <see langword="null"/> if empty.</param>
    /// <param name="endCursor">The encoded opaque cursor pointing to the last item in the page, or <see langword="null"/> if empty.</param>
    /// <param name="hasPreviousPage"><see langword="true"/> if a preceding page exists; otherwise, <see langword="false"/>.</param>
    /// <param name="hasNextPage"><see langword="true"/> if a subsequent page exists; otherwise, <see langword="false"/>.</param>
    public CursorPagedList(
        IReadOnlyList<T> items,
        string? startCursor,
        string? endCursor,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        _items = items ?? Array.Empty<T>();
        StartCursor = startCursor;
        EndCursor = endCursor;
        HasPreviousPage = hasPreviousPage;
        HasNextPage = hasNextPage;
    }

    /// <inheritdoc />
    public string? StartCursor { get; }

    /// <inheritdoc />
    public string? EndCursor { get; }

    /// <inheritdoc />
    public bool HasPreviousPage { get; }

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
    /// Creates a new <see cref="CursorPagedList{T}"/> from the specified items and cursor metadata.
    /// </summary>
    /// <param name="items">The collection of elements contained in the current page.</param>
    /// <param name="startCursor">The encoded opaque cursor pointing to the first item in the page, or <see langword="null"/> if empty.</param>
    /// <param name="endCursor">The encoded opaque cursor pointing to the last item in the page, or <see langword="null"/> if empty.</param>
    /// <param name="hasPreviousPage"><see langword="true"/> if a preceding page exists; otherwise, <see langword="false"/>.</param>
    /// <param name="hasNextPage"><see langword="true"/> if a subsequent page exists; otherwise, <see langword="false"/>.</param>
    /// <returns>A new <see cref="CursorPagedList{T}"/> containing the specified elements and metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    [Pure]
    public static CursorPagedList<T> Create(
        IReadOnlyList<T> items,
        string? startCursor,
        string? endCursor,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));

        return new CursorPagedList<T>(items, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }



    /// <summary>
    /// Projects each element of the paginated list into a new form using the specified transform function, preserving cursor metadata.
    /// </summary>
    /// <typeparam name="TResult">The type of elements contained in the projected page.</typeparam>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new <see cref="CursorPagedList{TResult}"/> containing the transformed elements and preserved metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    // Stryker disable once all
    public virtual CursorPagedList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        var count = _items.Count;
        var mapped = new TResult[count];
        for (var i = 0; i < count; i++)
        {
            mapped[i] = selector(_items[i]);
        }

        return new CursorPagedList<TResult>(mapped, StartCursor, EndCursor, HasPreviousPage, HasNextPage);
    }

    /// <summary>
    /// Represents an empty cursor-paginated list with no items or cursors.
    /// </summary>
    public static readonly CursorPagedList<T> Empty = new([], null, null, false, false);
}

