# packages.md — EricksonLopez.Pagination
### NuGet Package Architecture · Consumer Guide · August 2026

---

## Install Only What You Need

```
EricksonLopez.Pagination.Abstractions   →  Domain/Application layer (zero deps)
EricksonLopez.Pagination                →  Core implementations
EricksonLopez.Pagination.EntityFrameworkCore  →  EF Core users
EricksonLopez.Pagination.Dapper         →  Dapper users
EricksonLopez.Pagination.MongoDB        →  MongoDB users
EricksonLopez.Pagination.Cosmos         →  Azure Cosmos DB users
EricksonLopez.Pagination.AspNetCore     →  REST API layer
EricksonLopez.Pagination.OpenApi        →  Swagger/OpenAPI docs
EricksonLopez.Pagination.Blazor         →  Blazor UI components
EricksonLopez.Pagination.Grpc           →  gRPC endpoints
EricksonLopez.Pagination.SourceGenerators  →  Native AOT publishing
EricksonLopez.Pagination.Analyzers      →  Compile-time safety
```

---

## Package Catalog

### EricksonLopez.Pagination.Abstractions

| Property | Value |
|---|---|
| Purpose | Interfaces and contracts with zero runtime dependencies |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | None |
| AOT | ✅ Fully AOT-safe |
| Consumers | Domain projects, application projects, any project that references pagination types without implementations |

**Public API:**
- `IPagedList<T>` — read-only paginated list contract
- `ICountedPagedList<T>` — paginated list with guaranteed non-null TotalCount
- `ICursorPagedList<T>` — cursor-based paginated list
- `ICursorEncoder` — contract for cursor encode/decode
- `IPaginationParameters` — read-only pagination request contract
- `ICursorPaginationParameters` — cursor pagination request contract
- `InvalidPaginationCursorException` / `ExpiredPaginationCursorException`

---

### EricksonLopez.Pagination (Core)

| Property | Value |
|---|---|
| Purpose | Core implementations: PagedList<T>, cursor encoders, options |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Abstractions, Microsoft.Extensions.Options, Microsoft.Extensions.Logging.Abstractions |
| AOT | ✅ Fully AOT-safe (cursor path) |
| Consumers | Application projects that create PagedList<T> instances directly |

**Public API:**
- `PagedList<T>` + `CountedPagedList<T>` — immutable result models with factory methods
- `CursorPagedList<T>` + `CountedCursorPagedList<T>` — cursor result models
- `Base64CursorEncoder` — opaque Base64URL cursor encoding
- `HmacCursorEncoder` — HMAC-SHA256 signed cursors with TTL
- `PaginationCoreOptions` — global configuration (MaxPageSize, AcceptLegacyCursors, etc.)
- `PaginationCursorOptions` — cursor-specific options
- `PaginationParameters` (struct) — offset pagination request
- `CursorPaginationParameters` (struct) — cursor pagination request
- `FilterParameters` / `SortParameters` — filter and sort DSL request

---

### EricksonLopez.Pagination.EntityFrameworkCore

| Property | Value |
|---|---|
| Purpose | EF Core extensions: keyset builder, async pagination, filter/sort DSL |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Core, Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Relational |
| AOT | ⚠️ AOT-compatible (keyset/offset); filter DSL requires [RequiresUnreferencedCode] |
| Consumers | Data access / infrastructure projects using EF Core |

**Public API:**
- `IQueryable<T>.ToPagedListAsync()` — offset with COUNT (2 queries)
- `IQueryable<T>.ToPagedListWithoutCountAsync()` — offset without COUNT (N+1 probe)
- `IQueryable<T>.ToPagedListDeferredAsync()` — window COUNT (1 query)
- `IQueryable<T>.Keyset(cursor)` — initiates fluent keyset builder → `ToCursorPagedListAsync()`
- `KeysetBuilder<T>` — `.Ascending(x => x.Id)`, `.Descending(x => x.CreatedAt)`
- `IQueryable<T>.ApplyFilter(filter)` — filter DSL (requires [RequiresUnreferencedCode])
- `IQueryable<T>.ApplySort(sort)` — dynamic sort
- PostgreSQL approximate count extensions

