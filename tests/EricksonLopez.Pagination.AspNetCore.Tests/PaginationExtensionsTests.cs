// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationExtensionsTests
{
    [Fact]
    public void ToPagedResponse_WithNullPagedList_ThrowsArgumentNullException()
    {
        IPagedList<int> pagedList = null!;
        Action act = () => pagedList.ToPagedResponse();
        act.Should().Throw<ArgumentNullException>().WithParameterName("pagedList");

        var request = NSubstitute.Substitute.For<Microsoft.AspNetCore.Http.HttpRequest>();
        Action actReq = () => pagedList.ToPagedResponse(request);
        actReq.Should().Throw<ArgumentNullException>().WithParameterName("pagedList");

        var validList = PagedList<int>.WithCount(new[] { 1 }, new PaginationParameters { Page = 1, PageSize = 10 }, 1);
        Action actNullReq = () => validList.ToPagedResponse((Microsoft.AspNetCore.Http.HttpRequest)null!);
        actNullReq.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void ToPagedResponse_MapsPropertiesCorrectly_WithoutRequestUri()
    {
        var items = new List<int> { 1, 2, 3 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        var response = pagedList.ToPagedResponse();

        response.Items.Should().BeEquivalentTo(items);
        response.TotalCount.Should().Be(10);
        response.Page.Should().Be(2);
        response.PageSize.Should().Be(3);
        response.TotalPages.Should().Be(4);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeTrue();
        response.NextPageUrl.Should().BeNull();
        response.PreviousPageUrl.Should().BeNull();
    }

    [Fact]
    public void ToPagedResponse_GeneratesNextAndPrevUrls_WhenUriIsProvided()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        var response = pagedList.ToPagedResponse("https://api.example.com/items?sortBy=name&page=2&pageSize=3");

        response.NextPageUrl.Should().Be("https://api.example.com/items?sortBy=name&pageSize=3&page=3");
        response.PreviousPageUrl.Should().Be("https://api.example.com/items?sortBy=name&pageSize=3&page=1");
    }
    
    [Fact]
    public void ToPagedResponse_GeneratesRelativeUrls_WhenRelativeUriIsProvided()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        var response = pagedList.ToPagedResponse("/api/items?sortBy=name");

        response.NextPageUrl.Should().Be("/api/items?sortBy=name&pageSize=3&page=3");
        response.PreviousPageUrl.Should().Be("/api/items?sortBy=name&pageSize=3&page=1");
    }

    [Fact]
    public void ToPagedResponse_GeneratesUrls_WhenQueryIsAtStart()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        var response = pagedList.ToPagedResponse("?sortBy=name");

        response.NextPageUrl.Should().Be("?sortBy=name&pageSize=3&page=3");
        response.PreviousPageUrl.Should().Be("?sortBy=name&pageSize=3&page=1");
    }

    [Fact]
    public void ToPagedResponse_GeneratesUrls_WhenUriHasNoQueryString()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        var response = pagedList.ToPagedResponse("https://api.example.com/items");

        response.NextPageUrl.Should().Be("https://api.example.com/items?pageSize=3&page=3");
        response.PreviousPageUrl.Should().Be("https://api.example.com/items?pageSize=3&page=1");
    }

    [Fact]
    public void ToPagedResponse_GeneratesUrls_WhenRelativeUriHasNoQueryString()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        var response = pagedList.ToPagedResponse("/api/items");

        response.NextPageUrl.Should().Be("/api/items?pageSize=3&page=3");
        response.PreviousPageUrl.Should().Be("/api/items?pageSize=3&page=1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ToPagedResponse_DoesNotGenerateUrls_WhenUrlIsNullOrWhiteSpace(string? value)
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);
        
        var response = pagedList.ToPagedResponse(value);

        response.NextPageUrl.Should().BeNull();
        response.PreviousPageUrl.Should().BeNull();
    }

    [Fact]
    public void ToPagedResponse_GeneratesUrls_WhenQueryIsOnlyQuestionMark()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        var response = pagedList.ToPagedResponse("http://api.example.com?");

        response.NextPageUrl.Should().Be("http://api.example.com?pageSize=3&page=3");
        response.PreviousPageUrl.Should().Be("http://api.example.com?pageSize=3&page=1");
    }

    [Fact]
    public void ToPagedResponse_HttpRequest_Extension_GeneratesCorrectly()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);
        
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.PathBase = "/app";
        context.Request.Path = "/api/items";
        context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?sort=name");

        var response = pagedList.ToPagedResponse(context.Request);

        response.NextPageUrl.Should().Be("/app/api/items?sort=name&pageSize=3&page=3");
        response.PreviousPageUrl.Should().Be("/app/api/items?sort=name&pageSize=3&page=1");
    }

    [Fact]
    public void ToPagedResponse_HttpRequest_WithNullRequest_ThrowsArgumentNullException()
    {
        var items = new List<int> { 4, 5, 6 };
        var pagedList = PagedList<int>.WithCount(items, new PaginationParameters { Page = 2, PageSize = 3 }, 10);

        Action act = () => pagedList.ToPagedResponse((Microsoft.AspNetCore.Http.HttpRequest)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void ToCursorPagedResponse_WithNullPagedList_ThrowsArgumentNullException()
    {
        ICursorPagedList<int> pagedList = null!;
        Action act = () => pagedList.ToCursorPagedResponse(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));
        act.Should().Throw<ArgumentNullException>().WithParameterName("pagedList");
    }

    [Fact]
    public void ToCursorPagedResponse_WithNullKeySelector_ThrowsArgumentNullException()
    {
        ICursorPagedList<int> pagedList = CursorPagedList<int>.Empty;
        Action act = () => pagedList.ToCursorPagedResponse<int, string>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("keySelector");
    }
    
    [Fact]
    public void ToCursorPagedResponse_WithNullRawCursorSelector_ThrowsArgumentNullException()
    {
        ICursorPagedList<int> pagedList = CursorPagedList<int>.Empty;
        Func<int, RawCursorValue> rawCursorSelector = null!;
        Action act = () => pagedList.ToCursorPagedResponse(rawCursorSelector);
        act.Should().Throw<ArgumentNullException>().WithParameterName("rawCursorSelector");
    }

    [Fact]
    public void ToCursorPagedResponse_MapsPropertiesCorrectly()
    {
        var items = new List<int> { 1, 2 };
        var startCursor = "enc_1";
        var endCursor = "enc_2";
        
        var pagedList = CursorPagedList<int>.Create(items, startCursor, endCursor, hasPreviousPage: true, hasNextPage: false);
        
        var response = pagedList.ToCursorPagedResponse(item => item.ToString(System.Globalization.CultureInfo.InvariantCulture));
        
        response.PageInfo.StartCursor.Should().Be("enc_1");
        response.PageInfo.EndCursor.Should().Be("enc_2");
        response.PageInfo.HasPreviousPage.Should().BeTrue();
        response.PageInfo.HasNextPage.Should().BeFalse();
        
        response.Edges.Count.Should().Be(2);
        
        // Cursors are encoded by the extension method
        response.Edges[0].Node.Should().Be(1);
        response.Edges[0].Cursor.Should().Be(HmacCursorEncoder.DevelopmentDefault.Encode("S|1"));
        
        response.Edges[1].Node.Should().Be(2);
        response.Edges[1].Cursor.Should().Be(HmacCursorEncoder.DevelopmentDefault.Encode("S|2"));
    }
    
    [Fact]
    public void RawCursorValue_Constructor()
    {
        var raw = new RawCursorValue("test");
        raw.Value.Should().Be("test");
    }

    private sealed class DummyCursorEncoder : ICursorEncoder
    {
        public string? Encode(string? plainText) => plainText == null ? null : "DUMMY_" + plainText;
        public string? Decode(string? encodedText) => encodedText?.Replace("DUMMY_", "", StringComparison.Ordinal);
    }

    [Fact]
    public void ToCursorPagedResponse_UsesProvidedEncoder()
    {
        var items = new List<int> { 1, 2 };
        var startCursor = "enc_1";
        var endCursor = "enc_2";
        
        var pagedList = CursorPagedList<int>.Create(items, startCursor, endCursor, hasPreviousPage: true, hasNextPage: false);
        
        var response = pagedList.ToCursorPagedResponse(
            item => item.ToString(System.Globalization.CultureInfo.InvariantCulture),
            new DummyCursorEncoder());
        
        response.Edges[0].Cursor.Should().Be("DUMMY_S|1");
        response.Edges[1].Cursor.Should().Be("DUMMY_S|2");
    }
}

