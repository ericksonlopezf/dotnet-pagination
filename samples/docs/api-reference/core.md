# API Reference: EricksonLopez.Pagination

> Microsoft Learn-Style Reference for core implementations, encoders, options, metrics, and diagnostics.

---

## `PagedList<T>` (class)

**Namespace:** `EricksonLopez.Pagination`  
**Assembly:** `EricksonLopez.Pagination.dll`  
**Implements:** `IPagedList<T>`, `IReadOnlyList<T>`

In-memory implementation of `IPagedList<T>` for offset-paginated data collections.

### Signature
```csharp
public class PagedList<T> : IPagedList<T>, IReadOnlyList<T>
```

### Static Factory Methods

#### `WithCount(IReadOnlyList<T> items, PaginationParameters parameters, long totalCount)`
Creates a `CountedPagedList<T>` instance with known exact total count.
- **Parameters:**
  - `IReadOnlyList<T> items`: Current page items.
  - `PaginationParameters parameters`: Requested pagination parameters.
  - `long totalCount`: Total records in the unfiltered dataset.
- **Return:** `CountedPagedList<T>`
- **Exceptions:** `ArgumentNullException` if `items` is null.
- **When to use:** Use when total record count is known (e.g., standard SQL count or Dapper dual query).

#### `WithoutCount(IReadOnlyList<T> items, PaginationParameters parameters, bool hasNextPage)`
Creates a `PagedList<T>` with `TotalCount = null` for countless fast-path queries.
- **Parameters:**
  - `IReadOnlyList<T> items`: Current page items.
  - `PaginationParameters parameters`: Requested parameters.
  - `bool hasNextPage`: Whether additional pages exist.
- **Return:** `PagedList<T>`
- **When to use:** Use in infinite scrolling feeds where `COUNT(*)` overhead is avoided.

#### `Empty(PaginationParameters parameters)`
Returns an empty paged list with `TotalCount = 0` and empty `Items`.
- **Return:** `CountedPagedList<T>`

### Instance Methods

#### `Map<TResult>(Func<T, TResult> selector)`
Transforms elements immediately into a new `IPagedList<TResult>`, preserving pagination metadata.
- **Parameters:** `Func<T, TResult> selector`
- **Return:** `IPagedList<TResult>` (or `CountedPagedList<TResult>` if count is present).
- **Performance:** Allocates a new list of size `Items.Count`.

#### `LazyMap<TResult>(Func<T, TResult> selector)`
Transforms elements lazily on enumeration or serialization, avoiding intermediate list allocations.
- **Parameters:** `Func<T, TResult> selector`
- **Return:** `IPagedList<TResult>`
- **Performance:** Zero intermediate array allocations.

---

## `CursorPagedList<T>` (class)

**Namespace:** `EricksonLopez.Pagination`  
**Assembly:** `EricksonLopez.Pagination.dll`  
**Implements:** `ICursorPagedList<T>`, `IReadOnlyList<T>`

Core in-memory representation of keyset / cursor paginated collections.

### Signature
```csharp
public class CursorPagedList<T> : ICursorPagedList<T>, IReadOnlyList<T>
```

### Properties
- `IReadOnlyList<T> Items { get; }`
- `string? StartCursor { get; }`
- `string? EndCursor { get; }`
- `bool HasNextPage { get; }`
- `bool HasPreviousPage { get; }`

### Factory Method
#### `Create(IReadOnlyList<T> items, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)`
- **Return:** `CursorPagedList<T>`

---

## `HmacCursorEncoder` (sealed class)

**Namespace:** `EricksonLopez.Pagination`  
**Assembly:** `EricksonLopez.Pagination.dll`  
**Implements:** `ICursorEncoder`, `IDisposable`

Provides cryptographic signing and verification of cursor tokens using HMAC-SHA256 with optional expiration (TTL), clock-skew tolerance, and replay protection.

### Signature
```csharp
public sealed class HmacCursorEncoder : ICursorEncoder, IDisposable
```

