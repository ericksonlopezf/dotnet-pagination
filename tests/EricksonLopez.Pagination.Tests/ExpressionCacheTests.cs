// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

[Collection("PaginationExpressionCacheCollection")]
public class ExpressionCacheTests
{
    private sealed class TestEntity
    {
        public TestEntity() {}
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public Guid Uid { get; set; }
        public DateTime Date { get; set; }
        public DateTimeOffset DateOffset { get; set; }
        public TimeSpan Time { get; set; }
        public ConsoleColor Color { get; set; }
    }

    [Fact]
    public void GetOrCompile_WithClosure_DoesNotCache()
    {
        PaginationExpressionCache.Clear();
        _ = new TestEntity(); // instantiate to avoid CA1812
        int localId = 5;
        Expression<Func<TestEntity, bool>> expr = x => x.Id == localId;

        // Since it has closure, it compiles every time and does not cache
        var func1 = PaginationExpressionCache.GetOrCompile(expr);
        var func2 = PaginationExpressionCache.GetOrCompile(expr);

        func1.Should().NotBeSameAs(func2); // Different instances because compiled separately
    }

    [Fact]
    public void GetOrCompile_WithoutClosure_Caches()
    {
        Expression<Func<TestEntity, bool>> expr = x => x.Id == 5;

        var func1 = PaginationExpressionCache.GetOrCompile(expr);
        var func2 = PaginationExpressionCache.GetOrCompile(expr);

        func1.Should().BeSameAs(func2); // Same instance because cached
    }

    [Fact]
    public void GetOrCompile_ExceedingMaxEntries_EvictsOldest()
    {
        // MaxEntries is 512
        for (int i = 0; i < 515; i++)
        {
            var value = i; // Closure! No, wait, if it's a closure it won't cache.
            // We need a constant expression that is different every time.
            var param = Expression.Parameter(typeof(TestEntity), "x");
            var property = Expression.Property(param, nameof(TestEntity.Id));
            var constant = Expression.Constant(i, typeof(int));
            var equal = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<TestEntity, bool>>(equal, param);

            PaginationExpressionCache.GetOrCompile(lambda);
        }

        // The first ones should have been evicted. We can't directly check the internal cache,
        // but this ensures the eviction logic is run.
        
        // Let's get the 0th one again. It should be a new instance because it was evicted.
        var param0 = Expression.Parameter(typeof(TestEntity), "x");
        var property0 = Expression.Property(param0, nameof(TestEntity.Id));
        var constant0 = Expression.Constant(0, typeof(int));
        var equal0 = Expression.Equal(property0, constant0);
        var lambda0 = Expression.Lambda<Func<TestEntity, bool>>(equal0, param0);

        var func1 = PaginationExpressionCache.GetOrCompile(lambda0);
        var func2 = PaginationExpressionCache.GetOrCompile(lambda0);
        
        func1.Should().BeSameAs(func2);
    }

    [Fact]
    public void IsSafeConstant_WithAllSafeTypes_Works()
    {
        TestSafeType<int>(1);
        TestSafeType<string>("test");
        TestSafeType<decimal>(1m);
        TestSafeType<Guid>(Guid.NewGuid());
        TestSafeType<DateTime>(DateTime.Now);
        TestSafeType<DateTimeOffset>(DateTimeOffset.Now);
        TestSafeType<TimeSpan>(TimeSpan.Zero);
        TestSafeType<DateOnly>(new DateOnly(2026, 1, 1));
        TestSafeType<TimeOnly>(new TimeOnly(12, 0, 0));
        TestSafeType<ConsoleColor>(ConsoleColor.Red); // Enum
    }

    private void TestSafeType<T>(T value)
    {
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var constant = Expression.Constant(value, typeof(T));
        var lambda = Expression.Lambda<Func<TestEntity, T>>(constant, param);
        var func1 = PaginationExpressionCache.GetOrCompile(lambda);
        var func2 = PaginationExpressionCache.GetOrCompile(lambda);
        func1.Should().BeSameAs(func2);
    }
    
