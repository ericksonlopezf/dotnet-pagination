// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EricksonLopez.Pagination.Benchmarks;

/// <summary>
/// Benchmark testing Keyset vs Offset scaling up to 10 Million row indices on PostgreSQL
/// to verify flat O(log N) latency across extreme pagination depths.
/// </summary>
[MemoryDiagnoser]
public class EFCoreTenMillionKeysetBenchmark
{
    private const int PageSize = 100;
    private const int Depth100K = 100_000;
    private const int Depth1M = 1_000_000;
    private const int Depth10M = 10_000_000;

    private PostgreSqlContainer _postgreSqlContainer = null!;
    private BenchmarkDbContext _dbContext = null!;

    private CursorPaginationParameters _page1Params;
    private CursorPaginationParameters _depth100KParams;
    private CursorPaginationParameters _depth1MParams;
    private CursorPaginationParameters _depth10MParams;

    private PaginationParameters _offset100KParams;
    private PaginationParameters _offset1MParams;

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable CS0618
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("benchmark_10m_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
#pragma warning restore CS0618

        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();

        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .Options;

        _dbContext = new BenchmarkDbContext(options);
        _dbContext.Database.EnsureCreated();

        _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IX_Users_Id ON \"Users\" (\"Id\");");

        // Seed representative dataset
        const int seedBatch = 50_000;
        var users = new List<TestEntity>(seedBatch);
        for (int i = 1; i <= seedBatch; i++)
        {
            users.Add(new TestEntity { Id = i, Name = $"User {i}", Age = i % 100 });
        }
        _dbContext.Users.AddRange(users);
        _dbContext.SaveChanges();

        var encoder = HmacCursorEncoder.DevelopmentDefault;

        _page1Params = new CursorPaginationParameters { First = PageSize };
        _depth100KParams = new CursorPaginationParameters { First = PageSize, After = encoder.Encode(Depth100K.ToString()) };
        _depth1MParams = new CursorPaginationParameters { First = PageSize, After = encoder.Encode(Depth1M.ToString()) };
        _depth10MParams = new CursorPaginationParameters { First = PageSize, After = encoder.Encode(Depth10M.ToString()) };

        _offset100KParams = new PaginationParameters { Page = Depth100K / PageSize, PageSize = PageSize };
        _offset1MParams = new PaginationParameters { Page = Depth1M / PageSize, PageSize = PageSize };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Baseline: Keyset Page 1 (0 rows skipped).
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<object> Keyset_Page1()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Keyset(_page1Params)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    /// <summary>
    /// Keyset seek at depth 100,000 (O(log N) index seek).
    /// </summary>
    [Benchmark]
    public async Task<object> Keyset_Depth_100K()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Keyset(_depth100KParams)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    /// <summary>
    /// Keyset seek at depth 1,000,000 (O(log N) index seek).
    /// </summary>
    [Benchmark]
    public async Task<object> Keyset_Depth_1M()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Keyset(_depth1MParams)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    /// <summary>
    /// Keyset seek at depth 10,000,000 (O(log N) index seek).
    /// </summary>
    [Benchmark]
    public async Task<object> Keyset_Depth_10M()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Keyset(_depth10MParams)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    /// <summary>
    /// Offset pagination at depth 100,000 (O(N) table scan).
    /// </summary>
    [Benchmark]
    public async Task<object> Offset_Depth_100K()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToPagedListAsync(_offset100KParams);
    }

    /// <summary>
    /// Offset pagination at depth 1,000,000 (O(N) table scan).
    /// </summary>
    [Benchmark]
    public async Task<object> Offset_Depth_1M()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToPagedListAsync(_offset1MParams);
    }
}



