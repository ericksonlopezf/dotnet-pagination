// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Internal;
using Xunit;

namespace EricksonLopez.Pagination.Tests.Internal;

public class FilterPredicateBuilderTests
{
    private class DummyModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? NullableAge { get; set; }
        public NestedModel Nested { get; set; } = new();
    }

    private class NestedModel
    {
        public string City { get; set; } = string.Empty;
    }

    private class FilterableModel
    {
        [Filterable]
        public int AllowedId { get; set; }
        
        public int DeniedId { get; set; }
        
        [Filterable]
        public FilterableNestedModel Nested { get; set; } = new();
    }

    private class FilterableNestedModel
    {
        [Filterable]
        public string AllowedCity { get; set; } = string.Empty;
    }

    private class DeepNested
    {
        public DeepNested N1 { get; set; } = default!;
        public DeepNested N2 { get; set; } = default!;
        public DeepNested N3 { get; set; } = default!;
        public DeepNested N4 { get; set; } = default!;
        public DeepNested N5 { get; set; } = default!;
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public void Build_DepthExceedsConfiguredMax_ThrowsInvalidOperationException()
    {
        var filter = new FilterParameters { Value = "N1.N2.N3.N4.N5.Value=1" };
        var act = () => FilterExpression.Build<DeepNested>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<InvalidOperationException>().WithMessage("*exceeds the maximum allowed nesting depth*");
    }

    [Fact]
    public void Build_CustomMaxPropertyDepth_RespectsConfiguredLimit()
    {
        var filter = new FilterParameters { Value = "N1.N2.N3.N4=1" };
        
        // With depth=3: 4 parts > 3 → should throw an InvalidOperationException about depth
        var actTooDeep = () => FilterExpression.Build<DeepNested>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException, maxPropertyDepth: 3);
        actTooDeep.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds the maximum allowed nesting depth of 3*");

        // With depth=6: 4 parts < 6 → depth check should be bypassed.
        // allowedProperties to bypass Filterable check
        var allowedProps = new[] { "N1.N2.N3.N4" };
        var actAllowed = () => FilterExpression.Build<DeepNested>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException, allowedProperties: allowedProps, maxPropertyDepth: 6);
        actAllowed.Should().NotThrow<InvalidOperationException>(because: "the depth limit of 6 is not exceeded by a 4-segment path");
    }

    [Fact]
    public void Build_EmptySegment_ThrowsArgumentException()
    {
        var filter = new FilterParameters { Value = "Nested..City=1" };
        var act = () => FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("*empty segment*");
    }

    [Fact]
    public void Build_InvalidCharacters_ThrowsArgumentException()
    {
        var filter = new FilterParameters { Value = "N@me=1" };
        var act = () => FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown operator*");
    }

    [Fact]
    public void Build_SegmentTooLong_ThrowsInvalidOperationException()
    {
        var longName = new string('A', 101);
        var filter = new FilterParameters { Value = $"{longName}=1" };
        var act = () => FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<InvalidOperationException>().WithMessage("*exceeds maximum length*");
    }

    [Fact]
    public void Build_PropertyNotFound_ThrowBehavior_ThrowsArgumentException()
    {
        var filter = new FilterParameters { Value = "MissingProp=1" };
        var act = () => FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("*not found*");
    }

    [Fact]
    public void Build_PropertyNotFound_IgnoreBehavior_ReturnsNull()
    {
        var filter = new FilterParameters { Value = "MissingProp=1" };
        var result = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        result.Should().BeNull();
    }

    [Fact]
    public void Build_NestedWithoutRootFilterable_ThrowsInvalidOperationException()
    {
        var filter = new FilterParameters { Value = "Nested.City=London" };
        Action act = () => FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException, allowedProperties: new string[0]);
        act.Should().Throw<InvalidOperationException>().WithMessage("Filtering on property 'Nested.City' is not permitted.");
    }

    [Fact]
    public void Build_MissingFilterableAttribute_ThrowBehavior_ThrowsArgumentException()
    {
        var filter = new FilterParameters { Value = "DeniedId=1" };
        var act = () => FilterExpression.Build<FilterableModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("*not allowed to be filtered*");
    }

    [Fact]
    public void Build_MissingFilterableAttribute_IgnoreBehavior_ReturnsNull()
    {
        var filter = new FilterParameters { Value = "DeniedId=1" };
        var result = FilterExpression.Build<FilterableModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        result.Should().BeNull();
    }

    [Fact]
    public void Build_ValueCoercionFails_ThrowBehavior_ThrowsArgumentException()
    {
        var filter = new FilterParameters { Value = "Id=not-an-int" };
        var act = () => FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("*not valid for property*");
    }

    [Fact]
    public void Build_ValueCoercionFails_IgnoreBehavior_ReturnsNull()
    {
        var filter = new FilterParameters { Value = "Id=not-an-int" };
        var result = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        result.Should().BeNull();
    }

    [Fact]
    public void Build_StringContainsOnNonString_ReturnsNull()
    {
        var filter = new FilterParameters { Value = "Id~=1" };
        var result = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        result.Should().BeNull();
    }

    [Fact]
    public void Build_StringOperations_BuildsCorrectExpressions()
    {
        var containsFilter = new FilterParameters { Value = "Name~=John" };
        var containsExpr = FilterExpression.Build<DummyModel>(containsFilter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        containsExpr.Should().NotBeNull();
        containsExpr!.Body.NodeType.Should().Be(ExpressionType.Call);
        
        var startsWithFilter = new FilterParameters { Value = "Name^=Jo" };
        var startsWithExpr = FilterExpression.Build<DummyModel>(startsWithFilter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        startsWithExpr.Should().NotBeNull();
        startsWithExpr!.Body.NodeType.Should().Be(ExpressionType.Call);

        var endsWithFilter = new FilterParameters { Value = "Name$=hn" };
        var endsWithExpr = FilterExpression.Build<DummyModel>(endsWithFilter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        endsWithExpr.Should().NotBeNull();
        endsWithExpr!.Body.NodeType.Should().Be(ExpressionType.Call);
    }
    
    [Fact]
    public void Build_ComparisonOperations_BuildsCorrectExpressions()
    {
        var ops = new[] 
        { 
            ("=", ExpressionType.Equal),
            ("!=", ExpressionType.NotEqual),
            (">", ExpressionType.GreaterThan),
            ("<", ExpressionType.LessThan),
            (">=", ExpressionType.GreaterThanOrEqual),
            ("<=", ExpressionType.LessThanOrEqual),
        };

        foreach (var (op, exprType) in ops)
        {
            var filter = new FilterParameters { Value = $"Id{op}1" };
            var expr = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
            expr.Should().NotBeNull();
            expr!.Body.NodeType.Should().Be(exprType);
        }
    }
    
    [Fact]
    public void Build_NullableType_BuildsCorrectExpression()
    {
        var filter = new FilterParameters { Value = "NullableAge=25" };
        var expr = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        expr.Should().NotBeNull();
        expr!.Body.NodeType.Should().Be(ExpressionType.Equal);
    }

    [Fact]
    public void Build_NullToken_WithEqualOperator_BuildsCorrectExpression()
    {
        var filter = new FilterParameters { Value = "NullableAge=null" };
        var expr = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        expr.Should().NotBeNull();
        expr!.Body.NodeType.Should().Be(ExpressionType.Equal);
    }

    [Fact]
    public void Build_NullToken_WithNotEqualOperator_BuildsCorrectExpression()
    {
        var filter = new FilterParameters { Value = "NullableAge!=null" };
        var expr = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        expr.Should().NotBeNull();
        expr!.Body.NodeType.Should().Be(ExpressionType.NotEqual);
    }

    [Fact]
    public void Build_NullToken_WithInvalidOperator_ThrowBehavior_ThrowsArgumentException()
    {
        var filter = new FilterParameters { Value = "NullableAge>null" };
        var act = () => FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("*Cannot use operator*");
    }

    [Fact]
    public void Build_NullToken_WithInvalidOperator_IgnoreBehavior_ReturnsNull()
    {
        var filter = new FilterParameters { Value = "NullableAge>null" };
        var result = FilterExpression.Build<DummyModel>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        result.Should().BeNull();
    }

    [Fact]
    public void Build_WithUnsupportedOperator_ThrowsInvalidOperationException()
    {
        var param = Expression.Parameter(typeof(DummyModel), "x");
        var clause = new FilterClause
        {
            FieldName = "Id",
            Op = (FilterOp)999,
            Value = "1"
        };
        var act = () => FilterPredicateBuilder.Build<DummyModel>(param, clause, FilterUnknownFieldBehavior.ThrowException, true, 5, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Unsupported filter operator*");
    }
}
