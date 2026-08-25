# Feature Implementation

## Metadata

- Feature ID: P2-F002
- Feature: PAG008 analyzer: warn on explicit Base64CursorEncoder
- Phase: 2
- Package: Analyzers
- Priority: MEDIUM
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0031 (ROADMAP ADR-0022)
- Dependencies: P1-F022 (Roslyn Analyzers), P2-F001 (HMAC default)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Implement the `PAG008` Roslyn diagnostic analyzer to emit a compile-time warning whenever `Base64CursorEncoder` is explicitly instantiated, referenced in DI registrations, or configured in pagination options, enforcing security best practices and nudging consumers to `HmacCursorEncoder`.

---

## 2. Roadmap Definition

From `roadmap.md` Phase 2:
- Feature: PAG008 analyzer: warn on explicit Base64CursorEncoder
- Priority: MEDIUM
- Notes: Complement to ADR-0017
- Acceptance Criteria: PAG008 analyzer implemented and documented with comprehensive unit tests

---

## 3. Existing Repository State

- Roslyn analyzers PAG002 through PAG007 exist in `EricksonLopez.Pagination.Analyzers`.
- `ExplicitBase64CursorEncoderAnalyzer` (PAG008) is now implemented and tested.
- `AnalyzerTestVerifier` is configured in `EricksonLopez.Pagination.Analyzers.Tests`.

---

## 4. Dependency Analysis

- Dependencies: `P1-F022` (Analyzers infrastructure) and `P2-F001` (HMAC default) are both COMPLETED.

---

## 5. Architecture Impact

Zero runtime impact. Adds Roslyn compile-time analyzer diagnostic `PAG008` with category `Security` and severity `Warning`.

---

## 6. Public API Design

```csharp
namespace EricksonLopez.Pagination.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExplicitBase64CursorEncoderAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PAG008";
}
```

---

## 7. Implementation Plan

### Step 1: Design and ADR
- [x] Create ADR-0031 for PAG008 analyzer rule
- [x] Implement `ExplicitBase64CursorEncoderAnalyzer` in `EricksonLopez.Pagination.Analyzers`

### Step 2: Testing
- [x] Implement unit tests in `EricksonLopez.Pagination.Analyzers.Tests` for `new Base64CursorEncoder()`
- [x] Implement tests for DI registrations `AddSingleton<ICursorEncoder, Base64CursorEncoder>()`
- [x] Implement tests for `typeof(Base64CursorEncoder)`
- [x] Implement tests for negative cases (`HmacCursorEncoder`, other types)

### Step 3: Documentation and Verification
- [x] Update `src/EricksonLopez.Pagination.Analyzers/README.md`
- [x] Run test suite

---

## 8. Files

### Create
- [x] `src/EricksonLopez.Pagination.Analyzers/ExplicitBase64CursorEncoderAnalyzer.cs`
- [x] `tests/EricksonLopez.Pagination.Analyzers.Tests/PAG008Tests.cs`
- [x] `docs/adr/0031-pag008-base64-encoder-warning.md`

### Modify
- [x] `src/EricksonLopez.Pagination.Analyzers/README.md`

---

## 9. Implementation Log

### 2026-08-14
- Action: Implemented `ExplicitBase64CursorEncoderAnalyzer`, `PAG008Tests`, and updated `README.md`.
- Result: 36 analyzer unit tests passing cleanly.
- Tests: `PAG008Tests` (5 test scenarios) verified.
- Issues: None.
- Next action: Proceed to P2-F003 (SQL Server approximate count).

---

## 10. Tests

### Unit
- [x] Object creation `new Base64CursorEncoder()` reports PAG008
- [x] Type argument `AddSingleton<ICursorEncoder, Base64CursorEncoder>()` reports PAG008
- [x] `typeof(Base64CursorEncoder)` reports PAG008
- [x] `new HmacCursorEncoder(...)` does not report PAG008
- [x] Unrelated classes in other namespaces do not report PAG008

---

## 11. Performance
- [x] Concurrent execution and generated code exclusion configured.

---

## 12. AOT / Trimming
- [x] Roslyn analyzers run at compile-time only (netstandard2.0).

---

## 13. Mutation Testing
- [x] Stryker executed on analyzer assembly.

---

## 14. Documentation
- [x] `README.md` in `EricksonLopez.Pagination.Analyzers` updated.

---

## 15. Acceptance Criteria
- [x] PAG008 analyzer implemented and documented
- [x] All unit tests passing

---

## 16. Definition of Done
- [x] Feature fully functional, tested, documented, and verified.

---

## 17. Known Risks
- None. Standard Roslyn analyzer.

---

## 18. Decisions
- ADR-0031: Warn whenever `Base64CursorEncoder` is explicitly referenced.

---

## 19. Final Validation
- [x] dotnet build (0 errors, 0 warnings)
- [x] dotnet test (All 36 analyzer tests pass)

---

## 20. Final Status

COMPLETED
