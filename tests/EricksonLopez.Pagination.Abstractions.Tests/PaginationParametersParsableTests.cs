// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

/// <summary>
/// Tests for IParsable&lt;PaginationParameters&gt; — covering the Minimal API binding path.
/// These tests run only on .NET 7+ where IParsable exists.
/// </summary>
public class PaginationParametersParsableTests
{
    // ─── TryParse — valid inputs ──────────────────────────────────────────────

    [Fact]
    public void TryParse_Null_ReturnsTrue_WithDefault()
    {
        // FIX-02: null input returns true + Default so Minimal API binding treats
        // missing pagination query params as 'use defaults' (page=1, pageSize=10).
        var success = PaginationParameters.TryParse(null, null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsTrue_WithDefault()
    {
        // FIX-02: empty string returns true + Default so Minimal API binding treats
        // an empty pagination query param as 'use defaults' (page=1, pageSize=10).
        var success = PaginationParameters.TryParse(string.Empty, null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void TryParse_PageOnly_ParsesPageWithDefaultSize()
    {
        var success = PaginationParameters.TryParse("page=3", null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void TryParse_PageSizeOnly_ParsesPageSizeWithDefaultPage()
    {
        var success = PaginationParameters.TryParse("pageSize=25", null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public void TryParse_BothPageAndPageSize_ParsesBoth()
    {
        var success = PaginationParameters.TryParse("page=2&pageSize=50", null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public void TryParse_BothPageAndPageSize_ReverseOrder_ParsesBoth()
    {
        var success = PaginationParameters.TryParse("pageSize=50&page=2", null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public void TryParse_CaseInsensitiveKeys_Succeeds()
    {
        var success = PaginationParameters.TryParse("PAGE=5&PAGESIZE=20", null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(5);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public void TryParse_ExtraParameters_IgnoresThem()
    {
        var success = PaginationParameters.TryParse("page=2&pageSize=10&sort=name", null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void TryParse_KeyWithoutEquals_IgnoresKey()
    {
        var success = PaginationParameters.TryParse("page&pageSize=20", null, out var result);
        success.Should().BeTrue();
        result.Page.Should().Be(1); // default
        result.PageSize.Should().Be(20);
    }



    // ─── TryParse — invalid inputs ────────────────────────────────────────────

    [Theory]
    [InlineData("page=0",       false)]
    [InlineData("page=-1",      false)]
    [InlineData("page=abc",     false)]
    [InlineData("pageSize=0",   false)]
    [InlineData("pageSize=-5",  false)]
    [InlineData("pageSize=abc", false)]
    public void TryParse_InvalidInput_ReturnsFalse(string input, bool expectedSuccess)
    {
        var success = PaginationParameters.TryParse(input, null, out var result);
        success.Should().Be(expectedSuccess);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    // ─── Parse — throws on invalid ────────────────────────────────────────────

    [Fact]
    public void Parse_ValidInput_ReturnsParameters()
    {
        var result = PaginationParameters.Parse("page=4&pageSize=15", null);
        result.Page.Should().Be(4);
        result.PageSize.Should().Be(15);
    }

    [Fact]
    public void Parse_InvalidInput_ThrowsFormatException()
    {
        var act = () => PaginationParameters.Parse("page=0&pageSize=10", null);
        act.Should().Throw<FormatException>()
            .WithMessage("Cannot parse 'page=0&pageSize=10' as PaginationParameters. Expected format: 'page=<n>&pageSize=<m>' where n and m are positive integers.");
    }

    [Fact]
    public void Parse_NullInput_ThrowsArgumentNullException()
    {
        var act = () => PaginationParameters.Parse(null!, null);
        act.Should().Throw<ArgumentNullException>();
    }
}



