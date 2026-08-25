// Copyright © Erickson Lopez. MIT License.
#pragma warning disable CA1308
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationResultExtensionsTests
{
    [Fact]
    public async Task ToPagedResult_WithMaxAge_SetsHeadersAndExecutesOk()
    {
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var pagedList = PagedList<string>.WithCount(new[] { "Item1" }, parameters, 1);
        var request = Substitute.For<HttpRequest>();
        request.Scheme.Returns("http");
        request.Host.Returns(new HostString("localhost"));
        request.Path.Returns(new PathString("/api/items"));

        var maxAge = TimeSpan.FromSeconds(60);
        var result = pagedList.ToPagedResult(request, maxAge);

        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Headers.CacheControl.ToString().Should().Be("private, max-age=60");
        context.Response.Headers.ETag.ToString().Should().NotBeEmpty();
        context.Response.Headers.Vary.ToString().Should().Be("Accept-Encoding");
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ToPagedResult_WithIfNoneMatch_Returns304NotModified()
    {
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var pagedList = PagedList<string>.WithCount(new[] { "Item1" }, parameters, 1);
        var request = Substitute.For<HttpRequest>();
        request.Scheme.Returns("http");
        request.Host.Returns(new HostString("localhost"));
        request.Path.Returns(new PathString("/api/items"));

        var result = pagedList.ToPagedResult(request);

        // First execution to get the ETag
        var context1 = new DefaultHttpContext();
        context1.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context1.Response.Body = new MemoryStream();
        await result.ExecuteAsync(context1);
        var etag = context1.Response.Headers.ETag.ToString();

        // Second execution with matching If-None-Match
        var context2 = new DefaultHttpContext();
        context2.Request.Headers.IfNoneMatch = etag;
        context2.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context2.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context2);

        context2.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);

        // Third execution with non-matching If-None-Match
        var context3 = new DefaultHttpContext();
        context3.Request.Headers.IfNoneMatch = "\"different-etag\"";
        context3.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context3.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context3);

        context3.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ToCursorPagedResult_WithMaxAge_SetsHeadersAndExecutesOk()
    {
        var parameters = new CursorPaginationParameters { First = 10 };
        var pagedList = CursorPagedList<string>.Create(new[] { "Item1" }, "start", "end", false, false);
        var maxAge = TimeSpan.FromSeconds(120);
        var result = pagedList.ToCursorPagedResult(x => x, null, maxAge);

        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Headers.CacheControl.ToString().Should().Be("private, max-age=120");
        context.Response.Headers.ETag.ToString().Should().NotBeEmpty();
        context.Response.Headers.Vary.ToString().Should().Be("Accept-Encoding");
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ToCursorPagedResult_WithIfNoneMatch_Returns304NotModified()
    {
        var parameters = new CursorPaginationParameters { First = 10 };
        var pagedList = CursorPagedList<string>.Create(new[] { "Item1" }, "start", "end", false, false);
        var result = pagedList.ToCursorPagedResult(x => x);

        // First execution to get the ETag
        var context1 = new DefaultHttpContext();
        context1.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context1.Response.Body = new MemoryStream();
        await result.ExecuteAsync(context1);
        var etag = context1.Response.Headers.ETag.ToString();

        // Second execution with matching If-None-Match
        var context2 = new DefaultHttpContext();
        context2.Request.Headers.IfNoneMatch = etag;
        context2.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context2.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context2);

        context2.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);

        // Third execution with non-matching If-None-Match
        var context3 = new DefaultHttpContext();
        context3.Request.Headers.IfNoneMatch = "\"different-etag\"";
        context3.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context3.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context3);

        context3.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void ToPagedResult_WithCustomETagFactory_UsesCustomETag()
    {
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var pagedList = PagedList<string>.WithCount(new[] { "Item1" }, parameters, 1);
        var request = Substitute.For<HttpRequest>();
        request.Scheme.Returns("http");
        request.Host.Returns(new HostString("localhost"));
        request.Path.Returns(new PathString("/api/items"));

        var response = pagedList.ToPagedResponse(request);
        var etagOptions = new PaginationETagOptions { CustomETagFactory = _ => "\"custom-etag\"" };
        var context = new DefaultHttpContext();
        
        response.ApplyETagHeaders(context, etagOptions: etagOptions);

        context.Response.Headers.ETag.ToString().Should().Be("\"custom-etag\"");

        // NormalizeETag handles unquoted, leading-only quote, and trailing-only quote
        var unquotedOptions = new PaginationETagOptions { CustomETagFactory = _ => "custom-etag" };
        var unquotedContext = new DefaultHttpContext();
        response.ApplyETagHeaders(unquotedContext, etagOptions: unquotedOptions);
        unquotedContext.Response.Headers.ETag.ToString().Should().Be("\"custom-etag\"");

        var leadingQuoteOptions = new PaginationETagOptions { CustomETagFactory = _ => "\"custom-etag" };
        var leadingQuoteContext = new DefaultHttpContext();
        response.ApplyETagHeaders(leadingQuoteContext, etagOptions: leadingQuoteOptions);
        leadingQuoteContext.Response.Headers.ETag.ToString().Should().Be("\"\"custom-etag\"");

        var trailingQuoteOptions = new PaginationETagOptions { CustomETagFactory = _ => "custom-etag\"" };
        var trailingQuoteContext = new DefaultHttpContext();
        response.ApplyETagHeaders(trailingQuoteContext, etagOptions: trailingQuoteOptions);
        trailingQuoteContext.Response.Headers.ETag.ToString().Should().Be("\"custom-etag\"\"");
    }

    [Fact]
    public void ToCursorPagedResult_WithCustomCursorETagFactory_UsesCustomETag()
    {
        var pagedList = CursorPagedList<string>.Create(new[] { "Item1" }, "start", "end", false, false);
        var response = pagedList.ToCursorPagedResponse(x => x);
        var etagOptions = new PaginationETagOptions { CustomCursorETagFactory = _ => "custom-cursor-etag" };
        
        var context = new DefaultHttpContext();
        response.ApplyETagHeaders(context, etagOptions: etagOptions);

        context.Response.Headers.ETag.ToString().Should().Be("\"custom-cursor-etag\"");
    }

    [Fact]
    public void PagedResponse_ETag_DiffersWhenAnyPropertyChanges()
    {
        var baseResponse = new PagedResponse<string>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 100,
            HasNextPage = true,
            HasPreviousPage = false,
            Items = new[] { "A", "B" }
        };

        var contextBase = new DefaultHttpContext();
        baseResponse.ApplyETagHeaders(contextBase);
        var baseETag = contextBase.Response.Headers.ETag.ToString();

        // 1. Page change
        var pageChanged = new PagedResponse<string> { Page = 2, PageSize = 10, TotalCount = 100, HasNextPage = true, Items = new[] { "A", "B" } };
        var ctx1 = new DefaultHttpContext(); pageChanged.ApplyETagHeaders(ctx1);
        ctx1.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 2. PageSize change
        var sizeChanged = new PagedResponse<string> { Page = 1, PageSize = 20, TotalCount = 100, HasNextPage = true, Items = new[] { "A", "B" } };
        var ctx2 = new DefaultHttpContext(); sizeChanged.ApplyETagHeaders(ctx2);
        ctx2.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 3. TotalCount change
        var countChanged = new PagedResponse<string> { Page = 1, PageSize = 10, TotalCount = 200, HasNextPage = true, Items = new[] { "A", "B" } };
        var ctx3 = new DefaultHttpContext(); countChanged.ApplyETagHeaders(ctx3);
        ctx3.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 4. TotalCount null
        var countNull = new PagedResponse<string> { Page = 1, PageSize = 10, TotalCount = null, HasNextPage = true, Items = new[] { "A", "B" } };
        var ctx4 = new DefaultHttpContext(); countNull.ApplyETagHeaders(ctx4);
        ctx4.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 5. HasNextPage change
        var nextChanged = new PagedResponse<string> { Page = 1, PageSize = 10, TotalCount = 100, HasNextPage = false, Items = new[] { "A", "B" } };
        var ctx5 = new DefaultHttpContext(); nextChanged.ApplyETagHeaders(ctx5);
        ctx5.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 6. Items count change
        var itemsCountChanged = new PagedResponse<string> { Page = 1, PageSize = 10, TotalCount = 100, HasNextPage = true, Items = new[] { "A" } };
        var ctx6 = new DefaultHttpContext(); itemsCountChanged.ApplyETagHeaders(ctx6);
        ctx6.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 7. Items content change
        var itemsContentChanged = new PagedResponse<string> { Page = 1, PageSize = 10, TotalCount = 100, HasNextPage = true, Items = new[] { "A", "C" } };
        var ctx7 = new DefaultHttpContext(); itemsContentChanged.ApplyETagHeaders(ctx7);
        ctx7.Response.Headers.ETag.ToString().Should().NotBe(baseETag);
    }

    [Fact]
    public void CursorPagedResponse_ETag_DiffersWhenAnyPropertyChanges()
    {
        var baseResponse = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo
            {
                StartCursor = "c1",
                EndCursor = "c2",
                HasNextPage = true,
                HasPreviousPage = false
            },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "B", Cursor = "c2" } }
        };

        var contextBase = new DefaultHttpContext();
        baseResponse.ApplyETagHeaders(contextBase);
        var baseETag = contextBase.Response.Headers.ETag.ToString();

        // 1. StartCursor change
        var startChanged = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = "c_alt", EndCursor = "c2", HasNextPage = true, HasPreviousPage = false },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "B", Cursor = "c2" } }
        };
        var ctx1 = new DefaultHttpContext(); startChanged.ApplyETagHeaders(ctx1);
        ctx1.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 2. StartCursor null
        var startNull = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = null, EndCursor = "c2", HasNextPage = true, HasPreviousPage = false },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "B", Cursor = "c2" } }
        };
        var ctx2 = new DefaultHttpContext(); startNull.ApplyETagHeaders(ctx2);
        ctx2.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 3. EndCursor change
        var endChanged = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = "c1", EndCursor = "c_alt", HasNextPage = true, HasPreviousPage = false },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "B", Cursor = "c2" } }
        };
        var ctx3 = new DefaultHttpContext(); endChanged.ApplyETagHeaders(ctx3);
        ctx3.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 4. EndCursor null
        var endNull = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = "c1", EndCursor = null, HasNextPage = true, HasPreviousPage = false },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "B", Cursor = "c2" } }
        };
        var ctx4 = new DefaultHttpContext(); endNull.ApplyETagHeaders(ctx4);
        ctx4.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 5. HasNextPage change
        var nextChanged = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = "c1", EndCursor = "c2", HasNextPage = false, HasPreviousPage = false },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "B", Cursor = "c2" } }
        };
        var ctx5 = new DefaultHttpContext(); nextChanged.ApplyETagHeaders(ctx5);
        ctx5.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 6. HasPreviousPage change
        var prevChanged = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = "c1", EndCursor = "c2", HasNextPage = true, HasPreviousPage = true },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "B", Cursor = "c2" } }
        };
        var ctx6 = new DefaultHttpContext(); prevChanged.ApplyETagHeaders(ctx6);
        ctx6.Response.Headers.ETag.ToString().Should().NotBe(baseETag);

        // 7. Edges content change
        var edgesContentChanged = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = "c1", EndCursor = "c2", HasNextPage = true, HasPreviousPage = false },
            Edges = new[] { new Edge<string> { Node = "A", Cursor = "c1" }, new Edge<string> { Node = "C", Cursor = "c2" } }
        };
        var ctx7 = new DefaultHttpContext(); edgesContentChanged.ApplyETagHeaders(ctx7);
        ctx7.Response.Headers.ETag.ToString().Should().NotBe(baseETag);
    }

    [Fact]
    public void PagedResponse_ETag_MatchesExactDeterministicHash()
    {
        // 1. Without items
        var respEmpty = new PagedResponse<string>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = null,
            HasNextPage = false,
            Items = Array.Empty<string>()
        };
        var ctxEmpty = new DefaultHttpContext();
        respEmpty.ApplyETagHeaders(ctxEmpty);
        var expectedEmpty = "\"" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("1|10|null|0|0")), 0, 8).ToLowerInvariant() + "\"";
        ctxEmpty.Response.Headers.ETag.ToString().Should().Be(expectedEmpty);

        // 2. With items
        var respItems = new PagedResponse<string>
        {
            Page = 2,
            PageSize = 5,
            TotalCount = 20,
            HasNextPage = true,
            Items = new[] { "itemA", "itemB" }
        };
        var ctxItems = new DefaultHttpContext();
        respItems.ApplyETagHeaders(ctxItems);
        var expectedItems = "\"" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("2|5|20|1|2|[\"itemA\",\"itemB\"]")), 0, 8).ToLowerInvariant() + "\"";
        ctxItems.Response.Headers.ETag.ToString().Should().Be(expectedItems);
    }

    [Fact]
    public void CursorPagedResponse_ETag_MatchesExactDeterministicHash()
    {
        // 1. Empty cursors and edges
        var respEmpty = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = null, EndCursor = null, HasNextPage = false, HasPreviousPage = false },
            Edges = Array.Empty<Edge<string>>()
        };
        var ctxEmpty = new DefaultHttpContext();
        respEmpty.ApplyETagHeaders(ctxEmpty);
        var expectedEmpty = "\"" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("null|null|0|0|0")), 0, 8).ToLowerInvariant() + "\"";
        ctxEmpty.Response.Headers.ETag.ToString().Should().Be(expectedEmpty);

        // 2. With cursors and edges
        var respEdges = new CursorPagedResponse<string>
        {
            PageInfo = new RelayPageInfo { StartCursor = "c1", EndCursor = "c2", HasNextPage = true, HasPreviousPage = false },
            Edges = new[] { new Edge<string> { Node = "itemA", Cursor = "c1" } }
        };
        var ctxEdges = new DefaultHttpContext();
        respEdges.ApplyETagHeaders(ctxEdges);
        var expectedEdges = "\"" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("c1|c2|1|0|1|[{\"cursor\":\"c1\",\"node\":\"itemA\"}]")), 0, 8).ToLowerInvariant() + "\"";
        ctxEdges.Response.Headers.ETag.ToString().Should().Be(expectedEdges);
    }
}





