# Level 4 — Advanced Integration (CQRS, HATEOAS, Endpoint Filters)

> **Implementation Reference:** [`Level4AdvancedIntegration.cs`](../../DemoApp/DemoApp.Api/Levels/Level4AdvancedIntegration.cs)  
> **API Base Route:** `/api/level4`

---

## 1. CQRS and MediatR Integration

In Clean Architecture or Domain-Driven Design, the API layer delegates query execution to the application layer:

```csharp
group.MapGet("/products/cqrs", async (
    [AsParameters] PaginationParameters pagination,
    string? searchTerm,
    IMediator mediator) =>
{
    var query = new GetProductsQuery(pagination, searchTerm);
    var pagedResult = await mediator.Send(query);

    return Results.Ok(pagedResult.ToPagedResponse());
});
```

### Application Query Handler
```csharp
public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, IPagedList<Product>>
{
    private readonly ApplicationDbContext _db;

    public GetProductsHandler(ApplicationDbContext db) => _db = db;

    public async Task<IPagedList<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Products
            .OrderBy(p => p.Id)
            .ToPagedListAsync(request.Pagination, cancellationToken: cancellationToken);
    }
}
```

---

## 2. Relative HATEOAS Links (`ToPagedResponse(HttpRequest)`)

By passing the `HttpRequest` instance to `ToPagedResponse`, the library automatically calculates relative `nextPageUrl` and `previousPageUrl` hyperlinks:

```csharp
group.MapGet("/products/hateoas", async (
    [AsParameters] PaginationParameters pagination,
    HttpRequest request,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse(request));
});
```

### Response with HATEOAS
```json
{
  "items": [...],
  "page": 2,
  "pageSize": 20,
  "totalPages": 50,
  "totalCount": 1000,
  "hasNextPage": true,
  "hasPreviousPage": true,
  "nextPageUrl": "/api/level4/products/hateoas?page=3&pageSize=20",
  "previousPageUrl": "/api/level4/products/hateoas?page=1&pageSize=20"
}
```

---

## 3. Endpoint Validation with `AddPaginationValidation()`

`AddPaginationValidation()` attaches the `PaginationEndpointFilter` to an endpoint or group, validating that `PageSize` does not exceed `PaginationCoreOptions.MaxPageSize`:

```csharp
group.MapGet("/products/validated", async (
    [AsParameters] PaginationParameters pagination,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
})
.AddPaginationValidation(); // Automatically returns HTTP 400 Bad Request if MaxPageSize is exceeded
```

---

## 4. Server-Side SQL Projection

To optimize network bandwidth between the database engine and the application, project directly in SQL:

```csharp
var pagedDtos = await db.Products
    .OrderBy(p => p.Id)
    .ToPagedListAsync(
        p => new { p.Id, p.Name, p.Price }, // Translated directly to SQL SELECT Id, Name, Price
        pagination,
        cancellationToken: ct);
```
