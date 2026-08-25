// Copyright © Erickson Lopez. MIT License.
#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace EricksonLopez.Pagination.OpenApi;

/// <summary>
/// Provides an OpenAPI operation transformer that enriches operations with pagination, filtering, and sorting parameter documentation.
/// </summary>
public class PaginationOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var (hasPagination, hasCursorPagination, hasFilter, hasSort) = OpenApiParameterDetector.DetectParameters(context.Description.ParameterDescriptions);

        // Stryker disable once all : Performance optimization
        if (!hasPagination && !hasCursorPagination && !hasFilter && !hasSort)
            return Task.CompletedTask;

        foreach (var param in operation.Parameters)
        {
            if (hasPagination)
            {
                if (param.Name.Equals("page", StringComparison.OrdinalIgnoreCase))
                    param.Description ??= "The 1-indexed page number to request. Defaults to 1.";
                else if (param.Name.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
                    param.Description ??= "The number of items per page. Defaults to 10.";
            }

            if (hasCursorPagination)
            {
                if (param.Name.Equals("first", StringComparison.OrdinalIgnoreCase))
                    param.Description ??= "Forward cursor pagination: the number of items to return from the start of the result set.";
                else if (param.Name.Equals("last", StringComparison.OrdinalIgnoreCase))
                    param.Description ??= "Backward cursor pagination: the number of items to return from the end of the result set.";
                else if (param.Name.Equals("after", StringComparison.OrdinalIgnoreCase))
                    param.Description ??= "An opaque cursor returned from the previous page. Items after this cursor are returned.";
                else if (param.Name.Equals("before", StringComparison.OrdinalIgnoreCase))
                    param.Description ??= "An opaque cursor returned from the next page. Items before this cursor are returned.";
            }

            if (hasFilter && param.Name.Equals("filter", StringComparison.OrdinalIgnoreCase))
            {
                param.Description ??=
                    "A comma-separated filter expression. " +
                    "Supported operators: `=` (equals), `!=` (not equals), `>`, `<`, `>=`, `<=` (comparisons), " +
                    "`~=` (contains), `^=` (starts with), `$=` (ends with). " +
                    "Example: `name~=John,age>=18,isActive=true`";
                param.Example ??= new OpenApiString("name~=John,age>=18,isActive=true");
            }

            if (hasSort && (param.Name.Equals("sortBy", StringComparison.OrdinalIgnoreCase) || param.Name.Equals("sort", StringComparison.OrdinalIgnoreCase)))
            {
                param.Description ??=
                    "Comma-separated sort expression. Each clause is a property name optionally followed by `asc` or `desc`. " +
                    "Example: `name asc,createdAt desc`";
                param.Example ??= new OpenApiString("name asc,createdAt desc");
            }
        }

        // If filter or sort params exist in method but Microsoft.AspNetCore.OpenApi didn't auto-generate them, inject them
        // Stryker disable once all : Performance optimization
        var parameterList = operation.Parameters as List<OpenApiParameter> ?? operation.Parameters.ToList();
        
        if (hasFilter && !parameterList.Exists(p => p.Name.Equals("filter", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "filter",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
                Description = 
                    "A comma-separated filter expression. " +
                    "Supported operators: `=` (equals), `!=` (not equals), `>`, `<`, `>=`, `<=` (comparisons), " +
                    "`~=` (contains), `^=` (starts with), `$=` (ends with). " +
                    "Example: `name~=John,age>=18,isActive=true`",
                Example = new OpenApiString("name~=John,age>=18,isActive=true")
            });
        }

        if (hasSort && !parameterList.Exists(p => p.Name.Equals("sortBy", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "sortBy",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
                Description = 
                    "Comma-separated sort expression. Each clause is a property name optionally followed by `asc` or `desc`. " +
                    "Example: `name asc,createdAt desc`",
                Example = new OpenApiString("name asc,createdAt desc")
            });
        }

        return Task.CompletedTask;
    }
}
#endif





