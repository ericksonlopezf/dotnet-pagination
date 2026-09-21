# Frequently Asked Questions — EricksonLopez.Pagination

---

## 1. What is the difference between Offset and Keyset (Cursor) Pagination?
- **Offset pagination** computes `(page - 1) * pageSize` and executes `OFFSET n ROWS FETCH NEXT m ROWS`. As page depth increases, database read latency degrades linearly ($O(N)$), because the database must scan and discard all prior records.
- **Keyset pagination** remembers an anchor value from the last seen item (`WHERE (CreatedAt < @LastDate) OR (CreatedAt = @LastDate AND Id > @LastId)`) and executes a direct B-Tree index seek ($O(\log N)$). Latency remains constant whether querying page 1 or page 50,000.

---

## 2. Does my Domain layer need to reference ASP.NET Core or EF Core?
No. The core pagination contracts (`IPagedList<T>`, `ICursorPagedList<T>`, `PaginationParameters`, `CursorPaginationParameters`, exceptions) reside strictly within `EricksonLopez.Pagination.Abstractions`. Your Domain and Application layers can depend solely on `Abstractions` without any web framework or database driver dependencies.

---

## 3. How does the library protect against cursor tampering?
By default in `AddPagination()`, the library registers `HmacCursorEncoder` with a development fallback key and warns at startup. In production, configure a 32-byte secret key. Cursors are signed with HMAC-SHA256, verified in constant time (`CryptographicOperations.FixedTimeEquals`), and can be configured with a time-to-live (`timeToLive`) and replay protection (`ICursorReplayStore`).

---

## 4. Why does Roslyn Analyzer PAG001 trigger on my query?
Roslyn Analyzer `PAG001` detects `ToPagedListAsync` invocations that lack an explicit `.OrderBy()` or `.OrderByDescending()`. Relational databases do not guarantee deterministic row ordering without an explicit `ORDER BY` clause; omitting it can result in duplicate or skipped records across pages.

---

## 5. Can I use this library with Native AOT?
Yes! `EricksonLopez.Pagination.Abstractions` and core models are 100% reflection-free and Native AOT compatible. For ASP.NET Core serialization, use `PaginationJsonSerializerContext`. For compile-time dynamic filtering without reflection, use the `[GenerateFilterProvider]` source generator attribute.

---

## 6. How does ETag caching work?
Chaining `.ToPagedResult(request, maxAge)` automatically calculates a deterministic SHA-256 hash of the paginated items and attaches an `ETag` header. When a browser or CDN sends a conditional `If-None-Match` header matching the hash, the server immediately returns `304 Not Modified` with an empty body, saving serialization CPU and network bandwidth.
