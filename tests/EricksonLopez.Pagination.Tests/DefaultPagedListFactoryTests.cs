// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class DefaultPagedListFactoryTests
{
    [Fact]
    public void CreatePagedList_ShouldCreatePagedList()
    {
        // Arrange
        var factory = DefaultPagedListFactory.Instance;
        var items = new List<string> { "A", "B" };

        // Act
        var result = factory.CreatePagedList(items, 10, 1, 5, true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void CreateCursorPagedList_ShouldCreateCursorPagedList()
    {
        // Arrange
        var factory = DefaultPagedListFactory.Instance;
        var items = new List<string> { "A", "B" };

        // Act
        var result = factory.CreateCursorPagedList(items, null, "start", "end", true, true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(items);
        result.StartCursor.Should().Be("start");
        result.EndCursor.Should().Be("end");
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }
    [Fact]
    public void CreatePagedList_WithoutTotalCount_ReturnsPagedList()
    {
        var factory = DefaultPagedListFactory.Instance;
        var items = new List<string> { "A", "B" };

        var result = factory.CreatePagedList(items, null, 1, 5, true);

        result.Should().BeOfType<PagedList<string>>();
        result.TotalCount.Should().BeNull();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void CreateCursorPagedList_WithTotalCount_ReturnsCountedCursorPagedList()
    {
        var factory = DefaultPagedListFactory.Instance;
        var items = new List<string> { "A", "B" };

        var result = factory.CreateCursorPagedList(items, 100, "start", "end", true, true);

        result.Should().BeOfType<CountedCursorPagedList<string>>();
        var counted = (CountedCursorPagedList<string>)result;
        counted.ExactTotalCount.Should().Be(100);
        counted.StartCursor.Should().Be("start");
    }
}



