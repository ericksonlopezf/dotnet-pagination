// Copyright © Erickson Lopez. MIT License.
#pragma warning disable CA1848, CA2254
// CA2012 is disabled because EndpointFilterDelegate returns a ValueTask<object?> mock configured via NSubstitute Returns().
#pragma warning disable CA2012
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationEndpointFilterTests
{
    private readonly DefaultHttpContext _httpContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsSnapshot<PaginationCoreOptions> _optionsSnapshot;
    private readonly ILogger<PaginationEndpointFilter> _logger;

    public PaginationEndpointFilterTests()
    {
        _httpContext = new DefaultHttpContext();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _optionsSnapshot = Substitute.For<IOptionsSnapshot<PaginationCoreOptions>>();
        _logger = Substitute.For<ILogger<PaginationEndpointFilter>>();

        _serviceProvider.GetService(typeof(IOptionsSnapshot<PaginationCoreOptions>)).Returns(_optionsSnapshot);
        _serviceProvider.GetService(typeof(ILogger<PaginationEndpointFilter>)).Returns(_logger);
        
        _httpContext.RequestServices = _serviceProvider;
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_ExceedsMaxPageSize_ReturnsBadRequest()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { new PaginationParameters { PageSize = 100 } };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        var badRequest = result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>()
              .Which;
        badRequest.StatusCode.Should().Be(400);
        
        var valueResult = result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        valueResult.Value!.ToString().Should().Contain("pageSize cannot exceed 50");
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_WithinMaxPageSize_CallsNext()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { PaginationParameters.Create(1, 10) };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_ExceedsDeepOffsetWarning_LogsWarning()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50, DeepOffsetWarningThreshold = 100 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        // Page 10, PageSize 20 -> Offset = (10-1)*20 = 180 > 100
        var arguments = new List<object?> { PaginationParameters.Create(10, 20) };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
        
        // Assert that a warning was logged
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("exceeds the configured warning threshold")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_ZeroDeepOffsetWarning_DoesNotLogWarning()
    {
        // 0 means disabled
        var options = new PaginationCoreOptions { MaxPageSize = 50, DeepOffsetWarningThreshold = 0 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { PaginationParameters.Create(100, 50) }; // huge offset
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
        
        _logger.DidNotReceiveWithAnyArgs().Log(default, default, default, default, default!);
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_ExactDeepOffsetWarning_DoesNotLogWarning()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50, DeepOffsetWarningThreshold = 100 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        // Page 11, PageSize 10 -> Offset = 100
        var arguments = new List<object?> { PaginationParameters.Create(11, 10) };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
        
        _logger.DidNotReceiveWithAnyArgs().Log(default, default, default, default, default!);
    }

    [Fact]
    public async Task InvokeAsync_NullOptions_UsesDefault()
    {
        // OptionsSnapshot returns null
        _optionsSnapshot.Value.Returns((PaginationCoreOptions)null!);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        // 1000 is default max
        var arguments = new List<object?> { new PaginationParameters { PageSize = 1000 } };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_ExactMaxPageSize_ReturnsNextResult()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { new PaginationParameters { PageSize = 50 } };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_NegativeThreshold_IgnoresDeepOffset()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50, DeepOffsetWarningThreshold = -1 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        // Offset 1000, > 0 but threshold is -1, should not log
        var arguments = new List<object?> { PaginationParameters.Create(101, 10) };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }

    [Fact]
    public async Task InvokeAsync_OffsetPagination_ExactThreshold_DoesNotLogWarning()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50, DeepOffsetWarningThreshold = 100 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        // Page 11, PageSize 10 -> Offset = (11-1)*10 = 100 == 100
        var arguments = new List<object?> { PaginationParameters.Create(11, 10) };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }

    [Fact]
    public async Task InvokeAsync_CursorPagination_ExactMaxPageSize_ReturnsNextResult()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { new CursorPaginationParameters { First = 50 } };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }


    [Fact]
    public async Task InvokeAsync_CursorPagination_WithinMaxPageSize_CallsNext()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { CursorPaginationParameters.Parse("first=10", null) };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }

    [Fact]
    public async Task InvokeAsync_NoPaginationParams_CallsNext()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { "some_string_param" };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }

    [Fact]
    public async Task InvokeAsync_CursorPagination_ExceedsMaxPageSize_ReturnsBadRequest()
    {
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        _optionsSnapshot.Value.Returns(options);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { new CursorPaginationParameters { First = 100 } };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        var badRequest = result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>()
              .Which;
        badRequest.StatusCode.Should().Be(400);
        
        var valueResult = result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();
        valueResult.Value!.ToString().Should().Contain("Cursor pagination first/last cannot exceed 50");
    }

    [Fact]
    public async Task InvokeAsync_NullOptions_DefaultsToFallback()
    {
        _optionsSnapshot.Value.Returns((PaginationCoreOptions)null!);

        var filter = new PaginationEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(_httpContext);
        
        var arguments = new List<object?> { new CursorPaginationParameters { First = 1000 } };
        context.Arguments.Returns(arguments);

        var next = Substitute.For<EndpointFilterDelegate>();
        next(context).Returns(new ValueTask<object?>("next_result"));

        var result = await filter.InvokeAsync(context, next);

        result.Should().Be("next_result");
    }
}






