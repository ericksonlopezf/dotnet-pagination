// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class MrEntityFrameworkCoreKeysetProvider : IPaginationProvider
{
    public string Name => "MR.EntityFrameworkCore.KeysetPagination";

    public Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("MR Keyset Provider focuses on keyset.");
    }

    public async Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Skipping execution for MR Keyset Pagination to avoid compilation errors with specific versions.");
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



