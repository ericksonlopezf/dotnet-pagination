# System Architecture

`EricksonLopez.Pagination` is an enterprise-grade, zero-allocation universal pagination standard for modern .NET applications (.NET 8, .NET 9, .NET 10). 
It establishes a strict separation of concerns by defining clean, framework-agnostic pagination contracts (interfaces, structs, and primitives) independent of any specific data access technology, paired with specialized, high-performance integration extensions for relational ORMs, NoSQL databases, search engines, and modern transport protocols.

---

## Core Design Principles

1. **Persistence-Agnostic Core**: Core contracts (`IPagedList<T>`, `ICursorPagedList<T>`, `PaginationParameters`, `CursorPaginationParameters`, `ICursorEncoder`) have zero database dependencies.
2. **Zero-Coupling to HTTP in Domain**: Pagination semantics model dataset traversal independently of HTTP headers, query strings, or JSON response envelopes.
3. **Explicit over Magic**: No implicit `COUNT(*)` queries, no silent fallback to sequential scans, and no hidden round trips without consumer opt-in.
4. **Deterministic Keyset Seeking ($O(\log N)$)**: Keyset queries require a strictly unique tiebreaker column (enforced at compile-time via Roslyn analyzer `PAG002`–`PAG004`).
5. **Index-Seek Bounding Optimization (ADR-0019)**: Injects primary column seek bounds (`WHERE CreatedAt >= @p1 AND ...`) to guarantee relational query planners execute B-Tree Index Seeks rather than degraded index scans.
6. **Cryptographic Tamper-Proof Cursors (ADR-0017)**: High-throughput HMAC-SHA256 authenticated wire formats (`S|` single-column, `M|` multi-column) with optional TTL and distributed nonce replay protection (ADR-0035).
7. **Schema Drift Immunity (ADR-0007)**: FNV-1a 64-bit deterministic fingerprinting binds cursor tokens to keyset sort signatures, preventing corrupt page traversal during rolling deployments.
8. **Native AOT First**: Zero runtime reflection on hot paths; compile-time code generation via Roslyn Source Generators for cursor decoding and filter expression providers.

---

## Context Diagram (C4 Level 1)

The reference implementation `/samples/DemoApp` showcases the library operating within a Clean Architecture / CQRS boundary:

```mermaid
C4Context
    title System Context for EricksonLopez.Pagination Ecosystem

    Person(user, "API Consumer", "Web SPAs, Mobile Apps, or Background ETL Workers consuming paginated endpoints.")
    System(app, "ASP.NET Core Web API", "Hosts REST endpoints, gRPC services, or GraphQL Relay schemas with EricksonLopez.Pagination extensions.")
    SystemDb(database, "Relational / NoSQL Storage", "PostgreSQL, SQL Server, SQLite, Oracle, MongoDB, Cosmos DB, or Elasticsearch.")
    SystemDb(redis, "Redis Cluster", "Optional distributed cursor nonce store for replay protection.")

    Rel(user, app, "Requests paginated resources (page/pageSize or after/first)", "HTTPS / gRPC")
    Rel(app, database, "Executes optimized seek predicates or count-less lookahead queries", "TCP / Driver")
    Rel(app, redis, "Validates and consumes single-use cursor nonces", "RESP")
```

---

## Component Architecture

