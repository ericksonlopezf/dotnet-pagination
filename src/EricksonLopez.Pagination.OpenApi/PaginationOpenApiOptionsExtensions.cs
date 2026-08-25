// Copyright © Erickson Lopez. MIT License.
#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.OpenApi;

namespace EricksonLopez.Pagination.OpenApi;

/// <summary>
/// Provides extension methods for configuring pagination support in OpenAPI.
/// </summary>
public static class PaginationOpenApiOptionsExtensions
{
    /// <summary>
    /// Configures OpenAPI options to include pagination, filtering, and sorting parameter descriptions.
    /// </summary>
    /// <param name="options">The OpenAPI options to configure.</param>
    [ExcludeFromCodeCoverage]
    public static void AddPaginationSupport(this OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, cancellationToken) => 
        {
            var transformer = new PaginationOperationTransformer();
            return transformer.TransformAsync(operation, context, cancellationToken);
        });
    }
}
#endif