### Constructor
```csharp
public HmacCursorEncoder(
    string secretKey,
    TimeSpan? timeToLive = null,
    TimeSpan? clockSkewTolerance = null,
    ICursorReplayStore? replayStore = null)
```
- **Parameters:**
  - `string secretKey`: Minimum 32 bytes (UTF-8) HMAC signing key.
  - `TimeSpan? timeToLive`: Maximum lifespan of generated cursors before throwing `ExpiredPaginationCursorException`.
  - `TimeSpan? clockSkewTolerance`: Tolerance for distributed server clock drift. Defaults to 60 seconds.
  - `ICursorReplayStore? replayStore`: Store for single-use nonce tracking.

### Static Properties
- `HmacCursorEncoder DevelopmentDefault`: Built-in instance for local development and smoke tests. **Do not use in production.**

### Methods
#### `Encode(string? rawCursor)`
Generates an opaque Base64Url token containing payload, expiration timestamp, nonce, and HMAC-SHA256 signature.
- **Return:** `string?`

#### `Decode(string? opaqueCursor)`
Validates HMAC signature in constant time (`CryptographicOperations.FixedTimeEquals`), verifies TTL, checks nonce against replay store, and returns raw payload.
- **Exceptions:**
  - `InvalidPaginationCursorException`: Signature mismatch or corrupt Base64 payload.
  - `ExpiredPaginationCursorException`: Cursor expired.
  - `ReplayedPaginationCursorException`: Nonce already acquired.

---

## `Base64CursorEncoder` (sealed class)

**Namespace:** `EricksonLopez.Pagination`  
**Assembly:** `EricksonLopez.Pagination.dll`  
**Implements:** `ICursorEncoder`

Simple Base64Url encoder/decoder for development or internal services where cryptographic signing is handled by downstream proxies.

---

## `PaginationCoreOptions` (class)

**Namespace:** `EricksonLopez.Pagination`  
**Assembly:** `EricksonLopez.Pagination.dll`  
**Implements:** `IValidatableObject`

Core configuration options registered in Dependency Injection via `AddPagination()`.

### Properties
- `int DefaultPageSize`: Fallback page size when omitted by client. Default: `20`.
- `int MaxPageSize`: Upper bound limit enforced across all endpoints. Default: `100`.
- `int DeepOffsetWarningThreshold`: Offset depth at which warning logs are emitted. Default: `500`.
- `int MaxFilterComplexity`: Maximum condition tokens allowed in filter DSL strings. Default: `10`.
- `int MaxFilterStringLength`: Maximum character length of filter query parameter. Default: `500`.
- `int MaxFilterValueLength`: Maximum length of an individual filter value token. Default: `200`.
- `int MaxPropertyDepth`: Maximum nested property navigation depth (e.g. `User.Profile.City`). Default: `2`.
- `bool AcceptLegacyCursors`: Compatibility flag for parsing legacy unversioned cursors. Default: `true`.
- `FilterUnknownFieldBehavior UnknownFieldBehavior`: `ThrowException` or `Ignore`. Default: `ThrowException`.

---

## Observability & Diagnostics

### `PaginationMetrics` (static class)
OpenTelemetry metrics collection instruments under meter `"EricksonLopez.Pagination"`:
- `RecordOffsetQuery(int page, int pageSize)`
- `RecordKeysetQuery(int pageSize)`
- `RecordCursorError(string errorReason)`
- `RecordQueryDuration(double milliseconds, string strategy)`

### `PaginationLogEvents` (static class)
High-performance compile-time logger definitions:
- `LogCursorExpired(ILogger? logger, string cursor, DateTimeOffset expiredAt)`
- `LogCursorTampered(ILogger? logger, string cursor)`
- `LogCursorReplayed(ILogger? logger, string cursor, string nonce)`
- `LogDeepOffsetWarning(ILogger? logger, int page, int pageSize, int threshold)`

### `PaginationDiagnostics` (static class)
Diagnostic hooks for non-DI scenarios and testing harnesses:
- `ILogger CreateLogger<T>()`
- `Meter Meter { get; }`
