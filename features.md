# features.md — EricksonLopez.Pagination
### Feature Taxonomy · Classification · Scoring · Decision Matrix
**Version**: 2026-Q3 · **Status**: Authoritative

---

## 1. What Is EricksonLopez.Pagination?

### 1.1 Concept Analysis

| Concept | Description | Verdict |
|---|---|---|
| Pagination primitives | Raw types: `Page`, `Cursor`, `PageSize` | Partially — the foundation |
| Pagination result model | `PagedList<T>`, `CursorPagedList<T>` | **YES — core** |
| Pagination query model | `PaginationParameters`, `CursorPaginationParameters` | **YES — core** |
| Cursor framework | Opaque cursor encode/decode/sign/version | **YES — first-class pillar** |
| Query builder | SQL generation, predicate construction | Partial — provider concern, not core |
| Sorting framework | Multi-column sort abstractions | Partially — required for deterministic keyset |
| Filtering framework | DSL, dynamic filters | Attached but distinct — separate from pagination semantics |
| API pagination framework | HTTP parsing, response format | Extension only |
| Database pagination abstraction | Provider integration layer | Extension layer |

### 1.2 Official Product Definition

> **EricksonLopez.Pagination is a database-backed pagination infrastructure library for .NET 8+.**
>
> **Primary responsibilities:**
> 1. **Pagination semantics** — modeling what it means to navigate a dataset.
> 2. **Pagination state** — `PagedList<T>`, `CursorPagedList<T>`.
> 3. **Cursor framework** — encoding, decoding, signing, versioning opaque tokens.
> 4. **Provider integration** — EF Core, Dapper, MongoDB, Cosmos adapters.
>
> **Non-responsibilities:**
> - Generating arbitrary SQL
> - Executing SQL or opening connections
> - Filtering data (application concern)
> - Parsing HTTP query strings (extension only)
> - Serializing HTTP responses (extension only)

### 1.3 Layered Architecture

```
Pagination semantics
        |
Pagination state (PagedList<T>, CursorPagedList<T>)
        |
Cursor framework (ICursorEncoder, HmacCursorEncoder)
        |
Provider layer (EFCore, Dapper, MongoDB, Cosmos)
        |
Database
```

---

## 2. Feature Classification Legend

| Tag | Meaning |
|---|---|
| **CORE** | Ships in `EricksonLopez.Pagination` or `EricksonLopez.Pagination.Abstractions` |
| **EXTENSION** | Ships in a provider/integration package |
| **OPTIONAL** | Available but not required; consumer opts in explicitly |
| **EXPERIMENTAL** | Available under experimental flag; may change |
| **REJECTED** | Explicitly not built; formalized in ADR |
| **OUT_OF_SCOPE** | Not pagination's responsibility |

---

## 3. Feature Catalog

### 3.1 Pagination Strategies

| Feature | Class | Package | ADR | Notes |
|---|---|---|---|---|
| Offset / Page-Number pagination | CORE | Core | — | OFFSET N LIMIT M; documented performance degradation |
| Keyset pagination (single column) | CORE | EFCore, Dapper | ADR-0003 | WHERE id > @lastId ORDER BY id |
| Keyset pagination (multi-column, up to 16) | CORE | EFCore, Dapper | ADR-0019 | Composite with bounding conditions |
| Cursor pagination (opaque token) | CORE | Core | ADR-0006 | Backed by keyset; opaque nextCursor |
| Seek pagination (row-value syntax) | CORE | EFCore, Dapper | — | (a, b) > (@a, @b) — PG/SQL Server |
| Streaming keyset (IAsyncEnumerable) | EXTENSION | EFCore | — | Auto-advances cursor; batch-based |
| Count-less offset (N+1 probe) | CORE | EFCore, Dapper | — | Take(N+1) to determine HasNextPage |
| Backward pagination (previous cursor) | CORE | EFCore, Dapper | — | ORDER BY reversed; bidirectional |
| Hybrid offset+keyset | REJECTED | — | — | Complexity without proportional value |
| In-memory (IEnumerable) pagination | REJECTED | — | ADR-0013 | X.PagedList owns this space |

### 3.2 Cursor Architecture

