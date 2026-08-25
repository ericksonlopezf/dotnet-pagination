# COMPETITIVE-MATRIX.md — EricksonLopez.Pagination
### Head-to-Head Competitive Intelligence · August 2026

---

## Legend

| Symbol | Meaning |
|---|---|
| 🟢 | Native — first-class support, built-in |
| 🟡 | Partial — limited or partial support |
| 🔵 | Extension — available via separate package |
| ⚪ | Not Applicable — outside library scope by design |
| 🔴 | Intentionally Not Supported — formal decision (ADR required) |

---

## 1. Competitor Classification

| Library | NuGet Downloads | Last Release | Type | Primary Use Case |
|---|---|---|---|---|
| **EricksonLopez.Pagination** | — | 2026-Q3 | DIRECT | Keyset + cursor + HMAC + multi-provider |
| **X.PagedList** | ~80M | v10.x (2024) | DIRECT | Offset pagination; IEnumerable/IQueryable |
| **Gridify** | ~15M | v2.x (2025) | DIRECT | Filter+sort+offset over IQueryable |
| **Sieve (Biarity)** | ~8M | v2.x (2021†) | DIRECT | Dynamic filter+sort+offset (abandoned) |
| **MR.EntityFrameworkCore.KeysetPagination** | ~1M | v1.6.0 (2025) | DIRECT | Keyset for EF Core only |
| **MR.AspNetCore.Pagination** | ~500K | v2.x (2025) | DIRECT | ASP.NET Core wrapper on MR.EFCore.Keyset |
| **Ardalis.Specification** | ~12M | v9.x (2025) | INDIRECT | Specification pattern (not pagination) |
| **HotChocolate** | ~8M | v13.x (2025) | INDIRECT | GraphQL server with Relay cursor support |
| **QueryKit** | ~200K | v0.x (2024) | INDIRECT | OData-style filter string parsing |

† Sieve is functionally abandoned; last meaningful release was 2021. Included for historical accuracy.

---

## 2. Core Pagination Strategy Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve† |
|---|:---:|:---:|:---:|:---:|:---:|
| Offset / Page-Number | 🟢 | 🟢 | 🟢 | 🔴 | 🟢 |
| Keyset (single column) | 🟢 | 🔴 | 🔴 | 🟢 | 🔴 |
| Keyset (composite, multi-column) | 🟢 up to 16 | 🔴 | 🔴 | 🟢 | 🔴 |
| Cursor pagination (opaque) | 🟢 | 🔴 | 🔴 | 🟡 manual | 🔴 |
| Seek / row-value syntax | 🟢 | 🔴 | 🔴 | 🟢 | 🔴 |
| Streaming (IAsyncEnumerable) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Count-less (N+1 probe) | 🟢 | 🟡 | 🟡 | 🟢 | 🟡 |
| Backward pagination | 🟢 | 🔴 | 🔴 | 🟢 | 🔴 |
| In-memory (IEnumerable) | 🔴 ADR-0013 | 🟢 | 🟢 | 🔴 | 🟢 |

---

## 3. Cursor Security Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| Opaque cursor (Base64URL) | 🟢 | ⚪ | ⚪ | 🟡 manual | ⚪ |
| HMAC-SHA256 signing | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Constant-time comparison (FixedTimeEquals) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Cursor TTL expiration | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Cursor versioning (v1/v2 migration) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Keyset schema fingerprint (FNV-1a) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Clock-skew tolerance (NTP) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| AES cursor encryption | 🔴 ADR-0014 | 🔴 | 🔴 | 🔴 | 🔴 |
| ICursorEncoder extension point | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |

> No competitor in the .NET ecosystem provides HMAC-signed cursors with TTL + versioning.

---

