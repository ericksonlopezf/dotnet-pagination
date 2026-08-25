// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides extension methods to return paginated lists as <see cref="IResult"/> with HTTP
/// <c>ETag</c> and <c>Cache-Control</c> headers, and to apply those headers directly to an
/// existing <see cref="HttpResponse"/> for manual response composition.
/// </summary>
public static class PaginationResultExtensions
{
    // -------------------------------------------------------------------------
    // IResult helpers (Minimal API / Results-based endpoints)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts an <see cref="IPagedList{T}"/> into an <see cref="IResult"/> containing a standardized <see cref="PagedResponse{T}"/> with ETag and cache headers.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="pagedList">The paginated result to convert.</param>
    /// <param name="request">The current HTTP request used to build navigation links.</param>
    /// <param name="maxAge">An optional duration for the Cache-Control max-age directive.</param>
    /// <returns>An <see cref="IResult"/> representing the paginated response with cache headers.</returns>
    public static IResult ToPagedResult<T>(this IPagedList<T> pagedList, HttpRequest request, TimeSpan? maxAge = null)
    {
        var response = pagedList.ToPagedResponse(request);
        return new PagedResult<T>(response, maxAge);
    }

    /// <summary>
    /// Converts an <see cref="ICursorPagedList{T}"/> into an <see cref="IResult"/> containing a standardized <see cref="CursorPagedResponse{T}"/> with ETag and cache headers.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The cursor key type.</typeparam>
    /// <param name="pagedList">The cursor-paginated result to convert.</param>
    /// <param name="keySelector">A function that extracts the unique cursor key from each item.</param>
    /// <param name="cursorEncoder">An optional cursor encoder override.</param>
    /// <param name="maxAge">An optional duration for the Cache-Control max-age directive.</param>
    /// <returns>An <see cref="IResult"/> representing the cursor-paginated response with cache headers.</returns>
    public static IResult ToCursorPagedResult<T, TKey>(this ICursorPagedList<T> pagedList, Func<T, TKey> keySelector, ICursorEncoder? cursorEncoder = null, TimeSpan? maxAge = null)
        where T : notnull
        where TKey : notnull
    {
        var response = pagedList.ToCursorPagedResponse(keySelector, cursorEncoder);
        return new CursorPagedResult<T>(response, maxAge);
    }

    // -------------------------------------------------------------------------
    // F-010: HttpResponse helpers (controller / manual response composition)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies ETag and cache control headers to an HTTP response based on the content of a <see cref="PagedResponse{T}"/> and determines if the cached representation is valid.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="response">The paginated response whose content is hashed.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="maxAge">An optional duration for the Cache-Control max-age directive.</param>
    /// <param name="etagOptions">Optional configuration controlling ETag generation strategy.</param>
    /// <returns><see langword="true"/> if the cached version matches the computed ETag; otherwise, <see langword="false"/>.</returns>
    [RequiresUnreferencedCode("ETag computation serializes items to JSON for content hashing. Use a custom PaginationETagOptions.CustomETagFactory for AOT/trimmed apps.")]
    [RequiresDynamicCode("ETag computation uses generic JSON serialization. Use a custom PaginationETagOptions.CustomETagFactory for AOT/trimmed apps.")]
    public static bool ApplyETagHeaders<T>(
        this PagedResponse<T> response,
        HttpContext httpContext,
        TimeSpan? maxAge = null,
        PaginationETagOptions? etagOptions = null)
    {
        var etag = ComputePagedResponseETag(response, etagOptions);

        ApplyCommonCacheHeaders(httpContext.Response, etag, maxAge);

        return IsNotModified(httpContext.Request, etag);
    }

