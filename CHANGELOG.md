# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] — 2026-09-21

### ⚠️ Breaking Changes

- **BC-001 (Runtime/ABI Breaking): Optional parameter added to public `HmacCursorEncoder` constructor**
  - **What changed:** Added an optional `Func<string?>? tenantContextProvider = null` parameter to the public `HmacCursorEncoder` constructor.
  - **Previous State:** 6-parameter constructor `HmacCursorEncoder(string secretKey, ICursorEncoder? innerEncoder = null, TimeSpan? timeToLive = null, TimeSpan? clockSkewTolerance = null, ICursorReplayStore? replayStore = null, Microsoft.Extensions.Logging.ILogger? logger = null)`.
  - **Current State:** 7-parameter constructor `HmacCursorEncoder(string secretKey, ICursorEncoder? innerEncoder = null, TimeSpan? timeToLive = null, TimeSpan? clockSkewTolerance = null, ICursorReplayStore? replayStore = null, Microsoft.Extensions.Logging.ILogger? logger = null, Func<string?>? tenantContextProvider = null)`.
  - **Affected Consumers:** Downstream assemblies compiled against `EricksonLopez.Pagination` v1.0.0 that invoke `new HmacCursorEncoder(...)` without recompilation.
  - **Impact:** Calling the constructor from pre-compiled binaries fails at runtime with `System.MissingMethodException: Method not found: 'Void EricksonLopez.Pagination.HmacCursorEncoder..ctor(...)'`.
  - **Migration:** Recompile consumer assemblies against the updated package.

- **BC-002 (Runtime/Behavioral Breaking): Keyset schema fingerprint algorithm updated in EF Core and LinqToDB**
  - **What changed:** `KeysetBuilder<T>.GetKeysetFingerprint()` in `EricksonLopez.Pagination.EntityFrameworkCore` and `EricksonLopez.Pagination.LinqToDB` now incorporates the entity type full name (`typeof(T).FullName`), property member names (`me.Member.Name`), and tenant identifier (`_tenantId`) in the FNV-1a hash calculation.
  - **Previous State:** Keyset fingerprint was computed solely from column property type names and sort direction.
  - **Current State:** Keyset fingerprint is uniquely bound to the entity type, property member names, sort directions, and tenant context.
  - **Affected Consumers:** Applications running keyset pagination where users, API clients, or cached URLs submit in-flight v2 keyset cursors minted under v1.0.0.
  - **Impact:** All v2 keyset cursors (`M|v2|<fingerprint>|...`) issued by v1.0.0 will fail fingerprint validation on updated servers and throw `InvalidPaginationCursorException("Cursor was generated for a different keyset and cannot be used here.")`.
  - **Migration:** Plan deployment during low-traffic windows. Ensure API clients gracefully handle cursor validation failures by falling back to the initial page.

- **BC-003 (Security/Behavioral Breaking): Enforced hard 8192-character maximum length limit in `HmacCursorEncoder.Decode`**
  - **What changed:** Added an explicit length boundary check `const int MaxCursorInputLength = 8192;` at the beginning of `HmacCursorEncoder.Decode`.
  - **Previous State:** `Decode` processed arbitrary length strings without an input length cap.
  - **Current State:** Inputs exceeding 8192 characters immediately throw `InvalidPaginationCursorException("The cursor exceeds the maximum allowed length of 8192 characters.", opaqueCursor)` and record a `tampered` metric.
  - **Affected Consumers:** Consumers passing oversized multi-column keyset cursors or large embedded data payloads inside HMAC cursors.
  - **Impact:** Cursor strings longer than 8192 characters will be rejected immediately with an exception instead of being processed.
  - **Migration:** Ensure cursor payloads and composite key values fit within the 8192-character limit.

