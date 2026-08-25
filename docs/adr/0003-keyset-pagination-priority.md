# ADR 0003: Prioritization of Keyset Pagination

## Status
Accepted

## Context
Standard offset pagination (`Skip` and `Take`, or `OFFSET/FETCH`) suffers from severe performance degradation as the page depth increases. Database engines must scan and discard all preceding rows before returning the requested page, leading to O(N) complexity where N is the offset size. This causes catastrophic timeouts on large datasets (e.g., millions of records).

Keyset pagination (also known as Cursor Pagination or the "Seek Method") solves this by using a `WHERE` clause (e.g., `WHERE Id > @LastCursor`) alongside an index, ensuring O(1) performance regardless of the page depth.

## Decision
We decided to natively implement and heavily promote Keyset Pagination within the EF Core extension (`ToCursorPagedListAsync`). The library provides an expression-based abstraction (`Expression<Func<T, TKey>> keySelector`) to automatically generate the necessary `WHERE` and `ORDER BY` clauses for Keyset pagination.

Furthermore, we implemented the "count-less" offset pagination strategy by fetching `PageSize + 1` elements to evaluate `HasNextPage`, actively discouraging the use of `CountAsync` in high-volume endpoints.

## Consequences
- **Positive:** The library enables enterprise-grade performance scaling out-of-the-box.
- **Positive:** Developers are guided towards better architectural practices (avoiding `COUNT(*)`).
- **Negative:** Implementing Keyset Pagination requires the client to handle state (the last cursor) across requests, increasing frontend complexity compared to stateless offset pagination.
