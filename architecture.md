# architecture.md — EricksonLopez.Pagination
### Technical Architecture Reference · August 2026

---

## 1. Core Philosophy

> **Pagination should model pagination semantics, not database execution or HTTP transport.**

The library is built on these non-negotiable principles:

1. **Persistence agnostic in the core** — Core types (`PagedList<T>`, `CursorPagedList<T>`, `ICursorEncoder`) have zero database dependencies.
2. **No HTTP coupling in core** — Pagination semantics exist independently of HTTP, query strings, or REST response shapes.
3. **Explicit over magic** — No implicit COUNT queries, no hidden SQL, no automatic sorting.
4. **Deterministic pagination** — Every keyset query must have a unique tiebreaker; non-deterministic ordering is detected at compile time via Roslyn analyzers.
5. **AOT-first where technically reasonable** — Core types, cursor encoding, and result models are Native AOT compatible. EF Core providers are AOT-compatible with documented expression tree limitations.
6. **Minimal allocations** — `stackalloc` for cursor encoding (.NET 8+), `ArrayPool` for filter building, factory constructors to prevent invalid state.
7. **Immutable results** — `PagedList<T>` constructor is `internal`; factory methods enforce invariants.

---

## 2. Pagination Strategy Taxonomy

The library uses strict terminology. Do not treat these as synonyms.

### 2.1 Offset Pagination
```sql
SELECT * FROM Items
ORDER BY Name
OFFSET 40 LIMIT 20
```
**Characteristics**: Simple; random page access; O(N) cost as offset grows; concurrent mutation causes skips/duplicates; `TotalCount` requires separate `COUNT(*)` query.

**When to use**: Small datasets (<100K rows); UI requires page number input; total count display is required.

**Real cost**: At page 10,000 with pageSize=100 on 1M rows → PostgreSQL ~150ms; SQL Server ~180ms. Raw SQL baseline: ~12ms. Inherent OFFSET behavior, not a library bug.

### 2.2 Keyset Pagination
```sql
SELECT * FROM Items
WHERE CreatedAt >= @p1
  AND (CreatedAt > @p1 OR (CreatedAt = @p1 AND Id > @p2))
ORDER BY CreatedAt ASC, Id ASC
LIMIT 20
```
**Characteristics**: O(log N) with proper index; deterministic; requires unique tiebreaker; cannot jump to arbitrary pages; previous cursor requires ORDER BY reversal.

**Bounding condition** (ADR-0019): The `CreatedAt >= @p1` prefix guarantees the optimizer uses an Index Seek on the primary sort column, dropping query time from ~10ms to ~2.2ms at deep pages.

**When to use**: Large datasets (>100K rows); feeds, timelines, logs; no random page access needed.

### 2.3 Seek Pagination
A subset of keyset pagination using **row-value comparison syntax**:
```sql
WHERE (CreatedAt, Id) > (@createdAt, @id)
```
Available on PostgreSQL and SQL Server 2022+. EricksonLopez generates this via `BuildRowValuePredicate()` when the provider supports it.

### 2.4 Cursor Pagination
Keyset pagination with an **opaque token representation** of the keyset position.

**The cursor is not the strategy** — it is the transport encoding of the keyset position:
```
Strategy: Keyset (WHERE predicate)
    ↓
State: { CreatedAt: "2024-01-15T10:30:00Z", Id: 4521 }
    ↓
Encoding: Base64URL of pipe-delimited string
    ↓
Signing: HMAC-SHA256 over encoded payload + expiry
    ↓
Cursor token: "M|v2|2A3F1B|2024-01-15T10...|4521|<signature>"
```

### 2.5 Strategy vs. State vs. Encoding

| Concern | Who Owns | Example |
|---|---|---|
| Pagination strategy | `KeysetBuilder<T>` | WHERE predicate construction |
| Position state | Cursor payload | { CreatedAt, Id } values |
| Position encoding | `ICursorEncoder` | Base64URL string |
| Transport security | `HmacCursorEncoder` | HMAC signature + TTL |
| HTTP representation | `CursorPagedResponse` | { nextCursor: "..." } |

---

## 3. Pagination Layers

