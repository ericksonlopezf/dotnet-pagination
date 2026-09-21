# NuGet Packages Overview

This document details the public API surface, compatibility matrix, and package management strategies for the `EricksonLopez.Pagination` ecosystem.

## Compatibility Matrix

All external package versions are centrally managed via `Directory.Packages.props` (Central Package Management).

| Package | Purpose | Target Frameworks | Publish Status | Main Dependencies |
|---------|---------|-------------------|----------------|-------------------|
| `EricksonLopez.Pagination.Abstractions` | Defines interfaces and standard contracts. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | None |
| `EricksonLopez.Pagination` | Core implementations, structs, and logic. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Abstractions`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions` |
| `EricksonLopez.Pagination.EntityFrameworkCore` | IQueryable extensions and Keyset builders. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Core` |
| `EricksonLopez.Pagination.AspNetCore` | API response wrappers (`PagedResponse`). | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Core`, `Microsoft.AspNetCore.App` (FrameworkReference) |
| `EricksonLopez.Pagination.Dapper` | Raw SQL offset & keyset pagination with `IDbConnection`. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Dapper.StrongName`, `Core`, `Abstractions` |
| `EricksonLopez.Pagination.LinqToDB` | LinqToDB LINQ provider keyset & offset pagination. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `linq2db`, `Core`, `Abstractions` |
| `EricksonLopez.Pagination.MongoDB` | MongoDB driver cursor and offset extensions. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `MongoDB.Driver`, `Core` |
| `EricksonLopez.Pagination.Redis` | Distributed cursor replay & nonce store. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `StackExchange.Redis`, `Abstractions` |
| `EricksonLopez.Pagination.Relay` | GraphQL Relay Cursor Connections specification adapter. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Core`, `Abstractions` |
| `EricksonLopez.Pagination.Elasticsearch` | Elasticsearch 8.x `search_after` cursor pagination. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Elastic.Clients.Elasticsearch`, `Core` |
| `EricksonLopez.Pagination.Blazor` | Headless pager components for UI. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Microsoft.AspNetCore.Components.Web`, `Abstractions` |
| `EricksonLopez.Pagination.Grpc` | Protobuf messages and gRPC mappings. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Google.Protobuf`, `Abstractions` |
| `EricksonLopez.Pagination.OpenApi` | Swagger/OpenAPI schema filters for paginated models. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Swashbuckle.AspNetCore.SwaggerGen`, `Microsoft.OpenApi`, `Core` |
| `EricksonLopez.Pagination.Cosmos` | Azure Cosmos DB pagination extensions. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Microsoft.Azure.Cosmos`, `Core` |
| `EricksonLopez.Pagination.Result` | Railway-Oriented Programming integration with `EricksonLopez.Result`. | `net8.0`, `net9.0`, `net10.0` | ✅ In `publish.yml` | `Abstractions`, `EricksonLopez.Result` |
| `EricksonLopez.Pagination.SourceGenerators` | AOT-friendly cursor decoders generator. | `netstandard2.0` | ✅ In `publish.yml` | `Microsoft.CodeAnalysis.CSharp` (compile-time) |
| `EricksonLopez.Pagination.Analyzers` | Roslyn analyzers (`PAG002`–`PAG008`, `PAG001` reserved). | `netstandard2.0` | ✅ In `publish.yml` | `Microsoft.CodeAnalysis.CSharp` (compile-time) |

> [!NOTE]
> The `SourceGenerators` and `Analyzers` packages target `netstandard2.0` because Roslyn analyzers must run inside the compiler host process. They do not add runtime dependencies to consuming projects.

## AOT and Trimming Compatibility

| Package | AOT Compatible | Trimmable | Notes |
|---------|---------------|-----------|---|
| `Abstractions` | ✅ | ✅ | Pure contracts, zero runtime reflection. |
| `Core` | ✅ | ✅ | Core implementations and cryptographic encoders. |
| `AspNetCore` | ✅ | ✅ | Includes JSON serializer contexts for Native AOT. |
| `EntityFrameworkCore` | ❌ | ✅ | Dynamic LINQ expression trees rely on `Expression.Compile()`. |
| `Dapper` | Not declared | Not declared | Dynamic mapping relies on runtime IL emission in Dapper. |
| `LinqToDB` | ❌ | ✅ | Expression tree compilation (`IL3050`/`IL2026`). |
| `MongoDB` | ❌ | Not declared | MongoDB BSON serializer uses dynamic reflection. |
| `Redis` | ✅ | ✅ | AOT compatible with StackExchange.Redis. |
| `Relay` | ✅ | ✅ | Zero-reflection GraphQL Relay specification models. |
| `Elasticsearch` | ❌ | Not declared | Elasticsearch 8.x client serializer dynamic dispatch. |
| `Blazor` | ✅ | Not declared | Headless Razor components fully AOT-ready. |
| `Grpc` | ✅ | Not declared | Google Protobuf generated classes are AOT-compatible. |
| `OpenApi` | Not declared | Not declared | Swashbuckle reflection-based schema generators. |
| `Cosmos` | ❌ | Not declared | Azure Cosmos DB SDK dynamic Newtonsoft/System.Text.Json paths. |
| `Result` | ✅ | ✅ | Struct-based zero-allocation Result pattern integration. |
| `SourceGenerators` | N/A | N/A | Compile-time Roslyn generator (`netstandard2.0`). |
| `Analyzers` | N/A | N/A | Compile-time Roslyn analyzer (`netstandard2.0`). |

## Public API Surface Summary

### `EricksonLopez.Pagination.Abstractions`
* **Interfaces**: `IPagedList<T>`, `ICursorPagedList<T>`, `IPaginationParameters`, `ICursorPaginationParameters`, `ICursorEncoder`, `ICursorReplayStore`, `IFilterOperatorProvider`
* **Attributes**: `FilterableAttribute`, `SortableAttribute`
* **Enums/Structs**: `PaginationParameters`, `CursorPaginationParameters`, `SortDirection`

### `EricksonLopez.Pagination` (Core)
* **Core Classes**: `PagedList<T>`, `CursorPagedList<T>`
* **Security & Stores**: `HmacCursorEncoder`, `Base64CursorEncoder`, `InMemoryCursorReplayStore`
* **Filtering AST**: `FilterExpressionParser`, `FilterPredicateBuilder`, custom operator evaluation pipeline.

### Providers & Integrations
* **EntityFrameworkCore**: `ToPagedListAsync`, `ToCursorPagedListAsync`, `ToPagedAsyncEnumerable`, `AsKeysetStreamAsync`, `Keyset()` fluent builder.
* **Dapper**: `ToPagedListAsync` via `IDbConnection` and cursor-based SQL builders.
* **LinqToDB**: `ToPagedListAsync`, `Keyset()` fluent builder for LinqToDB query trees.
* **MongoDB**: `ToCursorPagedListAsync` via `IFindFluent`, `MongoOffsetPaginationExtensions`, `MongoCursorPaginationExtensions`.
* **Redis**: `RedisCursorReplayStore`, `AddPaginationRedisReplayStore()`.
* **Relay**: `Connection<TNode>`, `Edge<TNode>`, `PageInfo`, `ToRelayConnection()`.
* **Elasticsearch**: `ApplyCursorPagination()`, `ToCursorPagedList()`, `ElasticsearchCursorHelper`.
* **AspNetCore**: `ToPagedResponse()`, `ToCursorPagedResponse()`, `PaginationEndpointFilter`, `AddPagination()`.
* **Blazor**: `<PagedListPager />`, `<CursorPagedListPager />`.
* **Grpc**: Protobuf message mappings, `ToParameters()`, `ToMessage()`.
* **OpenApi**: `PaginationOperationTransformer` (.NET 9+), `PaginationOperationFilter` (Swashbuckle), `AddPaginationSchemas()`.

## Breaking Changes & Semantic Versioning

* The project adheres to strict **Semantic Versioning** via `Release Please`.
* Major version bumps are required when changing contracts in `EricksonLopez.Pagination.Abstractions` due to downstream impacts on EF Core, Dapper, and all other provider extensions.
* Package API validation is enabled via `<EnablePackageValidation>true</EnablePackageValidation>` with a baseline version of `1.0.0` for all packable projects.

---

## Central Package Management — Pinned Versions

All dependency versions are centrally managed in [`Directory.Packages.props`](../Directory.Packages.props) (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`).

