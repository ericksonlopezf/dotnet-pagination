# Migration Guide

> [!NOTE]
> This migration guide documents breaking changes and architectural transitions across releases, including the **2.0.0** release (2026-09-21) and historical transitions leading to 1.0.0.

## Migrating from 1.0.0 to 2.0.0 (Release: 2026-09-21)

Version **2.0.0** introduces 8 breaking changes across API, ABI, behavior, and assembly identity:

### 1. Recompilation Required for `HmacCursorEncoder` (BC-001)
The public constructor for `HmacCursorEncoder` added an optional parameter `Func<string?>? tenantContextProvider = null`. Downstream assemblies compiled against `1.0.0` must be recompiled against `2.0.0` to avoid `System.MissingMethodException`.

### 2. Invalidation of v1.0.0 Keyset Cursors (BC-002)
The keyset fingerprint algorithm now cryptographically binds entity type names, property names, and tenant identifiers into the FNV-1a hash. All v2 cursors issued by `1.0.0` will fail validation on `2.0.0` servers with `InvalidPaginationCursorException`. Schedule deployment during low-traffic windows.

### 3. Maximum Cursor Length Limit (BC-003)
`HmacCursorEncoder.Decode` now enforces a hard limit of `8192` characters. Cursors longer than 8192 characters immediately throw `InvalidPaginationCursorException`.

### 4. Tenant Context Isolation (BC-004)
When `PaginationCursorOptions.TenantContextProvider` is active, HMAC cursors embed tenant identity (`CTX:<tenant>:`). Cursors missing tenant context or originating from a different tenant are rejected.

### 5. Typed `PaginationErrorResponse` in Endpoint Filter (BC-005)
`PaginationEndpointFilter` now returns typed `Results.BadRequest(new PaginationErrorResponse(...))` instead of an anonymous object. Update in-memory unit tests expecting dynamic camelCase `error`.

### 6. Dapper Parameter Names (BC-006)
`DynamicParameters` keys in `DbConnectionPaginationExtensions` changed from `@__Pagination_Limit__` / `@__Pagination_Skip__` to `__Pagination_Limit__` / `__Pagination_Skip__` (omitting `@`).

### 7. OpenTelemetry Metric Units (BC-007)
Meter instruments now declare explicit units (`{queries}`, `{pages}`, `{errors}`). Verify Prometheus alert queries.

### 8. Production Strong-Naming Key (BC-008)
Assemblies are now signed with the official key `EricksonLopez.snk`. Update any explicit `[InternalsVisibleTo]` or assembly identity checks.

## Historical: From Pre-Release Baseline (0.9.0) to 1.0.0

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
