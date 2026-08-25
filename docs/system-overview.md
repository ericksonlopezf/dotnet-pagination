# System Overview

## Purpose

`EricksonLopez.Pagination` is a high-performance, zero-allocation .NET pagination library for EF Core, Dapper, MongoDB, Cosmos DB, gRPC, and Blazor. It provides **both offset and keyset (cursor) pagination**, a filter/sort DSL, Native AOT support, HMAC-signed tamper-proof cursors, Roslyn analyzers, and a headless Blazor component — all from a single consistent API.

## Problem Statement

Traditional pagination relies on `OFFSET/FETCH`. As users navigate to deep pages, the database must sequentially read and discard all preceding rows — O(N) latency that degrades linearly with page depth. Additionally, executing a `COUNT(*)` for every page request causes full table scans on large tables.

`EricksonLopez.Pagination` solves these issues by:

1. **Count-less Offset** — `ToPagedListWithoutCountAsync()` evaluates `HasNextPage` by fetching `PageSize + 1` records, eliminating the `COUNT(*)` query entirely.
2. **Keyset (Cursor) Pagination** — `KeysetBuilder<T>` generates `WHERE (col1, col2) > (@c1, @c2)` predicates that leverage B-Tree index seeks (O(log N)) regardless of page depth. **Requires a database index on the keyset column(s)**; without one the database falls back to a sequential scan.
3. **HMAC-Signed Cursors** — `HmacCursorEncoder` cryptographically signs cursors with HMAC-SHA256 + optional TTL, preventing client tampering and replay attacks.
4. **Zero-Allocation** — Core types reduce GC pressure via `Span<T>`, `stackalloc`, `IAsyncEnumerable<T>`, and struct-based pagination parameters.
5. **Native AOT** — `Abstractions`, `Core`, `AspNetCore`, `Blazor`, and `Grpc` are fully AOT-compatible. `SourceGenerators` provides a Roslyn source generator that registers cursor decoders at compile time.

## Package Ecosystem

Install only what you need:

