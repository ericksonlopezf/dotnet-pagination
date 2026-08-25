// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DemoApp.Application;
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
/// Level 2 — Full Configuration.
/// Demonstrates: FilterParameters, SortParameters, ApplyFilter, ApplySort,
/// PaginationParameters with explicit values, the integrated combo
/// ToPagedListAsync(filter, sort, parameters), IFilterOperatorProvider&lt;T&gt;
/// with custom operators, ToPagedListAsync(filter, sort, selector, parameters)
/// with server-side projection, and PaginationSettings (global fallback constants).
/// </summary>
public static class Level2FullConfiguration
{
    public static void MapLevel2Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level2").WithTags("Level 2 - Full Configuration");

        // ─── 2a. Dynamic filtering with FilterParameters ──────────────────────
        group.MapGet("/products/filter", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] FilterParameters filter,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // FilterParameters encapsulates the filtering DSL: field=value, field>=value, field~=value, etc.
            // ApplyFilter translates the DSL to a LINQ/SQL expression via reflection.
            // Request example: GET /api/level2/products/filter?filter=name~=Product 1,price>=10&page=1&pageSize=20
            var pagedList = await db.Products
                .ApplyFilter(filter)
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("FilterParameters + ApplyFilter: dynamic DSL filtering over entity fields");

