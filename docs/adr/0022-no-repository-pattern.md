# ADR-0022 — No Repository or Unit of Work Pattern

## Status
**Accepted** — August 2026

## Context

Several community requests asked whether EricksonLopez.Pagination should provide a `IPaginationRepository<T>` or integrate with the Unit of Work pattern to provide a complete data access solution.

## Problem

Repository and Unit of Work are application architecture patterns. Pagination is an infrastructure concern. Merging them creates:
- Dependency on consumer's domain model structure
- Lock-in to a specific repository implementation
- Coupling between pagination semantics and transaction management

## Decision

EricksonLopez.Pagination **does not provide** Repository, Unit of Work, or any application-layer architecture patterns.

## Why

1. **Scope violation**: Pagination's scope is: "Navigate a set of results." Transactions, identity maps, and change tracking are orthogonal.
2. **Consumer diversity**: Different consumers use Dapper, EF Core, MongoDB, Cosmos. A unified repository abstraction for all providers would be either useless or a new ORM.
3. **Existing solutions**: Ardalis.Specification, Clean Architecture templates, and EF Core's `DbContext` already address this.
4. **Architectural lock-in**: A `IPaginationRepository<T>` forces the consumer's architecture into pagination's preferred pattern.

## Consequences

Consumers compose pagination with their own repositories. Example:
```csharp
// Consumer owns the repository; pagination is applied to IQueryable:
public async Task<PagedList<ProductDto>> GetProductsAsync(PaginationParameters pagination)
{
    return await _context.Products
        .Where(p => p.IsActive)           // Consumer's filter
        .OrderBy(p => p.CreatedAt)        // Consumer's sort
        .ToPagedListAsync(pagination);     // Library handles pagination
}
```

## Reconsideration Criteria

None. This is a permanent architectural boundary.
