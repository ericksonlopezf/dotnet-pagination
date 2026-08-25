# ADR-0025 — GraphQL Integration Scope

## Status
**Accepted** — August 2026

## Context

GraphQL APIs use the Relay cursor connection specification (edges, nodes, pageInfo) for pagination. The question arose whether to add a dedicated `EricksonLopez.Pagination.GraphQL` package.

## Problem

HotChocolate (the dominant .NET GraphQL server) natively implements Relay cursor connections. Adding a competing GraphQL pagination package would create conflicts with HotChocolate's built-in pagination infrastructure.

## Decision

GraphQL is **out of scope** for EricksonLopez.Pagination as a dedicated package. The library's `CursorPagedList<T>` already provides the data structure needed for GraphQL cursor-based pagination, but **does not impose** GraphQL conventions.

## Why

1. **HotChocolate dominates**: Adding a `GraphQL` package creates a dependency conflict with HotChocolate's own cursor pagination. Developers would need to choose which to use.
2. **Relay connections are a response format**: The library provides pagination state; Relay connections are a response shaping concern.
3. **`CursorPagedList<T>` maps naturally**: A Relay connection can be constructed from `CursorPagedList<T>.Items`, `.HasNextPage`, `.HasPreviousPage`, `.NextCursor`, `.PreviousCursor`. No library code needed.

## Consequences

Consumers using HotChocolate should use HotChocolate's `[UsePaging]` attribute or `Queryable.OrderBy().ToCursorPagedListAsync()` feeding into a Relay connection builder.

```csharp
// Consumer maps without a library package:
var cursorPage = await query.Keyset(cursor).Ascending(x => x.Id).ToCursorPagedListAsync();
var connection = new Connection<ProductDto>(
    cursorPage.Items.Select((item, i) => new Edge<ProductDto>(item, cursorPage.NextCursor)),
    new PageInfo(cursorPage.HasNextPage, cursorPage.HasPreviousPage, ...));
```

## Reconsideration Criteria

If significant community demand exists for a thin interop package that converts `CursorPagedList<T>` to Relay connection types, consider a `EricksonLopez.Pagination.Relay` package with no HotChocolate dependency.
