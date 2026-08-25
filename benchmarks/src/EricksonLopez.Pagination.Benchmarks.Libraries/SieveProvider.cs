// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class SieveProvider : IPaginationProvider
{
    public string Name => "Sieve";
    private readonly SieveProcessor _processor;

    public SieveProvider()
    {
        var options = Options.Create(new SieveOptions { ThrowExceptions = true });
        _processor = new SieveProcessor(options);
    }

    public async Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var sieveModel = new SieveModel
        {
            Page = parameters.PageNumber,
            PageSize = parameters.PageSize,
            Sorts = "Id"
        };
        
        var result = _processor.Apply(sieveModel, query);
        return await result.ToListAsync();
    }

    public Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Sieve does not support keyset pagination.");
    }

    public async Task<List<Product>> ExecuteFilterPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var filters = new List<string>();
        if (!string.IsNullOrEmpty(parameters.FilterCategory))
        {
            filters.Add($"Category=={parameters.FilterCategory}");
        }
        if (parameters.MinPrice.HasValue)
        {
            filters.Add($"Price>={parameters.MinPrice.Value}");
        }

        var sieveModel = new SieveModel
        {
            Page = parameters.PageNumber,
            PageSize = parameters.PageSize,
            Sorts = "Id",
            Filters = string.Join(",", filters)
        };

        var result = _processor.Apply(sieveModel, query);
        return await result.ToListAsync();
    }

    public async Task<List<Product>> ExecuteSortPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var sieveModel = new SieveModel
        {
            Page = parameters.PageNumber,
            PageSize = parameters.PageSize,
            Sorts = "-Price,CreatedAt"
        };

        var result = _processor.Apply(sieveModel, query);
        return await result.ToListAsync();
    }

    public async Task<long> ExecuteTotalCountPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var sieveModel = new SieveModel
        {
            Page = parameters.PageNumber,
            PageSize = parameters.PageSize,
            Sorts = "Id"
        };

        var count = await query.CountAsync();
        var result = _processor.Apply(sieveModel, query);
        await result.ToListAsync();
        
        return count;
    }

    public async Task<List<ProductDto>> ExecuteProjectionPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var sieveModel = new SieveModel
        {
            Page = parameters.PageNumber,
            PageSize = parameters.PageSize,
            Sorts = "Id"
        };

        var result = _processor.Apply(sieveModel, query);
        return await result.Select(x => new ProductDto 
        { 
            Id = x.Id, 
            Name = x.Name, 
            Price = x.Price 
        }).ToListAsync();
    }
}





