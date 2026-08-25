// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class ReplayedPaginationCursorExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndParameters_SetsPropertiesCorrectly()
    {
        var msg = "Replayed cursor detected";
        var cursor = "opaque_token_123";
        var nonce = "nonce_abc";

        var ex = new ReplayedPaginationCursorException(msg, cursor, nonce);

        ex.Message.Should().Be(msg);
        ex.OpaqueCursor.Should().Be(cursor);
        ex.Nonce.Should().Be(nonce);
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerExceptionAndParameters_SetsAllPropertiesCorrectly()
    {
        var msg = "Replayed cursor detected";
        var inner = new InvalidOperationException("Internal verification failed");
        var cursor = "opaque_token_456";
        var nonce = "nonce_xyz";

        var ex = new ReplayedPaginationCursorException(msg, inner, cursor, nonce);

        ex.Message.Should().Be(msg);
        ex.InnerException.Should().Be(inner);
        ex.OpaqueCursor.Should().Be(cursor);
        ex.Nonce.Should().Be(nonce);
    }

    [Fact]
    public void Constructor_WithDefaults_AllowsNullCursorAndNonce()
    {
        var msg = "Simple replayed cursor";
        var ex = new ReplayedPaginationCursorException(msg);

        ex.Message.Should().Be(msg);
        ex.OpaqueCursor.Should().BeNull();
        ex.Nonce.Should().BeNull();
        ex.InnerException.Should().BeNull();
    }
}
