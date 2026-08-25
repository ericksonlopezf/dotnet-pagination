// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Internal;
using LinqToDB;

namespace EricksonLopez.Pagination.LinqToDB;

/// <summary>
/// Provides extension methods for LinqToDB <see cref="IQueryable{T}"/> to create paginated results.
/// </summary>
public static class QueryableLinqToDBExtensions
{
    /// <summary>
    /// Initializes a fluent builder for multi-column keyset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="defaultPageSize">The fallback page size when none is specified.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="acceptLegacyCursors">A value indicating whether to accept legacy cursor formats.</param>
    /// <returns>A new <see cref="KeysetBuilder{T}"/> instance.</returns>
    public static KeysetBuilder<T> Keyset<T>(
        this IQueryable<T> source,
        CursorPaginationParameters parameters,
        int defaultPageSize = 10,
        ICursorEncoder? cursorEncoder = null,
        bool acceptLegacyCursors = true)
    {
        return new KeysetBuilder<T>(source, parameters, defaultPageSize, cursorEncoder, acceptLegacyCursors);
    }

    /// <summary>
    /// Materializes the query into a <see cref="PagedList{T}"/> using offset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <param name="countTotal">A value indicating whether to execute a count query for the total number of items.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the paged list.
    /// </returns>
    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = null,
        IPaginationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once all
        var actualMaxPageSize = maxPageSize ?? options?.MaxPageSize ?? PaginationSettings.MaxPageSize;
        var effectivePageSize = Math.Min(parameters.PageSize, actualMaxPageSize);
        var skipAmount = (parameters.Page - 1) * effectivePageSize;

        if (countTotal)
        {
            var count = await source.CountAsync(cancellationToken).ConfigureAwait(false);

            // Stryker disable once all
            if (count == 0)
            {
                return PagedList<T>.Empty(parameters);
            }

            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return PagedList<T>.WithCount(items, parameters, count);
        }
        else
        {
            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize + 1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Stryker disable once all
            var hasNextPage = items.Count > effectivePageSize;
            if (hasNextPage)
            {
                items.RemoveAt(effectivePageSize);
            }

            return PagedList<T>.WithoutCount(items, parameters, hasNextPage);
        }
    }

    /// <summary>
    /// Materializes the query into a projected <see cref="PagedList{TResult}"/> using offset pagination.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="selector">The projection expression.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <param name="countTotal">A value indicating whether to execute a count query for the total number of items.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the projected paged list.
    /// </returns>
    public static async Task<IPagedList<TResult>> ToPagedListAsync<T, TResult>(
        this IQueryable<T> source,
        Expression<Func<T, TResult>> selector,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = null,
        CancellationToken cancellationToken = default,
        IPaginationOptions? options = null)
    {
        // Stryker disable once all
        var actualMaxPageSize = maxPageSize ?? options?.MaxPageSize ?? PaginationSettings.MaxPageSize;
        var effectivePageSize = Math.Min(parameters.PageSize, actualMaxPageSize);
        var skipAmount = (parameters.Page - 1) * effectivePageSize;

        if (countTotal)
        {
            var count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
            if (count == 0) return PagedList<TResult>.Empty(parameters);

            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize)
                .Select(selector)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            
            return PagedList<TResult>.WithCount(items, parameters, count);
        }
        else
        {
            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize + 1)
                .Select(selector)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Stryker disable once all
            var hasNextPage = items.Count > effectivePageSize;
            if (hasNextPage) items.RemoveAt(effectivePageSize);

            return PagedList<TResult>.WithoutCount(items, parameters, hasNextPage);
        }
    }

    /// <summary>
    /// Materializes the query into a <see cref="PagedList{T}"/> using count-less offset pagination (N+1 lookahead probe).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the paged list without computing total count.</returns>
    public static Task<IPagedList<T>> ToPagedListWithoutCountAsync<T>(
        this IQueryable<T> source,
        PaginationParameters parameters,
        int? maxPageSize = null,
        IPaginationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return source.ToPagedListAsync(parameters, countTotal: false, maxPageSize, options, cancellationToken);
    }

    /// <summary>
    /// Materializes the query into a projected <see cref="PagedList{TResult}"/> using count-less offset pagination (N+1 lookahead probe).
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="selector">The projection expression.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>A task representing the asynchronous operation, containing the projected paged list without computing total count.</returns>
    public static Task<IPagedList<TResult>> ToPagedListWithoutCountAsync<T, TResult>(
        this IQueryable<T> source,
        Expression<Func<T, TResult>> selector,
        PaginationParameters parameters,
        int? maxPageSize = null,
        CancellationToken cancellationToken = default,
        IPaginationOptions? options = null)
    {
        return source.ToPagedListAsync(selector, parameters, countTotal: false, maxPageSize, cancellationToken, options);
    }
}





