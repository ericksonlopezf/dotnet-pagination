// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Pagination.Blazor;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register Blazor pagination services.
/// </summary>
public static class PaginationBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Registers Blazor pagination UI options in the service collection.
    /// </summary>
    /// <param name="services">The service collection to register options into.</param>
    /// <param name="configure">An optional action to configure <see cref="PaginationUIOptions"/>.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddPaginationBlazor(
        this IServiceCollection services,
        Action<PaginationUIOptions>? configure = null)
    {
        // Register PaginationUIOptions so IOptions<PaginationUIOptions> is always resolvable.
        // services.AddOptions() is idempotent — safe to call multiple times.
        var optionsBuilder = services.AddOptions<PaginationUIOptions>();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return services;
    }
}

