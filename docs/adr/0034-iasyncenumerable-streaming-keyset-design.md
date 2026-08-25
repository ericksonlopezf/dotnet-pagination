# ADR-0034 — IAsyncEnumerable Streaming Keyset Pagination

## Status
**Accepted** — August 2026

## Context
Standard cursor pagination delivers fixed-size pages (`ICursorPagedList<T>`) designed for request-response HTTP APIs. However, data export, ETL batch jobs, and background workers often need to process large volumes (e.g. millions of rows) sequentially without loading entire datasets into memory and without experiencing $O(N)$ deep offset degradation.

## Decision
We introduce `AsKeysetStreamAsync` extension methods in `EricksonLopez.Pagination.EntityFrameworkCore` returning `IAsyncEnumerable<T>`:

```csharp
namespace EricksonLopez.Pagination.EntityFrameworkCore;

public static class KeysetStreamingExtensions
{
    public static IAsyncEnumerable<T> AsKeysetStreamAsync<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction = SortDirection.Ascending,
        int batchSize = 1000,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default);

    public static IAsyncEnumerable<T> AsKeysetStreamAsync<T>(
        this KeysetBuilder<T> builder,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);
}
```

### Execution Strategy
1. The stream initializes with the base query.
2. In batches of `batchSize`, items are fetched using B-Tree index seeks.
3. Items are yielded sequentially to the consumer via `IAsyncEnumerable<T>`.
4. When a batch is exhausted, if `HasNextPage` is `true`, the cursor advances to `EndCursor` and fetches the next batch.
5. Constant $O(1)$ memory consumption and $O(\log N)$ database index seeks throughout the entire stream.

## Consequences
- Clean, idiomatic C# `await foreach` consumption for large-scale data export and background processing.
- Zero memory bloat and zero deep offset degradation.
