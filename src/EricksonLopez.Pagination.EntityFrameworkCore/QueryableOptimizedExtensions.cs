// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.EntityFrameworkCore;

/// <summary>
/// Provides advanced, optimized extension methods for EF Core paginated results.
/// </summary>
public static class QueryableOptimizedExtensions
{
    /// <summary>
    /// Materializes the query into a <see cref="PagedList{T}"/> using the deferred join pattern.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the primary key.</typeparam>
    /// <param name="source">The queryable to paginate.</param>
    /// <param name="keySelector">An expression selecting the primary key of the entity.</param>
    /// <param name="parameters">The pagination parameters defining page and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <param name="deferredMaxPageSize">The threshold above which deferred pagination falls back to standard offset pagination.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the paged list.
    /// </returns>
    public static async Task<IPagedList<T>> ToPagedListDeferredAsync<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = 1000,
        CancellationToken cancellationToken = default,
        int deferredMaxPageSize = 50)
        where TKey : notnull
    {
        // Stryker disable all
        var effectivePageSize = maxPageSize.HasValue 
            ? Math.Min(parameters.PageSize, maxPageSize.Value) 
            : parameters.PageSize;

        // The deferred join pattern with `WHERE IN` degrades performance for large pages
        // due to database limits (e.g. 65535 params in PostgreSQL) and index scan degenerating to table scans.
        if (effectivePageSize > deferredMaxPageSize)
        {
            return await source.ToPagedListAsync(parameters, countTotal, maxPageSize, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        // Stryker restore all

        if (countTotal)
        {
            var count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
            if (count == 0) return new PagedList<T>([], 0, parameters.Page, effectivePageSize);

            var pagedKeys = await source
                .Select(keySelector)
                .Skip((parameters.Page - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (pagedKeys.Count == 0) return new PagedList<T>([], count, parameters.Page, effectivePageSize);

            var items = await FetchEntitiesByKeysAsync(source, keySelector, pagedKeys, cancellationToken).ConfigureAwait(false);
            return new PagedList<T>(items, count, parameters.Page, effectivePageSize);
        }
        else
        {
            var pagedKeys = await source
                .Select(keySelector)
                .Skip((parameters.Page - 1) * effectivePageSize)
                .Take(effectivePageSize + 1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (pagedKeys.Count == 0) return new PagedList<T>([], 0, parameters.Page, effectivePageSize);

            var hasNextPage = pagedKeys.Count > effectivePageSize;
            if (hasNextPage) pagedKeys.RemoveAt(effectivePageSize);

            var items = await FetchEntitiesByKeysAsync(source, keySelector, pagedKeys, cancellationToken).ConfigureAwait(false);
            return new PagedList<T>(items, null, parameters.Page, effectivePageSize, hasNextPage);
        }
    }

    

    private static async Task<List<T>> FetchEntitiesByKeysAsync<T, TKey>(
        IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        List<TKey> pagedKeys,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        var pagedKeysConst = Expression.Constant(pagedKeys);
        var containsBody = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(TKey)], pagedKeysConst, keySelector.Body);
        var containsLambda = Expression.Lambda<Func<T, bool>>(containsBody, keySelector.Parameters);

        // Fetch matching entities. Note: SQL's WHERE id IN (...) does NOT guarantee
        // return order matches the input key order (SQL Server ignores it entirely).
        // We re-sort in memory after fetching to preserve the page order from pagedKeys.
        var unordered = await source.Where(containsLambda).ToListAsync(cancellationToken).ConfigureAwait(false);

        // Compile once via cache to avoid IL emission on every call.
        var compiledKey = PaginationExpressionCache.GetOrCompile(keySelector);
        var keyToItem = new Dictionary<TKey, T>(pagedKeys.Count);
        foreach (var item in unordered)
        {
            var key = compiledKey(item);
            if (key is not null)
            {
                keyToItem.TryAdd(key, item);
            }
        }

        // Reorder to match the pagedKeys sequence (which reflects the DB sort order for the window).
        var ordered = new List<T>(pagedKeys.Count);
        foreach (var key in pagedKeys)
        {
            #pragma warning disable S4158
            if (keyToItem.TryGetValue(key, out var item))
#pragma warning restore S4158
            {
                ordered.Add(item);
            }
        }

        return ordered;
    }
}



