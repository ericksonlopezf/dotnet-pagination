# Migration Guide

> [!NOTE]
> This project has not yet published a stable release (no `v1.x` or `v2.x` git tags exist). This guide documents the **breaking API changes** introduced during development relative to the initial `0.9.0` codebase baseline. When a `1.0.0` release is tagged, this guide will be updated to reflect the versioned migration path.

## From Pre-Release Baseline (0.9.0) to Current API

The keyset pagination philosophy was significantly overhauled during the development phase to support multi-column, type-safe ordering with cryptographic cursor security.

### 1. Change to `ToCursorPagedListAsync`

**Before (v1 — deprecated):**
```csharp
var page = await db.Products
    .ToCursorPagedListAsync(p => p.Id, parameters);
```

**Now (v2):**
The single-key overload is deprecated and will generate compiler warning `CS0618`. Migrate to the `KeysetBuilder` fluent API:
```csharp
var page = await db.Products
    .Keyset(parameters)
    .Ascending(p => p.Id)
    .ToCursorPagedListAsync();
```

### 2. Multi-Column Keyset (Tiebreaker)

You can chain multiple sort columns. Always add a strictly unique column (e.g., primary key) last to guarantee a deterministic tiebreaker:
```csharp
var page = await db.Products
    .Keyset(parameters)
    .Descending(p => p.CreatedAt)   // Primary sort
    .Ascending(p => p.Id)           // Tiebreaker
    .ToCursorPagedListAsync();
```

### 3. Cursor Exceptions

When a client submits an invalid or expired cursor string, the library now throws typed exceptions:
- `InvalidPaginationCursorException` — cursor format is corrupt or the HMAC signature is invalid.
- `ExpiredPaginationCursorException` — cursor was valid but its TTL has elapsed.

Catch these globally in your exception handling middleware rather than checking for `null`:
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (ex is ExpiredPaginationCursorException)
        {
            context.Response.StatusCode = 410; // Gone
            await context.Response.WriteAsJsonAsync(new { error = "Cursor expired." });
        }
        else if (ex is InvalidPaginationCursorException)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid cursor." });
        }
    });
});
```

### 4. Keyset Fingerprint Change (v2 Cursor Invalidation)

If upgrading from an earlier v2 pre-release build, be aware that the keyset fingerprint algorithm changed from `HashCode` (process-local randomized seed) to FNV-1a 32-bit (deterministic). This means **all existing cursors are invalidated once on upgrade** — clients that attempt to resume from a pre-upgrade cursor will receive `InvalidPaginationCursorException`. This is a one-time migration cost. See [ADR-0007](adr/0007-keyset-fingerprint-fnv1a.md) for the full rationale.
