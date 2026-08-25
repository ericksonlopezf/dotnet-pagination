// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Relay;

/// <summary>
/// Provides extension methods for converting pagination result types to GraphQL Relay Connection structures.
/// </summary>
public static class RelayPaginationExtensions
{
    /// <summary>
    /// Converts an <see cref="ICursorPagedList{T}"/> into a GraphQL Relay <see cref="Connection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The node entity type.</typeparam>
    /// <param name="list">The cursor paged list.</param>
    /// <param name="cursorSelector">A function extracting the unique opaque cursor for each node.</param>
    /// <returns>A GraphQL Relay <see cref="Connection{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> or <paramref name="cursorSelector"/> is <see langword="null"/></exception>
    public static Connection<T> ToRelayConnection<T>(
        this ICursorPagedList<T> list,
        Func<T, string> cursorSelector)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (cursorSelector is null)
        {
            throw new ArgumentNullException(nameof(cursorSelector));
        }

        var edges = new List<Edge<T>>(list.Count);
        foreach (var item in list)
        {
            edges.Add(new Edge<T>(item, cursorSelector(item)));
        }

        var pageInfo = new PageInfo
        {
            HasNextPage = list.HasNextPage,
            HasPreviousPage = list.HasPreviousPage,
            StartCursor = edges.Count > 0 ? edges[0].Cursor : list.StartCursor,
            EndCursor = edges.Count > 0 ? edges[^1].Cursor : list.EndCursor
        };

        return new Connection<T>(edges, pageInfo);
    }

    /// <summary>
    /// Converts an <see cref="ICursorPagedList{TSource}"/> into a mapped GraphQL Relay <see cref="Connection{TNode}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TNode">The mapped GraphQL node type.</typeparam>
    /// <param name="list">The cursor paged list.</param>
    /// <param name="nodeSelector">A mapping function from source to node.</param>
    /// <param name="cursorSelector">A function extracting the unique opaque cursor for each source item.</param>
    /// <returns>A GraphQL Relay <see cref="Connection{TNode}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/>, <paramref name="nodeSelector"/>, or <paramref name="cursorSelector"/> is <see langword="null"/></exception>
    public static Connection<TNode> ToRelayConnection<TSource, TNode>(
        this ICursorPagedList<TSource> list,
        Func<TSource, TNode> nodeSelector,
        Func<TSource, string> cursorSelector)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (nodeSelector is null)
        {
            throw new ArgumentNullException(nameof(nodeSelector));
        }

        if (cursorSelector is null)
        {
            throw new ArgumentNullException(nameof(cursorSelector));
        }

        var edges = new List<Edge<TNode>>(list.Count);
        foreach (var item in list)
        {
            edges.Add(new Edge<TNode>(nodeSelector(item), cursorSelector(item)));
        }

        var pageInfo = new PageInfo
        {
            HasNextPage = list.HasNextPage,
            HasPreviousPage = list.HasPreviousPage,
            StartCursor = edges.Count > 0 ? edges[0].Cursor : list.StartCursor,
            EndCursor = edges.Count > 0 ? edges[^1].Cursor : list.EndCursor
        };

        return new Connection<TNode>(edges, pageInfo);
    }

    /// <summary>
    /// Converts an <see cref="IPagedList{T}"/> (offset pagination) into a GraphQL Relay <see cref="Connection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The node entity type.</typeparam>
    /// <param name="list">The offset paged list.</param>
    /// <param name="cursorSelector">A function generating an opaque cursor per node.</param>
    /// <returns>A GraphQL Relay <see cref="Connection{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> or <paramref name="cursorSelector"/> is <see langword="null"/></exception>
    public static Connection<T> ToRelayConnection<T>(
        this IPagedList<T> list,
        Func<T, string> cursorSelector)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (cursorSelector is null)
        {
            throw new ArgumentNullException(nameof(cursorSelector));
        }

        var edges = new List<Edge<T>>(list.Count);
        foreach (var item in list)
        {
            edges.Add(new Edge<T>(item, cursorSelector(item)));
        }

        var pageInfo = new PageInfo
        {
            HasNextPage = list.HasNextPage,
            HasPreviousPage = list.HasPreviousPage,
            StartCursor = edges.Count > 0 ? edges[0].Cursor : null,
            EndCursor = edges.Count > 0 ? edges[^1].Cursor : null
        };

        return new Connection<T>(edges, pageInfo, list.TotalCount);
    }
}
