# Level 3 — Real Use Cases and Keyset Pagination

> **Implementation Reference:** [`Level3RealUseCases.cs`](../../DemoApp/DemoApp.Api/Levels/Level3RealUseCases.cs)  
> **API Base Route:** `/api/level3`

---

## 1. Basic Keyset Pagination ($O(\log N)$)

Cursor-based keyset pagination is the recommended strategy for datasets with >100,000 records:

```csharp
group.MapGet("/products/keyset", async (
    [AsParameters] CursorPaginationParameters cursor,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var page = await db.Products
        .Keyset(cursor)
        .Ascending(p => p.Id)
        .ToCursorPagedListAsync(cancellationToken: ct);

    return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
});
```

### Key Features of `KeysetBuilder<T>`
- **`Ascending(p => p.Id)`**: Defines the seek and sort column.
- **`ToCursorPagedListAsync`**: Executes the SQL query using a `WHERE Id > @cursor` seek predicate and `LIMIT (pageSize + 1)`.
- **`ToCursorPagedResponse(p => p.Id)`**: Type-safe projection that serializes items and computes `startCursor`, `endCursor`, `hasNextPage`, and `hasPreviousPage`.

---

## 2. Offset Pagination with `ICountedPagedList` and `ExactTotalCount`

When `countTotal: true` (the default behavior), the result implements `ICountedPagedList<T>`, exposing `ExactTotalCount` as a non-null `long`:

```csharp
var pagedList = await db.Products
    .OrderBy(p => p.Id)
    .ToPagedListAsync(pagination, countTotal: true, cancellationToken: ct);

if (pagedList is ICountedPagedList counted)
{
    long exactCount = counted.ExactTotalCount;
}
```

---

## 3. Countless Fast-Path Mode (`countTotal: false`)

For infinite scroll UIs or large tables where `COUNT(*)` is prohibitively expensive:

```csharp
var pagedList = await db.Products
    .OrderBy(p => p.Id)
    .ToPagedListAsync(pagination, countTotal: false, cancellationToken: ct);
```
- Executes a single query with `Take(pageSize + 1)` to evaluate `HasNextPage`.
- `TotalCount` and `TotalPages` are returned as `null`.

---

## 4. Collection Projections: `Map()` and `LazyMap()`

### `PagedList.Map()` (Immediate Projection)
Transforms entities to DTOs while preserving all pagination metadata:
```csharp
var mapped = pagedList.Map(p => new ProductDto(p.Id, p.Name, p.Price));
```

### `PagedList.LazyMap()` (Deferred Projection)
Avoids intermediate allocations; the selector runs on-demand during JSON serialization:
```csharp
var lazy = pagedList.LazyMap(p => new ProductDto(p.Id, p.Name, p.Price));
```

### `ICursorPagedList<T>.Map()` (Cursor Projection)
Maps entities while preserving opaque cursors and navigation flags:
```csharp
ICursorPagedList<ProductDto> mappedCursor = page.Map(p => new ProductDto(p.Id, p.Name, p.Price));
```

---

## 5. Manual Factory Methods

When executing custom queries manually (e.g., raw SQL or stored procedures):

```csharp
// With known total count
var withCount = PagedList<Product>.WithCount(items, pagination, totalCount: 150);

// Without count (specifying explicit hasNextPage)
var withoutCount = PagedList<Product>.WithoutCount(items, pagination, hasNextPage: true);

// Empty page (see Section 6 for the complete pattern)
var empty = PagedList<Product>.Empty(pagination);

// Manual cursor list
var cursorList = CursorPagedList<Product>.Create(items, startCursor, endCursor, hasPreviousPage: false, hasNextPage: true);
```

---

## 6. Empty Paged List: `PagedList<T>.Empty()` — Early Return Without DB Query

`PagedList<T>.Empty(params)` creates an `ICountedPagedList<T>` with zero items and `ExactTotalCount = 0`, without executing any database query:

```csharp
// GET /api/level3/products/empty-page?requiredFilter=...
// Omit requiredFilter to see Empty() in action

if (string.IsNullOrWhiteSpace(requiredFilter))
{
    // Early return: never touches the database
    var emptyResult = PagedList<Product>.Empty(pagination);
    return Results.Ok(emptyResult.ToPagedResponse());
    // Response: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNextPage: false }
}

var pagedList = await db.Products
    .OrderBy(p => p.Id)
    .ToPagedListAsync(pagination, cancellationToken: ct);

return Results.Ok(pagedList.ToPagedResponse());
```

### When to Use `Empty()` vs `WithCount([], params, 0)`

| Factory | Intent | `ExactTotalCount` |
|---|---|---|
| `Empty(params)` | Semantic early-return; dataset is known to be empty without querying | `0` (via `ICountedPagedList`) |
| `WithCount([], params, 0)` | Manual construction with explicit count, even if zero | `0` (any value provided) |
| `WithoutCount([], params, false)` | No count available; `HasNextPage = false` | `null` |

> [!TIP]
> Use `Empty()` when business logic determines the response is empty before reaching the database (e.g., missing required filters, tenant not found, feature flag disabled).
