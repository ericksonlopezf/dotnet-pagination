# ADR-0016 — No OData-Style Dynamic Query Language

## Status
**Accepted** — August 2026

## Context

OData (Open Data Protocol) provides a standardized URL convention for querying REST APIs, including filtering (`$filter`), sorting (`$orderby`), pagination (`$skip`/`$top`/`$skiptoken`), and field selection (`$select`).

Requests have been raised for EricksonLopez.Pagination to support an OData-compatible query format, or at minimum, a more expressive filter DSL that approaches OData's power.

## Decision

We **will not** implement OData-style query language or OData protocol compatibility.

## Rationale

### 1. Microsoft owns this space

`Microsoft.AspNetCore.OData` is the canonical OData implementation for ASP.NET Core. It is:
- Maintained by Microsoft with dedicated resources
- Deeply integrated with EF Core's query pipeline
- Supported by a rich ecosystem of client tools (Excel, Power BI, LINQPad)

Competing with a Microsoft-maintained package in its own domain is strategically indefensible. No independent library can match the investment, ecosystem, and longevity of a Microsoft package.

### 2. The Filter DSL already covers the 80% case

The existing filter DSL (`name~=John,age>=18,status:Active`) covers the vast majority of real-world filtering needs:
- Equality, inequality, comparison operators
- String pattern matching (contains, starts with, ends with)
- Logical AND (comma-separated)
- Nested navigation (`customer.city:London`)
- DoS protection (complexity limits, depth limits, length limits)

The remaining 20% of cases (complex OR logic, lambda expressions, `$expand` for related entities) are either application-specific enough to be hand-coded, or are a signal that the API design should be reconsidered.

### 3. OData compatibility is a maintenance trap

Implementing even a subset of OData correctly requires:
- Parsing the full OData URI syntax (a complex grammar)
- Handling `$format`, `$count`, `$select`, `$expand`, `$top`, `$skip`, `$skiptoken`
- Maintaining compatibility as the OData standard evolves
- Handling the semantic differences between OData versions (v3 vs v4)

This is equivalent to implementing a small standards body's specification inside a pagination library. The maintenance burden is disproportionate to the benefit.

### 4. Positioning conflict

EricksonLopez.Pagination is positioned as a **database pagination library**, not a query protocol framework. Supporting OData would shift the library's identity toward being a general-purpose query layer, which is a different product with different trade-offs.

## Alternatives Considered

- **OData `$skiptoken` cursor support only**: Could be valuable for OData-compatible APIs that want keyset pagination. Documented as a future possibility but not a current roadmap item. If demand is verified (>10 GitHub issues), this specific sub-feature may be reconsidered as `EricksonLopez.Pagination.ODataSkipToken`.

## Consequences

Developers requiring full OData support should use `Microsoft.AspNetCore.OData`. Developers who need filtering + pagination without OData overhead should use EricksonLopez.Pagination's Filter DSL. These are different products for different requirements.

The FAQ in `docs/faq.md` will document this decision and redirect OData users to the appropriate package.
