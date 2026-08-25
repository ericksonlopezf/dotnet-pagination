// Copyright © Erickson Lopez. MIT License.
#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace EricksonLopez.Pagination.MongoDB.Tests;

public class TestDocument
{
    [BsonId]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
}

public class TestOidDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

[Collection("MongoDB")]
public class FindFluentPaginationExtensionsTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    public FindFluentPaginationExtensionsTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }
    private IMongoCollection<TestDocument> _collection = null!;
    private IMongoCollection<TestOidDocument> _oidCollection = null!;
    private MongoClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = new MongoClient(_fixture.Container.GetConnectionString());
        var database = _client.GetDatabase("testdb_" + Guid.NewGuid().ToString("N")); // isolated db for each test
        _collection = database.GetCollection<TestDocument>("testdocs");

        var documents = Enumerable.Range(1, 25).Select(i => new TestDocument
        {
            Id = i,
            Name = $"Doc {i}"
        }).ToList();

        await _collection.InsertManyAsync(documents);
        _oidCollection = database.GetCollection<TestOidDocument>("testoids");
        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var oidDocs = Enumerable.Range(1, 25).Select(i => new TestOidDocument
        {
            Id = ObjectId.GenerateNewId(baseDate.AddSeconds(i)),
            Name = $"OidDoc {i}",
            Value = i * 10
        }).ToList();
        await _oidCollection.InsertManyAsync(oidDocs);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ToCursorPagedListAsync_OnCollection_WithFilter_FiltersAndPaginates()
    {
        var filter = Builders<TestOidDocument>.Filter.Gt(x => x.Value, 50);
        var pagedList = await _oidCollection.ToCursorPagedListAsync(
            filter,
            x => x.Id,
            new CursorPaginationParameters { First = 10 });

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.All(x => x.Value > 50).Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithCountTotal_ReturnsCorrectPage()
    {
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToPagedListAsync(parameters, countTotal: true);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.TotalCount.Should().Be(25);
        pagedList.HasNextPage.Should().BeTrue(); 
    }

    [Fact]
    public async Task ToPagedListAsync_WithoutCountTotal_ReturnsCorrectPage()
    {
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToPagedListAsync(parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.TotalCount.Should().BeNull();
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithoutCountTotal_LastPage_HasNoNextPage()
    {
        var parameters = PaginationParameters.Create(page: 3, pageSize: 10);
        
        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToPagedListAsync(parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(5);
        pagedList.TotalCount.Should().BeNull();
        pagedList.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public async Task ToPagedListAsync_EmptyResult_WithCountTotal_ReturnsEmpty()
    {
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Eq(x => x.Id, 100))
            .ToPagedListAsync(parameters, countTotal: true);

        pagedList.Count.Should().Be(0);
        pagedList.TotalCount.Should().Be(0);
        pagedList.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public async Task ToPagedListAsync_WithoutCountTotal_ExactPageSizeRemaining_HasNoNextPage()
    {
        var parameters = PaginationParameters.Create(page: 1, pageSize: 25);
        
        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToPagedListAsync(parameters, countTotal: false);

        pagedList.Count.Should().Be(25);
        pagedList.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public async Task ToPagedListAsync_MaxPageSize_ClampsPageSize()
    {
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10000);
        
        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToPagedListAsync(parameters, countTotal: false, maxPageSize: 5);

        pagedList.Count.Should().Be(5);
        pagedList.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ValueTypeKey_FirstPage()
    {
        var parameters = CursorPaginationParameters.Parse("first=10", null);

        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                parameters: parameters);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(1);
        pagedList[^1].Id.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_MaxPageSize_ClampsPageSize()
    {
        var parameters = CursorPaginationParameters.Parse("first=100", null);

        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                parameters: parameters,
                maxPageSize: 5);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(5);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_ProjectsCorrectly()
    {
        var parameters = CursorPaginationParameters.Parse("first=10", null);

        var pagedList = await _collection.AsQueryable()
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                projection: x => new { x.Id, NameUpper = x.Name.ToUpperInvariant() },
                parameters: parameters);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList[0].NameUpper.Should().Be("DOC 1");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_MaxPageSize_ClampsPageSize()
    {
        var parameters = CursorPaginationParameters.Parse("first=100", null);

        var pagedList = await _collection.AsQueryable()
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                projection: x => new { x.Id, NameUpper = x.Name.ToUpperInvariant() },
                parameters: parameters,
                maxPageSize: 5);

        pagedList.Count.Should().Be(5);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_BackwardPagination()
    {
        var parameters = CursorPaginationParameters.Parse("last=10", null) with { Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("20") };

        var pagedList = await _collection.AsQueryable()
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                projection: x => new { x.Id, NameUpper = x.Name.ToUpperInvariant() },
                parameters: parameters);

        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(10);
        pagedList[^1].Id.Should().Be(19);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter()
    {
        var parameters = CursorPaginationParameters.Parse("first=10", null) with { After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10") };

        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                parameters: parameters);

        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(11);
        pagedList[^1].Id.Should().Be(20);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_BackwardPagination()
    {
        var parameters = CursorPaginationParameters.Parse("last=10", null) with { Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("20") };

        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                parameters: parameters);

        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(10);
        pagedList[^1].Id.Should().Be(19);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Descending()
    {
        var parameters = CursorPaginationParameters.Parse("first=10", null) with { After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("20") };

        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                parameters: parameters,
                direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending);

        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(19);
        pagedList[^1].Id.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Backward_Descending()
    {
        var parameters = CursorPaginationParameters.Parse("last=10", null) with { Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10") };

        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                parameters: parameters,
                direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending);

        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(20);
        pagedList[^1].Id.Should().Be(11);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ReferenceTypeKey_FirstPage()
    {
        var parameters = CursorPaginationParameters.Parse("first=10", null);

        var pagedList = await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Name,
                parameters: parameters);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_MaxPageSizeNull_UsesParameterSize()
    {
        var parameters = PaginationParameters.Parse("page=1&pageSize=10", null);
        var items = new List<TestDocument>();

        await foreach (var item in _collection.AsQueryable().OrderBy(x => x.Id)
            .ToPagedAsyncEnumerable(parameters, maxPageSize: null))
        {
            items.Add(item);
        }

        items.Should().HaveCount(10);
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_MaxPageSizeExceeded_UsesMaxPageSize()
    {
        var parameters = PaginationParameters.Parse("page=1&pageSize=100", null);
        var items = new List<TestDocument>();

        await foreach (var item in _collection.AsQueryable().OrderBy(x => x.Id)
            .ToPagedAsyncEnumerable(parameters, maxPageSize: 5))
        {
            items.Add(item);
        }

        items.Should().HaveCount(5);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithCustomFactory_UsesFactory()
    {
        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var factory = new FakeFactory();

        await _collection.Find(Builders<TestDocument>.Filter.Empty)
            .ToCursorPagedListAsync(
                keySelector: x => x.Id,
                parameters: parameters,
                factory: factory);

        factory.WasCalled.Should().BeTrue();
    }

    private sealed class FakeFactory : ICursorPagedListFactory
    {
        public bool WasCalled { get; private set; }
        public ICursorPagedList<T> CreateCursorPagedList<T>(IReadOnlyList<T> items, long? totalCount, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)
        {
            WasCalled = true;
            return DefaultPagedListFactory.Instance.CreateCursorPagedList(items, totalCount, startCursor, endCursor, hasPreviousPage, hasNextPage);
        }
    }

    [Fact]
    public void ApplySort_FieldTooLong_IgnoresField()
    {
        var longName = new string('a', 129);
        var parameters = SortParameters.Parse(longName, null);
        
        var query = _collection.AsQueryable().ApplySort(parameters, EricksonLopez.Pagination.Abstractions.SortDirection.Ascending, x => x.Id);
        
        // It ignores the long field and applies the default sort
        query.ToString().Should().Contain("_id");
    }

    [Fact]
    public void ApplySort_FieldNotFound_IgnoresField()
    {
        var parameters = SortParameters.Parse("NonExistentField", null);
        
        var query = _collection.AsQueryable().ApplySort(parameters, EricksonLopez.Pagination.Abstractions.SortDirection.Ascending, x => x.Id);
        
        // It ignores the non-existent field and applies the default sort
        query.ToString().Should().Contain("_id");
    }

    private sealed class TestObjectIdDoc
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task MongoObjectIdPaginationExtensions_ToCursorPagedListAsync_WithRealCollection_Works()
    {
        var database = _client.GetDatabase("testdb_oid_" + Guid.NewGuid().ToString("N"));
        var collection = database.GetCollection<TestObjectIdDoc>("oid_docs");

        var docs = Enumerable.Range(1, 15).Select(i => new TestObjectIdDoc
        {
            Id = ObjectId.GenerateNewId(),
            Name = $"Doc {i}"
        }).ToList();

        await collection.InsertManyAsync(docs);

        var result = await collection.ToCursorPagedListAsync(
            filter: null,
            keySelector: x => x.Id,
            parameters: new CursorPaginationParameters { First = 5 });

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().NotBeNullOrWhiteSpace();
        result.EndCursor.Should().NotBeNullOrWhiteSpace();
    }
}





