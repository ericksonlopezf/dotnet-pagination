// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EricksonLopez.Pagination.AspNetCore;

#if NET8_0_OR_GREATER
/// <summary>
/// Provides an exception handler that translates pagination cursor exceptions into HTTP 400 Bad Request responses.
/// </summary>
public sealed class PaginationExceptionHandler : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not InvalidPaginationCursorException cursorException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid Pagination Cursor",
            Detail = cursorException.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
        problemDetails.Extensions["cursor"] = cursorException.OpaqueCursor;

        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status400BadRequest;
#pragma warning disable IL2026, IL3050 // JSON serialization and deserialization might require types that cannot be statically analyzed. ProblemDetails is usually supported.
        await response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);
#pragma warning restore IL2026, IL3050
        
        return true;
    }
}
#endif



