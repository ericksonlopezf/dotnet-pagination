# API Reference: EricksonLopez.Pagination.AspNetCore

> Microsoft Learn-Style Reference for ASP.NET Core integrations, model binders, response DTOs, endpoint filters, and ETags.

---

## `PagedResponse<T>` (sealed class)

**Namespace:** `EricksonLopez.Pagination.AspNetCore`  
**Assembly:** `EricksonLopez.Pagination.AspNetCore.dll`

Standard REST response DTO wrapping offset pagination items with metadata and optional HATEOAS relative hypermedia links.

### Signature
```csharp
public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int? TotalPages { get; init; }
    public long? TotalCount { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
    public IDictionary<string, string>? Links { get; init; }
}
```

### Methods
- **`ToPagedResponse(this IPagedList<T> pagedList, HttpRequest? request = null)`**: Converts `IPagedList<T>` to `PagedResponse<T>`. If `request` is provided, generates HATEOAS links (`first`, `prev`, `next`, `last`).

---

## `CursorPagedResponse<T>` (sealed class)

**Namespace:** `EricksonLopez.Pagination.AspNetCore`  
**Assembly:** `EricksonLopez.Pagination.AspNetCore.dll`

Standard GraphQL Relay-compatible response DTO wrapping keyset items in `Edges` and `RelayPageInfo`.

### Signature
```csharp
public sealed class CursorPagedResponse<T>
{
    public IReadOnlyList<Edge<T>> Edges { get; init; }
    public RelayPageInfo PageInfo { get; init; }
    public long? TotalCount { get; init; }
}
```

### Helper Types
- **`Edge<T>`** (`readonly record struct`): Encapsulates `T Node` and `string Cursor`.
- **`RelayPageInfo`** (`class`): Encapsulates `string? StartCursor`, `string? EndCursor`, `bool HasNextPage`, `bool HasPreviousPage`.

---

## `PaginationExtensions` (static class)

**Namespace:** `EricksonLopez.Pagination.AspNetCore`  
**Assembly:** `EricksonLopez.Pagination.AspNetCore.dll`

Extension methods for transforming pagination collections into HTTP response models.

### Methods

#### `ToPagedResponse<T>(this IPagedList<T> source, HttpRequest? request = null)`
- **Return:** `PagedResponse<T>`

#### `ToCursorPagedResponse<T, TKey>(this ICursorPagedList<T> source, Func<T, TKey> cursorSelector, ICursorEncoder? cursorEncoder = null)`
Generates Relay `CursorPagedResponse<T>` by mapping each element's key to an opaque cursor.
- **Parameters:**
  - `Func<T, TKey> cursorSelector`: Key projection function (e.g. `p => p.Id`).
  - `ICursorEncoder? cursorEncoder`: Custom or DI cursor encoder.
- **Return:** `CursorPagedResponse<T>`

#### `ToCursorPagedResponse<T>(this ICursorPagedList<T> source, Func<T, string> cursorSelector)`
Overload accepting a string-returning key selector (e.g. composite string `p => $"{p.CreatedAt:O}|{p.Id}"`).

---

## `PaginationResultExtensions` (static class)

**Namespace:** `EricksonLopez.Pagination.AspNetCore`  
**Assembly:** `EricksonLopez.Pagination.AspNetCore.dll`

Extensions producing Minimal API `IResult` instances with deterministic SHA-256 ETag headers and HTTP 304 Not Modified caching.

### Methods

#### `ToPagedResult<T>(this IPagedList<T> source, HttpRequest request, TimeSpan? maxAge = null, PaginationETagOptions? options = null)`
- **Behavior:**
  1. Computes SHA-256 hash over page elements.
  2. Compares against incoming `If-None-Match` header.
  3. Returns `Results.StatusCode(304)` if matched (empty body, 0 serialization).
  4. Returns `Results.Ok(response)` with `ETag` and `Cache-Control` headers if not matched.
- **Return:** `IResult`

---

## `PaginationEndpointFilter` & `PaginationEndpointExtensions`

**Namespace:** `EricksonLopez.Pagination.AspNetCore`  
**Assembly:** `EricksonLopez.Pagination.AspNetCore.dll`

Endpoint filters intercepting Minimal API requests to enforce `MaxPageSize` constraints before hitting database queries.

### Extension Method
```csharp
public static RouteHandlerBuilder AddPaginationValidation(this RouteHandlerBuilder builder)
public static RouteGroupBuilder AddPaginationValidation(this RouteGroupBuilder builder)
```
- **Behavior:** Inspects route parameters for `PaginationParameters` or `CursorPaginationParameters`. If requested size exceeds `MaxPageSize`, immediately returns HTTP 400 Bad Request with RFC 7807 `PaginationErrorResponse`.

---

## `PaginationExceptionHandler` (sealed class)

**Namespace:** `EricksonLopez.Pagination.AspNetCore`  
**Assembly:** `EricksonLopez.Pagination.AspNetCore.dll`  
**Implements:** `Microsoft.AspNetCore.Diagnostics.IExceptionHandler`

Native .NET 8+ global exception handler.

### Behavior
- Intercepts `InvalidPaginationCursorException`, `ExpiredPaginationCursorException`, and `ReplayedPaginationCursorException`.
- Formats response as standard RFC 7807 `ProblemDetails` with HTTP 400 status.
- Registers in DI: `builder.Services.AddExceptionHandler<PaginationExceptionHandler>();`

---

## `PaginationServiceCollectionExtensions` (static class)

**Namespace:** `EricksonLopez.Pagination.AspNetCore`  
**Assembly:** `EricksonLopez.Pagination.AspNetCore.dll`

### Methods

#### `AddPagination(this IServiceCollection services, Action<PaginationCoreOptions>? configure = null)`
Registers core options, `ICursorEncoder` (`HmacCursorEncoder`), factories, and model binders in DI.
- **Signature:**
```csharp
public static IServiceCollection AddPagination(
    this IServiceCollection services,
    Action<PaginationCoreOptions>? configure = null)
```
