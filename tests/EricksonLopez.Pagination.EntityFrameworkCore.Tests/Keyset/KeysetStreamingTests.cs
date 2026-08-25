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

public class KeysetStreamingTests
{
    

    private TestDbContext GetContext(int entityCount)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        for (int i = 1; i <= entityCount; i++)
        {
            context.Entities.Add(new TestEntityBuilder().WithId(i).WithName($"Entity {i}").Build());
        }
        context.SaveChanges();
        return context;
    }

    [Fact]
    public void AsKeysetStreamAsync_NullGuards()
    {
        using var context = GetContext(1);
        IQueryable<TestEntity> query = context.Entities;

        var act1 = () => KeysetStreamingExtensions.AsKeysetStreamAsync<TestEntity, int>(null!, x => x.Id);
        var act2 = () => query.AsKeysetStreamAsync<TestEntity, int>(null!);
        var act3 = () => query.AsKeysetStreamAsync(x => x.Id, batchSize: 0);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Batch size must be greater than zero.*");
    }

    [Fact]
    public async Task AsKeysetStreamAsync_EmptySource_YieldsZeroItems()
    {
        using var context = GetContext(0);

        var list = new List<TestEntity>();
        await foreach (var item in context.Entities.AsKeysetStreamAsync(x => x.Id, batchSize: 5))
        {
            list.Add(item);
        }

        list.Should().BeEmpty();
    }

    [Fact]
    public async Task AsKeysetStreamAsync_StreamsAcrossBatchBoundaries_Ascending()
    {
        using var context = GetContext(25);

        var list = new List<TestEntity>();
        await foreach (var item in context.Entities.AsKeysetStreamAsync(x => x.Id, SortDirection.Ascending, batchSize: 10))
        {
            list.Add(item);
        }

        list.Should().HaveCount(25);
        list.Select(x => x.Id).Should().ContainInOrder(Enumerable.Range(1, 25));
    }

    [Fact]
    public async Task AsKeysetStreamAsync_StreamsAcrossBatchBoundaries_Descending()
    {
        using var context = GetContext(25);

        var list = new List<TestEntity>();
        await foreach (var item in context.Entities.AsKeysetStreamAsync(x => x.Id, SortDirection.Descending, batchSize: 10))
        {
            list.Add(item);
        }

        list.Should().HaveCount(25);
        list.Select(x => x.Id).Should().ContainInOrder(Enumerable.Range(1, 25).Reverse());
    }

    [Fact]
    public async Task AsKeysetStreamAsync_WithCancellation_TerminatesEarly()
    {
        using var context = GetContext(100);
        using var cts = new CancellationTokenSource();

        var list = new List<TestEntity>();
        await foreach (var item in context.Entities.AsKeysetStreamAsync(x => x.Id, batchSize: 5, cancellationToken: cts.Token))
        {
            list.Add(item);
            if (list.Count == 12)
            {
#if NET8_0_OR_GREATER
                await cts.CancelAsync();
#else
                cts.Cancel();
#endif
            }
        }

        list.Count.Should().BeLessThan(100);
    }

    [Fact]
    public async Task AsKeysetStreamAsync_BatchSizeEqualsTotalCount_TerminatesAfterOneBatch()
    {
        using var context = GetContext(25);

        var list = new List<TestEntity>();
        await foreach (var item in context.Entities.AsKeysetStreamAsync(x => x.Id, batchSize: 25))
        {
            list.Add(item);
        }

        list.Should().HaveCount(25);
    }
}



