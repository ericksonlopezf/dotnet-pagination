// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class CursorPaginationParametersParsableTests
{
#if NET7_0_OR_GREATER
    [Fact]
    public void Parse_ValidString_ParsesSuccessfully()
    {
        var p = CursorPaginationParameters.Parse("first=20&after=token", null);
        p.First.Should().Be(20);
        p.After.Should().Be("token");
        p.Last.Should().BeNull();
        p.Before.Should().BeNull();
    }
    
    [Fact]
    public void Parse_NullInput_ThrowsArgumentNullException()
    {
        var act = () => CursorPaginationParameters.Parse(null!, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("first=abc")]
    public void Parse_InvalidString_ThrowsFormatException(string value)
    {
        Action act = () => CursorPaginationParameters.Parse(value, null);
        act.Should().Throw<FormatException>().WithMessage($"Cannot parse '{value}' as CursorPaginationParameters.");
    }

    [Fact]
    public void TryParse_NullOrWhiteSpace_ReturnsFalseWithDefault()
    {
        CursorPaginationParameters.TryParse(null, null, out var p1).Should().BeFalse();
        p1.First.Should().BeNull();
        
        CursorPaginationParameters.TryParse("   ", null, out var p2).Should().BeFalse();
        p2.First.Should().BeNull();
    }

    [Fact]
    public void TryParse_AllParameters_ParsedCorrectly()
    {
        var success = CursorPaginationParameters.TryParse("first=15&after=abc", null, out var p);
        
        success.Should().BeTrue();
        p.First.Should().Be(15);
        p.After.Should().Be("abc");
        p.Last.Should().BeNull();
        p.Before.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidIntForFirst_ReturnsFalse()
    {
        CursorPaginationParameters.TryParse("first=abc&after=token", null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_FirstLessThanOne_ReturnsFalse()
    {
        CursorPaginationParameters.TryParse("first=0", null, out _).Should().BeFalse();
        CursorPaginationParameters.TryParse("first=-5", null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_FirstExactlyOne_ParsesSuccessfully()
    {
        CursorPaginationParameters.TryParse("first=1", null, out var p).Should().BeTrue();
        p.First.Should().Be(1);
    }
    
    [Fact]
    public void TryParse_InvalidIntForLast_ReturnsFalse()
    {
        CursorPaginationParameters.TryParse("last=abc&before=token", null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_LastLessThanOne_ReturnsFalse()
    {
        CursorPaginationParameters.TryParse("last=0", null, out _).Should().BeFalse();
        CursorPaginationParameters.TryParse("last=-5", null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_LastExactlyOne_ParsesSuccessfully()
    {
        CursorPaginationParameters.TryParse("last=1", null, out var p).Should().BeTrue();
        p.Last.Should().Be(1);
    }

    [Fact]
    public void TryParse_IgnoresUnknownKeys()
    {
        CursorPaginationParameters.TryParse("first=10&unknown=5&after=token", null, out var p).Should().BeTrue();
        p.First.Should().Be(10);
        p.After.Should().Be("token");
    }

    [Fact]
    public void TryParse_EmptyValueForKeys()
    {
        CursorPaginationParameters.TryParse("first=&after=&last=&before=", null, out var p).Should().BeTrue();
        p.First.Should().BeNull();
        p.After.Should().Be("");
        p.Last.Should().BeNull();
        p.Before.Should().Be("");
    }
    
    [Fact]
    public void TryParse_KeyWithoutEquals()
    {
        CursorPaginationParameters.TryParse("first&after=token", null, out var p).Should().BeTrue();
        p.First.Should().BeNull(); // first is ignored since there's no =
        p.After.Should().Be("token");
    }
#endif
}

