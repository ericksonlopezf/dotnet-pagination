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

public class QueryableExtensionsCoverageTests
{
    

    
    private static TestDbContext GetContext(int entityCount = 0) => TestDbContext.CreateInMemory(entityCount);

    
    
#pragma warning restore S1172

[Fact]
    public void QueryableExtensions_ApplySort_MultipleColumns()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(2).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(3).WithName("Bob").Build()
        }.AsQueryable();

        var sorted = data.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name desc, Id asc" }, SortDirection.Ascending, e => e.Id);
        var result = sorted.ToList();
        result[0].Id.Should().Be(3); // Bob
        result[1].Id.Should().Be(1); // Alice Id 1
        result[2].Id.Should().Be(2); // Alice Id 2
    }

[Fact]
    public void QueryableExtensions_ApplySort_WithDefaultSort()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(2).WithName("Bob").Build(),
            new TestEntityBuilder().WithId(1).WithName("Alice").Build()
        }.AsQueryable();

        var sorted = data.ApplySort(default, SortDirection.Ascending, e => e.Id);
        var result = sorted.ToList();
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

[Fact]
    public void QueryableExtensions_ApplyFilter_EmptyFilterReturnsSource()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var filter = new FilterParameters(); // empty filter
        var filtered = query.ApplyFilter(filter);
        filtered.Should().BeSameAs(query);
    }

[Fact]
    public void QueryableExtensions_ApplyFilter_InvalidFilterThrowsByDefault()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var filter = new FilterParameters { Value = "NonExistent=123" };
        var act = () => query.ApplyFilter(filter);
        act.Should().Throw<ArgumentException>();
    }

[Fact]
    public void QueryableExtensions_ApplyFilter_ValidFilter()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).WithName("A").Build(),
            new TestEntityBuilder().WithId(2).WithName("B").Build()
        }.AsQueryable();

        var filter = new FilterParameters { Value = "Name=A" };
        var filtered = data.ApplyFilter(filter);
        var result = filtered.ToList();
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("A");
    }

[Fact]
    public void QueryableExtensions_ApplySort_NameAsc_IdDesc()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(2).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(3).WithName("Bob").Build()
        }.AsQueryable();

        var sorted = data.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name asc, Id desc" });
        var result = sorted.ToList();
        result[0].Id.Should().Be(2); // Alice Id 2
        result[1].Id.Should().Be(1); // Alice Id 1
        result[2].Id.Should().Be(3); // Bob Id 3
    }

[Fact]
    public void QueryableExtensions_ApplySort_InvalidProperty_Throws()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var action = () => query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "NonExistentProperty desc" });
        action.Should().Throw<InvalidOperationException>();
    }

[Fact]
    public void QueryableExtensions_ApplySort_MultipleColumns_ThenByDescending()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(2).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(3).WithName("Bob").Build()
        }.AsQueryable();

        var sorted = data.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name desc, Id desc" });
        var result = sorted.ToList();
        result[0].Id.Should().Be(3); // Bob Id 3
        result[1].Id.Should().Be(2); // Alice Id 2
        result[2].Id.Should().Be(1); // Alice Id 1
    }

[Fact]
    public void QueryableExtensions_ApplySort_WithDefaultSort_Ascending()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(2).Build(),
            new TestEntityBuilder().WithId(1).Build()
        }.AsQueryable();

        var sorted = data.ApplySort(default, SortDirection.Ascending, e => e.Id);
        var result = sorted.ToList();
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

[Fact]
    public void QueryableExtensions_ApplySort_WithDefaultSort_Descending()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).Build(),
            new TestEntityBuilder().WithId(2).Build()
        }.AsQueryable();

        var sorted = data.ApplySort(default, SortDirection.Descending, e => e.Id);
        var result = sorted.ToList();
        result[0].Id.Should().Be(2);
        result[1].Id.Should().Be(1);
    }

