// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class InvalidPaginationCursorExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndCursor_SetsProperties()
    {
        var ex = new InvalidPaginationCursorException("Test message", "cursor123");
        ex.Message.Should().Be("Test message");
        ex.OpaqueCursor.Should().Be("cursor123");
    }

    [Fact]
    public void Constructor_WithMessageCursorAndInnerException_SetsProperties()
    {
        var inner = new InvalidOperationException("Inner");
        var ex = new InvalidPaginationCursorException("Test message", "cursor123", inner);
        ex.Message.Should().Be("Test message");
        ex.OpaqueCursor.Should().Be("cursor123");
        ex.InnerException.Should().Be(inner);
    }
}

