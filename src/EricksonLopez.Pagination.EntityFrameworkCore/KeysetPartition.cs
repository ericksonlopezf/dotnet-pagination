// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Pagination.EntityFrameworkCore;

/// <summary>
/// Represents a bounded keyset partition for parallel execution.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <param name="PartitionIndex">The zero-based partition index.</param>
/// <param name="LowerBound">The inclusive lower bound key (null for first partition).</param>
/// <param name="UpperBound">The exclusive upper bound key (null for last partition).</param>
/// <param name="StartCursor">The encoded start cursor for this partition.</param>
/// <param name="EndCursor">The encoded end cursor for this partition.</param>
public sealed record KeysetPartition<TKey>(
    int PartitionIndex,
    TKey? LowerBound,
    TKey? UpperBound,
    string? StartCursor,
    string? EndCursor);
