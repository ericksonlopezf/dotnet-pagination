# Architectural Boundary Specification: EricksonLopez.Pagination.Abstractions

## 1. Purpose
`EricksonLopez.Pagination.Abstractions` defines pure, framework-agnostic pagination request and response contracts (`IPageRequest`, `IPagedList<T>`, `ICursorPageRequest`, `IKeysetPagination`) for consistent data subset retrieval across all layers.

## 2. Owns
- `IPageRequest`, `IPagedList<T>`.
- `ICursorPageRequest`, `IKeysetPageRequest`.
- Pagination metadata (`PageInfo`, `PaginationOrder`, `CursorToken`).

## 3. Does Not Own
- In-memory collection paginators (`EricksonLopez.Pagination`).
- ASP.NET Core model binders / query string binders (`EricksonLopez.Pagination.AspNetCore`).
- Database SQL pagination decorators (`EricksonLopez.Pagination.SqlBuilder`, `Dapper`).
- Functional Result extensions (`EricksonLopez.Pagination.Result`).

## 4. Allowed Dependencies
- **.NET BCL only**.
- **Zero** external or `EricksonLopez.*` dependencies.

## 5. Forbidden Dependencies
- Database driver SDKs.
- `Microsoft.AspNetCore.*`.
- `Microsoft.EntityFrameworkCore`, `Dapper`.

## 6. Who Can Depend On It
- `EricksonLopez.Pagination` (L2).
- `EricksonLopez.Pagination.Result` (L2).
- `EricksonLopez.Pagination.*` (Adapters).
- Application queries and API response DTOs.

## 7. Public API Rules
- Immutable page models with zero reflection.

## 8. AOT Expectations
- `IsAotCompatible=true`.

## 9. Trimming Expectations
- `IsTrimmable=true`.

## 10. Provider Isolation
- 100% database-agnostic.

## 11. Testing Isolation
- Fixture helpers live in test suites.
