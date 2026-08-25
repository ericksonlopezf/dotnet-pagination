// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Represents a standardized API response wrapper for cursor-paginated lists.
/// </summary>
/// <typeparam name="T">The type of the node in each edge.</typeparam>
public sealed class CursorPagedResponse<T> where T : notnull
{
    /// <summary>
    /// Gets the list of edges, each containing an item and its associated cursor.
    /// </summary>
    [JsonPropertyName("edges")]
    public IReadOnlyList<Edge<T>> Edges { get; init; } = [];

    /// <summary>
    /// Gets the pagination metadata for the current page.
    /// </summary>
    [JsonPropertyName("pageInfo")]
    public RelayPageInfo PageInfo { get; init; } = new RelayPageInfo();
}




