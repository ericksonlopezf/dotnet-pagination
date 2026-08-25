// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.LinqToDB;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Pagination.LinqToDB.Tests;

public sealed class TestDataConnection : DataConnection
{
    public TestDataConnection(DataOptions<TestDataConnection> options) : base(options.Options) { }

    public ITable<TestEntity> Entities => this.GetTable<TestEntity>();
}

public enum TestState { One = 1, Two = 2 }

[global::LinqToDB.Mapping.Table("TestEntities")]
public class TestEntity
{
    [global::LinqToDB.Mapping.PrimaryKey, global::LinqToDB.Mapping.Identity]
    public int Id { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public int? NullableId { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public string Name { get; set; } = string.Empty;
    
    [global::LinqToDB.Mapping.Column]
    public string? NullableString { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public TestState StateValue { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public Guid GuidValue { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public int AdditionalValue { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public string? Name2 { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public string? Name3 { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public bool BooleanValue { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public string? Name4 { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public string? Name5 { get; set; }
    
    [global::LinqToDB.Mapping.Column(DataType = global::LinqToDB.DataType.Int32)]
    public int CustomStructValueInt { get; set; }
    
    [global::LinqToDB.Mapping.Column]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed partial class QueryableLinqToDBExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public QueryableLinqToDBExtensionsTests()
    {
        _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private TestDataConnection GetDatabase()
    {
        var options = new DataOptions<TestDataConnection>(new DataOptions().UseSQLite(_connection.ConnectionString));
        var context = new TestDataConnection(options);

        // Create table and seed data
        try
        {
            context.CreateTable<TestEntity>();
            var entities = Enumerable.Range(1, 25).Select(i => new TestEntity { Id = i, Name = $"Item {i}" });
            context.BulkCopy(entities);
        }
        catch
        {
            // Ignore if already created due to shared cache
        }

        return context;
    }

    [Fact]
    public async Task ToPagedListAsync_WithCount_ReturnsCorrectMetadata()
    {
        using var context = GetDatabase();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: true);

        paged.TotalCount.Should().Be(25);
        paged.TotalPages.Should().Be(3);
        paged.Count.Should().Be(10);
        paged[0].Id.Should().Be(11);
        paged.HasNextPage.Should().BeTrue();
        paged.HasPreviousPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToPagedListAsync_WithCount_EmptyResult_ReturnsEmptyList()
    {
        using var context = GetDatabase();
        var query = context.Entities.Where(e => e.Id > 100).OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: true);

        paged.TotalCount.Should().Be(0);
        paged.Count.Should().Be(0);
        paged.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToPagedListAsync_WithoutCount_EvaluatesHasNextPageCorrectly()
    {
        using var context = GetDatabase();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 3, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: false);

        paged.TotalCount.Should().BeNull();
        paged.TotalPages.Should().BeNull();
        paged.Count.Should().Be(5);
        paged[0].Id.Should().Be(21);
        paged.HasNextPage.Should().BeFalse();
        paged.HasPreviousPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToPagedListAsync_WithoutCount_WithNextPage_ReturnsHasNextPageTrue()
    {
        using var context = GetDatabase();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: false);

        paged.Count.Should().Be(10);
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelectorAndCount_ReturnsMappedList()
    {
        using var context = GetDatabase();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        var paged = await query.ToPagedListAsync(e => e.Name, parameters, countTotal: true);

        paged.TotalCount.Should().Be(25);
        paged.Count.Should().Be(10);
        paged[0].Should().Be("Item 11");
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelectorAndCount_EmptyResult_ReturnsEmptyList()
    {
        using var context = GetDatabase();
        var query = context.Entities.Where(e => e.Id > 100).OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(e => e.Name, parameters, countTotal: true);

        paged.TotalCount.Should().Be(0);
        paged.Count.Should().Be(0);
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelectorWithoutCount_ReturnsMappedList()
    {
        using var context = GetDatabase();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(e => e.Name, parameters, countTotal: false);

        paged.Count.Should().Be(10);
        paged[0].Should().Be("Item 1");
        paged.HasNextPage.Should().BeTrue();
    }
}



