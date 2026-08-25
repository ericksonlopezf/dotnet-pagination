// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides an endpoint filter for Minimal APIs that enforces pagination configuration limits.
/// </summary>
public class PaginationEndpointFilter : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var optionsSnapshot = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<PaginationCoreOptions>>();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<PaginationEndpointFilter>>();

        var options = optionsSnapshot?.Value ?? new PaginationCoreOptions();

        for (int i = 0; i < context.Arguments.Count; i++)
        {
            var argument = context.Arguments[i];

            if (argument is PaginationParameters offsetParams)
            {
                if (offsetParams.PageSize > options.MaxPageSize)
                {
                    return Results.BadRequest(new { error = $"pageSize cannot exceed {options.MaxPageSize}." });
                }

                if (options.DeepOffsetWarningThreshold > 0)
                {
                    long offset = (offsetParams.Page - 1L) * offsetParams.PageSize;
                    if (offset > options.DeepOffsetWarningThreshold)
                    {
                        logger?.LogWarning("Pagination request requested an offset of {Offset} which exceeds the configured warning threshold of {Threshold}. This can cause severe performance degradation on large tables.", offset, options.DeepOffsetWarningThreshold);
                    }
                }
            }
            else if (argument is CursorPaginationParameters cursorParams)
            {
                var pageSize = cursorParams.GetPageSize(options.DefaultPageSize);
                if (pageSize > options.MaxPageSize)
                {
                    return Results.BadRequest(new { error = $"Cursor pagination first/last cannot exceed {options.MaxPageSize}." });
                }
            }
        }

        return await next(context);
    }
}



