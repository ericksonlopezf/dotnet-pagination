// Copyright © Erickson Lopez. MIT License.
using System;
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
/// Level 6 — Error Handling.
/// Demonstrates: InvalidPaginationCursorException, ExpiredPaginationCursorException,
/// ReplayedPaginationCursorException (cursor single-use replay attack protection),
/// PaginationExceptionHandler (IExceptionHandler .NET 8+), and defensive recovery.
/// </summary>
public static class Level6ErrorHandling
{
    public static void MapLevel6Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level6").WithTags("Level 6 - Error Handling");

        // ─── 6a. Invalid cursor — InvalidPaginationCursorException ───────────
        group.MapGet("/products/bad-cursor", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // InvalidPaginationCursorException is thrown when a cursor cannot be
            // decoded or its HMAC signature does not match (tampered cursor).
            //
            // Test with: GET /api/level6/products/bad-cursor?after=INVALID_BASE64
            var page = await db.Products
                .Keyset(cursor)
                .Ascending(p => p.Id)
                .ToCursorPagedListAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
        })
        .WithSummary("InvalidPaginationCursorException: tampered cursor intercepted by PaginationExceptionHandler (HTTP 400)");

        // ─── 6b. Expired cursor — HmacCursorEncoder with TTL ──────────────────
        group.MapGet("/products/ttl-cursor", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // HmacCursorEncoder with timeToLive: cursors that expire after a configured TTL.
            // ExpiredPaginationCursorException derives from InvalidPaginationCursorException.
            var ttlEncoder = new HmacCursorEncoder(
                secretKey: "DemoKey-MustBe32BytesLong-ForHMACSHA256",
                timeToLive: TimeSpan.FromSeconds(1)); // short TTL for demo

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
                    detail: $"Cursor expired at {ex.ExpiredAt:O}. Please restart pagination from the first page.",
                    statusCode: StatusCodes.Status410Gone);
            }
            catch (InvalidPaginationCursorException ex)
            {
                return Results.BadRequest(new { Error = ex.Message, Cursor = ex.OpaqueCursor });
            }
            finally
            {
                ttlEncoder.Dispose();
            }
        })
        .WithSummary("ExpiredPaginationCursorException: TTL cursors with distinct operational expiration vs security tampering");

        // ─── 6c. Explicit endpoint-level try/catch handling ──────────────────
        group.MapGet("/products/safe-cursor", async (
            [AsParameters] CursorPaginationParameters cursor,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            try
            {
                var page = await db.Products
                    .Keyset(cursor)
                    .Ascending(p => p.Id)
                    .ToCursorPagedListAsync(cancellationToken: ct)
                    .ConfigureAwait(false);

                return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
            }
            catch (ExpiredPaginationCursorException)
            {
                return Results.Problem(
                    title: "Cursor Expired",
                    detail: "Pagination cursor has expired. Please restart pagination.",
                    statusCode: StatusCodes.Status410Gone);
            }
            catch (InvalidPaginationCursorException ex)
            {
                return Results.BadRequest(new { Error = "Invalid cursor", Detail = ex.Message });
            }
        })
        .WithSummary("Explicit error handling: direct try/catch inside endpoint for custom error responses");

        // ─── 6d. PaginationParameters out of range ───────────────────────────
        group.MapGet("/products/param-validation", (
            [AsParameters] PaginationParameters pagination) =>
        {
            // PaginationParameters throws ArgumentOutOfRangeException if Page < 1 or PageSize < 1 or PageSize > 100000.
            // ASP.NET Core model binding converts this into a 400 Bad Request automatically.
            return Results.Ok(new
            {
                pagination.Page,
                pagination.PageSize,
                Note = "Parameters validated successfully (page >= 1, 1 <= pageSize <= 100000)"
            });
        })
        .WithSummary("Automatic PaginationParameters validation: ArgumentOutOfRangeException during binding");

        // ─── 6e. ReplayedPaginationCursorException — single-use cursors ───────
        group.MapGet("/products/replay-demo", async (
            [AsParameters] CursorPaginationParameters cursor,
            bool simulateReplay,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ReplayedPaginationCursorException is thrown when a single-use cursor
            // is decoded for a second time. It is a subtype of InvalidPaginationCursorException.
            var replayStore = new InMemoryCursorReplayStore();
            using var replayEncoder = new HmacCursorEncoder(
                secretKey: "DemoKey-MustBe32BytesLong-ForHMACSHA256",
                replayStore: replayStore,
                timeToLive: TimeSpan.FromMinutes(5));

            if (simulateReplay)
            {
                var rawCursor = "S|42";
                var encoded = replayEncoder.Encode(rawCursor);
                var firstDecode = replayEncoder.Decode(encoded); // OK

                try
                {
                    var secondDecode = replayEncoder.Decode(encoded); // Throws ReplayedPaginationCursorException
                    return Results.Ok(new { Warning = "Replay was NOT detected (unexpected)", SecondDecode = secondDecode });
                }
                catch (ReplayedPaginationCursorException ex)
                {
                    return Results.Problem(
                        title: "Cursor Already Consumed (Replay Detected)",
                        detail: $"Nonce: {ex.Nonce}. First decode was successful: '{firstDecode}'. " +
                                $"Second decode was rejected by ICursorReplayStore.",
                        statusCode: StatusCodes.Status409Conflict);
                }
            }

            try
            {
                var page = await db.Products
                    .Keyset(cursor, cursorEncoder: replayEncoder)
                    .Ascending(p => p.Id)
                    .ToCursorPagedListAsync(cancellationToken: ct)
                    .ConfigureAwait(false);

                return Results.Ok(new
                {
                    Data = page.ToCursorPagedResponse(p => p.Id),
                    Tip  = "Pass ?simulateReplay=true to observe ReplayedPaginationCursorException in action"
                });
            }
            catch (ReplayedPaginationCursorException ex)
            {
                return Results.Problem(
                    title: "Cursor Already Consumed (Replay Attack)",
                    detail: $"Nonce {ex.Nonce} already registered in ICursorReplayStore.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        })
        .WithSummary("ReplayedPaginationCursorException: single-use cursor — ?simulateReplay=true demonstrates replay defense");
    }
}
