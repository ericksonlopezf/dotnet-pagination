# Level 7 — Scalability and Micro-ORMs (Dapper)

> **Implementation Reference:** [`Level7Scalability.cs`](../../DemoApp/DemoApp.Api/Levels/Level7Scalability.cs)  
> **API Base Route:** `/api/level7`

---

## 1. Offset Pagination with Dapper (`IDbConnection`)

`EricksonLopez.Pagination.Dapper` extends `IDbConnection` to execute high-performance pagination with native SQL queries.

### 1.1 With Exact Total Count (`countTotal: true` — Multiple Result Sets)
The SQL query must return two result sets: the count and the page data.

```csharp
var pagedList = await connection.ToPagedListAsync<DapperProductDto>(
    sql: @"
        SELECT COUNT(*) FROM Product;
        SELECT * FROM Product ORDER BY Id
        LIMIT @__Pagination_PageSize__ OFFSET @__Pagination_Skip__",
    parameters: pagination,
    countTotal: true);
```

> [!IMPORTANT]
> The parameters `@__Pagination_Skip__` and `@__Pagination_PageSize__` are injected automatically by the library. They should not be passed manually.

### 1.2 Without Count (`countTotal: false` — Single Result Set)
```csharp
var pagedList = await connection.ToPagedListAsync<DapperProductDto>(
    sql: "SELECT * FROM Product ORDER BY Id LIMIT @__Pagination_Limit__ OFFSET @__Pagination_Skip__",
    parameters: pagination,
    countTotal: false);
```
- `@__Pagination_Limit__` is injected as `PageSize + 1` to determine `HasNextPage` in a single database round-trip.

---

## 2. Multi-Column Keyset in Dapper: `DapperKeysetBuilder<T>`

For high-throughput endpoints using raw SQL without EF Core:

```csharp
var page = await new DapperKeysetBuilder<ProductDto>(connection, cursor)
    .Select("Id, Name, Price")
    .From("Product")
    .OrderBy("Price", SortDirection.Ascending)
    .ThenBy("Id", SortDirection.Ascending)
    .WithCursorColumns(p => p.Price.ToString(), p => p.Id.ToString())
    .WithCursorDecoder(
        parts => decimal.Parse(parts[0]),
        parts => int.Parse(parts[1]))
    .ExecuteAsync(cancellationToken: ct);
```

### Features of `DapperKeysetBuilder<T>`
- Dynamically constructs multi-column SQL `WHERE (Price > @p1) OR (Price = @p1 AND Id > @p2)` clauses matching the target database dialect.
- Compatible with multiple database dialects (`DatabaseDialect.SqlServer`, `PostgreSql`, `MySql`, `Sqlite`, `Oracle`).
- Free of EF Core dependencies and runtime reflection overhead.
