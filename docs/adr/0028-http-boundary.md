# ADR-0028 — HTTP Boundary: What the Library Does and Does Not Own

## Status
**Accepted** — August 2026

## Context

Pagination affects the HTTP layer: query string parameters, response shapes, Link headers, ETag headers, and HATEOAS links are all related to pagination. The question arose how much of the HTTP layer the library should own.

## Problem

The library walks a line between providing useful HTTP integration (model binders, response wrappers) and overstepping into application-layer territory (HATEOAS, caching, routing).

## Decision

The library owns exactly:
- **HTTP IN**: Model binding of pagination parameters from query strings (`?page=2&pageSize=20`, `?after=cursor&first=20`)
- **HTTP OUT**: Pagination metadata in response body (`page`, `pageSize`, `totalCount`, `nextCursor`, `hasNext`)
- **HTTP OUT**: ETag headers based on cursor/page state

The library does NOT own:
- **Link headers (RFC 8288)**: Next/previous links require routing knowledge (URL construction)
- **HATEOAS**: Link relation construction requires the application's URL scheme
- **Cache-Control headers**: Caching strategy is application-specific
- **Rate limiting**: Pagination request rate limiting is infrastructure concern

## Why: HATEOAS Specifically (ADR-0015)

HATEOAS link builder was formally rejected in ADR-0015. To build `next: https://api.example.com/products?page=3`, the library would need:
- The current request's base URL
- The route template
- All query parameters (not just pagination parameters)
- Knowledge of whether to use `?page=3` or `?after=cursor`

This knowledge belongs to the ASP.NET Core endpoint, not the pagination library.

## Consequences

- HTTP response includes pagination metadata as a JSON body (not Link headers).
- Consumers who need RFC 8288 Link headers must construct them using the `IPagedList<T>` metadata provided by the library.
- A cookbook entry documents how to add Link headers in an ASP.NET Core middleware.

## Reconsideration Criteria

If >20 GitHub issues request Link header support with a clear proposal for how to provide routing context, reconsider via a new ADR.
