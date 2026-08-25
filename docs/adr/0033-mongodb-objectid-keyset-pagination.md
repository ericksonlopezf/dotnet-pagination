# ADR-0033 — MongoDB ObjectId-Based Keyset Pagination

## Status
**Accepted** — August 2026 · _Implemented in Phase 2 (P2-F005)_

## Context

MongoDB's primary key is `ObjectId` — a 12-byte BSON type encoding a timestamp, machine ID, and sequence counter. `ObjectId` is:
- **Monotonically increasing** by default (time-ordered), making it ideal as a keyset cursor column.
- **Not a numeric integer**, so standard `WHERE id > @cursor` SQL keyset predicates cannot be applied directly.
- Comparable as a string via `.ToString("N")` or as bytes, but comparisons must be stable across BSON representation boundaries.

The existing `MongoCursorPaginationExtensions` supports string and date-based cursor keys but lacks first-class `ObjectId` support.

## Decision

We introduce `MongoObjectIdPaginationExtensions` in `EricksonLopez.Pagination.MongoDB`.

### Cursor Encoding Strategy
- `ObjectId` is serialized to its 24-character hex string (`ObjectId.ToString()`) for embedding in cursor payloads.
- Deserialization uses `ObjectId.Parse(hexString)` — stable and unambiguous across BSON driver versions.
- Encoded cursor payload format: `S|{objectIdHex}` (single-key prefix, no fingerprint required for single-key cursors).

### Query Strategy
- Forward pagination: `_id > ObjectId(cursorHex)` via MongoDB `FilterDefinitionBuilder.Gt`.
- Backward pagination: `_id < ObjectId(cursorHex)` via `FilterDefinitionBuilder.Lt`.
- Results are always sorted by `_id` ascending (monotonic ObjectId order).

## Alternatives Considered

- **Reuse existing string-based cursor logic**: Rejected. String comparison of ObjectId hex strings is not semantically equivalent to ObjectId ordering.
- **Convert ObjectId to DateTime for time-based keyset**: Rejected. Two ObjectIds generated in the same millisecond differ only by machine ID and counter — DateTime-based keyset produces ambiguous boundaries.

## Consequences

- `ObjectId`-keyed MongoDB collections can paginate in O(log N) using the native `_id` B-Tree index.
- The `IObjectIdDocument` interface contract is intentionally minimal (one property `Id: ObjectId`).
- Integration tests use Testcontainers to validate ObjectId ordering and boundary conditions.