    /// <summary>
    /// Applies ETag and cache control headers to an HTTP response based on the content of a <see cref="CursorPagedResponse{T}"/> and determines if the cached representation is valid.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="response">The cursor-paginated response whose content is hashed.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="maxAge">An optional duration for the Cache-Control max-age directive.</param>
    /// <param name="etagOptions">Optional configuration controlling ETag generation strategy.</param>
    /// <returns><see langword="true"/> if the cached version matches the computed ETag; otherwise, <see langword="false"/>.</returns>
    [RequiresUnreferencedCode("ETag computation serializes items to JSON for content hashing. Use a custom PaginationETagOptions.CustomCursorETagFactory for AOT/trimmed apps.")]
    [RequiresDynamicCode("ETag computation uses generic JSON serialization. Use a custom PaginationETagOptions.CustomCursorETagFactory for AOT/trimmed apps.")]
    public static bool ApplyETagHeaders<T>(
        this CursorPagedResponse<T> response,
        HttpContext httpContext,
        TimeSpan? maxAge = null,
        PaginationETagOptions? etagOptions = null)
        where T : notnull
    {
        var etag = ComputeCursorPagedResponseETag(response, etagOptions);

        ApplyCommonCacheHeaders(httpContext.Response, etag, maxAge);

        return IsNotModified(httpContext.Request, etag);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private static void ApplyCommonCacheHeaders(HttpResponse response, string etag, TimeSpan? maxAge)
    {
        response.Headers.ETag = etag;
        response.Headers.Vary = "Accept-Encoding";

        if (maxAge.HasValue)
        {
            var seconds = (int)Math.Max(0, maxAge.Value.TotalSeconds);
            response.Headers.CacheControl = $"private, max-age={seconds}";
        }
    }

    private static bool IsNotModified(HttpRequest request, string etag)
    {
        var ifNoneMatch = request.Headers.IfNoneMatch.ToString();
        return !string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == etag;
    }

    /// <summary>
    /// Computes a deterministic ETag for an offset-paginated response.
    /// </summary>
    [RequiresUnreferencedCode("Uses JsonSerializer.Serialize<T> for content hashing.")]
    [RequiresDynamicCode("Uses JsonSerializer.Serialize<T> which requires runtime code generation.")]
    private static string ComputePagedResponseETag<T>(PagedResponse<T> response, PaginationETagOptions? options)
    {
        if (options?.CustomETagFactory != null)
        {
            return NormalizeETag(options.CustomETagFactory(response));
        }

        // Deterministic fingerprint: metadata + item count + serialized items.
        // Avoids GetHashCode() which is process-local and non-deterministic for custom types.
        var sb = new StringBuilder(64);
        sb.Append(response.Page).Append('|');
        sb.Append(response.PageSize).Append('|');
        sb.Append(response.TotalCount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null").Append('|');
        sb.Append(response.HasNextPage ? '1' : '0').Append('|');
        sb.Append(response.Items?.Count ?? 0);
        if (response.Items is { Count: > 0 } items)
        {
            sb.Append('|');
            // Serialize the items to JSON for a content-addressable fingerprint.
            // This is deterministic across processes for the same data, unlike GetHashCode().
            sb.Append(JsonSerializer.Serialize(items));
        }

        return ComputeSha256ETag(sb.ToString());
    }

    /// <summary>
    /// Computes a deterministic ETag for a cursor-paginated response.
    /// </summary>
    [RequiresUnreferencedCode("Uses JsonSerializer.Serialize<T> for content hashing.")]
    [RequiresDynamicCode("Uses JsonSerializer.Serialize<T> which requires runtime code generation.")]
    private static string ComputeCursorPagedResponseETag<T>(CursorPagedResponse<T> response, PaginationETagOptions? options)
        where T : notnull
    {
        if (options?.CustomCursorETagFactory != null)
        {
            return NormalizeETag(options.CustomCursorETagFactory(response));
        }

        var sb = new StringBuilder(64);
        sb.Append(response.PageInfo.StartCursor ?? "null").Append('|');
        sb.Append(response.PageInfo.EndCursor ?? "null").Append('|');
        sb.Append(response.PageInfo.HasNextPage ? '1' : '0').Append('|');
        sb.Append(response.PageInfo.HasPreviousPage ? '1' : '0').Append('|');
        sb.Append(response.Edges?.Count ?? 0);
        if (response.Edges is { Count: > 0 } edges)
        {
            sb.Append('|');
            sb.Append(JsonSerializer.Serialize(edges));
        }

        return ComputeSha256ETag(sb.ToString());
    }

    private static string ComputeSha256ETag(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Use first 8 bytes (64 bits) as a hex string — 16 hex chars.
        // Sufficient collision resistance for pagination ETags; full 32-byte hash is overkill.
        return $"\"{Convert.ToHexString(bytes, 0, 8).ToLowerInvariant()}\"";
    }

    private static string NormalizeETag(string raw)
    {
        // Ensure the ETag is quoted per RFC 7232 §2.3
        if (raw.StartsWith('"') && raw.EndsWith('"'))
            return raw;
        return $"\"{raw}\"";
    }
}





