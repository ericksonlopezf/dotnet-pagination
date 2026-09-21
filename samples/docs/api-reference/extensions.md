# API Reference: Extensions (Redis, Relay, Result, gRPC, Blazor, OpenAPI)

> Microsoft Learn-Style Reference for infrastructure extensions, distributed stores, protocol converters, and UI bindings.

---

## Redis: `EricksonLopez.Pagination.Redis`

### `RedisCursorReplayStore` (sealed class)
Distributed implementation of `ICursorReplayStore` using StackExchange.Redis.
- **Mechanism**: Executes atomic Redis `SET ... NX EX` commands to register nonces. If the key already exists, returns `false`, preventing cursor replay attacks across multi-instance Kubernetes clusters.

### `RedisPaginationServiceCollectionExtensions` (static class)
- **`AddRedisCursorReplayStore(this IServiceCollection services, Action<RedisCursorReplayStoreOptions> configure)`**: Registers distributed replay protection in DI.

---

## GraphQL Relay: `EricksonLopez.Pagination.Relay`

### `Connection<TNode>` (sealed record)
Strict implementation of GraphQL Relay Connection specification.
- **Properties:**
  - `IReadOnlyList<Edge<TNode>> Edges { get; init; }`
  - `PageInfo PageInfo { get; init; }`
  - `long? TotalCount { get; init; }`

### `Edge<TNode>` (sealed record)
- `TNode Node { get; init; }`
- `string Cursor { get; init; }`

### `PageInfo` (sealed record)
- `string? StartCursor { get; init; }`
- `string? EndCursor { get; init; }`
- `bool HasNextPage { get; init; }`
- `bool HasPreviousPage { get; init; }`

### `RelayPaginationExtensions` (static class)
- **`ToConnection<TNode>(this ICursorPagedList<TNode> pagedList, Func<TNode, string> cursorSelector)`**: Converts an `ICursorPagedList<TNode>` to a Relay `Connection<TNode>`.

---

## Functional Result: `EricksonLopez.Pagination.Result`

### `PaginationErrors` (static class)
Standardized functional domain error definitions conforming to `EricksonLopez.Result`:
- `PaginationErrors.InvalidCursor(string details)`
- `PaginationErrors.ExpiredCursor(DateTimeOffset expiredAt)`
- `PaginationErrors.ReplayedCursor(string nonce)`

### `PaginationResultExtensions` (static class)
- **`ToPagedListResultAsync<T>`**: Wraps pagination queries in a `Result<PagedResponse<T>>`, automatically intercepting exceptions and mapping them to domain errors without throwing.

---

## gRPC: `EricksonLopez.Pagination.Grpc`

### `PaginationGrpcExtensions` (static class)
- **`WritePaginationHeaders(this Metadata metadata, IPagedList pagedList)`**: Writes `x-pagination-page`, `x-pagination-pagesize`, `x-pagination-totalcount` to gRPC metadata trailers.
- **`ReadPaginationHeaders(this Metadata metadata)`**: Reconstructs pagination metadata from incoming gRPC response headers.

---

## Blazor: `EricksonLopez.Pagination.Blazor`

### `PaginationUIOptions` (class)
Configures CSS class names, button labels, and page-link thresholds for Blazor pagination UI components.

### `PaginationBlazorServiceCollectionExtensions` (static class)
- **`AddPaginationBlazor(this IServiceCollection services, Action<PaginationUIOptions>? configure = null)`**: Registers Blazor pagination UI services in DI.

---

## OpenAPI: `EricksonLopez.Pagination.OpenApi`

### `PaginationOperationFilter` (class)
Swashbuckle operation filter injecting `page`, `pageSize`, `first`, `after`, `filter`, and `sortBy` query parameter documentation into Swagger UI.

### `PaginationOperationTransformer` (class)
Modern ASP.NET Core 9+ `IOpenApiOperationTransformer` for native OpenAPI document generation without Swashbuckle dependencies.

### `PaginationSwaggerGenOptionsExtensions` & `PaginationOpenApiOptionsExtensions`
Extension methods registering pagination parameter schemas into SwaggerGen and Microsoft.AspNetCore.OpenApi configurations.