```mermaid
graph TD
    subgraph "Application Layer (ASP.NET Core / Web)"
        EP["Minimal API Endpoints / MVC Controllers"]
        MB["Model Binders:<br/>PaginationParametersModelBinder<br/>CursorPaginationParametersModelBinder"]
        F["Endpoint Filter:<br/>PaginationEndpointFilter"]
        R["Response Models:<br/>PagedResponse<T><br/>CursorPagedResponse<T><br/>Relay Connection<T>"]
    end

    subgraph "Core Library (EricksonLopez.Pagination)"
        PCO["PaginationCoreOptions"]
        CPCO["PaginationCursorOptions"]
        PL["PagedList<T> & CountedPagedList<T>"]
        CPL["CursorPagedList<T> & CountedCursorPagedList<T>"]
        HmacEnc["HmacCursorEncoder (HMAC-SHA256)"]
        B64Enc["Base64CursorEncoder"]
        Fnv["FNV-1a Fingerprint Engine"]
        Cache["PaginationExpressionCache"]
    end

    subgraph "Abstractions Layer (Zero Dependencies)"
        IPL["IPagedList<T> & ICountedPagedList<T>"]
        ICPL["ICursorPagedList<T> & ICursorPagedList"]
        Params["PaginationParameters & CursorPaginationParameters"]
        SortFilter["SortParameters & FilterParameters"]
        IEFilter["IFilterProvider<T> & IFilterOperatorProvider"]
        SecProt["ICursorEncoder, ICursorDecoderRegistry, ICursorReplayStore"]
    end

    subgraph "Infrastructure & Data Access Adapters"
        EF["EF Core:<br/>KeysetBuilder<T>, ToPagedListAsync"]
        Dap["Dapper:<br/>DapperKeysetBuilder<T>, ToPagedListAsync"]
        LtDb["LinqToDB:<br/>LinqToDBKeysetBuilder<T>"]
        Mdb["MongoDB:<br/>FindFluent & IMongoQueryable Extensions"]
        Cos["Cosmos DB:<br/>FeedIterator Continuation Translators"]
        Es["Elasticsearch:<br/>search_after SearchDescriptor Mappers"]
        Red["Redis:<br/>RedisCursorReplayStore"]
    end

    EP -->|Binds Struct Parameters| MB
    EP -->|Validates Input Ranges| F
    MB --> Params
    F --> Params
    
    EP -->|Executes Query| EF
    EP -->|Executes Query| Dap
    EP -->|Executes Query| LtDb
    EP -->|Executes Query| Mdb
    EP -->|Executes Query| Cos
    EP -->|Executes Query| Es
    
    EF -->|Produces| PL
    EF -->|Produces| CPL
    Dap -->|Produces| PL
    Dap -->|Produces| CPL
    LtDb -->|Produces| PL
    LtDb -->|Produces| CPL
    
    PL -.-> IPL
    CPL -.-> ICPL
    
    HmacEnc --> Fnv
    HmacEnc -.-> SecProt
    Red -.-> SecProt
    
    EP -->|Serializes Response| R
    R -.-> PL
    R -.-> CPL
```

---

## Package Dependency Graph

The 17 packages in the ecosystem adhere to a strict layered dependency structure:

```mermaid
graph LR
    Abstractions["EricksonLopez.Pagination.Abstractions<br/>(Zero Dependencies)"]
    Core["EricksonLopez.Pagination<br/>(Core Engine)"]
    
    AspNetCore["EricksonLopez.Pagination.AspNetCore"]
    EFCore["EricksonLopez.Pagination.EntityFrameworkCore"]
    Dapper["EricksonLopez.Pagination.Dapper"]
    LinqToDB["EricksonLopez.Pagination.LinqToDB"]
    MongoDB["EricksonLopez.Pagination.MongoDB"]
    Cosmos["EricksonLopez.Pagination.Cosmos"]
    Elasticsearch["EricksonLopez.Pagination.Elasticsearch"]
    Redis["EricksonLopez.Pagination.Redis"]
    Relay["EricksonLopez.Pagination.Relay"]
    Grpc["EricksonLopez.Pagination.Grpc"]
    Blazor["EricksonLopez.Pagination.Blazor"]
    OpenApi["EricksonLopez.Pagination.OpenApi"]
    Result["EricksonLopez.Pagination.Result"]
    SrcGen["EricksonLopez.Pagination.SourceGenerators<br/>(Roslyn Compile-Time)"]
    Analyzers["EricksonLopez.Pagination.Analyzers<br/>(Roslyn Compile-Time)"]

    Core --> Abstractions
    AspNetCore --> Core
    EFCore --> Core
    Dapper --> Core
    Dapper --> Abstractions
    LinqToDB --> Core
    MongoDB --> Core
    Cosmos --> Core
    Elasticsearch --> Core
    Elasticsearch --> Abstractions
    Redis --> Abstractions
    Relay --> Core
    Relay --> Abstractions
    OpenApi --> Core
    Result --> Abstractions
    Blazor --> Abstractions
    Grpc --> Abstractions
    SrcGen -..->|"Emits AOT Initializers"| Core
    Analyzers -..->|"Enforces Build Diagnostics"| Core
```

---

## Keyset Index-Seek Mechanics (ADR-0019)

Traditional multi-column keyset queries without bounding conditions often cause relational query planners to choose suboptimal index skip-scans. `KeysetBuilder<T>` injects **Seek Bounding Predicates**:

```sql
-- Query generated for ORDER BY CreatedAt DESC, Id ASC with Cursor (CreatedAt=@c1, Id=@c2)
SELECT "Id", "CreatedAt", "Name"
FROM "Products"
WHERE "CreatedAt" <= @c1
  AND ("CreatedAt" < @c1 OR ("CreatedAt" = @c1 AND "Id" > @c2))
ORDER BY "CreatedAt" DESC, "Id" ASC
LIMIT 21;
```

