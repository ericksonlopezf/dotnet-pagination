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
/// Level 1 — Quick Start.
/// Demonstrates basic offset pagination using <c>ToPagedListAsync</c> and <c>ToPagedResponse</c>.
/// This level requires only two lines of business code: <c>.OrderBy().ToPagedListAsync()</c>
/// and <c>.ToPagedResponse()</c> to obtain a complete paginated JSON response.
/// </summary>
public static class Level1QuickStart
{
    public static void MapLevel1Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level1").WithTags("Level 1 - Quick Start");

        group.MapGet("/products", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // Level 1: Most basic offset pagination workflow
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("Basic offset pagination with ToPagedListAsync");
    }
}
