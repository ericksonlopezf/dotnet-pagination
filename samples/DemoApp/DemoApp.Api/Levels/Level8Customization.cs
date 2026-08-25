// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using DemoApp.Domain;
using DemoApp.Infrastructure;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DemoApp.Api.Levels;

/// <summary>
/// Level 8 — Extensibility and Security.
/// Demonstrates: Custom ICursorEncoder, HmacCursorEncoder (secure),
/// HmacCursorEncoder with TTL, FilterableAttribute, Base64CursorEncoder.Default,
/// ICursorDecoderRegistry (AOT-safe), IFilterProvider&lt;T&gt;, GenerateFilterProviderAttribute,
/// and RawCursorValue struct.
/// </summary>
public static class Level8Customization
{
    public static void MapLevel8Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level8").WithTags("Level 8 - Extensibility and Security");

        // ─── 8a. Custom ICursorEncoder (PlainText for local debugging) ───────
        group.MapGet("/products/custom-encoder", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ICursorEncoder defines the cursor encoding/decoding contract.
            // PlainTextCursorEncoder does not obfuscate — ideal for local development and debugging.
            ICursorEncoder plainTextEncoder = new PlainTextCursorEncoder();

            var page = await db.Products
                .Keyset(cursor, cursorEncoder: plainTextEncoder)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
        })
        .WithSummary("Custom ICursorEncoder: PlainTextCursorEncoder for debugging (readable cursors)");

        // ─── 8b. Base64CursorEncoder.Default — standard obfuscation ──────────
        group.MapGet("/products/base64-encoder", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // Base64CursorEncoder.Default provides Base64Url obfuscation only (no cryptographic signature).
            var page = await db.Products
                .Keyset(cursor, cursorEncoder: Base64CursorEncoder.Default)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
        })
        .WithSummary("Base64CursorEncoder.Default: standard encoder (obfuscation only, no cryptographic signature)");

        // ─── 8c. HmacCursorEncoder — HMAC-SHA256 signed cursors ──────────────
        group.MapGet("/products/hmac-encoder", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            using var hmacEncoder = new HmacCursorEncoder(
                secretKey: "DemoKey-MustBe32BytesLong-ForHMACSHA256");

            var page = await db.Products
                .Keyset(cursor, cursorEncoder: hmacEncoder)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
        })
        .WithSummary("HmacCursorEncoder: HMAC-SHA256 signed cursors — prevents cursor tampering");

        // ─── 8d. HmacCursorEncoder with TTL — time-expiring cursors ──────────
        group.MapGet("/products/ttl-encoder", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            using var ttlEncoder = new HmacCursorEncoder(
                secretKey: "DemoKey-MustBe32BytesLong-ForHMACSHA256",
                timeToLive: TimeSpan.FromMinutes(10),
                clockSkewTolerance: TimeSpan.FromSeconds(30));

            try
            {
                var page = await db.Products
                    .Keyset(cursor, cursorEncoder: ttlEncoder)
                    .Ascending(p => p.Id)
                    .ToCursorPagedListAsync(cancellationToken: ct)
                    .ConfigureAwait(false);

                return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
            }
            catch (ExpiredPaginationCursorException ex)
            {
                return Results.Problem(
                    title: "Cursor Expired",
                    detail: $"Cursor expired at {ex.ExpiredAt:O}. Please restart from the first page.",
                    statusCode: StatusCodes.Status410Gone);
            }
        })
        .WithSummary("HmacCursorEncoder + TTL: time-bound cursors — ExpiredPaginationCursorException on expiration");

        // ─── 8e. FilterableAttribute — declarative filter allowlist ──────────
        group.MapGet("/products/filterable-attr", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] FilterParameters filter,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // [Filterable] on entity properties restricts accessible fields from DSL queries.
            var pagedList = await db.Products
                .ApplyFilter(filter)
                .OrderBy(p => p.Name)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                AllowedFilterFields = new[] { "Name", "Price" },
                BlockedFilterFields = new[] { "CreatedAt", "Id" },
                DemoClass = nameof(FilterableProductDemo),
                Note = "[Filterable] on Name and Price — filtering by CreatedAt throws InvalidOperationException",
                ExampleAllowed = "?filter=name~=Product,price>=50",
                ExampleBlocked = "?filter=createdAt>=2024-01-01  ->  rejected by [Filterable]"
            });
        })
        .WithSummary("FilterableAttribute: declarative allowlist — protects sensitive properties against filter injection");

        // ─── 8f. ICursorReplayStore — cluster replay protection store ────────
        group.MapGet("/products/custom-replay-store", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            ICursorReplayStore customStore = new InMemoryCursorReplayStore();
            using var encoder = new HmacCursorEncoder(
                secretKey: "DemoKey-MustBe32BytesLong-ForHMACSHA256",
                replayStore: customStore);

            var page = await db.Products
                .Keyset(cursor, cursorEncoder: encoder)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
        })
        .WithSummary("ICursorReplayStore: implementation and registration of replay protection store for clusters");

        // ─── 8g. ICursorDecoderRegistry — AOT-safe cursor decoders ────────────
        group.MapGet("/products/aot-decoder-registry", () =>
        {
            var registry = new InMemoryCursorDecoderRegistry();

            registry.Register<int>(s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
            registry.Register<Guid>(Guid.Parse);
            registry.Register<long>(s => long.Parse(s, System.Globalization.CultureInfo.InvariantCulture));

            bool hasIntDecoder = registry.TryGetDecoder<int>(out var intDecoder);
            bool hasGuidDecoder = registry.TryGetDecoder<Guid>(out var guidDecoder);
            bool hasStringDecoder = registry.TryGetDecoder<string>(out var stringDecoder);

            bool removed = registry.Unregister<long>();

            return Results.Ok(new
            {
                RegisteredTypes = new[] { "int", "Guid" },
                HasIntDecoder = hasIntDecoder,
                HasGuidDecoder = hasGuidDecoder,
                HasStringDecoder = hasStringDecoder,
                IntDecodedSample = hasIntDecoder ? intDecoder!("42") : (int?)null,
                GuidDecodedSample = hasGuidDecoder ? guidDecoder!("00000000-0000-0000-0000-000000000001") : (Guid?)null,
                LongUnregistered = removed,
                Note = "Register ICursorDecoderRegistry in PaginationCoreOptions for AOT-safe cursor decoding"
            });
        })
        .WithSummary("ICursorDecoderRegistry + InMemoryCursorDecoderRegistry: AOT-safe typed cursor decoding");

        // ─── 8h. IFilterProvider<T> — AOT-safe filtering ─────────────────────
        group.MapGet("/products/aot-filter-provider", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] FilterParameters filter,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            IFilterProvider<Domain.Product> filterProvider = new ProductNameFilterProvider();
            var predicate = filterProvider.Build(filter);

            var query = predicate is not null
                ? db.Products.Where(predicate)
                : db.Products;

            var pagedList = await query
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                FilterApplied = predicate is not null,
                Note = "IFilterProvider<T> is the AOT-safe alternative to reflection-based ApplyFilter()."
            });
        })
        .WithSummary("IFilterProvider<T> + [GenerateFilterProvider]: AOT-safe filtering without reflection");

        // ─── 8i. RawCursorValue struct — double-encoding prevention ──────────
        group.MapGet("/products/raw-cursor-value", () =>
        {
            var raw = new RawCursorValue("42");
            var encoded = Base64CursorEncoder.Default.Encode(raw.Value);
            var decoded = Base64CursorEncoder.Default.Decode(encoded);

            return Results.Ok(new
            {
                RawValue = raw.Value,
                EncodedCursor = encoded,
                DecodedBack = decoded,
                RoundTripMatch = raw.Value == decoded,
                Note = "RawCursorValue guarantees value is an unencoded cursor, preventing double-encoding."
            });
        })
        .WithSummary("RawCursorValue: strongly-typed wrapper for unencoded cursors — prevents double-encoding");

        // ─── 8j. PaginationETagOptions.CustomETagFactory — AOT-safe ETag ─────
        group.MapGet("/products/custom-etag-factory", async (
            [AsParameters] PaginationParameters pagination,
            HttpContext httpContext,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            var pagedResponse = pagedList.ToPagedResponse();

            var aotSafeOptions = new PaginationETagOptions
            {
                CustomETagFactory = obj =>
                {
                    if (obj is PagedResponse<DemoApp.Domain.Product> resp)
                    {
                        var first = resp.Items.Count > 0 ? resp.Items[0].Id : 0;
                        var last  = resp.Items.Count > 0 ? resp.Items[^1].Id : 0;
                        return $"{resp.Page}:{resp.PageSize}:{first}-{last}";
                    }
                    return "unknown";
                }
            };

            bool isNotModified = pagedResponse.ApplyETagHeaders(httpContext, maxAge: TimeSpan.FromMinutes(5), etagOptions: aotSafeOptions);
            if (isNotModified)
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            return Results.Ok(new
            {
                Data = pagedResponse,
                ETagHeader = httpContext.Response.Headers.ETag.ToString(),
                Note = "CustomETagFactory: ETag computed without JsonSerializer — Native AOT compatible"
            });
        })
        .WithSummary("PaginationETagOptions.CustomETagFactory: deterministic AOT-safe ETag without JsonSerializer");
    }

    private sealed class PlainTextCursorEncoder : ICursorEncoder
    {
        public string? Encode(string? rawCursor) => string.IsNullOrEmpty(rawCursor) ? null : $"[PT]-{rawCursor}";
        public string? Decode(string? opaqueCursor) => opaqueCursor?.Replace("[PT]-", string.Empty);
    }
}