---

### EricksonLopez.Pagination.Dapper

| Property | Value |
|---|---|
| Purpose | Dapper integration: offset and keyset pagination via IDbConnection |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Core, Abstractions, Dapper.StrongName |
| AOT | ⚠️ Dapper itself uses reflection for mapping |
| Consumers | Infrastructure projects using raw SQL via Dapper |
| Note | Uses ImplicitUsings=disable to prevent namespace conflicts with Dapper |

**Public API:**
- `IDbConnection.ToPagedListAsync<T>()` — offset pagination with SQL + countSql
- `DapperKeysetBuilder<T>` — fluent multi-column keyset: `.From()`, `.Where()`, `.OrderBy()`, `.ThenBy()`, `.WithCursorColumns()`, `.WithCursorDecoder()`, `.ExecuteAsync()`
- All 4 SQL dialects supported: PostgreSQL, SQL Server, MySQL, SQLite

---

### EricksonLopez.Pagination.MongoDB

| Property | Value |
|---|---|
| Purpose | MongoDB pagination: offset and cursor via IFindFluent |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Core, MongoDB.Driver |
| AOT | ❌ MongoDB.Driver uses reflection |
| Consumers | Infrastructure projects using MongoDB |

**Public API:**
- `IFindFluent<T, T>.ToPagedListAsync()` — offset pagination
- `IFindFluent<T, T>.ToCursorPagedListAsync()` — cursor pagination (ObjectId-based)
- `IMongoCollection<T>` offset and cursor extensions

---

### EricksonLopez.Pagination.Cosmos

| Property | Value |
|---|---|
| Purpose | Azure Cosmos DB pagination: continuation token-based |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Core, Microsoft.Azure.Cosmos |
| AOT | ⚠️ Cosmos SDK has partial AOT support |
| Consumers | Azure Cosmos DB backends |

---

### EricksonLopez.Pagination.AspNetCore

| Property | Value |
|---|---|
| Purpose | ASP.NET Core integration: model binders, response envelopes, ETag |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Core, Microsoft.AspNetCore.App (FrameworkReference) |
| AOT | ✅ Fully AOT-safe |
| Consumers | REST API projects (Minimal APIs, MVC controllers) |

**Public API:**
- `IPagedList<T>.ToPagedResult(request)` — converts to HTTP result with ETag
- `ICursorPagedList<T>.ToCursorPagedResult(request)` — cursor HTTP result
- `PaginationEndpointFilter` — Minimal API endpoint filter for validation
- `AddPagination(options)` — DI registration with options
- `PaginationParametersModelBinder` / `CursorPaginationParametersModelBinder`

---

### EricksonLopez.Pagination.OpenApi

| Property | Value |
|---|---|
| Purpose | Swagger/OpenAPI schema integration for paginated endpoints |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Core, Swashbuckle.AspNetCore.SwaggerGen, Microsoft.AspNetCore.OpenApi, Microsoft.OpenApi |
| AOT | ⚠️ Swagger tooling may not be AOT-safe |
| Consumers | API projects with Swagger documentation |

---

### EricksonLopez.Pagination.Blazor

| Property | Value |
|---|---|
| Purpose | Headless Blazor pagination component |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Abstractions only, Microsoft.AspNetCore.Components.Web |
| AOT | ✅ Fully AOT-safe |
| Consumers | Blazor Server / WASM projects |

**Note**: Depends only on `Abstractions` (not Core). Keeps Blazor dependency footprint minimal.

---

### EricksonLopez.Pagination.Grpc

| Property | Value |
|---|---|
| Purpose | gRPC/Protobuf pagination message converters |
| Target frameworks | net8.0, net9.0, net10.0 |
| Dependencies | Abstractions only, Google.Protobuf |
| AOT | ✅ Fully AOT-safe |
| Consumers | gRPC service endpoints |

