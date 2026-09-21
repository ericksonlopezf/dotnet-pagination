# API Reference: EricksonLopez.Pagination.Abstractions

> Microsoft Learn-Style Reference for core abstractions, contracts, structs, and typed exceptions.

---

## `PaginationParameters` (readonly record struct)

**Namespace:** `EricksonLopez.Pagination.Abstractions`  
**Assembly:** `EricksonLopez.Pagination.Abstractions.dll`  
**Implements:** `IParsable<PaginationParameters>`, `IEquatable<PaginationParameters>`

Encapsulates input parameters for standard offset pagination (`Page` and `PageSize`). Provides fast-path `IParsable` implementation for zero-allocation binding in Minimal APIs.

### Signature
```csharp
public readonly record struct PaginationParameters : IParsable<PaginationParameters>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Skip => (Page - 1) * PageSize;
}
```

### Properties
- **`Page`** (`int`): The 1-based page number. Defaults to `1`.
- **`PageSize`** (`int`): The number of items requested per page. Defaults to `20`.
- **`Skip`** (`int`): Computed record offset: `(Page - 1) * PageSize`.

### Methods

#### `Create(int page, int pageSize)`
Validates parameters during instantiation and returns a normalized `PaginationParameters`.
- **Parameters:**
  - `int page`: The page index (must be $\ge 1$).
  - `int pageSize`: The page size (must be $\ge 1$).
- **Return:** `PaginationParameters`
- **Exceptions:** `ArgumentOutOfRangeException` if `page < 1` or `pageSize < 1`.
- **When to use:** Use when constructing parameters programmatically in domain services or tests.
- **When NOT to use:** Avoid when using Minimal API endpoint binding (framework binds `[AsParameters]` automatically).

#### `TryParse(string? s, IFormatProvider? provider, out PaginationParameters result)`
Parses a comma-separated `"page,pageSize"` or single integer string.
- **Parameters:**
  - `string? s`: The input string representation.
  - `IFormatProvider? provider`: Format provider.
  - `out PaginationParameters result`: The parsed instance if successful.
- **Return:** `bool`: `true` if parsed successfully; otherwise `false`.

### Examples
#### Basic Example
```csharp
app.MapGet("/api/items", async ([AsParameters] PaginationParameters pagination, ApplicationDbContext db, CancellationToken ct) =>
{
    return Results.Ok(await db.Items.OrderBy(x => x.Id).ToPagedListAsync(pagination, cancellationToken: ct));
});
```

#### Advanced Example
```csharp
var pagination = PaginationParameters.Create(page: 3, pageSize: 50);
var paged = await repository.GetPagedAsync(pagination, ct);
```

### Best Practices & Performance
- Always use `[AsParameters]` in Minimal APIs for Native AOT zero-reflection binding.
- Ensure `PageSize` does not exceed `PaginationCoreOptions.MaxPageSize`.

---

## `CursorPaginationParameters` (readonly record struct)

**Namespace:** `EricksonLopez.Pagination.Abstractions`  
**Assembly:** `EricksonLopez.Pagination.Abstractions.dll`  
**Implements:** `IParsable<CursorPaginationParameters>`, `IEquatable<CursorPaginationParameters>`

Encapsulates cursor pagination parameters conforming to the GraphQL Relay specification (`First`, `After`, `Last`, `Before`).

### Signature
```csharp
public readonly record struct CursorPaginationParameters : IParsable<CursorPaginationParameters>
{
    public int? First { get; init; }
    public string? After { get; init; }
    public int? Last { get; init; }
    public string? Before { get; init; }
}
```

### Properties
- **`First`** (`int?`): Count of items requested forward from `After`.
- **`After`** (`string?`): Opaque cursor representing the start of forward pagination.
- **`Last`** (`int?`): Count of items requested backward from `Before`.
- **`Before`** (`string?`): Opaque cursor representing the start of backward pagination.

### Methods

#### `GetPageSize(int defaultSize = 10)`
Resolves the effective page size from `First` (forward) or `Last` (backward) with a fallback.
- **Signature:** `public int GetPageSize(int defaultSize = 10)`
- **Return:** `int`: The resolved page size.
- **When to use:** Use when calculating query limits in custom keyset providers or Dapper queries.

#### `TryParse(string? s, IFormatProvider? provider, out CursorPaginationParameters result)`
Parses cursor parameters from formatted query representations.

### Examples
#### Keyset Forward Endpoint
```csharp
app.MapGet("/api/feed", async ([AsParameters] CursorPaginationParameters cursor, ApplicationDbContext db, CancellationToken ct) =>
{
    var page = await db.Posts
        .Keyset(cursor)
        .Descending(p => p.CreatedAt)
        .Ascending(p => p.Id)
        .ToCursorPagedListAsync(cancellationToken: ct);

    return Results.Ok(page.ToCursorPagedResponse(p => $"{p.CreatedAt:O}|{p.Id}"));
});
```

