// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Relay;

/// <summary>
/// Represents a Relay Connection container for a paginated list of edges.
/// </summary>
/// <typeparam name="TNode">The type of the node entity.</typeparam>
public sealed record Connection<TNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Connection{TNode}"/> record.
    /// </summary>
    /// <param name="edges">The list of edges contained in this connection slice.</param>
    /// <param name="pageInfo">Information to aid in pagination navigation.</param>
    /// <param name="totalCount">Optional total count of items across all pages.</param>
    /// <exception cref="ArgumentNullException"><paramref name="edges"/> or <paramref name="pageInfo"/> is <see langword="null"/></exception>
    public Connection(IReadOnlyList<Edge<TNode>> edges, PageInfo pageInfo, long? totalCount = null)
    {
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        PageInfo = pageInfo ?? throw new ArgumentNullException(nameof(pageInfo));
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the list of edges in this connection slice.
    /// </summary>
    public IReadOnlyList<Edge<TNode>> Edges { get; init; }

    /// <summary>
    /// Gets information about pagination in a connection.
    /// </summary>
    public PageInfo PageInfo { get; init; }

    /// <summary>
    /// Gets the total number of items across all pages if requested and available.
    /// </summary>
    public long? TotalCount { get; init; }
}
