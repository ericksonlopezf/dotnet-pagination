// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class FilterParametersTests
{
    [Fact]
    public void Empty_HasNoValue()
    {
        var p = FilterParameters.Empty;
        p.Value.Should().BeNull();
        p.HasValue.Should().BeFalse();
        p.ToString().Should().BeEmpty();
    }

    [Fact]
    public void From_SetsValue()
    {
        var p = FilterParameters.From("name~=John");
        p.Value.Should().Be("name~=John");
        p.HasValue.Should().BeTrue();
        p.ToString().Should().Be("name~=John");
    }

#if NET7_0_OR_GREATER
    [Fact]
    public void Parse_ValidString_ReturnsParameters()
    {
        var p = FilterParameters.Parse("age>=18", null);
        p.Value.Should().Be("age>=18");
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrueAndParameters()
    {
        var success = FilterParameters.TryParse("age>=18", null, out var p);
        success.Should().BeTrue();
        p.Value.Should().Be("age>=18");
    }
    
    [Theory]
    [InlineData("name=John")]
    [InlineData("name!=John")]
    [InlineData("age>18")]
    [InlineData("age>=18")]
    [InlineData("age<50")]
    [InlineData("age<=50")]
    [InlineData("name~=John")]
    [InlineData("name^=John")]
    [InlineData("name$=John")]
    public void TryParse_ValidStringWithDifferentOperators_ReturnsTrue(string filter)
    {
        var success = FilterParameters.TryParse(filter, null, out var p);
        success.Should().BeTrue();
        p.Value.Should().Be(filter);
    }

    [Fact]
    public void TryParse_ValidStringWithMultipleConditions_ReturnsTrue()
    {
        var value = "name=John,age>18";
        var success = FilterParameters.TryParse(value, null, out var p);
        success.Should().BeTrue();
        p.Value.Should().Be(value);
    }

    [Fact]
    public void TryParse_ValidStringWithOrConditions_ReturnsTrue()
    {
        var value = "name=John|age>18";
        var success = FilterParameters.TryParse(value, null, out var p);
        success.Should().BeTrue();
        p.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("=18")]
    [InlineData("!=18")]
    [InlineData(">18")]
    [InlineData(">=18")]
    [InlineData("<50")]
    [InlineData("<=50")]
    [InlineData("~John")]
    [InlineData("^John")]
    [InlineData("$John")]
    public void TryParse_InvalidSyntax_ReturnsFalse(string filter)
    {
        var success = FilterParameters.TryParse(filter, null, out var p);
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_NullOrEmptyString_ReturnsFalseAndEmpty()
    {
        bool result = FilterParameters.TryParse("", null, out var parameters);
        result.Should().BeFalse();
        parameters.HasValue.Should().BeFalse();
        parameters.Value.Should().BeNull();
    }
    
    [Fact]
    public void TryParse_MissingOperator_ReturnsFalse()
    {
        var success = FilterParameters.TryParse("nameJohn", null, out var p);
        success.Should().BeFalse();
        p.Value.Should().BeNull();
    }

#pragma warning disable S2699 // Tests should include assertions
    [Property]
    public bool TryParse_Idempotence_Property(string input)
    {
        if (input is null) return true;
        var firstParse = FilterParameters.TryParse(input, null, out var firstResult);
        if (!firstParse || firstResult.Value is null)
        {
            return true;
        }
        
        var secondParse = FilterParameters.TryParse(firstResult.Value, null, out var secondResult);
        return secondParse && secondResult.Value == firstResult.Value;
    }
#pragma warning restore S2699

    [Fact]
    public void TryParse_TooLongString_ReturnsFalse()
    {
        var filter = "a=" + new string('b', 4097 - 2); // 4097 chars
        var success = FilterParameters.TryParse(filter, null, out var p);
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_MaximumLengthString_ReturnsTrue()
    {
        var filter = "a=" + new string('b', 4096 - 2); // exactly 4096 chars
        var success = FilterParameters.TryParse(filter, null, out var p);
        success.Should().BeTrue();
    }

    [Fact]
    public void TryParse_TooComplexString_ReturnsFalse()
    {
        // 51 clauses
        var filter = string.Join(",", Enumerable.Repeat("a=1", 51));
        var success = FilterParameters.TryParse(filter, null, out var p);
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_MaximumComplexityString_ReturnsTrue()
    {
        // 50 clauses (max complexity is 50)
        var filter = string.Join(",", Enumerable.Repeat("a=1", 50));
        var success = FilterParameters.TryParse(filter, null, out var p);
        success.Should().BeTrue();
    }

    [Fact]
    public void TryParse_EmptySegments_ReturnsFalse()
    {
        // Tests the segments.Length == 0 check (e.g. string with only commas)
        var success = FilterParameters.TryParse(",", null, out var p);
        success.Should().BeFalse();
        p.Value.Should().BeNull();
        
        // Tests the orSegments.Length == 0 check (e.g. string with only pipes in a segment)
        var success2 = FilterParameters.TryParse("name=John,|", null, out var p2);
        success2.Should().BeFalse();
        p2.Value.Should().BeNull();
    }
    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Action act = () => FilterParameters.Parse("=invalid", null);
        act.Should().Throw<FormatException>().WithMessage("The string '=invalid' is not a valid filter expression.");
    }

    [Fact]
    public void Parse_NullString_ThrowsArgumentNullException()
    {
        Action act = () => FilterParameters.Parse(null!, null);
        act.Should().Throw<ArgumentNullException>();
    }


    
    [Theory]
    [InlineData("!name=John")]
    [InlineData(" name = John ")]
    [InlineData("name=John,,age>18")] // Empty clause tests `if (clause.IsWhiteSpace()) continue;`
    [InlineData("name=John||age>18")] // Empty orSegment tests `if (orSegment.IsWhiteSpace()) continue;`
    public void TryParse_ValidSyntaxWithTrimming_ReturnsTrue(string filter)
    {
        var success = FilterParameters.TryParse(filter, null, out var p);
        success.Should().BeTrue();
    }

    [Fact]
    public void FilterParameters_Parse_TooManyClauses_ReturnsFalse()
    {
        // Max complexity is 10. We will provide 11 clauses.
        var str = string.Join("|", Enumerable.Repeat("name:eq:John", 11));
        var success = FilterParameters.TryParse(str, null, out var result);
        success.Should().BeFalse();
        result.Should().Be(default(FilterParameters));
    }
#endif

    [Property]
    public void From_WithNonNullString_PreservesStringAndHasValue(NonNull<string> nonNull)
    {
        var val = nonNull.Get;
        var p = FilterParameters.From(val);
        p.Value.Should().Be(val);
        p.HasValue.Should().Be(!string.IsNullOrWhiteSpace(val));
        p.ToString().Should().Be(val);
    }

    [Property]
    public void Equality_IsReflexive_ForAnyString(NonNull<string> nonNull)
    {
        var val = nonNull.Get;
        var p1 = FilterParameters.From(val);
        var p2 = FilterParameters.From(val);
        p1.Should().Be(p2);
        (p1 == p2).Should().BeTrue();
        p1.GetHashCode().Should().Be(p2.GetHashCode());
    }
}




