# Getting Started Guide — EricksonLopez.Pagination

> Comprehensive guide for integrating `EricksonLopez.Pagination` into modern .NET applications.

---

## 1. Choosing the Right Pagination Strategy

Before writing code, choose the strategy that fits your dataset size and UI requirements:

| Dimension | Offset Pagination | Keyset (Cursor) Pagination |
|---|---|---|
| **SQL Operation** | `OFFSET n ROWS FETCH NEXT m ROWS` | `WHERE Key > @LastKey ORDER BY Key LIMIT m` |
| **Complexity** | $O(N)$ database scan | $O(\log N)$ B-Tree index seek |
| **Random Page Access** | ✅ Yes (jump to page 50) | ❌ No (sequential navigation only) |
| **Immunity to Data Drift** | ❌ No (inserts/deletes shift rows) | ✅ Yes (anchor maintains position) |
| **Dataset Size** | Small to medium (<100k rows) | Large to massive (>100k rows) |
| **UI Type** | Admin tables, data grids, reports | Mobile feeds, infinite scroll, public APIs |

---

## 2. Offset Pagination Deep Dive

### Exact Count vs. Countless Mode

By default, `ToPagedListAsync(pagination, countTotal: true)` executes two queries:
1. `SELECT COUNT(*)` to calculate `TotalCount` and `TotalPages`.
2. `SELECT ... OFFSET ... LIMIT` to fetch page records.

```csharp
// Returns CountedPagedList<T> with ExactTotalCount
var paged = await db.Products
    .OrderBy(p => p.Id)
    .ToPagedListAsync(pagination, countTotal: true, cancellationToken: ct);
```

If your UI only needs "Next" and "Previous" buttons (no page numbers), turn off `countTotal` for massive throughput improvements:

```csharp
// Executes a single query fetching (pageSize + 1) rows to determine HasNextPage
var fastPaged = await db.Products
    .OrderBy(p => p.Id)
    .ToPagedListAsync(pagination, countTotal: false, cancellationToken: ct);
```

---

## 3. Keyset Pagination Deep Dive

### Multi-Column Keyset with Unique Tie-Breaker

To paginate over columns with duplicate values (e.g. `CreatedAt` or `Price`), always chain a strictly unique column (e.g. `Id`) as the tie-breaker:

```csharp
var page = await db.Products
    .Keyset(cursor)
    .Descending(p => p.CreatedAt) // Primary order
    .Ascending(p => p.Id)         // Mandatory tie-breaker
    .ToCursorPagedListAsync(cancellationToken: ct);

return Results.Ok(page.ToCursorPagedResponse(p => $"{p.CreatedAt:O}|{p.Id}"));
```

> [!IMPORTANT]
> Ensure a composite database index exists matching the exact order:  
> `CREATE INDEX IX_Products_CreatedAt_Id ON Products(CreatedAt DESC, Id ASC);`

---

## 4. Dynamic Filtering and Sorting

Add dynamic filtering and sorting capabilities to any endpoint securely without exposing raw SQL or unvalidated column names:

```csharp
app.MapGet("/api/products/search", async (
    [AsParameters] PaginationParameters pagination,
    [AsParameters] FilterParameters filter,
    [AsParameters] SortParameters sort,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .ApplyFilter(filter)
        .ApplySort(sort, defaultSort: p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});
```

### Client Filter Query Syntax
- **Equals**: `?filter=category=Electronics`
- **Range**: `?filter=price>=50,price<=200`
- **Substring**: `?filter=name~=Pro`
- **Multiple Conditions (AND)**: `?filter=category=Electronics,price>=50`
- **Disjunction (OR)**: `?filter=status=Active|status=Pending`

---

## 5. Security & Cryptographic Cursors

In production, encrypt and sign your cursors using `HmacCursorEncoder` to prevent client tampering:

```csharp
builder.Services.AddPagination(options =>
{
    options.DefaultPageSize = 20;
    options.MaxPageSize = 100;
    options.Cursor.Encoder = new HmacCursorEncoder(
        secretKey: builder.Configuration["Pagination:SecretKey"]!, // 32+ bytes
        timeToLive: TimeSpan.FromMinutes(30),
        clockSkewTolerance: TimeSpan.FromSeconds(30));
});
```

Tampered cursors immediately trigger `InvalidPaginationCursorException`, which is caught by `PaginationExceptionHandler` and translated into HTTP 400 ProblemDetails.