| Feature | Class | Package | ADR | Notes |
|---|---|---|---|---|
| Base64URL opaque cursor | CORE | Core | — | Default; not tamper-resistant alone |
| HMAC-SHA256 signed cursor | CORE | Core | ADR-0017 | HmacCursorEncoder; secure by default |
| Cursor versioning (v1 to v2 migration) | CORE | Core | ADR-0006 | AcceptLegacyCursors migration window |
| FNV-1a keyset schema fingerprint | CORE | Core | ADR-0007 | Deterministic cross-pod fingerprint |
| Cursor TTL expiration | OPTIONAL | Core | ADR-0017 | TimeToLive on HmacCursorEncoder |
| Cursor clock-skew tolerance | OPTIONAL | Core | — | Default 30s; NTP drift protection |
| Strongly typed cursor positions | CORE | EFCore | — | KeysetBuilder<T> type-inferred columns |
| Source-generated cursor decoders | OPTIONAL | SourceGenerators | ADR-0010 | Required for Native AOT |
| AES cursor encryption | REJECTED | — | ADR-0014 | HMAC signing solves real threat |
| Cursor expiration with server-side state | REJECTED | — | — | Stateless cursors are core principle |
| Tenant-aware cursor embedding | OUT_OF_SCOPE | — | — | Consumer responsibility |
| Custom cursor signing algorithms | OPTIONAL | Core | — | Via ICursorEncoder extension point |

### 3.3 Page Result Model

| Feature | Class | Package | ADR | Notes |
|---|---|---|---|---|
| PagedList<T> (class, optional count) | CORE | Core | ADR-0009 | HasNextPage via probe or TotalCount |
| CountedPagedList<T> (exact count) | CORE | Core | ADR-0009 | ExactTotalCount: long (non-nullable) |
| CursorPagedList<T> | CORE | Core | — | Next/previous cursor; HasNextPage |
| CountedCursorPagedList<T> | CORE | Core | — | Cursor pagination with exact count |
| IPagedList<T> interface | CORE | Abstractions | — | Zero-dependency contract |
| ICursorPagedList<T> interface | CORE | Abstractions | — | Cursor-based contract |
| Map<TResult>(selector) projection | CORE | Core | — | Preserves metadata; allocates new array |
| Relay-style PageInfo (edges/nodes) | EXTENSION | AspNetCore | — | JSON response shaping only |
| Page<T> as value type struct | REJECTED | — | ADR-0009 | Boxing on interface upcast; mutability hazard |

### 3.4 Count Strategy

| Feature | Class | Package | Notes |
|---|---|---|---|
| Exact COUNT(*) — two-query | CORE | EFCore, Dapper | Explicit opt-in; consumer is aware of cost |
| COUNT(*) OVER() window function | OPTIONAL | EFCore | Single-query; ToPagedListDeferredAsync |
| HasNext via Take(N+1) probe | CORE | EFCore, Dapper, MongoDB | No COUNT query; ToPagedListWithoutCountAsync |
| PostgreSQL approximate count (pg_class) | OPTIONAL | EFCore, Dapper | With exact-count fallback |
| SQL Server approximate count | OPTIONAL | EFCore | Planned; demand-gated |
| No count (keyset default) | CORE | EFCore, Dapper | Keyset default is HasNext-only |
| Mandatory total count | REJECTED | — | Forces expensive COUNT; hides cost from consumer |
| Automatic count in all strategies | REJECTED | — | Hides database cost |

### 3.5 Sorting (Required for Keyset Determinism)

| Feature | Class | Package | Notes |
|---|---|---|---|
| Static ordering via expression tree | CORE | EFCore | Ascending(x => x.CreatedAt) |
| Deterministic ordering enforcement | CORE | EFCore, Dapper | Unique tiebreaker column required |
| Multi-column ordering (up to 16) | CORE | EFCore, Dapper | Composite keyset |
| Dynamic ordering (string-based) | EXTENSION | EFCore | ApplySort(string) |
| Sort column allowlist | CORE | Core | [AllowSorting] attribute |
| Null ordering (NULLS FIRST/LAST) | OUT_OF_SCOPE | — | Database-specific; document, do not abstract |
| Sorting as a standalone framework | OUT_OF_SCOPE | — | Belongs to Specification / query layer |

### 3.6 Filtering

