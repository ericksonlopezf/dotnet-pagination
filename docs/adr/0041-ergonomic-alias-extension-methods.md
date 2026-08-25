# ADR-0041: Ergonomic Alias Extension Methods for Documentation Parity

## Status

Accepted

## Date

2026-08-24

## Context

During the comprehensive technical documentation audit, two ergonomic discrepancies were identified between documentation code samples and internal method names:

1. **Count-less Offset Pagination**: The `README.md`, `ROADMAP.md`, and `features.md` documented the method `.ToPagedListWithoutCountAsync()`, representing single-roundtrip $N+1$ lookahead probing. In code, this behavior was originally accessed solely via the boolean parameter `ToPagedListAsync(..., countTotal: false)`.
2. **Keyset Partitioning**: The `README.md` and `docs/cookbook.md` documented `.PartitionByKeysetAsync()` for multi-worker parallel partitioning. In code, the method was named `SplitKeysetPartitionsAsync()`.

While `countTotal: false` and `SplitKeysetPartitionsAsync()` accurately expressed the low-level API, requiring developers to adjust their mental model away from self-documenting method names created unnecessary friction and compilation errors when copying examples.

## Decision

We have decided to provide first-class, officially supported ergonomic alias extension methods:

1. **`ToPagedListWithoutCountAsync`**:
   - Implemented in `EricksonLopez.Pagination.EntityFrameworkCore` and `EricksonLopez.Pagination.LinqToDB`.
   - Directly delegates to `ToPagedListAsync(..., countTotal: false, ...)`.
   - Emits full XML documentation explaining the $N+1$ lookahead probe mechanism.

2. **`PartitionByKeysetAsync`**:
   - Implemented in `EricksonLopez.Pagination.EntityFrameworkCore.KeysetPartitioningExtensions`.
   - Directly delegates to `SplitKeysetPartitionsAsync(...)`.
   - Emits full XML documentation linking to the partitioning specification.

The original methods (`ToPagedListAsync` with `countTotal: false` and `SplitKeysetPartitionsAsync`) remain intact with zero breaking changes.

## Consequences

### Positive
- **100% Documentation Compilability**: All code snippets in `README.md`, `ROADMAP.md`, `features.md`, and `cookbook.md` compile and run seamlessly.
- **Superior Developer Ergonomics**: Explicit intent in code (`ToPagedListWithoutCountAsync`) without needing to remember boolean flag defaults.
- **Zero Breaking Changes**: Existing code calling `SplitKeysetPartitionsAsync` or `ToPagedListAsync(countTotal: false)` continues to function identically.

### Negative
- Slightly increased public API surface area in `EricksonLopez.Pagination.EntityFrameworkCore` and `EricksonLopez.Pagination.LinqToDB`.
