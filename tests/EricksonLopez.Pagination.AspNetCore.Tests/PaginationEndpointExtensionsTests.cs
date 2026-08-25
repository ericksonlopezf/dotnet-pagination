// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationEndpointExtensionsTests
{
    [Fact]
    public void AddPaginationValidation_RouteHandlerBuilder_AddsFilter()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var routeHandlerBuilder = app.MapGet("/test", () => "test");
        var result = routeHandlerBuilder.AddPaginationValidation();

        result.Should().BeSameAs(routeHandlerBuilder);
    }

    [Fact]
    public void AddPaginationValidation_RouteGroupBuilder_AddsFilter()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var groupBuilder = app.MapGroup("/group");
        var result = groupBuilder.AddPaginationValidation();

        result.Should().BeSameAs(groupBuilder);
    }
}