| Package | Purpose | Target Frameworks |
|---|---|---|
| `EricksonLopez.Pagination.Abstractions` | Interfaces and contracts (`IPagedList<T>`, `ICursorPagedList<T>`) — no runtime dependencies | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination` | Core implementations: `PagedList<T>`, `CursorPagedList<T>`, encoders, registry, DI registration | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.EntityFrameworkCore` | `IQueryable<T>` extensions, `KeysetBuilder<T>`, filter/sort DSL | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.AspNetCore` | Model binders, `PaginationEndpointFilter`, `ToPagedResult()`, ETag headers, `AddPagination()` DI | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Dapper` | `IDbConnection` offset extensions + `DapperKeysetBuilder<T>` for multi-column keyset | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.MongoDB` | `IFindFluent<T>` offset + cursor extensions via MongoDB.Driver | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Cosmos` | Azure Cosmos DB forward cursor pagination via continuation tokens | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Blazor` | Headless `<PagedListPager>` Razor component (Bootstrap preset + custom template) | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Grpc` | Protobuf message ↔ parameter converters (`ToParameters()`, `ToMessage()`) | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.OpenApi` | Swashbuckle/OpenAPI operation filters and schema registration | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Redis` | Distributed cursor nonce replay store backed by StackExchange.Redis | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Relay` | GraphQL Relay Cursor Connections specification (`Connection<T>`, `Edge<T>`, `PageInfo`) | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Elasticsearch` | Elasticsearch 8.x `search_after` cursor pagination and index mapping | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.LinqToDB` | LinqToDB LINQ provider offset and keyset cursor pagination extensions | `net8.0;net9.0;net10.0` |
| `EricksonLopez.Pagination.Result` | Railway-Oriented Programming integration with `EricksonLopez.Result` | `net10.0` |
| `EricksonLopez.Pagination.SourceGenerators` | Roslyn source generator — emits `[ModuleInitializer]` to register cursor decoders for Native AOT | `netstandard2.0` |
| `EricksonLopez.Pagination.Analyzers` | Roslyn analyzers (`PAG001`–`PAG008`) — catches pagination bugs at compile time | `netstandard2.0` |

## Key Features

| Feature | Details |
|---|---|
| Offset pagination | `ToPagedListAsync()` — COUNT + SELECT; `ToPagedListWithoutCountAsync()` — single query |
| Keyset / cursor pagination | N-column, type-safe `KeysetBuilder<T>` fluent API; forward and backward |
| Filter DSL | `ApplyFilter(FilterParameters)` — `field~=value,age>=18` → LINQ Expression → SQL WHERE |
| Sort DSL | `ApplySort(SortParameters)` — `price desc, name` → OrderBy/ThenBy |
| HMAC-signed cursors | `HmacCursorEncoder` with configurable TTL and clock-skew tolerance |
| ETag / 304 Not Modified | `ToPagedResult(request)` generates deterministic ETags and returns 304 when unchanged |
| Batch processing | `ToPagedListBatchedAsync()` iterates all pages as `IAsyncEnumerable<IPagedList<T>>` |
| Native AOT | `Abstractions`, `Core`, `AspNetCore`, `Blazor`, `Grpc` are fully AOT-compatible |
| Roslyn analyzers | `PAG001` (unsorted `IQueryable`), `PAG008` (insecure Base64 encoder), `PAG007` (keyset > 5 cols), and `PAG002`–`PAG006` (keyset configuration diagnostics) |

## Performance Model

```
OFFSET pagination:  O(N) — latency grows linearly with page depth
Keyset pagination:  O(log N) — constant latency via B-Tree index seek (when indexed)
```

For datasets exceeding 100K rows, keyset pagination is strongly recommended. See [docs/benchmark.md](benchmark.md) for BenchmarkDotNet results at 100K rows in PostgreSQL (keyset: ~2.26 ms at any depth vs. offset: 14–35 ms at pages 100–500).

## Architecture

The library is structured in strict layers with no circular dependencies:

```
Abstractions (zero dependencies)
    └── Core (implements Abstractions)
            ├── AspNetCore   (extends Core, uses Microsoft.AspNetCore.App)
            ├── EntityFrameworkCore (extends Core, uses Microsoft.EntityFrameworkCore)
            ├── Dapper       (extends Core + Abstractions, uses Dapper.StrongName)
            ├── LinqToDB     (extends Core + Abstractions, uses linq2db)
            ├── MongoDB      (extends Core, uses MongoDB.Driver)
            ├── Cosmos       (extends Core, uses Microsoft.Azure.Cosmos)
            ├── Elasticsearch (extends Core, uses Elastic.Clients.Elasticsearch)
            ├── OpenApi      (extends Core, uses Swashbuckle.AspNetCore.SwaggerGen)
            ├── Relay        (extends Core + Abstractions)
            └── Result       (extends Core + Abstractions; net10.0 only)
Abstractions (zero dependencies)
    ├── Blazor (extends Abstractions only, uses Microsoft.AspNetCore.Components.Web)
    ├── Grpc   (extends Abstractions only, uses Google.Protobuf)
    └── Redis  (extends Abstractions only, uses StackExchange.Redis)
SourceGenerators (compile-time only, netstandard2.0)
Analyzers        (compile-time only, netstandard2.0)
```

For detailed component diagrams, flows, and sequence diagrams, see:
- [Architecture](architecture.md) — component diagram, offset and keyset flows, package dependency graph
- [Diagrams](diagrams.md) — all Mermaid diagrams (HMAC flow, filter DSL, cursor state machine, batch processing)
- [Functional Map](functional-map.md) — runtime interaction map by layer
- [NuGet Packages](nuget-packages.md) — compatibility matrix and AOT/trimming status
- [Cookbook](cookbook.md) — practical recipes for common scenarios
