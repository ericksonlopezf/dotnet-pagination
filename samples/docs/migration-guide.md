# Migration Guide — EricksonLopez.Pagination

> Guide for updating applications to modern `EricksonLopez.Pagination` API standards.

---

## 1. Migrating to Multi-Column KeysetBuilder

### Deprecated API (Single-Key Overload)
In early pre-releases, keyset pagination was invoked via a single key selector:
```csharp
// DEPRECATED — emits compiler warning CS0618
var page = await db.Products
    .ToCursorPagedListAsync(p => p.Id, parameters);
```

### Modern KeysetBuilder API
Migrate to the type-safe fluent `KeysetBuilder<T>`:
```csharp
// MODERN & TYPE-SAFE
var page = await db.Products
    .Keyset(parameters)
    .Ascending(p => p.Id)
    .ToCursorPagedListAsync(cancellationToken: ct);
```

### Multi-Column Keyset with Deterministic Tie-Breaker
```csharp
var page = await db.Products
    .Keyset(parameters)
    .Descending(p => p.CreatedAt) // Primary sort
    .Ascending(p => p.Id)         // Strictly unique tie-breaker
    .ToCursorPagedListAsync(cancellationToken: ct);
```

---

## 2. Migrating Exception Handling to `PaginationExceptionHandler`

### Legacy Manual Middleware
```csharp
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (InvalidPaginationCursorException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});
```

### Modern .NET 8+ `IExceptionHandler`
Register `PaginationExceptionHandler` natively:
```csharp
builder.Services.AddExceptionHandler<PaginationExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
```
This automatically translates `InvalidPaginationCursorException`, `ExpiredPaginationCursorException`, and `ReplayedPaginationCursorException` into RFC 7807 `ProblemDetails`.

---

## 3. Migrating from `X.PagedList` to `EricksonLopez.Pagination`

| `X.PagedList` Pattern | `EricksonLopez.Pagination` Equivalent |
|---|---|
| `query.ToPagedList(pageNumber, pageSize)` | `await query.OrderBy(p => p.Id).ToPagedListAsync(pagination, cancellationToken: ct)` |
| `IPagedList<T>` | `IPagedList<T>` (in `EricksonLopez.Pagination.Abstractions`) |
| `pagedList.PageCount` | `pagedList.TotalPages` |
| `pagedList.TotalItemCount` | `pagedList.TotalCount` (or `counted.ExactTotalCount`) |
| (Not Supported) | Multi-column Keyset seeking ($O(\log N)$) |
| (Not Supported) | HMAC-SHA256 Signed Cursors |
| (Not Supported) | Native AOT compatible JSON Serialization Context |
