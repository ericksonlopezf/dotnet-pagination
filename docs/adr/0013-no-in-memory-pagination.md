# ADR-0013 — No In-Memory (IEnumerable) Pagination

## Status
**Accepted** — August 2026

## Context

The team evaluated whether to implement in-memory pagination over `IEnumerable<T>` as a complement to the existing database-backed offset and keyset pagination.

**X.PagedList** (the most popular .NET pagination library, ~80M NuGet downloads) natively supports in-memory pagination over any `IEnumerable<T>` or `IQueryable<T>`. Several community requests have asked whether EricksonLopez.Pagination offers the same.

## Decision

We **will not** implement `IEnumerable<T>` in-memory pagination.

## Rationale

### 1. Competitive reality

X.PagedList dominates this use case with 80M+ downloads and 10+ years of production use. There is no realistic path to gain significant market share against an established, well-maintained library in its strongest domain.

### 2. Positioning conflict

EricksonLopez.Pagination is positioned as **high-performance, database-backed pagination** with keyset/cursor support, HMAC security, and cross-persistence consistency. Adding in-memory pagination:
- Signals to developers that the library is a general-purpose paginator, not a specialized infrastructure tool.
- Invites direct feature comparison with X.PagedList on X.PagedList's strongest ground.
- Dilutes the core message: "pagination that scales when your dataset grows."

### 3. The wrong problem

In-memory pagination implies the entire dataset is already loaded in memory. This is an anti-pattern for datasets of any meaningful size. EricksonLopez.Pagination is designed for the opposite: to **avoid** loading full datasets into memory by executing optimized queries at the database level.

### 4. Maintenance cost

Adding a new package surface (`EricksonLopez.Pagination.InMemory` or similar) adds ongoing maintenance burden — tests, API compatibility, documentation — with no corresponding competitive benefit.

## Alternatives Considered

- **Thin wrapper over `Skip`/`Take` on `IEnumerable<T>`**: Rejected. This is already trivially achievable without any library. Any developer who needs this can use `source.Skip(offset).Take(pageSize).ToList()`.
- **Extension method in core package**: Rejected. Would contaminate the core API with a use case that conflicts with the library's design philosophy.

## Consequences

Developers who need in-memory pagination should use **X.PagedList** (`X.PagedList` NuGet package). This will be documented explicitly in the README's FAQ section.

EricksonLopez.Pagination focuses exclusively on database-backed pagination, where it has a genuine and defensible technical advantage.
