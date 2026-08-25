# ADR-0036 — Parallel Keyset Partitioning (Multi-Cursor ETL)

## Status
**Accepted** — August 2026

## Context
High-throughput background jobs, data pipelines, and parallel batch workers (e.g. `Parallel.ForEachAsync`, distributed job runners) need to partition a dataset across $K$ concurrent workers. Doing so with offset pagination causes severe database scanning and lock contention.

## Decision
We introduce `SplitKeysetPartitionsAsync` in `EricksonLopez.Pagination.EntityFrameworkCore`:

```csharp
namespace EricksonLopez.Pagination.EntityFrameworkCore;

public sealed record KeysetPartition<TKey>(
    int PartitionIndex,
    TKey? LowerBound,
    TKey? UpperBound,
    string? StartCursor,
    string? EndCursor);

public static class KeysetPartitioningExtensions
{
    public static Task<IReadOnlyList<KeysetPartition<TKey>>> SplitKeysetPartitionsAsync<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        int partitionCount,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default)
        where TKey : struct, IComparable<TKey>;
}
```

### Execution Strategy
1. Computes the minimum and maximum keys across the dataset.
2. Divides the key range into $K$ disjoint partition boundaries.
3. Emits signed cursors for each partition boundary.
4. Each worker streams or paginates its partition independently using bounded keyset queries `WHERE key >= LowerBound AND key < UpperBound`.

## Consequences
- Enables linear horizontal scaling of keyset queries across multi-core machines and distributed worker fleets.
- Zero table lock contention or offset degradation.
