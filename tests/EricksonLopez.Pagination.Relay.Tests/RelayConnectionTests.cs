// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Relay;
using Xunit;

namespace EricksonLopez.Pagination.Relay.Tests;

public class RelayConnectionTests
{
    private record Item(int Id, string Name);
    private record ItemDto(string Display);

    [Fact]
    public void ToRelayConnection_FromCursorPagedList_MapsNodesAndCursorsCorrectly()
    {
        var items = new List<Item>
        {
            new(1, "Item 1"),
            new(2, "Item 2"),
            new(3, "Item 3")
        };

        var cursorPagedList = new CursorPagedList<Item>(
            items: items,
            startCursor: "start_token",
            endCursor: "end_token",
            hasPreviousPage: false,
            hasNextPage: true);

        var connection = cursorPagedList.ToRelayConnection(item => $"cursor_{item.Id}");

        connection.Edges.Count.Should().Be(3);
        connection.Edges[0].Node.Id.Should().Be(1);
        connection.Edges[0].Cursor.Should().Be("cursor_1");
        connection.Edges[1].Node.Id.Should().Be(2);
        connection.Edges[1].Cursor.Should().Be("cursor_2");
        connection.Edges[2].Node.Id.Should().Be(3);
        connection.Edges[2].Cursor.Should().Be("cursor_3");

        connection.PageInfo.HasNextPage.Should().BeTrue();
        connection.PageInfo.HasPreviousPage.Should().BeFalse();
        connection.PageInfo.StartCursor.Should().Be("cursor_1");
        connection.PageInfo.EndCursor.Should().Be("cursor_3");
        connection.TotalCount.Should().BeNull();
    }

    [Fact]
    public void ToRelayConnection_WhenListIsEmpty_ReturnsEmptyEdgesAndFallbackCursors()
    {
        var emptyList = new CursorPagedList<Item>(
            items: Array.Empty<Item>(),
            startCursor: "empty_start",
            endCursor: "empty_end",
            hasPreviousPage: false,
            hasNextPage: false);

        var connection = emptyList.ToRelayConnection(item => $"cursor_{item.Id}");

        connection.Edges.Should().BeEmpty();
        connection.PageInfo.HasNextPage.Should().BeFalse();
        connection.PageInfo.HasPreviousPage.Should().BeFalse();
        connection.PageInfo.StartCursor.Should().Be("empty_start");
        connection.PageInfo.EndCursor.Should().Be("empty_end");
    }

    [Fact]
    public void ToRelayConnection_WithNodeMapping_TransformsNodes()
    {
        var items = new List<Item> { new(10, "Widget"), new(20, "Gadget") };

        var cursorPagedList = new CursorPagedList<Item>(
            items: items,
            startCursor: "raw_start_token",
            endCursor: "raw_end_token",
            hasPreviousPage: true,
            hasNextPage: false);

        var connection = cursorPagedList.ToRelayConnection(
            nodeSelector: item => new ItemDto($"{item.Id}:{item.Name}"),
            cursorSelector: item => $"c_{item.Id}");

        connection.Edges.Count.Should().Be(2);
        connection.Edges[0].Node.Display.Should().Be("10:Widget");
        connection.Edges[0].Cursor.Should().Be("c_10");
        connection.Edges[1].Node.Display.Should().Be("20:Gadget");
        connection.Edges[1].Cursor.Should().Be("c_20");

        connection.PageInfo.HasNextPage.Should().BeFalse();
        connection.PageInfo.HasPreviousPage.Should().BeTrue();
        connection.PageInfo.StartCursor.Should().Be("c_10");
        connection.PageInfo.EndCursor.Should().Be("c_20");
    }

    [Fact]
    public void ToRelayConnection_WithNodeMapping_WhenEmpty_ReturnsFallbackCursors()
    {
        var emptyList = new CursorPagedList<Item>(
            items: Array.Empty<Item>(),
            startCursor: "fallback_start",
            endCursor: "fallback_end",
            hasPreviousPage: true,
            hasNextPage: false);

        var connection = emptyList.ToRelayConnection(
            nodeSelector: item => new ItemDto(item.Name),
            cursorSelector: item => item.Id.ToString());

        connection.Edges.Should().BeEmpty();
        connection.PageInfo.StartCursor.Should().Be("fallback_start");
        connection.PageInfo.EndCursor.Should().Be("fallback_end");
        connection.PageInfo.HasPreviousPage.Should().BeTrue();
        connection.PageInfo.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToRelayConnection_FromOffsetPagedList_MapsCorrectly()
    {
        var items = new List<Item> { new(1, "A"), new(2, "B") };
        var pagedList = PagedList<Item>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 2 }, 50);

        var connection = pagedList.ToRelayConnection(item => $"offset_{item.Id}");

