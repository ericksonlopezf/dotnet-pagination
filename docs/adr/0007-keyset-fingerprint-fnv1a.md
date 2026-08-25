# ADR-0007: Deterministic Keyset Fingerprint via FNV-1a

## Status
Accepted — August 2026

## Context

`KeysetBuilder<T>` generates a cursor fingerprint embedded in every keyset cursor token:

```
M|v2|<fingerprint>|col1|col2|...
```

The fingerprint's purpose is to reject cursors generated for a _different_ keyset schema (e.g., a cursor from a 2-column keyset being submitted to a 1-column keyset). This prevents silent cross-keyset confusion where the column count happens to match but the columns are semantically different.

### The original defect

The original implementation used:

```csharp
var hash = new HashCode();
hash.Add(col.PropertyType.TypeHandle.Value);
hash.Add(col.IsAscending);
return hash.ToHashCode().ToString("X");
```

`HashCode` in .NET uses a **process-local randomized seed** controlled by `DOTNET_DISABLE_RANDOMIZED_HASH_TABLES`. This seed is:

- Chosen randomly at process startup
- Different per-process, per-restart, per-deployment
- **Not shared between Kubernetes pods, load-balanced instances, or blue-green deployments**

The consequence: any cursor token minted by instance A is guaranteed to have a fingerprint that does _not_ match the fingerprint computed by instance B. Every cursor submitted to a different pod (or the same pod after a restart) would throw `InvalidPaginationCursorException("Cursor was generated for a different keyset")`.

This makes keyset pagination **entirely unusable** in any multi-instance deployment.

### Why not `DOTNET_DISABLE_RANDOMIZED_HASH_TABLES=1`?

This is a process-wide environment variable that disables hash randomization for ALL types, introducing a DoS attack surface across the entire application (e.g., via hash flooding in dictionaries that accept user input). It is not an acceptable workaround.

### Why not MD5 or SHA-256?

Cryptographic hashes are appropriate for security contexts. This fingerprint is a structural integrity check for internal consistency, not a security boundary — the cursor is already integrity-protected by `HmacCursorEncoder` when configured. Adding crypto overhead (allocations, HMAC computation) to the fingerprint path is disproportionate to the requirement.

## Decision

Use **FNV-1a 32-bit** with the **fully-qualified type name** as the hash input:

```csharp
const uint FnvOffset = 2166136261u;
const uint FnvPrime = 16777619u;
uint hash = FnvOffset;
foreach (var col in _columns)
{
    var typeName = col.PropertyType.FullName ?? col.PropertyType.Name;
    foreach (char c in typeName)
    {
        hash ^= (byte)c;
        hash *= FnvPrime;
    }
    hash ^= col.IsAscending ? (byte)0x01 : (byte)0x00;
    hash *= FnvPrime;
}
return hash.ToString("X8");
```

### Why FNV-1a?

| Property | HashCode | FNV-1a |
|---|---|---|
| Deterministic across processes | ❌ | ✅ |
| Deterministic across pods | ❌ | ✅ |
| Allocation-free | ✅ | ✅ |
| No external dependencies | ✅ | ✅ |
| Collision resistance (32-bit) | ✅ | ✅ |
| Suitable for security boundary | ❌ | ❌ |

FNV-1a is:
- **Simple**: 3 lines of arithmetic per column, zero allocations
- **Deterministic**: no hidden seed, no environment variable dependency
- **Stable**: `Type.FullName` is stable across restarts for the same binary
- **Sufficient**: collision resistance for schema fingerprinting (not a security boundary)

### Why `Type.FullName` and not `Type.TypeHandle.Value`?

`TypeHandle.Value` (an `IntPtr`) is the runtime method table pointer. While it is _typically_ stable within a single process, it is:

- **Not stable across binary builds** (ReadyToRun images may assign different addresses)
- **Not stable across processes** (ASLR and NativeAOT can relocate type tables)
- **Explicitly not a public contract** — the runtime docs do not guarantee cross-process stability

`FullName` (e.g., `System.Int32`, `System.DateTimeOffset`) is:
- Part of the public BCL contract
- Stable across builds, processes, and deployments for the same .NET version
- Human-readable for debugging cursors

## Migration Impact

All existing v2 keyset cursors are **invalidated once** when upgrading from the `HashCode`-based fingerprint. This is a one-time migration cost.

**Recommended migration strategy:**

1. Deploy the new version with `AcceptLegacyCursors = false` (or keep `true` if v1 cursors are still in circulation).
2. Inform users that active cursor tokens will be invalidated (they will be directed to page 1 on next navigation — a safe degradation).
3. After all in-flight cursors have expired (typically 1–7 days depending on TTL), no further action is required.

## Consequences

### Positive
- Keyset pagination now works correctly in multi-instance deployments (Kubernetes, load balancers, blue-green).
- Cursor tokens are portable across process restarts.
- No external dependencies or runtime overhead.

### Negative
- One-time cursor invalidation on upgrade.
- `Type.FullName` changes if a namespace is renamed (a breaking change in the model, which is rare and expected to invalidate cursors regardless).

## Alternatives Rejected

| Alternative | Reason rejected |
|---|---|
| `DOTNET_DISABLE_RANDOMIZED_HASH_TABLES=1` | Process-wide DoS exposure |
| MD5/SHA-256 | Over-engineered; allocations; not a security boundary |
| Canonical type index (integer per column position) | Fragile — changing column order silently reorders the index |
| `TypeHandle.Value` with fixed seed | TypeHandle is not stable across processes in ReadyToRun/NativeAOT |
