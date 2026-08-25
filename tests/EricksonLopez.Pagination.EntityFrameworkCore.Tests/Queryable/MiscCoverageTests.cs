// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class MiscCoverageTests
{
    private static TestDbContext GetContext(int entityCount = 0) => TestDbContext.CreateInMemory(entityCount);

    [Fact]
    public async Task ToOptimizedPagedListAsync_ItemsCountEqualsPageSize_HasNextPageIsFalse()
    {
        var ctx = GetContext();
        await ctx.Entities.AddRangeAsync(
            new TestEntityBuilder().WithId(1).Build(),
            new TestEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Entities.AsQueryable();
        var result = await query.ToPagedListAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(2).Build(), countTotal: false);

        result.Count.Should().Be(2);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToOptimizedPagedListAsync_ItemsCountGreaterThanPageSize_HasNextPageIsTrue()
    {
        var ctx = GetContext();
        await ctx.Entities.AddRangeAsync(
            new TestEntityBuilder().WithId(1).Build(),
            new TestEntityBuilder().WithId(2).Build(),
            new TestEntityBuilder().WithId(3).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Entities.AsQueryable();
        var result = await query.ToPagedListAsync(e => e.Id, new PaginationParametersBuilder().WithPage(1).WithPageSize(2).Build(), countTotal: false);

        result.Count.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ApplyFilter_EmptyParameters_DoesNotAddWhereClause()
    {
        var query = Enumerable.Empty<TypeEntity>().AsQueryable();
        var parameters = new FilterParameters { Value = "" };
        var filtered = query.ApplyFilter(parameters);

        filtered.Expression.Should().BeSameAs(query.Expression);
    }

    [Fact]
    public void ApplyFilter_ExceedsMaxStringLength_ThrowsArgumentException()
    {
        var query = Enumerable.Empty<TypeEntity>().AsQueryable();
        var parameters = new FilterParameters { Value = new string('a', 1500) };
        Action act = () => query.ApplyFilter(parameters);
        act.Should().Throw<ArgumentException>().WithMessage("*exceeds the maximum allowed length*");
    }

    [Fact]
    public async Task SortBy_WithLeadingSpace_ThrowsArgumentException()
    {
        var ctx = GetContext();
        var query = ctx.Set<TypeEntity>().AsQueryable();

        var act = () => query.Keyset(new CursorPaginationParameters()).SortBy(new SortParameters { Value = " asc" });
        act.Should().Throw<ArgumentException>().WithMessage("*not found*");
    }

    [Fact]
    public void ApplyFilter_NullParameters_ReturnsSource()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var result = query.ApplyFilter(default);
        result.Should().BeSameAs(query);
    }

    [Fact]
    public void ApplySort_NullParameters_ReturnsSource()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var result = query.ApplySort(default);
        result.Should().BeSameAs(query);
    }

    [Fact]
    public async Task ApplySort_NullSortBy_NullDefault_ReturnsSource()
    {
        using var context = GetContext();
        await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).Build());
        await context.SaveChangesAsync();
        var source = context.Entities.AsQueryable();
        var sortBy = new SortParameters();

        var result = source.ApplySort(sortBy, SortDirection.Ascending, null, null);

        result.Should().BeSameAs(source);
    }

    [Fact]
    public async Task ParseCursor_LegacyV1_IsParsed()
    {
        using var context = GetContext();
        var v1Cursor = HmacCursorEncoder.DevelopmentDefault.Encode("5");
        var parameters = new CursorPaginationParametersBuilder().WithAfter(v1Cursor).Build();

        var builder = context.Entities.AsQueryable()
            .Keyset(parameters, acceptLegacyCursors: true)
            .Ascending(x => x.Id);

        var act = async () => await builder.ToCursorPagedListAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task KeysetBuilder_WithEnumColumn_PaginatesForwardAndBackwardCorrectly()
    {
        using var context = GetContext();
        await context.Entities.AddRangeAsync(
            new TestEntityBuilder().WithId(1).WithState(TestState.One).Build(),
            new TestEntityBuilder().WithId(2).WithState(TestState.Two).Build(),
            new TestEntityBuilder().WithId(3).WithState(TestState.Two).Build());
        await context.SaveChangesAsync();

        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(2).Build())
            .Ascending(x => x.StateValue)
            .Ascending(x => x.Id);

        var page1 = await builder.ToCursorPagedListAsync();
        page1.Count.Should().Be(2);
        page1[0].Id.Should().Be(1);
        page1[1].Id.Should().Be(2);

        var builder2 = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(2).WithAfter(page1.EndCursor).Build())
            .Ascending(x => x.StateValue)
            .Ascending(x => x.Id);

        var page2 = await builder2.ToCursorPagedListAsync();
        page2.Count.Should().Be(1);
        page2[0].Id.Should().Be(3);
    }

    [Fact]
    public async Task ParameterReplacer_WithNestedParameter_ThrowsInvalidOperationException()
    {
        using var context = GetContext();
        await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
        await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
        await context.SaveChangesAsync();

        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParameters(), acceptLegacyCursors: true)
            .Ascending(x => x.Id)
            .Ascending(x => x.Name.Select(c => c).FirstOrDefault());

        var e = await Record.ExceptionAsync(() => builder.ToCursorPagedListAsync(x => x.Id));
        e.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_Works()
    {
        using var context = GetContext();
        await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
        await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
        await context.SaveChangesAsync();

        var list1 = new List<TestEntity>();
        await foreach (var item in context.Entities.AsQueryable().ToPagedAsyncEnumerable(new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build()))
        {
            list1.Add(item);
        }
        list1.Count.Should().Be(2);
    }

    private sealed class ThrowingPaginationOptions : PaginationCoreOptions
    {
        public new int DeepOffsetWarningThreshold => throw new InvalidOperationException("Simulated failure in options resolution");
    }

    [Fact]
    public async Task ValidateAndApplyOffsetLimits_SwallowsExceptionFromThrowingOptions()
    {
        using var context = GetContext();
        var options = new ThrowingPaginationOptions();
        var result = await context.Entities.AsQueryable().ToPagedListAsync(PaginationParameters.Create(1, 10), options: options);
        result.Should().NotBeNull();
    }

    private sealed class ThrowingInfrastructureQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>, IQueryProvider, Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<IServiceProvider>
    {
        public IServiceProvider Instance => throw new InvalidOperationException("Simulated DI failure");
        public Type ElementType => typeof(T);
        public System.Linq.Expressions.Expression Expression { get; }
        public IQueryProvider Provider => this;

        public ThrowingInfrastructureQueryable()
        {
            Expression = System.Linq.Expressions.Expression.Constant(this);
        }

        public ThrowingInfrastructureQueryable(System.Linq.Expressions.Expression expression)
        {
            Expression = expression;
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new ThrowingInfrastructureQueryable<T>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new ThrowingInfrastructureQueryable<TElement>(expression);
        public object? Execute(System.Linq.Expressions.Expression expression) => default;
        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => default!;

        public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new EmptyAsyncEnumerator();

        private sealed class EmptyAsyncEnumerator : IAsyncEnumerator<T>
        {
            public T Current => default!;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);
        }
    }

    [Fact]
    public async Task ToPagedListAsync_WhenInfrastructureThrows_SwallowsExceptionAndAppliesLimits()
    {
        var queryable = new ThrowingInfrastructureQueryable<TestEntity>();
        Func<Task> act = async () => await queryable.ToPagedListAsync(new PaginationParametersBuilder().WithPage(1).WithPageSize(100).Build(), maxPageSize: 50, countTotal: false);
        await act.Should().NotThrowAsync();
    }
}
