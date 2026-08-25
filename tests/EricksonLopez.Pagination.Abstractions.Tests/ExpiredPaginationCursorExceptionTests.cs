// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class ExpiredPaginationCursorExceptionTests
{
    [Fact]
    public void Constructor_SetsMessageAndProperties()
    {
        var expiredAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var ex = new ExpiredPaginationCursorException("opaque", expiredAt);
        
        ex.OpaqueCursor.Should().Be("opaque");
        ex.ExpiredAt.Should().Be(expiredAt);
        ex.Message.Should().Be($"The cursor expired at {expiredAt:O}. Restart pagination from the first page.");
    }

    [Fact]
    public void Constructor_WithNullOpaqueCursor_SetsNullOpaqueCursor()
    {
        var expiredAt = new DateTimeOffset(2026, 5, 10, 12, 30, 0, TimeSpan.Zero);
        var ex = new ExpiredPaginationCursorException(null, expiredAt);
        
        ex.OpaqueCursor.Should().BeNull();
        ex.ExpiredAt.Should().Be(expiredAt);
        ex.Message.Should().Be($"The cursor expired at {expiredAt:O}. Restart pagination from the first page.");
    }
}

