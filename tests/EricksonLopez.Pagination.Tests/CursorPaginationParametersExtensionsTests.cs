// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.Tests.Builders;
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CursorPaginationParametersExtensionsTests
{
    private sealed class MockCursorEncoder : ICursorEncoder
    {
        public Func<string, string> DecodeFunc { get; set; } = s => $"decoded_{s}";
        public Func<string, string> EncodeFunc { get; set; } = s => $"encoded_{s}";
        public string? Encode(string? rawCursor) => rawCursor is null ? null : EncodeFunc(rawCursor);
        public string? Decode(string? opaqueCursor) 
        {
            if (string.IsNullOrEmpty(opaqueCursor))
                throw new ArgumentException("Should not reach encoder with null or empty cursor.");
            return DecodeFunc(opaqueCursor);
        }
    }

    private readonly MockCursorEncoder _encoder = new();

    [Fact]
    public void DecodeAfter_WithNullOrEmpty_ReturnsNull()
    {
        var p = new CursorPaginationParametersBuilder().WithAfter(null).Build();
        p.DecodeAfter<int>(_encoder).Should().BeNull();

        p = new CursorPaginationParametersBuilder().WithAfter(string.Empty).Build();
        p.DecodeAfter<int>(_encoder).Should().BeNull();
    }

    [Fact]
    public void DecodeBefore_WithNullOrEmpty_ReturnsNull()
    {
        var p = new CursorPaginationParametersBuilder().WithBefore(null).Build();
        p.DecodeBefore<int>(_encoder).Should().BeNull();

        p = new CursorPaginationParametersBuilder().WithBefore(string.Empty).Build();
        p.DecodeBefore<int>(_encoder).Should().BeNull();
    }

    [Fact]
    public void DecodeAfterString_WithNullOrEmpty_ReturnsNull()
    {
        var p = new CursorPaginationParametersBuilder().WithAfter(null).Build();
        p.DecodeAfterString(_encoder).Should().BeNull();

        p = new CursorPaginationParametersBuilder().WithAfter(string.Empty).Build();
        p.DecodeAfterString(_encoder).Should().BeNull();
    }

    [Fact]
    public void DecodeBeforeString_WithNullOrEmpty_ReturnsNull()
    {
        var p = new CursorPaginationParametersBuilder().WithBefore(null).Build();
        p.DecodeBeforeString(_encoder).Should().BeNull();

        p = new CursorPaginationParametersBuilder().WithBefore(string.Empty).Build();
        p.DecodeBeforeString(_encoder).Should().BeNull();
    }

    [Fact]
    public void DecodeAfterString_WithCustomEncoder_UsesEncoder()
    {
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque1").Build();
        var result = p.DecodeAfterString(_encoder);
        result.Should().Be("decoded_opaque1");
    }

    [Fact]
    public void DecodeBeforeString_WithCustomEncoder_UsesEncoder()
    {
        var p = new CursorPaginationParametersBuilder().WithBefore("opaque2").Build();
        var result = p.DecodeBeforeString(_encoder);
        result.Should().Be("decoded_opaque2");
    }

    [Fact]
    public void DecodeAfter_WithCustomEncoderAndEmptyDecodedString_ThrowsInvalidPaginationCursorException()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => string.Empty };
        
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        
        var act = () => p.DecodeAfter<int>(encoder);
        act.Should().Throw<InvalidPaginationCursorException>().WithMessage("Decoded cursor is empty.");
    }
    
    private struct CustomType { public int Value; }

    [Fact]
    public void DecodeAfter_WithCustomRegistry_UsesRegistry()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register(s => new CustomType { Value = s.Length });
        
        var p = new CursorPaginationParametersBuilder().WithAfter("abc").Build(); // base64 decoded string
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "resolved" };
        
        var result = p.DecodeAfter<CustomType>(encoder, registry);
        result.Should().Be(new CustomType { Value = "resolved".Length });
    }

    [Fact]
    public void DecodeAfter_WithConvertChangeType_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "A" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        
        var result = p.DecodeAfter<char>(encoder);
        result.Should().Be('A');
    }

    [Fact]
    public void DecodeAfter_WithConvertChangeTypeFailing_ThrowsInvalidPaginationCursorException()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "not-a-number" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        
        var act = () => p.DecodeAfter<double>(encoder);
        act.Should().Throw<InvalidPaginationCursorException>()
            .WithMessage("Failed to parse decoded cursor 'not-a-number' to type Double.");
    }
    
    [Fact]
    public void DecodeBefore_WithConvertChangeTypeFailing_ThrowsInvalidPaginationCursorException()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "not-a-guid" };
        var p = new CursorPaginationParametersBuilder().WithBefore("opaque").Build();
        
        var act = () => p.DecodeBefore<Guid>(encoder);
        act.Should().Throw<InvalidPaginationCursorException>();
    }

    [Fact]
    public void DecodeAfter_WithInt_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "42" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<int>(encoder).Should().Be(42);
    }

    [Fact]
    public void DecodeAfter_WithLong_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "424242424242" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<long>(encoder).Should().Be(424242424242L);
    }

    [Fact]
    public void DecodeAfter_WithDouble_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "42.5" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<double>(encoder).Should().Be(42.5);
    }

    [Fact]
    public void DecodeAfter_WithGuid_Works()
    {
        var guid = Guid.NewGuid();
        var encoder = new MockCursorEncoder { DecodeFunc = _ => guid.ToString() };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<Guid>(encoder).Should().Be(guid);
    }

    [Fact]
    public void DecodeAfter_WithDateTimeOffset_Works()
    {
        var dt = DateTimeOffset.UtcNow;
        var encoder = new MockCursorEncoder { DecodeFunc = _ => dt.ToString("O", System.Globalization.CultureInfo.InvariantCulture) };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<DateTimeOffset>(encoder).Should().Be(dt);
    }
    
    [Fact]
    public void DecodeAfterString_WithNullEncoder_UsesDefaultEncoder()
    {
        var p = new CursorPaginationParametersBuilder().WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("test")).Build();
        p.DecodeAfterString().Should().Be("test");
    }

    [Fact]
    public void DecodeBeforeString_WithNullEncoder_UsesDefaultEncoder()
    {
        var p = new CursorPaginationParametersBuilder().WithBefore(HmacCursorEncoder.DevelopmentDefault.Encode("test2")).Build();
        p.DecodeBeforeString().Should().Be("test2");
    }
    
    [Fact]
    public void DecodeAfter_WithNullEncoder_UsesDefaultEncoder()
    {
        var p = new CursorPaginationParametersBuilder().WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("123")).Build();
        p.DecodeAfter<int>().Should().Be(123);
    }

    [Fact]
    public void DecodeBefore_WithNullEncoder_UsesDefaultEncoder()
    {
        var p = new CursorPaginationParametersBuilder().WithBefore(HmacCursorEncoder.DevelopmentDefault.Encode("456")).Build();
        p.DecodeBefore<int>().Should().Be(456);
    }

    [Fact]
    public void TryDecode_WithMultiColumnCursor_ThrowsInvalidPaginationCursorException()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "M|value1|value2" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        
        var act = () => p.DecodeAfter<int>(encoder);
        act.Should().Throw<InvalidPaginationCursorException>()
           .WithMessage("Cursor format is invalid. Expected a single-column keyset cursor, but received a multi-column cursor.");
    }

    [Fact]
    public void TryDecode_WithSingleColumnPrefix_DecodesProperly()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "S|123" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        
        p.DecodeAfter<int>(encoder).Should().Be(123);
    }

    [Fact]
    public void TryDecode_StringType_ReturnsDirectly()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "my_string" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        
        p.DecodeAfterReference<string>(encoder).Should().Be("my_string");
    }

    [Fact]
    public void DecodeBeforeReference_WithValidCursor_ReturnsValue()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "my_string2" };
        var p = new CursorPaginationParametersBuilder().WithBefore("opaque").Build();
        
        p.DecodeBeforeReference<string>(encoder).Should().Be("my_string2");
    }

    [Fact]
    public void DecodeAfter_WithShort_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "12" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<short>(encoder).Should().Be(12);
    }

    [Fact]
    public void DecodeAfter_WithUInt_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "123" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<uint>(encoder).Should().Be(123u);
    }

    [Fact]
    public void DecodeAfter_WithULong_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "1234567890" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<ulong>(encoder).Should().Be(1234567890ul);
    }

    [Fact]
    public void DecodeAfter_WithByte_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "12" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<byte>(encoder).Should().Be(12);
    }

    [Fact]
    public void DecodeAfter_WithFloat_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "12.34" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<float>(encoder).Should().Be(12.34f);
    }

    [Fact]
    public void DecodeBefore_WithDouble_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "987.654321" };
        var p = new CursorPaginationParametersBuilder().WithBefore("opaque").Build();
        p.DecodeBefore<double>(encoder).Should().Be(987.654321);
    }

    [Fact]
    public void DecodeAfter_WithDecimal_Works()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "12.34567890123" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<decimal>(encoder).Should().Be(12.34567890123m);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void DecodeAfter_WithDateOnly_Works()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var encoder = new MockCursorEncoder { DecodeFunc = _ => date.ToString("O", System.Globalization.CultureInfo.InvariantCulture) };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<DateOnly>(encoder).Should().Be(date);
    }

    [Fact]
    public void DecodeAfter_WithTimeOnly_Works()
    {
        var time = TimeOnly.FromDateTime(DateTime.UtcNow);
        var encoder = new MockCursorEncoder { DecodeFunc = _ => time.ToString("O", System.Globalization.CultureInfo.InvariantCulture) };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<TimeOnly>(encoder).Should().Be(time);
    }
