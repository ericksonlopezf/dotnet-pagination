// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class InMemoryCursorReplayStoreTests
{
    [Fact]
    public void TryAcquireNonce_NullNonce_ThrowsArgumentNullException()
    {
        var store = new InMemoryCursorReplayStore();
        Action act = () => store.TryAcquireNonce(null!, TimeSpan.FromMinutes(1));
        act.Should().Throw<ArgumentNullException>().WithParameterName("nonce");
    }

    [Fact]
    public void TryAcquireNonce_WhenExpired_AllowsReacquisition()
    {
        var store = new InMemoryCursorReplayStore();
        var nonce = "expiring-nonce-1";

        // Acquire with negative TTL so it is already expired
        var first = store.TryAcquireNonce(nonce, TimeSpan.FromMilliseconds(-100));
        first.Should().BeTrue();

        // Reacquire should succeed because existingExpiry <= now
        var second = store.TryAcquireNonce(nonce, TimeSpan.FromMinutes(5));
        second.Should().BeTrue();

        // Third immediate acquisition should fail because it's not expired yet
        var third = store.TryAcquireNonce(nonce, TimeSpan.FromMinutes(5));
        third.Should().BeFalse();
    }


    [Fact]
    public void TryAcquireNonce_WhenExactZeroTtl_AllowsImmediateReacquisition()
    {
        var store = new InMemoryCursorReplayStore();
        var nonce = "zero-ttl-nonce-exact";

        var first = store.TryAcquireNonce(nonce, TimeSpan.Zero);
        first.Should().BeTrue();

        var second = store.TryAcquireNonce(nonce, TimeSpan.FromMinutes(5));
        second.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireNonceAsync_WithCancelledToken_ReturnsCancelledTask()
    {
        var store = new InMemoryCursorReplayStore();
        var token = new CancellationToken(canceled: true);

        var task = store.TryAcquireNonceAsync("any-nonce", TimeSpan.FromMinutes(1), token);

        task.IsCanceled.Should().BeTrue();
        Func<Task> act = async () => await task;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void OpportunisticCleanup_CleansExpiredNoncesAfter256Calls()
    {
        var store = new InMemoryCursorReplayStore();

        // Add 10 nonces that are already expired
        for (int i = 0; i < 10; i++)
        {
            store.TryAcquireNonce($"expired-{i}", TimeSpan.FromMilliseconds(-10));
        }

        // Add active nonces to trigger cleanup threshold (> 256 acquisitions)
        for (int i = 0; i < 260; i++)
        {
            store.TryAcquireNonce($"active-{i}", TimeSpan.FromHours(1));
        }

        // The expired ones should have been cleaned up and can be re-acquired as fresh nonces
        for (int i = 0; i < 10; i++)
        {
            store.TryAcquireNonce($"expired-{i}", TimeSpan.FromHours(1)).Should().BeTrue();
        }
    }
}



