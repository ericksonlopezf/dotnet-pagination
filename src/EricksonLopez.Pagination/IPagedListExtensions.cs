// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides extension methods for <see cref="IPagedList{T}"/> and <see cref="ICountedPagedList{T}"/> instances.
/// </summary>
public static class PagedListExtensions
{
    /// <summary>
    /// Projects each element of a paginated list into a new form using deferred evaluation without allocating an array upfront.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of elements in the resulting list.</typeparam>
    /// <param name="source">The paginated list to transform.</param>
    /// <param name="selector">A transform function to apply to each source element.</param>
    /// <returns>An <see cref="IPagedList{TResult}"/> whose elements are projected lazily on enumeration or indexing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/></exception>
    public static IPagedList<TResult> LazyMap<TSource, TResult>(
        this IPagedList<TSource> source,
        Func<TSource, TResult> selector)

    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        return new MappedPagedList<TSource, TResult>(source, selector);
    }

    /// <summary>
    /// Projects each element of a paginated list into a new form, immediately materializing the projected elements into a new list.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source page.</typeparam>
    /// <typeparam name="TResult">The type of elements in the projected page.</typeparam>
    /// <param name="source">The paginated list to transform.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new <see cref="IPagedList{TResult}"/> containing the projected elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/></exception>
    public static IPagedList<TResult> Map<TSource, TResult>(
        this IPagedList<TSource> source,
        Func<TSource, TResult> selector)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (selector is null) throw new ArgumentNullException(nameof(selector));


        // Stryker disable all : Fast path optimizations, fallback behaves identically for basic valid inputs.
        if (source is CountedPagedList<TSource> counted)
        {
            return counted.Map(selector);
        }
        if (source is PagedList<TSource> paged)
        {
            return paged.Map(selector);
        }
        // Stryker restore all

        var mappedItems = new TResult[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            mappedItems[i] = selector(source[i]);
        }
        
        return new PagedList<TResult>(
            mappedItems, 
            source.TotalCount, 
            source.Page, 
            source.PageSize, 
            source.HasNextPage);
    }

    /// <summary>
    /// Projects each element of a counted paginated list into a new form, immediately materializing the projected elements and preserving total count metadata.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source page.</typeparam>
    /// <typeparam name="TResult">The type of elements in the projected page.</typeparam>
    /// <param name="source">The counted paginated list to transform.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new <see cref="ICountedPagedList{TResult}"/> containing the projected elements and preserved count metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/></exception>
    public static ICountedPagedList<TResult> Map<TSource, TResult>(
        this ICountedPagedList<TSource> source,
        Func<TSource, TResult> selector)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        // Stryker disable all : Fast path optimizations, fallback behaves identically for basic valid inputs.
        if (source is CountedPagedList<TSource> counted)
        {
            return counted.Map(selector);
        }
        // Stryker restore all

        var mappedItems = new TResult[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            mappedItems[i] = selector(source[i]);
        }
        
        return new CountedPagedList<TResult>(
            mappedItems, 
            source.ExactTotalCount, 
            source.Page, 
            source.PageSize);
    }
}

