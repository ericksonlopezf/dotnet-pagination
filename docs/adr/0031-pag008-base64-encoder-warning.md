# ADR-0031 — PAG008: Warning on Explicit Base64CursorEncoder

## Status
**Accepted** — August 2026

## Context
In ADR-0017, the library established `HmacCursorEncoder` as the secure default for cursor pagination. However, developers migrating older applications or configuring services explicitly may still instantiate or register `Base64CursorEncoder`.

`Base64CursorEncoder` does not cryptographically sign cursor tokens, leaving tokens vulnerable to client tampering, offset enumeration, and unauthorized data exploration. To ensure security hygiene across codebases adopting `EricksonLopez.Pagination`, a compile-time analyzer is needed to flag any explicit creation or registration of `Base64CursorEncoder`.

## Decision
We introduce Roslyn Analyzer `PAG008`:
- **Diagnostic ID**: `PAG008`
- **Title**: `Explicit use of insecure Base64CursorEncoder`
- **Category**: `Security`
- **Severity**: `Warning`
- **Message**: `Explicit use of Base64CursorEncoder is insecure and vulnerable to cursor tampering. Use HmacCursorEncoder with a secret key instead.`
- **Target**: Flags `ObjectCreationOperation` and `TypeOfOperation` / type symbol references targeting `EricksonLopez.Pagination.Base64CursorEncoder`.

## Consequences
- Developers explicitly writing `new Base64CursorEncoder()` or `services.AddSingleton<ICursorEncoder, Base64CursorEncoder>()` will receive compile-time warning `PAG008`.
- If a developer genuinely requires un-signed Base64 cursors (e.g. for internal test harnesses or migration bridges), they can explicitly suppress `PAG008` via `#pragma warning disable PAG008` or `.editorconfig`.
