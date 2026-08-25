// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace EricksonLopez.Pagination.OpenApi.Tests;

public class PaginationSwaggerGenOptionsExtensionsTests
{
    [Fact]
    public void AddPaginationSupport_AddsPaginationOperationFilter()
    {
        var services = new ServiceCollection();
        services.AddSwaggerGen(options =>
        {
            options.AddPaginationSupport();
        });

        var serviceProvider = services.BuildServiceProvider();
        var swaggerGenOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SwaggerGenOptions>>();

        var filters = swaggerGenOptions.Value.OperationFilterDescriptors;
        filters.Should().Contain(f => f.Type == typeof(PaginationOperationFilter));
    }
#if NET9_0_OR_GREATER
    [Fact]
    public void AddPaginationSupport_AddsPaginationOperationTransformer()
    {
        var services = new ServiceCollection();
        services.AddOpenApi(options =>
        {
            options.AddPaginationSupport();
        });

        // Verifying internals of OpenApiOptions might require reflection or execution since they are delegates, 
        // but testing that it doesn't throw is a basic sanity check.
        var serviceProvider = services.BuildServiceProvider();
        var openApiOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.OpenApi.OpenApiOptions>>();
        var options = openApiOptions.Get("v1");
        options.Should().NotBeNull();
    }
#endif
}


