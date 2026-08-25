// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class EricksonLopezProvider : IPaginationProvider
{
    public string Name => "EricksonLopez";

    public async Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, Common.PaginationParameters parameters)
    {
        // Require explicit order by before offset pagination
        var pagedList = await query.OrderBy(x => x.Id).ToPagedListAsync(
            new Abstractions.PaginationParameters
            {
                Page = parameters.PageNumber,
                PageSize = parameters.PageSize
            },
            countTotal: false // Just materializing the page items, no total count for offset benchmark to match RawSql
        );
        return pagedList.ToList();
    }

    public async Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, Common.PaginationParameters parameters)
    {
        string? cursor = null;
        if (parameters.ReferenceDate.HasValue && parameters.ReferenceId.HasValue)
        {
            var rawCursor = $"{parameters.ReferenceDate.Value:O}|{parameters.ReferenceId.Value}";
            cursor = HmacCursorEncoder.DevelopmentDefault.Encode(rawCursor);
        }

        var cursorParams = new CursorPaginationParameters
        {
            First = parameters.PageSize,
            After = cursor
        };
        
        var pagedList = await query
            .Keyset(cursorParams)
            .Ascending(x => x.CreatedAt)
            .Ascending(x => x.Id)
            .ToCursorPagedListAsync();
        return pagedList.ToList();
    }

    public async Task<List<Product>> ExecuteFilterPaginationAsync(IQueryable<Product> query, Common.PaginationParameters parameters)
    {
        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(x => x.Price > parameters.MinPrice.Value);
        }
        var pagedList = await query.OrderBy(x => x.Id).ToPagedListAsync(
            new Abstractions.PaginationParameters
            {
                Page = parameters.PageNumber,
                PageSize = parameters.PageSize
            },
            countTotal: false
        );
        return pagedList.ToList();
    }

    public async Task<List<Product>> ExecuteSortPaginationAsync(IQueryable<Product> query, Common.PaginationParameters parameters)
    {
        var pagedList = await query
            .OrderByDescending(x => x.Price)
            .ThenBy(x => x.CreatedAt)
            .ToPagedListAsync(
            new Abstractions.PaginationParameters
            {
                Page = parameters.PageNumber,
                PageSize = parameters.PageSize
            },
            countTotal: false
        );
        return pagedList.ToList();
    }

    public async Task<long> ExecuteTotalCountPaginationAsync(IQueryable<Product> query, Common.PaginationParameters parameters)
    {
        var pagedList = await query
            .OrderBy(x => x.Id)
            .ToPagedListAsync(
            new Abstractions.PaginationParameters
            {
                Page = parameters.PageNumber,
                PageSize = parameters.PageSize
            },
            countTotal: true
        );
        return pagedList.TotalCount ?? 0L;
    }

    public async Task<List<ProductDto>> ExecuteProjectionPaginationAsync(IQueryable<Product> query, Common.PaginationParameters parameters)
    {
        var pagedList = await query
            .OrderBy(x => x.Id)
            .Select(x => new ProductDto 
            { 
                Id = x.Id, 
                Name = x.Name, 
                Price = x.Price 
            })
            .ToPagedListAsync(
            new Abstractions.PaginationParameters
            {
                Page = parameters.PageNumber,
                PageSize = parameters.PageSize
            },
            countTotal: false
        );
        return pagedList.ToList();
    }
}



