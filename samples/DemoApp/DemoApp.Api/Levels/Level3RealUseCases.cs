// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
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
/// Level 3 — Real Use Cases.
/// Demonstrates: Keyset pagination, PagedList factories (WithCount/WithoutCount/Empty),
/// Map() and LazyMap() over IPagedList and ICursorPagedList, type-safe ToCursorPagedResponse,
/// and PagedList&lt;T&gt;.Empty() for optimal zero-allocation empty returns.
/// </summary>
public static class Level3RealUseCases
{
    public static void MapLevel3Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level3").WithTags("Level 3 - Real Use Cases");

        // ─── 3a. Keyset Pagination O(log n) — recommended for large datasets ─
        group.MapGet("/products/keyset", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // KeysetBuilder: fluent API for multi-column keyset pagination.
            // ToCursorPagedListAsync executes query with appropriate keyset WHERE clause.
            // Supports forward (first/after) and backward (last/before) navigation.
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            // ToCursorPagedResponse<T,TKey> — type-safe overload with key projection.
            return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
        })
        .WithSummary("Keyset Pagination O(log n): KeysetBuilder with type-safe ToCursorPagedResponse");

        // ─── 3b. Counted offset pagination with TotalCount ───────────────────
        group.MapGet("/products/counted", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ToPagedListAsync with countTotal: true (default) returns an IPagedList<T>
            // that implements ICountedPagedList<T> for accessing ExactTotalCount.
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, countTotal: true, cancellationToken: ct);

            long? exactCount = null;
            if (pagedList is ICountedPagedList countedList)
            {
                exactCount = countedList.ExactTotalCount;
            }

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                ExactTotalCount = exactCount,
                TotalPagesNullable = pagedList.TotalPages
            });
        })
        .WithSummary("ICountedPagedList.ExactTotalCount: type-safe access to exact total record count");

        // ─── 3b2. ICountedPagedList<T>.Map() — projection preserving ExactTotalCount ─
        group.MapGet("/products/counted-map", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, countTotal: true, cancellationToken: ct);

            if (pagedList is ICountedPagedList<Product> countedList)
            {
                // This overload preserves ExactTotalCount on the projected list.
                ICountedPagedList<ProductDto> countedDtos = countedList.Map(
                    p => new ProductDto(p.Id, p.Name, p.Price));

                return Results.Ok(new
                {
                    Data = countedDtos.ToPagedResponse(),
                    ExactTotalCount = countedDtos.ExactTotalCount,
                    Note = "Map() on ICountedPagedList<T> preserves ExactTotalCount in the mapped result"
                });
            }

            return Results.Problem("The result is not ICountedPagedList — unexpected when countTotal=true.");
        })
        .WithSummary("ICountedPagedList<T>.Map(): overload that preserves ExactTotalCount during projection");

        // ─── 3c. Count-less offset pagination — optimized for large tables ───
        group.MapGet("/products/countless", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // countTotal: false -> uses Take(PageSize+1) lookahead probing
            // without executing COUNT(*). TotalCount and TotalPages will be null.
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, countTotal: false, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("Count-less pagination: countTotal=false for maximum throughput");

        // ─── 3d. PagedList.Map() — item projection preserving metadata ────────
        group.MapGet("/products/map", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            var mapped = pagedList.Map(p => new ProductDto(p.Id, p.Name, p.Price));

            return Results.Ok(mapped.ToPagedResponse());
        })
        .WithSummary("PagedList.Map(): item projection while preserving pagination metadata");

        // ─── 3e. PagedList.LazyMap() — deferred projection without allocation ─
        group.MapGet("/products/lazy-map", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // LazyMap() avoids materializing an intermediate list.
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            var lazy = pagedList.LazyMap(p => new ProductDto(p.Id, p.Name, p.Price));

            return Results.Ok(lazy.ToPagedResponse());
        })
        .WithSummary("PagedList.LazyMap(): deferred projection without prior allocation");

        // ─── 3f. Manual PagedList construction with factories ─────────────────
        group.MapGet("/products/manual-factory", (
            [AsParameters] PaginationParameters pagination) =>
        {
            var items = new List<Product>
            {
                new() { Id = 1, Name = "Manual Product A", Price = 99.99m, CreatedAt = DateTime.UtcNow },
                new() { Id = 2, Name = "Manual Product B", Price = 49.99m, CreatedAt = DateTime.UtcNow }
            };
            long knownTotal = 42;

            var withCount = PagedList<Product>.WithCount(items, pagination, knownTotal);
            var withoutCount = PagedList<Product>.WithoutCount(items, pagination, hasNextPage: true);
            var empty = PagedList<Product>.Empty(pagination);

            return Results.Ok(new
            {
                WithCount = withCount.ToPagedResponse(),
                WithoutCount = withoutCount.ToPagedResponse(),
                Empty = empty.ToPagedResponse()
            });
        })
        .WithSummary("PagedList<T> factories: WithCount(), WithoutCount(), Empty() for manual construction");

        // ─── 3g. CursorPagedList.Create() — manual cursor list construction ───
        group.MapGet("/products/cursor-factory", () =>
        {
            var items = new List<Product>
            {
                new() { Id = 10, Name = "Cursor Product A", Price = 10m, CreatedAt = DateTime.UtcNow },
                new() { Id = 20, Name = "Cursor Product B", Price = 20m, CreatedAt = DateTime.UtcNow }
            };

            var encodedStart = Base64CursorEncoder.Default.Encode("S|10");
            var encodedEnd = Base64CursorEncoder.Default.Encode("S|20");

            var cursorList = CursorPagedList<Product>.Create(
                items, encodedStart, encodedEnd, hasPreviousPage: false, hasNextPage: true);

            var emptyCursor = CursorPagedList<Product>.Empty;

            return Results.Ok(new
            {
                Manual = cursorList.ToCursorPagedResponse(p => p.Id),
                IsEmptyEmpty = emptyCursor.Count == 0 && emptyCursor.StartCursor == null
            });
        })
        .WithSummary("CursorPagedList<T>.Create() and .Empty: manual construction of cursor lists");

        // ─── 3h. ICursorPagedList<T>.Map() — projection over cursor lists ─────
        group.MapGet("/products/cursor-map", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            ICursorPagedList<ProductDto> mapped = page.Map(
                p => new ProductDto(p.Id, p.Name, p.Price));

            return Results.Ok(new
            {
                MappedPage = mapped.ToCursorPagedResponse(dto => dto.Id),
                StartCursor = mapped.StartCursor,
                EndCursor = mapped.EndCursor,
                HasNextPage = mapped.HasNextPage,
                HasPreviousPage = mapped.HasPreviousPage
            });
        })
        .WithSummary("ICursorPagedList<T>.Map(): projection over cursor lists preserving cursors and navigation metadata");

        // ─── 3i. ToCursorPagedResponse with RawCursorValue ────────────────────
        group.MapGet("/products/cursor-raw-value", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(p => p.Name)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            var withRawCursorValue = page.ToCursorPagedResponse(
                rawCursorSelector: p => new RawCursorValue($"{p.Name}|{p.Id}"));

            var withTypedKey = page.ToCursorPagedResponse(p => p.Id);

            return Results.Ok(new
            {
                WithRawCursorValue = new
                {
                    withRawCursorValue.PageInfo,
                    FirstEdgeCursor = withRawCursorValue.Edges.Count > 0
                        ? withRawCursorValue.Edges[0].Cursor
                        : null,
                    Note = "Func<T, RawCursorValue>: composite cursor Name|Id, formatted explicitly"
                },
                WithTypedKey = new
                {
                    withTypedKey.PageInfo,
                    FirstEdgeCursor = withTypedKey.Edges.Count > 0
                        ? withTypedKey.Edges[0].Cursor
                        : null,
                    Note = "Func<T, int>: single Id key, formatted with InvariantCulture"
                }
            });
        })
        .WithSummary("ToCursorPagedResponse(RawCursorValue): overload for composite or custom formatted cursors");

        // ─── 3j. PagedList<T>.Empty() — optimal empty return without query ────
        group.MapGet("/products/empty-page", async (
            [AsParameters] PaginationParameters pagination,
            string? requiredFilter,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(requiredFilter))
            {
                var emptyResult = PagedList<DemoApp.Domain.Product>.Empty(pagination);
                return Results.Ok(new
                {
                    Data = emptyResult.ToPagedResponse(),
                    Reason = "requiredFilter absent — returned empty page without querying database",
                    ExactTotalCount = (emptyResult as ICountedPagedList)?.ExactTotalCount,
                    Note = "PagedList<T>.Empty(params): TotalCount=0, HasNextPage=false, zero list allocation"
                });
            }

            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                Reason = "requiredFilter present — query executed normally",
                Note = "To see Empty() in action, omit the requiredFilter query parameter"
            });
        })
        .WithSummary("PagedList<T>.Empty(params): empty return without database query — TotalCount=0, ExactTotalCount=0");
    }
}

/// <summary>Product DTO for Map() examples.</summary>
internal sealed record ProductDto(int Id, string Name, decimal Price);
