// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.Tests.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class PagedListTests
{
    [Fact]
    public void Create_WithArray_DoesNotAllocateNewList()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var parameters = PaginationParameters.Create(1, 10);

        // Act
        var pagedList = PagedList<int>.WithCount(items, parameters, 3);

        // Assert
        items[0] = 99; // Mutating array to prove reference sharing
        pagedList[0].Should().Be(99);
    }

    [Fact]
    public void Create_WithList_DoesNotAllocateNewList()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var parameters = PaginationParameters.Create(1, 10);

        // Act
        var pagedList = PagedList<int>.WithCount(items, parameters, 3);

        // Assert
        items[0] = 99;
        pagedList[0].Should().Be(99);
    }

    [Fact]
    public void Map_PreservesPaginationMetadata()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var parameters = PaginationParameters.Create(2, 5);
        var pagedList = PagedList<int>.WithCount(items, parameters, 15);

        // Act
        var mapped = pagedList.Map(x => x * 2);

        // Assert
        mapped.TotalCount.Should().Be(15);
        mapped.Page.Should().Be(2);
        mapped.PageSize.Should().Be(5);
        mapped.TotalPages.Should().Be(3);
        mapped.HasPreviousPage.Should().BeTrue();
        mapped.HasNextPage.Should().BeTrue();
        mapped.ToArray().Should().BeEquivalentTo(new[] { 2, 4, 6 });
    }

    [Fact]
    public void Constructor_InvalidPage_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new PagedList<int>([], null, 0, 10);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("page")
           .WithMessage("*Page must be greater than or equal to 1.*");
    }

    [Fact]
    public void Constructor_InvalidPageSize_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new PagedList<int>([], null, 1, 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("pageSize")
           .WithMessage("*PageSize must be greater than or equal to 1.*");
    }

    [Fact]
    public void Constructor_NegativeTotalCount_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        var act = () => PagedList<int>.WithCount([], parameters, -1);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("totalCount")
           .WithMessage("*TotalCount cannot be negative.*");
    }

    [Fact]
    public void Constructor_ZeroTotalCount_ForcesHasNextPageFalse()
    {
        var list = new PagedList<int>([], 0, 1, 10, hasNextPage: true);
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void WithCount_NullItems_ThrowsArgumentNullException()
    {
        var parameters = PaginationParameters.Parse("page=1&pageSize=10", null);
        var act = () => PagedList<int>.WithCount(null!, parameters, 0);

        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }

    [Fact]
    public void WithoutCount_NullItems_ThrowsArgumentNullException()
    {
        var parameters = PaginationParameters.Parse("page=1&pageSize=10", null);
        var act = () => PagedList<int>.WithoutCount(null!, parameters, false);

        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }

    [Fact]
    public void WithoutCount_SetsPropertiesCorrectly()
    {
        var parameters = PaginationParameters.Parse("page=2&pageSize=10", null);
        var list = PagedList<int>.WithoutCount([1, 2, 3], parameters, hasNextPage: true, hasPreviousPage: true);

        list.TotalCount.Should().BeNull();
        list.TotalPages.Should().BeNull();
        list.Page.Should().Be(2);
        list.PageSize.Should().Be(10);
        list.HasNextPage.Should().BeTrue();
        list.HasPreviousPage.Should().BeTrue();
    }

    [Theory]
    [InlineData(10, 3, 4)]
    [InlineData(25, 10, 3)]
    [InlineData(0, 10, 0)]
    [InlineData(1, 1, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    public void TotalPages_IsComputedCorrectly(int totalCount, int pageSize, int expectedTotalPages)
    {
        var pagedList = new PagedList<int>(Array.Empty<int>(), totalCount, 1, pageSize, false);
        pagedList.TotalPages.Should().Be(expectedTotalPages);
    }

    [Fact]
    public void HasPreviousPage_IsTrue_WhenPageIsGreaterThan1()
    {
        var parameters = PaginationParameters.Create(3, 10);
        var pagedList = PagedList<string>.WithCount(Array.Empty<string>(), parameters, 100);
        pagedList.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasPreviousPage_IsFalse_OnPage1()
    {
        var parameters = PaginationParameters.Create(1, 10);
        var pagedList = PagedList<string>.WithCount(Array.Empty<string>(), parameters, 100);
        pagedList.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ExplicitFalse_OverridesPageGreaterThan1()
    {
        var list = new PagedList<int>([], null, 3, 10, hasNextPage: true, hasPreviousPage: false);
        list.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ExplicitTrue_OverridesPage1()
    {
        var list = new PagedList<int>([], null, 1, 10, hasNextPage: true, hasPreviousPage: true);
        list.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasPreviousPage_NullExplicit_UsesPageComparison()
    {
        var listPage1 = new PagedList<int>([], null, 1, 10, hasNextPage: false, hasPreviousPage: null);
        listPage1.HasPreviousPage.Should().BeFalse();

        var listPage2 = new PagedList<int>([], null, 2, 10, hasNextPage: false, hasPreviousPage: null);
        listPage2.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_IsTrue_WhenTotalCountIndicatesMorePages()
    {
        var parameters = PaginationParameters.Create(1, 10);
        var pagedList = PagedList<int>.WithCount(new int[10], parameters, 100);
        pagedList.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_IsFalse_OnLastPage()
    {
        var parameters = PaginationParameters.Create(3, 10);
        var pagedList = PagedList<int>.WithCount(new int[5], parameters, 25);
        pagedList.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_IsTrue_WhenHasNextPageHintIsTrue()
    {
        var parameters = PaginationParameters.Create(1, 10);
        var pagedList = PagedList<int>.WithoutCount(new int[10], parameters, hasNextPage: true);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.TotalCount.Should().BeNull();
    }

    [Fact]
    public void HasNextPage_IsFalse_WhenHasNextPageHintIsFalse()
    {
        var parameters = PaginationParameters.Create(2, 10);
        var pagedList = PagedList<int>.WithoutCount(new int[5], parameters, hasNextPage: false);
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.TotalPages.Should().BeNull();
    }

    [Fact]
    public void Map_OnEmptyList_ReturnsEmptyMappedList()
    {
        var pagedList = PagedList<int>.Empty(PaginationParameters.Default);
        var mapped = pagedList.Map(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));
        mapped.Count.Should().Be(0);
        mapped.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        var pagedList = PagedList<int>.Empty(PaginationParameters.Default);
        var act = () => pagedList.Map<string>(null!);
        act.Should().Throw<System.ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullItems_UsesEmptyArray()
    {
        var pagedList = new PagedList<int>(null!, totalCount: 0, page: 1, pageSize: 10);
        pagedList.Count.Should().Be(0);
        pagedList.Should().BeEmpty();
    }
}