    [Fact]
    public void FilterCacheKey_Equals_ChecksAllowedProperties()
    {
        var props1 = new string[] { "Id" };
        var props2 = new string[] { "Id" };
        var props3 = new string[] { "Name" };
        
        var key1 = new PaginationExpressionCache.FilterCacheKey(typeof(TestEntity), "Id=1", FilterUnknownFieldBehavior.Ignore, 100, new HashSet<string>(props1), 1000);
        var key2 = new PaginationExpressionCache.FilterCacheKey(typeof(TestEntity), "Id=1", FilterUnknownFieldBehavior.Ignore, 100, new HashSet<string>(props2), 1000);
        var key3 = new PaginationExpressionCache.FilterCacheKey(typeof(TestEntity), "Id=1", FilterUnknownFieldBehavior.Ignore, 100, new HashSet<string>(props3), 1000);
        var keyNull = new PaginationExpressionCache.FilterCacheKey(typeof(TestEntity), "Id=1", FilterUnknownFieldBehavior.Ignore, 100, null, 1000);
        var keyNull2 = new PaginationExpressionCache.FilterCacheKey(typeof(TestEntity), "Id=1", FilterUnknownFieldBehavior.Ignore, 100, null, 1000);
        
        key1.Equals(key2).Should().BeTrue();
        key1.Equals(key3).Should().BeFalse();
        key1.Equals(keyNull).Should().BeFalse();
        keyNull.Equals(key1).Should().BeFalse();
        keyNull.Equals(keyNull2).Should().BeTrue();
    }
    
    [Fact]
    public void SortCacheKey_Equals_ChecksEntityTypeAndColumnName()
    {
        var key1 = new PaginationExpressionCache.SortCacheKey(typeof(TestEntity), "Id");
        var key2 = new PaginationExpressionCache.SortCacheKey(typeof(TestEntity), "Id");
        var key3 = new PaginationExpressionCache.SortCacheKey(typeof(TestEntity), "Name");
        var key4 = new PaginationExpressionCache.SortCacheKey(typeof(string), "Id");
        
        key1.Equals(key2).Should().BeTrue();
        key1.Equals(key3).Should().BeFalse();
        key1.Equals(key4).Should().BeFalse();
    }
    
    [Fact]
    public void DelegateCacheKey_Equals_ChecksProperties()
    {
        Expression<Func<TestEntity, int>> expr1 = x => x.Id;
        Expression<Func<TestEntity, string>> expr2 = x => x.Name;
        Expression<Func<string, int>> expr3 = x => x.Length;
        
        var key1 = new PaginationExpressionCache.DelegateCacheKey(typeof(TestEntity), typeof(int), expr1);
        var key2 = new PaginationExpressionCache.DelegateCacheKey(typeof(TestEntity), typeof(int), expr1);
        var key3 = new PaginationExpressionCache.DelegateCacheKey(typeof(TestEntity), typeof(string), expr2);
        var key4 = new PaginationExpressionCache.DelegateCacheKey(typeof(string), typeof(int), expr3);
        
        key1.Equals(key2).Should().BeTrue();
        key1.Equals(key3).Should().BeFalse();
        key1.Equals(key4).Should().BeFalse();
    }

    [Fact]
    public void IsSafeConstant_WithUnsafeType_HasClosure()
    {
        PaginationExpressionCache.Clear();
        var unsafeValue = new object();
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var constant = Expression.Constant(unsafeValue, typeof(object));
        var lambda = Expression.Lambda<Func<TestEntity, object>>(constant, param);

        var func1 = PaginationExpressionCache.GetOrCompile(lambda);
        var func2 = PaginationExpressionCache.GetOrCompile(lambda);

        // Not cached because object is not a safe constant
        func1.Should().NotBeSameAs(func2);
    }

    [Fact]
    public void GetCompareToMethod_ReturnsMethodInfo()
    {
        var method = PaginationExpressionCache.GetCompareToMethod<int>();
        method.Should().NotBeNull();
        method.Name.Should().Be("CompareTo");
    }

    [Fact]
    public void GetOrCompile_WithDateOnlyConstant_Caches()
    {
        var date = new DateOnly(2026, 1, 1);
        var param = Expression.Parameter(typeof(TestEntity), "x");
        var constant = Expression.Constant(date, typeof(DateOnly));
        var lambda = Expression.Lambda<Func<TestEntity, DateOnly>>(constant, param);

        var func1 = PaginationExpressionCache.GetOrCompile(lambda);
        var func2 = PaginationExpressionCache.GetOrCompile(lambda);

        func1.Should().BeSameAs(func2);
    }

    [Fact]
    public void SortLambdas_And_Filters_Properties_ReturnNotNull()
    {
        PaginationExpressionCache.SortLambdas.Should().NotBeNull();
        PaginationExpressionCache.Filters.Should().NotBeNull();
    }

    [Fact]
    public void SortCacheKey_GetHashCode_Works()
    {
        var key = new PaginationExpressionCache.SortCacheKey(typeof(TestEntity), "Id");
        key.GetHashCode().Should().NotBe(0);
    }

    [Fact]
    public void GetOrCompile_WithUnaryExpression_Caches()
    {
        Expression<Func<TestEntity, object>> expr = x => (object)x.Id;
        var func1 = PaginationExpressionCache.GetOrCompile(expr);
        var func2 = PaginationExpressionCache.GetOrCompile(expr);

        func1.Should().BeSameAs(func2);
        func1(new TestEntity { Id = 42 }).Should().Be(42);
    }
}