- **BC-004 (Behavioral/Configuration Breaking): Cryptographic tenant context binding and cross-tenant replay prevention in `HmacCursorEncoder`**
  - **What changed:** Cursors generated when `TenantContextProvider` is configured now embed a `CTX:<tenant>:` segment. Decoding verifies tenant affinity.
  - **Previous State:** HMAC cursors had no tenant affinity; any cursor with a valid signature was accepted regardless of tenant.
  - **Current State:** When `TenantContextProvider` is active: cursors missing tenant context, matching a different tenant, or provided when no tenant context exists throw `InvalidPaginationCursorException`.
  - **Affected Consumers:** Multi-tenant applications enabling `PaginationCursorOptions.TenantContextProvider`.
  - **Impact:** Enabling tenant isolation immediately invalidates untyped cursors minted before tenant configuration was active, and strictly blocks cross-tenant pagination.
  - **Migration:** Enable `TenantContextProvider` during a planned release where active client cursor state can be reset.

- **BC-005 (Integration/Behavioral Breaking): `PaginationEndpointFilter` returns typed `PaginationErrorResponse` instead of anonymous object**
  - **What changed:** Minimal API endpoint filter `PaginationEndpointFilter` now returns `Results.BadRequest(new PaginationErrorResponse(...))` using the new public record `PaginationErrorResponse(string Error)`.
  - **Previous State:** Returned `Results.BadRequest(new { error = "..." })` with an anonymous object.
  - **Current State:** Returns `PaginationErrorResponse` with public property `Error` (PascalCase).
  - **Affected Consumers:** Unit and integration tests inspecting `Microsoft.AspNetCore.Http.HttpResults.BadRequest<object>.Value` or reflecting on property names.
  - **Impact:** The JSON wire format remains `{ "error": "..." }`, but in-memory object inspection or casting to dynamic expecting camelCase `error` property may fail.
  - **Migration:** Update test assertions to inspect `response.Value as PaginationErrorResponse` or access `Error`.

- **BC-006 (Behavioral Breaking): Dapper parameter dictionary keys altered (`@__Pagination_Limit__` → `__Pagination_Limit__`)**
  - **What changed:** Removed leading `@` character from parameter keys registered in `DynamicParameters` within `DbConnectionPaginationExtensions`.
  - **Previous State:** Parameters were registered as `@__Pagination_Limit__` and `@__Pagination_Skip__`.
  - **Current State:** Parameters are registered as `__Pagination_Limit__` and `__Pagination_Skip__` to allow Dapper to handle provider-specific prefixes automatically.
  - **Affected Consumers:** Custom Dapper command interceptors, diagnostic wrappers, or tests querying `DynamicParameters.ParameterNames`.
  - **Impact:** Downstream code expecting parameter names with the `@` prefix will not find them in the parameter dictionary.
  - **Migration:** Update parameter dictionary lookup keys in custom logging or query interceptors to omit `@`.

- **BC-007 (Observability/Integration Breaking): OpenTelemetry metric instruments declare explicit unit descriptors**
  - **What changed:** `PaginationMetrics` instruments (`QueriesTotal`, `PageDepth`, `CursorErrors`) now declare standard unit descriptors (`{queries}`, `{pages}`, `{errors}`).
  - **Previous State:** Instruments created without unit parameters (`unit: null`).
  - **Current State:** Explicit units passed during meter instrument creation.
  - **Affected Consumers:** Prometheus exporters, OpenTelemetry collectors, and metric alert systems with strict metric series naming rules.
  - **Impact:** Some Prometheus scraping pipelines append unit strings to metric names (e.g. `pagination_queries_total_queries`), potentially affecting dashboards or alerts.
  - **Migration:** Verify Prometheus alert rules and Grafana queries for compatibility with OpenTelemetry unit annotations.

- **BC-008 (Configuration/Build Breaking): Assembly strong-naming key file migrated to `EricksonLopez.snk`**
  - **What changed:** `Directory.Build.props` migrated assembly signing to check for `EricksonLopez.snk` and exports a public key token.
  - **Previous State:** Build referenced `DummyDevelopmentKey.snk`.
  - **Current State:** Standardized production key `EricksonLopez.snk` with explicit `<PublicKey>`.
  - **Affected Consumers:** Downstream consumers referencing strong-named assemblies or configuring `[InternalsVisibleTo]`.
  - **Impact:** Assembly identity changes. Consumers binding to internals or verifying public key tokens must update their key references.
  - **Migration:** Update `PublicKey` declarations in consuming test projects referencing internal members.

