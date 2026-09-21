# Architectural Boundary Specification: EricksonLopez.Pagination.Abstractions

## 1. Purpose
`EricksonLopez.Pagination.Abstractions` defines pure, framework-agnostic pagination request parameters, result contracts, and cursor primitives (`IPagedList<T>`, `ICursorPagedList<T>`, `PaginationParameters`, `CursorPaginationParameters`) for consistent data subset retrieval across all architectural layers.

## 2. Types Owned by Abstractions
- **Pagination Parameters**:
  - `PaginationParameters` (offset request struct with `IParsable<PaginationParameters>`)
  - `CursorPaginationParameters` (keyset request struct with `IParsable<CursorPaginationParameters>`)
- **Pagination Contracts**:
  - `IPagedList<T>`, `ICountedPagedList<T>`
  - `ICursorPagedList<T>`, `ICountedCursorPagedList<T>`, `ICursorPagedList`
- **Sorting & Filtering Primitives**:
  - `SortParameters`, `SortDirection`
  - `FilterParameters`, `FilterableAttribute`, `GenerateFilterProviderAttribute`
  - `IFilterProvider<T>`, `IFilterOperatorProvider`, `FilterExpressionExtensions`
- **Cursor & Security Primitives**:
  - `ICursorEncoder`, `ICursorDecoderRegistry`, `ICursorReplayStore`, `RawCursorValue`
  - `IPagedListFactory`, `ICursorPagedListFactory`, `IPaginationOptions`
- **Structured Exceptions**:
  - `InvalidPaginationCursorException`
  - `ExpiredPaginationCursorException`
  - `ReplayedPaginationCursorException`

## 3. Does Not Own
- In-memory collection implementations (`PagedList<T>`, `CursorPagedList<T>` in `EricksonLopez.Pagination`).
- Concrete cursor encoders and caching (`HmacCursorEncoder`, `Base64CursorEncoder`, `InMemoryCursorReplayStore`, `PaginationExpressionCache`).
- ASP.NET Core model binders, filters, and RFC 7232 ETag wrappers (`EricksonLopez.Pagination.AspNetCore`).
- Database SQL builders and query execution (`EntityFrameworkCore`, `Dapper`, `LinqToDB`, `MongoDB`, `Cosmos`, `Elasticsearch`).
- Functional Result extensions (`EricksonLopez.Pagination.Result`).
- Protocol buffers and transport mappings (`EricksonLopez.Pagination.Grpc`, `EricksonLopez.Pagination.Relay`).

## 4. Allowed Dependencies
- **.NET BCL only** (`net8.0`, `net9.0`, `net10.0`).
- **Zero** external NuGet packages or sibling package references.

## 5. Forbidden Dependencies
- Database driver SDKs (`Npgsql`, `Microsoft.Data.SqlClient`, `MongoDB.Driver`, `Azure.Cosmos`).
- `Microsoft.AspNetCore.*` (excluding standard BCL primitives).
- `Microsoft.EntityFrameworkCore`, `Dapper`, `linq2db`.
- JSON serializers or third-party reflection engines.

## 6. Consumers
- `EricksonLopez.Pagination` (Core Engine).
- All provider packages (`EntityFrameworkCore`, `Dapper`, `LinqToDB`, `MongoDB`, `Cosmos`, `Elasticsearch`, `Redis`, `Relay`, `Grpc`, `Blazor`, `AspNetCore`, `Result`).
- Application and Domain layers in Clean Architecture / DDD solutions.

## 7. Public API Invariants
- Immutable contracts with zero hidden state.
- Struct-based parameter types implementing `IParsable<T>` for zero-reflection binding.
- Exception-based error signaling for invalid/expired/replayed tokens.

## 8. AOT & Trimming Expectations
- `IsAotCompatible=true` enforced in build.
- `EnableTrimAnalyzer=true` enforced with zero warnings.
- 100% reflection-free contract design.
