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
/// Head-to-Head comparison against MR.EntityFrameworkCore.KeysetPagination
/// specifically focusing on N-column (complex) keyset queries.
/// </summary>
[MemoryDiagnoser]
public class MRKeysetComparisonBenchmark
{
    private const int DatasetSize = 100_000;
    private const int PageSize = 100;
    
    // Page 500 = 49900 rows skipped.
    private const int OffsetIndex = 49900; 

    private PostgreSqlContainer _postgreSqlContainer = null!;
    private BenchmarkDbContext _dbContext = null!;
    
    private CursorPaginationParameters _elParams;
    private int _mrReferenceAge;
    private int _mrReferenceId;

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

        // Create composite index for the 2-column keyset
        _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IX_Users_Age_Id ON \"Users\" (\"Age\", \"Id\");");

        var users = new List<TestEntity>(DatasetSize);
        for (int i = 1; i <= DatasetSize; i++)
        {
            users.Add(new TestEntity { Id = i, Name = $"User {i}", Age = i % 100 });
        }
        _dbContext.Users.AddRange(users);
        _dbContext.SaveChanges();

        // We need the exact reference point for the cursor. 
        // We are ordering by Age ASC, Id ASC.
        var referenceUser = _dbContext.Users
            .OrderBy(u => u.Age)
            .ThenBy(u => u.Id)
            .Skip(OffsetIndex - 1)
            .Take(1)
            .Single();

        // EricksonLopez requires an opaque cursor string for standard parsing,
        // but we can generate it easily:
        var rawCursor = $"{referenceUser.Age}|{referenceUser.Id}";
        _elParams = new CursorPaginationParameters 
        { 
            First = PageSize, 
            After = HmacCursorEncoder.DevelopmentDefault.Encode(rawCursor)
        };

        // MR requires an anonymous object reference, we'll instantiate it on each run
        // to mimic a realistic web request.
        _mrReferenceAge = referenceUser.Age;
        _mrReferenceId = referenceUser.Id;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task<object> EricksonLopez_Keyset_2Col()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Keyset(_elParams)
            .Ascending(u => u.Age)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    [Benchmark]
    public async Task<object> MR_Keyset_2Col()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .KeysetPaginateQuery(
                b => b.Ascending(x => x.Age).Ascending(x => x.Id),
                KeysetPaginationDirection.Forward,
                reference: new { Age = _mrReferenceAge, Id = _mrReferenceId })
            .Take(PageSize)
            .ToListAsync();
    }
}



