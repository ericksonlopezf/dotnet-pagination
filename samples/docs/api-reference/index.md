# API Reference — EricksonLopez.Pagination

> Technical reference documentation for core components of `EricksonLopez.Pagination`.

---

## 1. `PaginationParameters` (Record Struct)

**Namespace:** `EricksonLopez.Pagination.Abstractions`  
**Assembly:** `EricksonLopez.Pagination.Abstractions.dll`

Models input parameters for standard offset pagination (`Page` and `PageSize`). Implements `IParsable<PaginationParameters>`.

### Signature
```csharp
public readonly record struct PaginationParameters : IParsable<PaginationParameters>
```

### Properties
- **`Page`** (`int`): Current page number (1-based). Default: `1`.
- **`PageSize`** (`int`): Number of items per page. Default: `20`.
- **`Skip`** (`int`): Calculated number of records to skip: `(Page - 1) * PageSize`.

### Static Methods
- **`Create(int page, int pageSize)`**: Validates and creates an immutable instance of `PaginationParameters`.
- **`TryParse(string? s, IFormatProvider? provider, out PaginationParameters result)`**: Attempts to deserialize from `page,pageSize` format.

---

## 2. `CursorPaginationParameters` (Record Struct)

**Namespace:** `EricksonLopez.Pagination.Abstractions`  
**Assembly:** `EricksonLopez.Pagination.Abstractions.dll`

Models cursor-based pagination parameters conforming to the GraphQL Relay specification.

### Signature
```csharp
public readonly record struct CursorPaginationParameters : IParsable<CursorPaginationParameters>
```

### Properties
- **`First`** (`int?`): Number of items requested forward.
- **`After`** (`string?`): Opaque cursor from which to start forward pagination.
- **`Last`** (`int?`): Number of items requested backward.
- **`Before`** (`string?`): Opaque cursor from which to start backward pagination.

---

## 3. `PagedList<T>` (Class)

**Namespace:** `EricksonLopez.Pagination`  
**Assembly:** `EricksonLopez.Pagination.dll`

In-memory implementation of `IPagedList<T>` for offset-paginated collections.

### Signature
```csharp
public class PagedList<T> : IPagedList<T>
```

### Properties
- **`Items`** (`IReadOnlyList<T>`): Items belonging to the current page.
- **`Page`** (`int`): Current page number.
- **`PageSize`** (`int`): Page size.
- **`TotalCount`** (`long?`): Total elements in the dataset (or `null` if `countTotal: false`).
- **`TotalPages`** (`int?`): Total calculated pages (or `null` if `TotalCount` is `null`).
- **`HasPreviousPage`** (`bool`): `true` if `Page > 1`.
- **`HasNextPage`** (`bool`): `true` if subsequent pages exist.

### Static Factory Methods
- **`WithCount(IReadOnlyList<T> items, PaginationParameters parameters, long totalCount)`**: Creates an instance with known exact total count.
- **`WithoutCount(IReadOnlyList<T> items, PaginationParameters parameters, bool hasNextPage)`**: Creates an instance without global total count (countless fast-path).
- **`Empty(PaginationParameters parameters)`**: Returns an empty paged list (`TotalCount = 0`).

---

## 4. `HmacCursorEncoder` (Class)

**Namespace:** `EricksonLopez.Pagination`  
**Assembly:** `EricksonLopez.Pagination.dll`

Implements `ICursorEncoder` and `IDisposable` to cryptographically sign and verify cursors using HMAC-SHA256.

### Signature
```csharp
public sealed class HmacCursorEncoder : ICursorEncoder, IDisposable
```

### Constructors
- **`HmacCursorEncoder(string secretKey, TimeSpan? timeToLive = null, TimeSpan? clockSkewTolerance = null, ICursorReplayStore? replayStore = null)`**: Initializes the encoder with a secret key (minimum 32 bytes in UTF-8), optional TTL, clock skew tolerance, and replay store.

### Methods
- **`Encode(string? rawCursor)`**: Generates a Base64Url-encoded opaque cursor with HMAC signature and timestamp.
- **`Decode(string? opaqueCursor)`**: Decodes and verifies HMAC signature integrity in constant time (`CryptographicOperations.FixedTimeEquals`), validating expiration and replay nonces.

---

## 5. `KeysetBuilder<T>` (Class)

**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`  
**Assembly:** `EricksonLopez.Pagination.EntityFrameworkCore.dll`

Fluent builder for constructing $O(\log N)$ keyset queries over EF Core `IQueryable<T>`.

### Methods
- **`Ascending<TKey>(Expression<Func<T, TKey>> keySelector)`**: Appends an ascending sort and seek column.
- **`Descending<TKey>(Expression<Func<T, TKey>> keySelector)`**: Appends a descending sort and seek column.
- **`ToCursorPagedListAsync(CancellationToken cancellationToken)`**: Executes the optimized keyset query and returns `ICursorPagedList<T>`.
- **`ToPagedAsyncEnumerable()`**: Returns an `IAsyncEnumerable<T>` streaming results reactively.
