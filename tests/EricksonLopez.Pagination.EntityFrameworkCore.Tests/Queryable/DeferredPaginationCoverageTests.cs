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

public class DeferredPaginationCoverageTests
{
    

    
    private static TestDbContext GetContext(int entityCount = 0) => TestDbContext.CreateInMemory(entityCount);

    
    
#pragma warning restore S1172

[Fact]
    public async Task QueryableOptimizedExtensions_ToPagedListDeferredAsync_WorksWithCount()
    {
        var ctx = GetContext();
        
        
        await ctx.Entities.AddRangeAsync(new TestEntityBuilder().WithId(1).Build(), new TestEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Entities.OrderBy(e => e.Id);
        var result = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build(), countTotal: true);
        
        result.Count.Should().Be(2);
        result.TotalCount.Should().Be(2);
    }

[Fact]
    public async Task QueryableOptimizedExtensions_ToPagedListDeferredAsync_WorksWithoutCount()
    {
        var ctx = GetContext();
        
        
        await ctx.Entities.AddRangeAsync(new TestEntityBuilder().WithId(1).Build(), new TestEntityBuilder().WithId(2).Build(), new TestEntityBuilder().WithId(3).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Entities.OrderBy(e => e.Id);
        var result = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(2).Build(), countTotal: false);
        
        result.Count.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

[Fact]
    public async Task QueryableOptimizedExtensions_ToPagedListDeferredAsync_Empty()
    {
        var ctx = GetContext();
        
        

        var query = ctx.Entities.OrderBy(e => e.Id);
        var result = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(2).Build(), countTotal: true);
        result.Count.Should().Be(0);
        
        var resultNoCount = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(2).Build(), countTotal: false);
        resultNoCount.Count.Should().Be(0);
    }

[Fact]
    public async Task QueryableOptimizedExtensions_ToPagedListDeferredAsync_WithMaxPageSize()
    {
        var ctx = GetContext();
        
        
        await ctx.Entities.AddRangeAsync(new TestEntityBuilder().WithId(1).Build(), new TestEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Entities.OrderBy(e => e.Id);
        
        var result = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build(), countTotal: true, maxPageSize: 1);
        result.Count.Should().Be(1);
        
        var resultNoCount = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build(), countTotal: false, maxPageSize: 1);
        resultNoCount.Count.Should().Be(1);
    }

[Fact]
    public async Task QueryableOptimizedExtensions_ToPagedListDeferredAsync_MaxPageSize_Exceed()
    {
        var ctx = GetContext();
        
        
        await ctx.Entities.AddRangeAsync(new TestEntityBuilder().WithId(1).Build(), new TestEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Entities.OrderBy(e => e.Id);
        var result = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build(), countTotal: true, maxPageSize: 1);
        
        result.Count.Should().Be(1);
    }

[Fact]
    public async Task QueryableOptimizedExtensions_ToPagedListDeferredAsync_ExceedsDeferredMaxPageSize_Fallback()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // This will fall back to normal ToPagedListAsync internally because pageSize > deferredMaxPageSize (1)
        var paged = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPageSize(10).Build(), deferredMaxPageSize: 1);
        
        paged.Count.Should().Be(2);
    }

[Fact]
    public async Task QueryableExtensions_DeferredPagination_ThresholdAndPageSizeMutants()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddRangeAsync(new TypeEntityBuilder().WithId(1).Build(), new TypeEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();

        // effectivePageSize < originalPageSize (Math.Min(10, 1) = 1)
        var paged1 = await query.ToPagedListDeferredAsync(e => e.Id, new PaginationParametersBuilder().WithPageSize(10).Build(), maxPageSize: 1);
        paged1.Count.Should().Be(1);

        // threshold > 0 && skipAmount >= threshold
        var paged2 = await query.ToPagedListAsync(new PaginationParametersBuilder().WithPage(2).WithPageSize(1).Build(), options: new PaginationCoreOptions { DeepOffsetWarningThreshold = 1 });
        paged2.Count.Should().Be(1);
    }
}





