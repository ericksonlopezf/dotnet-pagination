// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EricksonLopez.Pagination.Redis;

/// <summary>
/// Provides extension methods for setting up Redis cursor replay store in an <see cref="IServiceCollection"/>.
/// </summary>
public static class RedisPaginationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="RedisCursorReplayStore"/> as the <see cref="ICursorReplayStore"/> implementation using an existing <see cref="IConnectionMultiplexer"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">The Redis connection multiplexer instance.</param>
    /// <param name="configureOptions">Optional action to configure <see cref="RedisCursorReplayStoreOptions"/>.</param>
    /// <returns>The modified service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="connection"/> is <see langword="null"/></exception>
    public static IServiceCollection AddPaginationRedisReplayStore(
        this IServiceCollection services,
        IConnectionMultiplexer connection,
        Action<RedisCursorReplayStoreOptions>? configureOptions = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.TryAddSingleton<ICursorReplayStore>(sp =>
            new RedisCursorReplayStore(connection, sp.GetService<IOptions<RedisCursorReplayStoreOptions>>()));

        return services;
    }

    /// <summary>
    /// Registers <see cref="RedisCursorReplayStore"/> as the <see cref="ICursorReplayStore"/> implementation resolving <see cref="IConnectionMultiplexer"/> from DI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure <see cref="RedisCursorReplayStoreOptions"/>.</param>
    /// <returns>The modified service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddPaginationRedisReplayStore(
        this IServiceCollection services,
        Action<RedisCursorReplayStoreOptions>? configureOptions = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.TryAddSingleton<ICursorReplayStore>(sp =>
        {
            var connection = sp.GetRequiredService<IConnectionMultiplexer>();
            var options = sp.GetService<IOptions<RedisCursorReplayStoreOptions>>();
            return new RedisCursorReplayStore(connection, options);
        });

        return services;
    }
}
