// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Benchmarks;

public class SortPaginationBenchmark : PaginationBenchmarkBase
{
    private PaginationParameters _parameters = null!;

    [IterationSetup]
    public void IterationSetup()
    {
        _parameters = new PaginationParameters
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortBy = "Price",
            SortDescending = true
        };
    }

    private async Task<List<Product>> RunSortAsync(string providerName)
    {
        var provider = Providers[providerName];
        return await provider.ExecuteSortPaginationAsync(DbContext.Products, _parameters);
    }

    [Benchmark(Baseline = true)]
    public Task<List<Product>> RawSqlOffset() => RunSortAsync("RawSqlOffset");

    [Benchmark]
    public Task<List<Product>> XPagedList() => RunSortAsync("XPagedList");

    [Benchmark]
    public Task<List<Product>> Sieve() => RunSortAsync("Sieve");

    [Benchmark]
    public Task<List<Product>> Ardalis() => RunSortAsync("Ardalis");

    [Benchmark]
    public Task<List<Product>> EricksonLopez() => RunSortAsync("EricksonLopez");
}



