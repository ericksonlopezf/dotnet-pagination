// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CursorReplayProtectionTests
{
    private const string SecretKey = "a-very-secret-32-byte-key-for-testing-purposes";

    [Fact]
    public void NonceAcquisition_FirstTimeSucceeds_SecondTimeFails()
    {
        var store = new InMemoryCursorReplayStore();
        var nonce = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromMinutes(5);

        store.TryAcquireNonce(nonce, ttl).Should().BeTrue();
        store.TryAcquireNonce(nonce, ttl).Should().BeFalse();
    }

    [Fact]
    public async Task NonceAcquisitionAsync_FirstTimeSucceeds_SecondTimeFails()
    {
        var store = new InMemoryCursorReplayStore();
        var nonce = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromMinutes(5);

        var first = await store.TryAcquireNonceAsync(nonce, ttl);
        var second = await store.TryAcquireNonceAsync(nonce, ttl);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public void HmacCursorEncoder_WithReplayProtection_EncodesAndDecodesOnce()
    {
        var store = new InMemoryCursorReplayStore();
        using var encoder = new HmacCursorEncoder(SecretKey, replayStore: store);

        var rawCursor = "user_42";
        var encoded = encoder.Encode(rawCursor);
        encoded.Should().NotBeNull();

        // First decode should succeed
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be(rawCursor);

        // Second decode of the exact same cursor should throw ReplayedPaginationCursorException
        Action act = () => encoder.Decode(encoded);
        act.Should().Throw<ReplayedPaginationCursorException>()
           .WithMessage("*consumed*");
    }

    [Fact]
    public void HmacCursorEncoder_WithReplayProtectionAndTTL_EncodesAndDecodesOnce()
    {
        var store = new InMemoryCursorReplayStore();
        using var encoder = new HmacCursorEncoder(SecretKey, timeToLive: TimeSpan.FromMinutes(10), replayStore: store);

        var rawCursor = "order_999";
        var encoded = encoder.Encode(rawCursor);
        encoded.Should().NotBeNull();

        // First decode should succeed
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be(rawCursor);

        // Second decode should fail replay
        Action act = () => encoder.Decode(encoded);
        act.Should().Throw<ReplayedPaginationCursorException>();
    }

    [Fact]
    public void HmacCursorEncoder_ReplayProtected_RejectsCursorWithoutNonce()
    {
        using var unprotectedEncoder = new HmacCursorEncoder(SecretKey);
        var store = new InMemoryCursorReplayStore();
        using var protectedEncoder = new HmacCursorEncoder(SecretKey, replayStore: store);

        var unprotectedCursor = unprotectedEncoder.Encode("item_1");

        Action act = () => protectedEncoder.Decode(unprotectedCursor);
        act.Should().Throw<InvalidPaginationCursorException>()
           .WithMessage("*missing a required replay protection nonce*");
    }
}



