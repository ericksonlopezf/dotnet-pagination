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

public class RawSqlOffsetProvider : IPaginationProvider
{
    public string Name => "Raw SQL (EF Core Offset)";

    public async Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        return await query
            .OrderBy(x => x.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();
    }

    public Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Offset provider does not support keyset pagination.");
    }

    public async Task<List<Product>> ExecuteFilterPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        if (!string.IsNullOrEmpty(parameters.FilterCategory))
        {
            query = query.Where(x => x.Category == parameters.FilterCategory);
        }

        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(x => x.Price >= parameters.MinPrice.Value);
        }

        return await ExecuteOffsetPaginationAsync(query, parameters);
    }

    public async Task<List<Product>> ExecuteSortPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        return await query
            .OrderByDescending(x => x.Price)
            .ThenBy(x => x.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();
    }

    public async Task<long> ExecuteTotalCountPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var count = await query.CountAsync();
        await query
            .OrderBy(x => x.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();
        
        return count;
    }

    public async Task<List<ProductDto>> ExecuteProjectionPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        return await query
            .OrderBy(x => x.Id)
            .Select(x => new ProductDto 
            { 
                Id = x.Id, 
                Name = x.Name, 
                Price = x.Price 
            })
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();
    }
}