        connection.Edges.Count.Should().Be(2);
        connection.PageInfo.HasNextPage.Should().BeTrue();
        connection.PageInfo.HasPreviousPage.Should().BeTrue();
        connection.PageInfo.StartCursor.Should().Be("offset_1");
        connection.PageInfo.EndCursor.Should().Be("offset_2");
        connection.TotalCount.Should().Be(50);
    }

    [Fact]
    public void ToRelayConnection_FromEmptyOffsetPagedList_MapsNullCursors()
    {
        var emptyPagedList = PagedList<Item>.WithCount(Array.Empty<Item>(), new PaginationParameters { Page = 1, PageSize = 10 }, 0);

        var connection = emptyPagedList.ToRelayConnection(item => $"offset_{item.Id}");

        connection.Edges.Should().BeEmpty();
        connection.PageInfo.HasNextPage.Should().BeFalse();
        connection.PageInfo.HasPreviousPage.Should().BeFalse();
        connection.PageInfo.StartCursor.Should().BeNull();
        connection.PageInfo.EndCursor.Should().BeNull();
        connection.TotalCount.Should().Be(0);
    }

    [Fact]
    public void NullGuards_ThrowArgumentNullException()
    {
        ICursorPagedList<Item>? nullCursorList = null;
        IPagedList<Item>? nullOffsetList = null;
        Func<Item, string>? nullCursorSelector = null;
        Func<Item, ItemDto>? nullNodeSelector = null;

        var validCursorList = new CursorPagedList<Item>(Array.Empty<Item>(), null, null, false, false);
        var validOffsetList = PagedList<Item>.WithCount(Array.Empty<Item>(), new PaginationParameters { Page = 1, PageSize = 10 }, 0);
        Func<Item, string> validCursorSelector = i => i.Id.ToString();
        Func<Item, ItemDto> validNodeSelector = i => new ItemDto(i.Name);

        var act1 = () => nullCursorList!.ToRelayConnection(validCursorSelector);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("list");

        var act2 = () => validCursorList.ToRelayConnection(nullCursorSelector!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("cursorSelector");

        var act3 = () => nullCursorList!.ToRelayConnection(validNodeSelector, validCursorSelector);
        act3.Should().Throw<ArgumentNullException>().WithParameterName("list");

        var act4 = () => validCursorList.ToRelayConnection(nullNodeSelector!, validCursorSelector);
        act4.Should().Throw<ArgumentNullException>().WithParameterName("nodeSelector");

        var act5 = () => validCursorList.ToRelayConnection(validNodeSelector, nullCursorSelector!);
        act5.Should().Throw<ArgumentNullException>().WithParameterName("cursorSelector");

        var act6 = () => nullOffsetList!.ToRelayConnection(validCursorSelector);
        act6.Should().Throw<ArgumentNullException>().WithParameterName("list");

        var act7 = () => validOffsetList.ToRelayConnection(nullCursorSelector!);
        act7.Should().Throw<ArgumentNullException>().WithParameterName("cursorSelector");

        var act8 = () => new Edge<string>("node", null!);
        act8.Should().Throw<ArgumentNullException>().WithParameterName("cursor");

        var act9 = () => new Connection<string>(null!, new PageInfo());
        act9.Should().Throw<ArgumentNullException>().WithParameterName("edges");

        var act10 = () => new Connection<string>(Array.Empty<Edge<string>>(), null!);
        act10.Should().Throw<ArgumentNullException>().WithParameterName("pageInfo");
    }

    [Fact]
    public void PageInfo_Properties_CanBeSetAndRead()
    {
        var pageInfo = new PageInfo
        {
            HasNextPage = true,
            HasPreviousPage = false,
            StartCursor = "cur_start",
            EndCursor = "cur_end"
        };

        pageInfo.HasNextPage.Should().BeTrue();
        pageInfo.HasPreviousPage.Should().BeFalse();
        pageInfo.StartCursor.Should().Be("cur_start");
        pageInfo.EndCursor.Should().Be("cur_end");
    }

    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Connection_JsonSerialization_ProducesCompliantJson()
    {
        var edges = new List<Edge<Item>> { new(new Item(1, "Test"), "cursor1") };
        var pageInfo = new PageInfo { HasNextPage = true, EndCursor = "cursor1" };
        var connection = new Connection<Item>(edges, pageInfo, totalCount: 1);

        string json = JsonSerializer.Serialize(connection, CamelCaseOptions);

        json.Should().Contain("\"edges\":");
        json.Should().Contain("\"pageInfo\":");
        json.Should().Contain("\"hasNextPage\":true");
        json.Should().Contain("\"cursor\":\"cursor1\"");
        json.Should().Contain("\"totalCount\":1");
    }
}
