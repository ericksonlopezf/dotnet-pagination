// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class ExpressionCacheTests
{
    public class DummyEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void GetOrCompile_ReturnsSameDelegateForSameExpression()
    {
        // Arrange
        Expression<Func<DummyEntity, int>> expression = e => e.Id;

        // Act
        var compiled1 = PaginationExpressionCache.GetOrCompile(expression);
        var compiled2 = PaginationExpressionCache.GetOrCompile(expression);

        // Assert
        compiled1.Should().BeSameAs(compiled2);
        
        var entity = new DummyEntity { Id = 42 };
        compiled1(entity).Should().Be(42);
    }


    [Fact]
    public void GetOrCompile_DifferentPropertyTypes_ProduceCorrectResults()
    {
        Expression<Func<DummyEntity, int>> idExpr = e => e.Id;
        Expression<Func<DummyEntity, string>> nameExpr = e => e.Name;

        var idFunc = PaginationExpressionCache.GetOrCompile(idExpr);
        var nameFunc = PaginationExpressionCache.GetOrCompile(nameExpr);

        var entity = new DummyEntity { Id = 7, Name = "audit" };
        idFunc(entity).Should().Be(7);
        nameFunc(entity).Should().Be("audit");
    }
}



