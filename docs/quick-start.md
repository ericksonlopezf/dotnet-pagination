# Quick Start Guide — EricksonLopez.Pagination

> Get up and running with enterprise-grade pagination in less than 5 minutes.

---

## 1. Installation

Install the required NuGet packages depending on your stack:

```bash
# Core abstractions and ASP.NET Core integration
dotnet add package EricksonLopez.Pagination.AspNetCore

# Entity Framework Core integration
dotnet add package EricksonLopez.Pagination.EntityFrameworkCore

# (Optional) Roslyn Analyzers for compile-time safety
dotnet add package EricksonLopez.Pagination.Analyzers
```

---

## 2. Dependency Injection Registration

In `Program.cs`, register the pagination services:

```csharp
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register pagination with sensible defaults
builder.Services.AddPagination(options =>
{
    options.DefaultPageSize = 20;
    options.MaxPageSize = 100;
    options.DeepOffsetWarningThreshold = 500;
});

// (Recommended) Register global cursor exception handling
builder.Services.AddExceptionHandler<PaginationExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
```

---

## 3. Your First Paginated Minimal API

### Offset Pagination (Standard)

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
        .OrderBy(p => p.Id) // Deterministic ordering required
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
})
.AddPaginationValidation(); // Automatically enforces MaxPageSize
```

### Keyset (Cursor) Pagination ($O(\log N)$ High-Throughput)

```csharp
app.MapGet("/api/feed", async (
    [AsParameters] CursorPaginationParameters cursor,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var page = await db.Products
        .Keyset(cursor)
        .Descending(p => p.CreatedAt)
        .Ascending(p => p.Id) // Unique tie-breaker column
        .ToCursorPagedListAsync(cancellationToken: ct);

    return Results.Ok(page.ToCursorPagedResponse(p => $"{p.CreatedAt:O}|{p.Id}"));
});
```

---

## 4. Testing Your Endpoint

### Request
```http
GET /api/products?page=1&pageSize=5 HTTP/1.1
Host: localhost:5000
```

### Response
```json
{
  "items": [
    { "id": 1, "name": "Mechanical Keyboard", "price": 120.00 },
    { "id": 2, "name": "Wireless Mouse", "price": 45.50 },
    { "id": 3, "name": "USB-C Hub", "price": 35.00 },
    { "id": 4, "name": "UltraWide Monitor", "price": 450.00 },
    { "id": 5, "name": "Desk Mat", "price": 20.00 }
  ],
  "page": 1,
  "pageSize": 5,
  "totalPages": 20,
  "totalCount": 100,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```
