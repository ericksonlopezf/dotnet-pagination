# API Reference: EricksonLopez.Pagination.EntityFrameworkCore

> Microsoft Learn-Style Reference for Entity Framework Core query extensions, multi-column KeysetBuilder, partitioning, and streaming.

---

## `KeysetBuilder<T>` (sealed class)

**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`  
**Assembly:** `EricksonLopez.Pagination.EntityFrameworkCore.dll`

Fluent builder that compiles type-safe, multi-column keyset (cursor) pagination over EF Core `IQueryable<T>` into direct B-Tree index seek queries.

### Signature
```csharp
public sealed class KeysetBuilder<T> where T : class
```

### Methods

#### `Ascending<TKey>(Expression<Func<T, TKey>> keySelector)`
Appends an ascending sort/seek column to the keyset hierarchy.
- **Parameters:** `Expression<Func<T, TKey>> keySelector`: Property selector.
- **Return:** `KeysetBuilder<T>`

#### `Descending<TKey>(Expression<Func<T, TKey>> keySelector)`
Appends a descending sort/seek column to the keyset hierarchy.
- **Parameters:** `Expression<Func<T, TKey>> keySelector`: Property selector.
- **Return:** `KeysetBuilder<T>`

#### `ToCursorPagedListAsync(CancellationToken cancellationToken = default)`
Executes the compiled keyset seek query against EF Core, slices the extra probe element to compute `HasNextPage`/`HasPreviousPage`, and generates opaque cursors for the boundaries.
- **Return:** `Task<ICursorPagedList<T>>`
- **Performance:** $O(\log N)$ database index seek. Zero row skips.

#### `ToPagedAsyncEnumerable(CancellationToken cancellationToken = default)`
Streams matching keyset elements asynchronously as `IAsyncEnumerable<T>`.
- **Return:** `IAsyncEnumerable<T>`

---

## `QueryableExtensions` (static class)

**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`  
**Assembly:** `EricksonLopez.Pagination.EntityFrameworkCore.dll`

Primary extension methods extending `IQueryable<T>`.

### Methods

#### `Keyset<T>(this IQueryable<T> source, CursorPaginationParameters parameters, int defaultPageSize = 10, ICursorEncoder? cursorEncoder = null, bool acceptLegacyCursors = true)`
Initializes a `KeysetBuilder<T>` for multi-column cursor pagination.
- **Return:** `KeysetBuilder<T>`

#### `ToPagedListAsync<T>(this IQueryable<T> source, PaginationParameters parameters, bool countTotal = true, int? maxPageSize = null, CancellationToken cancellationToken = default)`
Executes standard offset pagination.
- **Parameters:**
  - `PaginationParameters parameters`: Page and PageSize.
  - `bool countTotal`: If `true`, executes `COUNT(*)` and returns `CountedPagedList<T>`. If `false`, executes fast-path `Take(pageSize + 1)` and returns `PagedList<T>`.
  - `int? maxPageSize`: Optional local cap overriding global default.
  - `CancellationToken cancellationToken`: Cancellation token.
- **Return:** `Task<IPagedList<T>>`

#### `ApplyFilter<T>(this IQueryable<T> source, FilterParameters filter, IReadOnlySet<string>? allowedProperties = null, PaginationCoreOptions? options = null, IFilterOperatorProvider<T>? operatorProvider = null)`
Applies dynamic filter DSL expressions to `IQueryable<T>` via cached expression trees.
- **Return:** `IQueryable<T>`

#### `ApplySort<T>(this IQueryable<T> source, SortParameters sort, Expression<Func<T, object>> defaultSort, IReadOnlySet<string>? allowedProperties = null)`
Applies multi-column dynamic sorting to `IQueryable<T>`.
- **Return:** `IQueryable<T>`

#### `ToPagedListBatchedAsync<T>(this IQueryable<T> source, int batchSize = 100, CancellationToken cancellationToken = default)`
Returns an `IAsyncEnumerable<IPagedList<T>>` that paginates through the entire queryable in chunks of `batchSize`.
- **Return:** `IAsyncEnumerable<IPagedList<T>>`

---

## `KeysetPartitioningExtensions` (static class)

**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`  
**Assembly:** `EricksonLopez.Pagination.EntityFrameworkCore.dll`

Extensions for splitting massive datasets into non-overlapping keyset partitions for parallel worker execution.

### Method
```csharp
public static Task<IReadOnlyList<KeysetPartition<TKey>>> SplitKeysetPartitionsAsync<T, TKey>(
    this IQueryable<T> source,
    Expression<Func<T, TKey>> keySelector,
    int partitionCount,
    CancellationToken cancellationToken = default)
```
- **Parameters:**
  - `Expression<Func<T, TKey>> keySelector`: Monotonically increasing key selector.
  - `int partitionCount`: Number of disjoint partitions to generate.
- **Return:** `Task<IReadOnlyList<KeysetPartition<TKey>>>`

---

## Database-Specific Optimizations

### `PostgreSqlPaginationExtensions`
- **`CountApproximatePostgreSqlAsync<T>`**: Instantaneous $O(1)$ table count estimate using PostgreSQL `pg_class.reltuples`.

### `OraclePaginationExtensions`
- Oracle SQL dialect query generation (`OFFSET...FETCH` vs `ROWNUM`).

### `SqlServerPaginationExtensions`
- SQL Server `OFFSET...FETCH` query hints.
