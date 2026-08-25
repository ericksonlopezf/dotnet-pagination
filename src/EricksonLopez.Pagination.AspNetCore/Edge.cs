// Copyright © Erickson Lopez. MIT License.
using System.Text.Json.Serialization;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Represents an individual item paired with its opaque cursor.
/// </summary>
/// <typeparam name="T">The type of the node.</typeparam>
public readonly record struct Edge<T> where T : notnull
{
    /// <summary>
    /// Gets the opaque cursor identifying the position of the item.
    /// </summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    /// <summary>
    /// Gets the item at this position in the result set.
    /// </summary>
    [JsonPropertyName("node")]
    public required T Node { get; init; }
}
