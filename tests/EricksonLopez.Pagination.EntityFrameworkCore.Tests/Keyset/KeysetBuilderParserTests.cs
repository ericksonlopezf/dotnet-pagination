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

public class KeysetBuilderParserTests
{
    

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestEntity> Entities { get; set; } = null!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        }
    }

    private TestDbContext GetContext()
    {
        var context = new TestDbContext();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ParseCursor_DecodedCursorEmpty_ReturnsFirstPage()
    {
        using var ctx = GetContext();
        // A cursor that decodes to empty string
        var cursor = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("");
        var result = await ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithAfter(cursor).Build())
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();
        
        // It returns null, which means it starts from beginning without throwing
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ParseCursor_DecodedCursorTooLong_ThrowsInvalidPaginationCursorException()
    {
        using var ctx = GetContext();
        var longString = new string('a', 4097);
        var cursor = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode(longString);
        
        var query = ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithAfter(cursor).Build())
            .Ascending(e => e.Id);
            
        Func<Task> act = async () => await query.ToCursorPagedListAsync();
        await act.Should().ThrowAsync<InvalidPaginationCursorException>();
    }

    [Fact]
    public async Task ParseCursor_SingleColumnCursor_ThrowsInvalidPaginationCursorException()
    {
        using var ctx = GetContext();
        var cursor = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("S|123")!;
        
        var query = ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithAfter(cursor).Build())
            .Ascending(e => e.Id);
            
        Func<Task> act = async () => await query.ToCursorPagedListAsync();
        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*Expected a multi-column keyset cursor*");
    }

    [Fact]
    public async Task ParseCursor_LegacyFormatNotAccepted_ThrowsInvalidPaginationCursorException()
    {
        using var ctx = GetContext();
        var cursor = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("M|123")!;
        
        // Use reflection to construct KeysetBuilder with acceptLegacyCursors = false
        var parameters = new CursorPaginationParametersBuilder().WithAfter(cursor).Build();
        var builder = new KeysetBuilder<TestEntity>(ctx.Entities, parameters, 10, null, acceptLegacyCursors: false)
            .Ascending(e => e.Id);
            
        Func<Task> act = async () => await builder.ToCursorPagedListAsync();
        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*Legacy v1 cursor format is not accepted*");
    }

    [Fact]
    public async Task ParseCursor_InvalidFingerprint_ThrowsInvalidPaginationCursorException()
    {
        using var ctx = GetContext();
        // M|v2|wrongfingerprint|123
        var cursor = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("M|v2|WRONG|123");
        
        var query = ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithAfter(cursor).Build())
            .Ascending(e => e.Id);
            
        Func<Task> act = async () => await query.ToCursorPagedListAsync();
        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*Cursor was generated for a different keyset*");
    }

    [Fact]
    public async Task ParseCursor_InvalidPartFormat_ThrowsInvalidPaginationCursorException()
    {
        using var ctx = GetContext();
        
        // We need the correct fingerprint to bypass the fingerprint check.
        var query = ctx.Entities.Keyset(new CursorPaginationParameters()).Ascending(e => e.Id);
        // Fingerprint is hashed. Let's just create a valid cursor and modify the value
        await ctx.Entities.AddAsync(new TestEntity { Id = 1, Name = "A" });
        await ctx.SaveChangesAsync();
        var validResult = await query.ToCursorPagedListAsync();
        var validCursor = validResult.StartCursor!;
        
        var payload = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Decode(validCursor)!;
        var parts = payload.Split('|');
        parts[3] = "NotAnInteger";
        
        var badCursor = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode(string.Join("|", parts))!;
        
        var badQuery = ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithAfter(badCursor).Build())
            .Ascending(e => e.Id);
            
        Func<Task> act = async () => await badQuery.ToCursorPagedListAsync();
        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*Could not convert cursor part*");
    }
}





