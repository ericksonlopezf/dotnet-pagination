// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Benchmarks;

public class FilterPaginationBenchmark : PaginationBenchmarkBase
{
    private PaginationParameters _parameters = null!;

    [IterationSetup]
    public void IterationSetup()
    {
        _parameters = new PaginationParameters
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            FilterCategory = "Electronics", // Bogus department might vary, but for SQL it's the same structure
            MinPrice = 100m
        };
    }

    private async Task<List<Product>> RunFilterAsync(string providerName)
    {
        var provider = Providers[providerName];
        return await provider.ExecuteFilterPaginationAsync(DbContext.Products, _parameters);
    }

    [Benchmark(Baseline = true)]
    public Task<List<Product>> RawSqlOffset() => RunFilterAsync("RawSqlOffset");

    [Benchmark]
    public Task<List<Product>> Sieve() => RunFilterAsync("Sieve");

    [Benchmark]
    public Task<List<Product>> Ardalis() => RunFilterAsync("Ardalis");

    [Benchmark]
    public Task<List<Product>> EricksonLopez() => RunFilterAsync("EricksonLopez");
}



