# ADR-0021 — No Dynamic Filtering in Core

## Status
**Accepted** — August 2026

## Context

The `FilterExpression` engine (dynamic filter DSL) lives in `EricksonLopez.Pagination.EntityFrameworkCore` but was considered for promotion to the core package. The question arose whether filtering should be a first-class pagination concern.

## Problem

Filtering and pagination are orthogonal concerns:
- **Pagination** answers: "Given this ordered set of results, return items N through M."
- **Filtering** answers: "Which items match these criteria?"

Combining them in a single package violates the Single Responsibility Principle and creates coupling between the pagination core and EF Core's expression tree API.

## Decision

Dynamic filtering **remains an extension** in `EricksonLopez.Pagination.EntityFrameworkCore`. The core package has zero filtering responsibility.

## Why

1. **Architectural purity**: Pagination does not need to know what was filtered. It receives an `IQueryable<T>` and paginates it.
2. **AOT incompatibility**: The filter DSL uses reflection and `MakeGenericType`. Keeping it in the core would make the core AOT-incompatible (see ADR-0010).
3. **Provider coupling**: Dynamic filtering over `IQueryable<T>` is inherently EF Core-specific. Dapper filtering requires raw SQL predicates.
4. **Specification pattern**: For complex filter scenarios, the Specification pattern (`EricksonLopez.Specification`) is the correct abstraction, not the filter DSL.

## Consequences

- Core remains AOT-first and provider-agnostic.
- Filter DSL consumers must reference `EricksonLopez.Pagination.EntityFrameworkCore`.
- Consumers using Dapper must write their own WHERE clauses.

## Rejected Alternatives

- **Core filter abstraction (IFilter<T>)**: Would require the core to define filtering contracts, coupling it to expression trees.
- **Separate EricksonLopez.Filtering package**: Valid future direction if filtering grows beyond pagination, but premature now.

## Reconsideration Criteria

If `EricksonLopez.Specification` is built and provides a clean filter abstraction that integrates naturally with pagination, consider adding a thin adapter. Do not add filtering to core.