---

## `FilterParameters` (readonly record struct)

**Namespace:** `EricksonLopez.Pagination.Abstractions`  
**Assembly:** `EricksonLopez.Pagination.Abstractions.dll`  
**Implements:** `IParsable<FilterParameters>`, `IEquatable<FilterParameters>`

Models dynamic filter DSL query tokens (`filter=name~=John,price>=100`).

### Signature
```csharp
public readonly record struct FilterParameters(string? Value) : IParsable<FilterParameters>
```

### Properties
- **`Value`** (`string?`): The raw filter DSL string passed in the query parameter.
- **`HasValue`** (`bool`): Returns `true` if `Value` is non-empty and non-whitespace.

### Filter DSL Grammar
- Comma `,`: Logical AND operator (`name~=Pro,price>=50`).
- Pipe `|`: Logical OR operator (`status=Active|status=Pending`).
- Operators: `=` (Equals), `!=` (Not equals), `>=` (Greater/equal), `<=` (Less/equal), `>` (Greater), `<` (Less), `~=` (Contains), `^=` (Starts with), `$=` (Ends with).

---

## `SortParameters` (readonly record struct)

**Namespace:** `EricksonLopez.Pagination.Abstractions`  
**Assembly:** `EricksonLopez.Pagination.Abstractions.dll`  
**Implements:** `IParsable<SortParameters>`, `IEquatable<SortParameters>`

Models dynamic multi-column sorting tokens (`sortBy=createdAt desc,id asc`).

### Signature
```csharp
public readonly record struct SortParameters(string? Value) : IParsable<SortParameters>
```

### Properties
- **`Value`** (`string?`): The raw sorting string.
- **`HasValue`** (`bool`): `true` if `Value` is not null or whitespace.

---

## Interfaces

### `IPagedList<out T>` & `IPagedList`
Contract representing an offset-paginated collection.
- **Properties:**
  - `IReadOnlyList<T> Items { get; }`
  - `int Page { get; }`
  - `int PageSize { get; }`
  - `long? TotalCount { get; }`
  - `int? TotalPages { get; }`
  - `bool HasNextPage { get; }`
  - `bool HasPreviousPage { get; }`

### `ICountedPagedList<out T>`
Extends `IPagedList<T>` guaranteeing a non-null `ExactTotalCount`.
- **Property:** `long ExactTotalCount { get; }`

### `ICursorPagedList<out T>` & `ICursorPagedList`
Contract representing a keyset/cursor-paginated collection.
- **Properties:**
  - `IReadOnlyList<T> Items { get; }`
  - `string? StartCursor { get; }`
  - `string? EndCursor { get; }`
  - `bool HasNextPage { get; }`
  - `bool HasPreviousPage { get; }`

### `ICursorEncoder`
Contract for cursor serialization, encryption, and verification.
- **Methods:**
  - `string? Encode(string? rawCursor)`
  - `string? Decode(string? opaqueCursor)`

### `ICursorReplayStore`
Contract for distributed or in-memory cursor replay detection.
- **Methods:**
  - `bool TryAcquireNonce(string nonce, TimeSpan timeToLive)`
  - `Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default)`

### `IFilterProvider<TEntity>`
Interface for compile-time source-generated filter evaluators (AOT compliant).
- **Method:** `Expression<Func<TEntity, bool>>? BuildPredicate(FilterParameters filter)`

### `IFilterOperatorProvider<TEntity>`
Interface for registering custom filter operators (e.g. `%=` fuzzy match, `geo_near`).
- **Property:** `IReadOnlyDictionary<string, FilterOperatorHandler<TEntity>> Operators { get; }`

---

## Typed Exceptions

### `InvalidPaginationCursorException`
Thrown when an opaque cursor is malformed, cannot be decoded, or has a tampered HMAC signature.
- **Base:** `Exception`
- **HTTP Status:** 400 Bad Request
- **Properties:** `string? OpaqueCursor { get; }`

### `ExpiredPaginationCursorException`
Thrown when a cursor's HMAC signature is valid, but its embedded timestamp exceeds the configured `timeToLive`.
- **Base:** `InvalidPaginationCursorException`
- **HTTP Status:** 410 Gone / 400 Bad Request
- **Properties:** `DateTimeOffset ExpiredAt { get; }`

### `ReplayedPaginationCursorException`
Thrown when a single-use cursor nonce has already been consumed in the active `ICursorReplayStore`.
- **Base:** `InvalidPaginationCursorException`
- **HTTP Status:** 409 Conflict / 400 Bad Request
- **Properties:** `string Nonce { get; }`

---

## Attributes

### `[Filterable]`
Marks a model property as eligible for dynamic filtering via `ApplyFilter`.
- **Usage:** Prevents sensitive fields (passwords, hashes, internal IDs) from being queried.

### `[GenerateFilterProvider]`
Triggers source generation of an AOT-safe `IFilterProvider<T>` at compile-time.
