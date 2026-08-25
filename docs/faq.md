# Frequently Asked Questions

## 1. What is the difference between Offset and Cursor (Keyset) Pagination?

- **Offset** uses `Skip(n)` and `Take(m)`. As you navigate deeper into the data, the database must sequentially skip all preceding rows, making the query progressively slower — O(N) in the worst case.
- **Cursor (Keyset)** remembers a unique identifier from the last item seen and requests the next N records greater than that value (`WHERE Id > @cursor`). This uses B-Tree indexes efficiently, enabling O(log N) performance — **provided the keyset columns are indexed**. Without an index, the database falls back to a full scan.

## 2. Do I need to depend on ASP.NET Core in my Domain layer?

No. Use `EricksonLopez.Pagination` (Core) or `EricksonLopez.Pagination.Abstractions`. Neither has a dependency on `Microsoft.AspNetCore`. ASP.NET Core integration lives exclusively in `EricksonLopez.Pagination.AspNetCore`.

## 3. I get a compile warning that `ToCursorPagedListAsync` with a single key selector is obsolete. How do I fix it?

This single-key overload (`ToCursorPagedListAsync(keySelector: x => x.Id, ...)`) was part of the v1 API. Migrate to the new type-safe fluent API:
```csharp
await query
    .Keyset(cursorParameters)
    .Ascending(x => x.Id)
    .ToCursorPagedListAsync(cancellationToken: ct);
```
See the [Migration Guide](migration-guide.md) for full details.

## 4. Does the library support Dapper?

Yes. Reference `EricksonLopez.Pagination.Dapper` and use the `ToPagedListAsync` extension method on `IDbConnection`:
```csharp
var page = await connection.ToPagedListAsync<Product>(
    sql:        "SELECT * FROM Products ORDER BY Name",
    countSql:   "SELECT COUNT(*) FROM Products",
    parameters: pagination);
```

## 5. Are the generated cursors secure?

By default, cursors use `Base64CursorEncoder` — they are Base64-encoded (obfuscated, not cryptographically signed). Any client that understands Base64 can read and manipulate the cursor values.

For production APIs where cursors contain sensitive data or authorization bounds, configure `HmacCursorEncoder`:
```csharp
options.Cursor.Encoder = new HmacCursorEncoder(
    secretKey: configuration["Pagination:CursorKey"]!,
    timeToLive: TimeSpan.FromMinutes(30));
```
See [SECURITY.md](../SECURITY.md) for the full security boundary documentation.

## 6. What are the target frameworks for the Cosmos package?

`EricksonLopez.Pagination.Cosmos` targets `net8.0;net9.0;net10.0`, matching all other integration packages. The package sets `IsAotCompatible=false` because the Azure Cosmos SDK has AOT limitations. Add `AzureCosmosDisableNewtonsoftJsonCheck=true` to suppress the SDK's Newtonsoft.Json compatibility check.

## 7. Is `dotnet-stryker` in the local tool manifest?

Yes. `dotnet-tools.json` registers `dotnet-stryker` at v4.16.0. After cloning, run `dotnet tool restore` to install it locally, then run mutation tests with:
```bash
# Restore local tools (includes dotnet-stryker v4.16.0)
dotnet tool restore

# Run from repository root
dotnet stryker
```
See the [Quality Gates](quality-gates.md) document for threshold configuration.

## 8. What happens if I call `ToPagedListAsync` without an `OrderBy`?

The Roslyn analyzer `PAG001` (from `EricksonLopez.Pagination.Analyzers`) will raise an **error** at compile time. Without an `OrderBy`, SQL results are non-deterministic and pagination will produce inconsistent pages. Always add at least one `.OrderBy(...)` before calling `ToPagedListAsync`.

## 9. What is the difference between `ToPagedListAsync` and `ToPagedListWithoutCountAsync`?

- `ToPagedListAsync` executes **two queries**: a `COUNT(*)` to determine `TotalCount`/`TotalPages`, and then the data query with `OFFSET/LIMIT`. Use this when your UI needs total page count.
- `ToPagedListWithoutCountAsync` executes **one query** using a `Take(N+1)` probe to determine `HasNextPage`. `TotalCount` and `TotalPages` are not available. Use this for infinite-scroll UIs or when COUNT is prohibitively expensive on large tables.
