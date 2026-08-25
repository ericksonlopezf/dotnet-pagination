// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
#pragma warning disable S3459
#pragma warning disable S3881
#pragma warning disable S6966
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class QueryableExtensionsGapsTests
{
    public class NonComparableClass
    {
        public string Value { get; set; } = "";
    }

    private class GapsTestEntity
    {
        public int Id { get; set; }
        public NonComparableClass UncomparableProp { get; set; } = new NonComparableClass();
    }

    [Fact]
    public async Task KeysetBuilder_NonComparableType_ThrowsInvalidOperationException()
    {
        var data = new[]
        {
            new GapsTestEntity { Id = 1, UncomparableProp = new NonComparableClass() }
        }.AsQueryable();

        var paramsObj = new CursorPaginationParametersBuilder().WithFirst(5).Build();
        Func<Task> act = async () => await data.Keyset(paramsObj).Ascending(e => e.UncomparableProp).ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void ApplySort_InvalidSortCol_NoDefaultSort_ReturnsOriginalQuery()
    {
        var query = Enumerable.Empty<TestEntity>().AsQueryable();
        var sortParams = new SortParameters { Value = " , " };
        var allowedProps = new[] { "Id" };
        
        var result = query.ApplySort(sortParams, allowedProperties: allowedProps);
        
        // When the sort col is not allowed, it skips it. Since there's no valid col and no default sort,
        // it returns the original query. We can check if it's the exact same query object.
        result.Should().BeSameAs(query);
    }
    
    [Fact]
    public void ApplySort_InvalidSortCol_WithDefaultSortAscending_AppliesDefaultSort()
    {
        var data = new List<TestEntity> { new TestEntityBuilder().WithId(2).Build(), new TestEntityBuilder().WithId(1).Build() }.AsQueryable();
        var sortParams = new SortParameters { Value = " , " };
        var allowedProps = new[] { "Id" };
        
        var result = data.ApplySort(sortParams, SortDirection.Ascending, e => e.Id, allowedProperties: allowedProps);
        
        var list = result.ToList();
        list[0].Id.Should().Be(1);
        list[1].Id.Should().Be(2);
    }
    
    [Fact]
    public void ApplySort_InvalidSortCol_WithDefaultSortDescending_AppliesDefaultSort()
    {
        var data = new List<TestEntity> { new TestEntityBuilder().WithId(1).Build(), new TestEntityBuilder().WithId(2).Build() }.AsQueryable();
        var sortParams = new SortParameters { Value = " , " };
        var allowedProps = new[] { "Id" };
        
        var result = data.ApplySort(sortParams, SortDirection.Descending, e => e.Id, allowedProperties: allowedProps);
        
        var list = result.ToList();
        list[0].Id.Should().Be(2);
        list[1].Id.Should().Be(1);
    }

    [Fact]
    public void ApplyFilter_StringTooLong_ThrowsArgumentException()
    {
        var query = Enumerable.Empty<TestEntity>().AsQueryable();
        var filterParams = new FilterParameters { Value = "Id=1" };
        var options = new PaginationCoreOptions { MaxFilterStringLength = 3 }; // "Id=1" is 4 chars
        
        var action = () => query.ApplyFilter(filterParams, options: options);
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds the maximum allowed length*");
    }

    [Fact]
    public async Task ToPagedListAsync_SkipExceedsIntMaxValue_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new PaginationParametersBuilder().WithPage(30000).WithPageSize(100_000).Build();
        
        var action = async () => await Enumerable.Empty<TestEntity>().AsQueryable()
            .ToPagedListAsync(parameters, false, maxPageSize: 100_000, cancellationToken: default, options: null);
        
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}






