// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class FilterExpressionAdditionalTests
{
    private enum TestStatus { Alpha, Beta }
    
    private sealed class ComplexEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public long BigId { get; set; }
        public double Score { get; set; }
        public float Rating { get; set; }
        public decimal Price { get; set; }
        public Guid Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateOnly DateOnly { get; set; }
        public TimeSpan Duration { get; set; }
        public TestStatus Status { get; set; }
        public int? NullableInt { get; set; }
    }

    [Fact]
    public void Build_WithOrClauses_Works()
    {
        var parameters = FilterParameters.From("Id=1|Id=2");
        var expr = FilterExpression.Build<ComplexEntity>(parameters);
        var func = expr!.Compile();

        func(new ComplexEntity { Id = 1 }).Should().BeTrue();
        func(new ComplexEntity { Id = 2 }).Should().BeTrue();
        func(new ComplexEntity { Id = 3 }).Should().BeFalse();
    }

    [Fact]
    public void Build_WithAndClauses_Works()
    {
        var parameters = FilterParameters.From("Id>1,Id<5");
        var expr = FilterExpression.Build<ComplexEntity>(parameters);
        var func = expr!.Compile();

        func(new ComplexEntity { Id = 1 }).Should().BeFalse();
        func(new ComplexEntity { Id = 3 }).Should().BeTrue();
        func(new ComplexEntity { Id = 5 }).Should().BeFalse();
    }

    [Fact]
    public void Build_WithQuotes_StripsQuotes()
    {
        var parameters = FilterParameters.From("Name=\"Alice\"");
        var expr = FilterExpression.Build<ComplexEntity>(parameters);
        var func = expr!.Compile();

        func(new ComplexEntity { Name = "Alice" }).Should().BeTrue();
        func(new ComplexEntity { Name = "\"Alice\"" }).Should().BeFalse();
    }

    [Fact]
    public void Build_CaseInsensitivePropertyMatch_Works()
    {
        var parameters = FilterParameters.From("name=Alice");
        var expr = FilterExpression.Build<ComplexEntity>(parameters);
        var func = expr!.Compile();

        func(new ComplexEntity { Name = "Alice" }).Should().BeTrue();
    }

    [Fact]
    public void Build_Negation_Works()
    {
        var parameters = FilterParameters.From("!Id=5");
        var expr = FilterExpression.Build<ComplexEntity>(parameters);
        var func = expr!.Compile();

        func(new ComplexEntity { Id = 5 }).Should().BeFalse();
        func(new ComplexEntity { Id = 6 }).Should().BeTrue();
    }
    
    [Fact]
    public void Build_NegationOnStringMethods_Works()
    {
        var parameters = FilterParameters.From("!Name~=Alice");
        var expr = FilterExpression.Build<ComplexEntity>(parameters);
        var func = expr!.Compile();

        func(new ComplexEntity { Name = "Bob" }).Should().BeTrue();
        func(new ComplexEntity { Name = "AliceSmith" }).Should().BeFalse();
    }

    [Fact]
    public void Build_TypeCoercion_Bool()
    {
        var parameters = FilterParameters.From("IsActive=true");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { IsActive = true }).Should().BeTrue();
        func(new ComplexEntity { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void Build_TypeCoercion_Long()
    {
        var parameters = FilterParameters.From("BigId=10000000000");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { BigId = 10000000000 }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_Double()
    {
        var parameters = FilterParameters.From("Score=1.5");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { Score = 1.5 }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_Float()
    {
        var parameters = FilterParameters.From("Rating=2.5");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { Rating = 2.5f }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_Decimal()
    {
        var parameters = FilterParameters.From("Price=10.99");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { Price = 10.99m }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_Guid()
    {
        var guid = Guid.NewGuid();
        var parameters = FilterParameters.From($"Token={guid}");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { Token = guid }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_DateTime()
    {
        var parameters = FilterParameters.From("CreatedAt=2023-01-01T00:00:00Z");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) }).Should().BeTrue();
    }
    
    [Fact]
    public void Build_TypeCoercion_DateTimeOffset()
    {
        var parameters = FilterParameters.From("UpdatedAt=2023-01-01T00:00:00Z");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { UpdatedAt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) }).Should().BeTrue();
    }
    
    [Fact]
    public void Build_TypeCoercion_DateOnly()
    {
        var parameters = FilterParameters.From("DateOnly=2023-01-01");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { DateOnly = new DateOnly(2023, 1, 1) }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_TimeSpan()
    {
        var parameters = FilterParameters.From("Duration=01:30:00");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { Duration = new TimeSpan(1, 30, 0) }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_Enum()
    {
        var parameters = FilterParameters.From("Status=Beta");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { Status = TestStatus.Beta }).Should().BeTrue();
    }

    [Fact]
    public void Build_TypeCoercion_Nullable()
    {
        var parameters = FilterParameters.From("NullableInt=5");
        var func = FilterExpression.Build<ComplexEntity>(parameters)!.Compile();
        func(new ComplexEntity { NullableInt = 5 }).Should().BeTrue();
        func(new ComplexEntity { NullableInt = null }).Should().BeFalse();
    }

    [Fact]
    public void Build_InvalidValue_IgnoreBehavior_IgnoresClause()
    {
        var parameters = FilterParameters.From("Id=abc");
        var expr = FilterExpression.Build<ComplexEntity>(parameters, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        expr.Should().BeNull();
    }

    [Fact]
    public void Build_UnknownOperator_IgnoreBehavior_IgnoresClause()
    {
        var parameters = FilterParameters.From("Id abc");
        var expr = FilterExpression.Build<ComplexEntity>(parameters, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        expr.Should().BeNull();
    }
    
    [Fact]
    public void Build_StringMethods_OnNonStringProperty_ReturnsNull()
    {
        var parameters = FilterParameters.From("Id~=5");
        var expr = FilterExpression.Build<ComplexEntity>(parameters, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        expr.Should().BeNull();
    }
}

