// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
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

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public partial class QueryableExtensionsTests
{
    

    private async Task<TestDbContext> GetDatabaseAsync() { return await TestDbContext.CreateInMemoryAsync(25); }

    [Fact]
    public async Task ToPagedListAsync_WithCount_ReturnsCorrectMetadata()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: true);

        paged.TotalCount.Should().Be(25);
        paged.TotalPages.Should().Be(3);
        paged.Count.Should().Be(10);
        paged[0].Id.Should().Be(11);
        paged.HasNextPage.Should().BeTrue();
        paged.HasPreviousPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToPagedListAsync_WithCount_EmptyResult_ReturnsEmptyList()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.Where(e => e.Id > 100).OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: true);

        paged.TotalCount.Should().Be(0);
        paged.Count.Should().Be(0);
        paged.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToPagedListAsync_WithoutCount_EvaluatesHasNextPageCorrectly()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 3, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: false);

        paged.TotalCount.Should().BeNull();
        paged.TotalPages.Should().BeNull();
        paged.Count.Should().Be(5);
        paged[0].Id.Should().Be(21);
        paged.HasNextPage.Should().BeFalse();
        paged.HasPreviousPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToPagedListAsync_WithoutCount_WithNextPage_ReturnsHasNextPageTrue()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(parameters, countTotal: false);

        paged.Count.Should().Be(10);
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelectorAndCount_ReturnsMappedList()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        var paged = await query.ToPagedListAsync(e => e.Name, parameters, countTotal: true);

        paged.TotalCount.Should().Be(25);
        paged.Count.Should().Be(10);
        paged[0].Should().Be("Entity 11");
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelectorAndCount_EmptyResult_ReturnsEmptyList()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.Where(e => e.Id > 100).OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(e => e.Name, parameters, countTotal: true);

        paged.TotalCount.Should().Be(0);
        paged.Count.Should().Be(0);
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelectorWithoutCount_ReturnsMappedList()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var paged = await query.ToPagedListAsync(e => e.Name, parameters, countTotal: false);

        paged.Count.Should().Be(10);
        paged[0].Should().Be("Entity 1");
        paged.HasNextPage.Should().BeTrue();
    }





    [Fact]
    public async Task ToPagedListBatchedAsync_YieldsBatches()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);

        var batches = new List<IPagedList<TestEntity>>();
        await foreach (var batch in query.ToPagedListBatchedAsync(batchSize: 7))
        {
            batches.Add(batch);
        }

        batches.Should().HaveCount(4); // 7, 7, 7, 4
        batches[0].Count.Should().Be(7);
        batches[1].Count.Should().Be(7);
        batches[2].Count.Should().Be(7);
        batches[3].Count.Should().Be(4);
        batches[3].HasNextPage.Should().BeFalse();
    }
    [Fact]
    public void ToPagedListBatchedAsync_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
    {
        var query = Enumerable.Empty<TestEntity>().AsQueryable();
        Action act = () => query.ToPagedListBatchedAsync(batchSize: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ToPagedListBatchedAsync_EmptyQueryable_YieldsZeroBatches()
    {
        var context = await GetDatabaseAsync();
        context.Entities.RemoveRange(context.Entities);
        await context.SaveChangesAsync();
        
        var query = context.Entities.OrderBy(e => e.Id);

        var batches = new List<IPagedList<TestEntity>>();
        await foreach (var batch in query.ToPagedListBatchedAsync(batchSize: 10))
        {
            batches.Add(batch);
        }

        batches.Should().BeEmpty();
    }





    [Fact]
    public async Task ToPagedListAsync_ApproximateCount_ThrowsNotSupportedOnSqlite()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.AsQueryable();
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        Func<Task> act = async () => await query.ToPagedListAsync(parameters, countTotal: true, useApproximateCount: true);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not supported for the current provider*");
    }

    [Fact]
    public async Task ApplySort_WithEmptySort_UsesDefaultSort()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.AsQueryable();
        
        var sortedQuery = query.ApplySort(SortParameters.Empty, defaultSort: e => e.Name);
        var firstItem = await sortedQuery.FirstAsync();
        
        firstItem.Name.Should().Be("Entity 1"); 
    }

    [Fact]
    public async Task ToPagedListBatchedAsync_Cancellation_StopsLoop()
    {
        var context = await GetDatabaseAsync();
        var query = context.Entities.OrderBy(e => e.Id);
        using var cts = new CancellationTokenSource();

        var batches = new List<IPagedList<TestEntity>>();
        
        Func<Task> act = async () => 
        {
            await foreach (var batch in query.ToPagedListBatchedAsync(batchSize: 10, cancellationToken: cts.Token))
            {
                batches.Add(batch);
                if (batches.Count == 2)
                {
                    await cts.CancelAsync();
                }
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        batches.Should().HaveCount(2);
    }

    private class NonComparableClass
    {
        public string Value { get; set; } = string.Empty;
    }

    private class NonComparableEntity
    {
        public int Id { get; set; }
        public NonComparableClass Key { get; set; } = new();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_NonComparableKey_ThrowsInvalidOperationException()
    {
        var data = new[]
        {
            new NonComparableEntity { Id = 1, Key = new NonComparableClass { Value = "A" } }
        }.AsQueryable();

        var paramsObj = new CursorPaginationParametersBuilder().WithFirst(5).Build();
        Func<Task> act = async () => await data.Keyset(paramsObj).Ascending(e => e.Key).ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ToPagedListAsync_WhenCancellationTokenCanceled_ThrowsOperationCanceledException()
    {
        var context = await GetDatabaseAsync();
        var canceledToken = new CancellationToken(canceled: true);

        var query = context.Entities.OrderBy(e => e.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        Func<Task> act = async () => await query.ToPagedListAsync(parameters, countTotal: true, cancellationToken: canceledToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}




