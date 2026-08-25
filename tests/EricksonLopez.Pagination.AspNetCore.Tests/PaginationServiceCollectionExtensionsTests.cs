// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPagination_RegistersServicesCorrectly()
    {
        var services = new ServiceCollection();
        services.AddMvcCore();

        services.AddPagination(options =>
        {
            options.DefaultPageSize = 25;
            options.MaxPageSize = 250;
        });

        var provider = services.BuildServiceProvider();

        // Check options
        var options = provider.GetRequiredService<IOptions<PaginationCoreOptions>>().Value;
        options.DefaultPageSize.Should().Be(25);
        options.MaxPageSize.Should().Be(250);

        // Check encoder
        var encoder = provider.GetRequiredService<ICursorEncoder>();
        encoder.Should().BeOfType<HmacCursorEncoder>();

        // Check MVC options binder provider
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        var binderProvider = mvcOptions.ModelBinderProviders.FirstOrDefault(p => p is PaginationParametersModelBinderProvider);
        binderProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddPagination_RegistersValidateOnStart()
    {
        var services = new ServiceCollection();

        services.AddPagination(options =>
        {
            options.DefaultPageSize = -1; // Invalid!
        });

        // 1. Verify PaginationCoreOptionsValidator is registered
        services.Should().ContainSingle(s => 
            s.ServiceType == typeof(Microsoft.Extensions.Options.IValidateOptions<PaginationCoreOptions>) && 
            s.ImplementationType == typeof(PaginationCoreOptionsValidator));

        // 2. Verify ValidateOnStart is registered (it adds an IHostedService for validation, or IStartupValidator)
        // Since .NET uses an internal OptionsValidationHostedService or IStartupValidator, we just assert that
        // at least one IHostedService or IStartupValidator was added by this call.
        services.Should().Contain(s => 
            s.ServiceType.Name == "IHostedService" || s.ServiceType.Name == "IStartupValidator");

        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<PaginationCoreOptions>>().Value;
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void AddPaginationCursorEncoder_RegistersOnlyEncoder()
    {
        var services = new ServiceCollection();

        services.AddPaginationCursorEncoder();

        var provider = services.BuildServiceProvider();

        // Check encoder
        var encoder = provider.GetRequiredService<ICursorEncoder>();
        encoder.Should().BeOfType<HmacCursorEncoder>();
        
        // Options should not be configured explicitly by this method (it won't throw because Options uses open generics, but it will have default ctor values)
        var options = provider.GetService<IOptions<PaginationCoreOptions>>();
        options.Should().BeNull();
        
        // No MVC options configured
        var mvcOptions = provider.GetService<IOptions<MvcOptions>>();
        mvcOptions.Should().BeNull();
    }

    [Fact]
    public void AddPagination_WithNegativeBinderIndex_AddsToEnd()
    {
        var services = new ServiceCollection();
        services.AddMvcCore();

        services.AddPagination(configure: null, configureAspNetCore: options =>
        {
            options.ModelBinderProviderInsertIndex = -1;
        });

        var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        var binderProvider = mvcOptions.ModelBinderProviders.LastOrDefault(p => p is PaginationParametersModelBinderProvider);
        binderProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddPagination_WithSecureEncoder_DoesNotRegisterWarningService()
    {
        var services = new ServiceCollection();
        services.AddMvcCore();

        services.AddPagination(options =>
        {
            options.Cursor.Encoder = new HmacCursorEncoder("supersecretkey_with_at_least_32_bytes_of_entropy");
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        hostedServices.Should().NotContain(s => s.GetType().Name == "PaginationStartupSecurityWarning");
    }

    [Fact]
    public void AddPagination_ResolvesDefaultFactories()
    {
        var services = new ServiceCollection();
        services.AddPagination();

        var provider = services.BuildServiceProvider();
        var pagedListFactory = provider.GetRequiredService<IPagedListFactory>();
        var cursorPagedListFactory = provider.GetRequiredService<ICursorPagedListFactory>();

        pagedListFactory.Should().BeSameAs(DefaultPagedListFactory.Instance);
        cursorPagedListFactory.Should().BeSameAs(DefaultPagedListFactory.Instance);

        var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        hostedServices.Should().Contain(s => s.GetType().Name == "PaginationStartupSecurityWarning");
    }

    [Fact]
    public void AddPagination_WithOutOfBoundsBinderIndex_AppendsToEnd()
    {
        var services = new ServiceCollection();
        services.AddMvcCore();

        services.AddPagination(configureAspNetCore: options =>
        {
            options.ModelBinderProviderInsertIndex = 999;
        });

        var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        var last = mvcOptions.ModelBinderProviders.Last();
        last.Should().BeOfType<PaginationParametersModelBinderProvider>();
    }

    [Fact]
    public void AddPagination_WithSpecificBinderIndex_InsertsAtCorrectPosition()
    {
        var services = new ServiceCollection();
        services.AddMvcCore();

        services.AddPagination(configureAspNetCore: options =>
        {
            options.ModelBinderProviderInsertIndex = 0;
        });

        var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvcOptions.ModelBinderProviders[0].Should().BeOfType<PaginationParametersModelBinderProvider>();
    }
}

