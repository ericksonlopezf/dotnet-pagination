// Copyright © Erickson Lopez. MIT License.
#pragma warning disable CS0618
#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Pagination.Dapper.Tests;

public partial class DbConnectionCursorExtensionsTests
{
    [Fact]
    public async Task ToCursorPagedListAsync_FirstPage_ReturnsCorrectItems()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = "SELECT * FROM Entities WHERE (@Cursor IS NULL OR Id > @Cursor) ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s));

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(1);
        pagedList[^1].Id.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithMaxPageSizeNull_UsesDefaultPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = "SELECT * FROM Entities WHERE (@Cursor IS NULL OR Id > @Cursor) ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s),
            maxPageSize: null);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_BackwardPagination_ReturnsCorrectItems()
    {
        using var connection = await GetConnectionAsync();
        var before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("20")!;
        var parameters = CursorPaginationParameters.Parse($"last=10&before={before}", null);
        var sql = "SELECT * FROM Entities WHERE Id < @Cursor ORDER BY Id DESC LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s));

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(10);
        pagedList[^1].Id.Should().Be(19);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithNoParameters_UsesDefaultPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters();
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s),
            defaultPageSize: 2);

        pagedList.Count.Should().Be(2);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_BackwardWithNoBefore_StartsFromEnd()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("last=5", null);
        var sql = "SELECT * FROM Entities ORDER BY Id DESC LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s));

        pagedList.Count.Should().Be(5);
        pagedList[^1].Id.Should().Be(25);
        pagedList[0].Id.Should().Be(21);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter_UsesCursor()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10")!;
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s));

        pagedList.Count.Should().Be(5);
        pagedList[0].Id.Should().Be(11);
        pagedList[^1].Id.Should().Be(15);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_EmptyResult_ReturnsEmptyList()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=5", null);
        var sql = "SELECT * FROM Entities WHERE Id > 100 ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s));

        pagedList.Count.Should().Be(0);
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeFalse();
        pagedList.StartCursor.Should().BeNull();
        pagedList.EndCursor.Should().BeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_Long_DecodesProperly()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, long>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Count.Should().Be(5);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_Guid_DecodesProperly()
    {
        using var connection = await GetConnectionAsync();
        var guid = Guid.NewGuid();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode(guid.ToString());
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Name > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, Guid>(
            sql, 
            parameters, 
            keySelector: e => Guid.NewGuid());

        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_String_DecodesProperly()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("SomeString");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Name > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, string>(
            sql, 
            parameters, 
            keySelector: e => e.Name);

        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_NullMaxPageSize_ReturnsExpected()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10 };
        var sql = "SELECT * FROM Entities WHERE Id > COALESCE(@Cursor, 0) ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id,
            maxPageSize: null);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithBothFirstAndLast_EvaluatesToForward()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10, Last = 5 };
        var sql = "SELECT * FROM Entities WHERE Id > COALESCE(@Cursor, 0) ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_CustomCursorDecoder()
    {
        using var connection = await GetConnectionAsync();
        var encoder = new HmacCursorEncoder("abcdefghijklmnopqrstuvwxyz123456");
        var encodedCursor = encoder.Encode("42");
        
        var parameters = new CursorPaginationParameters { Last = 5, Before = encodedCursor };
        var sql = "SELECT * FROM Entities WHERE Id < @Cursor ORDER BY Id DESC LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id,
            cursorEncoder: encoder,
            cursorDecoder: s => int.Parse(s));

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(5); 
        pagedList[0].Id.Should().Be(21);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_RegisteredCursorDecoder()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register<ushort>(s => ushort.Parse(s));
        
        using var connection = await GetConnectionAsync();
        var encoder = new HmacCursorEncoder("abcdefghijklmnopqrstuvwxyz123456");
        var encodedCursor = encoder.Encode("10");
        
        var parameters = new CursorPaginationParameters { First = 5, After = encodedCursor };
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id ASC LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, ushort>(
            sql, 
            parameters, 
            keySelector: e => (ushort)e.Id,
            cursorEncoder: encoder,
            decoderRegistry: registry);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(5); 
        pagedList[0].Id.Should().Be(11);
    }

    [Fact]
    public async Task ToCursorPagedAsyncEnumerable_Forward_ReturnsAsyncEnumerable()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = "SELECT * FROM Entities WHERE (@Cursor IS NULL OR Id > @Cursor) ORDER BY Id LIMIT @__Pagination_Limit__;";

        var asyncEnumerable = connection.ToCursorPagedAsyncEnumerable<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s));

        var items = new List<Entity>();
        await foreach (var item in asyncEnumerable)
        {
            items.Add(item);
        }

        items.Count.Should().Be(10);
        items[0].Id.Should().Be(1);
        items[^1].Id.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedAsyncEnumerable_Backward_ThrowsInvalidOperationException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("last=10", null);
        var sql = "SELECT * FROM Entities ORDER BY Id DESC LIMIT @__Pagination_Limit__;";

        var act = () => connection.ToCursorPagedAsyncEnumerable<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s)).GetAsyncEnumerator().MoveNextAsync().AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Streaming is not supported when paginating backwards*");
    }

    [Fact]
    public async Task ToCursorPagedAsyncEnumerable_WithAfter_StartsCorrectly()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("5");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE (@Cursor IS NULL OR Id > @Cursor) ORDER BY Id LIMIT @__Pagination_Limit__;";

        var asyncEnumerable = connection.ToCursorPagedAsyncEnumerable<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s));

        var items = new List<Entity>();
        await foreach (var item in asyncEnumerable)
        {
            items.Add(item);
        }

        items.Count.Should().Be(5);
        items[0].Id.Should().Be(6);
        items[^1].Id.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter_DefaultDecoder_Int_UsesCursor()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Count.Should().Be(5);
        pagedList[0].Id.Should().Be(11);
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter_DefaultDecoder_Long_UsesCursor()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, long>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Count.Should().Be(5);
        pagedList[0].Id.Should().Be(11);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter_DefaultDecoder_Guid_UsesCursor()
    {
        using var connection = await GetConnectionAsync();
        var guid = Guid.NewGuid();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode(guid.ToString());
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, Guid>(
            sql, 
            parameters, 
            keySelector: e => Guid.NewGuid());

        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter_DefaultDecoder_String_UsesCursor()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("hello");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Name > @Cursor ORDER BY Name LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, string>(
            sql, 
            parameters, 
            keySelector: e => "hello"); 

        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter_DefaultDecoder_Double_UsesCursor()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10.5");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, double>(
            sql, 
            parameters, 
            keySelector: e => 10.5); 

        pagedList.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_WithAfter_InvalidFormat_ThrowsInvalidOperationException()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("not_an_int");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        var act = async () => await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Could not convert the decoded cursor value*");
    }

    [Fact]
    public async Task ToCursorPagedAsyncEnumerable_NoRows_YieldsNothing()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10 };
        var sql = "SELECT * FROM Entities WHERE 1=0 ORDER BY Id LIMIT @__Pagination_Limit__;";

        var enumerable = connection.ToCursorPagedAsyncEnumerable<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        var list = new List<Entity>();
        await foreach (var item in enumerable)
        {
            list.Add(item);
        }

        list.Should().BeEmpty();
    }

    [Fact]
    public async Task ToStreamingAsyncEnumerable_SingleKey_StreamsAllChunks()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters();
        var sql = "SELECT * FROM Entities WHERE (@Cursor IS NULL OR Id > @Cursor) ORDER BY Id LIMIT @__Pagination_Limit__;";

        var stream = connection.ToStreamingAsyncEnumerable<Entity, int>(
            sql,
            parameters,
            keySelector: e => e.Id,
            chunkSize: 5);

        var list = new List<Entity>();
        await foreach (var item in stream)
        {
            list.Add(item);
        }

        list.Should().HaveCount(25);
        list[0].Id.Should().Be(1);
        list[^1].Id.Should().Be(25);
    }

    [Fact]
    public async Task ToStreamingAsyncEnumerable_SingleKey_WithAfter_StreamsRemaining()
    {
        using var connection = await GetConnectionAsync();
        var customEncoder = new Base64CursorEncoder();
        var afterCursor = customEncoder.Encode("20");
        var parameters = new CursorPaginationParameters { After = afterCursor };
        var sql = "SELECT * FROM Entities WHERE (@Cursor IS NULL OR Id > @Cursor) ORDER BY Id LIMIT @__Pagination_Limit__;";

        var stream = connection.ToStreamingAsyncEnumerable<Entity, int>(
            sql,
            parameters,
            keySelector: e => e.Id,
            cursorEncoder: customEncoder,
            chunkSize: 2);

        var list = new List<Entity>();
        await foreach (var item in stream)
        {
            list.Add(item);
        }

        list.Should().HaveCount(5);
        list[0].Id.Should().Be(21);
        list[^1].Id.Should().Be(25);
    }

    [Fact]
    public async Task ToStreamingAsyncEnumerable_SingleKey_Backward_ThrowsInvalidOperationException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { Last = 5 };
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__;";

        var stream = connection.ToStreamingAsyncEnumerable<Entity, int>(
            sql,
            parameters,
            keySelector: e => e.Id);

        Func<Task> act = async () =>
        {
            await foreach (var item in stream)
            {
                _ = item;
            }
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Streaming is not supported when paginating backwards*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_FirstExactPageSize_ReturnsHasNextPageFalse()
    {
        using var connection = await DapperTestHelper.GetConnectionAsync(10);
        var parameters = new CursorPaginationParameters { First = 10 };
        var sql = "SELECT * FROM Entities WHERE Id > COALESCE(@Cursor, 0) ORDER BY Id LIMIT @__Pagination_Limit__";
        
        var paged = await connection.ToCursorPagedListAsync<Entity, int>(sql, parameters, e => e.Id);
        
        paged.HasNextPage.Should().BeFalse();
        paged.Count.Should().Be(10);
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_LastExactPageSize_ReturnsHasPreviousPageFalse()
    {
        using var connection = await DapperTestHelper.GetConnectionAsync(10);
        var parameters = new CursorPaginationParameters { Last = 10 };
        var sql = "SELECT * FROM Entities ORDER BY Id DESC LIMIT @__Pagination_Limit__";
        
        var paged = await connection.ToCursorPagedListAsync<Entity, int>(sql, parameters, e => e.Id);
        
        paged.HasPreviousPage.Should().BeFalse();
        paged.Count.Should().Be(10);
    }
}
