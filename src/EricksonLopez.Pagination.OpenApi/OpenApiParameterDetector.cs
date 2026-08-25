// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace EricksonLopez.Pagination.OpenApi;

/// <summary>
/// A shared utility for detecting pagination parameters in OpenAPI operations.
/// </summary>

internal static class OpenApiParameterDetector
{
    public static (bool HasPagination, bool HasCursorPagination, bool HasFilter, bool HasSort) DetectParameters(
        IList<ApiParameterDescription>? apiParams)
    {
        if (apiParams == null)
            return (false, false, false, false);

        bool hasPagination = apiParams.Any(p => p.Type == typeof(PaginationParameters) || p.Name.Equals("page", StringComparison.OrdinalIgnoreCase));
        bool hasCursorPagination = apiParams.Any(p => p.Type == typeof(CursorPaginationParameters) || p.Name.Equals("first", StringComparison.OrdinalIgnoreCase));
        bool hasFilter = apiParams.Any(p => p.Type == typeof(FilterParameters) || p.Name.Equals("filter", StringComparison.OrdinalIgnoreCase));
        bool hasSort = apiParams.Any(p => p.Type == typeof(SortParameters) || p.Name.Equals("sortBy", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("sort", StringComparison.OrdinalIgnoreCase));

        return (hasPagination, hasCursorPagination, hasFilter, hasSort);
    }
}

