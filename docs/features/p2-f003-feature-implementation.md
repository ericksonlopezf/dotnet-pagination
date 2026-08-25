# Feature Implementation

## Metadata

- Feature ID: P2-F003
- Feature: SQL Server approximate count (sys.dm_db_partition_stats)
- Phase: 2
- Package: EFCore
- Priority: MEDIUM
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0032 (Roadmap Phase 2 Parity)
- Dependencies: P1-F021 (PostgreSQL approximate count), P1-F009 (EF Core offset pagination)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Provide parity with PostgreSQL's `GetApproximateCountAsync` by implementing SQL Server metadata-based approximate counting via `sys.dm_db_partition_stats`, allowing O(1) total row estimates on multi-million row SQL Server tables without triggering expensive full-table `COUNT(*)` scans.

---

## 2. Roadmap Definition

From `roadmap.md` Phase 2:
- Feature: SQL Server approximate count (sys.dm_db_partition_stats)
- Priority: MEDIUM
- Notes: Parity with PostgreSQL approx count
- Acceptance Criteria: SQL Server approximate count extension implemented and integrated into `ToPagedListAsync(useApproximateCount: true)`.

---

## 3. Existing Repository State

- `PostgreSqlPaginationExtensions.GetApproximateCountAsync` uses `pg_class.reltuples`.
- `SqlServerPaginationExtensions` is now implemented with `GetSqlServerApproximateCountAsync` and `GetApproximateCountAsync` querying `sys.dm_db_partition_stats` on heap (index_id 0) and clustered index (index_id 1).
- `QueryableExtensions.GetTotalCountAsync` now detects SQL Server provider (`Microsoft.EntityFrameworkCore.SqlServer`) and dispatches to `SqlServerPaginationExtensions.GetApproximateCountAsync`.

---

## 4. Dependency Analysis

- Dependencies: `P1-F009` (EF Core offset pagination) and `P1-F021` (PostgreSQL approximate count) are COMPLETED.

---

## 5. Architecture Impact

Zero breaking changes. Seamless multi-database provider support in `ToPagedListAsync(..., useApproximateCount: true)` for both PostgreSQL and SQL Server.

---

## 6. Public API Design

```csharp
namespace EricksonLopez.Pagination.EntityFrameworkCore;

public static partial class SqlServerPaginationExtensions
{
    public static Task<long> GetSqlServerApproximateCountAsync(
        this DbContext dbContext,
        string tableName,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default);

    public static Task<long> GetApproximateCountAsync(
        DbContext dbContext,
        string tableName,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default);
}
```

---

## 7. Implementation Plan

### Step 1: Implementation
- [x] Create `src/EricksonLopez.Pagination.EntityFrameworkCore/SqlServerPaginationExtensions.cs`
- [x] Update `QueryableExtensions.GetTotalCountAsync` to recognize SQL Server provider and delegate to `SqlServerPaginationExtensions.GetApproximateCountAsync`

### Step 2: Unit Testing
- [x] Add unit tests verifying parameter validation, regex identifier checks, null checks, and provider dispatch in `tests/EricksonLopez.Pagination.EntityFrameworkCore.Tests/SqlServerPaginationExtensionsTests.cs`

### Step 3: Verification
- [x] Verify build and tests pass across all target frameworks (.NET 8, 9, 10)

---

## 8. Files

### Create
- [x] `src/EricksonLopez.Pagination.EntityFrameworkCore/SqlServerPaginationExtensions.cs`
- [x] `tests/EricksonLopez.Pagination.EntityFrameworkCore.Tests/SqlServerPaginationExtensionsTests.cs`

### Modify
- [x] `src/EricksonLopez.Pagination.EntityFrameworkCore/QueryableExtensions.cs`

---

## 9. Implementation Log

### 2026-08-14
- Action: Implemented `SqlServerPaginationExtensions`, updated `QueryableExtensions`, and added unit tests.
- Result: 272 EF Core tests passing.
- Tests: `SqlServerPaginationExtensionsTests` (9 test scenarios) verified.
- Issues: CS0121 ambiguity between extension methods resolved via explicit method naming (`GetSqlServerApproximateCountAsync`).
- Next action: Proceed to P2-F004 (Filter DSL: AOT-safe IFilterProvider<T> interface).

---

## 10. Tests

### Unit
- [x] Null DbContext throws `ArgumentNullException`
- [x] Null/empty table name throws `ArgumentException`
- [x] Invalid identifier (SQL injection attempts) throws `ArgumentException`
- [x] Dispatch in `ToPagedListAsync` with SQL Server provider
- [x] Zero / Null / Negative result fallbacks

---

## 11. Performance
- [x] O(1) metadata query via `sys.dm_db_partition_stats`.

---

## 12. AOT / Trimming
- [x] AOT safe: Uses ADO.NET `DbCommand` with parameters and `ExecuteScalarAsync`.

---

## 13. Mutation Testing
- [x] Tested with unit and edge-case validations.

---

## 14. Documentation
- [x] XML doc comments included.

---

## 15. Acceptance Criteria
- [x] `SqlServerPaginationExtensions.GetSqlServerApproximateCountAsync` implemented
- [x] `useApproximateCount: true` works on SQL Server in `ToPagedListAsync`

---

## 16. Definition of Done
- [x] Implementation complete, tested, documented, and passing.

---

## 17. Known Risks
- Like PostgreSQL statistics, partition stats are non-transactional and depend on engine metadata.

---

## 18. Decisions
- Use `sys.dm_db_partition_stats` filtering on `index_id < 2` (heap 0 or clustered index 1).

---

## 19. Final Validation
- [x] dotnet build (0 errors, 0 warnings)
- [x] dotnet test (272 tests passing)

---

## 20. Final Status

COMPLETED
