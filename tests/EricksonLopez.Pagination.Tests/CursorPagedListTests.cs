// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CursorPagedListTests
{
    // ─── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_StoresAlreadyEncodedCursors_Verbatim()
    {
        // The constructor must NOT re-encode cursors — encoding is the provider's responsibility.
        var pagedList = new CursorPagedList<int>(new[] { 1 }, "already-encoded", "also-encoded", false, false);

        pagedList.StartCursor.Should().Be("already-encoded");
        pagedList.EndCursor.Should().Be("also-encoded");
    }

    [Fact]
    public void Constructor_NullItems_InitializesEmptyList()
    {
        var pagedList = new CursorPagedList<int>(null!, null, null, false, false);
        pagedList.Count.Should().Be(0);
        pagedList.StartCursor.Should().BeNull();
        pagedList.EndCursor.Should().BeNull();
    }

    // ─── Empty ────────────────────────────────────────────────────────────────

    [Fact]
    public void Empty_ReturnsEmptyCursorPagedList()
    {
        var pagedList = CursorPagedList<string>.Empty;

        pagedList.Count.Should().Be(0);
        pagedList.StartCursor.Should().BeNull();
        pagedList.EndCursor.Should().BeNull();
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeFalse();
    }

    // ─── Create factory ───────────────────────────────────────────────────────

    [Fact]
    public void Create_NullItems_ThrowsArgumentNullException()
    {
        var act = () => CursorPagedList<int>.Create(null!, null, null, false, false);
        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }

    [Fact]
    public void Create_WithItems_PopulatesPropertiesCorrectly()
    {
        var items = new[] { 1, 2, 3 };

        var pagedList = CursorPagedList<int>.Create(items, "sc", "ec", true, false);

        pagedList.Count.Should().Be(3);
        pagedList.StartCursor.Should().Be("sc");
        pagedList.EndCursor.Should().Be("ec");
        pagedList.HasPreviousPage.Should().BeTrue();
        pagedList.HasNextPage.Should().BeFalse();
    }

    // ─── Indexer & Enumerator ─────────────────────────────────────────────────

    [Fact]
    public void Indexer_ReturnsCorrectItem()
    {
        var items = new[] { "A", "B", "C" };
        var pagedList = new CursorPagedList<string>(items, null, null, false, false);

        pagedList[0].Should().Be("A");
        pagedList[2].Should().Be("C");
    }

    [Fact]
    public void GetEnumerator_ReturnsItemsEnumerator()
    {
        var items = new[] { "A", "B" };
        var pagedList = new CursorPagedList<string>(items, "start", "end", false, false);

        var enumerator = pagedList.GetEnumerator();
        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be("A");
        
        // Non-generic enumerator
        var nonGenericEnumerator = ((IEnumerable)pagedList).GetEnumerator();
        nonGenericEnumerator.MoveNext().Should().BeTrue();
        nonGenericEnumerator.Current.Should().Be("A");
    }

    // ─── Map ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Map_ProjectsItemsCorrectly()
    {
        var items = new[] { 1, 2, 3 };
        var pagedList = CursorPagedList<int>.Create(items, "sc", "ec", true, false);

        var mapped = pagedList.Map(x => x * 2);

        mapped.Count.Should().Be(3);
        mapped[0].Should().Be(2);
        mapped[1].Should().Be(4);
        mapped[2].Should().Be(6);
    }

    [Fact]
    public void Map_PreservesCursorMetadata()
    {
        var items = new[] { 1, 2 };
        var pagedList = CursorPagedList<int>.Create(items, "start-cursor", "end-cursor", true, true);

        var mapped = pagedList.Map(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));

        mapped.StartCursor.Should().Be("start-cursor");
        mapped.EndCursor.Should().Be("end-cursor");
        mapped.HasPreviousPage.Should().BeTrue();
        mapped.HasNextPage.Should().BeTrue();
        mapped.Count.Should().Be(2);
        mapped[0].Should().Be("1");
        mapped[1].Should().Be("2");
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        var pagedList = CursorPagedList<int>.Empty;

        var act = () => pagedList.Map<string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }



    [Fact]
    public void Create_WithNullItems_ThrowsArgumentNullException()
    {
        var act = () => CursorPagedList<int>.Create(null!, null, null, false, false);
        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }


    [Fact]
    public void Map_OnEmptyList_ReturnsEmptyMappedList()
    {
        var pagedList = CursorPagedList<int>.Empty;

        var mapped = pagedList.Map(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));

        mapped.Count.Should().Be(0);
        mapped.StartCursor.Should().BeNull();
        mapped.EndCursor.Should().BeNull();
    }

    // ─── Equals & GetHashCode ──────────────────────────────────────────────────


    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var pagedList = CursorPagedList<int>.Empty;
        CursorPagedList<int>? nullList = null;
        object? nullObj = null;
#pragma warning disable CA1508, CS8602
        pagedList.Equals(nullList).Should().BeFalse();
        pagedList.Equals(nullObj).Should().BeFalse();
#pragma warning restore CA1508, CS8602
    }

    [Fact]
    public void Equals_WithSameInstance_ReturnsTrue()
    {
        var pagedList = CursorPagedList<int>.Empty;
        pagedList.Equals(pagedList).Should().BeTrue();
        pagedList.Equals((object)pagedList).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var pagedList = CursorPagedList<int>.Empty;
        pagedList.Equals(new object()).Should().BeFalse();
    }


}

