// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class AdditionalAdversarialTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        [SuppressMessage("SonarLint", "S1144:UnusedPrivateProperty", Justification = "Used via dynamic reflection in tests")]
        public string? Name { get; set; }
        public decimal DateOffset { get; set; }
        [SuppressMessage("SonarLint", "S1144:UnusedPrivateProperty", Justification = "Used via dynamic reflection in tests")]
        [SuppressMessage("SonarLint", "S3459:UnassignedField", Justification = "Used via dynamic reflection in tests")]
        public int? NullableInt { get; set; }
    }

    private static async Task<TestDbContext> GetDbContextAsync()
    {
        var context = new TestDbContext();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext() { }
        public DbSet<TestEntity> Entities { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().Property(e => e.Id).ValueGeneratedNever();
        }
    }

    [Fact]
    public async Task Ascending_Dynamic_ValidatesColumnName()
    {
        using var db = await GetDbContextAsync();
        var query = db.Entities.AsQueryable();
        
        // Mutant: ValidateColumnName missing or string mutated
        var act = () => query.Keyset(new CursorPaginationParameters()).Ascending("InvalidName", new[] { "Name" });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Ascending_Dynamic_MultipleColumns_AppendsColumnsSequentially()
    {
        using var db = await GetDbContextAsync();
        await db.Entities.AddAsync(new TestEntity { Id = 1, DateOffset = 100m });
        await db.Entities.AddAsync(new TestEntity { Id = 2, DateOffset = 50m });
        await db.SaveChangesAsync();

        var pagedList = await db.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(10).Build())
            .Ascending("DateOffset")
            .Ascending("Id")
            .ToCursorPagedListAsync();

        pagedList.Count.Should().Be(2);
        pagedList[0].Id.Should().Be(2);
        pagedList[1].Id.Should().Be(1);
    }

    [Fact]
    public async Task Descending_Dynamic_ValidatesColumnName()
    {
        using var db = await GetDbContextAsync();
        var query = db.Entities.AsQueryable();
        
        var act = () => query.Keyset(new CursorPaginationParameters()).Descending("InvalidName", new[] { "Name" });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Descending_Dynamic_MultipleColumns_AppendsColumnsSequentially()
    {
        using var db = await GetDbContextAsync();
        await db.Entities.AddAsync(new TestEntity { Id = 1, DateOffset = 50m });
        await db.Entities.AddAsync(new TestEntity { Id = 2, DateOffset = 100m });
        await db.SaveChangesAsync();

        var pagedList = await db.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(10).Build())
            .Descending("DateOffset")
            .Descending("Id")
            .ToCursorPagedListAsync();

        pagedList.Count.Should().Be(2);
        pagedList[0].Id.Should().Be(2);
        pagedList[1].Id.Should().Be(1);
    }

    [Fact]
    public async Task ThenAscending_Dynamic_ValidatesColumnName()
    {
        using var db = await GetDbContextAsync();
        var query = db.Entities.AsQueryable();
        
        var act = () => query.Keyset(new CursorPaginationParameters()).Ascending("Id").Ascending("InvalidName", new[] { "Name" });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ThenDescending_Dynamic_ValidatesColumnName()
    {
        using var db = await GetDbContextAsync();
        var query = db.Entities.AsQueryable();
        
        var act = () => query.Keyset(new CursorPaginationParameters()).Ascending("Id").Descending("InvalidName", new[] { "Name" });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithLast_Pagination()
    {
        using var db = await GetDbContextAsync();
        await db.Entities.AddAsync(new TestEntity { Id = 1 });
        await db.Entities.AddAsync(new TestEntity { Id = 2 });
        await db.SaveChangesAsync();

        var parameters = new CursorPaginationParametersBuilder().WithLast(1).Build();
        var result = await db.Entities.AsQueryable().Keyset(parameters)
            .Ascending(x => x.Id)
            .ToCursorPagedListAsync();

        result.Count.Should().Be(1);
        result[0].Id.Should().Be(2); // Last 1 should be Id 2
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenTotalCountEqualsPageSize_ReportsHasNextPageFalse()
    {
        using var db = await GetDbContextAsync();
        for(int i = 0; i < 10; i++) await db.Entities.AddAsync(new TestEntity { Id = i });
        await db.SaveChangesAsync();

        var parameters = new CursorPaginationParametersBuilder().WithFirst(10).Build();
        var result = await db.Entities.AsQueryable().Keyset(parameters)
            .Ascending(x => x.Id)
            .ToCursorPagedListAsync();

        result.HasNextPage.Should().BeFalse();
        result.Count.Should().Be(10);
    }

    [Fact]
    public async Task ParseCursor_LengthExactly4096_ShouldNotThrow()
    {
        using var db = await GetDbContextAsync();
        var validOpaqueCursor = new string('a', 4096);
        // We expect it to fail decoding because it's junk, but NOT throw length exception
        var parameters = new CursorPaginationParametersBuilder().WithFirst(10).WithAfter(validOpaqueCursor).Build();
        
        var act = async () => await db.Entities.AsQueryable().Keyset(parameters)
            .Ascending(x => x.Id)
            .ToCursorPagedListAsync();

        // It should throw an exception during parsing, but NOT the 4096 length exception
        var ex = await act.Should().ThrowAsync<Exception>();
        ex.WithMessage("*").Where(e => !e.Message.Contains("exceeds maximum allowed length"));
    }

    [Fact]
    public async Task ParseCursor_Length4097_ShouldThrowLengthException()
    {
        using var db = await GetDbContextAsync();
        var tooLongCursor = new string('a', 4097);
        var parameters = new CursorPaginationParametersBuilder().WithFirst(10).WithAfter(tooLongCursor).Build();
        
        var act = async () => await db.Entities.AsQueryable().Keyset(parameters)
            .Ascending(x => x.Id)
            .ToCursorPagedListAsync();

        (await act.Should().ThrowAsync<InvalidPaginationCursorException>())
            .WithMessage("*Cursor exceeds maximum allowed length of 4096 characters.*");
    }

    [Fact]
    public async Task MultiColumn_ParseCursor_Length4097_ShouldThrowLengthException()
    {
        using var db = await GetDbContextAsync();
        var tooLongCursor = new string('a', 4097);
        var parameters = new CursorPaginationParametersBuilder().WithFirst(10).WithAfter(tooLongCursor).Build();
        
        var act = async () => await db.Entities.AsQueryable().Keyset(parameters)
            .Ascending(x => x.DateOffset)
            .Ascending(x => x.Id)
            .ToCursorPagedListAsync();

        (await act.Should().ThrowAsync<InvalidPaginationCursorException>())
            .WithMessage("*Cursor exceeds maximum allowed length of 4096 characters.*");
    }

    [Fact]
    public async Task KeysetBuilder_EmptyQueryable_ToCursorPagedListAsync_ReturnsEmptyPagedList()
    {
        using var db = await GetDbContextAsync();
        var result = await db.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(10).Build())
            .Ascending(x => x.Id)
            .ToCursorPagedListAsync();

        result.Count.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().BeNull();
        result.EndCursor.Should().BeNull();
    }


    [Fact]
    public async Task DeterminePropertyType_Decimal()
    {
        using var db = await GetDbContextAsync();
        await db.Entities.AddAsync(new TestEntity { Id = 1, DateOffset = 10m });
        await db.SaveChangesAsync();

        var parameters = new CursorPaginationParametersBuilder().WithFirst(10).Build();
        var result = await db.Entities.AsQueryable().Keyset(parameters)
            .Ascending(x => x.DateOffset)
            .ToCursorPagedListAsync();
            
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task KeysetBuilder_Dynamic_NullProperty_Throws()
    {
        using var db = await GetDbContextAsync();
        await db.Entities.AddAsync(new TestEntity { Id = 1 });
        await db.SaveChangesAsync();

        var act = () => db.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(10).Build())
            .Ascending("NullableInt");
            
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Keyset pagination on nullable property*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_MultipleColumnsMixedDirections_PaginatesForwardAndBackwardCorrectly()
    {
        using var db = await GetDbContextAsync();
        await db.Entities.AddAsync(new TestEntity { Id = 1, DateOffset = 10m });
        await db.Entities.AddAsync(new TestEntity { Id = 2, DateOffset = 10m });
        await db.Entities.AddAsync(new TestEntity { Id = 3, DateOffset = 20m });
        await db.SaveChangesAsync();

        var builder = db.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(2).Build())
            .Ascending(x => x.DateOffset)
            .Descending(x => x.Id);
            
        var page1 = await builder.ToCursorPagedListAsync();
        page1.Count.Should().Be(2);
        
        var builder2 = db.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(2).WithAfter(page1.EndCursor).Build())
            .Ascending(x => x.DateOffset)
            .Descending(x => x.Id);
            
        var page2 = await builder2.ToCursorPagedListAsync();
        page2.Count.Should().Be(1);
        
        var builder3 = db.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithLast(2).WithBefore(page2.StartCursor).Build())
            .Ascending(x => x.DateOffset)
            .Descending(x => x.Id);
            
        var page3 = await builder3.ToCursorPagedListAsync();
        page3.Count.Should().Be(2);
    }
}