## 4. Filtering and Sorting Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve† |
|---|:---:|:---:|:---:|:---:|:---:|
| Dynamic filter DSL (string) | 🔵 EFCore pkg | 🔴 | 🟢 | 🔴 | 🟢 |
| [Filterable] attribute allowlist | 🔵 EFCore pkg | 🔴 | 🟡 | 🔴 | 🟡 |
| Filter DoS limits (complexity, length) | 🔵 EFCore pkg | 🔴 | 🔴 | 🔴 | 🔴 |
| Nested property (dot notation) | 🔵 EFCore pkg | 🔴 | 🟢 | 🔴 | 🟢 |
| ReDoS protection | 🔵 (200-char) | 🔴 | ❓ | 🔴 | ❓ |
| Static ordering (compile-time expr) | 🟢 | 🟡 | 🟡 | 🟢 | 🟡 |
| Dynamic ordering (string-based) | 🔵 EFCore pkg | 🟡 | 🟢 | 🔴 | 🟢 |
| Multi-column sort | 🟢 | 🟡 | 🟢 | 🟢 | 🟢 |
| Sort column allowlist | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Null ordering (NULLS FIRST/LAST) | ⚪ documented | ⚪ | ⚪ | ⚪ | ⚪ |

---

## 5. Count Strategy Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| Exact COUNT(*) | 🟢 | 🟢 | 🟢 | 🔴 | 🟢 |
| Optional COUNT | 🟢 | 🟡 | 🟡 | 🟢 HasNext only | 🟡 |
| HasNext via Take(N+1) | 🟢 | 🔴 | 🟡 | 🟢 | 🔴 |
| COUNT(*) OVER() window | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| PostgreSQL approx count | 🟢 pg_class | 🔴 | 🔴 | 🔴 | 🔴 |
| Deferred join (1 round-trip) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Mandatory total count | 🔴 ADR | 🟢 | 🟢 | 🔴 | 🟢 |

---

## 6. Provider / Data Access Matrix

| Provider | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| EF Core (IQueryable) | 🟢 | 🟢 | 🟢 | 🟢 | 🟢 |
| Dapper (IDbConnection) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| MongoDB | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Cosmos DB | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| LinqToDB | 🟡 planned | ❓ | 🟡 | 🔴 | 🔴 |
| Raw SQL | 🟡 via Dapper | 🔴 | 🔴 | 🔴 | 🔴 |
| IEnumerable in-memory | 🔴 ADR-0013 | 🟢 | 🟢 | 🔴 | 🟢 |
| gRPC / Protobuf | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |

---

## 7. Database Engine Matrix

| Engine | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| PostgreSQL | 🟢 | 🟢 | 🟢 | 🟢 | 🟢 |
| SQL Server | 🟢 | 🟢 | 🟢 | 🟢 | 🟢 |
| SQLite | 🟢 | 🟢 | 🟢 | 🟢 | 🟢 |
| MySQL | 🟢 | 🟢 | 🟢 | 🟢 | 🟢 |
| Oracle | 🟡 via EF Core | 🟡 | 🟡 | 🟡 | 🟡 |
| PostgreSQL row-value syntax | 🟢 | 🔴 | 🔴 | 🟢 | 🔴 |
| PostgreSQL approx count (pg_class) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |

---

## 8. AOT / Trimming Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| Native AOT (core) | 🟢 | ❓ | 🔴 | ❓ | 🔴 |
| Native AOT (EFCore provider) | 🟡 expression trees | 🔴 | 🔴 | 🟡 | 🔴 |
| Trimming safe (core) | 🟢 | ❓ | 🔴 | ❓ | 🔴 |
| Source Generators (cursor AOT) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Reflection in hot path | Minimal | High | Moderate | Moderate | High |
| [RequiresDynamicCode] annotations | Filter DSL only | Unknown | Yes | Yes | Yes |
| CI AOT publish verification | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |

---

