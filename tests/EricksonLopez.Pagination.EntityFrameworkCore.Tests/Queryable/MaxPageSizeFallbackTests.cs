// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class MaxPageSizeFallbackTests
{
    

    
    private static TestDbContext GetContext(int entityCount = 0) => TestDbContext.CreateInMemory(entityCount);

    
    
#pragma warning restore S1172

[Fact]
    public void ApplySort_DescendingFallback_SortsCorrectly()
    {
        var data = new List<TypeEntity>
        {
            new TypeEntityBuilder().WithId(1).WithString("A").Build(),
            new TypeEntityBuilder().WithId(2).WithString("B").Build()
        }.AsQueryable();

        // Testing the ternary fallback in ApplySort: (false ? OrderBy : OrderByDescending)
        // using ascending = false should trigger the OrderByDescending branch
        var parameters = new SortParameters { Value = null };
        var sorted = data.ApplySort(parameters, direction: SortDirection.Descending, defaultSort: x => x.Id);
        sorted.First().Id.Should().Be(2);
    }

[Fact]
    public async Task QueryableExtensions_MaxFilterComplexity_And_Length_Fallback()
    {
        var ctx = GetContext();
        
        
        var query = ctx.Set<TypeEntity>().AsQueryable();
        var filter = new FilterParameters { Value = "Id=1" };
        var options = new CustomPaginationOptions { MaxFilterComplexity = 10, MaxFilterValueLength = 10, MaxFilterStringLength = 10 };
        
        var act1 = async () => await query.ToPagedListAsync(filter, default(SortParameters), new PaginationParametersBuilder().WithPageSize(10).Build(), countTotal: true, maxPageSize: null, options: options);
        await act1.Should().NotThrowAsync();
    }

[Fact]
    public async Task QueryableExtensions_MaxPageSize_Fallback()
    {
        var ctx = GetContext();
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // No explicit maxPageSize passed, it should use options.MaxPageSize
        var options = new CustomPaginationOptions { MaxPageSize = 1 };
        var paged = await query.ToPagedListAsync(e => e.Id, new PaginationParametersBuilder().WithPageSize(10).Build(), options: options);
        
        paged.Count.Should().Be(1);
    }

[Fact]
    public async Task QueryableExtensions_OffsetLargeSkip_Threshold_Fallback()
    {
        var ctx = GetContext();
        
        
        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // Threshold is 100, we skip 101. This logs a warning but DOES NOT throw.
        var act = async () => await query.ToPagedListAsync(new PaginationParametersBuilder().WithPage(12).WithPageSize(10).Build(), options: null);
        await act.Should().NotThrowAsync();
    }

[Fact]
    public async Task QueryableExtensions_ApproximateCount_Zero_Fallback()
    {
        // FIX-14: useApproximateCount on a non-Postgres provider now throws ArgumentException.
        // The zero-fallback behavior (approxCount <= 0 -> LongCountAsync) is covered in
        // QueryableExtensionsApproximateCountMockTests with a mocked Postgres provider.
        var ctx = GetContext();
        
        
        var act = async () => await ctx.Set<TypeEntity>().AsQueryable()
            .ToPagedListAsync(new PaginationParametersBuilder().WithPage(1).Build(), countTotal: true, useApproximateCount: true);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*useApproximateCount*");
    }

    [Fact]
    public async Task KeysetBuilder_WithGuidColumn_PaginatesCorrectly()
    {
        using var context = GetContext();
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        await context.Entities.AddRangeAsync(
            new TestEntityBuilder().WithId(1).WithGuid(g1).Build(),
            new TestEntityBuilder().WithId(2).WithGuid(g2).Build());
        await context.SaveChangesAsync();

        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Ascending(x => x.GuidValue)
            .Ascending(x => x.Id);

        var page = await builder.ToCursorPagedListAsync();
        page.Count.Should().Be(1);
    }

    [Fact]
    public async Task KeysetBuilder_WithCustomComparableStruct_PaginatesCorrectly()
    {
        using var context = GetContext();
        await context.Entities.AddRangeAsync(
            new TestEntityBuilder().WithId(1).WithCustomStruct(10).Build(),
            new TestEntityBuilder().WithId(2).WithCustomStruct(20).Build());
        await context.SaveChangesAsync();

        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Ascending(x => x.CustomStructValue)
            .Ascending(x => x.Id);

        var page = await builder.ToCursorPagedListAsync();
        page.Count.Should().Be(1);
        page[0].Id.Should().Be(1);
    }

[Fact]
    public async Task CompileStringAccessor_FallbackToString()
    {
        using var context = GetContext();
        await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithCustomStruct(1).Build());
        await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithCustomStruct(2).Build());
        await context.SaveChangesAsync();

        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParameters(), acceptLegacyCursors: true)
            .Ascending(x => x.CustomStructValue);
            
        var result = await builder.ToCursorPagedListAsync();
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.StartCursor.Should().NotBeNull();
        result.EndCursor.Should().NotBeNull();
    }
}





