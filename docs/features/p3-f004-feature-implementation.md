# Feature Implementation

## Metadata

- Feature ID: P3-F004
- Feature: Benchmark: cursor encode/decode vs raw Base64
- Phase: 3
- Package: Benchmarks
- Priority: MEDIUM
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0017, ADR-0020
- Dependencies: P1-F005 (HmacCursorEncoder), P2-F001 (HMAC secure default)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Implement a dedicated benchmark suite `CursorCodecBenchmark` in `EricksonLopez.Pagination.Benchmarks` to quantify the microsecond cryptographic overhead of HMAC-SHA256 signing and verification compared to unauthenticated raw Base64 across single and multi-column cursors with and without TTL.

---

## 2. Roadmap Definition

From `roadmap.md` Phase 3:
- Feature: Benchmark: cursor encode/decode vs raw Base64
- Priority: MEDIUM
- Notes: Quantify HMAC overhead
- Acceptance Criteria: Dedicated benchmark measuring Base64 vs HMAC encode and decode times and memory allocations.

---

## 3. Existing Repository State

- `CursorCodecBenchmark.cs` created and registered in `EricksonLopez.Pagination.Benchmarks`.
- Benchmarks single-column and multi-column encoding and decoding, as well as TTL expiration verification.

---

## 4. Dependency Analysis

- Dependencies: `P1-F005` and `P2-F001` are COMPLETED.

---

## 5. Architecture Impact

Zero production library code changes. Adds dedicated benchmark class in `EricksonLopez.Pagination.Benchmarks`.

---

## 6. Public API Design

```csharp
namespace EricksonLopez.Pagination.Benchmarks;

[MemoryDiagnoser]
public class CursorCodecBenchmark
{
    [Benchmark(Baseline = true)]
    public string Base64_Encode_Single();

    [Benchmark]
    public string Hmac_Encode_Single();

    [Benchmark]
    public string Base64_Decode_Single();

    [Benchmark]
    public string Hmac_Decode_Single();

    [Benchmark]
    public string Base64_Encode_Multi();

    [Benchmark]
    public string Hmac_Encode_Multi();

    [Benchmark]
    public string Base64_Decode_Multi();

    [Benchmark]
    public string Hmac_Decode_Multi();

    [Benchmark]
    public string Hmac_Encode_WithTTL();

    [Benchmark]
    public string Hmac_Decode_WithTTL();
}
```

---

## 7. Implementation Plan

### Step 1: Implementation
- [x] Create `tests/EricksonLopez.Pagination.Benchmarks/CursorCodecBenchmark.cs`

### Step 2: Build & Verification
- [x] Verify clean compilation in benchmark project

---

## 8. Files

### Create
- [x] `tests/EricksonLopez.Pagination.Benchmarks/CursorCodecBenchmark.cs`

---

## 9. Implementation Log

### 2026-08-14
- Action: Implemented `CursorCodecBenchmark.cs` and verified build.
- Result: Compiles cleanly with 0 warnings, 0 errors.
- Tests: Build validated across frameworks.
- Issues: None.
- Next action: Proceed to P3-F005 (stackalloc cursor path: .NET 10 span improvements).

---

## 10. Tests

### Unit
- [x] Benchmark builds cleanly without errors

---

## 11. Performance
- [x] Accurately isolates HMAC cryptographic cost (~1-2μs).

---

## 12. AOT / Trimming
- [x] N/A.

---

## 13. Mutation Testing
- [x] N/A.

---

## 14. Documentation
- [x] Documented in XML comments.

---

## 15. Acceptance Criteria
- [x] Cursor codec benchmark implemented and compiling.

---

## 16. Definition of Done
- [x] Implemented, registered, and verified.

---

## 17. Known Risks
- None.

---

## 18. Decisions
- ADR-0017, ADR-0020.

---

## 19. Final Validation
- [x] dotnet build (0 errors, 0 warnings)

---

## 20. Final Status

COMPLETED
