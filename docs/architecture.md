# System Architecture

`EricksonLopez.Pagination` is a highly-optimized, zero-allocation universal pagination standard for .NET applications. 
It establishes a strict separation of concerns by defining clean pagination contracts (interfaces, DTOs, and primitives) independent of any specific data access technology, and offering specialized, high-performance integration extensions for various ORMs.

## Core Design Principles

1. **Abstractions First**: The contract is completely independent of the implementation.
2. **Performance by Default**: Avoiding `COUNT(*)` when unnecessary, zero-allocation structures where possible, and natively supporting `IAsyncEnumerable`.
3. **O(log n) Keyset Pagination (When Indexed)**: Native support for Keyset (Cursor) Pagination, avoiding the performance degradation of `OFFSET/FETCH` at large depths. Requires a database index on the keyset column(s) — without one, the database falls back to a sequential scan.

## Context Diagram (C4 Level 1)

The `/samples/DemoApp` project acts as a reference implementation of the library in a Clean Architecture context.

```mermaid
C4Context
    title System Context for DemoApp

    Person(user, "API Consumer", "A user of the Demo App API.")
    System(demoApp, "Demo API", "Provides paginated access to the domain entities using EricksonLopez.Pagination.")
    SystemDb(database, "PostgreSQL Database", "Stores the domain entities.")

    Rel(user, demoApp, "Requests paginated resources", "HTTPS/REST")
    Rel(demoApp, database, "Executes optimized SQL queries via EF Core/Dapper", "TCP")
```

## Component Architecture

```mermaid
graph TD
    subgraph "Application Layer (ASP.NET Core)"
        EP[Endpoint / Controller]
        MB["Model Binders:<br/>PaginationParametersModelBinder<br/>CursorPaginationParametersModelBinder"]
        F["Endpoint Filter:<br/>PaginationEndpointFilter"]
        R["Responses:<br/>PagedResponse<br/>CursorPagedResponse"]
    end

    subgraph "Core Library (EricksonLopez.Pagination)"
        PCO[PaginationCoreOptions]
        CPCO[PaginationCursorOptions]
        PL[PagedList]
        CPL[CursorPagedList]
        CE[ICursorEncoder]
        CD[ICursorDecoderRegistry]
    end

    subgraph "Abstractions (Zero Dependencies)"
        IPL[IPagedList]
        ICPL[ICursorPagedList]
    end

    subgraph "Infrastructure Layer (Data Access)"
        EF["EF Core:<br/>QueryableExtensions<br/>KeysetBuilder"]
        Dap["Dapper:<br/>DbConnectionPaginationExtensions"]
        Mdb["MongoDB:<br/>MongoQueryableExtensions"]
    end

    EP -->|Binds Request| MB
    EP -->|Validates| F
    MB --> PCO
    MB --> CPCO
    
    EP -->|Queries DB| EF
    EP -->|Queries DB| Dap
    EP -->|Queries DB| Mdb
    
    EF -->|Returns| PL
    EF -->|Returns| CPL
    
    PL -.-> IPL
    CPL -.-> ICPL
    
    EP -->|Formats Output| R
    R -.-> PL
    R -.-> CPL
```

## Primary Flow: Offset Pagination

```mermaid
sequenceDiagram
    participant Client
    participant API as ASP.NET Core API
    participant Binder as ModelBinder
    participant Handler as App Service / Handler
    participant EF as EF Core (QueryableExtensions)
    participant DB as Database

    Client->>API: GET /items?page=2&pageSize=20
    API->>Binder: BindModelAsync()
    Binder-->>API: PaginationParameters
    API->>Handler: Handle(PaginationParameters)
    Handler->>EF: query.ToPagedListAsync(parameters)
    
    EF->>DB: SELECT COUNT(*) FROM items
    DB-->>EF: 1500 (totalCount)
    
    EF->>DB: SELECT * FROM items OFFSET 20 LIMIT 20
    DB-->>EF: 20 rows
    
    EF-->>Handler: PagedList<T>
    Handler->>API: pagedList.ToPagedResponse()
    API-->>Client: PagedResponse<T> (JSON)
```

