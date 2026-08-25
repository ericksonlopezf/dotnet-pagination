# ADR-0023 — No Caching Layer

## Status
**Accepted** — August 2026

## Context

For high-traffic paginated endpoints, caching paginated results (Redis, in-memory) is a common optimization. The question arose whether the pagination library should provide caching primitives.

## Problem

Caching is a cross-cutting concern that operates at the application boundary, not the pagination boundary. Paginated results are highly variable (every filter, sort, and page combination produces a different result set), making cache key design application-specific.

## Decision

EricksonLopez.Pagination **does not provide** any caching layer, cache key generation, or cache invalidation.

## Why

1. **Cache key design is application-specific**: The correct cache key depends on authentication, tenancy, filters, sort order, and page position — all of which the library does not control.
2. **Cache invalidation requires domain knowledge**: When to invalidate a cached page depends on domain events (product updated, order created), which the pagination library cannot know.
3. **IDistributedCache / IMemoryCache already exists**: ASP.NET Core provides complete, well-understood caching infrastructure.
4. **Caching defeats pagination performance work**: Caching offset pagination results hides the O(N) degradation problem. The correct solution is keyset pagination, not caching.

## Consequences

Consumers who need response caching should use ASP.NET Core Output Caching or IDistributedCache with application-generated cache keys. Pagination provides stable cursor tokens that can serve as cache key components.

## Reconsideration Criteria

None. This is a permanent scope boundary.
