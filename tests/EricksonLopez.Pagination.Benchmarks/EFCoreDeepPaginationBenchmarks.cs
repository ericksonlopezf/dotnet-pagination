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
using Testcontainers.PostgreSql;

namespace EricksonLopez.Pagination.Benchmarks;

/// <summary>
/// The "Killer Demo" benchmark demonstrating O(N) degradation of OFFSET vs O(log N) stability of Keyset.
/// Tests deep pagination up to page 10,000 on a 1M row dataset.
/// </summary>
[MemoryDiagnoser]
// [ShortRunJob] // Uncomment for quick local testing if needed, but we want real results
public class EFCoreDeepPaginationBenchmarks
{
    private const int DatasetSize = 1_000_000;
    private const int PageSize = 100;

    [Params(1, 100, 10000)]
    public int TargetPage { get; set; }

    private PostgreSqlContainer _postgreSqlContainer = null!;
    private BenchmarkDbContext _dbContext = null!;
    private PaginationParameters _offsetParams;
    private CursorPaginationParameters _keysetParams;

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable CS0618
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("benchmark_db")
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

        // Ensure index exists for keyset sorting
        _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IX_Users_Id ON \"Users\" (\"Id\");");

        // Batch insert 1M rows to avoid memory issues during setup
        const int batchSize = 100_000;
        for (int batch = 0; batch < DatasetSize / batchSize; batch++)
        {
            var users = new List<TestEntity>(batchSize);
            for (int i = 1; i <= batchSize; i++)
            {
                int id = (batch * batchSize) + i;
                users.Add(new TestEntity { Id = id, Name = $"User {id}", Age = id % 100 });
            }
            _dbContext.Users.AddRange(users);
            _dbContext.SaveChanges();
            _dbContext.ChangeTracker.Clear();
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _offsetParams = new PaginationParameters { Page = TargetPage, PageSize = PageSize };

        // For Keyset, we need a cursor that points to the item exactly *before* our target page.
        // If TargetPage is 1, After is null.
        // If TargetPage is 100, we skip 9900 items, so the last item of the previous page has Id = 9900.
        string? cursor = null;
        if (TargetPage > 1)
        {
            int offset = (TargetPage - 1) * PageSize;
            cursor = HmacCursorEncoder.DevelopmentDefault.Encode(offset.ToString());
        }
        _keysetParams = new CursorPaginationParameters { First = PageSize, After = cursor };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task<object> Offset_Degradation()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToPagedListAsync(_offsetParams);
    }

    [Benchmark]
    public async Task<object> Keyset_Stability()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Keyset(_keysetParams)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }
}