[Fact]
    public async Task QueryableExtensions_ToPagedListAsync_WithFilterAndSort()
    {
        var ctx = GetContext();
        
        
        await ctx.Entities.AddRangeAsync(
            new TestEntityBuilder().WithId(1).WithName("B").Build(),
            new TestEntityBuilder().WithId(2).WithName("A").Build(),
            new TestEntityBuilder().WithId(3).WithName("C").Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Entities.AsQueryable();
        var filter = new FilterParameters { Value = "Name=A" };
        var pagination = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();

        var result = await query.ToPagedListAsync(
            filter,
            new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Id desc" },
            e => new { e.Id, e.Name },
            pagination,
            countTotal: true,
            maxPageSize: 5);

        result.Count.Should().Be(1);
        result[0].Name.Should().Be("A");
        
        var resultNoProject = await query.ToPagedListAsync(
            filter,
            new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Id desc" },
            pagination,
            countTotal: true,
            maxPageSize: 5);

        resultNoProject.Count.Should().Be(1);
        resultNoProject[0].Name.Should().Be("A");
    }

[Fact]
    public void QueryableExtensions_ApplySort_InvalidProperty_Throws2()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var action = () => query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "NonExistent" }, SortDirection.Descending, e => e.Id);
        action.Should().Throw<InvalidOperationException>();
    }

[Fact]
    public void QueryableExtensions_ApplySort_InvalidProperty_Throws3()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var action = () => query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "NonExistent" }, SortDirection.Ascending, e => e.Id);
        action.Should().Throw<InvalidOperationException>();
    }

[Fact]
    public void QueryableExtensions_ApplySort_EmptyParts()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(2).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(3).WithName("Bob").Build()
        }.AsQueryable();

        var sorted = data.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name asc,,Id desc" });
        var result = sorted.ToList();
        result[0].Id.Should().Be(2);
        result[1].Id.Should().Be(1);
        result[2].Id.Should().Be(3);
    }

[Fact]
    public void QueryableExtensions_ApplySort_UpperCaseSuffixes()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(2).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(3).WithName("Bob").Build()
        }.AsQueryable();

        var sorted = data.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name ASC, Id DESC" });
        var result = sorted.ToList();
        result[0].Id.Should().Be(2);
        result[1].Id.Should().Be(1);
        result[2].Id.Should().Be(3);
    }

[Fact]
    public void QueryableExtensions_ApplySort_NoSpaceSuffix_UsesDefaultDirection()
    {
        var data = new List<TestEntity>
        {
            new TestEntityBuilder().WithId(1).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(2).WithName("Alice").Build(),
            new TestEntityBuilder().WithId(3).WithName("Bob").Build()
        }.AsQueryable();

        var sorted = data.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name, Id" }, SortDirection.Descending);
        var result = sorted.ToList();
        result[0].Id.Should().Be(3); // Bob
        result[1].Id.Should().Be(2); // Alice Id 2
        result[2].Id.Should().Be(1); // Alice Id 1
    }

[Fact]
    public async Task ToPagedListAsync_SkipCountOverflow_ThrowsArgumentOutOfRangeException()
    {
        var ctx = GetContext();
        var query = ctx.Set<TypeEntity>();
        // Page * PageSize will overflow an int but fit in a long, capping skip at int.MaxValue
        var parameters = new PaginationParametersBuilder().WithPage(21476).WithPageSize(100000).Build();
        Func<Task> act = async () => await query.ToPagedListAsync(parameters, maxPageSize: 100000);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

[Fact]
    public async Task QueryableExtensions_Batching_Page1_Logic()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var batchCount = 0;
        await foreach (var batch in query.ToPagedListBatchedAsync(batchSize: 1).WithCancellation(cts.Token))
        {
            batchCount++;
            if (batchCount > 10) break; // PREVENT INFINITE LOOP FOR STRYKER!
        }
        
        batchCount.Should().Be(1);
    }

[Fact]
    public async Task QueryableExtensions_OffsetLargeSkip_IntMaxValue_Throws()
    {
        var ctx = GetContext();
        
        
        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // Using int.MaxValue page
        var act = async () => await query.ToPagedListAsync(new PaginationParametersBuilder().WithPage(int.MaxValue).WithPageSize(10).Build(), options: null);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

[Fact]
        public async Task GetTotalCountAsync_NonEFQueryable_ApproximateCount_FallsBack()
        {
            var items = new List<TestEntity> { new TestEntityBuilder().WithId(1).Build(), new TestEntityBuilder().WithId(2).Build() }.AsQueryable();
            
            var parameters = new PaginationParametersBuilder().WithPageSize(10).Build();
            
            var act = async () => await items.ToPagedListAsync(parameters, countTotal: true, useApproximateCount: true);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*IAsyncQueryProvider*");
        }
}





