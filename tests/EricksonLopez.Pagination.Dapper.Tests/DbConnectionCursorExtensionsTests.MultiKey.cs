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
    public async Task ToCursorPagedListAsync_2Keys_FirstPage_ReturnsCorrectItems()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id > @Cursor1) ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name), 
            cursorDecoder: s => (int.Parse(s.Split(',')[0]), s.Split(',')[1]));

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_WithAllOptionalParameters_AppliesOverridesCorrectly()
    {
        using var connection = await GetConnectionAsync();
        using var transaction = connection.BeginTransaction();
        var parameters = new CursorPaginationParameters(); 
        var sql = "SELECT * FROM Entities WHERE Name LIKE @SearchPattern AND (@Cursor1 IS NULL OR Id > @Cursor1) ORDER BY Id LIMIT @__Pagination_Limit__;";
        var param = new { SearchPattern = "Entity %" };

        var encoder = new CustomCursorEncoder();
        var factory = new CustomFactory();
        var tokenSource = new CancellationTokenSource();

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name), 
            cursorDecoder: s => (int.Parse(s.Split(',')[0]), s.Split(',')[1]),
            param: param,
            transaction: transaction,
            commandTimeout: 30,
            commandType: CommandType.Text,
            defaultPageSize: 4,
            cursorEncoder: encoder,
            factory: factory,
            cancellationToken: tokenSource.Token
        );

        pagedList.Count.Should().Be(4);
        pagedList.StartCursor.Should().Be("CUSTOM_START");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_Backward_ReturnsCorrectItems()
    {
        using var connection = await GetConnectionAsync();
        var before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("20,Entity 20")!;
        var parameters = CursorPaginationParameters.Parse($"last=10&before={before}", null);
        var sql = "SELECT * FROM Entities WHERE Id < @Cursor1 ORDER BY Id DESC LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name), 
            cursorDecoder: s => (int.Parse(s.Split(',')[0]), s.Split(',')[1]));

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_BackwardPagination()
    {
        using var connection = await GetConnectionAsync();
        var before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("20,Entity 20")!;
        var parameters = CursorPaginationParameters.Parse($"last=5&before={before}", null);
        var sql = "SELECT * FROM Entities WHERE Id < @Cursor1 ORDER BY Id DESC LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name), 
            cursorDecoder: s => (int.Parse(s.Split(',')[0]), s.Split(',')[1]));

        pagedList.Count.Should().Be(5);
        pagedList[0].Id.Should().Be(15);
        pagedList[^1].Id.Should().Be(19);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_WithAfter_DecodesProperly()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10|Entity 10");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor1 ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name));

        pagedList.Count.Should().Be(5);
        pagedList[0].Id.Should().Be(11);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_WithSpecialCharacters_EncodesAndDecodesProperly()
    {
        using var connection = await GetConnectionAsync();
        await connection.ExecuteAsync(
            "INSERT INTO Entities (Id, Name) VALUES (@Id, @Name)",
            new { Id = 999, Name = "Entity|With%Special" });
            
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("999|Entity%7CWith%25Special");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor1 ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name));

        pagedList.Count.Should().Be(0);
        
        var dummyParams = CursorPaginationParameters.Parse("first=5", null);
        var sql2 = "SELECT * FROM Entities WHERE Id = 999 ORDER BY Id LIMIT @__Pagination_Limit__;";
        
        var pagedList2 = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql2, 
            dummyParams, 
            keySelector: e => (e.Id, e.Name));
            
        pagedList2.Count.Should().Be(1);
        pagedList2.StartCursor.Should().Be(after);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_BackwardWithNoBefore_StartsFromEnd()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("last=5", null);
        var sql = "SELECT * FROM Entities ORDER BY Id DESC LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name));

        pagedList.Count.Should().Be(5);
        pagedList[^1].Id.Should().Be(25); 
        pagedList[0].Id.Should().Be(21);
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_InvalidParts_ThrowsException()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor1 ORDER BY Id LIMIT @__Pagination_Limit__;";

        Func<Task> act = async () => await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Expected 2 parts*");
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_UnDecodable_ThrowsException()
    {
        using var connection = await GetConnectionAsync();
        var after = "!@#$";
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor1 ORDER BY Id LIMIT @__Pagination_Limit__;";

        Func<Task> act = async () => await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name));

        await act.Should().ThrowAsync<InvalidPaginationCursorException>().WithMessage("*could not be decoded*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_ConvertFails_ThrowsException()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("NotAnInt|ValidString");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor1 ORDER BY Id LIMIT @__Pagination_Limit__;";

        Func<Task> act = async () => await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name));

        await act.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_DecoderReturnsNull_ThrowsException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=5&after=SOMETHING", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor1 ORDER BY Id LIMIT @__Pagination_Limit__;";

        Func<Task> act = async () => await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name),
            cursorEncoder: new NullReturningEncoder());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*could not be decoded.*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_NullMaxPageSize_ReturnsExpected()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10 };
        var sql = "SELECT * FROM Entities WHERE Id > COALESCE(@Cursor1, 0) ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name),
            maxPageSize: null);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithPageSizeGreaterThanMax_Composite_CapsPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 50 };
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, int>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Id),
            maxPageSize: 10);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_FirstAndLastNull_Composite_UsesForwardLogic()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { };
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, int>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Id));

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_WithAfter_DefaultDecoder_DecodesProperly()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10|20");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities ORDER BY Id, Name LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name)); 

        pagedList.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_2Keys_WithAfter_InvalidParts_ThrowsInvalidOperationException()
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities ORDER BY Id, Name LIMIT @__Pagination_Limit__;";

        var act = async () => await connection.ToCursorPagedListAsync<Entity, int, string>(
            sql, 
            parameters, 
            keySelector: e => (e.Id, e.Name)); 

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expected 2 parts in composite cursor*");
    }

    [Fact]
    public async Task ToStreamingAsyncEnumerable_2Keys_StreamsAllChunks()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters();
        var sql = "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id > @Cursor1) ORDER BY Id, Name LIMIT @__Pagination_Limit__;";

        var stream = connection.ToStreamingAsyncEnumerable<Entity, int, string>(
            sql,
            parameters,
            key1Selector: e => e.Id,
            key2Selector: e => e.Name,
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
    public async Task ToStreamingAsyncEnumerable_2Keys_Backward_ThrowsInvalidOperationException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { Last = 5 };
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__;";

        var stream = connection.ToStreamingAsyncEnumerable<Entity, int, string>(
            sql,
            parameters,
            key1Selector: e => e.Id,
            key2Selector: e => e.Name);

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
    public async Task ToStreamingAsyncEnumerable_2Keys_WithAfter_AndCustomEncoder_StreamsRemaining()
    {
        using var connection = await GetConnectionAsync();
        var customEncoder = new Base64CursorEncoder();
        var afterCursor = customEncoder.Encode("20|Item 20");
        var parameters = new CursorPaginationParameters { After = afterCursor };
        var sql = "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR (Id > @Cursor1 OR (Id = @Cursor1 AND (@Cursor2 IS NULL OR Name > @Cursor2)))) ORDER BY Id, Name LIMIT @__Pagination_Limit__;";

        var stream = connection.ToStreamingAsyncEnumerable<Entity, int, string>(
            sql,
            parameters,
            key1Selector: e => e.Id,
            key2Selector: e => e.Name,
            cursorDecoder: s => { var p = s.Split('|'); return (int.Parse(p[0]), p[1]); },
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
    public async Task CompositeKey_Backward_WithBefore_PaginatesCorrectly()
    {
        using var db = await DapperTestHelper.GetConnectionAsync();
        var before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("21|210")!;
        var parameters = new CursorPaginationParameters { Last = 10, Before = before };
        
        var result = await db.ToCursorPagedListAsync<Entity, int, int>(
            "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id1 < @Cursor1) OR (Id1 = @Cursor1 AND Id2 < @Cursor2) ORDER BY Id1 DESC, Id2 DESC LIMIT @__Pagination_Limit__",
            parameters,
            e => (e.Id1, e.Id2));
            
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result[0].Id1.Should().Be(11);
        result[^1].Id1.Should().Be(20);
    }
    
    [Fact]
    public async Task CompositeKey_Backward_WithoutBefore_ReturnsLastPage()
    {
        using var db = await DapperTestHelper.GetConnectionAsync();
        var parameters = new CursorPaginationParameters { Last = 10 };
        
        var result = await db.ToCursorPagedListAsync<Entity, int, int>(
            "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id1 < @Cursor1) OR (Id1 = @Cursor1 AND Id2 < @Cursor2) ORDER BY Id1 DESC, Id2 DESC LIMIT @__Pagination_Limit__",
            parameters,
            e => (e.Id1, e.Id2));
            
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
        result[0].Id1.Should().Be(16);
        result[^1].Id1.Should().Be(25);
    }
    
    [Fact]
    public async Task CompositeKey_Forward_WithAfter_PaginatesCorrectly()
    {
        using var db = await DapperTestHelper.GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("10|100")!;
        var parameters = new CursorPaginationParameters { First = 10, After = after };
        
        var result = await db.ToCursorPagedListAsync<Entity, int, int>(
            "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id1 > @Cursor1) OR (Id1 = @Cursor1 AND Id2 > @Cursor2) ORDER BY Id1 ASC, Id2 ASC LIMIT @__Pagination_Limit__",
            parameters,
            e => (e.Id1, e.Id2));
            
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result[0].Id1.Should().Be(11);
        result[^1].Id1.Should().Be(20);
    }
    
    [Fact]
    public async Task CompositeKey_Forward_WithoutAfter_ReturnsFirstPage()
    {
        using var db = await DapperTestHelper.GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10 };
        
        var result = await db.ToCursorPagedListAsync<Entity, int, int>(
            "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id1 > @Cursor1) OR (Id1 = @Cursor1 AND Id2 > @Cursor2) ORDER BY Id1 ASC, Id2 ASC LIMIT @__Pagination_Limit__",
            parameters,
            e => (e.Id1, e.Id2));
            
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result[0].Id1.Should().Be(1);
        result[^1].Id1.Should().Be(10);
    }

    [Fact]
    public async Task CompositeKey_TieBreaker_PaginatesCorrectlyOnSecondaryKey()
    {
        using var db = new SqliteConnection("DataSource=:memory:");
        db.Open();
        db.Execute("CREATE TABLE Entities (Id1 INT, Id2 INT, Name VARCHAR(100));");
        for (int i = 1; i <= 10; i++)
        {
            db.Execute("INSERT INTO Entities (Id1, Id2, Name) VALUES (1, @i, 'Entity ' || @i)", new { i });
        }
        
        var afterCursor = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("1|5");
        var parameters = new CursorPaginationParameters { First = 10, After = afterCursor };
        
        var result = await db.ToCursorPagedListAsync<Entity, int, int>(
            "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id1 > @Cursor1) OR (Id1 = @Cursor1 AND Id2 > @Cursor2) ORDER BY Id1 ASC, Id2 ASC LIMIT @__Pagination_Limit__",
            parameters,
            e => (e.Id1, e.Id2));
            
        result.Count.Should().Be(5);
        result[0].Id2.Should().Be(6);
        result[^1].Id2.Should().Be(10);
    }

    [Fact]
    public async Task CompositeKey_ExactPageSize_ReturnsAllItemsAndHasNextPageFalse()
    {
        using var db = await DapperTestHelper.GetConnectionAsync(10);
        var parameters = new CursorPaginationParameters { First = 10 };
        
        var result = await db.ToCursorPagedListAsync<Entity, int, int>(
            "SELECT * FROM Entities WHERE (@Cursor1 IS NULL OR Id1 > @Cursor1) OR (Id1 = @Cursor1 AND Id2 > @Cursor2) ORDER BY Id1 ASC, Id2 ASC LIMIT @__Pagination_Limit__",
            parameters,
            e => (e.Id1, e.Id2));
            
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
        result.EndCursor.Should().NotBeNull();
        
        var decodedEnd = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Decode(result.EndCursor!);
        decodedEnd.Should().Be("10|100");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_CompositeKeys_WithCustomFactory_AppliesFactory()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 5 };
        var sql = "SELECT * FROM Entities ORDER BY Id1 ASC, Id2 ASC LIMIT @__Pagination_Limit__;";
        var factory = new CustomFactory();

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int, int>(
            sql,
            parameters,
            keySelector: e => (e.Id1, e.Id2),
            factory: factory
        );

        pagedList.Should().NotBeNull();
        pagedList.StartCursor.Should().Be("CUSTOM_START");
        pagedList.EndCursor.Should().Be("CUSTOM_END");
    }
}
