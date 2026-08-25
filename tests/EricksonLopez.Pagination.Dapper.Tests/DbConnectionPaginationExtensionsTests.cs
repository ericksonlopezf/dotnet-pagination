// Copyright © Erickson Lopez. MIT License.
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
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.Dapper.Tests;

public class DbConnectionPaginationExtensionsTests
{
    private static Task<SqliteConnection> GetConnectionAsync() => DapperTestHelper.GetConnectionAsync();

    [Fact]
    public async Task ToPagedListAsync_WithCountTotal_ReturnsCorrectPageAndCount()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);
        var sql = "SELECT COUNT(*) FROM Entities; SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__;";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: true);

        pagedList.Should().NotBeNull();
        pagedList.TotalCount.Should().Be(25);
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToPagedListAsync_WithCountTotalZero_ReturnsEmptyPage()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        var sql = "SELECT COUNT(*) FROM Entities WHERE Id > 100; SELECT * FROM Entities WHERE Id > 100 ORDER BY Id LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__;";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: true);

        pagedList.Should().NotBeNull();
        pagedList.TotalCount.Should().Be(0);
        pagedList.Count.Should().Be(0);
    }

    [Fact]
    public async Task ToPagedListAsync_WithCountTotal_InvalidFirstResultSet_ThrowsException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        // Returns string Name instead of COUNT(*)
        var sql = "SELECT Name FROM Entities LIMIT 1; SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__;";

        Func<Task> act = async () => await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: true);

        await act.Should().ThrowAsync<System.InvalidOperationException>().WithMessage("*COUNT(*)*");
    }

    [Fact]
    public async Task ToPagedListAsync_WithoutCount_ReturnsCorrectPage()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 3, pageSize: 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.TotalCount.Should().BeNull();
        pagedList.Count.Should().Be(5);
    }

    [Fact]
    public async Task ToPagedListAsync_WithoutCount_WithNextPage_ReturnsHasNextPageTrue()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.Count.Should().Be(10);
    }
    
    [Fact]
    public async Task ToPagedListAsync_WithoutCount_ExactPageSize_ReturnsHasNextPageFalse()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        // Return exactly 10 items instead of 11 using a where clause or just by the nature of the table.
        // Wait, if we use LIMIT @__Pagination_PageSize__ (not @__Pagination_Limit__), Dapper pagination uses `@__Pagination_Limit__` when we don't count?
        // Wait, ToPagedListAsync automatically passes effectivePageSize + 1 as `@__Pagination_Limit__`? No, we pass `dynParams.Add("@__Pagination_PageSize__", effectivePageSize);`.
        // Wait! The user's query MUST have `@__Pagination_PageSize__`.
        // Dapper extension in `ToPagedListAsync` automatically sets `@__Pagination_PageSize__` to `effectivePageSize`.
        // If it doesn't do `+ 1`, it cannot detect `HasNextPage` correctly!
        // But the user's query here is "LIMIT @__Pagination_PageSize__". So it ONLY fetches `PageSize`.
        // If it only fetches `PageSize`, how does it know if there is a next page?
        // In DbConnectionPaginationExtensions.cs: `var hasNextPage = itemList.Count > effectivePageSize;`
        // Wait, if the query only fetches `@__Pagination_PageSize__`, `itemList.Count` will NEVER be `> effectivePageSize`!
        // Oh... wait!
        var sql = "SELECT * FROM Entities WHERE Id <= 10 ORDER BY Id LIMIT 11 OFFSET @__Pagination_Skip__";
        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToPagedListAsync_WithNullMaxPageSize_ReturnsExpected()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false, maxPageSize: null);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToPagedListAsync_WithPageSizeGreaterThanMax_CapsPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 50);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false, maxPageSize: 10);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithCustomFactory_UsesCustomFactory()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        var factory = NSubstitute.Substitute.For<IPagedListFactory>();
        factory.CreatePagedList(Arg.Any<IReadOnlyList<Entity>>(), null, 1, 10, Arg.Any<bool>())
               .Returns(new EricksonLopez.Pagination.PagedList<Entity>(System.Array.Empty<Entity>(), null, 1, 10, false));
        
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false, factory: factory);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(0);
        factory.ReceivedWithAnyArgs(1).CreatePagedList<Entity>(default!, null, 1, 10, false);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Forward_ReturnsCorrectPage()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10 };
        var sql = "SELECT * FROM Entities WHERE Id > COALESCE(@Cursor, 0) ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.StartCursor.Should().NotBeNull();
        pagedList.EndCursor.Should().NotBeNull();
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();
        
        var nextParams = new CursorPaginationParameters { First = 10, After = pagedList.EndCursor };
        var nextPagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            nextParams, 
            keySelector: e => e.Id);

        nextPagedList.Count.Should().Be(10);
        nextPagedList[0].Id.Should().Be(11);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Backward_ReturnsCorrectPage()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { Last = 10, Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("25") };
        var sql = "SELECT * FROM Entities WHERE Id < COALESCE(@Cursor, 999) ORDER BY Id DESC LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(15);
        pagedList[9].Id.Should().Be(24);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeTrue();
        
        var lastParams = new CursorPaginationParameters { Last = 10 };
        var sqlLast = "SELECT * FROM Entities ORDER BY Id DESC LIMIT @__Pagination_Limit__";
        var lastPagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sqlLast,
            lastParams,
            keySelector: e => e.Id);

        lastPagedList.Count.Should().Be(10);
        lastPagedList[0].Id.Should().Be(16);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_EmptyResult_ReturnsEmptyList()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10 };
        var sql = "SELECT * FROM Entities WHERE Id > 100 ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(0);
        pagedList.StartCursor.Should().BeNull();
        pagedList.EndCursor.Should().BeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_FirstAndLastNull_UsesForwardLogic()
    {
        using var connection = await GetConnectionAsync();
        // Both First and Last are null
        var parameters = new CursorPaginationParameters { };
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        pagedList.Should().NotBeNull();
        // Default page size is 10
        pagedList.Count.Should().Be(10);
        // If it used backward logic, it would return from the end, but since it's forward, it returns the first 10 items
        pagedList[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithPageSizeGreaterThanMax_CapsPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 50 };
        var sql = "SELECT * FROM Entities WHERE Id > COALESCE(@Cursor, 0) ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id,
            maxPageSize: 10);

        pagedList.Should().NotBeNull();
        // Since maxPageSize is 10, it should return 10 items, not 25.
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithStringKey_ReturnsCorrectPage()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 5 };
        var sql = "SELECT * FROM Entities ORDER BY Name LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, string>(
            sql, 
            parameters, 
            keySelector: e => e.Name,
            cursorDecoder: s => s);

        pagedList.Count.Should().Be(5);
        pagedList.StartCursor.Should().NotBeNull();
        pagedList.EndCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_InvalidCursor_ThrowsInvalidPaginationCursorException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10, After = "not-valid-base64!!!" };
        var sql = "SELECT * FROM Entities WHERE Id > COALESCE(@Cursor, 0) ORDER BY Id LIMIT @__Pagination_Limit__";

        var act = async () => await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*could not be decoded from Base64*");
    }

    [Fact]
    public async Task DbConnectionCursorExtensions_NullableKey_ReturnsNullCursor()
    {
        using var connection = await GetConnectionAsync();
        var parameters = new CursorPaginationParameters { First = 10 };
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int?>(
            sql, 
            parameters, 
            keySelector: _ => null);

        pagedList.StartCursor.Should().BeNull();
        pagedList.EndCursor.Should().BeNull();
    }


    [Fact]
    public async Task ToPagedListAsync_NullMaxPageSize_UsesPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false, maxPageSize: null);

        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_Works_WithNullMaxPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

        var enumerable = connection.ToPagedAsyncEnumerable<Entity>(sql, parameters, maxPageSize: null);

        var list = new List<Entity>();
        await foreach (var item in enumerable)
        {
            list.Add(item);
        }

        list.Should().HaveCount(10);
    }

    [Fact]
    public async Task ToPagedListAsync_ReservedKey_DynamicParameters_Throws()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities";
        var dp = new global::Dapper.DynamicParameters();
        dp.Add("__Pagination_Skip__", 1);

        var act = async () => await connection.ToPagedListAsync<Entity>(sql, parameters, param: dp);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contains the reserved pagination parameter name*");
    }

    [Fact]
    public async Task ToPagedListAsync_ReservedKey_Dictionary_Throws()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities";
        var dict = new Dictionary<string, object?> { { "__Pagination_Skip__", 1 } };

        var act = async () => await connection.ToPagedListAsync<Entity>(sql, parameters, param: dict);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contains the reserved pagination parameter name*");
    }

    [Fact]
    public async Task ToPagedListAsync_ReservedKey_AnonymousObject_Throws()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities";
        var poco = new { __Pagination_PageSize__ = 1 };

        var act = async () => await connection.ToPagedListAsync<Entity>(sql, parameters, param: poco);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contains the reserved pagination parameter name*");
    }
    [Fact]
    public async Task ToPagedListAsync_WithMaxPageSize_OverridesPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

        // maxPageSize = 5 should override parameters.PageSize = 10
        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false, maxPageSize: 5);

        // the query limits to 6 (effective + 1). If there are more than 5, it returns 5 and hasNextPage=true.
        pagedList.Count.Should().Be(5);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_WithMaxPageSize_OverridesPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

        var enumerable = connection.ToPagedAsyncEnumerable<Entity>(sql, parameters, maxPageSize: 5);

        var list = new List<Entity>();
        await foreach (var item in enumerable)
        {
            list.Add(item);
        }

        list.Should().HaveCount(5);
    }

    [Fact]
    public async Task ToPagedListAsync_ValidParam_DoesNotThrow()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities WHERE Name = @Name";
        var poco = new { Name = "Alice" };

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, param: poco, countTotal: false);

        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToPagedListAsync_ReservedKey_DictionaryNonNull_Throws()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities";
        var dict = new Dictionary<string, object> { { "__Pagination_Skip__", 1 } };

        var act = async () => await connection.ToPagedListAsync<Entity>(sql, parameters, param: dict);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contains the reserved pagination parameter name*");
    }

    [Fact]
    public async Task ToPagedListAsync_EmptyDictionary_DoesNotThrow()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities";
        var dict = new Dictionary<string, object>();

        var pagedList = await connection.ToPagedListAsync<Entity>(sql, parameters, param: dict, countTotal: false);

        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_NoRows_YieldsNothing()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities WHERE 1=0 ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

        var enumerable = connection.ToPagedAsyncEnumerable<Entity>(sql, parameters);

        var list = new List<Entity>();
        await foreach (var item in enumerable)
        {
            list.Add(item);
        }

        list.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryPagedListAsync_ExactPageSize_ReturnsHasNextPageFalse()
    {
        using var connection = await DapperTestHelper.GetConnectionAsync(10);
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        var paged = await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false);
        
        paged.HasNextPage.Should().BeFalse();
        paged.Count.Should().Be(10);
    }

    private class CustomOffsetFactory : IPagedListFactory
    {
        public bool WasCalled { get; set; }
        public IPagedList<T> CreatePagedList<T>(IReadOnlyList<T> items, long? totalCount, int page, int pageSize, bool? hasNextPage)
        {
            WasCalled = true;
            return DefaultPagedListFactory.Instance.CreatePagedList(items, totalCount, page, pageSize, hasNextPage);
        }
    }

    [Fact]
    public async Task ToPagedListAsync_CustomFactory_IsUsed()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(1, 10);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        var factory = new CustomOffsetFactory();
        
        await connection.ToPagedListAsync<Entity>(sql, parameters, countTotal: false, factory: factory);
        
        factory.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_WithReservedSkipParam_ThrowsInvalidOperationException()
    {
        var mockConn = NSubstitute.Substitute.For<IDbConnection>();
        var parameters = PaginationParameters.Create(1, 10);
        var param = new { __Pagination_Skip__ = 1 };

        var enumerable = mockConn.ToPagedAsyncEnumerable<Entity>("SELECT * FROM Entities", parameters, param: param);
        var act = async () =>
        {
            await foreach (var item in enumerable)
            {
                _ = item;
            }
        };
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*__Pagination_Skip__*");
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_WithReservedLimitParam_ThrowsInvalidOperationException()
    {
        var mockConn = NSubstitute.Substitute.For<IDbConnection>();
        var parameters = PaginationParameters.Create(1, 10);
        var param = new { __Pagination_Limit__ = 1 };

        var enumerable = mockConn.ToPagedAsyncEnumerable<Entity>("SELECT * FROM Entities", parameters, param: param);
        var act = async () =>
        {
            await foreach (var item in enumerable)
            {
                _ = item;
            }
        };
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*__Pagination_Limit__*");
    }
}





