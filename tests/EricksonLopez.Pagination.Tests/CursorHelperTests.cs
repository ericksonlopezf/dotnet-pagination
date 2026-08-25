// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CursorHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EncodeCursor_NullOrEmpty_ReturnsNull(string? rawCursor)
    {
        var result = Base64CursorEncoder.Default.Encode(rawCursor);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("123", "MTIz")]
    [InlineData("test cursor", "dGVzdCBjdXJzb3I")]
    public void EncodeCursor_ValidString_ReturnsBase64(string rawCursor, string expectedBase64)
    {
        var result = Base64CursorEncoder.Default.Encode(rawCursor);
        result.Should().Be(expectedBase64);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DecodeCursor_NullOrEmpty_ReturnsNull(string? opaqueCursor)
    {
        var result = Base64CursorEncoder.Default.Decode(opaqueCursor);
        result.Should().BeNull();
    }

    [Fact]
    public void DecodeCursor_InvalidBase64_ThrowsInvalidPaginationCursorException()
    {
        var result = () => Base64CursorEncoder.Default.Decode("not-base-64!!!");
        result.Should().Throw<InvalidPaginationCursorException>();
    }

    [Theory]
    [InlineData("MTIz", "123")]
    [InlineData("dGVzdCBjdXJzb3I=", "test cursor")]
    public void DecodeCursor_ValidBase64_ReturnsString(string opaqueCursor, string expectedString)
    {
        var result = Base64CursorEncoder.Default.Decode(opaqueCursor);
        result.Should().Be(expectedString);
    }
}