1. **Bounding Guard (`"CreatedAt" <= @c1`)**: Immediately restricts the B-Tree index scan to pages matching or preceding `@c1`.
2. **Composite Tuple Disjunction**: Eliminates skipped or duplicate rows when adjacent records share identical timestamps.
3. **Lookahead Slicing (`LIMIT pageSize + 1`)**: Fetches one extra record to compute `HasNextPage` without running a separate `COUNT(*)` query.

---

## Cryptographic Cursor Lifecycle & State Machine (ADR-0017 / ADR-0035)

```mermaid
stateDiagram-v2
    [*] --> OpaqueString: Client supplies cursor token in query parameter (?after=...)
    OpaqueString --> Base64UrlDecode: Span-based decode (Zero allocation in .NET 8+)
    
    Base64UrlDecode --> FormatCheck: Header validation ('S|' or 'M|')
    FormatCheck --> InvalidException: Invalid token format
    
    FormatCheck --> VerifyHmac: Compute HMAC-SHA256 with server key
    VerifyHmac --> InvalidException: Constant-time signature mismatch (Tampering detected)
    
    VerifyHmac --> ValidateFingerprint: Compare 64-bit FNV-1a query hash
    ValidateFingerprint --> InvalidException: Schema drift / Sort order changed
    
    ValidateFingerprint --> CheckTTL: Verify timestamp against TimeSpan.FromMinutes(N)
    CheckTTL --> ExpiredException: Cursor exceeded TTL (HTTP 410 Gone)
    
    CheckTTL --> CheckReplay: Query ICursorReplayStore (Redis / In-Memory)
    CheckReplay --> ReplayedException: Nonce already consumed (Replay attack detected)
    
    CheckReplay --> ExtractValues: Parse typed key values (Id, CreatedAt, etc.)
    ExtractValues --> KeysetSeek: KeysetBuilder generates SQL Index Seek
    
    InvalidException --> [*]: Throw InvalidPaginationCursorException (HTTP 400)
    ExpiredException --> [*]: Throw ExpiredPaginationCursorException (HTTP 410)
    ReplayedException --> [*]: Throw ReplayedPaginationCursorException (HTTP 409)
    KeysetSeek --> [*]: Return CursorPagedList<T> to consumer
```

---

## Primary Execution Flows

### 1. Count-less Offset Lookahead Query

```mermaid
sequenceDiagram
    participant Client
    participant API as ASP.NET Core Minimal API
    participant Filter as PaginationEndpointFilter
    participant EF as EF Core Provider
    participant DB as PostgreSQL / SQL Server

    Client->>API: GET /products?page=3&pageSize=20
    API->>Filter: Validates page >= 1 and pageSize <= MaxPageSize (1000)
    Filter->>API: Valid parameters accepted
    API->>EF: query.OrderBy(x => x.Id).ToPagedListWithoutCountAsync(params)
    EF->>DB: SELECT * FROM products ORDER BY Id OFFSET 40 LIMIT 21
    DB-->>EF: 21 rows returned
    Note over EF: Slice 20 items for page. HasNextPage = true (21st item exists).
    EF-->>API: PagedList<Product> (TotalCount = null)
    API-->>Client: 200 OK PagedResponse<Product>
```

### 2. High-Performance Keyset Traversal

```mermaid
sequenceDiagram
    participant Client
    participant API as ASP.NET Core Minimal API
    participant Codec as HmacCursorEncoder
    participant Store as ICursorReplayStore (Redis)
    participant Keyset as KeysetBuilder<Product>
    participant DB as Database Engine

    Client->>API: GET /products?first=20&after=ey...
    API->>Codec: Decode(afterToken)
    Codec->>Codec: Verify HMAC-SHA256 & FNV-1a fingerprint
    Codec->>Store: TryAcquireNonceAsync(nonce, ttl)
    Store-->>Codec: Nonce valid (not seen before)
    Codec-->>API: RawCursorValue (CreatedAt=2026-08-25, Id=1042)
    API->>Keyset: query.Keyset(params).Descending(x=>x.CreatedAt).Ascending(x=>x.Id).ToCursorPagedListAsync()
    Keyset->>DB: SELECT ... WHERE CreatedAt <= @c1 AND (...) ORDER BY CreatedAt DESC, Id ASC LIMIT 21
    DB-->>Keyset: 21 rows (Index Seek)
    Keyset->>Codec: Encode(lastRow.CreatedAt, lastRow.Id)
    Codec-->>Keyset: newEndCursor = ey...
    Keyset-->>API: CursorPagedList<Product>
    API-->>Client: 200 OK CursorPagedResponse<Product> (RelayPageInfo)
```
