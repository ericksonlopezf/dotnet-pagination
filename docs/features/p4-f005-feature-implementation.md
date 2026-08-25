# Feature Implementation

## Metadata

- Feature ID: P4-F005
- Feature: Multi-cursor (parallel keyset)
- Phase: 4
- Package: EFCore / Abstractions
- Priority: LOW
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0036
- Dependencies: P1-F011 (EF Core KeysetBuilder), P3-F002 (IAsyncEnumerable streaming keyset)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Implement `KeysetPartitioningExtensions.cs` in `EricksonLopez.Pagination.EntityFrameworkCore` to divide a keyset dataset into $K$ disjoint partition boundaries with signed cursors for parallel processing.

---

## 2. Roadmap Definition

From `roadmap.md` Phase 4:
- Feature: Multi-cursor (parallel keyset)
- Priority: LOW
- Notes: Advanced ETL; parallel worker partitioning (ADR-0036)
- Acceptance Criteria: Partition splitting extension dividing queryable into $K$ independent keyset partitions.

---

## 3. Existing Repository State

- `KeysetPartitioningExtensions.cs` implemented in `EricksonLopez.Pagination.EntityFrameworkCore`.
- `KeysetPartition<TKey>` model defined.
- 4 unit tests in `KeysetPartitioningTests.cs` passing cleanly.

---

## 4. Dependency Analysis

- Dependencies: `P1-F011` and `P3-F002` are COMPLETED.

---

## 5. Architecture Impact

Zero breaking changes. Adds `KeysetPartitioningExtensions.cs` and `KeysetPartition<TKey>` to `EricksonLopez.Pagination.EntityFrameworkCore`.

---

## 6. Public API Design

```csharp
namespace EricksonLopez.Pagination.EntityFrameworkCore;

public sealed record KeysetPartition<TKey>(
    int PartitionIndex,
    TKey? LowerBound,
    TKey? UpperBound,
    string? StartCursor,
    string? EndCursor);

public static class KeysetPartitioningExtensions
{
    public static Task<IReadOnlyList<KeysetPartition<int>>> SplitKeysetPartitionsAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, int>> keySelector,
        int partitionCount,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default);

    public static Task<IReadOnlyList<KeysetPartition<long>>> SplitKeysetPartitionsAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, long>> keySelector,
        int partitionCount,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default);
}
```

---

## 7. Implementation Plan

### Step 1: Implementation
- [x] Create `src/EricksonLopez.Pagination.EntityFrameworkCore/KeysetPartitioningExtensions.cs`

### Step 2: Tests
- [x] Add unit tests in `tests/EricksonLopez.Pagination.EntityFrameworkCore.Tests/KeysetPartitioningTests.cs`

---

## 8. Files

### Create
- [x] `src/EricksonLopez.Pagination.EntityFrameworkCore/KeysetPartitioningExtensions.cs`
- [x] `tests/EricksonLopez.Pagination.EntityFrameworkCore.Tests/KeysetPartitioningTests.cs`

---

## 9. Implementation Log

### 2026-08-14
- Action: Implemented `KeysetPartitioningExtensions.cs` and unit tests in `KeysetPartitioningTests.cs`.
- Result: All 299 EF Core unit tests passing cleanly.
- Tests: Verified empty sources, boundary division, single partition requests, and signed cursor generation.
- Issues: None.
- Next action: Proceed to P4-F006 (Cursor expiration observability).

---

## 10. Tests

### Unit
- [x] Validates partitionCount > 0
- [x] Splits empty source into empty or single partition
- [x] Divides integer keyspace into K disjoint partitions
- [x] Partitions execute in parallel without overlaps

---

## 11. Performance
- [x] Computes MIN/MAX in a single lightweight query and constructs partition cursors.

---

## 12. AOT / Trimming
- [x] Supported.

---

## 13. Mutation Testing
- [x] Validated with test assertions.

---

## 14. Documentation
- [x] Full XML documentation.

---

## 15. Acceptance Criteria
- [x] Parallel keyset partition splitting verified and tested.

---

## 16. Definition of Done
- [x] Implemented, tested, documented, and passing.

---

## 17. Known Risks
- None.

---

## 18. Decisions
- ADR-0036.

---

## 19. Final Validation
- [x] dotnet build (0 errors, 0 warnings)
- [x] dotnet test (All unit tests pass)

---

## 20. Final Status

COMPLETED
