// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class InvalidPaginationCursorExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var msg = "invalid cursor";
        var ex = new InvalidPaginationCursorException(msg, "opaque");
        ex.Message.Should().Be(msg);
        ex.OpaqueCursor.Should().Be("opaque");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullOpaqueCursor_SetsNullOpaqueCursor()
    {
        var msg = "invalid cursor";
        var ex = new InvalidPaginationCursorException(msg, null);
        ex.Message.Should().Be(msg);
        ex.OpaqueCursor.Should().BeNull();
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var msg = "invalid cursor";
        var inner = new Exception("inner");
        var ex = new InvalidPaginationCursorException(msg, "opaque", inner);
        ex.Message.Should().Be(msg);
        ex.OpaqueCursor.Should().Be("opaque");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void Constructor_WithMessageAndNullInnerException_SetsInnerExceptionNull()
    {
        var msg = "invalid cursor";
        var ex = new InvalidPaginationCursorException(msg, "opaque", null!);
        ex.Message.Should().Be(msg);
        ex.OpaqueCursor.Should().Be("opaque");
        ex.InnerException.Should().BeNull();
    }
}

