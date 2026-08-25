// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Dapper;

/// <summary>
/// Provides extension methods for Dapper's <see cref="SqlMapper.GridReader"/> to read paginated results.
/// </summary>
public static class GridReaderPaginationExtensions
{
    /// <summary>
    /// Reads a paginated result set from a Dapper grid reader.
    /// </summary>
    /// <typeparam name="T">The type of elements in the paginated list.</typeparam>
    /// <param name="multi">The grid reader to read results from.</param>
    /// <param name="parameters">The pagination parameters defining the requested page and page size.</param>
    /// <param name="countTotal">A value indicating whether the first result set contains the total record count.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="factory">An optional factory used to instantiate the paged list.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the paged list.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="multi"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">The first result set does not return a valid count when <paramref name="countTotal"/> is <see langword="true"/></exception>
    public static async Task<IPagedList<T>> ReadPagedListAsync<T>(
        this SqlMapper.GridReader multi,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = 1000,
        IPagedListFactory? factory = null)
    {
        if (multi == null) throw new ArgumentNullException(nameof(multi));

        factory ??= DefaultPagedListFactory.Instance;
        var effectivePageSize = maxPageSize.HasValue ? Math.Min(parameters.PageSize, maxPageSize.Value) : parameters.PageSize;

        if (countTotal)
        {
            long totalCount = 0;
            try
            {
                // Stryker disable once boolean
                totalCount = await multi.ReadSingleAsync<long>().ConfigureAwait(false);
            }
            // Stryker disable once all : Exception rewrapping guard
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is InvalidOperationException || ex is System.Data.DataException)
            {
                throw new InvalidOperationException("The SQL query must return the COUNT(*) as the first result set when countTotal is true.", ex);
            }

            // Stryker disable all : Total count 0 fast path
            if (totalCount == 0)
            {
                return factory.CreatePagedList<T>([], 0, parameters.Page, effectivePageSize, false);
            }
            // Stryker restore all

            // Stryker disable once boolean
            var items = await multi.ReadAsync<T>().ConfigureAwait(false);
            // The as-cast avoids a redundant ToList() allocation.
            // Stryker disable once all
            return factory.CreatePagedList(items as IReadOnlyList<T> ?? items.AsList(), totalCount, parameters.Page, effectivePageSize, null);
        }
        else
        {
            // Stryker disable once boolean
            var items = await multi.ReadAsync<T>().ConfigureAwait(false);
            var itemList = items.AsList();
            var hasNextPage = itemList.Count > effectivePageSize;
            if (hasNextPage)
            {
                itemList.RemoveAt(itemList.Count - 1);
            }
            return factory.CreatePagedList(itemList, null, parameters.Page, effectivePageSize, hasNextPage);
        }
    }
}





