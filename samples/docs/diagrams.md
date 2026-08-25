# Architecture Diagrams — EricksonLopez.Pagination

> Updated with complete diagrams for all library layers.
> For narrative description, see [architecture.md](architecture.md) and [functional-map.md](functional-map.md).

---

## 1. General Component Architecture

```mermaid
graph TD
    subgraph "Presentation (ASP.NET Core)"
        API["Minimal API / Controllers"]
        Binder["PaginationParametersModelBinder\nCursorPaginationParametersModelBinder\nFilterParametersModelBinder\nSortParametersModelBinder"]
        Filter["PaginationEndpointFilter\n(AddPaginationValidation)"]
        Response["PagedResponse / CursorPagedResponse\nToPagedResult / ToCursorPagedResult\nApplyETagHeaders"]
    end

    subgraph "Core (Abstractions & Domain)"
        Params["PaginationParameters\nCursorPaginationParameters\nFilterParameters\nSortParameters"]
        List["IPagedList / ICursorPagedList\nICountedPagedList / ICountedCursorPagedList"]
        Factory["IPagedListFactory / ICursorPagedListFactory"]
        Encoder["ICursorEncoder\n(Base64 / HMAC)"]
    end

    subgraph "Persistence"
        EF["EF Core\n(QueryableExtensions\nKeysetBuilder)"]
        Dapper["Dapper\n(DbConnectionPaginationExtensions)"]
        Mongo["MongoDB\n(MongoQueryableExtensions)"]
        Cosmos["Cosmos DB"]
    end

    subgraph "DI / Configuration"
        AddPagination["AddPagination(services, configure)"]
        CoreOptions["PaginationCoreOptions\nMaxPageSize, DefaultPageSize\nDeepOffsetWarningThreshold"]
        CursorOptions["PaginationCursorOptions\nEncoder, Registry, TTL"]
    end

    API --> Binder
    API --> Filter
    Binder --> Params
    Filter --> Params

    API --> EF
    API --> Dapper
    API --> Mongo
    API --> Cosmos

    EF --> List
    Dapper --> List
    Mongo --> List
    Cosmos --> List

    List --> Response
    Response --> API

    AddPagination --> CoreOptions
    AddPagination --> CursorOptions
    AddPagination --> Factory
    AddPagination --> Encoder
```

---

## 2. Main Flow — Offset Pagination

```mermaid
sequenceDiagram
    participant Client
    participant API as Minimal API Endpoint
    participant Filter as PaginationEndpointFilter
    participant DB as EF Core / IQueryable

    Client->>API: GET /products?page=2&pageSize=20
    API->>Filter: Validates page >= 1, pageSize <= MaxPageSize
    Filter-->>Client: 400 if pageSize > MaxPageSize
    Filter->>API: Continues if valid
    API->>DB: query.OrderBy(x=>x.Id).ToPagedListAsync(params)
    DB->>DB: SELECT COUNT(*) FROM products (if countTotal=true)
    DB->>DB: SELECT * FROM products ORDER BY Id OFFSET 20 LIMIT 20
    DB-->>API: CountedPagedList<Product>
    API->>API: pagedList.ToPagedResult(request, maxAge: 5min)
    API-->>Client: 200 JSON + ETag + Cache-Control
    Client->>API: GET (If-None-Match: "previous-etag")
    API-->>Client: 304 Not Modified (no body)
```

---

## 3. Main Flow — Keyset (Cursor) Pagination

```mermaid
sequenceDiagram
    participant Client
    participant API as Minimal API Endpoint
    participant Encoder as ICursorEncoder
    participant DB as EF Core KeysetBuilder

    Client->>API: GET /products?first=10
    API->>DB: query.Keyset(cursor).Ascending(x=>x.Id).ToCursorPagedListAsync()
    DB->>DB: SELECT * FROM products ORDER BY Id FETCH NEXT 11 ROWS
    DB-->>API: CursorPagedList<Product> (startCursor, endCursor)
    API-->>Client: JSON {edges, pageInfo: {endCursor, hasNextPage}}

    Client->>API: GET /products?first=10&after=<endCursor>
    API->>Encoder: ICursorEncoder.Decode(endCursor)
    Encoder-->>API: rawCursor = "S|42"
    API->>DB: query.Keyset(cursor).Ascending(x=>x.Id).ToCursorPagedListAsync()
    DB->>DB: WHERE Id > 42 ORDER BY Id FETCH NEXT 11 ROWS (index seek)
    DB-->>API: CursorPagedList<Product>
    API-->>Client: JSON {edges, pageInfo}
```

