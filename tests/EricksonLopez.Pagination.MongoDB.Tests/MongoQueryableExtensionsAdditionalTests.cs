// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.MongoDB.Tests;

[Collection("MongoDB")]
public class MongoQueryableExtensionsAdditionalTests
{
    [Fact]
    public void ApplySort_EmptySort_NoDefault_ReturnsSource()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(default);
        sorted.Should().BeSameAs(query);
    }
    
    [Fact]
    public void ApplySort_EmptySort_WithDefault_UsesDefault()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" } };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(default, EricksonLopez.Pagination.Abstractions.SortDirection.Descending, e => e.Id);
        var result = sorted.ToList();
        result[0].Id.Should().Be(2);
        result[1].Id.Should().Be(1);
    }
    
    [Fact]
    public void ApplySort_Ascending_ValidProperty()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name asc" });
        var result = sorted.ToList();
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
    }

    [Fact]
    public void ApplySort_Descending_ValidProperty()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" } };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name desc" });
        var result = sorted.ToList();
        result[0].Name.Should().Be("Bob");
        result[1].Name.Should().Be("Alice");
    }
    
    [Fact]
    public void ApplySort_MultipleProperties()
    {
        var users = new List<User> 
        { 
            new User { Id = 1, Name = "Alice" }, 
            new User { Id = 2, Name = "Alice" },
            new User { Id = 3, Name = "Bob" }
        };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name asc, Id desc" });
        var result = sorted.ToList();
        result[0].Id.Should().Be(2); // Alice Id 2
        result[1].Id.Should().Be(1); // Alice Id 1
        result[2].Id.Should().Be(3); // Bob Id 3
    }
    
    [Fact]
    public void ApplySort_MultipleProperties_Ascending()
    {
        var users = new List<User> 
        { 
            new User { Id = 2, Name = "Alice" }, 
            new User { Id = 1, Name = "Alice" },
            new User { Id = 3, Name = "Bob" }
        };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name desc, Id asc" });
        var result = sorted.ToList();
        result[0].Name.Should().Be("Bob");
        result[1].Id.Should().Be(1);
        result[2].Id.Should().Be(2);
    }
    
    [Fact]
    public void ApplySort_EmptyPart_IsIgnored()
    {
        var users = new List<User> 
        { 
            new User { Id = 1, Name = "Alice" }, 
            new User { Id = 2, Name = "Alice" },
            new User { Id = 3, Name = "Bob" }
        };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Name asc, , Id desc" });
        var result = sorted.ToList();
        result[0].Id.Should().Be(2);
        result[1].Id.Should().Be(1);
        result[2].Id.Should().Be(3);
    }

    [Fact]
    public void ApplySort_InvalidProperty_IgnoresIt()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "InvalidProperty" });
        sorted.Should().BeSameAs(query); // Should ignore invalid property
    }
    
    [Fact]
    public void ApplyFilter_Empty_ReturnsSource()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var filtered = query.ApplyFilter(new FilterParameters());
        filtered.Should().BeSameAs(query);
    }
    
    [Fact]
    public void ApplyFilter_Valid_AppliesFilter()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var filter = new FilterParameters { Value = "Name=Alice" };
        var filtered = query.ApplyFilter(filter);
        filtered.Should().NotBeSameAs(query);
    }
    
    internal sealed class NonComparableClass { public int Id { get; set; } }

    [Fact]
    public async Task BuildComparisonPredicate_UnsupportedType_Throws()
    {
        var items = new List<NonComparableClass> { new NonComparableClass { Id = 1 } };
        var query = new MockMongoQueryable<NonComparableClass>(items);
        var parameters = new CursorPaginationParameters { First = 10, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("test") };
        Func<Task> act = async () => await query.ToCursorPagedListAsync<NonComparableClass, object>(e => e, parameters);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cursor key types must implement IComparable<T>*");
    }

    private sealed class EntityWithTimeSpan { public int Id { get; set; } public TimeSpan Duration { get; set; } }

    [Fact]
    public void ApplyFilter_TimeSpan_AppliesFilter()
    {
        var users = new List<EntityWithTimeSpan> { new EntityWithTimeSpan { Id = 1, Duration = TimeSpan.FromMinutes(5) } };
        var query = new MockMongoQueryable<EntityWithTimeSpan>(users);
        
        var filter = new FilterParameters { Value = "Duration=00:05:00" };
        var filtered = query.ApplyFilter(filter);
        filtered.Should().NotBeSameAs(query);
        // Verify it actually filters
        var result = filtered.ToList();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ToPagedListAsync_CustomFactory()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var factory = NSubstitute.Substitute.For<IPagedListFactory>();
        
        factory.CreatePagedList<User>(Arg.Any<IReadOnlyList<User>>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>())
               .Returns(new PagedList<User>(users, 1, 1, 1));

        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var pagedList = await query.ToPagedListAsync(parameters, factory: factory);
        
        factory.ReceivedWithAnyArgs().CreatePagedList<User>(default!, default, default, default, default);
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelector_CustomFactory()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var factory = NSubstitute.Substitute.For<IPagedListFactory>();
        
        factory.CreatePagedList<int>(Arg.Any<IReadOnlyList<int>>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>())
               .Returns(new PagedList<int>(new[] { 1 }, 1, 1, 1));

        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var pagedList = await query.ToPagedListAsync(u => u.Id, parameters, factory: factory);
        
        factory.ReceivedWithAnyArgs().CreatePagedList<int>(default!, default, default, default, default);
    }

    [Fact]
    public async Task QueryPagedListAsync_Paginates()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var factory = Substitute.For<ICursorPagedListFactory>();
        
        var parameters = new CursorPaginationParameters { First = 1 };
        var pagedList = await query.ToCursorPagedListAsync(u => u.Id, parameters, factory: factory);
        
        factory.ReceivedWithAnyArgs().CreateCursorPagedList<User>(default!, default, default, default, default, default);
    }

    [Fact]
    public void ApplySort_EmptySort_WithDefault_Ascending_UsesDefault()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(default, EricksonLopez.Pagination.Abstractions.SortDirection.Ascending, e => e.Id);
        var result = sorted.ToList();
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public void ApplySort_InvalidProperty_All_UsesDefault_Ascending()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Invalid1, Invalid2" }, EricksonLopez.Pagination.Abstractions.SortDirection.Ascending, e => e.Id);
        var result = sorted.ToList();
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ClassKey_ConvenienceOverload()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { First = 10 };
        var pagedList = await query.ToCursorPagedListAsync(u => u.Name, parameters);
        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToPagedListAsync_WithFilterAndSortBy()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var filter = new FilterParameters { Value = "Name=Alice" };
        var pagedList = await query.ToPagedListAsync(filter, new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Id desc" }, parameters);
        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToPagedListAsync_WithFilterSortByAndSelector()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var filter = new FilterParameters { Value = "Name=Alice" };
        var pagedList = await query.ToPagedListAsync(filter, new EricksonLopez.Pagination.Abstractions.SortParameters { Value = "Id desc" }, u => new { u.Id }, parameters);
        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_Works()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var enumerable1 = query.OrderBy(u => u.Id).ToPagedAsyncEnumerable(parameters);
        
        var list1 = new List<User>();
        await foreach (var item in enumerable1) list1.Add(item);
        list1.Should().HaveCount(1);
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_Page2()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new PaginationParameters { Page = 2, PageSize = 10 };
        var enumerable1 = query.OrderBy(u => u.Id).ToPagedAsyncEnumerable(parameters);
        
        var list1 = new List<User>();
        await foreach (var item in enumerable1) list1.Add(item);
        list1.Should().BeEmpty();
    }

    [Fact]
    public async Task ToPagedListAsync_CountZero_ReturnsEmptyPage()
    {
        var users = new List<User>();
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 10);

        var pagedList = await query.ToPagedListAsync(parameters, countTotal: true);

        pagedList.Count.Should().Be(0);
        pagedList.TotalCount.Should().Be(0);
        pagedList.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToPagedListAsync_WithoutCountTotal_HasNextPage_RemovesLastItem()
    {
        var users = Enumerable.Range(1, 15).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 10);

        var pagedList = await query.ToPagedListAsync(parameters, countTotal: false);

        pagedList.Count.Should().Be(10);
        pagedList.TotalCount.Should().BeNull();
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelector_WithoutCountTotal_ReturnsPage()
    {
        var users = Enumerable.Range(1, 15).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 10);

        var pagedList = await query.ToPagedListAsync(
            selector: u => u.Id,
            parameters: parameters, 
            countTotal: false);

        pagedList.Count.Should().Be(10);
        pagedList.TotalCount.Should().BeNull();
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelector_WithoutCountTotal_LastPage_HasNoNextPage()
    {
        var users = Enumerable.Range(1, 15).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(2, 10);

        var pagedList = await query.ToPagedListAsync(
            selector: u => u.Id,
            parameters: parameters, 
            countTotal: false);

        pagedList.Count.Should().Be(5);
        pagedList.TotalCount.Should().BeNull();
        pagedList.HasNextPage.Should().BeFalse();
    }
    [Fact]
    public async Task ToPagedListAsync_NullMaxPageSize_UsesPageSize()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 3);
        var pagedList = await query.ToPagedListAsync(parameters, countTotal: true, maxPageSize: null);

        pagedList.PageSize.Should().Be(3);
    }

    [Fact]
    public async Task ToPagedListAsync_WithMaxPageSize_ConstrainsPageSize()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 10);
        var pagedList = await query.ToPagedListAsync(parameters, countTotal: true, maxPageSize: 2);

        pagedList.PageSize.Should().Be(2); // PageSize property on PagedList reflects effective page size
        pagedList.Count.Should().Be(2);     // But actual items returned is constrained
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelector_NullMaxPageSize_UsesPageSize()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 3);
        var pagedList = await query.ToPagedListAsync(u => u.Id, parameters, countTotal: true, maxPageSize: null);

        pagedList.PageSize.Should().Be(3);
    }

    [Fact]
    public async Task ToPagedListAsync_CountTotalFalse_ExactFullPage_DoesNotThrow()
    {
        var users = new List<User> { new User { Id = 1, Name = "A" }, new User { Id = 2, Name = "B" } };
        var query = new MockMongoQueryable<User>(users);
        
        // Request page size 2, exactly matching the number of items
        var parameters = PaginationParameters.Create(1, 2);
        
        // This will query Take(3) and get exactly 2 items.
        // items.Count > pageSize (2 > 2) is false.
        // Mutant >= will be true, and try to RemoveAt(2) which throws exception.
        var pagedList = await query.ToPagedListAsync(parameters, countTotal: false);

        pagedList.HasNextPage.Should().BeFalse();
        pagedList.Count.Should().Be(2);
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelector_CountTotalFalse_ExactFullPage_DoesNotThrow()
    {
        var users = new List<User> { new User { Id = 1, Name = "A" }, new User { Id = 2, Name = "B" } };
        var query = new MockMongoQueryable<User>(users);
        
        var parameters = PaginationParameters.Create(1, 2);
        
        var pagedList = await query.ToPagedListAsync(u => u.Id, parameters, countTotal: false);

        pagedList.HasNextPage.Should().BeFalse();
        pagedList.Count.Should().Be(2);
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelector_WithMaxPageSize_ConstrainsPageSize()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 10);
        var pagedList = await query.ToPagedListAsync(u => u.Id, parameters, countTotal: true, maxPageSize: 2);

        pagedList.PageSize.Should().Be(2);
        pagedList.Count.Should().Be(2);
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelector_CountZero_ReturnsEmptyPage()
    {
        var users = new List<User>();
        var query = new MockMongoQueryable<User>(users);
        var parameters = PaginationParameters.Create(1, 10);

        var pagedList = await query.ToPagedListAsync(u => u.Id, parameters, countTotal: true);

        pagedList.Count.Should().Be(0);
        pagedList.TotalCount.Should().Be(0);
    }
    [Fact]
    public void ApplySort_TooLongProperty_IgnoresIt()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var longName = new string('A', 129);
        var sorted = query.ApplySort(new EricksonLopez.Pagination.Abstractions.SortParameters { Value = longName });
        sorted.Should().BeSameAs(query);
    }

    [Fact]
    public void ApplyFilter_WithUnparseableFilter_ReturnsSource()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        // A filter that parses to an empty/null expression (e.g. invalid syntax)
        var filter = new FilterParameters { Value = "InvalidSyntax" };
        var filtered = query.ApplyFilter(filter, unknownFieldBehavior: EricksonLopez.Pagination.FilterUnknownFieldBehavior.Ignore);
        
        // The predicate is null because there are no valid clauses
        filtered.Should().BeSameAs(query);
    }
    [Fact]
    public async Task ToCursorPagedListAsync_Descending_ForwardNavigation()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { First = 10, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("2") };

        var pagedList = await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            parameters: parameters,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending,
            maxPageSize: null);

        pagedList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Descending_BackwardNavigation()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { Last = 10, Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("1") };

        var pagedList = await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            parameters: parameters,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending,
            maxPageSize: null);

        pagedList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithSelector_Ascending_ForwardNavigation()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { First = 10, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("1") };

        var pagedList = await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            projection: u => u.Name,
            parameters: parameters,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Ascending,
            maxPageSize: null);

        pagedList.Should().HaveCount(2);
        pagedList[0].Should().Be("Bob");
        pagedList[1].Should().Be("Charlie");
        var encoder = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault;
        pagedList.StartCursor.Should().Be(encoder.Encode("2"));
        pagedList.EndCursor.Should().Be(encoder.Encode("3"));
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithSelector_Descending_ForwardNavigation()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { First = 10, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("2") };

        var pagedList = await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            projection: u => u.Name,
            parameters: parameters,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending,
            maxPageSize: null);

        pagedList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithSelector_Descending_BackwardNavigation()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { Last = 10, Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("1") };

        var pagedList = await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            projection: u => u.Name,
            parameters: parameters,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending,
            maxPageSize: null);

        pagedList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_StringKey_Ascending_ForwardNavigation()
    {
        var users = new List<User>
        {
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" },
            new User { Id = 3, Name = "Charlie" }
        };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { First = 2, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("Alice") };

        var pagedList = await query.ToCursorPagedListAsync(
            keySelector: u => u.Name,
            parameters: parameters,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Ascending);

        pagedList.Should().HaveCount(2);
        pagedList[0].Name.Should().Be("Bob");
        pagedList[1].Name.Should().Be("Charlie");
        pagedList.StartCursor.Should().NotBeNullOrWhiteSpace();
        pagedList.EndCursor.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_StringKey_Descending_ForwardNavigation()
    {
        var users = new List<User>
        {
            new User { Id = 3, Name = "Charlie" },
            new User { Id = 2, Name = "Bob" },
            new User { Id = 1, Name = "Alice" }
        };
        var query = new MockMongoQueryable<User>(users);
        var parameters = new CursorPaginationParameters { First = 2, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("Charlie") };

        var pagedList = await query.ToCursorPagedListAsync(
            keySelector: u => u.Name,
            parameters: parameters,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending);

        pagedList.Should().HaveCount(2);
        pagedList[0].Name.Should().Be("Bob");
        pagedList[1].Name.Should().Be("Alice");
        pagedList.StartCursor.Should().NotBeNullOrWhiteSpace();
        pagedList.EndCursor.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_NonMongoProvider_ThrowsInvalidOperationException()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        // This queryable's provider is EnumerableQuery (System.Linq assembly)
        var query = users.AsQueryable();
        var parameters = new CursorPaginationParameters { First = 10 };

        Func<Task> act = async () => await query.ToCursorPagedListAsync(u => u.Id, parameters);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*was called with a provider from assembly*");
    }

    [Fact]
    public void ApplySort_AllInvalidColumns_UsesDefaultSort_Descending()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = "Bob" } };
        var query = new MockMongoQueryable<User>(users);

        var sorted = query.ApplySort(
            new SortParameters { Value = "NonExistentField" },
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending,
            defaultSort: u => u.Id
        );

        var result = sorted.ToList();
        result[0].Id.Should().Be(2);
        result[1].Id.Should().Be(1);
    }

    [Fact]
    public void ApplySort_WithEmptyPartInList_SkipsEmpty()
    {
        var users = new List<User> { new User { Id = 2, Name = "Bob" }, new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);

        var sorted = query.ApplySort(new SortParameters { Value = " , Name asc, " });
        var result = sorted.ToList();
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Forward_WithAfter_SetsHasPreviousPageTrue()
    {
        var users = Enumerable.Range(1, 5).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        var afterToken = HmacCursorEncoder.DevelopmentDefault.Encode("2");

        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            parameters: new CursorPaginationParameters { First = 2, After = afterToken }
        );

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Backward_WithBefore_SetsHasNextPageTrue()
    {
        var users = Enumerable.Range(1, 5).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        var beforeToken = HmacCursorEncoder.DevelopmentDefault.Encode("4");

        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            parameters: new CursorPaginationParameters { Last = 2, Before = beforeToken }
        );

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Backward_WithBefore_NoHasMore_SetsHasNextPageTrue()
    {
        var users = new List<User> { new User { Id = 1, Name = "User 1" }, new User { Id = 2, Name = "User 2" }, new User { Id = 3, Name = "User 3" } };
        var query = new MockMongoQueryable<User>(users);
        var beforeToken = HmacCursorEncoder.DevelopmentDefault.Encode("3");

        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            parameters: new CursorPaginationParameters { Last = 5, Before = beforeToken }
        );

        result.Count.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }
}






