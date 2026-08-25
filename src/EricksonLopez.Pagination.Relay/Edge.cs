// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Relay;

/// <summary>
/// Represents an edge in a GraphQL Relay connection consisting of a node and its cursor.
/// </summary>
/// <typeparam name="TNode">The type of the encapsulated node entity.</typeparam>
public sealed record Edge<TNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Edge{TNode}"/> record.
    /// </summary>
    /// <param name="node">The item entity at the end of the edge.</param>
    /// <param name="cursor">A cursor token for pagination seek navigation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cursor"/> is <see langword="null"/></exception>
    public Edge(TNode node, string cursor)
    {
        Node = node;
        Cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
    }

    /// <summary>
    /// Gets the item entity at the end of the edge.
    /// </summary>
    public TNode Node { get; init; }

    /// <summary>
    /// Gets a cursor for use in pagination.
    /// </summary>
    public string Cursor { get; init; }
}
