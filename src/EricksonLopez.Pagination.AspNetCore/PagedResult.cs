// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EricksonLopez.Pagination.AspNetCore;

internal sealed class PagedResult<T> : IResult
{
    private readonly PagedResponse<T> _response;
    private readonly TimeSpan? _maxAge;

    public PagedResult(PagedResponse<T> response, TimeSpan? maxAge)
    {
        _response = response;
        _maxAge = maxAge;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Internal class — controlled usage. Consumers use PaginationETagOptions.CustomETagFactory to bypass JsonSerializer in AOT/trimmed apps.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Internal class — controlled usage. Consumers use PaginationETagOptions.CustomETagFactory to bypass JsonSerializer in AOT/trimmed apps.")]
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        if (_response.ApplyETagHeaders(httpContext, _maxAge))
        {
            httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        await Results.Ok(_response).ExecuteAsync(httpContext).ConfigureAwait(false);
    }
}
