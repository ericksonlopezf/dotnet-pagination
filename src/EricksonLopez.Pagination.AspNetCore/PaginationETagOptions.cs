// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Represents configuration options for ETag generation in pagination result extensions.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class PaginationETagOptions
{
    /// <summary>
    /// Gets or sets a custom ETag factory for offset-paginated responses.
    /// </summary>
    public Func<object, string>? CustomETagFactory { get; init; }

    /// <summary>
    /// Gets or sets a custom ETag factory for cursor-paginated responses.
    /// </summary>
    public Func<object, string>? CustomCursorETagFactory { get; init; }

    /// <summary>
    /// Represents default ETag generation options using SHA-256 content hashing.
    /// </summary>
    public static readonly PaginationETagOptions Default = new();
}
