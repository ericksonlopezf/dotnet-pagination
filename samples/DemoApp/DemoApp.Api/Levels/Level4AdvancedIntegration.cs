// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using DemoApp.Application;
using DemoApp.Infrastructure;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using EricksonLopez.Pagination.EntityFrameworkCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DemoApp.Api.Levels;

/// <summary>
/// Level 4 — Advanced Integration.
/// Demonstrates: CQRS with MediatR, ToPagedResponse with HttpRequest (HATEOAS),
/// AddPaginationValidation (endpoint filter), and server-side SQL projection with selector.
/// </summary>
public static class Level4AdvancedIntegration
{
    public static void MapLevel4Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level4").WithTags("Level 4 - Advanced Integration");

        // ─── 4a. CQRS with MediatR ───────────────────────────────────────────
        group.MapGet("/products/cqrs", async (
            [AsParameters] PaginationParameters pagination,
            string? searchTerm,
            IMediator mediator) =>
        {
            // Level 4: Pagination delegated to the Application layer using CQRS.
            // The query returns IPagedList<T> — the API layer remains decoupled from EF Core.
            var query = new GetProductsQuery(pagination, searchTerm);
            var pagedResult = await mediator.Send(query);

            return Results.Ok(pagedResult.ToPagedResponse());
        })
        .WithSummary("Pagination with CQRS and MediatR: API delegates to Application layer handler");

        // ─── 4b. ToPagedResponse with HttpRequest (generates HATEOAS links) ──
        group.MapGet("/products/hateoas", async (
            [AsParameters] PaginationParameters pagination,
            HttpRequest request,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ToPagedResponse(HttpRequest) uses request URL to construct nextPageUrl/previousPageUrl.
            // Relative links operate cleanly behind reverse proxies and API gateways.
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse(request));
        })
        .WithSummary("ToPagedResponse(HttpRequest): generates nextPageUrl/previousPageUrl with relative HATEOAS links");

        // ─── 4c. AddPaginationValidation: endpoint filter for MaxPageSize ────
        group.MapGet("/products/validated", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // AddPaginationValidation() attaches PaginationEndpointFilter to the endpoint.
            // Verifies that PageSize does not exceed PaginationCoreOptions.MaxPageSize.
            // Automatically returns HTTP 400 Bad Request if validation fails.
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .AddPaginationValidation()
        .WithSummary("AddPaginationValidation(): validates PageSize against configured MaxPageSize (returns 400 on breach)");

        // ─── 4d. Server-side projection with SQL selector ────────────────────
        group.MapGet("/products/projected", async (
            [AsParameters] PaginationParameters pagination,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ToPagedListAsync<T, TResult>(source, selector, parameters) executes projection
            // directly in SQL on the database server, transferring only requested columns.
            var pagedDtos = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(
                    p => new { p.Id, p.Name, p.Price },
                    pagination,
                    cancellationToken: ct);

            return Results.Ok(pagedDtos.ToPagedResponse());
        })
        .WithSummary("ToPagedListAsync<T,TResult> with selector: server-side projection transferring only required columns");
    }
}
