// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EricksonLopez.Pagination.OpenApi;

/// <summary>
/// Provides a Swagger operation filter that enriches operations with pagination, filtering, and sorting parameter documentation.
/// </summary>
[RequiresUnreferencedCode("Swashbuckle is not compatible with Native AOT or trimming. Use Microsoft.AspNetCore.OpenApi instead for AOT scenarios.")]
[RequiresDynamicCode("Swashbuckle is not compatible with Native AOT or trimming. Use Microsoft.AspNetCore.OpenApi instead for AOT scenarios.")]
public class PaginationOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Swashbuckle itself is not trim-compatible. Warned at class level.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Swashbuckle itself is not AOT-compatible. Warned at class level.")]
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var (hasPagination, hasCursorPagination, hasFilter, hasSort) = OpenApiParameterDetector.DetectParameters(context.ApiDescription?.ParameterDescriptions);

        // Stryker disable once all : Performance optimization
        if (!hasPagination && !hasCursorPagination && !hasFilter && !hasSort) return;

        operation.Parameters ??= new List<OpenApiParameter>();

        // Annotate existing Swashbuckle-generated parameters with descriptions
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
                param.Example ??= new Microsoft.OpenApi.Any.OpenApiString("name~=John,age>=18,isActive=true");
            }

            if (hasSort && (param.Name.Equals("sortBy", StringComparison.OrdinalIgnoreCase) || param.Name.Equals("sort", StringComparison.OrdinalIgnoreCase)))
            {
                param.Description ??=
                    "Comma-separated sort expression. Each clause is a property name optionally followed by `asc` or `desc`. " +
                    "Example: `name asc,createdAt desc`";
                param.Example ??= new Microsoft.OpenApi.Any.OpenApiString("name asc,createdAt desc");
            }
        }

        // If filter param exists in method but Swashbuckle didn't auto-generate it, inject it
        // Stryker disable once all : Performance optimization
        var parameterList = operation.Parameters as List<OpenApiParameter> ?? operation.Parameters.ToList();
        if (hasFilter && !parameterList.Exists(p =>
            p.Name.Equals("filter", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "filter",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
                Description =
                    "A comma-separated filter expression. " +
                    "Supported operators: `=`, `!=`, `>`, `<`, `>=`, `<=`, `~=` (contains), `^=` (starts with), `$=` (ends with). " +
                    "Example: `name~=John,age>=18,isActive=true`",
                Example = new Microsoft.OpenApi.Any.OpenApiString("name~=John,age>=18,isActive=true")
            });
        }

        // Inject sortBy if the method declares it and Swashbuckle didn't auto-generate it
        if (hasSort && !parameterList.Exists(p =>
            p.Name.Equals("sortBy", StringComparison.OrdinalIgnoreCase)))
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
                Example = new Microsoft.OpenApi.Any.OpenApiString("name asc,createdAt desc")
            });
        }
    }
}
