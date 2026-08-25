// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class CompositeQueryableExtensionsTests
{
    private async Task<TestDbContext> GetDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        var context = new TestDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        // Generate data with duplicates in Name to test composite keys
        var entities = Enumerable.Range(1, 25).Select(i => new TestEntityBuilder().WithId(i).WithName($"Group {i % 5}").Build());
        await context.Entities.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Composite_ReturnsCorrectPage()
    {
        // Arrange
        using var context = await GetDatabaseAsync();
        var query = context.Entities.AsQueryable();
        var parameters = CursorPaginationParameters.Default;

        // Act - First Page
        var page1 = await query
            .Keyset(parameters)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();

        // Assert
        page1.Count.Should().Be(10);
        // It should order by Name, then Id
        // Group 0: Ids 5, 10, 15, 20, 25
        // Group 1: Ids 1, 6, 11, 16, 21
        page1[0].Name.Should().Be("Group 0");
        page1[0].Id.Should().Be(5);
        page1[^1].Name.Should().Be("Group 1");
        page1[^1].Id.Should().Be(21);
        page1.HasNextPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_Composite_WithAfterKey_ReturnsCorrectNextPage()
    {
        // Arrange
        using var context = await GetDatabaseAsync();
        var query = context.Entities.AsQueryable();
        var parameters = new CursorPaginationParametersBuilder().WithAfter(EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("Group 1|21")).Build();

        // Act - Second Page (after Group 1, Id 21)
        var page2 = await query
            .Keyset(parameters)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();

        // Assert
        page2.Count.Should().Be(10);
        // Next should be Group 2: Ids 2, 7, 12, 17, 22
        // Then Group 3: Ids 3, 8, 13, 18, 23
        page2[0].Name.Should().Be("Group 2");
        page2[0].Id.Should().Be(2);
        page2[^1].Name.Should().Be("Group 3");
        page2[^1].Id.Should().Be(23);
        page2.HasNextPage.Should().BeTrue();
    }
}



