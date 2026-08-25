// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Internal;
using Xunit;

namespace EricksonLopez.Pagination.Tests.Internal;

public class FilterDslParserTests
{
    [Theory]
    [InlineData("Name~=John", "Name", (int)FilterOp.Contains, "John", false)]
    [InlineData("!Name~=John", "Name", (int)FilterOp.Contains, "John", true)]
    [InlineData("Age>=18", "Age", (int)FilterOp.GreaterThanOrEqual, "18", false)]
    [InlineData("Score<=100", "Score", (int)FilterOp.LessThanOrEqual, "100", false)]
    [InlineData("Prefix^=Pre", "Prefix", (int)FilterOp.StartsWith, "Pre", false)]
    [InlineData("Suffix$=Suf", "Suffix", (int)FilterOp.EndsWith, "Suf", false)]
    [InlineData("Rank>5", "Rank", (int)FilterOp.GreaterThan, "5", false)]
    [InlineData("Rank<10", "Rank", (int)FilterOp.LessThan, "10", false)]
    [InlineData("Status!=Active", "Status", (int)FilterOp.NotEqual, "Active", false)]
    [InlineData("Status=Active", "Status", (int)FilterOp.Equal, "Active", false)]
    [InlineData("Field=\"Quoted\"", "Field", (int)FilterOp.Equal, "Quoted", false)]
    [InlineData("Field=\"Quoted \\\"with\\\" quotes\"", "Field", (int)FilterOp.Equal, "Quoted \"with\" quotes", false)]
    [InlineData("Field=Val%7C1", "Field", (int)FilterOp.Equal, "Val|1", false)]
    [InlineData("Field=\"%7C\"", "Field", (int)FilterOp.Equal, "|", false)]
    [InlineData("Field=\"\"", "Field", (int)FilterOp.Equal, "", false)]
    [InlineData("Field=\"abc", "Field", (int)FilterOp.Equal, "\"abc", false)]
    [InlineData("Field=abc\"", "Field", (int)FilterOp.Equal, "abc\"", false)]
    public void Parse_ValidSegments_ReturnsFilterClause(string segment, string expectedField, object expectedOp, string expectedValue, bool expectedNegate)
    {
        var result = FilterDslParser.Parse(segment, FilterUnknownFieldBehavior.ThrowException);
        
        result.Should().NotBeNull();
        result!.Value.FieldName.Should().Be(expectedField);
        ((int)result.Value.Op).Should().Be((int)expectedOp);
        result.Value.Value.Should().Be(expectedValue);
        result.Value.Negate.Should().Be(expectedNegate);
    }

    [Fact]
    public void Parse_InvalidOperator_ThrowsArgumentException_WhenBehaviorIsThrow()
    {
        Action act = () => FilterDslParser.Parse("Name??John", FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("Invalid filter segment: 'Name??John'. Unknown operator.");
    }

    [Fact]
    public void Parse_InvalidOperator_ReturnsNull_WhenBehaviorIsIgnore()
    {
        var result = FilterDslParser.Parse("Name??John", FilterUnknownFieldBehavior.Ignore);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_FieldNameExceedsMaxLength_ThrowsArgumentException_WhenBehaviorIsThrow()
    {
        var longFieldName = new string('A', 129);
        Action act = () => FilterDslParser.Parse($"{longFieldName}=Value", FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage("Invalid filter segment: property name exceeds maximum allowed length of 128 characters.");
    }

    [Fact]
    public void Parse_FieldNameExactlyMaxLength_ParsesSuccessfully()
    {
        var longFieldName = new string('A', 128);
        var result = FilterDslParser.Parse($"{longFieldName}=Value", FilterUnknownFieldBehavior.ThrowException);
        result.Should().NotBeNull();
        result!.Value.FieldName.Should().Be(longFieldName);
    }

    [Fact]
    public void Parse_FieldNameExceedsMaxLength_ReturnsNull_WhenBehaviorIsIgnore()
    {
        var longFieldName = new string('A', 129);
        var result = FilterDslParser.Parse($"{longFieldName}=Value", FilterUnknownFieldBehavior.Ignore);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptySegment_ReturnsNull()
    {
        var result = FilterDslParser.Parse("", FilterUnknownFieldBehavior.ThrowException);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_OnlyOperators_ReturnsNull()
    {
        var result = FilterDslParser.Parse(">=", FilterUnknownFieldBehavior.ThrowException);
        result.Should().BeNull();
    }
}


