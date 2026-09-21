# Best Practices Guide — EricksonLopez.Pagination

> Production recommendations, security guidelines, and architectural rules for high-scale pagination.

---

## 1. The 10 Golden Rules of Production Pagination

1. **Always Index Keyset Columns**  
   Keyset pagination achieves $O(\log N)$ latency *only* when the database engine can seek directly into a matching B-Tree index. Ensure your composite index matches the exact sort order and columns defined in your keyset builder:
   ```sql
   CREATE INDEX IX_Orders_CreatedAt_Id ON Orders(CreatedAt DESC, Id ASC);
   ```

2. **Always Chain a Strictly Unique Tie-Breaker**  
   Non-unique columns (e.g. `CreatedAt`, `Price`, `Category`) can cause records with identical values to be skipped or duplicated across page boundaries. Always end your keyset hierarchy with a unique column (such as primary key `Id`):
   ```csharp
   query.Keyset(cursor).Descending(o => o.CreatedAt).Ascending(o => o.Id);
   ```

3. **Enforce `MaxPageSize` at the API Gateway / Filter Layer**  
   Never let consumers request arbitrary page sizes (e.g. `pageSize=1000000`). Always chain `.AddPaginationValidation()` on your routes to reject oversized requests before database query execution begins.

4. **Cryptographically Seal Cursors with HMAC-SHA256 in Public APIs**  
   Never expose unencrypted, plain Base64 cursor IDs in public endpoints. An attacker can manipulate IDs to scrape records or inspect data patterns. Use `HmacCursorEncoder` with a strong 32-byte secret key.

5. **Set Realistic Cursor Expiration (TTL)**  
   Stale cursors referencing datasets that have undergone significant modifications can cause unexpected query results. Set a reasonable TTL (e.g. 15 to 60 minutes) to keep pagination bounded to recent state.

6. **Avoid `COUNT(*)` on Massive Tables**  
   If your UI displays an infinite feed or standard "Next / Previous" buttons, set `countTotal: false`. This avoids expensive table-wide count scans and reduces query latency by over 50%.

7. **Use `LazyMap` for Large DTO Transformations**  
   When transforming large page collections into DTOs, use `pagedList.LazyMap(p => new Dto(...))` instead of `Map(...)`. `LazyMap` evaluates projections on-the-fly during serialization, saving significant GC allocations.

8. **Leverage Deterministic ETags & HTTP 304 Caching**  
   Use `ToPagedResult(request, maxAge)` to attach deterministic SHA-256 ETags to your responses. Browsers and CDNs sending `If-None-Match` will receive an instant `304 Not Modified`, saving server CPU and network bandwidth.

9. **Restrict Filter DSL Properties with Allowlist Security**  
   Always provide an allowlist of permitted property names to `ApplyFilter` and `ApplySort` (or annotate properties with `[Filterable]`). This prevents unauthorized querying of sensitive columns like passwords, secrets, or internal flags.

10. **Propagate `CancellationToken` Through All Calls**  
    Database queries can be long-running under heavy load. Always pass the HTTP request's `CancellationToken` into `ToPagedListAsync`, `ToCursorPagedListAsync`, and batch streaming operations so aborted requests terminate immediately.
