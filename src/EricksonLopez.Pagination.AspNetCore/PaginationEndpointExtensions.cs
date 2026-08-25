// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides extension methods for registering pagination validation filters on Minimal API endpoints.
/// </summary>
public static class PaginationEndpointExtensions
{
    /// <summary>
    /// Adds pagination validation endpoint filters to the route handler builder.
    /// </summary>
    /// <param name="builder">The route handler builder to configure.</param>
    /// <returns>The configured <see cref="RouteHandlerBuilder"/> instance.</returns>
    public static RouteHandlerBuilder AddPaginationValidation(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<PaginationEndpointFilter>();
    }

    /// <summary>
    /// Adds pagination validation endpoint filters to the route group builder.
    /// </summary>
    /// <param name="builder">The route group builder to configure.</param>
    /// <returns>The configured <see cref="RouteGroupBuilder"/> instance.</returns>
    public static RouteGroupBuilder AddPaginationValidation(this RouteGroupBuilder builder)
    {
        return builder.AddEndpointFilter<PaginationEndpointFilter>();
    }
}

