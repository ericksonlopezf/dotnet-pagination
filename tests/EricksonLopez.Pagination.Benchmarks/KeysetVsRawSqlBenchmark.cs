// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EricksonLopez.Pagination.Benchmarks;

/// <summary>
/// Benchmark comparing EricksonLopez Keyset pagination against a Raw SQL ADO.NET baseline
/// to isolate SQL execution latency from LINQ translation and cursor encoding overhead.
/// </summary>
[MemoryDiagnoser]
public class KeysetVsRawSqlBenchmark
{
    private const int DatasetSize = 100_000;
    private const int PageSize = 100;
    private const int OffsetIndex = 49_900;

    private PostgreSqlContainer _postgreSqlContainer = null!;
    private BenchmarkDbContext _dbContext = null!;
    private NpgsqlConnection _rawConnection = null!;
    
    private CursorPaginationParameters _keysetParams;
    private int _referenceId;
    private string _encodedCursor = string.Empty;
    private readonly ICursorEncoder _encoder = HmacCursorEncoder.DevelopmentDefault;

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

        _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IX_Users_Id ON \"Users\" (\"Id\");");

        var users = new List<TestEntity>(DatasetSize);
        for (int i = 1; i <= DatasetSize; i++)
        {
            users.Add(new TestEntity { Id = i, Name = $"User {i}", Age = i % 100 });
        }
        _dbContext.Users.AddRange(users);
        _dbContext.SaveChanges();

        _referenceId = OffsetIndex;
        _encodedCursor = _encoder.Encode(_referenceId.ToString()) ?? string.Empty;
        _keysetParams = new CursorPaginationParameters
        {
            First = PageSize,
            After = _encodedCursor
        };

        _rawConnection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        _rawConnection.Open();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _rawConnection.Dispose();
        _dbContext.Dispose();
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Baseline: Raw SQL execution using NpgsqlCommand executing index seek WHERE Id > @afterId.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> RawSql_Keyset()
    {
        using var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\", \"Age\" FROM \"Users\" WHERE \"Id\" > @afterId ORDER BY \"Id\" LIMIT @pageSize", _rawConnection);
        cmd.Parameters.AddWithValue("afterId", _referenceId);
        cmd.Parameters.AddWithValue("pageSize", PageSize);

        using var reader = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Raw SQL offset scan: WHERE / ORDER BY Id OFFSET @skip LIMIT @pageSize.
    /// </summary>
    [Benchmark]
    public async Task<int> RawSql_Offset()
    {
        using var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\", \"Age\" FROM \"Users\" ORDER BY \"Id\" OFFSET @skip LIMIT @pageSize", _rawConnection);
        cmd.Parameters.AddWithValue("skip", OffsetIndex);
        cmd.Parameters.AddWithValue("pageSize", PageSize);

        using var reader = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Full EricksonLopez Keyset execution (Cursor decoding + KeysetBuilder + DB execution + Cursor encoding).
    /// </summary>
    [Benchmark]
    public async Task<object> EricksonLopez_Keyset_Full()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Keyset(_keysetParams)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
    }

    /// <summary>
    /// Isolated cursor token decode + encode latency.
    /// </summary>
    [Benchmark]
    public string Cursor_Codec_Only()
    {
        var decoded = _encoder.Decode(_encodedCursor);
        return _encoder.Encode(decoded) ?? string.Empty;
    }
}