| Feature | Class | Package | ADR | Notes |
|---|---|---|---|---|
| Filter DSL (string parsing) | EXTENSION | EFCore | ADR-0010 | `name~=John,age>=18`; NOT pagination semantics |
| [Filterable] attribute allowlist | EXTENSION | EFCore | — | Per-property opt-in |
| [Filterable(Name="alias")] field aliasing | EXTENSION | Abstractions, EFCore | — | Decouples API parameter naming from entity property naming |
| IFilterOperatorProvider<T> custom operators | EXTENSION | Abstractions, EFCore | — | Extensible operator pipeline (e.g. `%=`, full-text, geo) |
| Filter complexity limits (DoS protection) | EXTENSION | EFCore | — | Max clauses, max value length |
| Case-insensitive string filtering | EXTENSION | EFCore | — | DB collation dependent |
| Nested property filtering (dot notation) | EXTENSION | EFCore | — | customer.name~=John |
| Dynamic filtering as core concern | REJECTED | — | — | Filtering is not pagination |
| OData-style query language | REJECTED | — | ADR-0016 | Microsoft.AspNetCore.OData owns this |
| GraphQL filter integration | OUT_OF_SCOPE | — | — | HotChocolate owns this |

### 3.7 Provider Integration

| Feature | Class | Package | Notes |
|---|---|---|---|
| EF Core offset pagination | EXTENSION | EntityFrameworkCore | ToPagedListAsync |
| EF Core keyset pagination | EXTENSION | EntityFrameworkCore | KeysetBuilder<T>, ToCursorPagedListAsync |
| EF Core deferred join | EXTENSION | EntityFrameworkCore | ToPagedListDeferredAsync; 1 round-trip |
| Dapper offset pagination | EXTENSION | Dapper | IDbConnection.ToPagedListAsync |
| Dapper keyset (multi-column) | EXTENSION | Dapper | DapperKeysetBuilder<T> |
| MongoDB offset pagination | EXTENSION | MongoDB | IFindFluent extensions |
| MongoDB cursor pagination | EXTENSION | MongoDB | ObjectId-based |
| Cosmos DB cursor pagination | EXTENSION | Cosmos | Continuation token |
| LinqToDB pagination | EXPERIMENTAL | LinqToDB | Experimental; code present in repository, in active development (Stryker target >=80% before NuGet release) |
| NHibernate adapter | REJECTED | — | Legacy; outside .NET 8+ target |
| LINQ-to-Objects (IEnumerable) | REJECTED | — | ADR-0013 |

### 3.8 HTTP / API Layer

| Feature | Class | Package | ADR | Notes |
|---|---|---|---|---|
| ASP.NET Core model binders | EXTENSION | AspNetCore | — | PaginationParameters, CursorPaginationParameters |
| Minimal API endpoint filter | EXTENSION | AspNetCore | — | PaginationEndpointFilter |
| PagedResponse<T> / CursorPagedResponse<T> | EXTENSION | AspNetCore | — | JSON response envelopes |
| ETag generation | EXTENSION | AspNetCore | — | Based on cursor + page metadata |
| OpenAPI / Swagger schema integration | EXTENSION | OpenApi | — | PaginationOperationTransformer |
| HATEOAS link builder | REJECTED | — | ADR-0015 | Requires route knowledge the library does not have |
| Link header (RFC 8288) | OUT_OF_SCOPE | — | — | Consumer responsibility |
| HTTP caching strategy | OUT_OF_SCOPE | — | — | CDN/cache concerns |

### 3.9 GraphQL

| Feature | Class | Package | Notes |
|---|---|---|---|
| Relay Connections mapping | OUT_OF_SCOPE | — | HotChocolate handles natively |
| GraphQL cursor interop | OPTIONAL | — | CursorPagedList<T> maps cleanly; no package needed |

### 3.10 Performance and AOT

| Feature | Class | Package | ADR | Notes |
|---|---|---|---|---|
| Native AOT (core types) | CORE | Core, Abstractions | ADR-0010 | Cursor encoding, result models: fully AOT |
| Source generators (cursor decoders) | OPTIONAL | SourceGenerators | — | Required for AOT; [ModuleInitializer] |
| Roslyn analyzers (PAG002-PAG008) | OPTIONAL | Analyzers | — | Compile-time bug detection |
| Expression caching (LRU) | CORE | Core | — | ConcurrentFifoCache; warm path 29ns |
| stackalloc cursor encoding (.NET 8+) | CORE | Core | — | Near-zero allocation |
| ArrayPool (filter builder) | EXTENSION | Core | — | Avoids LOH allocation |
| Row-value predicate (PG/SQL Server) | EXTENSION | EFCore | ADR-0019 | (a, b) > (@a, @b) syntax |
| Keyset bounding condition | CORE | EFCore, Dapper | ADR-0019 | Age >= @p1 AND (Age > @p1 OR ...) |
| Runtime reflection in hot path | REJECTED | — | — | Reflection only in filter DSL (documented) |

