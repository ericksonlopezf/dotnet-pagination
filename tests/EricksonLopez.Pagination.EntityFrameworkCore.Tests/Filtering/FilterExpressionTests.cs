// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class FilterExpressionTests
{
    private enum TestStatus { Active, Inactive }

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public TestStatus Status { get; set; }
        public char Initial { get; set; }
    }

    [Theory]
    [InlineData("=", "5", 5)]
    [InlineData("=", "10", 10)]
    public void Build_OperatorEqual_Works(string op, string value, int expectedId)
    {
        var parameters = FilterParameters.From($"Id{op}{value}");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Id = expectedId }).Should().BeTrue();
        func(new TestEntity { Id = expectedId + 1 }).Should().BeFalse();
    }

    [Fact]
    public void Build_OperatorNotEqual_Works()
    {
        var parameters = FilterParameters.From("Id!=5");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Id = 5 }).Should().BeFalse();
        func(new TestEntity { Id = 6 }).Should().BeTrue();
    }

    [Fact]
    public void Build_OperatorGreaterThan_Works()
    {
        var parameters = FilterParameters.From("Id>5");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Id = 5 }).Should().BeFalse();
        func(new TestEntity { Id = 6 }).Should().BeTrue();
    }

    [Fact]
    public void Build_OperatorGreaterThanOrEqual_Works()
    {
        var parameters = FilterParameters.From("Id>=5");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Id = 4 }).Should().BeFalse();
        func(new TestEntity { Id = 5 }).Should().BeTrue();
    }

    [Fact]
    public void Build_OperatorLessThan_Works()
    {
        var parameters = FilterParameters.From("Id<5");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Id = 5 }).Should().BeFalse();
        func(new TestEntity { Id = 4 }).Should().BeTrue();
    }

    [Fact]
    public void Build_OperatorLessThanOrEqual_Works()
    {
        var parameters = FilterParameters.From("Id<=5");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Id = 6 }).Should().BeFalse();
        func(new TestEntity { Id = 5 }).Should().BeTrue();
    }

    [Fact]
    public void Build_OperatorContains_Works()
    {
        var parameters = FilterParameters.From("Name~=Alice");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Name = "AliceSmith" }).Should().BeTrue();
        func(new TestEntity { Name = "Bob" }).Should().BeFalse();
    }

    [Fact]
    public void Build_OperatorStartsWith_Works()
    {
        var parameters = FilterParameters.From("Name^=Alice");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Name = "AliceSmith" }).Should().BeTrue();
        func(new TestEntity { Name = "SmithAlice" }).Should().BeFalse();
    }

    [Fact]
    public void Build_OperatorEndsWith_Works()
    {
        var parameters = FilterParameters.From("Name$=Alice");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Name = "SmithAlice" }).Should().BeTrue();
        func(new TestEntity { Name = "AliceSmith" }).Should().BeFalse();
    }

    [Fact]
    public void Build_WithUnknownField_IgnoreBehavior_Ignores()
    {
        var parameters = FilterParameters.From("Unknown=5");
        var expr = FilterExpression.Build<TestEntity>(parameters, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        
        // When ignored and it's the only one, it returns null expression
        expr.Should().BeNull();
    }
    
    [Fact]
    public void Build_WithUnknownField_ThrowBehavior_Throws()
    {
        var parameters = FilterParameters.From("Unknown=5");
        var act = () => FilterExpression.Build<TestEntity>(parameters, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_NoOperatorFound_ThrowsByDefault()
    {
        var parameters = FilterParameters.From("Id+");
        var act = () => FilterExpression.Build<TestEntity>(parameters, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_InvalidValueType_WithThrowBehavior_ThrowsArgumentException()
    {
        var parameters = FilterParameters.From("Id=abc");
        var act = () => FilterExpression.Build<TestEntity>(parameters, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Build_EmptyParameters_ReturnsNull()
    {
        var parameters = FilterParameters.Empty;
        var expr = FilterExpression.Build<TestEntity>(parameters);
        expr.Should().BeNull();
    }
    
    [Fact]
    public void Build_StripsQuotesFromStringValue()
    {
        var parameters = FilterParameters.From("Name=\"Alice\"");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Name = "Alice" }).Should().BeTrue();
        func(new TestEntity { Name = "\"Alice\"" }).Should().BeFalse();
    }

    [Fact]
    public void Build_ParsesEnumCaseInsensitive()
    {
        var parameters = FilterParameters.From("Status=active");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Status = TestStatus.Active }).Should().BeTrue();
        func(new TestEntity { Status = TestStatus.Inactive }).Should().BeFalse();
    }

    [Fact]
    public void Build_FallsBackToChangeType()
    {
        var parameters = FilterParameters.From("Initial=A");
        var expr = FilterExpression.Build<TestEntity>(parameters);
        var func = expr!.Compile();

        func(new TestEntity { Initial = 'A' }).Should().BeTrue();
        func(new TestEntity { Initial = 'B' }).Should().BeFalse();
    }

    [Fact]
    public void Build_MaxComplexity_ThrowsIfExceeded()
    {
        var clauses = Enumerable.Range(1, 21).Select(i => $"Id={i}").ToArray();
        var filterString = string.Join(",", clauses);
        var parameters = FilterParameters.From(filterString);
        Action act = () => FilterExpression.Build<TestEntity>(parameters);
        // FIX-05: Changed from InvalidOperationException to ArgumentOutOfRangeException
        // so that standard middleware can catch it as an ArgumentException subclass.
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*maximum allowed complexity*");
    }

    [Fact]
    public void Build_MaxComplexity_DoesNotThrowIfExactlyAtThreshold()
    {
        var clauses = Enumerable.Range(1, 20).Select(i => $"Id={i}").ToArray();
        var filterString = string.Join(",", clauses);
        var parameters = FilterParameters.From(filterString);
        FilterExpression.Build<TestEntity>(parameters).Should().NotBeNull();
    }

    [Theory]
    [InlineData("Name=\"John\"", "John")]
    [InlineData("Name=\"\"", "")]
    [InlineData("Name=\"J\"", "J")]
    [InlineData("Name=J\"", "J\"")] // Does not have starting quote
    [InlineData("Name=\"J", "\"J")] // Does not have ending quote
    public void Build_StringQuotes_ProperlyStripsQuotes(string filterString, string expectedName)
    {
        var parameters = FilterParameters.From(filterString);
        var expr = FilterExpression.Build<TestEntity>(parameters);
        
        var func = expr!.Compile();
        
        var entity = new TestEntity { Name = expectedName };
        func(entity).Should().BeTrue();
    }
}

