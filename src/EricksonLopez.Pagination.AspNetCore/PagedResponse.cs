// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Represents a standardized API response wrapper for offset-paginated lists.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public sealed class PagedResponse<T>
{
    /// <summary>
    /// Gets the items on the current page.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// Gets the total number of items across all pages, or <see langword="null"/> when not computed.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public long? TotalCount { get; init; }

    /// <summary>
    /// Gets the current 1-indexed page number.
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; init; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages, or <see langword="null"/> when not computed.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public long? TotalPages { get; init; }

    /// <summary>
    /// Gets a value indicating whether a subsequent page exists.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; init; }

    /// <summary>
    /// Gets a value indicating whether a preceding page exists.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; init; }

    /// <summary>
    /// Gets the URL pointing to the next page of results, if available.
    /// </summary>
    [JsonPropertyName("nextPageUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextPageUrl { get; init; }

    /// <summary>
    /// Gets the URL pointing to the previous page of results, if available.
    /// </summary>
    [JsonPropertyName("previousPageUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PreviousPageUrl { get; init; }
}
