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
using Xunit;
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class QueryableOptimizedExtensionsTests
{
    private async Task<TestDbContext> GetDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        var context = new TestDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var entities = Enumerable.Range(1, 100).Select(i => new TestEntityBuilder().WithId(i).WithName($"Item {i}").Build());
        await context.Entities.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task ToPagedListDeferredAsync_WithCount_ReturnsCorrectMetadata()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: true);

        paged.TotalCount.Should().Be(100);
        paged.TotalPages.Should().Be(10);
        paged.Count.Should().Be(10);
        paged[0].Id.Should().Be(11);
        paged.HasNextPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToPagedListDeferredAsync_WithCount_EmptyResult_ReturnsEmptyList()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.Where(e => e.Id > 1000).OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: true);

        paged.TotalCount.Should().Be(0);
        paged.Count.Should().Be(0);
        paged.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public async Task ToPagedListDeferredAsync_WithCount_PageOutOfRange_ReturnsEmptyList()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 15, pageSize: 10);

        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: true);

        paged.TotalCount.Should().Be(100);
        paged.Count.Should().Be(0);
        paged.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToPagedListDeferredAsync_WithoutCount_EvaluatesHasNextPageCorrectly()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 10, pageSize: 10);

        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: false);

        paged.TotalCount.Should().BeNull();
        paged.Count.Should().Be(10);
        paged[0].Id.Should().Be(91);
        paged.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public async Task ToPagedListDeferredAsync_WithoutCount_WithNextPage_ReturnsHasNextPageTrue()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: false);

        paged.Count.Should().Be(10);
        paged.HasNextPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToPagedListDeferredAsync_WithoutCount_EmptyResult_ReturnsEmptyList()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.Where(e => e.Id > 1000).OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: false);

        paged.Count.Should().Be(0);
        paged.HasNextPage.Should().BeFalse();
    }
    
    [Fact]
    public async Task ToPagedListDeferredAsync_LargePageSize_FallsBackToStandardPagination()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 100);

        // Effective page size > 50 should fallback to standard pagination (ToPagedListAsync)
        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: true);

        paged.Count.Should().Be(100);
        paged.TotalCount.Should().Be(100);
    }
    
    [Fact]
    public async Task ToPagedListDeferredAsync_WithMaxPageSizeApplied_UsesEffectivePageSize()
    {
        using var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 40);

        // With maxPageSize=20, it should clamp to 20. 
        // 20 is not > 50 so it uses deferred.
        var paged = await query.ToPagedListDeferredAsync(e => e.Id, parameters, countTotal: true, maxPageSize: 20);

        paged.Count.Should().Be(20);
        paged.TotalCount.Should().Be(100);
    }
}