#endif

    [Fact]
    public void DecodeAfterReference_WithEmptyCursor_ReturnsNull()
    {
        var p = new CursorPaginationParametersBuilder().WithAfter(null).Build();
        p.DecodeAfterReference<string>(_encoder).Should().BeNull();
    }

    [Fact]
    public void DecodeBeforeReference_WithEmptyCursor_ReturnsNull()
    {
        var p = new CursorPaginationParametersBuilder().WithBefore(null).Build();
        p.DecodeBeforeReference<string>(_encoder).Should().BeNull();
    }

    [Fact]
    public void TryDecodeAfter_WithValidCursor_ReturnsTrueAndValue()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "123" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        
        var result = p.TryDecodeAfter<int>(out var value, encoder);
        result.Should().BeTrue();
        value.Should().Be(123);
    }

    [Fact]
    public void TryDecodeAfter_WithEmptyCursor_ReturnsFalse()
    {
        var p = new CursorPaginationParametersBuilder().WithAfter(null).Build();
        var result = p.TryDecodeAfter<int>(out var value, _encoder);
        result.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryDecodeBefore_WithValidCursor_ReturnsTrueAndValue()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "456" };
        var p = new CursorPaginationParametersBuilder().WithBefore("opaque").Build();
        
        var result = p.TryDecodeBefore<int>(out var value, encoder);
        result.Should().BeTrue();
        value.Should().Be(456);
    }

    [Fact]
    public void TryDecodeBefore_WithEmptyCursor_ReturnsFalse()
    {
        var p = new CursorPaginationParametersBuilder().WithBefore(null).Build();
        var result = p.TryDecodeBefore<int>(out var value, _encoder);
        result.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void DecodeAfter_WithSingleColumnPrefix_StripsPrefixAndDecodes()
    {
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "S|987" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();
        p.DecodeAfter<int>(encoder).Should().Be(987);
    }

    [Fact]
    public void DecodeAfter_WithDefaultEncoder_DecodesProperly()
    {
        var validBase64 = HmacCursorEncoder.DevelopmentDefault.Encode("42");
        var p = new CursorPaginationParametersBuilder().WithAfter(validBase64).Build();
        p.DecodeAfter<int>().Should().Be(42);
    }

    [Fact]
    public void DecodeBefore_WithDefaultEncoder_DecodesProperly()
    {
        var validBase64 = HmacCursorEncoder.DevelopmentDefault.Encode("100");
        var p = new CursorPaginationParametersBuilder().WithBefore(validBase64).Build();
        p.DecodeBefore<int>().Should().Be(100);
    }

    [Fact]
    public void DecodeAfterString_WithDefaultEncoder_DecodesProperly()
    {
        var validBase64 = HmacCursorEncoder.DevelopmentDefault.Encode("hello");
        var p = new CursorPaginationParametersBuilder().WithAfter(validBase64).Build();
        p.DecodeAfterString().Should().Be("hello");
    }

    [Fact]
    public void DecodeBeforeString_WithDefaultEncoder_DecodesProperly()
    {
        var validBase64 = HmacCursorEncoder.DevelopmentDefault.Encode("world");
        var p = new CursorPaginationParametersBuilder().WithBefore(validBase64).Build();
        p.DecodeBeforeString().Should().Be("world");
    }

    private class CustomRefType
    {
        public string? Name { get; set; }
    }

    [Fact]
    public void DecodeAfterReference_WithCustomRegistry_DecodesProperly()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register(s => new CustomRefType { Name = s });
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "alice" };
        var p = new CursorPaginationParametersBuilder().WithAfter("opaque").Build();

        var result = p.DecodeAfterReference<CustomRefType>(encoder, registry);
        result.Should().NotBeNull();
        result!.Name.Should().Be("alice");
    }

    [Fact]
    public void DecodeBeforeReference_WithCustomRegistry_DecodesProperly()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register(s => new CustomRefType { Name = s });
        var encoder = new MockCursorEncoder { DecodeFunc = _ => "bob" };
        var p = new CursorPaginationParametersBuilder().WithBefore("opaque").Build();

        var result = p.DecodeBeforeReference<CustomRefType>(encoder, registry);
        result.Should().NotBeNull();
        result!.Name.Should().Be("bob");
    }
}



