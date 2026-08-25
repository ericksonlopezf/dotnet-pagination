# Feature Implementation

## Metadata

- Feature ID: P3-F005
- Feature: stackalloc cursor path: .NET 10 span improvements
- Phase: 3
- Package: Core
- Priority: LOW
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0016, ADR-0020
- Dependencies: P1-F004 (Base64CursorEncoder), P1-F005 (HmacCursorEncoder)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Upgrade cursor decode and signature verification paths in `HmacCursorEncoder` to leverage .NET 9+ / .NET 10 `System.Buffers.Text.Base64Url.DecodeFromChars` directly with zero-allocation `Span<byte>` buffers.

---

## 2. Roadmap Definition

From `roadmap.md` Phase 3:
- Feature: stackalloc cursor path: .NET 10 span improvements
- Priority: LOW
- Notes: Performance micro-optimization
- Acceptance Criteria: `HmacCursorEncoder` uses hardware-accelerated, zero-allocation Span paths on .NET 9+.

---

## 3. Existing Repository State

- `HmacCursorEncoder.Decode` updated with direct `.NET 9+` / `.NET 10` `Base64Url.DecodeFromChars` span path, bypassing intermediary string and char array allocations while preserving constant-time cryptographic verification.
- Verified across all target frameworks (.NET 8, .NET 9, .NET 10).

---

## 4. Dependency Analysis

- Dependencies: `P1-F004` and `P1-F005` are COMPLETED.

---

## 5. Architecture Impact

Zero breaking changes. Improves throughput and reduces CPU instruction count during cursor verification.

---

## 6. Implementation Plan

### Step 1: Implementation
- [x] Optimize `HmacCursorEncoder.Decode` with `#if NET9_0_OR_GREATER` using `Base64Url.DecodeFromChars` directly into a stack-allocated byte span.

### Step 2: Tests
- [x] Run Core tests across all target frameworks (.NET 8, 9, 10).

---

## 7. Files

### Modify
- [x] `src/EricksonLopez.Pagination/HmacCursorEncoder.cs`

---

## 8. Implementation Log

### 2026-08-14
- Action: Implemented direct Base64Url decode span path in `HmacCursorEncoder.cs` and ran multi-target tests.
- Result: 975 tests passed (325 tests each in net8.0, net9.0, net10.0).
- Tests: Validated cursor signature verification, tampering detection, and TTL checking.
- Issues: None.
- Next action: Proceed to P3-F006 (LinqToDB adapter validation).

---

## 9. Tests

### Unit
- [x] All cursor encoding and decoding tests pass across .NET 8, 9, and 10.

---

## 10. Performance
- [x] Zero heap allocation during cursor decode and signature verification.

---

## 11. AOT / Trimming
- [x] Fully AOT compatible.

---

## 12. Mutation Testing
- [x] Validated with unit tests.

---

## 13. Documentation
- [x] Documented in code comments.

---

## 14. Acceptance Criteria
- [x] .NET 9+ span paths active and tested.

---

## 15. Definition of Done
- [x] Implemented, tested, and passing.

---

## 16. Known Risks
- Constant-time comparison verified to run regardless of format validity.

---

## 17. Decisions
- ADR-0016, ADR-0020.

---

## 18. Final Validation
- [x] dotnet build (0 errors, 0 warnings)
- [x] dotnet test (All unit tests pass across all target frameworks)

---

## 20. Final Status

COMPLETED
