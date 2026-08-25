# ADR-0015 — No HATEOAS Builder

## Status
**Accepted** — August 2026

## Context

HATEOAS (Hypermedia as the Engine of Application State) is a REST constraint where responses include links to related actions. For paginated responses, this means embedding `next`, `prev`, `first`, and `last` links in the response body alongside the data.

Several developers have requested a `ToHateoasPagedResult()` or `AddHateoasLinks()` extension method.

## Decision

We **will not** implement a HATEOAS builder in EricksonLopez.Pagination.

## Rationale

### 1. Core design principle: zero knowledge of HTTP/routing

The core package (`EricksonLopez.Pagination`) explicitly has zero dependencies on ASP.NET Core, HTTP, or any web framework. This is a design pillar documented in `docs/adr/0001-core-and-orm-architecture.md`.

HATEOAS link generation requires knowledge of:
- The current HTTP verb and route
- The route template and route parameters
- The application's URL generation strategy (`IUrlHelper`, `LinkGenerator`)
- Whether the request is HTTP or gRPC

This is application-layer knowledge that cannot exist in a pagination library without introducing tight coupling to the web framework.

### 2. The diversity problem

Different applications structure their HATEOAS links differently:
- `{ "links": [{ "rel": "next", "href": "..." }] }` (HAL-style)
- `{ "_links": { "next": { "href": "..." } } }` (HAL)
- `{ "next": "https://api.example.com/products?page=3" }` (flat)
- JSON:API links format

A HATEOAS builder in the library would be either too opinionated (breaking applications that use a different format) or too generic (useless without significant configuration).

### 3. Correct responsibility assignment

HATEOAS link generation belongs to the **application layer** (controller or endpoint handler), not to an infrastructure library. The application knows its routes; the library does not.

The correct implementation pattern is:
```csharp
var paged = await dbContext.Products.ToPagedListAsync(request, ct);
return new
{
    Items = paged.Items,
    Links = new
    {
        Next = paged.HasNextPage ? linkGenerator.GetUriByName(ctx, "GetProducts", new { page = paged.PageNumber + 1 }) : null,
        Prev = paged.HasPreviousPage ? linkGenerator.GetUriByName(ctx, "GetProducts", new { page = paged.PageNumber - 1 }) : null
    }
};
```

This is already possible with the existing `IPagedList<T>` interface. No additional library support is needed.

## Alternatives Considered

- **Optional ASP.NET Core extension package (`EricksonLopez.Pagination.Hateoas`)**: Rejected. The complexity of handling different HATEOAS formats, URL generation strategies, and cursor vs. offset links makes this a non-trivial, high-maintenance package that would be used by a small fraction of adopters.

## Consequences

HATEOAS link generation is documented as an application-layer concern in `docs/cookbook.md`, with code examples showing how to compose links using `LinkGenerator` and the `IPagedList<T>` / `ICursorPagedList<T>` response models.
