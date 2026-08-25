# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
  - OpenTelemetry metrics and Activity instrumentation (`PaginationMetrics`, `PaginationActivity`).
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
  - `EricksonLopez.Pagination.SqlBuilder`: Dialect-agnostic SQL builder supporting PostgreSQL, SQL Server, MySQL, SQLite, and Oracle.

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

[1.0.0]: https://github.com/ericksonlopezf/dotnet-pagination/releases/tag/v1.0.0
