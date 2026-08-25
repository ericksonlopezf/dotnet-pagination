# ADR-0024 — Filtering Responsibility Boundary

## Status
**Accepted** — August 2026

## Context

The library ships with a dynamic filter DSL (`ApplyFilter(string)`) in the EFCore provider package. This creates an implicit coupling between pagination and filtering that conflicts with the library's core design principle: pagination does not know about filtering.

## Problem

When developers use the filter DSL, they may perceive the library as a "filter + sort + paginate" framework (like Gridify or Sieve), not a pagination infrastructure library. This creates:
1. Positioning confusion
2. AOT incompatibility surfacing in the pagination core (ADR-0010)
3. Difficulty separating filter/sort from pagination in CQRS architectures

## Decision

The filter DSL is **an extension**, not a pagination feature. The library's documentation must clearly separate:
- Pagination semantics (the library's core mission)
- Filtering (an orthogonal concern, provided as a convenience extension)

Additionally, the library will provide `IFilterProvider<TEntity>` as the AOT-safe alternative to the dynamic filter DSL, allowing consumers who need AOT compatibility to opt out of reflection-based filtering.

## Why

Pagination operates on an ordered dataset. Filtering determines which dataset to paginate. These are composed by the consumer:

```
IQueryable<T> (full table)
    → .Where(spec.Predicate)    (filtering — Specification's concern)
    → .OrderBy(...)             (sorting — consumer's concern)
    → .ToPagedListAsync(params) (pagination — library's concern)
```

## Consequences

- Documentation clearly labels filtering as an extension.
- `IFilterProvider<TEntity>` documented as the AOT-safe path.
- Future: consider extracting filter DSL into `EricksonLopez.Specification.EFCore`.

## Reconsideration Criteria

If `EricksonLopez.Specification` is built with an EFCore provider, evaluate whether the filter DSL should migrate there.
