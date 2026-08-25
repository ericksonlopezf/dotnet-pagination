// Copyright © Erickson Lopez. MIT License.
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
using NSubstitute;
using SortDirection = EricksonLopez.Pagination.Abstractions.SortDirection;
using Xunit;

namespace EricksonLopez.Pagination.MongoDB.Tests;

public class MongoObjectIdDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class MongoObjectIdKeysetTests
{
    private readonly List<MongoObjectIdDocument> _items;

    public MongoObjectIdKeysetTests()
    {
        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _items = new List<MongoObjectIdDocument>();

        for (int i = 1; i <= 20; i++)
        {
            // Generate deterministic sequential ObjectIds
            var oid = ObjectId.GenerateNewId(baseDate.AddSeconds(i));
            _items.Add(new MongoObjectIdDocument
            {
                Id = oid,
                Name = $"Item {i}",
                Value = i * 10
            });
        }
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ObjectId_FirstPage_ReturnsCorrectItems()
    {
        var queryable = new MockMongoQueryable<MongoObjectIdDocument>(_items);
        var parameters = new CursorPaginationParameters { First = 5 };

        var result = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            parameters,
            SortDirection.Ascending);

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().NotBeNullOrWhiteSpace();
        result.EndCursor.Should().NotBeNullOrWhiteSpace();
        result[0].Name.Should().Be("Item 1");
        result[4].Name.Should().Be("Item 5");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ObjectId_SecondPage_WithAfterCursor()
    {
        var queryable = new MockMongoQueryable<MongoObjectIdDocument>(_items);
        var page1Params = new CursorPaginationParameters { First = 5 };

        var page1 = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            page1Params,
            SortDirection.Ascending);

        var page2Params = new CursorPaginationParameters
        {
            First = 5,
            After = page1.EndCursor
        };

        var page2 = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            page2Params,
            SortDirection.Ascending);

        page2.Should().NotBeNull();
        page2.Count.Should().Be(5);
        page2.HasNextPage.Should().BeTrue();
        page2.HasPreviousPage.Should().BeTrue();
        page2[0].Name.Should().Be("Item 6");
        page2[4].Name.Should().Be("Item 10");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ObjectId_BackwardPagination_WithBeforeCursor()
    {
        var queryable = new MockMongoQueryable<MongoObjectIdDocument>(_items);
        var page1Params = new CursorPaginationParameters { First = 5 };

        var page1 = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            page1Params,
            SortDirection.Ascending);

        var page2Params = new CursorPaginationParameters
        {
            First = 5,
            After = page1.EndCursor
        };

        var page2 = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            page2Params,
            SortDirection.Ascending);

        var backParams = new CursorPaginationParameters
        {
            Last = 5,
            Before = page2.StartCursor
        };

        var backPage = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            backParams,
            SortDirection.Ascending);

        backPage.Should().NotBeNull();
        backPage.Count.Should().Be(5);
        backPage[0].Name.Should().Be("Item 1");
        backPage[4].Name.Should().Be("Item 5");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ObjectId_DescendingOrder()
    {
        var queryable = new MockMongoQueryable<MongoObjectIdDocument>(_items);
        var parameters = new CursorPaginationParameters { First = 5 };

        var result = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            parameters,
            SortDirection.Descending);

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result[0].Name.Should().Be("Item 20");
        result[4].Name.Should().Be("Item 16");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ObjectId_EmptySource_ReturnsEmpty()
    {
        var empty = new List<MongoObjectIdDocument>();
        var queryable = new MockMongoQueryable<MongoObjectIdDocument>(empty);
        var parameters = new CursorPaginationParameters { First = 5 };

        var result = await queryable.ToCursorPagedListAsync(
            x => x.Id,
            parameters);

        result.Should().NotBeNull();
        result.Count.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().BeNull();
        result.EndCursor.Should().BeNull();
    }

    [Fact]
    public void ObjectId_DefaultMongoCursorDecoderRegistry_DecodesSuccessfully()
    {
        var registry = DefaultMongoCursorDecoderRegistry.Instance;
        var hasDecoder = registry.TryGetDecoder<ObjectId>(out var decoder);

        hasDecoder.Should().BeTrue();
        decoder.Should().NotBeNull();

        var originalOid = ObjectId.GenerateNewId();
        var decodedOid = decoder!(originalOid.ToString());

        decodedOid.Should().Be(originalOid);
    }

    [Fact]
    public async Task MongoObjectIdPaginationExtensions_NullCollection_ThrowsArgumentNullException()
    {
        IMongoCollection<MongoObjectIdDocument> collection = null!;
        Func<Task> act = async () => await collection.ToCursorPagedListAsync(
            null,
            x => x.Id,
            new CursorPaginationParameters());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("collection");
    }

    [Fact]
    public async Task MongoObjectIdPaginationExtensions_NullKeySelector_ThrowsArgumentNullException()
    {
        var collection = NSubstitute.Substitute.For<IMongoCollection<MongoObjectIdDocument>>();
        Func<Task> act = async () => await collection.ToCursorPagedListAsync(
            null,
            null!,
            new CursorPaginationParameters());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("keySelector");
    }



}





