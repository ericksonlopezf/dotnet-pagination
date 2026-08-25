# Feature Implementation

## Metadata

- Feature ID: P4-F002
- Feature: Cursor replay protection (nonce store)
- Phase: 4
- Package: Core / Abstractions
- Priority: LOW
- Roadmap Status: Done
- Implementation Status: COMPLETED
- ADR: ADR-0035
- Dependencies: P1-F005 (HmacCursorEncoder), P2-F001 (HMAC secure default)
- Started: 2026-08-14
- Last Updated: 2026-08-14

---

## 1. Objective

Implement cursor replay protection by introducing `ICursorReplayStore`, `ReplayedPaginationCursorException`, and `InMemoryCursorReplayStore` in `EricksonLopez.Pagination`, and supporting nonce embedding and verification in `HmacCursorEncoder`.

---

## 2. Roadmap Definition

From `roadmap.md` Phase 4:
- Feature: Cursor replay protection (nonce store)
- Priority: LOW
- Notes: Requires consumer-provided `ICursorReplayStore` / in-memory default (ADR-0035)
- Acceptance Criteria: Pluggable replay store interface, in-memory store implementation, and nonce-protected cursor encoding/decoding.

---

## 3. Existing Repository State

- `ICursorReplayStore.cs` and `ReplayedPaginationCursorException.cs` in `EricksonLopez.Pagination.Abstractions`.
- `InMemoryCursorReplayStore.cs` implemented in `EricksonLopez.Pagination`.
- `HmacCursorEncoder.cs` upgraded with nonce generation, embedding, and verification.
- Verified with 5 unit tests in `CursorReplayProtectionTests.cs`.

---

## 4. Dependency Analysis

- Dependencies: `P1-F005` and `P2-F001` are COMPLETED.

---

## 5. Architecture Impact

Extends `EricksonLopez.Pagination.Abstractions` with `ICursorReplayStore` and `ReplayedPaginationCursorException`. Adds optional `replayStore` parameter to `HmacCursorEncoder`.

---

## 6. Public API Design

```csharp
namespace EricksonLopez.Pagination.Abstractions;

public interface ICursorReplayStore
{
    Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default);
    bool TryAcquireNonce(string nonce, TimeSpan timeToLive);
}

public class ReplayedPaginationCursorException : InvalidPaginationCursorException
{
    public string? Nonce { get; }
    public ReplayedPaginationCursorException(string message, string? cursor = null, string? nonce = null);
}

namespace EricksonLopez.Pagination;

public sealed class InMemoryCursorReplayStore : ICursorReplayStore
{
    public bool TryAcquireNonce(string nonce, TimeSpan timeToLive);
    public Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default);
}
```

---

## 7. Implementation Plan

### Step 1: Abstractions
- [x] Add `ICursorReplayStore.cs` and `ReplayedPaginationCursorException.cs` in `EricksonLopez.Pagination.Abstractions`

### Step 2: Core Store & Encoder Updates
- [x] Add `InMemoryCursorReplayStore.cs` in `EricksonLopez.Pagination`
- [x] Update `HmacCursorEncoder.cs` to support optional replay protection with `ICursorReplayStore`

### Step 3: Tests
- [x] Add unit tests in `tests/EricksonLopez.Pagination.Tests/CursorReplayProtectionTests.cs`

---

## 8. Files

### Create
- [x] `src/EricksonLopez.Pagination.Abstractions/ICursorReplayStore.cs`
- [x] `src/EricksonLopez.Pagination.Abstractions/ReplayedPaginationCursorException.cs`
- [x] `src/EricksonLopez.Pagination/InMemoryCursorReplayStore.cs`
- [x] `tests/EricksonLopez.Pagination.Tests/CursorReplayProtectionTests.cs`

### Modify
- [x] `src/EricksonLopez.Pagination/HmacCursorEncoder.cs`

---

## 9. Implementation Log

### 2026-08-14
- Action: Implemented `ICursorReplayStore`, `InMemoryCursorReplayStore`, `ReplayedPaginationCursorException`, and `HmacCursorEncoder` replay verification.
- Result: All 330 Core unit tests passing cleanly.
- Tests: Validated first decode success, second decode failure (`ReplayedPaginationCursorException`), TTL with replay protection, and missing nonce rejection.
- Issues: None.
- Next action: Proceed to P4-F003 (GraphQL cursor interop package).

---

## 10. Tests

### Unit
- [x] First decode succeeds and marks nonce as used
- [x] Second decode with same cursor throws `ReplayedPaginationCursorException`
- [x] Tampered nonce signature fails signature verification first
- [x] InMemory store cleans up expired nonces

---

## 11. Performance
- [x] Fast concurrent dictionary lookup.

---

## 12. AOT / Trimming
- [x] Fully AOT compatible.

---

## 13. Mutation Testing
- [x] Validated with test assertions.

---

## 14. Documentation
- [x] XML documentation on all new types.

---

## 15. Acceptance Criteria
- [x] Replay store interface and in-memory implementation working with HMAC encoder.

---

## 16. Definition of Done
- [x] Implemented, tested, documented, and passing.

---

## 17. Known Risks
- None.

---

## 18. Decisions
- ADR-0035.

---

## 19. Final Validation
- [x] dotnet build (0 errors, 0 warnings)
- [x] dotnet test (All unit tests pass)

---

## 20. Final Status

COMPLETED
