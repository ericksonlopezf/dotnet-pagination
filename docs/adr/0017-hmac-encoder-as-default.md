# ADR-0017 — HmacCursorEncoder as Secure Default

## Status
**Accepted** — August 2026 · _Implemented in Phase 2 (P2-F001)_

## Context

`PaginationCoreOptions.CursorEncoder` currently defaults to `Base64CursorEncoder`. This means that unless the developer explicitly configures `HmacCursorEncoder`, cursor tokens produced by the library are Base64URL-encoded but **not cryptographically signed**.

A Base64 cursor can be decoded by any client. While the decoded payload is not useful without knowledge of the schema structure, a malicious client can:
1. **Enumerate schema structure** by decoding multiple cursors and comparing payloads.
2. **Construct crafted cursors** by modifying field values and re-encoding in Base64, potentially accessing data outside the intended range.
3. **Bypass TTL controls** — Base64 cursors have no expiration mechanism.

For APIs that expose cursors to external clients (REST APIs, mobile backends, B2B integrations), the current default is insecure.

## Decision

We **will** change the default `CursorEncoder` to `HmacCursorEncoder` with a **development-mode fallback strategy**:

1. If the developer provides an explicit HMAC key in `AddPagination(options => { options.HmacKey = "..." })`, use `HmacCursorEncoder` with that key.
2. If no key is provided, use a **deterministic development key** derived from the assembly name + a fixed salt, and emit `ILogger.LogWarning` on every application startup with message: _"EricksonLopez.Pagination is using a development HMAC key for cursor signing. This key changes between deployments and is NOT suitable for production. Configure `PaginationCoreOptions.HmacKey` in `AddPagination()` before deploying to production."_
3. `Base64CursorEncoder` remains available for specific internal or legacy use cases where signing is explicitly not desired.

## Rationale

### 1. Secure by default (Principle of Least Privilege)

Modern security engineering mandates that the secure option must be the default. The developer should have to explicitly opt **out** of security, not opt **in**. This aligns with the security posture of ASP.NET Core Data Protection, Azure Key Vault SDK, and AWS SDK — all of which use authenticated encryption by default.

### 2. The development key pattern is established and usable

ASP.NET Core Data Protection uses a similar pattern: in development, it generates an ephemeral in-memory key and warns on startup. The developer experience is:
- Development: cursor signing works out-of-the-box with a warning that is easy to understand.
- Production: the startup warning is visible and actionable (configure the key).

This is a UX pattern that .NET developers already know and accept.

### 3. The Base64 default is a liability

Any future security audit of a production application using EricksonLopez.Pagination with the default configuration would flag cursor unsigned tokens as a finding. This creates liability for the library and undermines trust.

### 4. HmacCursorEncoder already exists and is production-grade

`HmacCursorEncoder` is already implemented with:
- HMAC-SHA256 signing
- `CryptographicOperations.FixedTimeEquals` for timing-safe comparison
- TTL support
- Cursor versioning

The change required is only in the default configuration, not in the encoder implementation itself.

## Migration Impact

### Breaking changes
- Projects that use default configuration (no explicit encoder) will now sign cursors. **Existing cursors in clients' bookmarks or caches will become invalid** on upgrade.
- This is documented in the migration guide as a one-time migration cost.

### Mitigation
- `AcceptLegacyCursors = true` option allows the library to accept both signed (v2) and unsigned (v1) cursors during a transition window.
- The CHANGELOG will explicitly document this as a breaking change with upgrade instructions.

## Alternatives Considered

- **Keep Base64 as default, add opt-in flag for HMAC**: Rejected. This preserves the insecure default. The opt-in adoption rate would be low — most developers configure the minimum required to make pagination work.
- **Hard-fail at startup if no HMAC key configured**: Rejected. Too disruptive for local development and testing. The warning + development key pattern is a better UX.
- **Emit a Roslyn analyzer warning (PAG0XX) when Base64 encoder is detected**: Considered as an additional measure. Could be added as PAG008. Does not replace the default change but complements it for existing users.

## Consequences

- All new projects using `AddPagination()` without explicit configuration will produce HMAC-signed cursors out of the box.
- Existing projects upgrading from v0.x to v1.1 will see a startup warning and must configure `HmacKey` for production deployments.
- The security section of the README is updated to document cursor security as a first-class concern, not an opt-in.
- A new Roslyn analyzer (PAG008) warning when `Base64CursorEncoder` is explicitly configured is added to the roadmap.
