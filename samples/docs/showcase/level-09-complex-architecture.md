# Level 9 — Complex Architecture and Provider Extensions

> **Implementation Reference:** [`Level9Extensions.cs`](../../DemoApp/DemoApp.Api/Levels/Level9Extensions.cs)  
> **API Base Route:** `/api/level9`

---

## 1. Multi-Column Keyset with Unique Tie-Breaker

In datasets sorted by non-unique columns (such as `CreatedAt`, `Price`, or `Category`), it is essential to chain a strictly unique column (such as `Id`) at the end of the ordering hierarchy to preserve deterministic page boundaries:

```csharp
var page = await db.Products
    .Keyset(cursor)
    .Descending(p => p.Price)
    .Ascending(p => p.Id) // Mandatory unique tie-breaker column
    .ToCursorPagedListAsync(cancellationToken: ct);
```

> [!IMPORTANT]
> Roslyn analyzer `PAG003` verifies at compile-time that the trailing column in a keyset query is unique. Without a unique column, rows with duplicate values across page boundaries will be duplicated or skipped.

---

## 2. Backward Navigation (Backward Pagination)

To navigate toward previous pages using cursor pagination:

```csharp
// Request: GET /api/level9/products/backward?last=10&before={endCursor}
var page = await db.Products
    .Keyset(cursor)
    .Ascending(p => p.Id)
    .ToCursorPagedListAsync(cancellationToken: ct);
```

`KeysetBuilder<T>` automatically reverses the `WHERE` seek operators and SQL sort directions to evaluate the preceding block in $O(\log N)$ before returning the results in natural order.

---

## 3. Parallel Keyset Table Partitioning (`SplitKeysetPartitionsAsync`)

To partition massive tables (e.g., millions of records) across concurrent background workers:

```csharp
var partitions = await db.Products
    .SplitKeysetPartitionsAsync(
        keySelector: p => p.Id,
        partitionCount: 4,
        cancellationToken: ct);

// Each partition contains disjoint boundaries [LowerBound, UpperBound]
Parallel.ForEach(partitions, async partition =>
{
    using var scope = serviceProvider.CreateScope();
    var localDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var partitionQuery = localDb.Products
        .Where(p => p.Id >= partition.LowerBound && p.Id <= partition.UpperBound);

    // Concurrently process partition in isolation
});
```

---

## 4. PostgreSQL Approximate Counting

For massive tables in PostgreSQL where exact `COUNT(*)` creates table scan contention:

```csharp
// Queries pg_class.reltuples for an instantaneous O(1) estimate
var approximateCount = await db.Products.CountApproximatePostgreSqlAsync(ct);
```

---

## 5. `CursorPaginationParameters.GetPageSize(defaultSize)` — Resolving Effective Page Size

`GetPageSize(int defaultSize = 10)` resolves the effective page size from `First`/`Last` with a configurable fallback:

```csharp
// GET /api/level9/products/cursor-defaults?first=25 → GetPageSize(10) = 25
// GET /api/level9/products/cursor-defaults?last=50  → GetPageSize(10) = 50
// GET /api/level9/products/cursor-defaults          → GetPageSize(10) = 10 (fallback)

group.MapGet("/products/cursor-defaults", ([AsParameters] CursorPaginationParameters cursor) =>
{
    int effectivePageSize = cursor.GetPageSize(defaultSize: 10);

    return Results.Ok(new
    {
        cursor.First,
        cursor.After,
        cursor.Last,
        cursor.Before,
        GetPageSizeResult = effectivePageSize
    });
});
```

### Resolution Logic

| Condition | Result |
|---|---|
| `cursor.First > 0` | Returns `First` (forward pagination) |
| `cursor.Last  > 0` | Returns `Last` (backward pagination) |
| Both are `null` or `0` | Returns `defaultSize` |

This method is used internally by all `KeysetBuilder<T>`, `DapperKeysetBuilder<T>`, `LinqToDB.KeysetBuilder<T>`, and MongoDB cursor extensions to determine how many items to request from the data source.

> [!TIP]
> When implementing custom data source adapters or third-party integrations, use `GetPageSize(defaultSize)` instead of reading `First ?? Last ?? defaultSize` manually to ensure consistent resolution behavior.
