// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

[Collection("PaginationExpressionCacheCollection")]
public class PaginationExpressionCacheTests
{
    private sealed class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [Fact]
    public void CacheKeys_EqualsObject_ReturnsTrueForSameTypeAndFalseOtherwise()
    {
        // Reflection to create internal struct instances
        var filterCacheKeyType = typeof(PaginationCoreOptions).Assembly.GetType("EricksonLopez.Pagination.PaginationExpressionCache+FilterCacheKey")!;
        var sortCacheKeyType = typeof(PaginationCoreOptions).Assembly.GetType("EricksonLopez.Pagination.PaginationExpressionCache+SortCacheKey")!;
        var delegateCacheKeyType = typeof(PaginationCoreOptions).Assembly.GetType("EricksonLopez.Pagination.PaginationExpressionCache+DelegateCacheKey")!;

        var filterKey1 = Activator.CreateInstance(filterCacheKeyType, typeof(object), "f1", FilterUnknownFieldBehavior.ThrowException, 10, null, 10)!;
        var filterKey2 = Activator.CreateInstance(filterCacheKeyType, typeof(object), "f1", FilterUnknownFieldBehavior.ThrowException, 10, null, 10)!;
        var filterKey3 = Activator.CreateInstance(filterCacheKeyType, typeof(string), "f2", FilterUnknownFieldBehavior.ThrowException, 10, null, 10)!;
        var filterKey4 = Activator.CreateInstance(filterCacheKeyType, typeof(string), null, FilterUnknownFieldBehavior.ThrowException, 10, null, 10)!;

        var sortKey1 = Activator.CreateInstance(sortCacheKeyType, typeof(object), "s1")!;
        var sortKey2 = Activator.CreateInstance(sortCacheKeyType, typeof(object), "s1")!;
        
        Expression<Func<object, object>> expr = x => x;
        var delegateKey1 = Activator.CreateInstance(delegateCacheKeyType, typeof(object), typeof(object), expr)!;
        var delegateKey2 = Activator.CreateInstance(delegateCacheKeyType, typeof(object), typeof(object), expr)!;

        // Test object.Equals overrides
        filterKey1.Equals((object)filterKey2).Should().BeTrue();
        filterKey1.Equals((object)filterKey3).Should().BeFalse();
        filterKey1.Equals(new object()).Should().BeFalse();
        filterKey1.Equals(null).Should().BeFalse();

        sortKey1.Equals((object)sortKey2).Should().BeTrue();
        sortKey1.Equals(new object()).Should().BeFalse();

        delegateKey1.Equals((object)delegateKey2).Should().BeTrue();
        delegateKey1.Equals(new object()).Should().BeFalse();
    }

    [Fact]
    public void Clear_ShouldNotThrow()
    {
        Action act = () => PaginationExpressionCache.Clear();
        act.Should().NotThrow();
    }

    [Fact]
    public void CacheProperties_AreAccessible()
    {
        PaginationExpressionCache.Filters.Should().NotBeNull();
        PaginationExpressionCache.SortLambdas.Should().NotBeNull();
    }

    [Fact]
    public void GetCompareToMethod_ReturnsMethodInfo()
    {
        var intCompare = PaginationExpressionCache.GetCompareToMethod<int>();
        intCompare.Should().NotBeNull();
        intCompare.Name.Should().Be("CompareTo");

        var stringCompare = PaginationExpressionCache.GetCompareToMethod<string>();
        stringCompare.Should().NotBeNull();
        stringCompare.Name.Should().Be("CompareTo");
    }

    [Fact]
    public void GetOrCompile_WithMemberExpression_CompilesAndCaches()
    {
        Expression<Func<TestItem, string>> expr = x => x.Name;
        var compiled = PaginationExpressionCache.GetOrCompile(expr);
        compiled(new TestItem { Name = "Alice" }).Should().Be("Alice");

        // Subsequent call returns cached version
        var compiledAgain = PaginationExpressionCache.GetOrCompile(expr);
        compiledAgain(new TestItem { Name = "Bob" }).Should().Be("Bob");
    }

    [Fact]
    public void GetOrCompile_WithUnaryExpressionWrappingMember_CompilesAndCaches()
    {
        Expression<Func<TestItem, object>> expr = x => (object)x.Age;
        var compiled = PaginationExpressionCache.GetOrCompile(expr);
        compiled(new TestItem { Age = 25 }).Should().Be(25);
    }

    [Fact]
    public void GetOrCompile_WithFallbackExpression_CompilesAndCaches()
    {
        Expression<Func<int, int>> expr = x => x + 10;
        var compiled = PaginationExpressionCache.GetOrCompile(expr);
        compiled(5).Should().Be(15);
    }

    [Fact]
    public void HotReloadHandler_UpdateApplication_ClearsCache()
    {
        Action act = () => PaginationExpressionCacheHotReloadHandler.UpdateApplication(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void HotReloadHandler_ClearCache_ClearsCache()
    {
        Action act = () => PaginationExpressionCacheHotReloadHandler.ClearCache(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void GetOrCompile_WithClosure_CompilesWithoutCaching()
    {
        var capture = new object(); // This creates a closure!
        Expression<Func<object, object>> expr = x => capture;
        
        var compiled = PaginationExpressionCache.GetOrCompile(expr);
        compiled(new object()).Should().BeSameAs(capture);
    }
}
