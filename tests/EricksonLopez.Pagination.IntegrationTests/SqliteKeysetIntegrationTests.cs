// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.IntegrationTests;

public class SqliteKeysetIntegrationTests : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private TestDbContext? _dbContext;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed data
        var users = new List<User>();
        for (int i = 1; i <= 50; i++)
        {
            users.Add(new User
            {
                Name = $"User {i:D3}",
                Age = 20 + (i % 10),
                IsActive = i % 2 == 0
            });
        }
        _dbContext.Users.AddRange(users);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task KeysetBuilder_MultiColumnSort_WorksCorrectly()
    {
        var parameters = new CursorPaginationParameters { First = 10 };
        
        var pagedList = await _dbContext!.Users
            .Keyset(parameters)
            .Ascending(u => u.Age)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();

        pagedList.Count.Should().Be(10);
        
        // Expected sort: Age ASC, Id ASC
        // Lowest age is 20, for i=10, 20, 30, 40, 50
        // Next age is 21, for i=1, 11, 21, 31, 41
        var expectedIds = new[] { 10, 20, 30, 40, 50, 1, 11, 21, 31, 41 };
        var actualIds = pagedList.Select(x => x.Id).ToArray();
        
        actualIds.Should().Equal(expectedIds);
    }
}