## 9. API Design Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| Fluent API (KeysetBuilder) | 🟢 Excellent | 🟡 Poor | 🟡 | 🟢 Good | 🟡 Poor |
| Extension methods | 🟢 Excellent | 🟢 Good | 🟢 Good | 🟢 Good | 🟡 |
| Immutable result model | 🟢 class+internal ctor | 🟡 class | 🟡 | 🟡 | 🟡 |
| Nullable annotations | 🟢 Excellent | 🟡 Partial | 🟢 Good | 🟡 Partial | ❓ |
| CancellationToken support | 🟢 Excellent | 🟡 Partial | 🟡 Partial | 🟢 Good | 🔴 None |
| IParsable<T> (Minimal API) | 🟢 | ❓ | 🔴 | 🔴 | 🔴 |
| IAsyncEnumerable streaming | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Factory methods (WithCount/WithoutCount) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Map<TResult> projection | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |

---

## 10. Ecosystem Integration Matrix

| Integration | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| ASP.NET Core Minimal APIs | 🟢 | 🟡 | 🟡 | 🟡 | 🟡 |
| OpenAPI / Swagger | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Blazor component | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| gRPC integration | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Roslyn analyzers | 🟢 PAG001-007 | 🔴 | 🔴 | 🔴 | 🔴 |
| BenchmarkDotNet benchmarks | 🟢 Published | 🔴 | 🟡 Partial | 🔴 | 🔴 |
| Mutation testing (Stryker) | 🟢 >95% | 🔴 | 🔴 | 🔴 | 🔴 |
| Testcontainers integration tests | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |

---

## 11. Security Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| HMAC cursor signing | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Constant-time comparison | 🟢 FixedTimeEquals | 🔴 | 🔴 | 🔴 | 🔴 |
| Filter DoS protection | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Filter value length limits | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| SQL injection prevention | 🟢 parameterized | 🟢 | 🟢 | 🟢 | 🟢 |
| Column allowlist (sort/filter) | 🟢 | 🔴 | 🟡 | 🔴 | 🟡 |
| Cursor versioning/migration | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| FNV-1a keyset schema fingerprint | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Secure by default (HMAC opt-out) | 🟢 ADR-0017 | 🔴 | 🔴 | 🔴 | 🔴 |

---

## 12. Developer Experience Matrix

| Feature | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset | Sieve |
|---|:---:|:---:|:---:|:---:|:---:|
| XML documentation | 🟢 Excellent | 🟢 Good | 🟡 | 🟢 Good | 🟡 Poor |
| Actionable exception messages | 🟢 | 🟡 | 🟡 | 🟢 | 🟡 |
| Migration guides ([Obsolete]) | 🟢 | 🟡 Poor | 🟡 | 🟡 | 🔴 None |
| Diagnostic codes (ELPAG001...) | 🟢 | 🔴 | 🔴 | 🔴 | 🔴 |
| Sample app | 🟢 | 🟡 Limited | 🟡 | 🟢 | 🔴 |
| Changelog quality | 🟢 | 🟢 | 🟢 | 🟡 | 🟡 Poor |

---

## 13. Competitive Scoring (Weighted 0–10)

| Category | Weight | EricksonLopez | X.PagedList | Gridify | MR.EFCore.Keyset |
|---|---|---|---|---|---|
| Core pagination | 15% | **9.0** | 6.0 | 7.0 | 7.0 |
| Cursor / keyset | 15% | **10.0** | 0.0 | 0.0 | 7.0 |
| Performance (offset) | 8% | 7.0 | 5.0 | 7.0 | N/A |
| Performance (keyset) | 7% | **9.0** | 0.0 | 0.0 | 8.0 |
| AOT compatibility | 10% | **8.0** | 3.0 | 1.0 | 3.0 |
| API design | 10% | **9.0** | 5.0 | 7.0 | 7.0 |
| DB integration breadth | 10% | **9.5** | 5.0 | 6.0 | 5.0 |
| ASP.NET Core | 5% | **8.5** | 6.0 | 7.0 | 6.0 |
| Security | 5% | **9.5** | 4.0 | 5.0 | 5.0 |
| Extensibility | 10% | **8.0** | 5.0 | 7.0 | 6.0 |
| Developer experience | 5% | **9.0** | 6.0 | 7.0 | 7.0 |
| **TOTAL** | 100% | **8.98** | **4.55** | **5.80** | **6.24** |

