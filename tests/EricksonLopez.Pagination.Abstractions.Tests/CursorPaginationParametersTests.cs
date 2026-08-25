// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class CursorPaginationParametersTests
{
    [Fact]
    public void Default_HasFirst10()
    {
        var p = CursorPaginationParameters.Default;
        p.First.Should().Be(10);
        p.After.Should().BeNull();
        p.Last.Should().BeNull();
        p.Before.Should().BeNull();
    }

    [Fact]
    public void GetPageSize_ReturnsFirst_WhenSet()
    {
        var p = new CursorPaginationParameters { First = 20 };
        p.GetPageSize(10).Should().Be(20);
    }

    [Fact]
    public void GetPageSize_ReturnsLast_WhenOnlyLastSet()
    {
        var p = new CursorPaginationParameters { Last = 15 };
        p.GetPageSize(10).Should().Be(15);
    }

    [Fact]
    public void GetPageSize_ReturnsFirst_WhenBothSet()
    {
        var p = new CursorPaginationParameters { First = 5, Last = 100 };
        p.GetPageSize(10).Should().Be(5);
    }

    [Fact]
    public void GetPageSize_ReturnsDefault_WhenNeitherSet()
    {
        var p = new CursorPaginationParameters();
        p.GetPageSize(10).Should().Be(10);
    }


    [Fact]
    public void AllProperties_AreInitOnly()
    {
        // Verifies that record struct can be initialized and properties persist
        var p = new CursorPaginationParameters
        {
            First = 5,
            After = "cursor-after",
            Last = 3,
            Before = "cursor-before"
        };

        p.First.Should().Be(5);
        p.After.Should().Be("cursor-after");
        p.Last.Should().Be(3);
        p.Before.Should().Be("cursor-before");
    }
    [Fact]
    public void GetPageSize_WithFirst_ReturnsFirst()
    {
        var parameters = new CursorPaginationParameters { First = 10 };
        parameters.GetPageSize(20).Should().Be(10);
    }

    [Fact]
    public void GetPageSize_WithLast_ReturnsLast()
    {
        var parameters = new CursorPaginationParameters { Last = 15 };
        parameters.GetPageSize(20).Should().Be(15);
    }

    [Fact]
    public void GetPageSize_WithNeither_ReturnsDefault()
    {
        var parameters = new CursorPaginationParameters();
        parameters.GetPageSize(20).Should().Be(20);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public void Parse_ValidString_ReturnsParameters()
    {
        var p = CursorPaginationParameters.Parse("first=10&after=abc", null);
        p.First.Should().Be(10);
        p.After.Should().Be("abc");
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Action act = () => CursorPaginationParameters.Parse("first=abc", null);
        act.Should().Throw<System.FormatException>();
    }

    [Fact]
    public void Parse_NullString_ThrowsArgumentNullException()
    {
        Action act = () => CursorPaginationParameters.Parse(null!, null);
        act.Should().Throw<System.ArgumentNullException>();
    }

    [Fact]
    public void TryParse_ValidFirst_ReturnsTrue()
    {
        var success = CursorPaginationParameters.TryParse("first=20", null, out var p);
        success.Should().BeTrue();
        p.First.Should().Be(20);
    }

    [Fact]
    public void TryParse_ValidLast_ReturnsTrue()
    {
        var success = CursorPaginationParameters.TryParse("last=15", null, out var p);
        success.Should().BeTrue();
        p.Last.Should().Be(15);
    }

    [Fact]
    public void TryParse_ValidBefore_ReturnsTrue()
    {
        var success = CursorPaginationParameters.TryParse("before=xyz", null, out var p);
        success.Should().BeTrue();
        p.Before.Should().Be("xyz");
    }

    [Theory]
    [InlineData("first=10&last=10")]
    [InlineData("first=-1")]
    [InlineData("last=0")]
    [InlineData("first=abc")]
    [InlineData("last=abc")]
    [InlineData("=10")]
    [InlineData("first=")]
    [InlineData("last=")]
    [InlineData(" ")]
    [InlineData("unknown=value")]
    public void TryParse_InvalidSyntax_ReturnsFalse(string filter)
    {
        var success = CursorPaginationParameters.TryParse(filter, null, out var p);
        success.Should().BeFalse();
    }
    
    [Fact]
    public void TryParse_EmptyParts_Ignored()
    {
        // Verifies the branch where length is 0
        var success = CursorPaginationParameters.TryParse("first=&after=abc", null, out var p);
        success.Should().BeTrue();
        p.After.Should().Be("abc");
        p.First.Should().BeNull();
        
        var success2 = CursorPaginationParameters.TryParse("last=&before=def", null, out var p2);
        success2.Should().BeTrue();
        p2.Before.Should().Be("def");
        p2.Last.Should().BeNull();
    }

    [Fact]
    public void TryParse_NoValidParameters_ReturnsFalse()
    {
        var success = CursorPaginationParameters.TryParse("foo=bar", null, out var p);
        success.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NegativeFirst_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new CursorPaginationParameters { First = 0 };
        act.Should().Throw<System.ArgumentOutOfRangeException>()
            .WithMessage("*First must be greater than or equal to 1.*");
    }

    [Fact]
    public void Constructor_NegativeLast_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new CursorPaginationParameters { Last = 0 };
        act.Should().Throw<System.ArgumentOutOfRangeException>()
            .WithMessage("*Last must be greater than or equal to 1.*");
    }

    [Fact]
    public void Constructor_LargeFirst_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new CursorPaginationParameters { First = 100_001 };
        act.Should().Throw<System.ArgumentOutOfRangeException>()
            .WithMessage("*First cannot exceed the absolute maximum of 100,000 to prevent memory exhaustion.*");
    }

    [Fact]
    public void Constructor_LargeLast_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new CursorPaginationParameters { Last = 100_001 };
        act.Should().Throw<System.ArgumentOutOfRangeException>()
            .WithMessage("*Last cannot exceed the absolute maximum of 100,000 to prevent memory exhaustion.*");
    }

    [Fact]
    public void Constructor_MaximumFirst_DoesNotThrow()
    {
        Action act = () => new CursorPaginationParameters { First = 100_000 };
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_MaximumLast_DoesNotThrow()
    {
        Action act = () => new CursorPaginationParameters { Last = 100_000 };
        act.Should().NotThrow();
    }


    [Fact]
    public void CursorPaginationParameters_Parse_NoRecognizedKeys_EmptyPairs_ReturnsDefault()
    {
        var result = CursorPaginationParameters.Parse("&&&", null);
        result.Should().Be(CursorPaginationParameters.Default);
    }
#endif
}




