# Architectural Decision Record: REJECT-009
## Rejection of Untyped Opaque String Cursors in Core Pagination

### Status
**REJECTED (Permanent Directorial Invariant)**

### Context
Suggestions evaluated using opaque Base64-encoded strings as the universal cursor parameter across all domain contracts.

### Decision
Permanently rejected. `EricksonLopez.Pagination.Abstractions` enforces strongly-typed keyset cursors (`Cursor<TKey>`) with typed serialization tokens. Opaque token encoding is an optional presentation concern, not a domain contract constraint.

### Consequences
- Type safety and compile-time index matching on keyset queries.
- Zero unnecessary string allocations on database query execution.
