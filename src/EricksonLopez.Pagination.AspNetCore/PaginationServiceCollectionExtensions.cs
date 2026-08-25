// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register pagination services.
/// </summary>
public static class PaginationServiceCollectionExtensions
{
    /// <summary>
    /// Registers pagination services, options validation, and MVC model binder providers in the service collection.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configure">An optional action to configure pagination core options.</param>
    /// <param name="configureAspNetCore">An optional action to configure ASP.NET Core-specific pagination options.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("ValidateDataAnnotations uses reflection to scan [Range] attributes on PaginationCoreOptions. This is incompatible with Native AOT trimming. Callers targeting AOT should use ValidateOnStart without ValidateDataAnnotations.")]
    public static IServiceCollection AddPagination(
        this IServiceCollection services,
        Action<PaginationCoreOptions>? configure = null,
        Action<PaginationAspNetCoreOptions>? configureAspNetCore = null)
    {
        // 1. Options — validate both IValidatableObject cross-field rules AND [Range] DataAnnotations.
        var optionsBuilder = services.AddOptions<PaginationCoreOptions>();
        services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<PaginationCoreOptions>, PaginationCoreOptionsValidator>();
#pragma warning disable IL2026 // ValidateDataAnnotations requires reflection — suppressed; AddPagination is marked [RequiresUnreferencedCode]
        // Stryker disable once all : Options are already validated via registered PaginationCoreOptionsValidator
        optionsBuilder
            .ValidateDataAnnotations()  // enforces [Range] attributes on MaxPageSize etc.
            .ValidateOnStart();
#pragma warning restore IL2026

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        // 2. Cursor encoder (singleton — stateless).
        //
        // Design note (FIX-04): We resolve the encoder directly from the configure delegate
        // rather than through a lazy IOptions<T> factory. This prevents a race condition in
        // multi-tenant or hosted-service scenarios where two callers both invoke AddPagination
        // with different encoders — TryAddSingleton only registers once, so whichever call
        // runs first wins. By reading the encoder from the delegate eagerly (at registration
        // time rather than at first-resolve time), the behavior is deterministic: the last
        // winning call to configure the encoder (before BuildServiceProvider) is the one that
        // takes effect.
        //
        // Callers that want to override the encoder after registration should call
        // services.AddSingleton<ICursorEncoder>(myEncoder) BEFORE AddPagination,
        // which will satisfy TryAddSingleton without any factory being invoked.
        ICursorEncoder? configuredEncoder = null;
        if (configure is not null)
        {
            var probe = new PaginationCoreOptions();
            configure(probe);
            configuredEncoder = probe.Cursor.Encoder;
        }
        bool isDevelopmentKey = false;
        if (configuredEncoder == null)
        {
            // SEC-1: Secure by default. If no encoder is provided, use an HMAC encoder
            // with a deterministic development key, and emit a warning at startup.
            configuredEncoder = HmacCursorEncoder.DevelopmentDefault;
            isDevelopmentKey = true;
        }

        services.TryAddSingleton<ICursorEncoder>(configuredEncoder);

        if (isDevelopmentKey)
        {
            // Register a hosted startup action that logs the warning once ILogger is available.
            // This defers the warning to application startup rather than DI build time,
            // ensuring the logging infrastructure is ready and the message appears in structured logs.
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp =>
                new PaginationStartupSecurityWarning(
                    sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        }

        // 3. PagedList factories
        services.TryAddSingleton<IPagedListFactory>(DefaultPagedListFactory.Instance);
        services.TryAddSingleton<ICursorPagedListFactory>(DefaultPagedListFactory.Instance);

        // 4. MVC model binder (MVC / Razor Pages / API Controllers).
        //
        // Design note (FIX-17): The binder is now registered by default (at index 0).
        // When ModelBinderProviderInsertIndex is explicitly set via configureAspNetCore,
        // that value is used instead. When configureAspNetCore is null or InsertIndex is
        // not overridden, the default of 0 (head of the pipeline) applies.
        var aspNetCoreOptions = new PaginationAspNetCoreOptions();
        configureAspNetCore?.Invoke(aspNetCoreOptions);
        int binderIndex = aspNetCoreOptions.ModelBinderProviderInsertIndex ?? 0;
        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
        {
            // Stryker disable once all : In IList<T>, Insert(Count, item) is functionally identical to Add(item)
            if (binderIndex < 0 || binderIndex > options.ModelBinderProviders.Count)
            {
                options.ModelBinderProviders.Add(new PaginationParametersModelBinderProvider());
            }
            else
            {
                options.ModelBinderProviders.Insert(binderIndex, new PaginationParametersModelBinderProvider());
            }
        });

        return services;
    }

    /// <summary>
    /// Registers the default <see cref="ICursorEncoder"/> into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register the encoder into.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddPaginationCursorEncoder(this IServiceCollection services)
    {
        services.TryAddSingleton<ICursorEncoder>(HmacCursorEncoder.DevelopmentDefault);
        return services;
    }

}

