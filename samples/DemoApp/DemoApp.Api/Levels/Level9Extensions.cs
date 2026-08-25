// Copyright © Erickson Lopez. MIT License.
using System;
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
using Microsoft.EntityFrameworkCore;

namespace DemoApp.Api.Levels;

/// <summary>
/// Level 9 — Complex Architecture.
/// Demonstrates: Multi-column keyset, descending keyset, backward pagination (last/before),
/// CursorPaginationParameters.GetPageSize(defaultSize), PostgreSQL approximate count,
/// parallel partitioning (int and long overloads), and cursor schema migration via AcceptLegacyCursors.
/// </summary>
public static class Level9Extensions
{
    public static void MapLevel9Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level9").WithTags("Level 9 - Complex Architecture");

        // ─── 9a. Multi-column Keyset with unique tiebreaker ──────────────────
        group.MapGet("/products/multitenant", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // KeysetBuilder supports N sorting columns.
            // The final column MUST always be unique (p.Id) to break ties deterministically.
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(p => p.Name)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(page.ToCursorPagedResponse(p => $"{p.Name}|{p.Id}"));
        })
        .WithSummary("Multi-column Keyset: Ascending(Name) + Ascending(Id) with unique tiebreaker");

        // ─── 9b. Descending Keyset ───────────────────────────────────────────
        group.MapGet("/products/descending", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var page = await db.Products
                .Keyset(cursor)
                .Descending(p => p.Price)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
        })
        .WithSummary("Keyset Descending: descending sort with ascending tiebreaker on Id");

        // ─── 9c. Backward pagination (last/before) ────────────────────────────
        group.MapGet("/products/backward", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            var response = page.ToCursorPagedResponse(p => p.Id);
            return Results.Ok(new
            {
                Page = response,
                Note = "Pass last=N&before={endCursor} to paginate backward"
            });
        })
        .WithSummary("Backward pagination: CursorPaginationParameters.Last + Before to navigate backward");

        // ─── 9d. CursorPaginationParameters.Default + GetPageSize() ──────────
        group.MapGet("/products/cursor-defaults", ([AsParameters] CursorPaginationParameters cursor) =>
        {
            var cursorDefault = CursorPaginationParameters.Default;
            var offsetDefault = PaginationParameters.Default;

            int effectivePageSize   = cursor.GetPageSize(defaultSize: 10);
            int effectivePageSize20 = cursor.GetPageSize(defaultSize: 20);
            int defaultPageSize     = cursorDefault.GetPageSize(defaultSize: 15);

            return Results.Ok(new
            {
                CursorFromRequest = new
                {
                    cursor.First,
                    cursor.After,
                    cursor.Last,
                    cursor.Before,
                    GetPageSize_default10 = effectivePageSize,
                    GetPageSize_default20 = effectivePageSize20
                },
                CursorDefault = new
                {
                    cursorDefault.First,
                    cursorDefault.After,
                    cursorDefault.Last,
                    cursorDefault.Before,
                    GetPageSize_default15 = defaultPageSize
                },
                OffsetDefault = new
                {
                    offsetDefault.Page,
                    offsetDefault.PageSize
                },
                TryIt = "?first=25 -> GetPageSize(10)=25 | ?last=50 -> GetPageSize(10)=50 | no params -> GetPageSize(10)=10"
            });
        })
        .WithSummary("CursorPaginationParameters.Default + GetPageSize(defaultSize): resolves effective page size with fallback");

        // ─── 9e. ApproximateCount — PostgreSQL statistical count ──────────────
        group.MapGet("/products/approximate-count", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, countTotal: true, useApproximateCount: true, cancellationToken: ct);

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                Warning = "useApproximateCount=true: non-transactional, utilizes pg_class.reltuples in PostgreSQL"
            });
        })
        .WithSummary("useApproximateCount=true: fast count via pg_class.reltuples (PostgreSQL only)");

        // ─── 9f. Keyset partitioning for parallel workers ────────────────────
        group.MapGet("/products/partitions", async (
            int? partitionCount,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var count = partitionCount.GetValueOrDefault(4);
            var partitions = await db.Products
                .SplitKeysetPartitionsAsync(p => p.Id, count, cancellationToken: ct)
                .ConfigureAwait(false);

            string? partition0StartCursor = partitions.Count > 0 ? partitions[0].StartCursor : null;
            string? partition0EndCursor   = partitions.Count > 0 ? partitions[0].EndCursor   : null;

            return Results.Ok(new
            {
                RequestedPartitions = count,
                ActualPartitions = partitions.Count,
                Partitions = partitions,
                WorkerUsagePattern = new
                {
                    Partition0StartCursor = partition0StartCursor,
                    Partition0EndCursor   = partition0EndCursor,
                    WorkerRequest = partition0StartCursor is not null
                        ? $"/api/level3/products/keyset?first=100&after={Uri.EscapeDataString(partition0StartCursor)}"
                        : null,
                    Note = "Each worker uses StartCursor as 'after' in CursorPaginationParameters"
                }
            });
        })
        .WithSummary("SplitKeysetPartitionsAsync: parallel partitioning with StartCursor/EndCursor per partition");

        // ─── 9g. SplitKeysetPartitionsAsync<long> — 64-bit PK overload ────────
        group.MapGet("/products/partitions-long", async (
            int? partitionCount,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var count = partitionCount.GetValueOrDefault(4);
            var source = db.Products.Select(p => new { LongId = (long)p.Id });
            var partitions = await source
                .SplitKeysetPartitionsAsync(p => p.LongId, count, cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(new
            {
                RequestedPartitions = count,
                ActualPartitions = partitions.Count,
                Partitions = partitions,
                Note = "SplitKeysetPartitionsAsync<long>: overload for 64-bit bigint primary keys"
            });
        })
        .WithSummary("SplitKeysetPartitionsAsync<long>: 64-bit PK (bigint) overload for parallel partitioning");

        // ─── 9h. PaginationCoreOptions.AcceptLegacyCursors — migration ───────
        group.MapGet("/products/cursor-migration-info", () =>
        {
            return Results.Ok(new
            {
                OptionName = "PaginationCoreOptions.AcceptLegacyCursors",
                DefaultValue = true,
                CurrentlyAccepting = new[] { "v1 (M|col1|col2)", "v2 (M|v2|fingerprint|col1|col2)" },
                AfterMigration = new[] { "v2 (M|v2|fingerprint|col1|col2) ONLY" },
                MigrationSteps = new[]
                {
                    "1. Deploy with AcceptLegacyCursors=true (default)",
                    "2. Wait for active v1 cursors to expire (1-7 days)",
                    "3. Monitor LegacyCursorCounter in OpenTelemetry until it reaches zero",
                    "4. Switch to AcceptLegacyCursors=false and redeploy"
                },
                MonitoringInstrument = PaginationDiagnostics.LegacyCursorCounter.Name
            });
        })
        .WithSummary("AcceptLegacyCursors: v1 to v2 cursor migration with cryptographic fingerprint");

        // ─── 9i. KeysetBuilder.Ascending(string propertyName) ────────────────
        group.MapGet("/products/keyset-dynamic-column", async (
            [AsParameters] CursorPaginationParameters cursor,
            string? sortColumn,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var allowedColumns = new[] { "Name", "Price", "Id" };
            var column = allowedColumns.Contains(sortColumn, StringComparer.OrdinalIgnoreCase)
                ? sortColumn!
                : "Id";

#pragma warning disable IL2026 // RequiresUnreferencedCode — dynamic sorting via reflection
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(column, allowedProperties: allowedColumns)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);
#pragma warning restore IL2026

            return Results.Ok(new
            {
                ColumnUsed = column,
                AllowedColumns = allowedColumns,
                Data = page.ToCursorPagedResponse(p => p.Id),
                Note = "Ascending(string) uses reflection: use Ascending(lambda) in Native AOT environments"
            });
        })
        .WithSummary("KeysetBuilder.Ascending(string): dynamic sorting column with allowlist validation");
    }
}
