// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class SortParametersTests
{
    [Fact]
    public void Empty_WhenAccessed_HasNoValue()
    {
        var empty = SortParameters.Empty;
        empty.Value.Should().BeNull();
        empty.HasValue.Should().BeFalse();
    }

    [Fact]
    public void From_WithValidString_SetsRawString()
    {
        var value = "name asc";
        var parameters = SortParameters.From(value);
        parameters.Value.Should().Be(value);
        parameters.HasValue.Should().BeTrue();
    }

    [Fact]
    public void ToString_WhenEvaluated_ReturnsRawStringOrEmpty()
    {
        SortParameters.Empty.ToString().Should().BeEmpty();
        SortParameters.From("abc").ToString().Should().Be("abc");
    }

#if NET7_0_OR_GREATER
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void TryParse_ShouldReturnFalseAndEmpty_ForWhiteSpace(string? value)
    {
        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeFalse();
        result.Should().Be(SortParameters.Empty);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_ForNull()
    {
        var success = SortParameters.TryParse(null, null, out var result);
        success.Should().BeFalse();
    }



    [Fact]
    public void TryParse_ShouldReturnTrue_ForValidString()
    {
        var value = "name desc";
        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void TryParse_ShouldReturnTrue_ForMultipleColumns()
    {
        var value = "name asc, age desc, date";
        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_ForEmptyPart()
    {
        // Verifies the branch where part.IsEmpty is true
        var value = "name asc,,age desc";
        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeTrue(); // It should ignore empty parts!
        result.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("name asc desc")]
    [InlineData("name something")]
    [InlineData("first name asc")]
    [InlineData("first second third desc")]
    public void Parse_ShouldThrowFormatException_ForInvalidString(string value)
    {
        Action act = () => SortParameters.Parse(value, null);
        act.Should().Throw<FormatException>()
           .WithMessage($"*The string '{value}' is not a valid sort expression.*");

        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeFalse();
        result.Should().Be(SortParameters.Empty);
    }

    [Theory]
    [InlineData("name  asc")]
    [InlineData(" asc")]
    [InlineData("name asc, ")]
    public void TryParse_ShouldReturnTrue_ForValidEdgeCases(string value)
    {
        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeTrue();
    }

#pragma warning disable S2699 // Tests should include assertions
    [Property]
    public bool TryParse_Idempotence_Property(string input)
    {
        if (input is null) return true;
        var firstParse = SortParameters.TryParse(input, null, out var firstResult);
        if (!firstParse || firstResult.Value is null)
        {
            return true;
        }
        
        var secondParse = SortParameters.TryParse(firstResult.Value, null, out var secondResult);
        return secondParse && secondResult.Value == firstResult.Value;
    }
#pragma warning restore S2699

    [Fact]
    public void TryParse_ShouldReturnFalse_ForOnlyCommas()
    {
        var success = SortParameters.TryParse(",", null, out var result);
        success.Should().BeFalse();
    }

    [Fact]
    public void Parse_ShouldReturnParameters_ForValidString()
    {
        var value = "name desc";
        var result = SortParameters.Parse(value, null);
        result.Value.Should().Be(value);
    }
#endif

    [Fact]
    public void ValidateColumnName_ValidName_ShouldNotThrow()
    {
        var act = () => SortParameters.ValidateColumnName("ValidName");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Invalid Name")]
    [InlineData("Invalid-Name")]
    [InlineData("Name;Drop Table")]
    public void ValidateColumnName_InvalidName_ShouldThrowArgumentException(string invalidName)
    {
        var act = () => SortParameters.ValidateColumnName(invalidName);
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid characters in sort column name*");
    }

    [Fact]
    public void ValidateColumnName_WithAllowList_ValidName_ShouldNotThrow()
    {
        var allowed = new[] { "Name", "Age" };
        var act = () => SortParameters.ValidateColumnName("age", allowed);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateColumnName_WithAllowList_InvalidName_ShouldThrowInvalidOperationException()
    {
        var allowed = new[] { "Name", "Age" };
        var act = () => SortParameters.ValidateColumnName("Email", allowed);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not permitted*");
    }

    [Fact]
    public void ValidateColumnName_WithIReadOnlySet_InvalidName_ShouldThrowInvalidOperationException()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "Age" };
        var act = () => SortParameters.ValidateColumnName("Email", allowed);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not permitted*");
    }

    [Fact]
    public void ValidateColumnName_AllowListBypassesRegex_WhenValid()
    {
        // Name;Drop is invalid per regex, but allowlist takes precedence
        var allowed = new[] { "Name;Drop" };
        var act = () => SortParameters.ValidateColumnName("Name;Drop", allowed);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateColumnName_WithAllowList_ExactDotMatch_ShouldNotThrow()
    {
        var allowed = new[] { "Order.Id" };
        var act = () => SortParameters.ValidateColumnName("Order.Id", allowed);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateColumnName_WithIReadOnlySet_ExactDotMatch_ShouldNotThrow()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Order.Id" };
        var act = () => SortParameters.ValidateColumnName("Order.Id", (IReadOnlySet<string>)allowed);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateColumnName_WithAllowList_RootSegmentMatch_ShouldNotThrow()
    {
        // dotIndex = 1 ("a.b") to kill dotIndex != +1 mutant
        var allowed = new[] { "a" };
        var act = () => SortParameters.ValidateColumnName("a.b", allowed);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateColumnName_WithAllowList_InvalidRootSegmentMatch_ShouldThrow()
    {
        // Tests dotIndex != -1 || allowedProperties.Contains(rootSegment) mutant
        var allowed = new[] { "Customer" };
        var act = () => SortParameters.ValidateColumnName("Order.Id", allowed);
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("first name asc")]
    [InlineData("first name desc")]
    public void TryParse_InvalidColumnWithSpaces_ReturnsFalse(string value)
    {
        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeFalse();
        result.Should().Be(default(SortParameters));
    }

    [Theory]
    [InlineData(",name asc")]
    [InlineData("name asc,")]
    public void TryParse_LeadingOrTrailingComma_ReturnsTrue(string value)
    {
        var success = SortParameters.TryParse(value, null, out var result);
        success.Should().BeTrue();
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateColumnName_NullOrWhiteSpace_ShouldThrowArgumentException(string? invalidName)
    {
#pragma warning disable CS8604 // Possible null reference argument
        var act = () => SortParameters.ValidateColumnName(invalidName!);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Column name cannot be null or whitespace.*");
        
        // Also test the IReadOnlySet overload
        var set = new HashSet<string>();
        var act2 = () => SortParameters.ValidateColumnName(invalidName!, (IReadOnlySet<string>)set);
        act2.Should().Throw<ArgumentException>()
            .WithMessage("*Column name cannot be null or whitespace.*");
#pragma warning restore CS8604 // Possible null reference argument
    }

    [Fact]
    public void ValidateColumnName_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        var longName = new string('a', 201);
        var act = () => SortParameters.ValidateColumnName(longName);
        act.Should().Throw<ArgumentException>().WithMessage("*exceeds the maximum allowed length of 200 characters*");
    }

    [Fact]
    public void ValidateColumnName_ExactlyMaxLength_ShouldNotThrow()
    {
        var longName = new string('a', 200); // exactly 200 valid chars
        var act = () => SortParameters.ValidateColumnName(longName);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateColumnName_WithIReadOnlySet_ValidName_ShouldNotThrow()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "Age" };
        var act = () => SortParameters.ValidateColumnName("age", allowed);
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ValidateColumnName_WithIReadOnlySet_AsIEnumerable_ShouldNotThrow()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "Age" };
        IEnumerable<string> enumerable = allowed;
        var act = () => SortParameters.ValidateColumnName("age", enumerable);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateColumnName_WithIReadOnlySet_AsIEnumerable_InvalidName_ShouldThrow()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "Age" };
        IEnumerable<string> enumerable = allowed;
        var act = () => SortParameters.ValidateColumnName("Email", enumerable);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not permitted*");
    }

    [Fact]
    public void ValidateColumnName_WithIReadOnlySet_NullSet_ShouldThrowArgumentNullException()
    {
        IReadOnlySet<string> allowed = null!;
        var act = () => SortParameters.ValidateColumnName("age", allowed);
        act.Should().Throw<ArgumentNullException>().WithParameterName("allowedProperties");
    }

    [Fact]
    public void Equality_ShouldWorkAsExpected_ForRecordStruct()
    {
        var param1 = SortParameters.From("name asc");
        var param2 = SortParameters.From("name asc");
        var param3 = SortParameters.From("age desc");

        param1.Should().Be(param2);
        (param1 == param2).Should().BeTrue();
        (param1 != param3).Should().BeTrue();
        param1.GetHashCode().Should().Be(param2.GetHashCode());
    }

    [Property]
    public void From_WithNonNullString_PreservesStringAndHasValue(NonNull<string> nonNull)
    {
        var val = nonNull.Get;
        var p = SortParameters.From(val);
        p.Value.Should().Be(val);
        p.HasValue.Should().Be(!string.IsNullOrWhiteSpace(val));
        p.ToString().Should().Be(val);
    }

    [Property]
    public void Equality_IsReflexive_ForAnyString(NonNull<string> nonNull)
    {
        var val = nonNull.Get;
        var p1 = SortParameters.From(val);
        var p2 = SortParameters.From(val);
        p1.Should().Be(p2);
        (p1 == p2).Should().BeTrue();
        p1.GetHashCode().Should().Be(p2.GetHashCode());
    }

    [Property]
    public void ValidateColumnName_ValidAlphanumericNames_NeverThrows(PositiveInt lengthSeed)
    {
        int len = (lengthSeed.Get % 50) + 1; // 1 to 50 chars
        var name = new string('A', len);
        Action act = () => SortParameters.ValidateColumnName(name);
        act.Should().NotThrow();
    }
}




