# ADR 0001: Multi-Package Architecture for Core and ORMs

## Status
Accepted

## Context
When designing a pagination library, a common anti-pattern is creating a monolithic package that includes dependencies for various ORMs (Entity Framework, Dapper, etc.) or web frameworks (ASP.NET Core). This approach leads to "dependency bloat," where a domain project referencing the pagination library is forced to transitively depend on packages it does not need.

Furthermore, pagination is not entirely agnostic; its implementation deeply depends on the underlying data access technology (e.g., in-memory LINQ vs. relational SQL queries).

## Decision
We have decided to adopt a strict multi-package architecture:
1. `EricksonLopez.Pagination.Abstractions`: Contains pure interfaces (`IPagedList`) and structs with zero dependencies (The Contract).
2. `EricksonLopez.Pagination` (Core): Contains default implementations (`PagedList`, `PaginationOptions`).
3. `EricksonLopez.Pagination.EntityFrameworkCore`: Contains extension methods (`ToPagedListAsync`) bound to `Microsoft.EntityFrameworkCore`.
4. `EricksonLopez.Pagination.Dapper`: Contains IDbConnection extensions bound to `Dapper`.
5. `EricksonLopez.Pagination.AspNetCore`: Contains API wrappers (`PagedResponse`) bound to ASP.NET Core primitives.

## Consequences
- **Positive:** Domain layers remain clean and free of infrastructural dependencies by referencing only the Core package.
- **Positive:** Consumers only download the specific binaries they need.
- **Negative:** Maintaining multiple packages adds a slight overhead to the CI/CD pipeline and release management.
