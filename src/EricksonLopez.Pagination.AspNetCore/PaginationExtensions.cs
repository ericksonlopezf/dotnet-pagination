// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.WebUtilities;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides extension methods for converting paginated lists into standardized ASP.NET Core API responses.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Converts an <see cref="IPagedList{T}"/> into a standardized <see cref="PagedResponse{T}"/> using the current request to generate relative HATEOAS links.
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated list.</typeparam>
    /// <param name="pagedList">The paginated list to convert.</param>
    /// <param name="request">The current HTTP request.</param>
    /// <returns>A standardized <see cref="PagedResponse{T}"/> containing pagination metadata and relative navigation links.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> or <paramref name="request"/> is <see langword="null"/></exception>
    public static PagedResponse<T> ToPagedResponse<T>(this IPagedList<T> pagedList, Microsoft.AspNetCore.Http.HttpRequest request)
    {
        // Stryker disable once all : Duplicate guard clause, pagedList.ToPagedResponse(requestUri) also checks for null
        if (pagedList is null) throw new ArgumentNullException(nameof(pagedList));
        if (request is null) throw new ArgumentNullException(nameof(request));
        
        var requestUri = request.PathBase.Value + request.Path.Value + request.QueryString.Value;
        return pagedList.ToPagedResponse(requestUri);
    }

    /// <summary>
    /// Converts an <see cref="IPagedList{T}"/> into a standardized <see cref="PagedResponse{T}"/> suitable for API responses.
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated list.</typeparam>
    /// <param name="pagedList">The paginated list to convert.</param>
    /// <param name="requestUri">The original request URI used to generate navigation links.</param>
    /// <returns>A standardized <see cref="PagedResponse{T}"/> containing pagination metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> is <see langword="null"/></exception>
    public static PagedResponse<T> ToPagedResponse<T>(this IPagedList<T> pagedList, string? requestUri = null)
    {
        if (pagedList is null) throw new ArgumentNullException(nameof(pagedList));

        string? nextPageUrl = null;
        string? prevPageUrl = null;

        if (!string.IsNullOrWhiteSpace(requestUri))
        {
            var questionMarkIndex = requestUri.IndexOf('?');
            var queryString = questionMarkIndex >= 0 ? requestUri.Substring(questionMarkIndex) : "";
            var basePath = questionMarkIndex >= 0 ? requestUri.Substring(0, questionMarkIndex) : requestUri;

            var queryParams = new List<KeyValuePair<string, string?>>();
            
            // Stryker disable once all : QueryHelpers.ParseQuery("?") returns empty dictionary, so > 1 vs >= 1 is an internal fast path optimization
            if (queryString.Length > 1)
            {
                var parsedQuery = QueryHelpers.ParseQuery(queryString);
                foreach (var kvp in parsedQuery)
                {
                    if (!kvp.Key.Equals("page", StringComparison.OrdinalIgnoreCase) && 
                        !kvp.Key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var val in kvp.Value)
                        {
                            queryParams.Add(new KeyValuePair<string, string?>(kvp.Key, val));
                        }
                    }
                }
            }

            // Always add pageSize at the end (or where it was? we just append it for simplicity, or we could insert it where it was. Replacing at the end is fine, the problem was alphabetizing).
            // Actually, let's just append them. The audit complained about dict.OrderBy changing the original order of the other params.
            queryParams.Add(new KeyValuePair<string, string?>("pageSize", pagedList.PageSize.ToString()));

            if (pagedList.HasNextPage)
            {
                var nextParams = new List<KeyValuePair<string, string?>>(queryParams);
                nextParams.Add(new KeyValuePair<string, string?>("page", (pagedList.Page + 1).ToString()));
                nextPageUrl = QueryHelpers.AddQueryString(basePath, nextParams);
            }

            if (pagedList.HasPreviousPage)
            {
                var prevParams = new List<KeyValuePair<string, string?>>(queryParams);
                prevParams.Add(new KeyValuePair<string, string?>("page", (pagedList.Page - 1).ToString()));
                prevPageUrl = QueryHelpers.AddQueryString(basePath, prevParams);
            }
        }

        return new PagedResponse<T>
        {
            Items = pagedList,
            TotalCount = pagedList.TotalCount,
            Page = pagedList.Page,
            PageSize = pagedList.PageSize,
            TotalPages = pagedList.TotalPages,
            HasNextPage = pagedList.HasNextPage,
            HasPreviousPage = pagedList.HasPreviousPage,
            NextPageUrl = nextPageUrl,
            PreviousPageUrl = prevPageUrl
        };
    }


    /// <summary>
    /// Converts an <see cref="ICursorPagedList{T}"/> into a standardized <see cref="CursorPagedResponse{T}"/> using a raw cursor value extractor.
    /// </summary>
    /// <typeparam name="T">The type of items in the cursor-paginated list.</typeparam>
    /// <param name="pagedList">The cursor-paginated list to convert.</param>
    /// <param name="rawCursorSelector">A function that extracts the raw cursor value from each item.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <returns>A <see cref="CursorPagedResponse{T}"/> containing edges and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> or <paramref name="rawCursorSelector"/> is <see langword="null"/></exception>
    public static CursorPagedResponse<T> ToCursorPagedResponse<T>(
        this ICursorPagedList<T> pagedList,
        Func<T, RawCursorValue> rawCursorSelector,
        ICursorEncoder? cursorEncoder = null)
        where T : notnull
    {
        if (pagedList is null) throw new ArgumentNullException(nameof(pagedList));
        if (rawCursorSelector is null) throw new ArgumentNullException(nameof(rawCursorSelector));

        var edges = new List<Edge<T>>(pagedList.Count);
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        foreach (var item in pagedList)
        {
            edges.Add(new Edge<T>
            {
                Cursor = encoder.Encode("S|" + rawCursorSelector(item).Value),
                Node = item
            });
        }

        return new CursorPagedResponse<T>
        {
            Edges = edges,
            PageInfo = new RelayPageInfo
            {
                HasNextPage = pagedList.HasNextPage,
                HasPreviousPage = pagedList.HasPreviousPage,
                StartCursor = pagedList.StartCursor,
                EndCursor = pagedList.EndCursor
            }
        };
    }

    /// <summary>
    /// Converts an <see cref="ICursorPagedList{T}"/> into a standardized <see cref="CursorPagedResponse{T}"/> using a typed key selector.
    /// </summary>
    /// <typeparam name="T">The type of items in the cursor-paginated list.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="pagedList">The cursor-paginated list to convert.</param>
    /// <param name="keySelector">A function that extracts the unique cursor key from each item.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <returns>A <see cref="CursorPagedResponse{T}"/> containing edges and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    public static CursorPagedResponse<T> ToCursorPagedResponse<T, TKey>(
        this ICursorPagedList<T> pagedList,
        Func<T, TKey> keySelector,
        ICursorEncoder? cursorEncoder = null)
        where T : notnull
        where TKey : notnull
    {
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

        // FIX-07: Use IFormattable.ToString(format, InvariantCulture) when available.
        // Plain object.ToString() uses the thread's current culture, which causes round-trip
        // failures on servers with non-en-US cultures (e.g., decimal '1.5' → '1,5' in de-DE).
        // All common key types (int, long, Guid, decimal, DateTimeOffset, etc.) implement
        // IFormattable so this path covers virtually all real-world cursor key types.
        return pagedList.ToCursorPagedResponse(
            item =>
            {
                var key = keySelector(item);
                var raw = key is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : key.ToString()!;
                return new RawCursorValue(raw);
            },
            cursorEncoder);
    }
}


