// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Pagination.Relay;

/// <summary>
/// Represents pagination information according to the GraphQL Relay Cursor Connections specification.
/// </summary>
public sealed record PageInfo
{
    /// <summary>
    /// Gets a value indicating whether more edges exist after the current page.
    /// </summary>
    public bool HasNextPage { get; init; }

    /// <summary>
    /// Gets a value indicating whether edges exist prior to the current page.
    /// </summary>
    public bool HasPreviousPage { get; init; }

    /// <summary>
    /// Gets the opaque cursor pointing to the first edge in the list, or <see langword="null"/> if empty.
    /// </summary>
    public string? StartCursor { get; init; }

    /// <summary>
    /// Gets the opaque cursor pointing to the last edge in the list, or <see langword="null"/> if empty.
    /// </summary>
    public string? EndCursor { get; init; }
}
