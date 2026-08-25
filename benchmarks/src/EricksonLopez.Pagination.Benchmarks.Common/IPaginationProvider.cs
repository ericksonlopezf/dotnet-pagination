// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Common;

public interface IPaginationProvider
{
    string Name { get; }

    /// <summary>
    /// Executes a standard offset/limit pagination query.
    /// </summary>
    Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters);

    /// <summary>
    /// Executes a keyset (cursor) pagination query.
    /// Throws NotSupportedException if the library doesn't support keyset pagination natively.
    /// </summary>
    Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters);

    /// <summary>
    /// Executes a pagination query with simple single-column filtering.
    /// </summary>
    Task<List<Product>> ExecuteFilterPaginationAsync(IQueryable<Product> query, PaginationParameters parameters);

    /// <summary>
    /// Executes a pagination query with dynamic sorting.
    /// </summary>
    Task<List<Product>> ExecuteSortPaginationAsync(IQueryable<Product> query, PaginationParameters parameters);

    /// <summary>
    /// Executes a pagination query and returns the total count of the query.
    /// Returns the long count (not the items themselves, or just fetches both and returns count).
    /// </summary>
    Task<long> ExecuteTotalCountPaginationAsync(IQueryable<Product> query, PaginationParameters parameters);

    /// <summary>
    /// Executes a pagination query projecting to a DTO.
    /// </summary>
    Task<List<ProductDto>> ExecuteProjectionPaginationAsync(IQueryable<Product> query, PaginationParameters parameters);
}



