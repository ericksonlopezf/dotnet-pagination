// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides extension methods for <see cref="ICursorPagedList{T}"/> instances.
/// </summary>
public static class ICursorPagedListExtensions
{
    /// <summary>
    /// Projects each element of a cursor-paginated list into a new form using the specified transform function.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of elements in the resulting list.</typeparam>
    /// <param name="source">The cursor-paginated list to transform.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new <see cref="ICursorPagedList{TResult}"/> containing the transformed elements and preserved cursor metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/></exception>
    public static ICursorPagedList<TResult> Map<TSource, TResult>(
        this ICursorPagedList<TSource> source,
        Func<TSource, TResult> selector)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        if (source is ICountedCursorPagedList<TSource> countedCursor)
        {
            var mappedItemsCounted = new TResult[countedCursor.Count];
            for (int i = 0; i < countedCursor.Count; i++)
            {
                mappedItemsCounted[i] = selector(countedCursor[i]);
            }
            return new CountedCursorPagedList<TResult>(mappedItemsCounted, countedCursor.ExactTotalCount, countedCursor.StartCursor, countedCursor.EndCursor, countedCursor.HasPreviousPage, countedCursor.HasNextPage);
        }

        var mappedItems = new TResult[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            mappedItems[i] = selector(source[i]);
        }
        
        return new CursorPagedList<TResult>(mappedItems, source.StartCursor, source.EndCursor, source.HasPreviousPage, source.HasNextPage);
    }
}
