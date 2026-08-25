// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;

namespace EricksonLopez.Pagination.Benchmarks.Benchmarks;

public class KeysetPaginationBenchmark : PaginationBenchmarkBase
{
    private PaginationParameters _parameters = null!;

    [GlobalSetup]
    public void KeysetSetup()
    {
        base.GlobalSetup();
        
        // Simulating jumping to a deep page by pre-fetching the reference values
        var referenceProduct = DbContext.Products
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((PageNumber - 1) * PageSize)
            .FirstOrDefault();
            
        _parameters = new PaginationParameters
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            ReferenceId = referenceProduct?.Id,
            ReferenceDate = referenceProduct?.CreatedAt
        };
    }

    private async Task<List<Product>> RunKeysetAsync(string providerName)
    {
        var provider = Providers[providerName];
        return await provider.ExecuteKeysetPaginationAsync(DbContext.Products, _parameters);
    }

    [Benchmark(Baseline = true)]
    public Task<List<Product>> RawSqlKeyset() => RunKeysetAsync("RawSqlKeyset");

    [Benchmark]
    public Task<List<Product>> MrKeyset() => RunKeysetAsync("MrKeyset");

    [Benchmark]
    public Task<List<Product>> EricksonLopez() => RunKeysetAsync("EricksonLopez");
}