## Primary Flow: Keyset (Cursor) Pagination

```mermaid
sequenceDiagram
    participant Client
    participant API as ASP.NET Core API
    participant Binder as ModelBinder
    participant Handler as App Service / Handler
    participant Keyset as EF Core (KeysetBuilder)
    participant Encoder as ICursorEncoder
    participant DB as Database

    Client->>API: GET /items?after=Base64Cursor==&pageSize=20
    API->>Binder: BindModelAsync()
    Binder->>Encoder: Decode("Base64Cursor==")
    Encoder-->>Binder: Decoded Values (e.g. Id=45)
    Binder-->>API: CursorPaginationParameters
    
    API->>Handler: Handle(CursorPaginationParameters)
    Handler->>Keyset: query.OrderBy(x => x.Id).ToCursorPagedListAsync(parameters)
    
    Keyset->>DB: SELECT * FROM items WHERE Id > 45 ORDER BY Id LIMIT 21
    DB-->>Keyset: 21 rows (20 items + 1 to determine HasNextPage)
    
    Keyset->>Encoder: Encode(LastItem.Id)
    Encoder-->>Keyset: "NewBase64Cursor=="
    
    Keyset-->>Handler: CursorPagedList<T>
    Handler->>API: cursorList.ToCursorPagedResponse(p => p.Id)
    API-->>Client: CursorPagedResponse<T> (JSON with RelayPageInfo)
```

## Package Dependency Graph

```mermaid
graph LR
    Abstractions["EricksonLopez.Pagination.Abstractions"]
    Core["EricksonLopez.Pagination"]
    AspNetCore["EricksonLopez.Pagination.AspNetCore"]
    EFCore["EricksonLopez.Pagination.EntityFrameworkCore"]
    Dapper["EricksonLopez.Pagination.Dapper"]
    Mongo["EricksonLopez.Pagination.MongoDB"]
    Cosmos["EricksonLopez.Pagination.Cosmos"]
    Elasticsearch["EricksonLopez.Pagination.Elasticsearch"]
    LinqToDB["EricksonLopez.Pagination.LinqToDB"]
    Redis["EricksonLopez.Pagination.Redis"]
    Relay["EricksonLopez.Pagination.Relay"]
    Grpc["EricksonLopez.Pagination.Grpc"]
    Blazor["EricksonLopez.Pagination.Blazor"]
    OpenApi["EricksonLopez.Pagination.OpenApi"]
    Result["EricksonLopez.Pagination.Result"]
    SrcGen["EricksonLopez.Pagination.SourceGenerators"]
    Analyzers["EricksonLopez.Pagination.Analyzers"]

    Core --> Abstractions
    AspNetCore --> Core
    EFCore --> Core
    Dapper --> Core
    Dapper --> Abstractions
    Mongo --> Core
    Cosmos --> Core
    Elasticsearch --> Core
    LinqToDB --> Core
    LinqToDB --> Abstractions
    Redis --> Abstractions
    Relay --> Core
    Relay --> Abstractions
    OpenApi --> Core
    Result --> Core
    Result --> Abstractions
    Blazor --> Abstractions
    Grpc --> Abstractions
    SrcGen -..->|"Roslyn Generator<br/>(compile-time only)"| Core
    Analyzers -..->|"Roslyn Analyzer<br/>(compile-time only)"| Core
```

> [!NOTE]
> The dependency graph above reflects the actual `ProjectReference` entries in each `.csproj` file. `Blazor`, `Grpc`, and `Redis` depend only on `Abstractions` (not `Core`), keeping their dependency footprint minimal. `OpenApi` depends on `Core` (not `AspNetCore`). `SourceGenerators` and `Analyzers` are Roslyn analyzers that run at compile-time and do not create runtime dependencies. `EricksonLopez.Pagination.Result` currently only targets `net10.0`.
