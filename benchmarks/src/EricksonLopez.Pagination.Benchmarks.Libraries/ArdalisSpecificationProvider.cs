// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class ArdalisSpecificationProvider : IPaginationProvider
{
    public string Name => "Ardalis.Specification";

    public async Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var spec = new OffsetPaginationSpec(parameters.PageNumber, parameters.PageSize);
        var evaluator = SpecificationEvaluator.Default;
        var resultQuery = evaluator.GetQuery(query, spec);
        return await resultQuery.ToListAsync();
    }

    public Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("Ardalis Specification doesn't provide native Keyset Pagination utilities, requires custom spec.");
    }

    public async Task<List<Product>> ExecuteFilterPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var spec = new FilterPaginationSpec(parameters);
        var evaluator = SpecificationEvaluator.Default;
        var resultQuery = evaluator.GetQuery(query, spec);
        return await resultQuery.ToListAsync();
    }

    public async Task<List<Product>> ExecuteSortPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var spec = new SortPaginationSpec(parameters);
        var evaluator = SpecificationEvaluator.Default;
        var resultQuery = evaluator.GetQuery(query, spec);
        return await resultQuery.ToListAsync();
    }

    public async Task<long> ExecuteTotalCountPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var spec = new OffsetPaginationSpec(parameters.PageNumber, parameters.PageSize);
        var evaluator = SpecificationEvaluator.Default;
        
        // Ardalis evaluates count by ignoring pagination in the query evaluator, but we just use EF Core CountAsync on the base query before skip/take
        var count = await query.CountAsync();
        var resultQuery = evaluator.GetQuery(query, spec);
        await resultQuery.ToListAsync();
        return count;
    }

    public async Task<List<ProductDto>> ExecuteProjectionPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        var spec = new ProjectionPaginationSpec(parameters.PageNumber, parameters.PageSize);
        var evaluator = SpecificationEvaluator.Default;
        var resultQuery = evaluator.GetQuery(query, spec);
        return await resultQuery.ToListAsync();
    }

    private sealed class OffsetPaginationSpec : Specification<Product>
    {
        public OffsetPaginationSpec(int pageNumber, int pageSize)
        {
            Query.OrderBy(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
    }

    private sealed class FilterPaginationSpec : Specification<Product>
    {
        public FilterPaginationSpec(PaginationParameters parameters)
        {
            if (!string.IsNullOrEmpty(parameters.FilterCategory))
            {
                Query.Where(x => x.Category == parameters.FilterCategory);
            }
            if (parameters.MinPrice.HasValue)
            {
                Query.Where(x => x.Price >= parameters.MinPrice.Value);
            }
            Query.OrderBy(x => x.Id).Skip((parameters.PageNumber - 1) * parameters.PageSize).Take(parameters.PageSize);
        }
    }

    private sealed class SortPaginationSpec : Specification<Product>
    {
        public SortPaginationSpec(PaginationParameters parameters)
        {
            Query.OrderByDescending(x => x.Price).ThenBy(x => x.CreatedAt)
                 .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                 .Take(parameters.PageSize);
        }
    }

    private sealed class ProjectionPaginationSpec : Specification<Product, ProductDto>
    {
        public ProjectionPaginationSpec(int pageNumber, int pageSize)
        {
            Query.OrderBy(x => x.Id)
                 .Skip((pageNumber - 1) * pageSize)
                 .Take(pageSize);
            
            Query.Select(x => new ProductDto 
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price
            });
        }
    }
}



