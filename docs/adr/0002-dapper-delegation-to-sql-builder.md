# ADR-0002: Delegation of Dapper Pagination to SQL Builder

## Status
Amended — August 2026 (see Decision 2 below)

> **Note**: This ADR documents two related decisions: (1) the initial reversal of the plan to delegate to an external SQL builder, and (2) the subsequent internal `CursorSqlBuilder` design for keyset pagination within the Dapper package.

---

## Decision 1 — Restore `EricksonLopez.Pagination.Dapper` (original)

### Context
Initially, we considered removing the `EricksonLopez.Pagination.Dapper` package to delegate raw SQL generation to an external `dotnet-sql-builder` repository. The rationale was to avoid maintaining dialect-specific string manipulation within a pagination library.

However, community feedback and further architectural review revealed that requiring a full enterprise-grade SQL builder merely to paginate a query created unnecessary friction and a steep adoption barrier. A lightweight, focused Dapper integration within this repository provides immediate value to users without the overhead of an external SQL builder.

### Decision
We decided to **restore** and maintain the `EricksonLopez.Pagination.Dapper` package within this repository.

The package provides high-performance, dialect-aware extensions directly over `IDbConnection`, translating standard `PaginationParameters` into paginated queries. It remains a first-class citizen alongside the EF Core integration.

### Consequences
- **Positive:** Users achieve Dapper pagination immediately by installing a single package from this ecosystem, without needing an external SQL builder.
- **Positive:** Unified developer experience (DX) across EF Core and Dapper using the same `PaginationParameters`.
- **Negative:** We must maintain minimal SQL dialect generation (e.g., dialect-specific `LIMIT`/`OFFSET` clauses) within the Dapper extension package.

---

## Decision 2 — Internal `CursorSqlBuilder` for Keyset Pagination in Dapper (August 2026)

### Context
Keyset pagination in Dapper requires generating a `WHERE (col1, col2) > (@val1, @val2)` predicate and a matching `ORDER BY col1 ASC, col2 ASC` clause from a runtime-specified keyset definition. This is more complex than offset pagination's simple `OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY`.

An external SQL builder (e.g., `SqlKata`, `Dapper.SqlBuilder`) would provide this capability, but at the cost of:
1. A mandatory transitive dependency for all consumers of `EricksonLopez.Pagination.Dapper`
2. Coupling the library's SQL output format to the external library's opinionated DSL
3. Version skew risk (external library updates breaking our cursor format)

### Decision
Implement an **internal `CursorSqlBuilder`** class within `EricksonLopez.Pagination.Dapper` that generates the minimum SQL required for keyset pagination:

```csharp
// Generated for a 2-column keyset (Id ASC, CreatedAt DESC):
// WHERE (id > @p_id OR (id = @p_id AND created_at < @p_created_at))
// ORDER BY id ASC, created_at DESC
// LIMIT @take
```

The builder implements a **dialect fallback chain**: `PostgreSQL` → `SQLite` → `MySQL` → `MSSQL (FETCH NEXT)` → `Generic (LIMIT/OFFSET)`. The dialect is selected via `DapperPaginationDialect` (an enum on `DapperPaginationOptions`), defaulting to `Auto` which inspects the `IDbConnection` concrete type at runtime.

### Rationale

| Option | Dependency | Dialect support | Cursor integration |
|---|---|---|---|
| External SQL builder | Required | ✅ Full | ❌ Custom glue needed |
| Internal `CursorSqlBuilder` | None | ✅ Sufficient (top 4 dialects) | ✅ Native |
| Raw string interpolation | None | ❌ Error-prone | ❌ Manual |

The internal builder covers the top 4 dialects used by our consumer base (confirmed via GitHub Discussions survey). Exotic dialects (Oracle, Firebird, DB2) are intentionally not supported in v2 — consumers with these databases are directed to the EF Core provider or to implement `ICursorSqlGenerator`.

### Security Note
All column names used by `CursorSqlBuilder` are validated against an allowlist of `[A-Za-z_][A-Za-z0-9_]*` before interpolation (no user-controlled input reaches the SQL template). Parameter _values_ are always passed via Dapper's parameterized `DynamicParameters`. There is no string-concatenation of user values into SQL.

### Consequences
- **Positive:** Zero additional transitive dependencies for consumers.
- **Positive:** Full control over cursor format and SQL output.
- **Positive:** Extensible via `ICursorSqlGenerator` for non-standard dialects.
- **Negative:** We own the SQL generation logic and must test all 4 dialects in CI.
- **Negative:** Esoteric dialects are unsupported without consumer-provided `ICursorSqlGenerator`.
