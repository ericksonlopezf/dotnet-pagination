// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Threading.Tasks;

namespace EricksonLopez.Pagination.MongoDB;

/// <summary>
/// Provides extension methods for MongoDB <see cref="IQueryable{T}"/> to stream paginated results asynchronously.
/// </summary>
public static class MongoAsyncEnumerableExtensions
{
    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{T}"/> that asynchronously streams entities for the requested page.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to paginate and stream.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> yielding items sequentially.</returns>
    public static IAsyncEnumerable<T> ToPagedAsyncEnumerable<T>(
        this IQueryable<T> source,
        PaginationParameters parameters,
        int? maxPageSize = 1000)
    {
        var effectivePageSize = maxPageSize.HasValue ? System.Math.Min(parameters.PageSize, maxPageSize.Value) : parameters.PageSize;
        return ((IAsyncCursorSource<T>)source
            .Skip((parameters.Page - 1) * effectivePageSize)
            .Take(effectivePageSize))
            .ToAsyncEnumerable();
    }

}


