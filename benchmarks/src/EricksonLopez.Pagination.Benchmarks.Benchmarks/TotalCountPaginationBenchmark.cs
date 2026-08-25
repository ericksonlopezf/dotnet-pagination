// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Benchmarks;

public class TotalCountPaginationBenchmark : PaginationBenchmarkBase
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

    private async Task<long> RunTotalCountAsync(string providerName)
    {
        var provider = Providers[providerName];
        return await provider.ExecuteTotalCountPaginationAsync(DbContext.Products, _parameters);
    }

    [Benchmark(Baseline = true)]
    public Task<long> RawSqlOffset() => RunTotalCountAsync("RawSqlOffset");

    [Benchmark]
    public Task<long> XPagedList() => RunTotalCountAsync("XPagedList");

    [Benchmark]
    public Task<long> Sieve() => RunTotalCountAsync("Sieve");

    [Benchmark]
    public Task<long> Ardalis() => RunTotalCountAsync("Ardalis");

    [Benchmark]
    public Task<long> EricksonLopez() => RunTotalCountAsync("EricksonLopez");
}



