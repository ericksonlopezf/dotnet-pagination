# ADR-0035 — Cursor Replay Protection and Nonce Store

## Status
**Accepted** — August 2026

## Context
In high-security environments (audit trails, one-time export tokens, zero-trust financial pipelines), pagination cursors should only be consumed once. Without replay protection, an interceptor or client could replay a previously generated cursor multiple times.

## Decision
We define an `ICursorReplayStore` interface in `EricksonLopez.Pagination.Abstractions`, providing a pluggable contract for nonce verification and storage (e.g. Distributed Redis cache, database table, or in-memory cache).

```csharp
namespace EricksonLopez.Pagination.Abstractions;

public interface ICursorReplayStore
{
    Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default);
    bool TryAcquireNonce(string nonce, TimeSpan timeToLive);
}
```

We provide `InMemoryCursorReplayStore` in `EricksonLopez.Pagination` as the standard local implementation, along with `ReplayedPaginationCursorException`.

### Wire Format
When replay protection is enabled, a 128-bit cryptographically secure random nonce is generated and embedded within the HMAC-protected payload:
- `T{expiresAt}:R{nonce}:{rawCursor}` (with TTL)
- `R{nonce}:{rawCursor}` (without TTL)

On decode, the HMAC signature is verified first in constant time. If valid, the nonce is checked against `ICursorReplayStore`. If the nonce was already used, a `ReplayedPaginationCursorException` is thrown.

## Consequences
- Protects against replay attacks in sensitive workflows.
- Clean separation: core provides the abstraction and in-memory store; enterprise applications can implement `ICursorReplayStore` using Redis, SQL, or DynamoDB.
