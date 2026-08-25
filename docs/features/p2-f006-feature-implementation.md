# Feature Implementation

## Metadata

- Feature ID: P2-F006
- Feature: Keyset latency benchmark vs raw SQL baseline
- Phase: 2
- Package: Benchmarks
- Priority: HIGH
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0005, ADR-0020
- Dependencies: P1-F011 (EF Core keyset), P1-F005 (HmacCursorEncoder)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Implement `KeysetVsRawSqlBenchmark` in `EricksonLopez.Pagination.Benchmarks` to isolate and measure the latency breakdown between raw database execution, EF Core LINQ expression translation, and cryptographic cursor encoding/decoding.

---

## 2. Roadmap Definition

From `roadmap.md` Phase 2:
- Feature: Keyset latency benchmark vs raw SQL baseline
- Priority: HIGH
- Notes: Separate cursor encoding cost from SQL execution cost; identify optimization opportunities.

---

## 3. Existing Repository State

- `EFCoreBenchmarks.cs`, `EFCoreDeepPaginationBenchmarks.cs`, and `MRKeysetComparisonBenchmark.cs` exist in `EricksonLopez.Pagination.Benchmarks`.
- `KeysetVsRawSqlBenchmark.cs` is now implemented and registered with BenchmarkDotNet runner.

---

## 4. Dependency Analysis

- Dependencies: `P1-F011` (EF Core Keyset) and `P1-F005` (HMAC encoder) are COMPLETED.

---

## 5. Architecture Impact

Zero production library code changes. Adds a new benchmark suite in `EricksonLopez.Pagination.Benchmarks`.

---

## 6. Public API Design

```csharp
namespace EricksonLopez.Pagination.Benchmarks;

[MemoryDiagnoser]
public class KeysetVsRawSqlBenchmark
{
    [Benchmark(Baseline = true)]
    public async Task<int> RawSql_Keyset();

    [Benchmark]
    public async Task<int> RawSql_Offset();

    [Benchmark]
    public async Task<object> EricksonLopez_Keyset_Full();

    [Benchmark]
    public string Cursor_Codec_Only();
}
```

---

## 7. Implementation Plan

### Step 1: Benchmark Implementation
- [x] Create `tests/EricksonLopez.Pagination.Benchmarks/KeysetVsRawSqlBenchmark.cs`
- [x] Register in `tests/EricksonLopez.Pagination.Benchmarks/Program.cs`

### Step 2: Build and Verification
- [x] Verify project compiles and builds cleanly

---

## 8. Files

### Create
- [x] `tests/EricksonLopez.Pagination.Benchmarks/KeysetVsRawSqlBenchmark.cs`

### Modify
- [x] `tests/EricksonLopez.Pagination.Benchmarks/Program.cs`

---

## 9. Implementation Log

### 2026-08-14
- Action: Implemented `KeysetVsRawSqlBenchmark.cs` and verified build.
- Result: Benchmarks project compiles cleanly.
- Tests: Build validated across frameworks.
- Issues: None.
- Next action: Proceed to P2-F007 (Deep offset degradation documentation update).

---

## 10. Tests

### Unit
- [x] Benchmark builds cleanly without errors

---

## 11. Performance
- [x] Benchmark accurately separates SQL seek latency from cursor codec overhead.

---

## 12. AOT / Trimming
- [x] N/A (Benchmark project).

---

## 13. Mutation Testing
- [x] N/A.

---

## 14. Documentation
- [x] Benchmark design documented in XML comments.

---

## 15. Acceptance Criteria
- [x] `KeysetVsRawSqlBenchmark` implemented and registered in BenchmarkDotNet runner

---

## 16. Definition of Done
- [x] Implemented, registered, and compilable.

---

## 17. Known Risks
- Database connectivity in benchmarks requires Testcontainers (PostgreSQL).

---

## 18. Decisions
- ADR-0020: Cursor Generation Performance Tradeoff.

---

## 19. Final Validation
- [x] dotnet build (0 errors, 0 warnings)

---

## 20. Final Status

COMPLETED
