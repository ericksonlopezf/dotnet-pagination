// Copyright © Erickson Lopez. MIT License.
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
/// Level 0 — Conceptual.
/// What is EricksonLopez.Pagination? What problem does it solve?
/// When to use Offset vs Keyset? What are their benefits and limitations?
/// This level does not execute live database queries — it answers conceptual architectural questions via API.
/// </summary>
public static class Level0Conceptual
{
    public static void MapLevel0Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level0").WithTags("Level 0 - Conceptual");

        // ─── 0a. What is the library? ───────────────────────────────────────
        group.MapGet("/about", () => Results.Ok(new
        {
            Library = "EricksonLopez.Pagination",
            Version = "Preview",
            Description = """
                A high-performance, zero-allocation pagination library for .NET.
                Supports offset and keyset (cursor) pagination across EF Core, Dapper,
                MongoDB, Cosmos DB, and gRPC, with dynamic DSL filtering/sorting,
                native Native AOT support, and HMAC-signed secure cursors.
                """,
            PackageEntryPoints = new[]
            {
                "EricksonLopez.Pagination — core types (IPagedList, PaginationParameters, encoders)",
                "EricksonLopez.Pagination.Abstractions — interfaces without implementation (for domain layers)",
                "EricksonLopez.Pagination.EntityFrameworkCore — extensions for EF Core",
                "EricksonLopez.Pagination.AspNetCore — binders, responses, ETags for Minimal APIs / Controllers",
                "EricksonLopez.Pagination.Dapper — extensions for Dapper / IDbConnection",
                "EricksonLopez.Pagination.MongoDB — extensions for MongoDB.Driver",
                "EricksonLopez.Pagination.Cosmos — extensions for Azure Cosmos DB"
            }
        }))
        .WithSummary("What is EricksonLopez.Pagination? Description and available packages");

        // ─── 0b. What problem does it solve? ────────────────────────────────
        group.MapGet("/problem", () => Results.Ok(new
        {
            Problem = "Manual pagination is repetitive, error-prone, and difficult to scale",
            ManualApproach = new[]
            {
                "Manually compute Skip/Take: (page-1)*pageSize and pageSize",
                "Execute separate COUNT(*) query for TotalCount",
                "Construct JSON response with metadata (page, pageSize, totalPages, hasNextPage)",
                "Manually append X-Pagination headers or HATEOAS links",
                "Protect cursors against tampering in keyset pagination",
                "Implement secure DSL filtering protected against SQL injection",
                "Globally handle InvalidPaginationCursorException",
                "Compute ETags for cacheable responses"
            },
            WhatThisLibraryDoes = new[]
            {
                "ToPagedListAsync() executes COUNT + SELECT in a single configurable call",
                "PaginationParameters/CursorPaginationParameters bind automatically from HTTP queries",
                "ToPagedResponse() generates standard JSON responses with full metadata",
                "HmacCursorEncoder signs and verifies cursors with HMAC-SHA256",
                "FilterParameters + ApplyFilter() compiles a type-safe DSL into secure SQL",
                "ToPagedResult() generates deterministic ETags and supports HTTP 304 Not Modified"
            }
        }))
        .WithSummary("What problem does it solve? Why this library exists");

        // ─── 0c. Offset vs Keyset — when to use each? ───────────────────────
        group.MapGet("/offset-vs-keyset", () => Results.Ok(new
        {
            OffsetPagination = new
            {
                Api = "PaginationParameters + ToPagedListAsync()",
                Mechanism = "SELECT ... ORDER BY Id OFFSET (page-1)*pageSize ROWS FETCH NEXT pageSize ROWS",
                Complexity = "O(N) — database engine must read and discard all preceding rows",
                Advantages = new[]
                {
                    "Supports jumping directly to any arbitrary page (e.g., page 500 of 1000)",
                    "Compatible with any sorting column",
                    "Provides TotalCount/TotalPages for page-number UI paginators ('1 2 3 ... N')",
                    "Simple to understand and implement"
                },
                Disadvantages = new[]
                {
                    "Performance degrades linearly on large tables (>100K rows)",
                    "Inconsistent results if rows are inserted/deleted between page requests",
                    "Additional COUNT(*) can be expensive on massive tables"
                },
                WhenToUse = "Small to medium tables, UIs requiring direct page number navigation, administrative reports"
            },
            KeysetPagination = new
            {
                Api = "CursorPaginationParameters + KeysetBuilder.Ascending/Descending().ToCursorPagedListAsync()",
                Mechanism = "SELECT ... WHERE Id > @lastId ORDER BY Id FETCH NEXT pageSize ROWS — uses index seek",
                Complexity = "O(log N) — utilizes B-Tree index seek directly, zero SKIP overhead",
                Advantages = new[]
                {
                    "Constant execution time regardless of page depth",
                    "Consistent results even when rows are concurrently inserted or deleted",
                    "Ideal for real-time streaming feeds (social media, IoT telemetry, audit logs)",
                    "Supports backward pagination (last/before)"
                },
                Disadvantages = new[]
                {
                    "Does not support jumping to arbitrary random page numbers",
                    "Requires indexed sorting columns",
                    "Not compatible with Native AOT (internally compiles expression trees)",
                    "Does not natively provide TotalCount/TotalPages"
                },
                WhenToUse = "Large datasets (>100K rows), infinite scroll UIs, real-time activity feeds, telemetry logs"
            }
        }))
        .WithSummary("Offset vs Keyset: technical differences, advantages, disadvantages, and selection criteria");

