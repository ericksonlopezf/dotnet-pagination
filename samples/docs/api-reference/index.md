# API Reference Index — EricksonLopez.Pagination

> Comprehensive, Microsoft Learn-grade reference documentation for all public APIs across Core Library and Infrastructure packages.

---

## Documentation Modules

| Module | Target Package | Key Components & APIs |
|---|---|---|
| **[Abstractions](abstractions.md)** | `EricksonLopez.Pagination.Abstractions` | `PaginationParameters`, `CursorPaginationParameters`, `FilterParameters`, `SortParameters`, `RawCursorValue`, `IPagedList<T>`, `ICursorPagedList<T>`, `ICountedPagedList<T>`, `ICursorEncoder`, `ICursorReplayStore`, `IFilterProvider<T>`, `IFilterOperatorProvider<T>`, `InvalidPaginationCursorException`, `ExpiredPaginationCursorException`, `ReplayedPaginationCursorException`, `[Filterable]`, `[GenerateFilterProvider]` |
| **[Core](core.md)** | `EricksonLopez.Pagination` | `PagedList<T>`, `CountedPagedList<T>`, `CursorPagedList<T>`, `CountedCursorPagedList<T>`, `HmacCursorEncoder`, `Base64CursorEncoder`, `DefaultPagedListFactory`, `InMemoryCursorDecoderRegistry`, `InMemoryCursorReplayStore`, `PaginationCoreOptions`, `PaginationCursorOptions`, `PaginationDiagnostics`, `PaginationMetrics`, `PaginationLogEvents`, `FilterUnknownFieldBehavior` |
| **[ASP.NET Core](aspnetcore.md)** | `EricksonLopez.Pagination.AspNetCore` | `PagedResponse<T>`, `CursorPagedResponse<T>`, `Edge<T>`, `RelayPageInfo`, `PaginationExtensions`, `PaginationResultExtensions` (ETags), `PaginationEndpointFilter` (`AddPaginationValidation`), `PaginationExceptionHandler`, `AddPagination()`, Model Binders |
| **[EF Core](efcore.md)** | `EricksonLopez.Pagination.EntityFrameworkCore` | `KeysetBuilder<T>`, `QueryableExtensions` (`Keyset`, `ToPagedListAsync`, `ApplyFilter`, `ApplySort`, `ToPagedListBatchedAsync`), `KeysetStreamingExtensions`, `KeysetPartitioningExtensions` (`SplitKeysetPartitionsAsync`), `PostgreSqlPaginationExtensions` |
| **[Dapper](dapper.md)** | `EricksonLopez.Pagination.Dapper` | `DapperKeysetBuilder<T>`, `DbConnectionPaginationExtensions`, `DbConnectionCursorExtensions`, `GridReaderPaginationExtensions`, `DatabaseDialect`, `CursorSqlBuilder` |
| **[NoSQL & Specialized](nosql.md)** | `MongoDB`, `Cosmos`, `LinqToDB`, `Elasticsearch` | `MongoCursorPaginationExtensions`, `MongoObjectIdPaginationExtensions`, `CosmosPaginationExtensions`, `CursorPaginationLinqToDBExtensions`, `ElasticsearchCursorPaginationExtensions` |
| **[Extensions & Ecosystem](extensions.md)** | `Redis`, `Relay`, `Result`, `Grpc`, `Blazor`, `OpenApi` | `RedisCursorReplayStore`, `Connection<TNode>`, `RelayPaginationExtensions`, `PaginationErrors`, `PaginationResultExtensions`, `PaginationGrpcExtensions`, `PaginationUIOptions`, `PaginationOperationFilter` |

---

## Public API Design Principles

1. **Zero-Allocation Hot Paths**: Record structs (`PaginationParameters`, `CursorPaginationParameters`) implement `IParsable<T>` for zero-reflection binding in Minimal APIs and Native AOT.
2. **Defensive Cryptography**: Keyset cursors are cryptographically sealed with HMAC-SHA256 by default. Timing attacks are mitigated via `CryptographicOperations.FixedTimeEquals`.
3. **Replay Protection**: Single-use cursor nonces prevent replay attacks and rogue web-scraping in distributed topologies.
4. **Compile-Time Safety**: Roslyn analyzers (`PAG001` through `PAG007`) prevent common anti-patterns like non-deterministic sorting or unencrypted cursors.
5. **Deterministic HTTP Caching**: Built-in SHA-256 ETags provide automated HTTP 304 Not Modified responses, conserving bandwidth and serialization CPU cycles.
