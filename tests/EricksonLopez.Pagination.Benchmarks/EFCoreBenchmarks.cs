// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;
using Testcontainers.PostgreSql;

namespace EricksonLopez.Pagination.Benchmarks;

/// <summary>
/// Benchmarks comparing EricksonLopez.Pagination keyset and offset strategies against
/// MR.EntityFrameworkCore.KeysetPagination and X.PagedList.
/// </summary>
/// <remarks>
/// Dataset: <see cref="DatasetSize"/> rows inserted into PostgreSQL via Testcontainers.
/// Access point: page 50 of <see cref="PageSize"/> rows = skip <see cref="CursorOffset"/> rows (mid-range, not deep-page).
/// Both WarmStart (compiled query plans cached) and ColdStart (fresh DbContext, service provider not shared)
/// scenarios are measured to capture both steady-state and startup-time costs.
/// </remarks>
[MemoryDiagnoser]
public class EFCoreBenchmarks
{
    /// <summary>The number of rows seeded into the benchmark PostgreSQL instance.</summary>
    /// <remarks>
    /// Previously set to 10,000 — but the README and all published benchmark comparisons claim 100,000 rows.
    /// F-012: Corrected to 100,000 to match the documented dataset and restore benchmark credibility.
    /// </remarks>
    private const int DatasetSize = 100_000;

    /// <summary>The page size used for all benchmarks.</summary>
    private const int PageSize = 100;

    /// <summary>The number of rows skipped to access page 50 (mid-range, not deep-page).</summary>
    private const int CursorOffset = (50 - 1) * PageSize; // = 4900

    private PostgreSqlContainer _postgreSqlContainer = null!;
    private BenchmarkDbContext _dbContext = null!;
    private PaginationParameters _offsetParams;

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("benchmark_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
#pragma warning restore CS0618 // Type or member is obsolete

        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();

        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .Options;

        _dbContext = new BenchmarkDbContext(options);
        _dbContext.Database.EnsureCreated();

        var users = new List<TestEntity>(DatasetSize);
        for (int i = 1; i <= DatasetSize; i++)
        {
            users.Add(new TestEntity { Id = i, Name = $"User {i}", Age = i % 100 });
        }
        _dbContext.Users.AddRange(users);
        _dbContext.SaveChanges();

        _offsetParams = new PaginationParameters { Page = 50, PageSize = PageSize };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task<object> WarmStart_Offset()
    {
        return await _dbContext.Users
            .OrderBy(u => u.Id)
            .ToPagedListAsync(_offsetParams);
    }

    [Benchmark]
    public async Task<object> WarmStart_Keyset()
    {
        var cursor = HmacCursorEncoder.DevelopmentDefault.Encode(CursorOffset.ToString());
        var pagedParams = new CursorPaginationParameters { First = PageSize, After = cursor };
        return await _dbContext.Users
            .Keyset(pagedParams)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    [Benchmark]
    public async Task<object> WarmStart_MRKeyset()
    {
        return await _dbContext.Users
            .KeysetPaginateQuery(
                b => b.Ascending(x => x.Id),
                KeysetPaginationDirection.Forward,
                reference: new { Id = CursorOffset })
            .Take(PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Comparable X.PagedList benchmark using the same offset (page 50, pageSize 100, dataset 100,000).
    /// F-013: Added to make the README comparative numbers reproducible from the repository.
    /// </summary>
    [Benchmark]
    public async Task<object> WarmStart_XPagedList()
    {
        return await X.PagedList.EF.PagedListExtensions.ToPagedListAsync(
            _dbContext.Users.OrderBy(u => u.Id), 50, PageSize);
    }

    [Benchmark]
    public async Task<object> ColdStart_Offset()
    {
        // Use a new DbContext with a unique service provider to force model building and query compilation
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .EnableServiceProviderCaching(false)
            .Options;

        using var ctx = new BenchmarkDbContext(options);
        return await ctx.Users
            .OrderBy(u => u.Id)
            .ToPagedListAsync(_offsetParams);
    }

    [Benchmark]
    public async Task<object> ColdStart_Keyset()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .EnableServiceProviderCaching(false)
            .Options;

        using var ctx = new BenchmarkDbContext(options);
        var cursor = HmacCursorEncoder.DevelopmentDefault.Encode(CursorOffset.ToString());
        var pagedParams = new CursorPaginationParameters { First = PageSize, After = cursor };
        return await ctx.Users
            .Keyset(pagedParams)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    [Benchmark]
    public async Task<object> ColdStart_MRKeyset()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .EnableServiceProviderCaching(false)
            .Options;

        using var ctx = new BenchmarkDbContext(options);
        return await ctx.Users
            .KeysetPaginateQuery(
                b => b.Ascending(x => x.Id),
                KeysetPaginationDirection.Forward,
                reference: new { Id = CursorOffset })
            .Take(PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Comparable X.PagedList cold-start benchmark (fresh DbContext, forced service-provider rebuild).
    /// F-013: Added to make the README comparative numbers reproducible from the repository.
    /// </summary>
    [Benchmark]
    public async Task<object> ColdStart_XPagedList()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .EnableServiceProviderCaching(false)
            .Options;

        using var ctx = new BenchmarkDbContext(options);
        return await X.PagedList.EF.PagedListExtensions.ToPagedListAsync(
            ctx.Users.OrderBy(u => u.Id), 50, PageSize);
    }
}

public class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }
    public DbSet<TestEntity> Users => Set<TestEntity>();
}




