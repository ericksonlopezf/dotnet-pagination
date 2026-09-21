# API Reference: EricksonLopez.Pagination.Dapper

> Microsoft Learn-Style Reference for Dapper micro-ORM extensions, multi-result set GridReader, and DapperKeysetBuilder.

---

## `DapperKeysetBuilder<T>` (sealed class)

**Namespace:** `EricksonLopez.Pagination.Dapper`  
**Assembly:** `EricksonLopez.Pagination.Dapper.dll`

Fluent builder that constructs dialect-aware multi-column SQL keyset queries over an open `IDbConnection`.

### Signature
```csharp
public sealed class DapperKeysetBuilder<T>
```

### Constructor
```csharp
public DapperKeysetBuilder(IDbConnection connection, CursorPaginationParameters parameters)
```

### Fluent Methods
- **`Select(string columns)`**: Defines columns to retrieve (`SELECT ...`).
- **`From(string table)`**: Target database table or view (`FROM ...`).
- **`Where(string condition)`**: Base filtering condition.
- **`OrderBy(string column, SortDirection direction)`**: Primary keyset column.
- **`ThenBy(string column, SortDirection direction)`**: Secondary keyset column.
- **`WithCursorColumns(params Func<T, string>[] cursorSelectors)`**: Serializers for extracting cursor values from materialized items.
- **`WithCursorDecoder(params Func<string[], object>[] columnDecoders)`**: Typed decoders for reconstructing SQL parameter boundaries from cursor tokens.
- **`UseDialect(DatabaseDialect dialect)`**: Sets database dialect for parameter naming and quoting (`SqlServer`, `PostgreSql`, `MySql`, `Sqlite`, `Oracle`).
- **`ExecuteAsync(CancellationToken cancellationToken = default)`**: Executes the compiled query and returns `Task<ICursorPagedList<T>>`.

---

## `DbConnectionPaginationExtensions` (static class)

**Namespace:** `EricksonLopez.Pagination.Dapper`  
**Assembly:** `EricksonLopez.Pagination.Dapper.dll`

Extensions extending `IDbConnection` for standard offset pagination.

### Methods

#### `ToPagedListAsync<T>(this IDbConnection connection, string sql, PaginationParameters parameters, object? param = null, bool countTotal = true, IDbTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default)`
- **When `countTotal: true`**: Expects `sql` to contain two statements returning two result sets (1: Total count, 2: Page records). Automatically injects `@__Pagination_Skip__` and `@__Pagination_PageSize__`. Returns `CountedPagedList<T>`.
- **When `countTotal: false`**: Expects a single statement. Injects `@__Pagination_Limit__` (`PageSize + 1`) and `@__Pagination_Skip__`. Returns `PagedList<T>`.

---

## `GridReaderPaginationExtensions` (static class)

**Namespace:** `EricksonLopez.Pagination.Dapper`  
**Assembly:** `EricksonLopez.Pagination.Dapper.dll`

Extensions for materializing `IPagedList<T>` from Dapper `SqlMapper.GridReader` when executing stored procedures or multiple result sets.

### Method
```csharp
public static async Task<IPagedList<T>> ReadPagedListAsync<T>(
    this SqlMapper.GridReader gridReader,
    PaginationParameters parameters,
    CancellationToken cancellationToken = default)
```
- **Execution:** First reads `int totalCount = await gridReader.ReadSingleAsync<int>()`, then reads `IEnumerable<T> items = await gridReader.ReadAsync<T>()`. Returns `CountedPagedList<T>`.

---

## `DatabaseDialect` (enum)

**Namespace:** `EricksonLopez.Pagination.Dapper`  
**Assembly:** `EricksonLopez.Pagination.Dapper.dll`

Supported SQL dialects for keyset query generation:
- `SqlServer`: Uses `@p` parameters and `OFFSET...FETCH NEXT` syntax.
- `PostgreSql`: Uses `$1` / `@p` parameters and `LIMIT...OFFSET` syntax.
- `MySql`: Uses `LIMIT offset, count` syntax.
- `Sqlite`: Uses `LIMIT count OFFSET offset` syntax.
- `Oracle`: Uses `FETCH FIRST n ROWS ONLY` syntax.