        // ─── 2b. Dynamic sorting with SortParameters ──────────────────────────
        group.MapGet("/products/sort", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] SortParameters sortBy,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // SortParameters encapsulates the sorting string: "name asc,price desc"
            // ApplySort translates the tokens to OrderBy/ThenBy in LINQ.
            // Example: GET /api/level2/products/sort?sortBy=name asc,price desc&page=1&pageSize=10
            var pagedList = await db.Products
                .ApplySort(sortBy, defaultSort: p => (object)p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("SortParameters + ApplySort: multi-column dynamic sorting");

        // ─── 2c. Integrated combo: filter + sort + paginate in a single call ───
        group.MapGet("/products/filter-sort", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] FilterParameters filter,
            [AsParameters] SortParameters sortBy,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // The overload ToPagedListAsync(filter, sortBy, parameters) applies
            // filter + sort + pagination in a single pipeline execution.
            // This is the recommended entry point for data table / grid endpoints.
            // Example: GET /api/level2/products/filter-sort?filter=price>=50&sortBy=name asc&page=1&pageSize=20
            var pagedList = await db.Products
                .ToPagedListAsync(filter, sortBy, pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("Integrated ToPagedListAsync: filter + sort + paginate in a single call");

        // ─── 2d. PaginationParameters: explicit values and Create() factory ───
        group.MapGet("/products/explicit", async (
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // PaginationParameters.Create() validates parameters during instantiation.
            // Enables programmatic construction without relying on HTTP model binding.
            var pagination = PaginationParameters.Create(page: 2, pageSize: 5);

            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("PaginationParameters.Create(): programmatic creation without HTTP model binding");

        // ─── 2e. SortParameters.ValidateColumnName: dynamic sort security ─────
        group.MapGet("/products/sort-validated", async (
            [AsParameters] PaginationParameters pagination,
            string? sortBy,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ValidateColumnName protects against injection by restricting allowed sorting columns.
            var allowedSortColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Name", "Price", "CreatedAt"
            };

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                // Throws InvalidOperationException if any column is not in the allowlist.
                SortParameters.ValidateColumnName(sortBy, allowedSortColumns);
            }

            var sort = SortParameters.From(sortBy);

            var pagedList = await db.Products
                .ApplySort(sort, defaultSort: p => (object)p.Id, allowedProperties: allowedSortColumns)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("SortParameters.ValidateColumnName: column allowlist validation to prevent sorting injection");

        // ─── 2f. FilterParameters DSL with field OR conditions ────────────────
        group.MapGet("/products/filter-or", async (
            [AsParameters] PaginationParameters pagination,
            string? filter,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // FilterParameters supports OR conditions per field using the '|' separator inside a clause.
            // Syntax: field=value1|value2  ->  WHERE field = 'value1' OR field = 'value2'
            // Multiple clauses are combined with AND: field1=v1|v2,field2>=50
            //
            // Example: GET /api/level2/products/filter-or?filter=name~=Electronics|name~=Books
            var filterParams = FilterParameters.From(filter);

            var pagedList = await db.Products
                .ApplyFilter(filterParams)
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            return Results.Ok(new
            {
                FilterApplied = filterParams.HasValue ? filterParams.Value : "(none)",
                Data = pagedList.ToPagedResponse(),
                DslNote = "Use '|' inside a clause for OR logic: filter=name~=Electronics|name~=Books"
            });
        })
        .WithSummary("FilterParameters DSL with OR: field=v1|v2 applies OR within field, AND between clauses");

        // ─── 2g. ToPagedResponse with requestUri (string overload) ────────────
        group.MapGet("/products/response-uri", async (
            [AsParameters] PaginationParameters pagination,
            HttpRequest request,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ToPagedResponse provides multiple overloads:
            //   A) .ToPagedResponse()            — without links (metadata only)
            //   B) .ToPagedResponse(HttpRequest) — generates relative nextPageUrl / previousPageUrl
            //   C) .ToPagedResponse(string?)     — accepts custom base URI (for API gateways, proxies, tests)
            var pagedList = await db.Products
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);

            var customUri = $"/api/v2/catalog/products?pageSize={pagination.PageSize}&page={pagination.Page}";

            return Results.Ok(new
            {
                WithHttpRequest = pagedList.ToPagedResponse(request),
                WithCustomUri = pagedList.ToPagedResponse(customUri),
                WithoutLinks = pagedList.ToPagedResponse(),
                Note = "ToPagedResponse provides 3 overloads: metadata only, with HttpRequest, and with custom string URI"
            });
        })
        .WithSummary("ToPagedResponse overloads: no links, with HttpRequest, and with custom URI string");

        // ─── 2h. IFilterOperatorProvider<T> — custom filter operator ──────────
        group.MapGet("/products/custom-operator", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] FilterParameters filter,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // IFilterOperatorProvider<T> extends the filter DSL engine with custom operators.
            // Example: "%=" operator for fuzzy-match (Name starts with or contains the value).
            //   GET /api/level2/products/custom-operator?filter=name%25=Pro&page=1&pageSize=10

#pragma warning disable IL2026, IL3050 // RequiresUnreferencedCode/DynamicCode — demonstration of IFilterOperatorProvider
            var pagedList = await db.Products
                .ApplyFilter(filter, new ProductFuzzyFilterOperatorProvider())
                .OrderBy(p => p.Id)
                .ToPagedListAsync(pagination, cancellationToken: ct);
#pragma warning restore IL2026, IL3050

            return Results.Ok(new
            {
                Data = pagedList.ToPagedResponse(),
                CustomOperator = "%=",
                Description = "Fuzzy match: name%25=value (URL: %25 = %) — starts with or contains the value",
                ExampleRequest = "?filter=name%25=Pro&page=1&pageSize=10",
                StandardOperatorsAlsoWork = "?filter=name%25=Pro,price>=50",
                Note = "IFilterOperatorProvider<T> extends the DSL without replacing standard operators."
            });
        })
        .WithSummary("IFilterOperatorProvider<T>: extends the filter DSL with custom operators (e.g. %= fuzzy-match)");

        // ─── 2i. ToPagedListAsync(Filter, Sort, Selector, Params) ─────────────
        group.MapGet("/products/filter-sort-projected", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] FilterParameters filter,
            [AsParameters] SortParameters sortBy,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // ToPagedListAsync<T, TResult>(source, filter, sort, selector, parameters) combines
            // all steps in a single server-side operation:
            //   1. ApplyFilter   — translates DSL to SQL WHERE
            //   2. ApplySort     — translates SortParameters to SQL ORDER BY
            //   3. SELECT(...)   — server-side column projection
            //   4. Pagination    — OFFSET / FETCH with target parameters

#pragma warning disable IL2026, IL3050 // RequiresUnreferencedCode/DynamicCode — reflection-based filter/sort
            var pagedDtos = await db.Products
                .ToPagedListAsync(
                    filter,
                    sortBy,
                    p => new { p.Id, p.Name, p.Price },
                    pagination,
                    cancellationToken: ct);
#pragma warning restore IL2026, IL3050

            return Results.Ok(new
            {
                Data = pagedDtos.ToPagedResponse(),
                Overload = "ToPagedListAsync<T,TResult>(filter, sort, selector, params)",
                Columns = new[] { "Id", "Name", "Price" },
                Note = "Complete server-side pipeline: filter + sort + SELECT projection + pagination in one call"
            });
        })
        .WithSummary("ToPagedListAsync<T,TResult>(filter, sort, selector, params): complete server-side pipeline");

        // ─── 2j. PaginationCoreOptions — DI configuration and effective limits ─
        group.MapGet("/pagination-options", (Microsoft.Extensions.Options.IOptions<PaginationCoreOptions> opts) =>
        {
            var options = opts.Value;
            return Results.Ok(new
            {
                MaxPageSize                = options.MaxPageSize,
                DefaultPageSize            = options.DefaultPageSize,
                DeepOffsetWarningThreshold = options.DeepOffsetWarningThreshold,
                MaxFilterComplexity        = options.MaxFilterComplexity,
                MaxFilterStringLength      = options.MaxFilterStringLength,
                MaxFilterValueLength       = options.MaxFilterValueLength,
                MaxPropertyDepth           = options.MaxPropertyDepth,
                Description = "Active library configuration — configured via AddPagination() in Program.cs"
            });
        })
        .WithSummary("PaginationCoreOptions: active library configuration via DI — MaxPageSize, anti-DoS limits, etc.");
    }
}

/// <summary>
/// Implementation of IFilterOperatorProvider&lt;Product&gt; with a custom fuzzy-match operator.
/// </summary>
file sealed class ProductFuzzyFilterOperatorProvider : IFilterOperatorProvider<DemoApp.Domain.Product>
{
    public IReadOnlyDictionary<string, FilterOperatorHandler<DemoApp.Domain.Product>> Operators { get; } =
        new Dictionary<string, FilterOperatorHandler<DemoApp.Domain.Product>>(StringComparer.OrdinalIgnoreCase)
        {
            ["%="] = (propertyExpr, rawValue) =>
            {
                var constValue = System.Linq.Expressions.Expression.Constant(rawValue);
                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
                return System.Linq.Expressions.Expression.Call(propertyExpr, containsMethod, constValue);
            }
        };
}
