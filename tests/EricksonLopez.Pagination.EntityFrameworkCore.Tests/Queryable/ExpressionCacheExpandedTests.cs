// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class ExpressionCacheExpandedTests
{
    public class Entity { public int Id { get; set; } public string Name { get; set; } = ""; }

    [Fact]
    public void GetOrCompile_ReturnsSameDelegateForSameExpressionInstance()
    {
        Expression<Func<Entity, int>> expression = e => e.Id;

        var compiled1 = PaginationExpressionCache.GetOrCompile(expression);
        var compiled2 = PaginationExpressionCache.GetOrCompile(expression);

        // Same instance of expression → same structural key → same cached delegate
        compiled1.Should().BeSameAs(compiled2);
    }

    [Fact]
    public void GetOrCompile_ReturnsSameDelegateForStructurallyEquivalentExpressions()
    {
        // Two separate lambda instances with the same structure — this is the fix for the
        // ConditionalWeakTable bug: two different object references for "e => e.Id"
        Expression<Func<Entity, int>> expr1 = e => e.Id;
        Expression<Func<Entity, int>> expr2 = e => e.Id;

        // Must be different object references
        ReferenceEquals(expr1, expr2).Should().BeFalse();

        var compiled1 = PaginationExpressionCache.GetOrCompile(expr1);
        var compiled2 = PaginationExpressionCache.GetOrCompile(expr2);

        // The fix: both should return the same cached delegate (structural key equality)
        compiled1.Should().BeSameAs(compiled2);
    }

    [Fact]
    public void GetOrCompile_CompiledDelegate_ReturnsCorrectValue()
    {
        Expression<Func<Entity, int>> expression = e => e.Id;
        var compiled = PaginationExpressionCache.GetOrCompile(expression);
        
        var entity = new Entity { Id = 42 };
        compiled(entity).Should().Be(42);
    }

    [Fact]
    public void GetOrCompile_DifferentExpressions_ReturnDifferentDelegates()
    {
        Expression<Func<Entity, int>> idExpr = e => e.Id;
        Expression<Func<Entity, string>> nameExpr = e => e.Name;

        var compiledId = PaginationExpressionCache.GetOrCompile(idExpr);
        var compiledName = PaginationExpressionCache.GetOrCompile(nameExpr);

        var entity = new Entity { Id = 99, Name = "test" };
        compiledId(entity).Should().Be(99);
        compiledName(entity).Should().Be("test");
    }
}


