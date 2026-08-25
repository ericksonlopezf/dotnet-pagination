// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Libraries;

public class ODataProvider : IPaginationProvider
{
    public string Name => "OData";

    public Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("OData requires full ASP.NET Core MVC/WebAPI stack (HttpRequest, EdmModel) to parse $top and $skip accurately. Testing it directly on IQueryable without the HTTP pipeline is inaccurate and misrepresents its overhead.");
    }

    public Task<List<Product>> ExecuteKeysetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)
    {
        throw new NotSupportedException("OData relies on $skip, keyset is not native without custom extensions.");
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



