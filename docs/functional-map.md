# Functional Map — EricksonLopez.Pagination

> Official Architectural and Functional Flow Specification for `EricksonLopez.Pagination`.

This document maps all public library components across all architectural layers, describing end-to-end execution flows from entry point to response generation.

---

## 1. Architectural Layers Overview

```
┌────────────────────────────────────────────────────────────────────────┐
│                        ENTRY LAYER (ASP.NET Core)                      │
│   • PaginationParameters / CursorPaginationParameters (IParsable)      │
│   • FilterParameters / SortParameters (DSL Binding)                    │
│   • PaginationEndpointFilter (MaxPageSize Validation)                  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                    SECURITY & VERIFICATION LAYER                       │
│   • ICursorEncoder (Base64CursorEncoder / HmacCursorEncoder)           │
│   • HMAC-SHA256 Signature Verification                                 │
│   • TTL & Clock Skew Validation (ExpiredPaginationCursorException)     │
│   • Nonce Replay Store (ICursorReplayStore / InMemoryCursorReplayStore)│
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                      PROCESSING & QUERY LAYER                          │
│   • EF Core: QueryableExtensions, KeysetBuilder<T>, KeysetPartitioning  │
│   • Dapper: DbConnectionPaginationExtensions, DapperKeysetBuilder<T>   │
│   • LINQ / MongoDB / Cosmos DB / LinqToDB Providers                     │
│   • FilterExpression & ExpressionCache (Thread-safe Predicates)        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                     OUTPUT & TELEMETRY LAYER                           │
│   • PagedResponse<T> / CursorPagedResponse<T> (Relay Specification)    │
│   • Deterministic ETags & 304 Not Modified (ToPagedResult)             │
│   • PaginationExceptionHandler (IExceptionHandler -> ProblemDetails)   │
│   • OpenTelemetry Metrics (PaginationMetrics) & Logging               │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Execution Flows

### Flow 1: Standard Offset Pagination (EF Core)

```
HTTP Client GET /api/products?page=2&pageSize=20&filter=price>=50&sortBy=name asc
   │
   ▼
[1. Binding]
   • PaginationParameters, FilterParameters, SortParameters parsed via IParsable<T> / ModelBinders.
   │
   ▼
[2. Endpoint Validation]
   • PaginationEndpointFilter checks: PageSize <= PaginationCoreOptions.MaxPageSize.
   │
   ▼
[3. Filter & Sort Compilation]
   • ApplyFilter(filter): Looks up / compiles predicate from ExpressionCache.
   • ApplySort(sort): Validates property allowlist and adds OrderBy / ThenBy.
   │
   ▼
[4. Query Execution - ToPagedListAsync]
   ├── If countTotal == true:
   │     1. Executes COUNT(*) query for ExactTotalCount.
   │     2. Executes SELECT ... OFFSET 20 LIMIT 20.
   │     └── Returns CountedPagedList<T>.
   └── If countTotal == false (Countless fast-path):
         1. Executes SELECT ... OFFSET 20 LIMIT 21 (Take PageSize + 1).
         2. HasNextPage = items.Count > PageSize.
         └── Returns PagedList<T>.
   │
   ▼
[5. In-Memory Projections (Optional)]
   • Map(dtoSelector) or LazyMap(dtoSelector).
   │
   ▼
[6. Response Formatting]
   • ToPagedResponse(): Emits metadata (Page, PageSize, TotalPages, HasNextPage, HasPreviousPage).
   • ToPagedResult(request, maxAge): Generates SHA-256 ETag -> returns 304 Not Modified or 200 OK.
   • Telemetry: PaginationMetrics.RecordOffsetQuery(page, pageSize).
```

---

### Flow 2: Keyset (Cursor) Pagination with HMAC & Replay Protection

```
HTTP Client GET /api/products/keyset?first=10&after=<hmac_signed_opaque_cursor>
   │
   ▼
[1. Binding]
   • CursorPaginationParameters (First, After, Last, Before) bound automatically.
   │
   ▼
[2. Cursor Decoding & Cryptographic Verification]
   • HmacCursorEncoder.Decode(after):
       1. Base64Url decode payload.
       2. Computes HMAC-SHA256 over content and verifies FixedTimeEquals.
            └── If signature mismatch -> throws InvalidPaginationCursorException (tampered).
       3. Checks expiration timestamp against UtcNow + ClockSkewTolerance.
            └── If expired -> throws ExpiredPaginationCursorException (expired).
       4. Checks nonce against ICursorReplayStore.TryAcquireNonce.
            └── If nonce was previously acquired -> throws ReplayedPaginationCursorException (replayed).
   │
   ▼
