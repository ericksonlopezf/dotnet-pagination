// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class AutoQueryableProvider : IPaginationProvider
{
    public string Name => "AutoQueryable";

    public Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("AutoQueryable operates directly on the HTTP Request string. Benchmarking it at the IQueryable level without context is not supported.");
    }

    public Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Not supported.");
    }

    public Task<List<Product>> ExecuteFilterPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Not supported.");
    }

    public Task<List<Product>> ExecuteSortPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Not supported.");
    }

    public Task<long> ExecuteTotalCountPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Not supported.");
    }

    public Task<List<ProductDto>> ExecuteProjectionPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Not supported.");
    }
}



