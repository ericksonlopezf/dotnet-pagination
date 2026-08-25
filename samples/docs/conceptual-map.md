# Conceptual Map — EricksonLopez.Pagination

This document provides a conceptual comparison between pagination strategies and architecture components.

---

## 1. Offset Pagination vs. Keyset (Cursor) Pagination

| Dimension | Offset Pagination | Keyset (Cursor) Pagination |
|---|---|---|
| **Underlying Mechanism** | `OFFSET (page-1)*pageSize LIMIT pageSize` | `WHERE Key > @LastKey ORDER BY Key LIMIT pageSize` |
| **Complexity** | $O(N)$ — database reads and discards prior rows | $O(\log N)$ — direct B-Tree index seek |
| **Deep Page Performance** | Degrades linearly as page number increases | Constant latency regardless of pagination depth |
| **Arbitrary Page Navigation** | Yes (e.g. jump directly to Page 42) | No (sequential forward/backward navigation only) |
| **Total Count Support** | Native ($O(N)$ `COUNT(*)` or fast approximate) | Requires separate count or omitted for throughput |
| **Data Drift Tolerance** | Prone to skipped/duplicate items on inserts | Immune to drift; stable pagination window |
| **Native AOT Support** | Fully compatible (zero reflection) | Reflection/Expression compilation (use source generator) |
| **Primary Use Cases** | Admin grids, reporting, numbered page UI | Infinite scroll, social feeds, high-throughput APIs, IoT logs |

---

## 2. Security Architecture: HMAC Signing & Replay Protection

### Threat Model
1. **Cursor Tampering**: An attacker modifies cursor parameters (e.g. changing an ID or timestamp) to access unauthorized records.
   - **Mitigation**: `HmacCursorEncoder` signs the cursor payload with `HMAC-SHA256`. Any modification causes a cryptographic mismatch, throwing `InvalidPaginationCursorException`.
2. **Stale Cursor Drift**: Stored or cached cursors referencing old dataset states lead to inconsistent queries.
   - **Mitigation**: `HmacCursorEncoder` with `timeToLive` embeds a timestamp. Cursors older than the TTL throw `ExpiredPaginationCursorException`.
3. **Replay Attacks**: A consumer repeatedly submits the same pagination request to scrape or overwhelm backends.
   - **Mitigation**: `ICursorReplayStore` (`InMemoryCursorReplayStore` or Redis-backed) validates unique nonces. Re-submitted nonces throw `ReplayedPaginationCursorException`.

---

## 3. Data Source Ecosystem Compatibility

```
                        ┌─────────────────────────────────────┐
                        │   EricksonLopez.Pagination.Core     │
                        │    (IPagedList, CursorParameters)   │
                        └──────────────────┬──────────────────┘
                                           │
         ┌───────────────────┬─────────────┴───────┬────────────────────┐
         │                   │                     │                    │
         ▼                   ▼                     ▼                    ▼
┌──────────────────┐┌──────────────────┐┌──────────────────┐┌──────────────────┐
│ Entity Framework ││      Dapper      ││     MongoDB      ││ Azure Cosmos DB  │
│      Core        ││                  ││                  ││                  │
│ • KeysetBuilder  ││ • DapperKeyset   ││ • MongoCursor    ││ • FeedIterator   │
│ • Partitioning   ││ • CursorSqlBldr  ││ • ObjectIdSeek   ││ • Cont. Tokens   │
│ • Batching       ││ • DualResultSet  ││ • FluentFind     ││                  │
└──────────────────┘└──────────────────┘└──────────────────┘└──────────────────┘
```
