// Copyright © Erickson Lopez. MIT License.
using System.Text.Json.Serialization;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Represents pagination metadata about the current cursor-paginated page.
/// </summary>
public sealed class RelayPageInfo
{
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
    /// Gets the opaque cursor of the first item on the page.
    /// </summary>
    [JsonPropertyName("startCursor")]
    public string? StartCursor { get; init; }

    /// <summary>
    /// Gets the opaque cursor of the last item on the page.
    /// </summary>
    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; init; }
}
