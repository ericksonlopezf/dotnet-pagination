// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EricksonLopez.Pagination.Internal;

/// <summary>
/// A thread-safe, bounded cache used internally.
/// Evicts the oldest entry when maximum capacity is reached (FIFO).
/// </summary>
/// <remarks>
/// Note: Types in the <c>EricksonLopez.Pagination.Internal</c> namespace are not part of the public API 
/// and may change in any minor version. Do not use them directly.
/// <para>
/// <b>Concurrency note — soft limit:</b> Under high concurrent insertion, the cache may temporarily
/// exceed <c>maxCapacity</c> by up to <c>N</c> concurrent writers before eviction completes.
/// This implementation uses atomic decrements to ensure the count remains eventually consistent
/// even if multiple threads compete to evict the same key.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "We only use the factory constructor of Lazy<T> which does not require a parameterless constructor.")]

internal sealed class ConcurrentFifoCache<TKey, TValue> where TKey : notnull
{
    private readonly int _maxCapacity;
    private readonly ConcurrentDictionary<TKey, Lazy<TValue?>> _cache;
    private readonly ConcurrentQueue<TKey> _queue = new();
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentFifoCache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="maxCapacity">The maximum number of items the cache can hold before evicting the oldest ones.</param>
    /// <param name="comparer">The equality comparer to use for comparing keys.</param>
    public ConcurrentFifoCache(int maxCapacity = 512, IEqualityComparer<TKey>? comparer = null)
    {
        _maxCapacity = maxCapacity > 0 ? maxCapacity : throw new ArgumentOutOfRangeException(nameof(maxCapacity));
        _cache = new ConcurrentDictionary<TKey, Lazy<TValue?>>(comparer ?? EqualityComparer<TKey>.Default);
    }


    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        // ConcurrentQueue.Clear() is available in .NET Core 2.0+ and .NET Standard 2.1+
        // Since we target netstandard2.0, we just dequeue until empty.
#if NETSTANDARD2_0
        while (_queue.TryDequeue(out _)) { }
#else
        _queue.Clear();
#endif
        // Stryker disable once statement : Clearing count concurrently is a threading optimization that cannot be functionally tested reliably
        Interlocked.Exchange(ref _count, 0);
    }

    /// <summary>
    /// Adds a key/value pair to the cache if the key does not already exist, or returns the existing value.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key.</param>
    /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the cache, or the new value if the key was not in the cache.</returns>
    public TValue? GetOrAdd(TKey key, Func<TKey, TValue?> valueFactory)
    {
        // Fast path: key already in cache.
        // Stryker disable once block : Fast path optimization. If removed, the slow path still returns the correct value via ConcurrentDictionary.GetOrAdd
        if (_cache.TryGetValue(key, out var lazy))
        {
            return lazy.Value;
        }

#pragma warning disable IL2091
        lazy = new Lazy<TValue?>(() => valueFactory(key), LazyThreadSafetyMode.ExecutionAndPublication);
#pragma warning restore IL2091

        if (_cache.TryAdd(key, lazy))
        {
            _queue.Enqueue(key);
            Interlocked.Increment(ref _count);

            // Evict oldest entries while over capacity.
            // Multiple threads may enter this loop concurrently; using TryDequeue ensures each
            // queue slot is consumed by exactly one thread. If TryRemove fails (another thread
            // already removed the same key), we still decrement _count to account for the
            // queue slot we consumed so the counter does not drift upward permanently.
            while (Volatile.Read(ref _count) > _maxCapacity && _queue.TryDequeue(out var evictKey))
            {
                _cache.TryRemove(evictKey, out _);
                Interlocked.Decrement(ref _count);
            }

            return lazy.Value;
        }

        return HandleRaceCondition(key, valueFactory);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private TValue? HandleRaceCondition(TKey key, Func<TKey, TValue?> valueFactory)
    {
        if (_cache.TryGetValue(key, out var lazy))
        {
            return lazy.Value;
        }

        for (int retry = 0; retry < 3; retry++)
        {
            lazy = new Lazy<TValue?>(() => valueFactory(key), LazyThreadSafetyMode.ExecutionAndPublication);
            if (_cache.TryAdd(key, lazy))
            {
                _queue.Enqueue(key);
                Interlocked.Increment(ref _count);

                while (Volatile.Read(ref _count) > _maxCapacity && _queue.TryDequeue(out var evictKey2))
                {
                    _cache.TryRemove(evictKey2, out _);
                    Interlocked.Decrement(ref _count);
                }

                return lazy.Value;
            }

            if (_cache.TryGetValue(key, out lazy))
                return lazy.Value;
        }

        return valueFactory(key);
    }
}

