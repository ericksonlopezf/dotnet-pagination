// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using EricksonLopez.Pagination.Benchmarks.Libraries;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public abstract class PaginationBenchmarkBase
{
    protected AppDbContext DbContext { get; private set; } = null!;
    
    // We instantiate providers here
    protected readonly Dictionary<string, IPaginationProvider> Providers = new();

    // Common params
    [Params(10, 100)]
    public int PageSize { get; set; }
    
    [Params(1, 100, 10000)]
    public int PageNumber { get; set; }

    [Params(DatabaseEngine.PostgreSQL, DatabaseEngine.SQLServer, DatabaseEngine.MySQL, DatabaseEngine.Oracle, DatabaseEngine.SQLite)]
    public DatabaseEngine Engine { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var options = DbEngineFactory.CreateOptions(Engine);
            
        DbContext = new AppDbContext(options);

        Providers.Add("EricksonLopez", new EricksonLopezProvider());
        Providers.Add("RawSqlOffset", new RawSqlOffsetProvider());
        Providers.Add("RawSqlKeyset", new RawSqlKeysetProvider());
        Providers.Add("XPagedList", new XPagedListProvider());
        Providers.Add("Sieve", new SieveProvider());
        Providers.Add("MrKeyset", new MrEntityFrameworkCoreKeysetProvider());
        Providers.Add("Ardalis", new ArdalisSpecificationProvider());
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        DbContext.Dispose();
    }
}
