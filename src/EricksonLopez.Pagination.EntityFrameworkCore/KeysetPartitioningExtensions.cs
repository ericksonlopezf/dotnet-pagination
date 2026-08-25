// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.EntityFrameworkCore;

/// <summary>
/// Provides extension methods to partition a keyset queryable into multiple disjoint ranges for parallel worker processing.
/// </summary>
public static class KeysetPartitioningExtensions
{
    /// <summary>
    /// Divides an integer-keyed query into disjoint partitions based on minimum and maximum key boundaries.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable to partition.</param>
    /// <param name="keySelector">An expression selecting the integer key.</param>
    /// <param name="partitionCount">The number of parallel partitions to create.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the list of keyset partitions.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="partitionCount"/> is less than or equal to zero</exception>
    public static async Task<IReadOnlyList<KeysetPartition<int>>> SplitKeysetPartitionsAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, int>> keySelector,
        int partitionCount,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (partitionCount <= 0) throw new ArgumentOutOfRangeException(nameof(partitionCount), "Partition count must be greater than zero.");

        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;

        var hasAny = await source.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (!hasAny)
        {
            return Array.Empty<KeysetPartition<int>>();
        }

        var minKey = await source.MinAsync(keySelector, cancellationToken).ConfigureAwait(false);
        var maxKey = await source.MaxAsync(keySelector, cancellationToken).ConfigureAwait(false);

        // Stryker disable once all : Early return optimization produces identical partition to general loop
        if (minKey == maxKey || partitionCount == 1)
        {
            return new[]
            {
                new KeysetPartition<int>(0, minKey, maxKey, encoder.Encode(minKey.ToString()), encoder.Encode(maxKey.ToString()))
            };
        }

        long range = (long)maxKey - minKey + 1;
        long step = (long)Math.Ceiling((double)range / partitionCount);

        var partitions = new List<KeysetPartition<int>>(partitionCount);

        // Stryker disable once equality : loop index bound is guarded by internal break when lower > maxKey
        for (int i = 0; i < partitionCount; i++)
        {
            int lower = (int)(minKey + (i * step));
            int upper = (int)Math.Min(minKey + ((i + 1) * step) - 1, maxKey);

            if (lower > maxKey) break;

            partitions.Add(new KeysetPartition<int>(
                i,
                lower,
                upper,
                encoder.Encode(lower.ToString()),
                encoder.Encode(upper.ToString())));
        }

        return partitions;
    }

    /// <summary>
    /// Divides a 64-bit integer-keyed query into disjoint partitions based on minimum and maximum key boundaries.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable to partition.</param>
    /// <param name="keySelector">An expression selecting the long integer key.</param>
    /// <param name="partitionCount">The number of parallel partitions to create.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the list of keyset partitions.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="partitionCount"/> is less than or equal to zero</exception>
    public static async Task<IReadOnlyList<KeysetPartition<long>>> SplitKeysetPartitionsAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, long>> keySelector,
        int partitionCount,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (partitionCount <= 0) throw new ArgumentOutOfRangeException(nameof(partitionCount), "Partition count must be greater than zero.");

        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;

        var hasAny = await source.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (!hasAny)
        {
            return Array.Empty<KeysetPartition<long>>();
        }

        var minKey = await source.MinAsync(keySelector, cancellationToken).ConfigureAwait(false);
        var maxKey = await source.MaxAsync(keySelector, cancellationToken).ConfigureAwait(false);

        // Stryker disable once all : Early return optimization produces identical partition to general loop
        if (minKey == maxKey || partitionCount == 1)
        {
            return new[]
            {
                new KeysetPartition<long>(0, minKey, maxKey, encoder.Encode(minKey.ToString()), encoder.Encode(maxKey.ToString()))
            };
        }

        decimal range = (decimal)maxKey - minKey + 1;
        long step = (long)Math.Ceiling(range / partitionCount);

        var partitions = new List<KeysetPartition<long>>(partitionCount);

        // Stryker disable once equality : loop index bound is guarded by internal break when lower > maxKey
        for (int i = 0; i < partitionCount; i++)
        {
            long lower = minKey + (i * step);
            long upper = Math.Min(minKey + ((i + 1) * step) - 1, maxKey);

            if (lower > maxKey) break;

            partitions.Add(new KeysetPartition<long>(
                i,
                lower,
                upper,
                encoder.Encode(lower.ToString()),
                encoder.Encode(upper.ToString())));
        }

        return partitions;
    }

    /// <summary>
    /// Partitions an integer-keyed query into disjoint ranges for parallel worker processing.
    /// Alias for <see cref="SplitKeysetPartitionsAsync{T}(IQueryable{T}, Expression{Func{T, int}}, int, ICursorEncoder?, CancellationToken)"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable to partition.</param>
    /// <param name="keySelector">An expression selecting the integer key.</param>
    /// <param name="partitionCount">The number of parallel partitions to create.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of keyset partitions.</returns>
    public static Task<IReadOnlyList<KeysetPartition<int>>> PartitionByKeysetAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, int>> keySelector,
        int partitionCount,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default)
    {
        return source.SplitKeysetPartitionsAsync(keySelector, partitionCount, cursorEncoder, cancellationToken);
    }

    /// <summary>
    /// Partitions a 64-bit integer-keyed query into disjoint ranges for parallel worker processing.
    /// Alias for <see cref="SplitKeysetPartitionsAsync{T}(IQueryable{T}, Expression{Func{T, long}}, int, ICursorEncoder?, CancellationToken)"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable to partition.</param>
    /// <param name="keySelector">An expression selecting the long integer key.</param>
    /// <param name="partitionCount">The number of parallel partitions to create.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of keyset partitions.</returns>
    public static Task<IReadOnlyList<KeysetPartition<long>>> PartitionByKeysetAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, long>> keySelector,
        int partitionCount,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default)
    {
        return source.SplitKeysetPartitionsAsync(keySelector, partitionCount, cursorEncoder, cancellationToken);
    }
}



