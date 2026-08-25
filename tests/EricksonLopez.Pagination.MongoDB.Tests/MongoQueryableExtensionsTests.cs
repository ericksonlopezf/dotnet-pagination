// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[Collection("MongoDB")]
public class MongoQueryableExtensionsTests
{
    [Fact]
    public async Task ToPagedListAsync_CountTotal_ReturnsPagedList()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users).OrderBy(u => u.Id);
        
        var result = await query.ToPagedListAsync(new PaginationParameters { Page = 1, PageSize = 10 }, countTotal: true);
        
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task ToPagedListAsync_NoCountTotal_ReturnsPagedList()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users).OrderBy(u => u.Id);
        
        var result = await query.ToPagedListAsync(new PaginationParameters { Page = 1, PageSize = 10 }, countTotal: false);
        
        result.TotalCount.Should().BeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_NullAfterKey_ReturnsCursorPagedList()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            
            parameters: new CursorPaginationParameters { First = 10 }
        );
        
        result.Should().HaveCount(1);
        result.StartCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_BackwardNavigation_ReturnsCursorPagedList()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            
            
            parameters: new CursorPaginationParameters { Last = 10, Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("15") }
        );
        
        result.Should().HaveCount(1);
        result.StartCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_StringKey_BackwardNavigation_ReturnsCursorPagedList()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        
        var result = await query.ToCursorPagedListAsync(
            u => u.Name,
            parameters: new CursorPaginationParameters { Last = 10, Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("Z") }
        );
        
        result.Should().HaveCount(1);
        result.StartCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ForwardNavigation_ReturnsCursorPagedList()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var result = await query.ToCursorPagedListAsync(u => u.Id, parameters: new CursorPaginationParameters { First = 10, Last = 10, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("1") });
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(3);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_StringKey_ForwardNavigation_ReturnsCursorPagedList()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 3, Name = "Charlie" } };
        var query = new MockMongoQueryable<User>(users);
        var result = await query.ToCursorPagedListAsync(u => u.Name,   parameters: new CursorPaginationParameters { First = 10 });
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_MaxPageSize_ClampsPageSize()
    {
        var users = Enumerable.Range(1, 10).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        var parameters = CursorPaginationParameters.Parse("first=100", null);

        var result = await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            projection: u => new { u.Id, NameUpper = u.Name.ToUpperInvariant() },
            parameters: parameters,
            maxPageSize: 5
        );

        result.Count.Should().Be(5);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_BackwardNavigation()
    {
        var users = Enumerable.Range(1, 20).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        var parameters = CursorPaginationParameters.Parse("last=10", null) with { Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("21") };

        var result = await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            projection: u => new { u.Id, NameUpper = u.Name.ToUpperInvariant() },
            parameters: parameters
        );

        result.Count.Should().Be(10);
        result[0].Id.Should().Be(11);
        result[^1].Id.Should().Be(20);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithCustomFactory_UsesFactory()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var factory = new FakeFactory();

        await query.ToCursorPagedListAsync(
            keySelector: u => u.Id,
            parameters: new CursorPaginationParameters { First = 10 },
            factory: factory
        );

        factory.WasCalled.Should().BeTrue();
    }

    private sealed class FakeFactory : ICursorPagedListFactory
    {
        public bool WasCalled { get; private set; }
        public ICursorPagedList<T> CreateCursorPagedList<T>(IReadOnlyList<T> items, long? totalCount, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)
        {
            WasCalled = true;
            return DefaultPagedListFactory.Instance.CreateCursorPagedList(items, totalCount, startCursor, endCursor, hasPreviousPage, hasNextPage);
        }
    }

    [Fact]
    public async Task ToCursorPagedListAsync_BackwardNavigation_HasMore_RemovesFirstItem()
    {
        var users = Enumerable.Range(1, 15).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);
        
        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            
            
            parameters: new CursorPaginationParameters { Last = 10, Before = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("15") }
        );
        
        result.Should().HaveCount(10);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_FirstKeyNull_ThrowsInvalidOperationException()
    {
        var users = new List<User> { new User { Id = 1, Name = null! } };
        var query = new MockMongoQueryable<User>(users);
        
        var act = async () => await query.ToCursorPagedListAsync(
            u => u.Name,
            parameters: new CursorPaginationParameters { First = 10 }
        );
        
        await act.Should().ThrowAsync<System.InvalidOperationException>()
            .WithMessage("*first item*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_LastKeyNull_ThrowsInvalidOperationException()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" }, new User { Id = 2, Name = null! } };
        // By setting maxPageSize = 2 and fetching First = 2, we get both items. 
        // We will just mock the list returned to have null LAST.
        // Wait, MockMongoQueryable does sort. If we use Ascending, null is first. 
        // Let's use Descending, so "Alice" is first and null is last.
        var query = new MockMongoQueryable<User>(users).OrderByDescending(u => u.Name); 
        // Wait, if I explicitly pre-sort it, ToCursorPagedListAsync might apply ANOTHER OrderByDescending which works.
        // Let's just do it with Ascending, but we make the FIRST item NOT null, and LAST item null.
        // If Ascending puts null first, then we need null to be LARGER than the other element!
        // We can do this by using a keySelector that returns a type where null is considered LARGER? No.
        
        // Actually, just test the method directly with a FakeAsyncCursor!
        // ToCursorPagedListAsync internally calls ToListAsync.
        // If we provide a query provider that returns ["Alice", null] without sorting, it will work.
        var fakeQuery = new FakeUnsortedQueryable<User>(new[] { new User { Name = "Alice" }, new User { Name = null! } });
        var act = async () => await fakeQuery.ToCursorPagedListAsync(
            u => u.Name,
            parameters: new CursorPaginationParameters { First = 10 }
        );
        
        await act.Should().ThrowAsync<System.InvalidOperationException>()
            .WithMessage("*last item*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithMaxPageSize_ClampsPageSize()
    {
        var users = Enumerable.Range(1, 10).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);

        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            parameters: new CursorPaginationParameters { First = 10 },
            maxPageSize: 3
        );

        result.Count.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Backward_Descending_AppliesCorrectOrdering()
    {
        var users = Enumerable.Range(1, 10).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);

        var beforeCursor = HmacCursorEncoder.DevelopmentDefault.Encode("5");
        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            parameters: new CursorPaginationParameters { Last = 2, Before = beforeCursor },
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending
        );

        result.Count.Should().Be(2);
        result[0].Id.Should().Be(7);
        result[1].Id.Should().Be(6);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Backward_Ascending_AppliesCorrectOrdering()
    {
        var users = Enumerable.Range(1, 10).Select(i => new User { Id = i, Name = $"User {i}" }).ToList();
        var query = new MockMongoQueryable<User>(users);

        var beforeCursor = HmacCursorEncoder.DevelopmentDefault.Encode("5");
        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            parameters: new CursorPaginationParameters { Last = 2, Before = beforeCursor },
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Ascending
        );

        result.Count.Should().Be(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(4);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithCustomEncoder_UsesCustomEncoder()
    {
        var users = new List<User> { new User { Id = 1, Name = "Alice" } };
        var query = new MockMongoQueryable<User>(users);
        var customEncoder = NSubstitute.Substitute.For<ICursorEncoder>();
        customEncoder.Encode("1").Returns("CUSTOM_1");

        var result = await query.ToCursorPagedListAsync(
            u => u.Id,
            parameters: new CursorPaginationParameters { First = 5 },
            cursorEncoder: customEncoder
        );

        result.StartCursor.Should().Be("CUSTOM_1");
        result.EndCursor.Should().Be("CUSTOM_1");
    }

    private sealed class UserWithNullableKey
    {
        public int Id { get; set; }
        public string? Code { get; set; }
    }

    private sealed class NonComparableType
    {
        public int Value { get; set; }
    }

    private sealed class UserWithNonComparableKey
    {
        public int Id { get; set; }
        public NonComparableType CustomKey { get; set; } = new();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ThrowsInvalidOperationException_WhenFirstItemKeyIsNull()
    {
        var users = new List<UserWithNullableKey>
        {
            new() { Id = 1, Code = null }
        };
        var query = new MockMongoQueryable<UserWithNullableKey>(users);

        Func<Task> act = async () => await query.ToCursorPagedListAsync(
            u => u.Code,
            parameters: new CursorPaginationParameters { First = 10 }
        );

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("The cursor key selector returned null for the first item in the page. Cursor key columns must be non-null. Key type: String. If the column is nullable, filter out null values before calling ToCursorPagedListAsync.");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ThrowsInvalidOperationException_WhenLastItemKeyIsNull()
    {
        var users = new List<UserWithNullableKey>
        {
            new() { Id = 1, Code = "VALID_CODE" },
            new() { Id = 2, Code = null }
        };
        var query = new MockMongoQueryable<UserWithNullableKey>(users);

        Func<Task> act = async () => await query.ToCursorPagedListAsync(
            u => u.Code,
            direction: EricksonLopez.Pagination.Abstractions.SortDirection.Descending,
            parameters: new CursorPaginationParameters { First = 10 }
        );

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("The cursor key selector returned null for the last item in the page. Cursor key columns must be non-null. Key type: String. If the column is nullable, filter out null values before calling ToCursorPagedListAsync.");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_ThrowsInvalidOperationException_WhenKeyTypeIsNotIComparable()
    {
        var users = new List<UserWithNonComparableKey>
        {
            new() { Id = 1, CustomKey = new NonComparableType { Value = 100 } }
        };
        var query = new MockMongoQueryable<UserWithNonComparableKey>(users);
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register<NonComparableType>(s => new NonComparableType { Value = int.Parse(s) });

        Func<Task> act = async () => await query.ToCursorPagedListAsync(
            u => u.CustomKey,
            parameters: new CursorPaginationParameters { First = 10, After = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("100") },
            decoderRegistry: registry
        );

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("Cannot build a keyset cursor comparison for type 'NonComparableType'. The type does not define 'CompareTo(NonComparableType)'. Cursor key types must implement IComparable<T>.");
    }
}

public class FakeUnsortedQueryable<T> : IOrderedQueryable<T>, IAsyncCursorSource<T>
{
    private readonly IEnumerable<T> _items;
    public FakeUnsortedQueryable(IEnumerable<T> items) { _items = items; Provider = new FakeProvider(_items); }
    public Type ElementType => typeof(T);
    public Expression Expression => Expression.Constant(this);
    public IQueryProvider Provider { get; }
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IAsyncCursor<T> ToCursor(CancellationToken cancellationToken = default) => new MockAsyncCursor<T>(_items);
    public Task<IAsyncCursor<T>> ToCursorAsync(CancellationToken cancellationToken = default) => Task.FromResult<IAsyncCursor<T>>(new MockAsyncCursor<T>(_items));

    private sealed class FakeProvider : IQueryProvider
    {
        private readonly IEnumerable<T> _items;
        public FakeProvider(IEnumerable<T> items) { _items = items; }
        public IQueryable CreateQuery(Expression expression) => throw new NotImplementedException();
        // Return this same queryable, IGNORING any OrderBy or Where expressions!
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => (IQueryable<TElement>)new FakeUnsortedQueryable<T>(_items);
        public object? Execute(Expression expression) => throw new NotImplementedException();
        public TResult Execute<TResult>(Expression expression) => throw new NotImplementedException();
    }
}