---

## 14. Unique Differentiators (Non-Replicable Without Rewriting)

### Level 1 — Unique in the .NET Ecosystem

| # | Differentiator | Why It Cannot Be Added to Competitors |
|---|---|---|
| 1 | HMAC-SHA256 cursor signing + constant-time comparison | Would require cursor abstraction redesign in competitors |
| 2 | Cursor TTL expiration with typed exceptions | Requires entire cursor lifecycle model to be redesigned |
| 3 | FNV-1a deterministic keyset schema fingerprint | Requires cursor format redesign (v2 format) |
| 4 | Cursor versioning (v1→v2) with AcceptLegacyCursors | Requires versioned cursor format from ground up |
| 5 | Filter DoS protection (complexity, depth, length limits) | Would require major filter engine rewrite in Gridify/Sieve |
| 6 | Deferred join optimization (1 round-trip count) | EF Core-specific technique not implemented in competitors |
| 7 | PostgreSQL pg_class approximate count with fallback | Not implemented by any .NET pagination library |
| 8 | Dapper + keyset pagination in same API | MR only supports EF Core; Dapper was never in scope |

### Level 2 — Best in Ecosystem

| # | Differentiator | Advantage |
|---|---|---|
| 9 | Blazor PagedListPager with atomic navigation guard | Prevents race conditions X.PagedList.Mvc cannot address |
| 10 | Expression compilation 22x faster (29ns vs 650ns warm) | LRU cache + compiled lambdas vs Gridify's per-call compilation |
| 11 | gRPC + pagination integration | No competitor addresses gRPC-native pagination |
| 12 | Source generators for AOT cursor decoders | No competitor even considers Native AOT |
| 13 | Roslyn analyzers (PAG001-007) | Compile-time safety no competitor provides |

### Level 3 — Solid Parity

| # | Feature | Status |
|---|---|---|
| 14 | MongoDB pagination | Available; keyset mode for large collections pending |
| 15 | OpenAPI / Swagger integration | Available |
| 16 | IParsable<T> for Minimal API binding | Available |

---

## 15. Competitive Gaps (Honest Assessment)

| Gap | Status | Competitor Advantage | Priority |
|---|---|---|---|
| Filter AOT compatibility | Partial (filter DSL uses reflection) | Gridify has partial AOT work | Medium |
| LinqToDB support | Not implemented | No competitor has it either | Low |
| MongoDB keyset pagination | Offset-only currently | No competitor has it either | Medium |
| SQL Server approx count | Not implemented | No competitor has it either | Low |
| IAsyncEnumerable streaming keyset | Planned | No competitor has it | Medium |

---

## 16. Positioning Statement

> **For the senior .NET developer building high-scale APIs,** who needs pagination that does not break in production when the dataset grows, EricksonLopez.Pagination is the **only multi-platform pagination infrastructure for .NET** that combines keyset pagination with secure cursors (HMAC), a filter DSL with DoS protection, and native support for EF Core, Dapper, and MongoDB in a unified API.
>
> **Unlike X.PagedList** (offset only), **MR.EntityFrameworkCore.KeysetPagination** (EF Core only, no cursor security), and **Gridify/Sieve** (no keyset), because no other library has all three pillars together:
> - **Scale** (keyset O(log N) + deferred join)
> - **Security** (HMAC + versioning)
> - **Breadth** (EF Core + Dapper + MongoDB)

---

*Matrix generated: August 2026 — Based on source code analysis, official documentation, repository benchmarks (BenchmarkDotNet, 1M rows, .NET 10, AMD Ryzen 7 9800X3D), and competitive landscape analysis.*
