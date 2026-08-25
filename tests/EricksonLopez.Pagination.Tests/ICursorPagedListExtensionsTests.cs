// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class ICursorPagedListExtensionsTests
{
    [Fact]
    public void Map_WithNullSource_ThrowsArgumentNullException()
    {
        ICursorPagedList<int>? source = null;

        var act = () => source!.Map(x => x.ToString());

        act.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        ICursorPagedList<int> source = new CursorPagedList<int>([1, 2], "start", "end", true, false);

        var act = () => source.Map<int, string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Map_WithCountedCursorPagedList_ReturnsCountedCursorPagedList()
    {
        var items = new[] { 1, 2, 3 };
        ICursorPagedList<int> source = new CountedCursorPagedList<int>(items, 10, "start", "end", true, false);

        var result = source.Map(x => x.ToString());

        result.Should().BeOfType<CountedCursorPagedList<string>>();
        var countedResult = (ICountedCursorPagedList<string>)result;
        
        countedResult.ExactTotalCount.Should().Be(10);
        countedResult.StartCursor.Should().Be("start");
        countedResult.EndCursor.Should().Be("end");
        countedResult.HasPreviousPage.Should().BeTrue();
        countedResult.HasNextPage.Should().BeFalse();
        countedResult.Should().BeEquivalentTo(["1", "2", "3"]);
    }

    [Fact]
    public void Map_WithRegularCursorPagedList_ReturnsCursorPagedList()
    {
        var items = new[] { 1, 2, 3 };
        ICursorPagedList<int> source = new CursorPagedList<int>(items, "start", "end", true, false);

        var result = source.Map(x => x.ToString());

        result.Should().BeOfType<CursorPagedList<string>>();
        
        result.StartCursor.Should().Be("start");
        result.EndCursor.Should().Be("end");
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
        result.Should().BeEquivalentTo(["1", "2", "3"]);
    }
}



