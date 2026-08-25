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
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;

namespace DemoApp.Api.Levels;

/// <summary>
/// Level 10 — Enterprise Architecture and Best Practices.
/// Demonstrates: ToPagedResult with automatic ETag, manual ApplyETagHeaders,
/// OutputCaching + Vary, endpoint-specific maxPageSize, OpenTelemetry metrics,
/// ToPagedListDeferredAsync (deferred join pattern), and FilterExpressionExtensions (.And/.Or/.Not).
/// </summary>
public static class Level10EnterpriseArchitecture
{
    public static void MapLevel10Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level10").WithTags("Level 10 - Enterprise Best Practices");

        // ─── 10a. ToPagedResult — IResult with automated ETag ─────────────────
        group.MapGet("/products/etag", async (
            [AsParameters] PaginationParameters pagination,
            HttpContext httpContext,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return pagedList.ToPagedResult(httpContext.Request, maxAge: TimeSpan.FromMinutes(5));
        })
        .WithSummary("ToPagedResult: IResult with deterministic ETag + Cache-Control — automated 304 Not Modified");

        // ─── 10b. ToCursorPagedResult — ETag for keyset cursor pagination ─────
        group.MapGet("/products/cursor-etag", async (
            [AsParameters] CursorPaginationParameters cursor,
            HttpContext httpContext,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return page.ToCursorPagedResult(p => p.Id, maxAge: TimeSpan.FromMinutes(5));
        })
        .WithSummary("ToCursorPagedResult: ETag + Cache-Control for keyset cursor pagination");

        // ─── 10c. OutputCaching + VaryByQuery ─────────────────────────────────
        group.MapGet("/products/cached", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("OutputCaching: server-cached paginated responses with VaryByQuery on page and pageSize")
        .CacheOutput(c => c
            .SetVaryByQuery("page", "pageSize")
            .Expire(TimeSpan.FromMinutes(5)));

        // ─── 10d. Endpoint-specific maxPageSize ───────────────────────────────
        group.MapGet("/products/max-size", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, maxPageSize: 50, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("Endpoint maxPageSize: overrides global MaxPageSize for specific high-volume export endpoints");

        // ─── 10e. ApplyETagHeaders — manual conditional ETag control ──────────
        group.MapGet("/products/manual-etag", async (
            [AsParameters] PaginationParameters pagination,
            HttpContext httpContext,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            var pagedResponse = pagedList.ToPagedResponse();
            bool isNotModified = pagedResponse.ApplyETagHeaders(httpContext, maxAge: TimeSpan.FromMinutes(2));
            if (isNotModified)
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            return Results.Ok(pagedResponse);
        })
        .WithSummary("ApplyETagHeaders: manual ETag evaluation + 304 Not Modified control");

        // ─── 10f. Observability and OpenTelemetry (Tracing and Metrics) ───────
        group.MapGet("/products/observability", () =>
        {
            return Results.Ok(new
            {
                MeterName = PaginationMetrics.MeterName,
                MeterVersion = PaginationMetrics.Meter.Version,
                Instruments = new[]
                {
                    PaginationMetrics.QueriesTotal.Name + " — " + PaginationMetrics.QueriesTotal.Description,
                    PaginationMetrics.CursorErrors.Name + " — " + PaginationMetrics.CursorErrors.Description,
                    PaginationMetrics.PageSize.Name + " — " + PaginationMetrics.PageSize.Description,
                    PaginationMetrics.PageDepth.Name + " — " + PaginationMetrics.PageDepth.Description,
                    PaginationDiagnostics.LegacyCursorCounter.Name + " — Counter for legacy accepted cursors"
                }
            });
        })
        .WithSummary("OpenTelemetry Observability: native Meter and instruments for pagination telemetry");

        // ─── 10g. ToPagedListDeferredAsync — deferred join pattern ────────────
        group.MapGet("/products/deferred-join", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListDeferredAsync(
                    keySelector: p => p.Id,
                    parameters: pagination,
                    countTotal: true,
                    maxPageSize: null,
                    cancellationToken: ct);

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                Pattern = "Deferred Join: first IDs (O(offset) lightweight rows), then full entities (O(pageSize) items)",
                BestFor = "Wide tables with large payloads, numerous .Include() joins, or cartesian explosion prevention"
            });
        })
        .WithSummary("ToPagedListDeferredAsync: deferred join pattern — IDs first, full entities second for wide tables");

        // ─── 10h. FilterExpressionExtensions (.And/.Or/.Not) ─────────────────
        group.MapGet("/products/composed-filter", async (
            [AsParameters] PaginationParameters pagination,
            decimal? minPrice,
            decimal? maxPrice,
            string? nameContains,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            Expression<Func<Domain.Product, bool>> predicate = _ => true;

            if (minPrice.HasValue)
            {
                var minFilter = (Expression<Func<Domain.Product, bool>>)(p => p.Price >= minPrice.Value);
                predicate = predicate.And(minFilter);
            }

            if (maxPrice.HasValue)
            {
                var maxFilter = (Expression<Func<Domain.Product, bool>>)(p => p.Price <= maxPrice.Value);
                predicate = predicate.And(maxFilter);
            }

            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                var nameFilter = (Expression<Func<Domain.Product, bool>>)(p => p.Name.Contains(nameContains));
                predicate = predicate.And(nameFilter);
            }

            var pagedList = await db.Products
                .Where(predicate)
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                Note = "FilterExpressionExtensions (.And/.Or/.Not): AOT-safe strongly typed predicate composition"
            });
        })
        .WithSummary("FilterExpressionExtensions (.And/.Or/.Not): fluent typed Expression<Func<T,bool>> composition");

        // ─── 10i. AddPaginationValidation() on RouteGroupBuilder ──────────────
        var exportGroup = app.MapGroup("/api/level10/export")
            .WithTags("Level 10 - Enterprise Best Practices")
            .AddPaginationValidation();

        exportGroup.MapGet("/products", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, maxPageSize: 100, cancellationToken: ct);

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                Note = "Protected by AddPaginationValidation() applied to RouteGroupBuilder"
            });
        })
        .WithSummary("Export endpoint #1: protected by RouteGroupBuilder.AddPaginationValidation() (400 if PageSize > MaxPageSize)");

        exportGroup.MapGet("/products/summary", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, maxPageSize: 100, cancellationToken: ct);

            var summary = pagedList.Map(p => new { p.Id, p.Name });

            return Results.Ok(new
            {
                Data = summary.ToPagedResponse(),
                Note = "Shares group-level protection from AddPaginationValidation() on RouteGroupBuilder"
            });
        })
        .WithSummary("Export endpoint #2: inherits RouteGroupBuilder AddPaginationValidation() protection");
    }
}