```
┌─────────────────────────────────────────────────────────────┐
│  Application Layer (HTTP, gRPC, Blazor)                     │
│  PaginationParameters, CursorPaginationParameters           │
│  (Parsed from query string / Protobuf / UI component)       │
└────────────────────────────┬────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│  Core Library                                               │
│  PagedList<T>, CursorPagedList<T>                           │
│  ICursorEncoder, HmacCursorEncoder                          │
│  PaginationCoreOptions                                      │
└────────────────────────────┬────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│  Provider Layer (EFCore, Dapper, MongoDB, Cosmos)           │
│  KeysetBuilder<T>, ToPagedListAsync, DapperKeysetBuilder<T> │
└────────────────────────────┬────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│  Database (PostgreSQL, SQL Server, SQLite, MySQL, MongoDB)   │
│  SQL queries executed here; results returned to provider    │
└─────────────────────────────────────────────────────────────┘
```

**Invariant**: The core library never opens a connection or executes a query.

---

## 4. Package Architecture

### 4.1 Package Dependency Graph

```
EricksonLopez.Pagination.Abstractions   (zero deps)
        |
EricksonLopez.Pagination (Core)         (Abstractions + Options + Logging.Abstractions)
        |
   ┌────┼────────────────────────────────────┐
   |    |                                    |
   ▼    ▼                                    ▼
EFCore  Dapper  MongoDB  Cosmos  AspNetCore  OpenApi
   |                             |
   └─────────────────────────────┘
                 |
             Blazor (depends on Abstractions only)
             Grpc   (depends on Abstractions only)

Compile-time only (no runtime deps):
  SourceGenerators (Roslyn; netstandard2.0)
  Analyzers        (Roslyn; netstandard2.0)
```

### 4.2 Package Justification

| Package | Purpose | Consumer |
|---|---|---|
| `Abstractions` | `IPagedList<T>`, `ICursorPagedList<T>`, `ICursorEncoder` | Domain / application projects |
| `Core` | `PagedList<T>`, `HmacCursorEncoder`, `PaginationCoreOptions` | Application projects |
| `EntityFrameworkCore` | Keyset builder, ToPagedListAsync, filter/sort DSL | EF Core data access |
| `Dapper` | IDbConnection extensions, DapperKeysetBuilder<T> | Raw SQL / stored proc access |
| `MongoDB` | IFindFluent extensions, cursor pagination | MongoDB-backed applications |
| `Cosmos` | Continuation token pagination | Azure Cosmos DB backends |
| `AspNetCore` | Model binders, response envelopes, ETag | REST API projects |
| `OpenApi` | Swagger operation transformer | API documentation |
| `Blazor` | PagedListPager component | Blazor Server / WASM |
| `Grpc` | Protobuf message converters | gRPC service endpoints |
| `SourceGenerators` | AOT cursor decoder registration | Native AOT publishing |
| `Analyzers` | Compile-time safety: PAG001-007 | All consuming projects |

---

## 5. Cursor Architecture

### 5.1 Cursor Format (v2 — Current)

```
M|v2|<fingerprint>|<col1>|<col2>|...|<colN>
```

Where:
- `M` — cursor marker
- `v2` — version identifier
- `<fingerprint>` — FNV-1a 32-bit hash of (column FullName + IsAscending) for cross-pod schema validation
- `<col1..N>` — pipe-delimited string representations of keyset column values

The entire string is then Base64URL-encoded and optionally HMAC-SHA256 signed.

### 5.2 Cursor v1 (Deprecated)

```
M|<col1>|<col2>|...|<colN>
```

No fingerprint; accepted only when `PaginationCoreOptions.AcceptLegacyCursors = true` (default: true).

### 5.3 HMAC Envelope

When `HmacCursorEncoder` is active:
```
base64url(<rawCursorPayload>|<unixExpiryTimestamp>).<hmacSHA256Signature>
```

The HMAC covers the full base64url payload + expiry. Signature verification uses `CryptographicOperations.FixedTimeEquals` to prevent timing oracle attacks.

### 5.4 Cursor Security Threat Model

| Threat | Mitigation |
|---|---|
| Tampering (modify cursor values) | HMAC-SHA256 signature; invalid signature → `InvalidPaginationCursorException` |
| Replay attacks | TTL expiration; expired cursor → `ExpiredPaginationCursorException` |
| Cross-keyset confusion | FNV-1a fingerprint mismatch → `InvalidPaginationCursorException` |
| Schema migration breakage | Cursor versioning (v1/v2); schema change → fingerprint mismatch |
| Schema enumeration | Base64URL provides opacity; HMAC prevents construction of valid cursors |
| Timing oracle attacks | `CryptographicOperations.FixedTimeEquals` for signature comparison |
| Clock skew (distributed) | `ClockSkewTolerance` (default 30s) matches ASP.NET Core Data Protection |
| Tenant cursor crossover | Consumer responsibility; pagination does not embed tenant context |

