// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class PaginationParametersTests
{
    [Fact]
    public void Page_DefaultValue_Is1()
    {
        var parameters = new PaginationParameters();
        parameters.Page.Should().Be(1);
    }

    [Fact]
    public void PageSize_DefaultValue_Is10()
    {
        var parameters = new PaginationParameters();
        parameters.PageSize.Should().Be(10);
    }

    [Fact]
    public void AllProperties_AreInitOnly()
    {
        var p = new PaginationParameters
        {
            Page = 5,
            PageSize = 20
        };

        p.Page.Should().Be(5);
        p.PageSize.Should().Be(20);
    }

    [Fact]
    public void Constructor_LargePageSize_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new PaginationParameters { PageSize = 100_001 };
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*PageSize cannot exceed the absolute maximum of 100,000 to prevent memory exhaustion.*");
    }

    [Fact]
    public void Constructor_MaximumPageSize_DoesNotThrow()
    {
        Action act = () => new PaginationParameters { PageSize = 100_000 };
        act.Should().NotThrow();
    }

    [Fact]
    public void Default_StaticProperty_ReturnsDefaultValues()
    {
        var parameters = PaginationParameters.Default;
        parameters.Page.Should().Be(1);
        parameters.PageSize.Should().Be(10);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    public void Create_WithValidValues_SetsProperties(int page, int pageSize)
    {
        var parameters = PaginationParameters.Create(page, pageSize);
        
        parameters.Page.Should().Be(page);
        parameters.PageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InitPage_WithInvalidValue_ThrowsArgumentOutOfRangeException(int invalidPage)
    {
        var act = () => new PaginationParameters { Page = invalidPage };
        
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Page must be greater than or equal to 1.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InitPageSize_WithInvalidValue_ThrowsArgumentOutOfRangeException(int invalidPageSize)
    {
        var act = () => new PaginationParameters { PageSize = invalidPageSize };
        
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*PageSize must be greater than or equal to 1.*");
    }

    [Fact]
    public void Create_WithInvalidPage_ThrowsArgumentOutOfRangeException()
    {
        var act = () => PaginationParameters.Create(0, 10);
        
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithInvalidPageSize_ThrowsArgumentOutOfRangeException()
    {
        var act = () => PaginationParameters.Create(1, 0);
        
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

#if NET7_0_OR_GREATER
    [Theory]
    [InlineData("page=2", 2, 10)]
    [InlineData("pageSize=50", 1, 50)]
    [InlineData("page=3&pageSize=25", 3, 25)]
    [InlineData("&page=4", 4, 10)] // leading ampersand
    [InlineData("page=5&&pageSize=15", 5, 15)] // double ampersand
    [InlineData("page=1", 1, 10)] // boundary: page 1
    [InlineData("pageSize=1", 1, 1)] // boundary: pageSize 1
    [InlineData("pageSize=100000", 1, 100000)] // boundary: max pageSize
    public void TryParse_ValidSyntax_ReturnsTrueAndCorrectValues(string s, int expectedPage, int expectedSize)
    {
        var success = PaginationParameters.TryParse(s, null, out var p);
        success.Should().BeTrue();
        p.Page.Should().Be(expectedPage);
        p.PageSize.Should().Be(expectedSize);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=100001")]
    [InlineData("page=abc")]
    [InlineData("pageSize=abc")]
    public void TryParse_InvalidValues_ReturnsFalse(string s)
    {
        var success = PaginationParameters.TryParse(s, null, out var p);
        success.Should().BeFalse();
    }

    [Fact]
    public void PaginationParameters_Init_WithPage1_DoesNotThrow()
    {
        var parameters = new PaginationParameters { Page = 1 };
        parameters.Page.Should().Be(1);
    }
    
    [Fact]
    public void PaginationParameters_Init_WithPageSize1_DoesNotThrow()
    {
        var parameters = new PaginationParameters { PageSize = 1 };
        parameters.PageSize.Should().Be(1);
    }
#endif

    [Fact]
    public void Equality_And_HashCode_WorkAsExpected()
    {
        var p1 = PaginationParameters.Create(2, 25);
        var p2 = PaginationParameters.Create(2, 25);
        var p3 = PaginationParameters.Create(3, 25);

        (p1 == p2).Should().BeTrue();
        (p1 != p3).Should().BeTrue();
        p1.Equals(p2).Should().BeTrue();
        p1.Equals((object)p2).Should().BeTrue();
        p1.Equals(p3).Should().BeFalse();
        p1.GetHashCode().Should().Be(p2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var p = PaginationParameters.Create(3, 15);
        p.ToString().Should().Contain("3").And.Contain("15");
    }

    [FsCheck.Xunit.Property]
    public void Create_ValidRange_AlwaysPreservesInvariants(FsCheck.PositiveInt pageGen, FsCheck.PositiveInt sizeGen)
    {
        int page = (pageGen.Get % 10_000) + 1;
        int pageSize = (sizeGen.Get % 100_000) + 1;

        var p = PaginationParameters.Create(page, pageSize);
        p.Page.Should().Be(page);
        p.PageSize.Should().Be(pageSize);
    }
}


