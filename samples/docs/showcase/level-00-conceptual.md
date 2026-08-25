# Level 0 — Conceptual Overview

> **Implementation Reference:** [`Level0Conceptual.cs`](../../DemoApp/DemoApp.Api/Levels/Level0Conceptual.cs)  
> **API Base Route:** `/api/level0`

---

## 1. What is EricksonLopez.Pagination?

`EricksonLopez.Pagination` is a high-performance .NET library (compatible with .NET 8, .NET 9, and .NET 10) providing unified support for:
- **Offset Pagination** (`OFFSET...FETCH` / `LIMIT...OFFSET`) with exact total count or countless ($N+1$ probe) mode.
- **Keyset (Cursor) Pagination** with multi-column type safety, constant $O(\log N)$ complexity, and immunity to data drift.
- **Tamper-Proof Cryptographic Cursors** signed with HMAC-SHA256, configurable expiration (TTL), and replay attack prevention (`ICursorReplayStore`).
- **Dynamic Filter and Sort DSL** (`ApplyFilter`, `ApplySort`) compiled into cached LINQ expression trees (`ExpressionCache`).
- **Native AOT Support** in core abstractions, models, and `System.Text.Json` contexts.
- **Multi-Provider Ecosystem**: Entity Framework Core, Dapper, MongoDB, Azure Cosmos DB, LinqToDB, and gRPC.

---

## 2. What Problems Does It Solve?

In traditional .NET API development, pagination is frequently implemented with ad-hoc and error-prone patterns:
1. **Manual Skip/Take Calculations**: Off-by-one errors when computing `(page - 1) * pageSize`.
2. **Unnecessary Double Queries**: Running expensive `COUNT(*)` queries on every request even when the UI does not display total page numbers.
3. **Severe Offset Degradation at Scale**: Tables with >100,000 rows experience increasing latency when users navigate to deep pages.
4. **Cursor Tampering**: In public APIs, unsigned Base64 cursors can be altered by malicious clients to bypass query boundaries or scrape records.
5. **Dynamic Sort Injection**: Unsafe concatenation of column names in SQL `ORDER BY` clauses.

`EricksonLopez.Pagination` encapsulates these responsibilities through declarative, secure, and optimized APIs.

---

## 3. Offset vs. Keyset (Cursor): Comparative Matrix

| Dimension | Offset Pagination | Keyset (Cursor) Pagination |
|---|---|---|
| **SQL Mechanism** | `ORDER BY Id OFFSET @Skip ROWS FETCH NEXT @Take ROWS` | `WHERE (Price > @LastPrice) OR (Price = @LastPrice AND Id > @LastId) ORDER BY Price, Id LIMIT @Take` |
| **Complexity** | $O(N)$ — Engine scans and discards all previous rows | $O(\log N)$ — Direct B-Tree index seek |
| **Random Page Jump** | ✅ Yes (e.g., jump directly to page 50) | ❌ No (sequential forward/backward navigation) |
| **Total Count (`TotalCount`)** | ✅ Yes (`COUNT(*)` exact or approximate) | ⚠️ Optional / Discouraged on massive tables |
| **Drift Immunity** | ❌ No (concurrent inserts/deletes cause skipped or duplicate items) | ✅ Yes (cursor anchors exact continuation point) |
| **Native AOT Support** | ✅ Yes (100% reflection-free core) | ⚠️ Requires Source Generators for custom decoders |
| **Recommended Use Case** | Admin grids, dashboards, numbered reporting pages | Infinite feeds, high-throughput public APIs, microservices, IoT |

---

## 4. Comparison with Ecosystem Alternatives

| Feature | `EricksonLopez.Pagination` | X.PagedList | Gridify | Sieve |
|---|---|---|---|---|
| **Offset Pagination** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| **Multi-Column Keyset** | ✅ Yes ($O(\log N)$) | ❌ No | ❌ No | ❌ No |
| **HMAC-SHA256 Signed Cursors** | ✅ Yes | ❌ No | ❌ No | ❌ No |
| **Replay Attack Protection** | ✅ Yes (`ICursorReplayStore`) | ❌ No | ❌ No | ❌ No |
| **Cached AST Filter DSL** | ✅ Yes (`ExpressionCache`) | ❌ No | ✅ Yes | ✅ Yes |
| **Roslyn Analyzers (PAG001–PAG007)** | ✅ Yes | ❌ No | ❌ No | ❌ No |
| **Multi-Provider (EF, Dapper, Mongo, Cosmos)** | ✅ Yes | ❌ EF / IEnumerable only | ❌ EF / basic Dapper | ❌ EF Core only |
| **Native AOT Ready** | ✅ Yes | ❌ No | ❌ No | ❌ No |

---

## 5. Showcase Demo Endpoints

- `GET /api/level0/about` — Library metadata and available packages.
- `GET /api/level0/problem` — Problems solved by the library.
- `GET /api/level0/offset-vs-keyset` — Technical comparison between Offset and Keyset.
- `GET /api/level0/comparison` — Feature comparison against .NET ecosystem alternatives.
- `GET /api/level0/tradeoffs` — Architectural tradeoffs and transparency.