[3. Keyset Query Building]
   • KeysetBuilder<T>:
       - .Ascending(p => p.Price)
       - .Ascending(p => p.Id)  <-- Mandatory unique tie-breaker
       - Generates SQL: WHERE (Price > @LastPrice) OR (Price = @LastPrice AND Id > @LastId)
       - Appends LIMIT 11 (PageSize + 1).
   │
   ▼
[4. Query Execution]
   • Executes optimized B-Tree index seek (O(log N) complexity, 0 row offset skip).
   │
   ▼
[5. Response Construction]
   • Extracts new StartCursor and EndCursor from first and last elements.
   • Encodes new cursors with fresh HMAC signatures and unique nonces.
   • Returns CursorPagedResponse<T> with Relay PageInfo & Edges.
   • Telemetry: PaginationMetrics.RecordKeysetQuery(pageSize).
```

---

### Flow 3: Dapper Keyset Pagination (`DapperKeysetBuilder<T>`)

```
HTTP Client GET /api/products/dapper-keyset?first=20&after=<cursor>
   │
   ▼
[1. DapperKeysetBuilder Initialization]
   • Connection and CursorPaginationParameters injected.
   │
   ▼
[2. Fluent Configuration]
   • .Select("Id, Name, Price")
   • .From("Product")
   • .OrderBy("Price", SortDirection.Ascending)
   • .ThenBy("Id", SortDirection.Ascending)
   • .WithCursorColumns(p => p.Price.ToString(), p => p.Id.ToString())
   • .WithCursorDecoder(parts => decimal.Parse(parts[0]), parts => int.Parse(parts[1]))
   • .UseDialect(DatabaseDialect.Sqlite)
   │
   ▼
[3. SQL & Parameter Generation]
   • Injects @__Pagination_Limit__ (PageSize + 1) and @Cursor0, @Cursor1 parameters.
   • CursorSqlBuilder generates multi-column SQL WHERE clauses matching dialect.
   │
   ▼
[4. Execution & Slicing]
   • Executes via connection.QueryAsync<T>().
   • Slices extra item to determine HasNextPage.
   • Encodes StartCursor and EndCursor via configured ICursorEncoder.
   └── Returns ICursorPagedList<T>.
```

---

### Flow 4: Parallel Keyset Partitioning (`KeysetPartitioningExtensions`)

```
Batch Worker / ETL Job requests N parallel partitions
   │
   ▼
[1. Partition Request]
   • db.Products.SplitKeysetPartitionsAsync(p => p.Id, partitionCount: 4)
   │
   ▼
[2. Boundary Determination]
   • Executes MinAsync(keySelector) and MaxAsync(keySelector).
   • Calculates step = Math.Ceiling((Max - Min + 1) / partitionCount).
   │
   ▼
[3. Partition Array Generation]
   • Partition 0: [Min, Min + step - 1] (LowerBound = Min, UpperBound = Min + step - 1)
   • Partition 1: [Min + step, Min + 2*step - 1]
   • ...
   • Partition N-1: [Min + (N-1)*step, Max]
   │
   ▼
[4. Parallel Worker Distribution]
   • Workers execute queries concurrently: WHERE Id >= LowerBound AND Id <= UpperBound
   • Zero row overlap, zero contention, perfectly distributed workload.
```

---

## 3. Global Exception & Error Handling

```
Application Pipeline
   │
   ├── InvalidPaginationCursorException (tampered cursor)
   │      └── Intercepted by PaginationExceptionHandler -> HTTP 400 Bad Request + ProblemDetails
   │
   ├── ExpiredPaginationCursorException (TTL exceeded)
   │      ├── Handled explicitly at endpoint -> HTTP 410 Gone
   │      └── Default fallback -> HTTP 400 Bad Request + ProblemDetails
   │
   ├── ReplayedPaginationCursorException (single-use replay detected)
   │      ├── Handled explicitly at endpoint -> HTTP 409 Conflict
   │      └── Default fallback -> HTTP 400 Bad Request + ProblemDetails
   │
   └── ArgumentOutOfRangeException (Page < 1, PageSize < 1)
          └── ASP.NET Core Model Binding -> HTTP 400 Bad Request
```