### 5.5 Cursor Security Decision: No AES Encryption (ADR-0014)

HMAC signing provides integrity and authenticity. AES encryption provides confidentiality. For pagination cursors:
- Confidentiality of field values is not a core requirement; use opaque surrogate keys if needed.
- AES introduces IV management, key rotation complexity, and padding oracle risks.
- HMAC is already the correct cryptographic primitive for this threat model.

---

## 6. Page Result Model

### 6.1 Type Hierarchy

```
IPagedList<T>  (Abstractions)
    |
PagedList<T>  (class, internal constructor)
    |
    └── CountedPagedList<T>  (sealed, ExactTotalCount: long)

ICursorPagedList<T>  (Abstractions)
    |
CursorPagedList<T>  (class)
    |
    └── CountedCursorPagedList<T>  (sealed)
```

### 6.2 Factory Methods

```csharp
// With exact count
CountedPagedList<T> PagedList<T>.WithCount(items, parameters, totalCount)

// Without count (HasNextPage via probe strategy)
PagedList<T> PagedList<T>.WithoutCount(items, parameters, hasNextPage, hasPreviousPage?)

// Empty result
CountedPagedList<T> PagedList<T>.Empty(parameters)
```

### 6.3 Why Not a Struct (ADR-0009)

- `IPagedList<T>` is a generic interface → boxing on every interface upcast eliminates allocation savings
- `CountedPagedList<T>` (subtype) requires inheritance → C# does not support struct inheritance
- `Map<TResult>` requires polymorphic dispatch between base and subtype
- External subclassing prevented by `internal` constructor; `sealed` on subtype

---

## 7. Deterministic Ordering

### 7.1 Rule

> **Every keyset query MUST have a uniquely identifying tiebreaker column.**

Without a unique tiebreaker, rows with identical primary sort column values may be skipped or duplicated across pages.

### 7.2 Enforcement

- **Analyzer PAG001** detects `ToPagedListAsync` on unsorted `IQueryable` → compile error
- **Analyzer PAG007** warns when `KeysetBuilder<T>` has more than 5 columns → SQL predicate grows O(2^N)
- Documentation explicitly requires `Id` or another unique column as the final `Ascending()/Descending()` call

### 7.3 Null Handling

NULLS FIRST/LAST behavior is database-specific and intentionally NOT abstracted:
- PostgreSQL: NULLS LAST for ASC, NULLS FIRST for DESC (defaults)
- SQL Server: NULLs sort as minimum values
- The library documents this limit but does not attempt to normalize it (OUT_OF_SCOPE)

---

## 8. Count Strategy Architecture

| Strategy | Method | SQL | Round Trips | Notes |
|---|---|---|---|---|
| Exact count | `ToPagedListAsync` | SELECT + COUNT(*) | 2 | Full pagination metadata |
| Window count | `ToPagedListDeferredAsync` | `COUNT(*) OVER()` | 1 | Deferred join optimization |
| N+1 probe | `ToPagedListWithoutCountAsync` | SELECT LIMIT N+1 | 1 | HasNextPage only, no TotalPages |
| Approx count | `ToPagedListWithApproxCountAsync` | pg_class/sys.dm | 1+1 | Fast for large tables; may be stale |
| No count | `ToCursorPagedListAsync` | SELECT LIMIT N+1 | 1 | Keyset default |

**Rule**: Total count is never forced on the consumer. Every strategy is a deliberate opt-in.

---

## 9. Specification Integration Architecture

```
Application
    |
    ├── Specification (filter + sort predicates)
    |       |
    |       └── Applied to IQueryable<T>
    |
    └── Pagination (page request + cursor)
            |
            └── Applied to ordered IQueryable<T>
```

**Dependency direction**: Pagination knows nothing about Specification. The consumer composes them:

```csharp
// Consumer composes both
var page = await db.Products
    .Where(spec.Predicate)    // Specification applies filter
    .ApplySort(sort)           // Sorting applied
    .Keyset(cursor)            // Pagination keyset builder
    .Ascending(x => x.CreatedAt)
    .Ascending(x => x.Id)
    .ToCursorPagedListAsync();
```

**There is no `EricksonLopez.Pagination.Specification` package** — pagination depends on zero specification types. The integration is via plain `IQueryable<T>`, which both systems understand.

---

## 10. SqlBuilder / Dapper Integration Architecture