/// <summary>
/// Manual implementation of IFilterProvider&lt;Product&gt; for AOT demo (endpoint 8h).
/// </summary>
file sealed class ProductNameFilterProvider : IFilterProvider<Product>
{
    public Expression<Func<Product, bool>>? Build(FilterParameters filter)
    {
        if (!filter.HasValue) return null;

        var value = filter.Value!;
        const string nameContainsOp = "name~=";
        var idx = value.IndexOf(nameContainsOp, StringComparison.OrdinalIgnoreCase);

        if (idx < 0) return null;

        var startIdx = idx + nameContainsOp.Length;
        var commaIdx = value.IndexOf(',', startIdx);
        var searchTerm = commaIdx >= 0
            ? value[startIdx..commaIdx].Trim()
            : value[startIdx..].Trim();

        if (string.IsNullOrWhiteSpace(searchTerm)) return null;

        return p => p.Name.Contains(searchTerm);
    }
}

/// <summary>
/// Demonstration model for [FilterableAttribute] (endpoint 8e).
/// </summary>
file sealed class FilterableProductDemo
{
    /// <summary>Product name — DSL filtering permitted.</summary>
    [EricksonLopez.Pagination.Abstractions.FilterableAttribute]
    public string Name { get; init; } = string.Empty;

    /// <summary>Product price — DSL filtering permitted.</summary>
    [EricksonLopez.Pagination.Abstractions.FilterableAttribute]
    public decimal Price { get; init; }

    /// <summary>Creation date — not filterable.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Internal ID — not filterable.</summary>
    public int Id { get; init; }
}
