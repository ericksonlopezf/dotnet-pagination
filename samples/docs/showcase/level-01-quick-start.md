# Level 1 — Quick Start

> **Implementation Reference:** [`Level1QuickStart.cs`](../../DemoApp/DemoApp.Api/Levels/Level1QuickStart.cs)  
> **API Base Route:** `/api/level1`

---

## 1. Minimal Setup in `Program.cs`

To start using `EricksonLopez.Pagination` in an ASP.NET Core application:

```csharp
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register pagination services
builder.Services.AddPagination(options =>
{
    options.DefaultPageSize = 20;
    options.MaxPageSize = 100;
});

var app = builder.Build();
```

`AddPagination` registers the following components in the Dependency Injection container:
- `PaginationCoreOptions` with DataAnnotations startup validation.
- `ICursorEncoder` (`HmacCursorEncoder` with development default key).
- `IPagedListFactory` and `ICursorPagedListFactory`.
- `PaginationParametersModelBinderProvider` for traditional MVC controllers.

---

## 2. Basic Offset Pagination Endpoint

The simplest usage in Minimal APIs requires only two chained calls:

```csharp
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using EricksonLopez.Pagination.EntityFrameworkCore;

app.MapGet("/api/products", async (
    [AsParameters] PaginationParameters pagination,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});
```

### Key API Elements
- **`[AsParameters] PaginationParameters`**: Record struct automatically binding `page` (default: 1) and `pageSize` (default: 20) from query parameters via `IParsable<T>`.
- **`.OrderBy(p => p.Id)`**: Mandatory deterministic sort. Roslyn analyzer `PAG001` emits a build warning/error if `ToPagedListAsync` is called without explicit sorting.
- **`ToPagedListAsync(pagination, cancellationToken)`**: Asynchronous extension method on `IQueryable<T>` executing total count and paged item retrieval.
- **`ToPagedResponse()`**: Serializes the result into a standardized response DTO with structured metadata (`Page`, `PageSize`, `TotalPages`, `TotalCount`, `HasNextPage`, `HasPreviousPage`).

---

## 3. Request and Response Example

### HTTP Request
```http
GET /api/level1/products?page=2&pageSize=5 HTTP/1.1
Host: localhost:5000
```

### JSON Response
```json
{
  "items": [
    { "id": 6, "name": "Product 0006 — Clothing", "price": 124.50 },
    { "id": 7, "name": "Product 0007 — Books", "price": 45.99 },
    { "id": 8, "name": "Product 0008 — Home", "price": 89.00 },
    { "id": 9, "name": "Product 0009 — Sports", "price": 15.20 },
    { "id": 10, "name": "Product 0010 — Electronics", "price": 340.00 }
  ],
  "page": 2,
  "pageSize": 5,
  "totalPages": 200,
  "totalCount": 1000,
  "hasNextPage": true,
  "hasPreviousPage": true
}
```
