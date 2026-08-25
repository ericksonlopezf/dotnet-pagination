// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using X.PagedList;
using X.PagedList.EF;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class XPagedListProvider : IPaginationProvider
{
    public string Name => "X.PagedList";

    public async Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var pagedList = await query.OrderBy(x => x.Id).ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        return pagedList.ToList();
    }

    public Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("X.PagedList does not support keyset pagination.");
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

        var pagedList = await query.OrderBy(x => x.Id).ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        return pagedList.ToList();
    }

    public async Task<List<Product>> ExecuteSortPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var pagedList = await query
            .OrderByDescending(x => x.Price)
            .ThenBy(x => x.CreatedAt)
            .ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        return pagedList.ToList();
    }

    public async Task<long> ExecuteTotalCountPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var pagedList = await query
            .OrderBy(x => x.Id)
            .ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        return pagedList.TotalItemCount;
    }

    public async Task<List<ProductDto>> ExecuteProjectionPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var pagedList = await query
            .OrderBy(x => x.Id)
            .Select(x => new ProductDto 
            { 
                Id = x.Id, 
                Name = x.Name, 
                Price = x.Price 
            })
            .ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        return pagedList.ToList();
    }
}



