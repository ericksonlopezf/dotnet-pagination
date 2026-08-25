# Level 10 — Enterprise Architecture and Observability

> **Implementation Reference:** [`Level10EnterpriseArchitecture.cs`](../../DemoApp/DemoApp.Api/Levels/Level10EnterpriseArchitecture.cs)  
> **API Base Route:** `/api/level10`

---

## 1. Deterministic ETag Headers and HTTP 304 Not Modified

`ToPagedResult` returns an `IResult` that computes a deterministic SHA-256 ETag based on page content:

```csharp
group.MapGet("/products/etag", async (
    [AsParameters] PaginationParameters pagination,
    HttpContext httpContext,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return pagedList.ToPagedResult(httpContext.Request, maxAge: TimeSpan.FromMinutes(5));
});
```

### ETag Execution Flow
1. **Initial Request**: Server returns `200 OK` with headers `ETag: "W/\"a8f4c2...\""` and `Cache-Control: public, max-age=300`.
2. **Subsequent Request**: Client sends conditional header `If-None-Match: "W/\"a8f4c2...\""`.
3. **Optimized Short-Circuit**: Server evaluates matching hash and returns `304 Not Modified` with an empty body, conserving bandwidth and serialization CPU cycles.

---

## 2. Server-Side Response Caching (`OutputCaching`)

Combine ASP.NET Core `OutputCaching` with `VaryByQuery` to cache heavily requested pages in memory:

```csharp
group.MapGet("/products/cached", async (
    [AsParameters] PaginationParameters pagination,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
})
.CacheOutput(c => c
    .SetVaryByQuery("page", "pageSize")
    .Expire(TimeSpan.FromMinutes(5)));
```

---

## 3. Granular Endpoint Page Size Bounds (`maxPageSize`)

Override global options on specific sensitive or dense endpoints:

```csharp
var pagedList = await db.Products
    .OrderBy(p => p.Id)
    .ToPagedListAsync(pagination, maxPageSize: 50, cancellationToken: ct);
```

---

## 4. OpenTelemetry Observability and Metrics (`PaginationMetrics`)

The library emits standard OpenTelemetry instruments and metrics:
- **`pagination.offset.queries`**: Counter tracking total offset queries executed.
- **`pagination.keyset.queries`**: Counter tracking total keyset queries executed.
- **`pagination.query.duration`**: Histogram tracking pagination query execution latency.
- **`pagination.cursor.errors`**: Counter tracking cursor tampering, validation, or expiration errors.

Metrics are emitted via `System.Diagnostics.Metrics` under the meter name `"EricksonLopez.Pagination"`.
