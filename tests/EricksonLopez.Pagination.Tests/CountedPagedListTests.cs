// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CountedPagedListTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var items = new[] { 1, 2, 3 };
        var list = new CountedPagedList<int>(items, 100, 2, 10);

        list.ExactTotalCount.Should().Be(100);
        list.TotalCount.Should().Be(100);
        list.Page.Should().Be(2);
        list.PageSize.Should().Be(10);
        list.TotalPages.Should().Be(10);
        list.HasPreviousPage.Should().BeTrue();
        list.HasNextPage.Should().BeTrue();
        list.Should().BeEquivalentTo(items);

        // Test explicit interface implementation
        ((EricksonLopez.Pagination.Abstractions.ICountedPagedList)list).TotalCount.Should().Be(100);
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        var list = new CountedPagedList<int>([], 0, 1, 10);
        var act = () => list.Map<string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Map_ValidSelector_ReturnsMappedCountedPagedList()
    {
        var items = new[] { 1, 2 };
        var list = new CountedPagedList<int>(items, 50, 1, 10);

        var mapped = list.Map(x => x.ToString());

        mapped.Should().BeOfType<CountedPagedList<string>>();
        mapped.ExactTotalCount.Should().Be(50);
        mapped.Page.Should().Be(1);
        mapped.PageSize.Should().Be(10);
        mapped.HasPreviousPage.Should().BeFalse();
        mapped.HasNextPage.Should().BeTrue();
        mapped.Should().BeEquivalentTo(["1", "2"]);
    }
}
