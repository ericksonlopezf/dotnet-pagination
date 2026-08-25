// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
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

public class GridReaderPaginationExtensionsTests
{
    private async Task<SqliteConnection> GetConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE Entities (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            )");

        for (int i = 1; i <= 25; i++)
        {
            await connection.ExecuteAsync(
                "INSERT INTO Entities (Id, Name) VALUES (@Id, @Name)",
                new { Id = i, Name = $"Entity {i}" });
        }

        return connection;
    }

    [Fact]
    public async Task ReadPagedListAsync_WithNullGridReader_ThrowsArgumentNullException()
    {
        SqlMapper.GridReader multi = null!;
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var act = async () => await multi.ReadPagedListAsync<Entity>(parameters);
        
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadPagedListAsync_WithCountTotal_ReturnsCorrectPageAndCount()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);
        
        var sql = "SELECT COUNT(*) FROM Entities; SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: true);

        pagedList.Should().NotBeNull();
        pagedList.TotalCount.Should().Be(25);
        pagedList.Count.Should().Be(10);
        pagedList[0].Id.Should().Be(11);
    }
    
    [Fact]
    public async Task ReadPagedListAsync_WithCountTotal_EmptyResult_ReturnsEmptyPage()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var sql = "SELECT COUNT(*) FROM Entities WHERE Id > 100; SELECT * FROM Entities WHERE Id > 100 ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: true);

        pagedList.Should().NotBeNull();
        pagedList.TotalCount.Should().Be(0);
        pagedList.Count.Should().Be(0);
    }
    
    [Fact]
    public async Task ReadPagedListAsync_WithCountTotal_InvalidFirstResultSet_ThrowsInvalidOperationException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        // Return a string instead of a count
        var sql = "SELECT Name FROM Entities LIMIT 1; SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var act = async () => await multi.ReadPagedListAsync<Entity>(parameters, countTotal: true);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*COUNT(*)*");
    }

    [Fact]
    public async Task ReadPagedListAsync_WithoutCountTotal_ReturnsCorrectPage()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 3, pageSize: 10);
        
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize + 1, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.TotalCount.Should().BeNull();
        pagedList.Count.Should().Be(5);
        pagedList.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public async Task ReadPagedListAsync_WithoutCountTotal_WithNextPage_ReturnsHasNextPageTrue()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        // Take + 1
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize + 1, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ReadPagedListAsync_WithoutCountTotal_ExactPageSize_ReturnsHasNextPageFalse()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        // EXACTLY 10 items returned from query (because we only request 10, wait, the method queries PageSize + 1 usually? No, ReadPagedListAsync doesn't build the query, we just pass the grid reader).
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: false);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ReadPagedListAsync_WithNullMaxPageSize_ReturnsExpected()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        // maxPageSize = null
        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: false, maxPageSize: null);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(10);
    }

    [Fact]
    public async Task ReadPagedListAsync_WithPageSizeGreaterThanMax_CapsPageSize()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 50);
        
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: false, maxPageSize: 10);

        pagedList.Should().NotBeNull();
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ReadPagedListAsync_WithCustomFactory_UsesCustomFactory()
    {
        using var connection = await GetConnectionAsync();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);
        var factory = NSubstitute.Substitute.For<IPagedListFactory>();
        factory.CreatePagedList(Arg.Any<IReadOnlyList<Entity>>(), null, 1, 10, false)
               .Returns(new EricksonLopez.Pagination.PagedList<Entity>(Array.Empty<Entity>(), null, 1, 10, false));
        
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__;";
        
        using var multi = await connection.QueryMultipleAsync(sql, new { __Pagination_Limit__ = parameters.PageSize, __Pagination_Skip__ = (parameters.Page - 1) * parameters.PageSize });

        var pagedList = await multi.ReadPagedListAsync<Entity>(parameters, countTotal: false, factory: factory);

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(0); // Because factory returns empty array
        factory.ReceivedWithAnyArgs(1).CreatePagedList<Entity>(default!, null, 1, 10, false);
    }
}




