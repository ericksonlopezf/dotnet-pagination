// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Benchmarks;

public class ProjectionPaginationBenchmark : PaginationBenchmarkBase
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

    private async Task<List<ProductDto>> RunProjectionAsync(string providerName)
    {
        var provider = Providers[providerName];
        return await provider.ExecuteProjectionPaginationAsync(DbContext.Products, _parameters);
    }

    [Benchmark(Baseline = true)]
    public Task<List<ProductDto>> RawSqlOffset() => RunProjectionAsync("RawSqlOffset");

    [Benchmark]
    public Task<List<ProductDto>> XPagedList() => RunProjectionAsync("XPagedList");

    [Benchmark]
    public Task<List<ProductDto>> Sieve() => RunProjectionAsync("Sieve");

    [Benchmark]
    public Task<List<ProductDto>> Ardalis() => RunProjectionAsync("Ardalis");

    [Benchmark]
    public Task<List<ProductDto>> EricksonLopez() => RunProjectionAsync("EricksonLopez");
}



