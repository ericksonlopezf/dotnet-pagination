// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class RawSqlKeysetProvider : IPaginationProvider
{
    public string Name => "Raw SQL (EF Core Keyset)";

    public Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Keyset provider does not support offset pagination.");
    }

    public async Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        query = query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id);
        
        if (parameters.ReferenceDate.HasValue && parameters.ReferenceId.HasValue)
        {
            var date = parameters.ReferenceDate.Value;
            var id = parameters.ReferenceId.Value;
            
            query = query.Where(x => x.CreatedAt > date || (x.CreatedAt == date && x.Id > id));
        }

        return await query.Take(parameters.PageSize).ToListAsync();
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



