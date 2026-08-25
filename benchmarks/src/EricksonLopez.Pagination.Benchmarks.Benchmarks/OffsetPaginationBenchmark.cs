// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Benchmarks;

public class OffsetPaginationBenchmark : PaginationBenchmarkBase
{
    private PaginationParameters _parameters = null!;

    [IterationSetup]
    public void IterationSetup()
    {
        _parameters = new PaginationParameters
        {
            PageNumber = PageNumber,
            PageSize = PageSize
        };
    }

    private async Task<List<Product>> RunOffsetAsync(string providerName)
    {
        var provider = Providers[providerName];
        return await provider.ExecuteOffsetPaginationAsync(DbContext.Products, _parameters);
    }

    [Benchmark(Baseline = true)]
    public Task<List<Product>> RawSqlOffset() => RunOffsetAsync("RawSqlOffset");

    [Benchmark]
    public Task<List<Product>> XPagedList() => RunOffsetAsync("XPagedList");

    [Benchmark]
    public Task<List<Product>> Sieve() => RunOffsetAsync("Sieve");

    [Benchmark]
    public Task<List<Product>> Ardalis() => RunOffsetAsync("Ardalis");

    [Benchmark]
    public Task<List<Product>> EricksonLopez() => RunOffsetAsync("EricksonLopez");
}



