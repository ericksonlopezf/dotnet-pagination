// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
/// Level 5 — Processing.
/// Demonstrates: ToPagedAsyncEnumerable (streaming offset), ToPagedListBatchedAsync (batch processing),
/// and Keyset ToPagedAsyncEnumerable (streaming cursor without list materialization).
/// </summary>
public static class Level5Processing
{
    public static void MapLevel5Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level5").WithTags("Level 5 - Processing");

        // ─── 5a. Streaming with IAsyncEnumerable (offset) ────────────────────
        group.MapGet("/products/stream", (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db) =>
        {
            // ToPagedAsyncEnumerable returns IAsyncEnumerable<T> over the requested page.
            // Avoids materializing all items in a List<T> before streaming response chunks.
            var stream = db.Products
                .OrderBy(p => p.Id)
                .ToPagedAsyncEnumerable(pagination);

            return Results.Ok(stream);
        })
        .WithSummary("ToPagedAsyncEnumerable: offset streaming — deferred reading without buffer allocation");

        // ─── 5b. Batch processing with ToPagedListBatchedAsync ────────────────
        group.MapGet("/products/batch-process", async (
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ToPagedListBatchedAsync processes entire dataset in sequentially paginated batches.
            // Useful for: data exports, ETL migrations, background worker tasks, and re-indexing.
            var batchSize = 100;
            var results = new List<string>();

            await foreach (var batch in db.Products
                .OrderBy(p => p.Id)
                .ToPagedListBatchedAsync(batchSize, cancellationToken: ct))
            {
                foreach (var product in batch)
                {
                    results.Add(product.Name);
                }
            }

            return Results.Ok(new
            {
                TotalProcessed = results.Count,
                Message = $"Successfully processed {results.Count} products in batches of {batchSize}"
            });
        })
        .WithSummary("ToPagedListBatchedAsync: batch processing — iterates full table across pages for ETL and jobs");

        // ─── 5c. Keyset streaming with ToPagedAsyncEnumerable ────────────────
        group.MapGet("/products/keyset-stream", (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db) =>
        {
            // KeysetBuilder.ToPagedAsyncEnumerable() combines keyset pagination with streaming.
            // Note: only forward pagination (First/After) is supported in streaming mode.
            var stream = db.Products
                .Keyset(cursor)
                .Ascending(p => p.Id)
                .ToPagedAsyncEnumerable();

            return Results.Ok(stream);
        })
        .WithSummary("KeysetBuilder.ToPagedAsyncEnumerable(): cursor streaming without buffer materialization");

        // ─── 5d. Concurrent pagination with CancellationToken ────────────────
        group.MapGet("/products/cancellable", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("CancellationToken in pagination: graceful cancellation when HTTP connection drops");
    }
}
