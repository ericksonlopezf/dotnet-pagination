// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#if NET8_0_OR_GREATER
namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_NonCursorException_ReturnsFalse()
    {
        var handler = new PaginationExceptionHandler();
        var context = new DefaultHttpContext();
        
        var result = await handler.TryHandleAsync(context, new InvalidOperationException("Normal error"), CancellationToken.None);
        
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_CursorException_ReturnsTrueAndWritesResponse()
    {
        var handler = new PaginationExceptionHandler();
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        var stream = new MemoryStream();
        context.Response.Body = stream;

        var exception = new InvalidPaginationCursorException("Bad cursor", "invalid-value", new InvalidOperationException());
        
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain("Invalid Pagination Cursor");
        responseBody.Should().Contain("Bad cursor");
        responseBody.Should().Contain("invalid-value");
        responseBody.Should().Contain("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        responseBody.Should().Contain("\"cursor\":");
    }
}
#endif





