// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Elasticsearch;

/// <summary>
/// Provides extension methods for integrating Elasticsearch search requests and responses with the EricksonLopez.Pagination cursor ecosystem.
/// </summary>
public static class ElasticsearchCursorPaginationExtensions
{
    /// <summary>
    /// Configures an Elasticsearch <see cref="SearchRequestDescriptor{T}"/> with pagination size and decoded <c>search_after</c> cursor token.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="descriptor">The search request descriptor.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="encoder">Optional cursor encoder for decoding <see cref="CursorPaginationParameters.After"/>.</param>
    /// <returns>The modified search request descriptor.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is <see langword="null"/></exception>
    public static SearchRequestDescriptor<T> ApplyCursorPagination<T>(
        this SearchRequestDescriptor<T> descriptor,
        CursorPaginationParameters parameters,
        ICursorEncoder? encoder = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        int pageSize = parameters.GetPageSize();

        // Fetch 1 extra document to evaluate HasNextPage without issuing a secondary count query
        descriptor.Size(pageSize + 1);

        if (!string.IsNullOrWhiteSpace(parameters.After))
        {
            var sortValues = ElasticsearchCursorHelper.DecodeSort(parameters.After, encoder);
            if (sortValues is not null)
            {
                descriptor.SearchAfter(sortValues);
            }
        }

        return descriptor;
    }

    /// <summary>
    /// Materializes an Elasticsearch <see cref="SearchResponse{T}"/> into an immutable <see cref="CursorPagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="response">The search response returned by Elasticsearch.</param>
    /// <param name="parameters">The pagination parameters used to execute the search.</param>
    /// <param name="encoder">Optional cursor encoder for encoding <see cref="ICursorPagedList.StartCursor"/> and <see cref="ICursorPagedList.EndCursor"/>.</param>
    /// <returns>An immutable <see cref="CursorPagedList{T}"/> containing the documents and pagination cursors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/></exception>
    public static CursorPagedList<T> ToCursorPagedList<T>(
        this SearchResponse<T> response,
        CursorPaginationParameters parameters,
        ICursorEncoder? encoder = null)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        int pageSize = parameters.GetPageSize();
        var hits = response.Hits?.ToList() ?? new List<Hit<T>>();
        bool hasNextPage = hits.Count > pageSize;

        // Stryker disable once all : Equivalent optimization / fallback
        var pageHits = hasNextPage ? hits.Take(pageSize).ToList() : hits;
        var items = pageHits
            .Where(h => h.Source is not null)
            .Select(h => h.Source!)
            .ToList();

        string? startCursor = pageHits.Count > 0
            ? ElasticsearchCursorHelper.EncodeSort(pageHits[0].Sort, encoder)
            : null;

        string? endCursor = pageHits.Count > 0
            ? ElasticsearchCursorHelper.EncodeSort(pageHits[^1].Sort, encoder)
            : null;

        bool hasPreviousPage = !string.IsNullOrWhiteSpace(parameters.After);

        return new CursorPagedList<T>(
            items: items,
            startCursor: startCursor,
            endCursor: endCursor,
            hasPreviousPage: hasPreviousPage,
            hasNextPage: hasNextPage);
    }
}
