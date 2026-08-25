// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class KeysetPartitioningTests
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
    public async Task SplitKeysetPartitionsAsync_NullGuards()
    {
        using var context = GetContext(10);
        IQueryable<TestEntity> query = context.Entities;

        var act1 = () => KeysetPartitioningExtensions.SplitKeysetPartitionsAsync<TestEntity>(null!, x => x.Id, 4);
        var act2 = () => query.SplitKeysetPartitionsAsync((System.Linq.Expressions.Expression<Func<TestEntity, int>>)null!, 4);
        var act3 = () => query.SplitKeysetPartitionsAsync(x => x.Id, 0);

        await act1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("source");
        await act2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("keySelector");
        await act3.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Partition count must be greater than zero.*");
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_EmptySource_ReturnsEmptyList()
    {
        using var context = GetContext(0);

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 4);

        partitions.Should().BeEmpty();
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Splits100EntitiesInto4Partitions()
    {
        using var context = GetContext(100);

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 4);

        partitions.Should().HaveCount(4);
        partitions[0].LowerBound.Should().Be(1);
        partitions[0].UpperBound.Should().Be(25);
        partitions[3].LowerBound.Should().Be(76);
        partitions[3].UpperBound.Should().Be(100);

        // Every partition has valid non-empty signed cursors
        foreach (var p in partitions)
        {
            p.StartCursor.Should().NotBeNullOrWhiteSpace();
            p.EndCursor.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_SinglePartitionRequested_ReturnsEntireRange()
    {
        using var context = GetContext(50);

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 1);

        partitions.Should().HaveCount(1);
        partitions[0].LowerBound.Should().Be(1);
        partitions[0].UpperBound.Should().Be(50);
    }

    private class LongEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class LongDbContext : DbContext
    {
        public DbSet<LongEntity> LongEntities { get; set; } = null!;
        public LongDbContext(DbContextOptions<LongDbContext> options) : base(options) { }
    }

    private LongDbContext GetLongContext(int entityCount)
    {
        var options = new DbContextOptionsBuilder<LongDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
        var context = new LongDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        for (long i = 1; i <= entityCount; i++)
        {
            context.LongEntities.Add(new LongEntity { Id = i * 1000L, Name = $"Entity {i}" });
        }
        context.SaveChanges();
        return context;
    }

    private LongDbContext GetLongContextContinuous(int entityCount)
    {
        var options = new DbContextOptionsBuilder<LongDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
        var context = new LongDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        for (long i = 1; i <= entityCount; i++)
        {
            context.LongEntities.Add(new LongEntity { Id = i, Name = $"Entity {i}" });
        }
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_NullGuards()
    {
        using var context = GetLongContext(10);
        IQueryable<LongEntity> query = context.LongEntities;

        var act1 = () => KeysetPartitioningExtensions.SplitKeysetPartitionsAsync<LongEntity>(null!, x => x.Id, 4);
        var act2 = () => query.SplitKeysetPartitionsAsync((System.Linq.Expressions.Expression<Func<LongEntity, long>>)null!, 4);
        var act3 = () => query.SplitKeysetPartitionsAsync(x => x.Id, 0);

        await act1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("source");
        await act2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("keySelector");
        await act3.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Partition count must be greater than zero.*");
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_EmptySource_ReturnsEmptyList()
    {
        using var context = GetLongContext(0);

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 4);

        partitions.Should().BeEmpty();
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_SplitsIntoPartitions()
    {
        using var context = GetLongContext(100);

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 4);

        partitions.Should().HaveCount(4);
        partitions[0].LowerBound.Should().Be(1000L);
        partitions[3].UpperBound.Should().Be(100000L);

        foreach (var p in partitions)
        {
            p.StartCursor.Should().NotBeNullOrWhiteSpace();
            p.EndCursor.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_SinglePartition_ReturnsEntireRange()
    {
        using var context = GetLongContext(50);

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 1);

        partitions.Should().HaveCount(1);
        partitions[0].LowerBound.Should().Be(1000L);
        partitions[0].UpperBound.Should().Be(50000L);
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_SingleEntity_ReturnsSinglePartition()
    {
        using var context = GetContext(1);

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 4);

        partitions.Should().HaveCount(1);
        partitions[0].LowerBound.Should().Be(1);
        partitions[0].UpperBound.Should().Be(1);
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_SingleEntity_ReturnsSinglePartition()
    {
        using var context = GetLongContext(1);

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 4);

        partitions.Should().HaveCount(1);
        partitions[0].LowerBound.Should().Be(1000L);
        partitions[0].UpperBound.Should().Be(1000L);
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_PartitionCountGreaterThanRange_BreaksEarly()
    {
        using var context = GetContext(3);

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 100);

        partitions.Count.Should().BeLessThanOrEqualTo(3);
        partitions[0].LowerBound.Should().Be(1);
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_PartitionCountGreaterThanRange_BreaksEarly()
    {
        using var context = GetLongContext(3);

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 10000);

        partitions.Count.Should().BeLessThanOrEqualTo(3000);
        partitions[0].LowerBound.Should().Be(1000L);
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_CustomEncoder_EncodesCorrectly()
    {
        using var context = GetContext(10);
        var encoder = new HmacCursorEncoder("custom-secret-key-that-is-at-least-32-chars-long!");

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 2, encoder);

        partitions.Should().HaveCount(2);
        encoder.Decode(partitions[0].StartCursor!).Should().Be("1");
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_CustomEncoder_EncodesCorrectly()
    {
        using var context = GetLongContext(10);
        var encoder = new HmacCursorEncoder("custom-secret-key-that-is-at-least-32-chars-long!");

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 2, encoder);

        partitions.Should().HaveCount(2);
        encoder.Decode(partitions[0].StartCursor!).Should().Be("1000");
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_ExactPartitionsEqualToRange_EmitsAllPartitions()
    {
        using var context = GetContext(10);

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 10);

        partitions.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            partitions[i].PartitionIndex.Should().Be(i);
            partitions[i].LowerBound.Should().Be(i + 1);
            partitions[i].UpperBound.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_ExactPartitionsEqualToRange_EmitsAllPartitions()
    {
        using var context = GetLongContextContinuous(10);

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 10);

        partitions.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            partitions[i].PartitionIndex.Should().Be(i);
            partitions[i].LowerBound.Should().Be(i + 1);
            partitions[i].UpperBound.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_StepMathWithOddDivision()
    {
        using var context = GetContext(10);

        var partitions = await context.Entities.SplitKeysetPartitionsAsync(x => x.Id, 3);

        partitions.Should().HaveCount(3);
        partitions[0].LowerBound.Should().Be(1);
        partitions[0].UpperBound.Should().Be(4);
        partitions[1].LowerBound.Should().Be(5);
        partitions[1].UpperBound.Should().Be(8);
        partitions[2].LowerBound.Should().Be(9);
        partitions[2].UpperBound.Should().Be(10);
    }

    [Fact]
    public async Task SplitKeysetPartitionsAsync_Long_StepMathWithOddDivision()
    {
        using var context = GetLongContextContinuous(10);

        var partitions = await context.LongEntities.SplitKeysetPartitionsAsync(x => x.Id, 3);

        partitions.Should().HaveCount(3);
        partitions[0].LowerBound.Should().Be(1L);
        partitions[0].UpperBound.Should().Be(4L);
        partitions[1].LowerBound.Should().Be(5L);
        partitions[1].UpperBound.Should().Be(8L);
        partitions[2].LowerBound.Should().Be(9L);
        partitions[2].UpperBound.Should().Be(10L);
    }

    [Fact]
    public async Task PartitionByKeysetAsync_IntAndLong_DelegatesToSplitKeysetPartitionsAsync()
    {
        using var contextInt = GetContext(10);
        var partitionsInt = await contextInt.Entities.PartitionByKeysetAsync(x => x.Id, 3);
        partitionsInt.Should().HaveCount(3);
        partitionsInt[0].LowerBound.Should().Be(1);

        using var contextLong = GetLongContextContinuous(10);
        var partitionsLong = await contextLong.LongEntities.PartitionByKeysetAsync(x => x.Id, 3);
        partitionsLong.Should().HaveCount(3);
        partitionsLong[0].LowerBound.Should().Be(1L);
    }
}