### 3.11 Blazor

| Feature | Class | Package | Notes |
|---|---|---|---|
| PagedListPager headless component | EXTENSION | Blazor | Bootstrap preset available |
| Atomic navigation guard | EXTENSION | Blazor | Prevents double-navigation race |
| MVC Razor TagHelpers | REJECTED | — | X.PagedList.Mvc dominates |

### 3.12 gRPC

| Feature | Class | Package | Notes |
|---|---|---|---|
| Protobuf message to/from pagination parameters | EXTENSION | Grpc | ToParameters(), ToMessage() |

### 3.13 Testing Infrastructure

| Feature | Class | Package | Notes |
|---|---|---|---|
| Unit tests (xUnit, AwesomeAssertions) | CORE | Tests | |
| Property-based tests (FsCheck) | OPTIONAL | Tests | Cursor encoding, page arithmetic |
| Integration tests (Testcontainers) | EXTENSION | Tests | PostgreSQL, SQL Server, MongoDB |
| Blazor component tests (bunit) | EXTENSION | Tests | |
| BenchmarkDotNet benchmarks | CORE | Benchmarks | Offset vs keyset; cursor encode/decode |
| Mutation testing (Stryker.NET, ≥95% on Core packages) | CORE | Stryker | Quality gate; EFCore and MongoDB excluded per ADR-0004 |
| AOT publish test (CI) | CORE | CI | dotnet publish -r linux-x64 --aot |

---

## 4. Feature Scoring Model

### 4.1 Scoring Formula

```
Score = ArchitecturalValue    * 0.25
      + DeveloperExperience   * 0.15
      + PerformanceImpact     * 0.15
      + AOTCompatibility      * 0.12
      + DatabaseIndependence  * 0.13
      + APIValue              * 0.12
      + Complexity_INV        * 0.05   (inverted: 5=minimal cost)
      + MaintenanceCost_INV   * 0.02   (inverted)
      + APISurfaceCost_INV    * 0.01   (inverted)
```

> **Note**: CompetitiveDiff has been removed from the scoring formula. Features are scored on their value to the consumer and alignment with the product architecture, independent of the feature landscape.

### 4.2 Feature Scores

| Feature | Arch | DX | Perf | AOT | DB | API | Diff | Cplx | Mnt | Srf | TOTAL |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Keyset pagination (multi-col) | 5 | 5 | 5 | 5 | 4 | 4 | 5 | 3 | 3 | 4 | **4.54** |
| HMAC-signed cursor | 5 | 4 | 4 | 5 | 5 | 4 | 5 | 4 | 4 | 5 | **4.55** |
| Cursor versioning | 4 | 4 | 3 | 5 | 5 | 4 | 5 | 4 | 4 | 5 | **4.22** |
| FNV-1a fingerprint | 4 | 3 | 4 | 5 | 5 | 3 | 5 | 5 | 5 | 5 | **4.27** |
| Keyset bounding condition | 4 | 2 | 5 | 5 | 4 | 2 | 4 | 5 | 5 | 5 | **4.02** |
| PagedList<T> result model | 5 | 5 | 4 | 5 | 5 | 5 | 3 | 4 | 4 | 4 | **4.42** |
| Count-less offset (probe) | 4 | 4 | 4 | 5 | 5 | 4 | 4 | 5 | 5 | 5 | **4.35** |
| Deferred join optimization | 4 | 3 | 5 | 3 | 4 | 3 | 5 | 4 | 4 | 4 | **3.98** |
| PostgreSQL approx count | 3 | 3 | 4 | 4 | 2 | 3 | 5 | 4 | 4 | 5 | **3.55** |
| Source generators (AOT) | 4 | 3 | 4 | 5 | 5 | 3 | 4 | 3 | 3 | 4 | **3.90** |
| Roslyn analyzers | 3 | 5 | 3 | 5 | 5 | 4 | 4 | 4 | 3 | 3 | **3.88** |
| Expression caching (LRU) | 4 | 2 | 5 | 5 | 5 | 2 | 3 | 4 | 4 | 5 | **3.92** |
| Filter DSL | 3 | 4 | 3 | 1 | 4 | 4 | 3 | 2 | 2 | 2 | **2.99** |
| Dapper keyset builder | 4 | 4 | 4 | 3 | 4 | 3 | 5 | 3 | 3 | 3 | **3.82** |
| Blazor pager component | 2 | 5 | 2 | 3 | 3 | 4 | 4 | 4 | 3 | 4 | **3.28** |
| AES cursor encryption | 2 | 2 | 1 | 3 | 4 | 2 | 2 | 1 | 1 | 2 | **1.95** — REJECTED |
| HATEOAS builder | 2 | 3 | 2 | 3 | 1 | 3 | 1 | 2 | 2 | 2 | **2.13** — REJECTED |
| In-memory pagination | 1 | 3 | 1 | 4 | 5 | 3 | 1 | 5 | 4 | 3 | **2.40** — REJECTED |
| OData query language | 1 | 2 | 2 | 1 | 3 | 2 | 1 | 1 | 1 | 1 | **1.57** — REJECTED |

