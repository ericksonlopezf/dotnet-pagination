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
using Testcontainers.MsSql;
using Xunit;

namespace EricksonLopez.Pagination.IntegrationTests;

public class SqlServerKeysetIntegrationTests : IAsyncLifetime
{
    private MsSqlContainer? _dbContainer;
    private TestDbContext? _dbContext;
    private bool _dockerAvailable = true;

    public async Task InitializeAsync()
    {
        try
        {
            _dbContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            await _dbContainer.StartAsync();
        }
        catch
        {
            _dockerAvailable = false;
            return;
        }

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(_dbContainer.GetConnectionString())
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
    public async Task KeysetBuilder_BackwardPagination_WorksCorrectly()
    {
        Skip.If(!_dockerAvailable || _dbContext == null, "Docker is not available or SQL Server container failed to start.");

        var parameters = new CursorPaginationParameters { Last = 10 };
        
        var pagedList = await _dbContext!.Users
            .Keyset(parameters)
            .Ascending(u => u.Id)
            .ToCursorPagedListAsync();

        pagedList.Count.Should().Be(10);
        pagedList.HasPreviousPage.Should().BeTrue();
        pagedList.HasNextPage.Should().BeFalse();
        pagedList[0].Name.Should().Be("User 041");
        pagedList[pagedList.Count - 1].Name.Should().Be("User 050");
    }
}




