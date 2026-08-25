// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace EricksonLopez.Pagination.MongoDB;

/// <summary>
/// Provides extension methods for MongoDB <see cref="IQueryable{T}"/> to execute offset pagination queries.
/// </summary>
public static class MongoOffsetPaginationExtensions
{
// ─── Offset pagination ────────────────────────────────────────────────────

    /// <summary>
    /// Materializes the query into a <see cref="PagedList{T}"/> using offset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="factory">An optional factory used to instantiate the paged list.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the paged list.
    /// </returns>
    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = 1000,
        IPagedListFactory? factory = null,
        CancellationToken cancellationToken = default)
    {
        factory ??= DefaultPagedListFactory.Instance;
        var effectivePageSize = maxPageSize.HasValue ? Math.Min(parameters.PageSize, maxPageSize.Value) : parameters.PageSize;

        if (countTotal)
        {
            // Stryker disable once boolean
var count = await source.LongCountAsync(cancellationToken).ConfigureAwait(false);

            // Stryker disable once all : Early return optimization on count == 0
            if (count == 0)
            {
                return factory.CreatePagedList<T>([], 0, parameters.Page, effectivePageSize, false);
            }

            var items = await source
                .Skip(((parameters.Page - 1) * effectivePageSize))
                .Take(effectivePageSize)
                .ToListAsync(cancellationToken)
                // Stryker disable once boolean
.ConfigureAwait(false);

            return factory.CreatePagedList(items, count, parameters.Page, effectivePageSize, null);
        }
        else
        {
            var pageSize = effectivePageSize;
            var items = await source
                .Skip(((parameters.Page - 1) * effectivePageSize))
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken)
                // Stryker disable once boolean
.ConfigureAwait(false);

            var hasNextPage = items.Count > pageSize;
            if (hasNextPage)
            {
                items.RemoveAt(pageSize);
            }

            return factory.CreatePagedList(items, null, parameters.Page, effectivePageSize, hasNextPage);
        }
    }

    /// <summary>
    /// Materializes the query into a projected <see cref="PagedList{TResult}"/> using offset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="selector">A projection expression applied inside the database.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="factory">An optional factory used to instantiate the paged list.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the projected paged list.
    /// </returns>
    public static async Task<IPagedList<TResult>> ToPagedListAsync<T, TResult>(
        this IQueryable<T> source,
        Expression<Func<T, TResult>> selector,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = 1000,
        IPagedListFactory? factory = null,
        CancellationToken cancellationToken = default)
    {
        factory ??= DefaultPagedListFactory.Instance;
        var effectivePageSize = maxPageSize.HasValue ? Math.Min(parameters.PageSize, maxPageSize.Value) : parameters.PageSize;

        if (countTotal)
        {
            // Stryker disable once boolean
var count = await source.LongCountAsync(cancellationToken).ConfigureAwait(false);
            // Stryker disable once boolean : empty list has no next page
            if (count == 0) return factory.CreatePagedList<TResult>([], 0, parameters.Page, effectivePageSize, false);

            var items = await source
                .Skip(((parameters.Page - 1) * effectivePageSize))
                .Take(effectivePageSize)
                .Select(selector)
                .ToListAsync(cancellationToken)
                // Stryker disable once boolean
.ConfigureAwait(false);

            return factory.CreatePagedList(items, count, parameters.Page, effectivePageSize, null);
        }
        else
        {
            var pageSize = effectivePageSize;
            var items = await source
                .Skip(((parameters.Page - 1) * effectivePageSize))
                .Take(pageSize + 1)
                .Select(selector)
                .ToListAsync(cancellationToken)
                // Stryker disable once boolean
.ConfigureAwait(false);

            var hasNextPage = items.Count > pageSize;
            if (hasNextPage) items.RemoveAt(pageSize);

            return factory.CreatePagedList(items, null, parameters.Page, parameters.PageSize, hasNextPage);
        }
    }

    // ─── Integrated Filter + Sort + Paginate overloads ──────────────────────

    /// <summary>
    /// Applies dynamic filtering, sorting, and offset pagination in a single operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to filter, sort, and paginate.</param>
    /// <param name="filter">The dynamic filter parameters.</param>
    /// <param name="sortBy">The dynamic sort parameters.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="factory">An optional factory used to instantiate the paged list.</param>
    /// <param name="unknownFieldBehavior">The behavior when an unrecognized field is encountered during filtering.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the paged list.
    /// </returns>
    [RequiresUnreferencedCode("ApplyFilter and ApplySort use reflection, which is incompatible with trimming.")]
    public static Task<IPagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        FilterParameters filter,
        SortParameters sortBy,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = 1000,
        IPagedListFactory? factory = null,
        FilterUnknownFieldBehavior unknownFieldBehavior = FilterUnknownFieldBehavior.Ignore,
        CancellationToken cancellationToken = default)
    {
        return source
            .ApplyFilter(filter, unknownFieldBehavior: unknownFieldBehavior)
            .ApplySort(sortBy)
            .ToPagedListAsync(parameters, countTotal, maxPageSize, factory, cancellationToken);
    }

    /// <summary>
    /// Applies dynamic filtering, sorting, and offset pagination with server-side projection in a single operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source to filter, sort, project, and paginate.</param>
    /// <param name="filter">The dynamic filter parameters.</param>
    /// <param name="sortBy">The dynamic sort parameters.</param>
    /// <param name="selector">A projection expression applied inside the database.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="factory">An optional factory used to instantiate the paged list.</param>
    /// <param name="unknownFieldBehavior">The behavior when an unrecognized field is encountered during filtering.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the projected paged list.
    /// </returns>
    [RequiresUnreferencedCode("ApplyFilter and ApplySort use reflection, which is incompatible with trimming.")]
    public static Task<IPagedList<TResult>> ToPagedListAsync<T, TResult>(
        this IQueryable<T> source,
        FilterParameters filter,
        SortParameters sortBy,
        Expression<Func<T, TResult>> selector,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = 1000,
        IPagedListFactory? factory = null,
        FilterUnknownFieldBehavior unknownFieldBehavior = FilterUnknownFieldBehavior.Ignore,
        CancellationToken cancellationToken = default)
    {
        return source
            .ApplyFilter(filter, unknownFieldBehavior: unknownFieldBehavior)
            .ApplySort(sortBy)
            .ToPagedListAsync(selector, parameters, countTotal, maxPageSize, factory, cancellationToken);
    }


    
}






