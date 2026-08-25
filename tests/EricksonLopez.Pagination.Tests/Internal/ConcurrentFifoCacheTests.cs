// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Internal;
using Xunit;

namespace EricksonLopez.Pagination.Tests.Internal;

public class ConcurrentFifoCacheTests
{
    [Fact]
    public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new ConcurrentFifoCache<string, string>(0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxCapacity");
    }

    [Fact]
    public void GetOrAdd_AddsValue_WhenNotExists()
    {
        var cache = new ConcurrentFifoCache<int, string>(2);
        
        var val = cache.GetOrAdd(1, k => $"Val{k}");
        val.Should().Be("Val1");
    }

    [Fact]
    public void GetOrAdd_ReturnsExistingValue_WhenExists()
    {
        var cache = new ConcurrentFifoCache<int, string>(2);
        
        cache.GetOrAdd(1, _ => "First");
        var val = cache.GetOrAdd(1, _ => "Second");
        
        val.Should().Be("First");
    }

    [Fact]
    public void GetOrAdd_EvictsOldest_WhenExceedsCapacity()
    {
        var cache = new ConcurrentFifoCache<int, string>(2);
        
        cache.GetOrAdd(1, _ => "A");
        cache.GetOrAdd(2, _ => "B");
        cache.GetOrAdd(3, _ => "C"); // This should evict 1
        
        var val2 = cache.GetOrAdd(2, _ => "NewB"); // 2 should NOT be evicted yet
        var val1 = cache.GetOrAdd(1, _ => "NewA"); // This should be a cache miss and run factory
        
        val1.Should().Be("NewA");
        val2.Should().Be("B");
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cache = new ConcurrentFifoCache<int, string>(2);
        
        cache.GetOrAdd(1, _ => "A");
        cache.GetOrAdd(2, _ => "B");
        
        cache.Clear();
        
        var val1 = cache.GetOrAdd(1, _ => "NewA");
        val1.Should().Be("NewA");
    }

    [Fact]
    public void Clear_ClearsQueueSoEvictionWorksCorrectlyAfterwards()
    {
        var cache = new ConcurrentFifoCache<int, string>(2);
        
        cache.GetOrAdd(1, _ => "A");
        cache.GetOrAdd(2, _ => "B");
        
        cache.Clear();
        
        cache.GetOrAdd(3, _ => "C");
        cache.GetOrAdd(4, _ => "D");
        cache.GetOrAdd(5, _ => "E"); // This should evict 3 if queue was properly cleared
        
        var val3 = cache.GetOrAdd(3, _ => "NewC");
        val3.Should().Be("NewC"); // 3 must have been evicted, so factory runs again
    }
    
    [Fact]
    public async Task GetOrAdd_ConcurrentAccess_IsThreadSafe()
    {
        var cache = new ConcurrentFifoCache<int, string>(100);
        
        var tasks = new Task[500];
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i % 150; // Causes evictions since maxCapacity is 100
            tasks[i] = Task.Run(() => 
            {
                var val = cache.GetOrAdd(index, k => $"Val{k}");
                val.Should().Be($"Val{index}");
            });
        }
        