---

## 4. Flow — Dapper Multi-Result-Set

```mermaid
sequenceDiagram
    participant API
    participant Dapper as DbConnectionPaginationExtensions
    participant DB as SQL Database

    API->>Dapper: connection.ToPagedListAsync(sql, params, countTotal: true)
    Note over Dapper: injects @__Pagination_Skip__, @__Pagination_PageSize__
    Dapper->>DB: SELECT COUNT(*) FROM products;\nSELECT * FROM products LIMIT N OFFSET M
    DB-->>Dapper: Result set 1 → total count
    DB-->>Dapper: Result set 2 → items
    Dapper->>Dapper: Builds CountedPagedList<T>
    Dapper-->>API: IPagedList<T>
```

---

## 5. State Machine — Cursor

```mermaid
stateDiagram-v2
    [*] --> OpaqueString: Client sends after=<string>
    OpaqueString --> Base64Decoded: Base64CursorEncoder.Decode()
    OpaqueString --> HmacValidated: HmacCursorEncoder.Decode()

    Base64Decoded --> ParsedRawCursor: "S|42" extracted
    HmacValidated --> CheckTTL: Valid signature
    HmacValidated --> [*]: InvalidPaginationCursorException

    CheckTTL --> ParsedRawCursor: Within TTL
    CheckTTL --> [*]: ExpiredPaginationCursorException

    ParsedRawCursor --> WhereKeyset: KeysetBuilder generates WHERE Id > 42
    WhereKeyset --> ResultItems: SQL executed by EF Core
    ResultItems --> EncodedCursors: New start/endCursor encoded
    EncodedCursors --> [*]: Response to client
```

---

## 6. HTTP Processing Pipeline

```mermaid
graph LR
    A["HTTP Request"] --> B{"PaginationEndpointFilter\n(AddPaginationValidation)"}
    B -- "Valid PageSize" --> C["Model Binder\n(PaginationParameters)"]
    B -- "Invalid PageSize" --> Z["400 Bad Request"]
    C --> D["ApplyFilter(FilterParameters)\nApplySort(SortParameters)"]
    D --> E["ToPagedListAsync()\nToPagedListBatchedAsync()\nToPagedAsyncEnumerable()"]
    E --> F["IPagedList / ICursorPagedList"]
    F --> G["Map() / LazyMap()"]
    G --> H["ToPagedResult(request)\nToCursorPagedResult()"]
    H --> I["ApplyETagHeaders()\n304 Not Modified?"]
    I -- "Yes" --> J["304 Not Modified"]
    I -- "No" --> K["200 OK + ETag + Cache-Control"]
```

---

## 7. Flow — Filter DSL (ApplyFilter)

```mermaid
flowchart TD
    A["FilterParameters: \"name~=John,age>=18\""] --> B["FilterExpression.Build()"]
    B --> C{"Complexity <= MaxComplexity?"}
    C -- No --> D["InvalidOperationException: too complex"]
    C -- Yes --> E["Parses tokens separated by comma (AND) and pipe (OR)"]
    E --> F["For each token: field + operator + value"]
    F --> G{"Field in allowlist?"}
    G -- "No, and allowlist active" --> H["ArgumentException: field not allowed"]
    G -- Yes --> I{"Operator"}
    I -- "= != >= <= > <" --> J["Expression.Equal/NotEqual/GreaterThan/LessThan"]
    I -- "~=" --> K["string.Contains()"]
    I -- "^=" --> L["string.StartsWith()"]
    I -- "$=" --> M["string.EndsWith()"]
    J --> N["Expression<Func<T,bool>> combined via AndAlso/OrElse"]
    K --> N
    L --> N
    M --> N
    N --> O["IQueryable<T>.Where(expression) → SQL WHERE"]
```

