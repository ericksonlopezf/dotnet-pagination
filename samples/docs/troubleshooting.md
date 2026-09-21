# Troubleshooting Guide — EricksonLopez.Pagination

> Common issues, Roslyn analyzer diagnostics, runtime exceptions, and resolution steps.

---

## 1. Runtime Exceptions

### `InvalidPaginationCursorException`

**Symptom:**  
HTTP 400 Bad Request with details: `"The provided cursor is malformed, corrupted, or has an invalid HMAC signature."`

**Root Causes:**
1. The client modified or truncated the opaque cursor string.
2. The client presented an unencrypted cursor to an endpoint expecting an HMAC-signed cursor.
3. The server's HMAC secret key was rotated without backward compatibility.

**Fix:**
- Ensure clients treat cursors as opaque tokens without decoding or altering them.
- If rotating secret keys, implement key versioning or keep previous keys active during a grace period.

---

### `ExpiredPaginationCursorException`

**Symptom:**  
HTTP 410 Gone (or HTTP 400) with details indicating cursor expired at `<timestamp>`.

**Root Causes:**
- The cursor's age exceeds the configured `timeToLive` on `HmacCursorEncoder`.

**Fix:**
- Adjust `timeToLive` in `Program.cs` if users frequently pause during long sessions:
  ```csharp
  options.Cursor.Encoder = new HmacCursorEncoder(key, timeToLive: TimeSpan.FromHours(2));
  ```
- Catch `ExpiredPaginationCursorException` explicitly to return HTTP 410 Gone and instruct clients to refresh their query.

---

### `ReplayedPaginationCursorException`

**Symptom:**  
HTTP 409 Conflict (or HTTP 400) with details indicating cursor nonce was already consumed.

**Root Causes:**
- A client submitted the exact same cursor multiple times when `ICursorReplayStore` is active.

**Fix:**
- Verify whether single-use replay protection is strictly required on the endpoint. If idempotent re-reads should be permitted, configure `ICursorReplayStore` only on sensitive write-heavy or one-time paging streams.

---

## 2. Roslyn Analyzer Diagnostics

The `EricksonLopez.Pagination.Analyzers` package enforces pagination best practices at compile time:

| Diagnostic ID | Severity | Title | Cause & Fix |
|---|---|---|---|
| **`PAG001`** | Warning / Error | Missing Deterministic OrderBy | Calling `ToPagedListAsync` without preceding `.OrderBy()`. Add `.OrderBy(x => x.Id)`. |
| **`PAG002`** | Warning | Insecure Base64 Cursor in Production | Explicit instantiation of `Base64CursorEncoder`. Replace with `HmacCursorEncoder`. |
| **`PAG003`** | Error | Missing Unique Keyset Tie-Breaker | Keyset query without a strictly unique trailing column. Append `.Ascending(x => x.Id)` as the final column. |
| **`PAG004`** | Warning | OrderBy Before Keyset Call | Manually calling `.OrderBy(...)` before `.Keyset(...)`. Remove the manual `OrderBy` and use `.Ascending()` / `.Descending()` on the keyset builder. |
| **`PAG005`** | Info | Deep Offset Threshold Exceeded | Constant page number exceeds warning threshold. Migrate to keyset pagination. |
| **`PAG006`** | Info | Recommend AOT Source Generators | Using reflection-based decoders in Native AOT projects. Add `[GenerateFilterProvider]`. |
| **`PAG007`** | Warning | Unsafe Dynamic Column Sort | Dynamic sort expression targeting unvalidated property name. Pass `allowedProperties` allowlist. |

---

## 3. Common Configuration Issues

### "PageSize exceeds maximum allowed limit"
**Symptom:**  
Request returns HTTP 400: `{"error": "PageSize cannot exceed 100."}`.

**Fix:**
- Increase `MaxPageSize` in `Program.cs` via `options.MaxPageSize = 250;`, or pass a localized `maxPageSize` override:
  ```csharp
  .ToPagedListAsync(pagination, maxPageSize: 250);
  ```

---

### SQLite / In-Memory Database Multiple Queries Warning
**Symptom:**  
`InvalidOperationException: A second operation was started on this context instance before a previous operation completed.`

**Fix:**
- Ensure `await` is used on both the count query and data query, or ensure scoped DbContext lifecycle. Do not execute parallel async tasks on the same `DbContext` instance.
