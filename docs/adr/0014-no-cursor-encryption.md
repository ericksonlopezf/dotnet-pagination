# ADR-0014 — No Cursor Encryption (AES-256)

## Status
**Accepted** — August 2026

## Context

A question arose whether to add AES-256 encryption to cursor tokens, beyond the existing HMAC-SHA256 signing provided by `HmacCursorEncoder`.

The argument for encryption: encrypting the cursor payload would hide the internal structure (keyset columns and values) from clients who may attempt to reverse-engineer the schema from Base64-decoded cursor contents.

## Decision

We **will not** implement AES-256 cursor encryption.

## Rationale

### 1. HMAC already solves the real problem

The security threat from cursor manipulation is **tampering**: a malicious client modifying cursor values to access data they shouldn't, bypass TTL constraints, or enumerate the schema via trial-and-error.

`HmacCursorEncoder` addresses this with:
- **HMAC-SHA256 signing**: Any modification to the cursor payload invalidates the signature.
- **`CryptographicOperations.FixedTimeEquals`**: Timing-safe comparison preventing timing oracle attacks.
- **TTL expiration**: Expired cursors are rejected regardless of validity.

Encryption would add schema obfuscation on top of an already tamper-proof payload. This addresses a confidentiality concern, not an integrity concern.

### 2. Cursors are already opaque

Cursors produced by `HmacCursorEncoder` are Base64URL-encoded payloads. While Base64 is not encryption, the cursor's internal structure (which columns, which values) is not documented as a public contract. A sophisticated attacker can decode the Base64 and see the field names, but cannot use this knowledge to produce a valid cursor without the HMAC key.

For the vast majority of use cases, this level of opacity is sufficient.

### 3. AES introduces new attack surfaces

Symmetric encryption requires:
- **Key management**: How is the AES key stored, rotated, and distributed across pods?
- **IV handling**: Each encryption must use a fresh, unpredictable IV. Reusing IVs with AES-CBC breaks confidentiality.
- **Mode selection**: AES-GCM provides authenticated encryption but is more complex; AES-CBC without MAC is vulnerable to padding oracle attacks.

Getting this wrong introduces vulnerabilities more severe than the schema-leakage problem being solved.

### 4. Performance overhead

AES-256 encryption adds ~1-3μs per cursor encode/decode operation in .NET. While small in absolute terms, this is significant relative to the current near-zero-allocation cursor path (`stackalloc`, no heap allocation). For high-throughput APIs with many paginated requests, this overhead accumulates.

### 5. If schema confidentiality is required

Developers who require full schema confidentiality from cursors should:
1. Use `HmacCursorEncoder` with a strong key (current recommendation).
2. Design cursors that use opaque identifiers (e.g., `RowId` surrogate keys) rather than business data (e.g., `CustomerName`, `CreatedAt`).

This is documented in `docs/api-reference.md` under cursor design guidance.

## Alternatives Considered

- **AES-GCM authenticated encryption**: Provides confidentiality + integrity. Rejected due to key management complexity and performance overhead vs. marginal gain over HMAC.
- **Envelope encryption** (per-cursor key wrapped with a master key): Rejected as disproportionately complex for a pagination library.

## Consequences

`HmacCursorEncoder` remains the recommended and most secure cursor implementation. Cursor schema confidentiality is addressed via documentation guidance, not cryptographic concealment.