---

## 8. Flow — HMAC Cursor Encoder (Security)

```mermaid
sequenceDiagram
    participant App as Endpoint
    participant Enc as HmacCursorEncoder
    participant HMAC as HMACSHA256

    Note over App,HMAC: Encoding (when generating cursor)
    App->>Enc: Encode("S|42")
    Enc->>Enc: If TTL active: payload = "S|42|expiry=<timestamp>"
    Enc->>HMAC: ComputeHash(payload + secretKey)
    HMAC-->>Enc: signature (32 bytes)
    Enc-->>App: Base64Url(payload + "." + signature)

    Note over App,HMAC: Decoding (when receiving cursor from client)
    App->>Enc: Decode(opaqueCursor)
    Enc->>Enc: Split Base64Url → payload + signature
    Enc->>HMAC: ComputeHash(payload + secretKey)
    HMAC-->>Enc: expectedSignature
    Enc->>Enc: CryptographicOperations.FixedTimeEquals(actual, expected)
    alt valid signature
        Enc->>Enc: If TTL: verify expiry <= now + clockSkew
        alt within TTL
            Enc-->>App: "S|42" (raw cursor)
        else expired
            Enc-->>App: ExpiredPaginationCursorException
        end
    else invalid signature
        Enc-->>App: InvalidPaginationCursorException
    end
```

---

## 9. Package Dependencies

> **Note**: Verified against actual `.csproj` `<ProjectReference>` entries.

```mermaid
graph TD
    Abstractions["EricksonLopez.Pagination.Abstractions\n(interfaces, records, exceptions)"]
    Core["EricksonLopez.Pagination\n(implementations: PagedList, encoders)"]
    EFCore["EricksonLopez.Pagination.EntityFrameworkCore\n(KeysetBuilder, QueryableExtensions)"]
    AspNetCore["EricksonLopez.Pagination.AspNetCore\n(binders, responses, DI)"]
    Dapper["EricksonLopez.Pagination.Dapper"]
    MongoDB["EricksonLopez.Pagination.MongoDB"]
    Cosmos["EricksonLopez.Pagination.Cosmos"]
    Grpc["EricksonLopez.Pagination.Grpc"]
    Blazor["EricksonLopez.Pagination.Blazor"]
    Analyzers["EricksonLopez.Pagination.Analyzers\n(PAG001-PAG007)"]
    SrcGen["EricksonLopez.Pagination.SourceGenerators"]

    Core --> Abstractions
    EFCore --> Core
    AspNetCore --> Core
    Dapper --> Core
    Dapper --> Abstractions
    MongoDB --> Core
    Cosmos --> Core
    Grpc --> Abstractions
    Blazor --> Abstractions
    Analyzers -.->|"Roslyn Analyzer\n(compile-time only)"| Core
    SrcGen -.->|"Roslyn Source Generator\n(compile-time only)"| Core
```

> [!NOTE]
> `Grpc` and `Blazor` reference only `Abstractions` (not `Core`), keeping their dependency footprint minimal.
> `OpenApi` depends on `Core` (not `AspNetCore`). `SourceGenerators` and `Analyzers` are compile-time only.

---

## 10. Flow — Batch Processing (ToPagedListBatchedAsync)

```mermaid
sequenceDiagram
    participant App as Background Job / Service
    participant Ext as ToPagedListBatchedAsync
    participant DB as EF Core

    App->>Ext: await foreach (batch in query.ToPagedListBatchedAsync(100, ct))
    loop Until HasNextPage = false
        Ext->>DB: ToPagedListAsync(currentPage, pageSize=100)
        DB-->>Ext: IPagedList<T> with up to 100 items
        Ext-->>App: yield IPagedList<T>
        App->>App: Process batch (export, send, index...)
        Ext->>Ext: currentPage++
    end
    Ext-->>App: (end of stream)
```