### 🚀 Added

- **Multi-Tenant Keyset Pagination (`EricksonLopez.Pagination.EntityFrameworkCore` & `EricksonLopez.Pagination.LinqToDB`)**: Added fluent `.WithTenant(string tenantId)` to `KeysetBuilder<T>` to cryptographically bind keyset queries to a tenant.
- **Tenant Context Configuration (`EricksonLopez.Pagination`)**: Added `TenantContextProvider` property to `PaginationCursorOptions` for automatic HMAC tenant token binding.
- **Structured Error Response Model (`EricksonLopez.Pagination.AspNetCore`)**: Added `public sealed record PaginationErrorResponse(string Error)`.
- **Large Page Arithmetic Hardening**: Replaced 32-bit offset arithmetic with checked 64-bit `long` calculations across EF Core, LinqToDB, Dapper, and MongoDB extensions to eliminate integer overflow on deep pagination.
- **TotalPages Calculation Hardening (`EricksonLopez.Pagination`)**: Refactored `PagedList<T>.CalculateTotalPages` to protect against `long.MaxValue` overflow.
- **SourceLink Packaging**: Added `Microsoft.SourceLink.GitHub` package across all packable projects for Git-linked symbol debugging.

### 🐛 Fixed

- **Dapper Keyset Multicolumn Parsing (`EricksonLopez.Pagination.Dapper`)**: Fixed column delimiter splitting in `DapperKeysetBuilder<T>` so percent-encoded pipe (`%7C`) and percent (`%25`) characters are preserved without breaking column boundaries.
- **Native AOT Trimming Preservation (`EricksonLopez.Pagination`)**: Added `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]` to `PaginationExpressionCache.GetCompareToMethod<TKey>()` to prevent trimmer removal under Native AOT.

### 🔄 Changed

- **Documentation Architecture**: Consolidated root documentation files (`aot.md`, `architecture.md`, `boundary.md`, `features.md`, `packages.md`, `performance.md`, `testing.md`) into structured documents under `docs/` according to ADR-0039. Renamed `roadmap.md` to `ROADMAP.md`.

## [1.0.0] — 2026-08-25

### Initial Release

Inaugural production release of the `EricksonLopez.Pagination` ecosystem: a high-performance, zero-allocation, AOT-first offset and keyset pagination framework with cryptographic tamper-proof cursors, dynamic filter/sort DSL, and ORM integrations for modern .NET (8.0, 9.0, 10.0).

### Added

- **Core Primitives & Abstractions (`EricksonLopez.Pagination.Abstractions`)**:
  - Zero-dependency interfaces: `IPagedList<T>`, `ICountedPagedList<T>`, `ICursorPagedList<T>`, and `ICursorPagedList`.
  - Pagination parameter value types: `PaginationParameters` (offset) and `CursorPaginationParameters` (keyset) with `IParsable<T>` for native Minimal API binding.
  - Pluggable cursor contracts: `ICursorEncoder`, `ICursorDecoderRegistry`, `ICursorReplayStore`, `RawCursorValue`.
  - Structured cursor exceptions: `InvalidPaginationCursorException`, `ExpiredPaginationCursorException`.

- **Core Engine & Implementations (`EricksonLopez.Pagination`)**:
  - High-performance immutable collections: `PagedList<T>`, `CountedPagedList<T>`, `CursorPagedList<T>`, `CountedCursorPagedList<T>`.
  - Pluggable cursor encoders: `Base64CursorEncoder` and `HmacCursorEncoder` (HMAC-SHA256 authenticated tamper-evident tokens).
  - High-throughput expression caching via `PaginationExpressionCache` with concurrent thread-safe lookups.
  - In-memory cursor replay store with TTL eviction (`InMemoryCursorReplayStore`).
  - OpenTelemetry metrics instrumentation via `PaginationMetrics` (public) with internal `ActivitySource` tracing for distributed trace propagation.
  - Dependency Injection setup: `services.AddPagination()`.