---

## 5. Responsibility Boundary Matrix

| Responsibility | Pagination | Specification | SqlBuilder | Dapper | Repository | API/HTTP |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Page semantics (offset/keyset/cursor) | OWNS | — | — | — | — | — |
| Cursor encode/decode/sign | OWNS | — | — | — | — | — |
| Cursor TTL / versioning | OWNS | — | — | — | — | — |
| Page result model | OWNS | — | — | — | — | — |
| Count strategy | OWNS | — | — | — | — | — |
| HasNext / HasPrevious | OWNS | — | — | — | — | — |
| Deterministic ordering (keyset) | OWNS | COMPOSES | — | — | — | — |
| Filtering predicates | EXTENSION | OWNS | COMPOSES | — | — | — |
| Sorting abstractions | APPLIES | OWNS | — | — | — | — |
| SQL predicate gen (keyset WHERE) | PROVIDER | — | PARTICIPATES | — | — | — |
| SQL execution | — | — | — | OWNS | OWNS | — |
| Connection management | — | — | — | WRAPS | OWNS | — |
| HTTP query string parsing | — | — | — | — | — | OWNS |
| HTTP response serialization | — | — | — | — | — | OWNS |
| HATEOAS link building | — | — | — | — | — | OWNS |
| Schema introspection | — | — | — | — | — | NONE |
| Index management | — | — | — | — | — | DOCUMENT |

---

## 6. Features Intentionally Not Built

| Feature | ADR | Core Reason |
|---|---|---|
| AES-256 cursor encryption | ADR-0014 | HMAC signing solves tampering; AES adds key management complexity without proportional gain |
| HATEOAS builder | ADR-0015 | Requires route knowledge the library cannot have; violates zero-HTTP-knowledge principle |
| OData / OData-style query language | ADR-0016 | Microsoft.AspNetCore.OData competes with infinite MS resources |
| In-memory (IEnumerable) pagination | ADR-0013 | Out of scope: this library targets database-backed pagination. IEnumerable pagination has no performance or correctness relationship with the keyset/cursor model. |
| MVC Razor TagHelpers | REJECTED | Out of scope: the Blazor headless component is the supported UI integration. MVC Razor views are a different rendering model not targeted by this library. |
| NHibernate adapter | — | Legacy ecosystem; outside .NET 8+ target philosophy |
| Mandatory total count | — | Hides database cost from consumer; must be explicit |
| Repository / Unit of Work pattern | — | Application-architecture concern; not pagination |
| Database index management | — | DBA / migration tooling concern |
| Caching (Redis/memory) | — | IDistributedCache responsibility; pagination models cache keys not results |
| Cursor expiration with server state | — | Stateless cursors are a core design principle |
| Dynamic code generation (Emit) | — | AOT incompatible; no benefit over compiled expressions |
| Tenant-aware cursor embedding | — | Consumer responsibility; multi-tenancy is an app domain concern |
| Automatic filter-sort coupling | — | Pagination is not filtering; separate DSL is the correct boundary |
