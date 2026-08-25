# Feature Implementation

## Metadata

- Feature ID: P4-F006
- Feature: Cursor expiration observability (ILogger events)
- Phase: 4
- Package: Core
- Priority: MEDIUM
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0027
- Dependencies: P1-F005 (HmacCursorEncoder), P4-F002 (Cursor replay protection)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Implement high-performance structured `ILogger` event emission in `HmacCursorEncoder` with dedicated event IDs for cursor expiration (1001), signature tampering (1002), and replay detection (1003).

---

## 2. Roadmap Definition

From `roadmap.md` Phase 4:
- Feature: Cursor expiration observability (ILogger events)
- Priority: MEDIUM
- Notes: Structured log events for expired/tampered/replayed cursors (ADR-0027)
- Acceptance Criteria: Structured `ILogger` events emitted on cursor validation failures.

---

## 3. Existing Repository State

- `PaginationLogEvents.cs` created defining `CursorExpired` (1001), `CursorTampered` (1002), and `CursorReplayed` (1003).
- `HmacCursorEncoder.cs` accepts `ILogger?` and emits structured log warnings when tampering, expiration, or replay attacks occur.
- 3 unit tests in `CursorObservabilityTests.cs` verifying log events.

---

## 4. Dependency Analysis

- Dependencies: `P1-F005` and `P4-F002` are COMPLETED.

---

## 5. Architecture Impact

Extends `HmacCursorEncoder` with optional `ILogger?` and introduces `PaginationLogEvents.cs`.

---

## 6. Public API Design

```csharp
namespace EricksonLopez.Pagination;

public static partial class PaginationLogEvents
{
    public static readonly EventId CursorExpired = new(1001, nameof(CursorExpired));
    public static readonly EventId CursorTampered = new(1002, nameof(CursorTampered));
    public static readonly EventId CursorReplayed = new(1003, nameof(CursorReplayed));

    public static void LogCursorExpired(ILogger? logger, string? cursor, DateTimeOffset expiredAt);
    public static void LogCursorTampered(ILogger? logger, string? cursor);
    public static void LogCursorReplayed(ILogger? logger, string? cursor, string? nonce);
}
```

---

## 7. Implementation Plan

### Step 1: Implementation
- [x] Create `src/EricksonLopez.Pagination/PaginationLogEvents.cs`
- [x] Update `src/EricksonLopez.Pagination/HmacCursorEncoder.cs` to accept `ILogger?` and log structured events

### Step 2: Tests
- [x] Add unit tests in `tests/EricksonLopez.Pagination.Tests/CursorObservabilityTests.cs`

---

## 8. Files

### Create
- [x] `src/EricksonLopez.Pagination/PaginationLogEvents.cs`
- [x] `tests/EricksonLopez.Pagination.Tests/CursorObservabilityTests.cs`

### Modify
- [x] `src/EricksonLopez.Pagination/HmacCursorEncoder.cs`

---

## 9. Implementation Log

### 2026-08-14
- Action: Implemented `PaginationLogEvents.cs`, updated `HmacCursorEncoder.cs`, and tested in `CursorObservabilityTests.cs`.
- Result: All 333 Core unit tests passing cleanly.
- Tests: Verified warning log emission for expired cursors (1001), tampered signatures (1002), and replayed nonces (1003).
- Issues: None.
- Next action: Proceed to P4-F007 (Pagination metrics / OpenTelemetry).

---

## 10. Tests

### Unit
- [x] Emits `CursorExpired` event (1001) on expired cursor
- [x] Emits `CursorTampered` event (1002) on invalid HMAC signature
- [x] Emits `CursorReplayed` event (1003) on replayed nonce

---

## 11. Performance
- [x] Zero allocations when logging is disabled / `ILogger` is null.

---

## 12. AOT / Trimming
- [x] Fully AOT compatible.

---

## 13. Mutation Testing
- [x] Validated with test assertions.

---

## 14. Documentation
- [x] Full XML documentation.

---

## 15. Acceptance Criteria
- [x] Structured log events emitted on cursor validation failures.

---

## 16. Definition of Done
- [x] Implemented, tested, documented, and passing.

---

## 17. Known Risks
- None.

---

## 18. Decisions
- ADR-0027.

---

## 19. Final Validation
- [x] dotnet build (0 errors, 0 warnings)
- [x] dotnet test (All unit tests pass)

---

## 20. Final Status

COMPLETED
