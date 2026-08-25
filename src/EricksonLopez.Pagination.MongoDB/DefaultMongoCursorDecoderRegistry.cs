// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Bson;

namespace EricksonLopez.Pagination.MongoDB;

/// <summary>
/// Provides a default cursor decoder registry for MongoDB with support for <see cref="ObjectId"/> and common scalar types.
/// </summary>
public static class DefaultMongoCursorDecoderRegistry
{
    // Stryker disable all : Lazy singleton static registrations
    private static readonly Lazy<ICursorDecoderRegistry> _instance = new(() =>
    {
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register<ObjectId>(s => ObjectId.Parse(s));
        registry.Register<Guid>(s => Guid.Parse(s));
        registry.Register<DateTimeOffset>(s => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        registry.Register<int>(s => int.Parse(s, CultureInfo.InvariantCulture));
        registry.Register<long>(s => long.Parse(s, CultureInfo.InvariantCulture));
        registry.Register<string>(s => s);
        return registry;
    });
    // Stryker restore all

    /// <summary>
    /// Gets the shared singleton instance of the default MongoDB cursor decoder registry.
    /// </summary>
    public static ICursorDecoderRegistry Instance => _instance.Value;

    /// <summary>
    /// Returns the provided registry if not <see langword="null"/>; otherwise returns the shared <see cref="Instance"/>.
    /// </summary>
    /// <param name="userRegistry">An optional user-supplied decoder registry.</param>
    /// <returns>The effective decoder registry to use for MongoDB cursor operations.</returns>
    public static ICursorDecoderRegistry GetEffectiveRegistry(ICursorDecoderRegistry? userRegistry)
    {
        return userRegistry ?? Instance;
    }
}