        // ─── 0d. Comparison with alternatives ───────────────────────────────
        group.MapGet("/comparison", () => Results.Ok(new
        {
            Note = "Comparison based on verifiable architectural capabilities of each library",
            Libraries = new[]
            {
                new
                {
                    Name = "EricksonLopez.Pagination",
                    OffsetPagination = true,
                    KeysetPagination = "Yes — N columns",
                    FilterDsl = "Yes — name~=John,age>=18",
                    HmacSignedCursors = true,
                    NativeAot = true,
                    DataSources = "EF Core, Dapper, MongoDB, Cosmos DB, gRPC",
                    RoslynAnalyzers = "PAG001–PAG007",
                    BlazorComponent = "Yes — headless"
                },
                new
                {
                    Name = "X.PagedList",
                    OffsetPagination = true,
                    KeysetPagination = "No",
                    FilterDsl = "No",
                    HmacSignedCursors = false,
                    NativeAot = false,
                    DataSources = "IEnumerable, EF Core",
                    RoslynAnalyzers = "No",
                    BlazorComponent = "No"
                },
                new
                {
                    Name = "Gridify",
                    OffsetPagination = true,
                    KeysetPagination = "No",
                    FilterDsl = "Yes — name=John",
                    HmacSignedCursors = false,
                    NativeAot = false,
                    DataSources = "EF Core, Dapper",
                    RoslynAnalyzers = "No",
                    BlazorComponent = "No"
                },
                new
                {
                    Name = "Sieve",
                    OffsetPagination = true,
                    KeysetPagination = "No",
                    FilterDsl = "Yes — via attributes",
                    HmacSignedCursors = false,
                    NativeAot = false,
                    DataSources = "EF Core",
                    RoslynAnalyzers = "No",
                    BlazorComponent = "No"
                }
            }
        }))
        .WithSummary("Comparison with alternatives: X.PagedList, Gridify, Sieve — verifiable architectural capabilities");

        // ─── 0e. Explicit advantages and trade-offs ─────────────────────────
        group.MapGet("/tradeoffs", () => Results.Ok(new
        {
            Advantages = new[]
            {
                "Unified API surface: identical patterns across EF Core, Dapper, MongoDB, and Cosmos DB",
                "Security: HMAC-SHA256 signature verification prevents cursor forgery and tampering",
                "Native AOT: zero reflection in the hot path of offset pagination",
                "Roslyn Analyzers: compile-time defect detection (e.g. missing OrderBy clause)",
                "Compile-time safe filter DSL: avoids fragile string concatenation",
                "Deterministic ETags: functions across multi-pod clusters without shared distributed state",
                "Multi-column keyset seek: formal guarantee of O(1) B-Tree seeks (ADR 0019)"
            },
            Limitations = new[]
            {
                "Keyset pagination is not compatible with Native AOT (relies on Expression.Compile)",
                "FilterExpression utilizes reflection — incompatible with trimming/AOT",
                "PostgreSQL approximate COUNT is non-transactional (statistical estimate from pg_class)",
                "KeysetBuilder.ToPagedAsyncEnumerable() only supports forward pagination",
                "HmacCursorEncoder with TTL requires cluster clock synchronization (NTP)",
                "Dapper: parameter names @__Pagination_Skip__ and @__Pagination_Limit__ are reserved"
            },
            GapDocumentation = new[]
            {
                "MongoDB, Cosmos DB, and gRPC providers are tested in test suites but not demonstrated in this InMemory DemoApp",
                "(these providers require external infrastructure not hosted in the demo memory context)"
            }
        }))
        .WithSummary("Documented advantages, limitations, and architectural trade-offs");
    }
}
