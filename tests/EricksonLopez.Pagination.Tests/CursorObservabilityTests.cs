// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CursorObservabilityTests
{
    private const string SecretKey = "a-very-secret-32-byte-key-for-testing-purposes";

    [Fact]
    public void HmacCursorEncoder_LogsWarning_WhenCursorIsTampered()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);

        using var encoder = new HmacCursorEncoder(SecretKey, logger: logger);

        // Encode a payload with an invalid/forged signature
        var tamperedCursor = Base64CursorEncoder.Default.Encode("N:item_123.forgedSignature123456789012345678901234");

        Action act = () => encoder.Decode(tamperedCursor);
        act.Should().Throw<InvalidPaginationCursorException>();

        logger.Received().Log(
            LogLevel.Warning,
            PaginationLogEvents.CursorTampered,
            Arg.Is<object>(o => o.ToString()!.Contains("failed HMAC signature validation")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void HmacCursorEncoder_LogsWarning_WhenCursorIsExpired()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);

        using var encoder = new HmacCursorEncoder(
            SecretKey,
            timeToLive: TimeSpan.FromSeconds(-60), // already expired
            clockSkewTolerance: TimeSpan.Zero,
            logger: logger);

        var cursor = encoder.Encode("item_123");

        Action act = () => encoder.Decode(cursor);
        act.Should().Throw<ExpiredPaginationCursorException>();

        logger.Received().Log(
            LogLevel.Warning,
            PaginationLogEvents.CursorExpired,
            Arg.Is<object>(o => o.ToString()!.Contains("expired")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void HmacCursorEncoder_LogsWarning_WhenCursorIsReplayed()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var store = new InMemoryCursorReplayStore();

        using var encoder = new HmacCursorEncoder(
            SecretKey,
            replayStore: store,
            logger: logger);

        var cursor = encoder.Encode("item_replayed");

        // First decode succeeds
        encoder.Decode(cursor);

        // Second decode triggers replay warning and throws
        Action act = () => encoder.Decode(cursor);
        act.Should().Throw<ReplayedPaginationCursorException>();

        logger.Received().Log(
            LogLevel.Warning,
            PaginationLogEvents.CursorReplayed,
            Arg.Is<object>(o => o.ToString()!.Contains("replay")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