```
Dapper integration (EricksonLopez.Pagination.Dapper)
    |
    ├── IDbConnection extensions (offset pagination)
    |       → Consumer provides SQL; library adds OFFSET/LIMIT
    |
    └── DapperKeysetBuilder<T> (keyset pagination)
            → Consumer specifies table, WHERE, columns
            → Library generates keyset WHERE predicate
            → Library manages cursor encode/decode
            → Dapper executes the final SQL
```

**The library generates keyset predicates but does not execute SQL.** `IDbConnection.QueryAsync<T>` is called by the Dapper integration layer. Core has zero Dapper dependency.

---

## 11. AOT Strategy

### 11.1 AOT Status by Component

| Component | AOT Status | Notes |
|---|---|---|
| `EricksonLopez.Pagination.Abstractions` | FULLY AOT | Pure interfaces and enums |
| `EricksonLopez.Pagination` (Core) | FULLY AOT | Cursor encoding via stackalloc; no reflection |
| `EricksonLopez.Pagination.EntityFrameworkCore` | AOT-COMPATIBLE | Expression trees via `KeysetBuilder<T>` compile-time |
| Filter DSL (`FilterExpression`) | NOT AOT | `[RequiresUnreferencedCode]`, `MakeGenericType`; documented in ADR-0010 |
| `EricksonLopez.Pagination.AspNetCore` | FULLY AOT | Model binders use IParsable<T> |
| `EricksonLopez.Pagination.Blazor` | FULLY AOT | |
| `EricksonLopez.Pagination.Grpc` | FULLY AOT | |
| `EricksonLopez.Pagination.SourceGenerators` | N/A (compile-time) | Enables AOT for cursor decoders |

### 11.2 Source Generator Role

`EricksonLopez.Pagination.SourceGenerators` analyzes `Ascending()`/`Descending()` calls at compile time and emits a `[ModuleInitializer]` that registers cursor decoder delegates into `ICursorDecoderRegistry`. This eliminates reflection-based type discovery at runtime, enabling Native AOT for cursor pagination.

---

## 12. Performance Architecture

### 12.1 Allocation Budget

| Path | Allocation |
|---|---|
| Cursor encode (HmacCursorEncoder) | ~0 bytes on .NET 8+ (stackalloc) |
| Cursor decode | 1 string allocation (Base64URL decode) |
| Expression compilation (cached) | 0 bytes (warm path) |
| PagedList<T> construction | 1 object per page result |
| Filter DSL (warm path) | ArrayPool; ~0 heap allocation |

### 12.2 Expression Cache

`ConcurrentFifoCache<TKey, TValue>` — thread-safe LRU-style cache with bounded capacity. Filter expression compilation warm path: **29ns** (vs Gridify baseline 650ns). Cache key includes filter string + maxComplexity + unknownFieldBehavior to prevent cache poisoning.

### 12.3 Keyset Bounding Condition (ADR-0019)

Without bounding condition: PostgreSQL OR-tree → index scan → ~10ms at deep pages.
With bounding condition (`WHERE Age >= @p1 AND (Age > @p1 OR (Age = @p1 AND Id > @p2))`): PostgreSQL optimizer performs index seek → ~2.2ms. A 78% latency reduction.

---

## 13. EricksonLopez Ecosystem Integration

| Library | Relationship | Integration Type |
|---|---|---|
| EricksonLopez.Specification | Separate concern; Pagination consumes its `IQueryable` output | NONE — consumer composes |
| EricksonLopez.SqlBuilder | May generate keyset WHERE predicates consumed by Pagination.Dapper | OPTIONAL ADAPTER |
| EricksonLopez.Dapper.Extensions | Pagination.Dapper wraps IDbConnection; may share connection management | OPTIONAL ADAPTER |
| EricksonLopez.Result | Cursor validation could return Result<T> instead of exception | OPTIONAL — via ICursorEncoder |
| EricksonLopez.Mapper | Map<TResult>() on PagedList<T> is already built-in | NONE |
| EricksonLopez.Mediator | Pagination parameters can be part of query objects | NONE — consumer composes |
| EricksonLopez.DomainPrimitives | Cursor positions could be DomainPrimitive types | OPTIONAL — KeysetBuilder<T> accepts any type |
| EricksonLopez.SharedKernel | May share base interfaces or marker interfaces | OPTIONAL |
| EricksonLopez.Outbox | No pagination relationship | NONE |

**Rule**: Pagination does not directly depend on any other EricksonLopez package. All integration is via interfaces and extension points.
