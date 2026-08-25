// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using EricksonLopez.Pagination.Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;

namespace DemoApp.Api.Levels;

/// <summary>
/// Level 7 — Scalability with Dapper.
/// Demonstrates: ToPagedListAsync with Dapper (countTotal=true and false),
/// ToPagedAsyncEnumerable with Dapper, and additional user parameter handling.
/// </summary>
public static class Level7Scalability
{
    public static void MapLevel7Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/level7").WithTags("Level 7 - Scalability (Dapper)");

        // ─── 7a. Dapper with COUNT — double result set ───────────────────────
        group.MapGet("/products/dapper-counted", async (
            [AsParameters] PaginationParameters pagination) =>
        {
            // ToPagedListAsync Dapper with countTotal=true:
            // The SQL MUST return TWO result sets:
            //   1st: SELECT COUNT(*) FROM ...
            //   2nd: SELECT ... LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__
            // Parameters @__Pagination_Skip__ and @__Pagination_PageSize__ are injected automatically.
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Product (Id INTEGER PRIMARY KEY, Name TEXT, Price REAL);
                INSERT INTO Product (Name, Price) VALUES
                    ('Dapper Product 1', 19.99), ('Dapper Product 2', 29.99),
                    ('Dapper Product 3', 39.99), ('Dapper Product 4', 49.99),
                    ('Dapper Product 5', 59.99);
            ");

            var pagedList = await connection.ToPagedListAsync<DapperProductDto>(
                sql: @"
                    SELECT COUNT(*) FROM Product;
                    SELECT * FROM Product ORDER BY Id
                    LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__",
                parameters: pagination,
                countTotal: true);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("Dapper countTotal=true: 2-result-set SQL (COUNT + SELECT) with automated parameters");

        // ─── 7b. Dapper without COUNT — single result set ────────────────────
        group.MapGet("/products/dapper", async (
            [AsParameters] PaginationParameters pagination) =>
        {
            // ToPagedListAsync Dapper with countTotal=false (default):
            // Returns a single result set using @__Pagination_Limit__ = PageSize + 1.
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Product (Id INTEGER PRIMARY KEY, Name TEXT, Price REAL);
                INSERT INTO Product (Name, Price) VALUES
                    ('Dapper Product 1', 19.99), ('Dapper Product 2', 29.99),
                    ('Dapper Product 3', 39.99);
            ");

            var pagedList = await connection.ToPagedListAsync<DapperProductDto>(
                sql: "SELECT * FROM Product ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__",
                parameters: pagination,
                countTotal: false);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("Dapper countTotal=false: 1-result-set SQL with @__Pagination_Limit__ and @__Pagination_Skip__");

        // ─── 7c. Dapper with additional user parameters ──────────────────────
        group.MapGet("/products/dapper-filtered", async (
            [AsParameters] PaginationParameters pagination,
            string? searchTerm) =>
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Product (Id INTEGER PRIMARY KEY, Name TEXT, Price REAL);
                INSERT INTO Product (Name, Price) VALUES
                    ('Search Product A', 10.00), ('Search Product B', 20.00),
                    ('Other Product C', 30.00);
            ");

            var sql = string.IsNullOrWhiteSpace(searchTerm)
                ? "SELECT * FROM Product ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__"
                : "SELECT * FROM Product WHERE Name LIKE @SearchTerm ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__";

            var param = string.IsNullOrWhiteSpace(searchTerm)
                ? (object?)null
                : new { SearchTerm = $"%{searchTerm}%" };

            var pagedList = await connection.ToPagedListAsync<DapperProductDto>(
                sql: sql,
                parameters: pagination,
                param: param,
                countTotal: false);

            return Results.Ok(pagedList.ToPagedResponse());
        })
        .WithSummary("Dapper with additional parameters: user parameters combined with automated pagination parameters");

        // ─── 7d. Dapper streaming — IAsyncEnumerable ─────────────────────────
        group.MapGet("/products/dapper-stream", (
            [AsParameters] PaginationParameters pagination) =>
        {
            static async IAsyncEnumerable<DapperProductDto> StreamProducts(PaginationParameters pagination)
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                await connection.OpenAsync();
                await connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS Product (Id INTEGER PRIMARY KEY, Name TEXT, Price REAL);
                    INSERT INTO Product (Name, Price) VALUES
                        ('Stream Product 1', 1.00), ('Stream Product 2', 2.00),
                        ('Stream Product 3', 3.00);
                ");

                await foreach (var item in connection.ToPagedAsyncEnumerable<DapperProductDto>(
                    sql: "SELECT * FROM Product ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__",
                    parameters: pagination))
                {
                    yield return item;
                }

                await connection.DisposeAsync();
            }

            return Results.Ok(StreamProducts(pagination));
        })
        .WithSummary("Dapper ToPagedAsyncEnumerable: streaming from raw SQL without list materialization");

        // ─── 7e. Dapper Keyset Pagination with DapperKeysetBuilder ───────────
        group.MapGet("/products/dapper-keyset", async (
            [AsParameters] CursorPaginationParameters cursor) =>
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Product (Id INTEGER PRIMARY KEY, Name TEXT, Price REAL);
                INSERT INTO Product (Name, Price) VALUES
                    ('Keyset Product 1', 19.99), ('Keyset Product 2', 29.99),
                    ('Keyset Product 3', 39.99), ('Keyset Product 4', 49.99),
                    ('Keyset Product 5', 59.99);
            ");

            var page = await new DapperKeysetBuilder<DapperProductDto>(connection, cursor)
                .Select("Id, Name, Price")
                .From("Product")
                .OrderBy("Price", SortDirection.Ascending)
                .ThenBy("Id", SortDirection.Ascending)
                .WithCursorColumns(
                    p => p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p => p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .WithCursorDecoder(
                    parts => decimal.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                    parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture))
                .UseDialect(DatabaseDialect.Sqlite)
                .ExecuteAsync();

            return Results.Ok(page.ToCursorPagedResponse(p => $"{p.Price}|{p.Id}"));
        })
        .WithSummary("DapperKeysetBuilder: fluent API for multi-column keyset pagination over Dapper / IDbConnection");
    }
}

#pragma warning disable S3459 // "Remove unassigned auto-property" — Dapper assigns via reflection
#pragma warning disable S1144 // "Remove unused private set accessor" — Dapper requires set accessor
internal sealed class DapperProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public override string ToString() => $"[{Id}] {Name} @ {Price:C}";
}
#pragma warning restore S1144
#pragma warning restore S3459
