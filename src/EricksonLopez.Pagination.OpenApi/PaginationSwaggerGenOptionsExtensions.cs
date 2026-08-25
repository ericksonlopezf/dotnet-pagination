// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EricksonLopez.Pagination.OpenApi;

/// <summary>
/// Provides extension methods for configuring pagination support in Swashbuckle Swagger generation.
/// </summary>
public static class PaginationSwaggerGenOptionsExtensions
{
    /// <summary>
    /// Configures Swashbuckle options to include pagination, filtering, and sorting parameter descriptions.
    /// </summary>
    /// <param name="options">The SwaggerGen options to configure.</param>
    [RequiresUnreferencedCode("Swashbuckle is not compatible with Native AOT or trimming. Use Microsoft.AspNetCore.OpenApi instead for AOT scenarios.")]
    [RequiresDynamicCode("Swashbuckle is not compatible with Native AOT or trimming. Use Microsoft.AspNetCore.OpenApi instead for AOT scenarios.")]
    public static void AddPaginationSupport(this SwaggerGenOptions options)
    {
        options.OperationFilter<PaginationOperationFilter>();
    }
}