- **Entity Framework Core Integration (`EricksonLopez.Pagination.EntityFrameworkCore`)**:
  - Multi-column fluent Keyset pagination: `query.Keyset(parameters).Ascending(x => x.CreatedAt).Ascending(x => x.Id).ToCursorPagedListAsync()`.
  - Count-less fast offset lookahead: `ToPagedListWithoutCountAsync()`.
  - Approximate count acceleration using database statistics on PostgreSQL (`pg_class.reltuples`) and SQL Server (`sys.dm_db_partition_stats`).
  - Memory-efficient streaming: `ToPagedAsyncEnumerable<T>()` and `ToPagedListBatchedAsync<T>()`.
  - Integrated dynamic filter and sort pipeline: `ToPagedListAsync(filter, sort, parameters)`.

- **ASP.NET Core Web Integration (`EricksonLopez.Pagination.AspNetCore`)**:
  - Standardized JSON response models: `PagedResponse<T>`, `CursorPagedResponse<T>`, `Edge<T>`, and `RelayPageInfo`.
  - Model binders for MVC Controllers and Minimal APIs (`PaginationParametersModelBinder`, `CursorPaginationParametersModelBinder`, `FilterParametersModelBinder`, `SortParametersModelBinder`).
  - Minimal API endpoint filter `PaginationEndpointFilter` enforcing parameter range bounds and logging.
  - RFC 7232 HTTP conditional request and deterministic ETag generation via `PaginationETagOptions`.
  - RFC 7807 Problem Details exception handler mapping pagination errors to standard HTTP status codes.

- **Data Access Ecosystem Packages**:
  - `EricksonLopez.Pagination.Dapper`: Dynamic SQL keyset and offset pagination extensions with parameterized SQL generation.
  - `EricksonLopez.Pagination.LinqToDB`: Zero-overhead queryable extension methods for LinqToDB.
  - `EricksonLopez.Pagination.MongoDB`: Keyset seek and offset extensions for MongoDB Driver `IMongoQueryable<T>` and `IFindFluent<TEntity, TProjection>`.
  - `EricksonLopez.Pagination.Cosmos`: Azure Cosmos DB SQL continuation token translation and feed iterators.
  - `EricksonLopez.Pagination.Elasticsearch`: Search After cursor pagination integration for Elasticsearch .NET Client.

- **Web, API & Transport Integrations**:
  - `EricksonLopez.Pagination.Relay`: GraphQL Relay Cursor Connections Specification compliant connection builders and models.
  - `EricksonLopez.Pagination.Grpc`: Protocol Buffers cursor mapping and gRPC service interceptors.
  - `EricksonLopez.Pagination.Blazor`: Reusable pagination state containers and components for Blazor Server and WebAssembly.
  - `EricksonLopez.Pagination.OpenApi`: Swagger/OpenAPI schema and operation transformers for pagination parameters and responses.
  - `EricksonLopez.Pagination.Redis`: Distributed cursor replay protection and stateful token expiration store via StackExchange.Redis.
  - `EricksonLopez.Pagination.Result`: Railway-oriented programming integration (`Result<IPagedList<T>>`).

- **Compile-Time Safety & Tooling**:
  - `EricksonLopez.Pagination.Analyzers`: Roslyn analyzers (`PAG002`–`PAG008`) preventing common pagination anti-patterns at compile time.
  - `EricksonLopez.Pagination.SourceGenerators`: Compile-time Native AOT cursor decoders and filter provider generators.

- **Security & Integrity**:
  - Cryptographic HMAC-SHA256 authenticated cursor wire format (`S|` single-column, `M|` multi-column).
  - Distributed nonce replay protection preventing cursor token reuse.
  - Schema fingerprinting preventing desynchronization across rolling deployments.
  - Configurable AST depth, clause complexity, and string length limits preventing ReDoS and denial of service.

- **Compatibility & Quality Gates**:
  - Target frameworks: `net8.0`, `net9.0`, `net10.0` (Native AOT verified in CI with zero trim warnings).
  - 100% Mutation testing score via Stryker.NET.
  - Zero warnings under `AnalysisMode=All` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

[2.0.0]: https://github.com/ericksonlopezf/dotnet-pagination/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/ericksonlopezf/dotnet-pagination/releases/tag/v1.0.0
