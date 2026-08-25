// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides the default implementation of <see cref="IPagedListFactory"/> and <see cref="ICursorPagedListFactory"/>.
/// </summary>
public sealed class DefaultPagedListFactory : IPagedListFactory, ICursorPagedListFactory
{
    /// <summary>
    /// Represents the singleton instance of <see cref="DefaultPagedListFactory"/>.
    /// </summary>
    public static readonly DefaultPagedListFactory Instance = new();

    /// <inheritdoc />
    public IPagedList<T> CreatePagedList<T>(IReadOnlyList<T> items, long? totalCount, int page, int pageSize, bool? hasNextPage)
    {
        if (totalCount.HasValue)
        {
            return new CountedPagedList<T>(items, totalCount.Value, page, pageSize);
        }
        return new PagedList<T>(items, null, page, pageSize, hasNextPage);
    }

    /// <inheritdoc />
    public ICursorPagedList<T> CreateCursorPagedList<T>(IReadOnlyList<T> items, long? totalCount, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)
    {
        if (totalCount.HasValue)
        {
            return new CountedCursorPagedList<T>(items, totalCount.Value, startCursor, endCursor, hasPreviousPage, hasNextPage);
        }
        return CursorPagedList<T>.Create(items, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }
}