        await Task.WhenAll(tasks);
    }

    private class RaceConditionComparer : IEqualityComparer<int>
    {
        public bool TryAddFailed { get; set; }
        private int _callCount;
        
        public bool Equals(int x, int y)
        {
            if (x == 2 || y == 2)
            {
                _callCount++;
                if (_callCount == 1) return false; // TryGetValue 1: not found
                if (_callCount == 2)
                {
                    TryAddFailed = true;
                    return true; // TryAdd: found (collision/already exists)
                }
                return false; // TryGetValue 2: not found (evicted)
            }
            return x == y;
        }

        public int GetHashCode(int obj) => 1;
    }

    [Fact]
    public void GetOrAdd_WhenEvictedBetweenTryAddAndTryGetValue_InvokesFactoryAgain()
    {
        var comparer = new RaceConditionComparer();
        var cache = new ConcurrentFifoCache<int, string>(10, comparer);

        // Add first item, stored in bucket 1
        cache.GetOrAdd(1, _ => "One");

        // Add second item. 
        // Hash is 1. TryAdd checks bucket 1, finds key 1. Calls Equals(1, 2). Returns true. TryAdd fails.
        // TryGetValue computes Hash 1. Checks bucket 1, finds key 1. Calls Equals(1, 2). Returns false. TryGetValue fails.
        // Falls back to factory!
        var result = cache.GetOrAdd(2, _ => "TwoFallback");

        result.Should().Be("TwoFallback");
        comparer.TryAddFailed.Should().BeTrue();
    }

    private class ExhaustRetryComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y)
        {
            // Always pretend it exists for TryAdd (causes TryAdd to fail), 
            // but return false for TryGetValue (causes TryGetValue to fail)
            // By doing this based on the stack trace or simply alternating we can force behavior.
            // Actually, we can just alternate:
            // TryGetValue 1: false
            // TryAdd 1: true (fails)
            // TryGetValue 2: false
            // Retry 0 TryAdd: true (fails)
            // Retry 0 TryGetValue: false
            // Retry 1 TryAdd: true (fails)
            // Retry 1 TryGetValue: false
            // Retry 2 TryAdd: true (fails)
            // Retry 2 TryGetValue: false
            
            // Wait, if it always returns true for TryAdd, it will fail TryAdd. 
            // We need to differentiate TryAdd from TryGetValue.
            // TryGetValue calls Equals when walking the bucket. TryAdd calls Equals when checking for duplicates.
            // Let's use a call counter.
            return _callCount++ % 2 != 0; // 0=false, 1=true, 2=false, 3=true...
        }
        private int _callCount;
        public int GetHashCode(int obj) => 1;
    }

    [Fact]
    public void GetOrAdd_ExhaustsRetries_ComputesDirectly()
    {
        var comparer = new ExhaustRetryComparer();
        var cache = new ConcurrentFifoCache<int, string>(10, comparer);

        cache.GetOrAdd(1, _ => "One");
        var result = cache.GetOrAdd(2, _ => "Fallback");
        result.Should().Be("Fallback");
    }

    private class SucceedOnRetryTryGetValueComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y)
        {
            _callCount++;
            if (_callCount == 1) return false; // fast TryGetValue (fails)
            if (_callCount == 2) return true;  // TryAdd (fails)
            if (_callCount == 3) return false; // TryGetValue race (fails)
            
            // Retry 0
            if (_callCount == 4) return true;  // TryAdd (fails)
            if (_callCount == 5) return true;  // TryGetValue (succeeds!)
            
            return false;
        }
        private int _callCount;
        public int GetHashCode(int obj) => 1;
    }

    [Fact]
    public void GetOrAdd_SucceedsTryGetValue_InRetryLoop()
    {
        var comparer = new SucceedOnRetryTryGetValueComparer();
        var cache = new ConcurrentFifoCache<int, string>(10, comparer);

        cache.GetOrAdd(1, _ => "One"); // bucket 1 now has key 1
        var result = cache.GetOrAdd(2, _ => "Two"); // should return "One" because TryGetValue succeeds on retry!
        
        // Wait, if TryGetValue succeeds, it returns the existing lazy value, which evaluates to "One".
        result.Should().Be("One");
    }

    [Fact]
    public void GetOrAdd_EvictsInsideRetryLoop()
    {
        var comparer = new RaceConditionComparer(); // succeeds TryAdd on Retry 0
        var cache = new ConcurrentFifoCache<int, string>(1, comparer);

        cache.GetOrAdd(1, _ => "One");
        // Capacity is 1, so count will be 1. 
        // Adding 2 will succeed in Retry 0, so count becomes 2, which triggers eviction inside the retry loop!
        var result = cache.GetOrAdd(2, _ => "TwoFallback");
        
        result.Should().Be("TwoFallback");
    }
}