**Note**: Depends only on `Abstractions`. Proto-generated types call `.ToParameters()` / `.ToMessage()`.

---

### EricksonLopez.Pagination.SourceGenerators

| Property | Value |
|---|---|
| Purpose | Roslyn source generator for AOT cursor decoder registration |
| Target frameworks | netstandard2.0 (runs in Roslyn compiler host) |
| Dependencies | Microsoft.CodeAnalysis.CSharp (compile-time only) |
| AOT | N/A (compile-time tool) |
| Consumers | Any project that publishes with `PublishAot=true` |

**What it does**: Analyzes `Ascending()`/`Descending()` calls → emits `[ModuleInitializer]` → registers cursor type decoders into `ICursorDecoderRegistry` → no reflection at runtime.

---

### EricksonLopez.Pagination.Analyzers

| Property | Value |
|---|---|
| Purpose | Roslyn analyzers for compile-time safety (PAG001-007) |
| Target frameworks | netstandard2.0 (runs in Roslyn compiler host) |
| Dependencies | Microsoft.CodeAnalysis.CSharp (compile-time only) |
| AOT | N/A (compile-time tool) |
| Consumers | All consuming projects (install as an analyzer) |

| Analyzer | Severity | Description |
|---|---|---|
| PAG001 | Error | ToPagedListAsync on unsorted IQueryable — non-deterministic results |
| PAG002 | Warning | Offset pagination with deep page number (>threshold) — consider keyset |
| PAG003 | Warning | Cursor re-used across different keyset configurations |
| PAG007 | Warning | KeysetBuilder<T> has >5 columns — SQL predicate grows O(2^N) |

---

## Compatibility Matrix

| Package | .NET 8 | .NET 9 | .NET 10 | Native AOT | Trimming |
|---|:---:|:---:|:---:|:---:|:---:|
| Abstractions | ✅ | ✅ | ✅ | ✅ | ✅ |
| Core | ✅ | ✅ | ✅ | ✅ | ✅ |
| EntityFrameworkCore | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| Dapper | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| MongoDB | ✅ | ✅ | ✅ | ❌ | ⚠️ |
| Cosmos | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| AspNetCore | ✅ | ✅ | ✅ | ✅ | ✅ |
| OpenApi | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| Blazor | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| Grpc | ✅ | ✅ | ✅ | ✅ | ✅ |
| SourceGenerators | netstandard2.0 | netstandard2.0 | netstandard2.0 | N/A | N/A |
| Analyzers | netstandard2.0 | netstandard2.0 | netstandard2.0 | N/A | N/A |

**Legend**: ✅ Fully supported · ⚠️ Supported with documented limitations · ❌ Not supported

---

## Typical Installation Patterns

### Minimal REST API with EF Core

```bash
dotnet add package EricksonLopez.Pagination.EntityFrameworkCore
dotnet add package EricksonLopez.Pagination.AspNetCore
```

### Minimal REST API with Dapper

```bash
dotnet add package EricksonLopez.Pagination.Dapper
dotnet add package EricksonLopez.Pagination.AspNetCore
```

### Clean Architecture (domain layer)

```bash
# Domain project — zero database dependency
dotnet add package EricksonLopez.Pagination.Abstractions

# Infrastructure project
dotnet add package EricksonLopez.Pagination.EntityFrameworkCore

# API project
dotnet add package EricksonLopez.Pagination.AspNetCore
```

### Native AOT publishing

```bash
dotnet add package EricksonLopez.Pagination.EntityFrameworkCore
dotnet add package EricksonLopez.Pagination.AspNetCore
dotnet add package EricksonLopez.Pagination.SourceGenerators  # Required for AOT cursor decoding
```

### Full stack (all features)

```bash
dotnet add package EricksonLopez.Pagination.EntityFrameworkCore
dotnet add package EricksonLopez.Pagination.AspNetCore
dotnet add package EricksonLopez.Pagination.OpenApi
dotnet add package EricksonLopez.Pagination.Analyzers
dotnet add package EricksonLopez.Pagination.SourceGenerators  # AOT
```
