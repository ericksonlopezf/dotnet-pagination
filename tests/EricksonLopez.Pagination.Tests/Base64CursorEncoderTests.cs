// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class Base64CursorEncoderTests
{
    private readonly Base64CursorEncoder _sut = Base64CursorEncoder.Default;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encode_NullOrEmpty_ReturnsNull(string? rawCursor)
    {
        var result = _sut.Encode(rawCursor);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decode_NullOrWhiteSpace_ReturnsNull(string? opaqueCursor)
    {
        var result = _sut.Decode(opaqueCursor);
        result.Should().BeNull();
    }

    [Fact]
    public void EncodeAndDecode_WithSmallString_RoundTripsSuccessfully()
    {
        var raw = "HelloWorld123";
        var encoded = _sut.Encode(raw);
        
        encoded.Should().NotBeNullOrEmpty();
        encoded.Should().NotContain("+").And.NotContain("/").And.NotContain("=");

        var decoded = _sut.Decode(encoded);
        decoded.Should().Be(raw);
    }

    [Fact]
    public void EncodeAndDecode_WithLargeString_RoundTripsSuccessfully()
    {
        // 2000 chars should exceed 1024 bytes for UTF8 buffer and Base64 buffer
        var raw = new string('A', 2000);
        var encoded = _sut.Encode(raw);
        
        encoded.Should().NotBeNullOrEmpty();

        var decoded = _sut.Decode(encoded);
        decoded.Should().Be(raw);
    }

    [Fact]
    public void Decode_WithPaddingCharacters_DecodesSuccessfully()
    {
        // "abc" encoded in normal base64 is "YWJj"
        // Let's create an exact padding scenario:
        // "a" -> YQ==
        // "ab" -> YWI=
        // "abc" -> YWJj
        
        var raw1 = "a";
        var encoded1 = _sut.Encode(raw1);
        encoded1.Should().Be("YQ"); // Removed padding
        _sut.Decode(encoded1).Should().Be(raw1);
        
        var raw2 = "ab";
        var encoded2 = _sut.Encode(raw2);
        encoded2.Should().Be("YWI");
        _sut.Decode(encoded2).Should().Be(raw2);
    }

    [Fact]
    public void Decode_InvalidBase64_ThrowsInvalidPaginationCursorException()
    {
        // Normal base64 characters are OK, but here we provide something that is definitively invalid.
        // Base64 requires valid characters. We provide an invalid sequence or char.
        var act = () => _sut.Decode("???!");
        
        var ex = act.Should().Throw<InvalidPaginationCursorException>()
           .WithMessage("*Base64Url*tampered*")
           .And;
           
        ex.InnerException.Should().BeOfType<FormatException>()
           .Which.Message.Should().Match(m => m.Contains("status") || m.Contains("format"));
           
        ex.OpaqueCursor.Should().Be("???!");
    }

    [Fact]
    public void Decode_WithLargeInvalidString_ThrowsInvalidPaginationCursorException()
    {
        // 2000 chars should exceed 1024 bytes for UTF8 buffer and Base64 buffer
        var raw = new string('?', 2000); // Invalid characters to force exception
        
        var act = () => _sut.Decode(raw);
        
        var ex = act.Should().Throw<InvalidPaginationCursorException>()
           .WithMessage("*Base64Url*tampered*")
           .And;
           
        ex.InnerException.Should().BeOfType<FormatException>()
           .Which.Message.Should().Match(m => m.Contains("status") || m.Contains("format"));
           
        ex.OpaqueCursor.Should().Be(raw);
    }

    [Fact]
    public void EncodeAndDecode_WithLargeString_WithSpecialChars_RoundTripsSuccessfully()
    {
        // This string forces the UTF8 bytes to generate '+' and '/' in normal Base64,
        // which tests the URL-safe replacement branches.
        // It's also large enough to exceed 1024 bytes and test the rented array branches.
        var baseString = "\u00fb\u00fc\u00fd\u00fe\u00ff\u0100\u0101~?~";
        var raw = string.Empty;
        for (int i = 0; i < 200; i++)
        {
            raw += baseString;
        }

        var encoded = _sut.Encode(raw);
        
        encoded.Should().NotBeNullOrEmpty();
        encoded.Should().NotContain("+").And.NotContain("/"); // Must be URL safe

        var decoded = _sut.Decode(encoded);
        decoded.Should().Be(raw);
    }
}


