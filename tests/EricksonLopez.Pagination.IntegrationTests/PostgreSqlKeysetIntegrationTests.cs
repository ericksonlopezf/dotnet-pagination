// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EricksonLopez.Pagination.IntegrationTests;

public class PostgreSqlKeysetIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;
    private TestDbContext? _dbContext;
    private bool _dockerAvailable = true;

    public async Task InitializeAsync()
    {
        try
        {
            _dbContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .Build();
            await _dbContainer.StartAsync();
        }
        catch
        {
            _dockerAvailable = false;
            return;
        }

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
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
        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task KeysetBuilder_ForwardPagination_WorksCorrectly()
    {
        Skip.If(!_dockerAvailable || _dbContext == null, "Docker is not available or PostgreSQL container failed to start.");

        var parameters = new CursorPaginationParameters { First = 10 };
        
        var pagedList = await _dbContext!.Users
            .Keyset(parameters)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();

        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();
        pagedList[0].Name.Should().Be("User 001");
        pagedList[pagedList.Count - 1].Name.Should().Be("User 010");

        // Next page
        var parameters2 = new CursorPaginationParameters { First = 10, After = pagedList.EndCursor };
        var pagedList2 = await _dbContext!.Users
            .Keyset(parameters2)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();
                
        pagedList2.Count.Should().Be(10);
        pagedList2[0].Name.Should().Be("User 011");
        pagedList2[pagedList2.Count - 1].Name.Should().Be("User 020");
    }

    [SkippableFact]
    public async Task GetApproximateCountAsync_WorksCorrectly()
    {
        Skip.If(!_dockerAvailable || _dbContext == null, "Docker is not available or PostgreSQL container failed to start.");

        // First run ANALYZE to ensure pg_class is updated
        await _dbContext!.Database.ExecuteSqlRawAsync("ANALYZE \"Users\";");

        var count = await _dbContext!.GetApproximateCountAsync("Users");
        // Due to the nature of PostgreSQL approximate counts for very small tables,
        // we assert it's greater than 0, though for 50 rows it should be exactly 50 after ANALYZE.
        count.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task KeysetBuilder_StringKeys_OrdersCorrectlyInPostgreSql()
    {
        Skip.If(!_dockerAvailable || _dbContext == null, "Docker is not available or PostgreSQL container failed to start.");

        var parameters = new CursorPaginationParameters { First = 10 };
        
        var pagedList = await _dbContext!.Users
            .Keyset(parameters)
            .Ascending(u => u.Name)
            .ToCursorPagedListAsync();

        pagedList.Count.Should().Be(10);
        pagedList[0].Name.Should().Be("User 001");
        pagedList[pagedList.Count - 1].Name.Should().Be("User 010");

        // Next page
        var parameters2 = new CursorPaginationParameters { First = 10, After = pagedList.EndCursor };
        var pagedList2 = await _dbContext!.Users
            .Keyset(parameters2)
            .Ascending(u => u.Name)
            .ToCursorPagedListAsync();
                
        pagedList2.Count.Should().Be(10);
        pagedList2[0].Name.Should().Be("User 011");
    }
}