### Runtime Dependencies

| Package | Version | Used By |
|---|---|---|
| `Dapper.StrongName` | 2.1.79 | `Pagination.Dapper` |
| `Elastic.Clients.Elasticsearch` | 8.17.1 | `Pagination.Elasticsearch` |
| `Google.Protobuf` | 3.35.1 | `Pagination.Grpc` |
| `Grpc.Tools` | 2.83.0 | `Pagination.Grpc` (build-time) |
| `linq2db` | 5.4.1 | `Pagination.LinqToDB` |
| `Microsoft.AspNetCore.Components.Web` | 8.0.0 | `Pagination.Blazor` |
| `Microsoft.AspNetCore.OpenApi` | 9.0.2 | `Pagination.OpenApi` (net9.0+) |
| `Microsoft.Azure.Cosmos` | 3.62.1 | `Pagination.Cosmos` |
| `Microsoft.EntityFrameworkCore` | 8.0.16 / 10.0.9 | `Pagination.EntityFrameworkCore` |
| `Microsoft.EntityFrameworkCore.Relational` | 8.0.16 / 10.0.9 | `Pagination.EntityFrameworkCore` |
| `Microsoft.Extensions.DependencyInjection` | 8.0.1 | `Pagination.Redis` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.2 | `Pagination.Redis` |
| `Microsoft.Extensions.Options` | 8.0.2 | `Pagination` (Core) |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0.2 | `Pagination` (Core) |
| `Microsoft.OpenApi` | 1.6.22 | `Pagination.OpenApi` |
| `MongoDB.Driver` | 3.10.0 | `Pagination.MongoDB` |
| `StackExchange.Redis` | 2.8.24 | `Pagination.Redis` |
| `Swashbuckle.AspNetCore.SwaggerGen` | 6.5.0 | `Pagination.OpenApi` |
