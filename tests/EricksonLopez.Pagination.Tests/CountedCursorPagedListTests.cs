// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CountedCursorPagedListTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var items = new[] { 1, 2, 3 };
        var list = new CountedCursorPagedList<int>(items, 100, "start", "end", true, false);

        list.ExactTotalCount.Should().Be(100);
        list.TotalCount.Should().Be(100);
        list.StartCursor.Should().Be("start");
        list.EndCursor.Should().Be("end");
        list.HasPreviousPage.Should().BeTrue();
        list.HasNextPage.Should().BeFalse();
        list.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        var list = new CountedCursorPagedList<int>([], 0, null, null, false, false);
        var act = () => list.Map<string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Map_ValidSelector_ReturnsMappedCountedCursorPagedList()
    {
        var items = new[] { 1, 2 };
        var list = new CountedCursorPagedList<int>(items, 50, "c1", "c2", true, true);

        var mapped = list.Map(x => x.ToString());

        mapped.ExactTotalCount.Should().Be(50);
        mapped.StartCursor.Should().Be("c1");
        mapped.EndCursor.Should().Be("c2");
        mapped.HasPreviousPage.Should().BeTrue();
        mapped.HasNextPage.Should().BeTrue();
        mapped.Should().BeEquivalentTo(["1", "2"]);
    }
}
