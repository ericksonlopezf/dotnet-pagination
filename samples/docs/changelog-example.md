# CHANGELOG Example & Release Specifications

> Sample release notes demonstrating semantic versioning (SemVer 2.0.0) and conventional commits for `EricksonLopez.Pagination`.

---

## [2.0.0] — 2026-09-21

### 🚀 Major Features & Architectural Enhancements
- **Multi-Column KeysetBuilder (`KeysetBuilder<T>`)**: Replaced legacy single-key cursor queries with a fluent, type-safe builder supporting arbitrary sort columns and deterministic tie-breakers.
- **Cryptographic Cursor Security (`HmacCursorEncoder`)**: Built-in HMAC-SHA256 signature sealing with constant-time verification, configurable TTL, and distributed replay protection.
- **Dynamic Filter DSL Engine (`ApplyFilter`)**: Compile-time cached expression tree generator supporting `=`, `!=`, `>=`, `<=`, `>`, `<`, `~=`, `^=`, `$=`.
- **OpenTelemetry Diagnostics (`PaginationMetrics`)**: Native metrics meters for offset queries, keyset queries, query duration, and cursor errors.
- **HTTP 304 ETag Caching (`ToPagedResult`)**: Deterministic SHA-256 ETag generation for instant conditional responses.

### 🛡️ Roslyn Analyzers
- Added `PAG001`: Mandatory deterministic `OrderBy` detection.
- Added `PAG002`: Deprecation warning for unencrypted Base64 cursors.
- Added `PAG003`: Compile-time validation of keyset tie-breaker uniqueness.
- Added `PAG004`: Analyzer warning when `OrderBy` precedes `Keyset`.

### 🔄 Breaking Changes & Deprecations
- Deprecated `ToCursorPagedListAsync(keySelector, parameters)` in favor of `.Keyset(parameters).Ascending(...).ToCursorPagedListAsync()`.
- Cursor validation failures now throw typed `InvalidPaginationCursorException` and `ExpiredPaginationCursorException` instead of returning null.

### 📦 Ecosystem Integrations
- Added `EricksonLopez.Pagination.Dapper`: Native `DapperKeysetBuilder<T>` and `GridReader` pagination.
- Added `EricksonLopez.Pagination.Redis`: Distributed `RedisCursorReplayStore`.
- Added `EricksonLopez.Pagination.Relay`: GraphQL Relay specification compliant `Connection<TNode>`, `Edge<TNode>`, `PageInfo`.
- Added `EricksonLopez.Pagination.Result`: Railway-oriented programming extensions with `EricksonLopez.Result`.
