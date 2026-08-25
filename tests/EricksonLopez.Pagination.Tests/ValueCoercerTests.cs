// Copyright © Erickson Lopez. MIT License.
#pragma warning disable CA1034, CA1008, CA1815, CA1711, CA1305, S125, S1128
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Internal;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class ValueCoercerTests
{
    [Theory]
    [InlineData("123", typeof(int), 123)]
    [InlineData("123", typeof(int?), 123)]
    [InlineData("123", typeof(long), 123L)]
    [InlineData("123", typeof(long?), 123L)]
    [InlineData("123.45", typeof(double), 123.45)]
    [InlineData("123.45", typeof(double?), 123.45)]
    [InlineData("123.45", typeof(float), 123.45f)]
    [InlineData("123.45", typeof(float?), 123.45f)]
    [InlineData("123.45", typeof(decimal), 123.45)]
    [InlineData("123.45", typeof(decimal?), 123.45)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("false", typeof(bool?), false)]
    [InlineData("string value", typeof(string), "string value")]
    public void TryCoerce_ValidValues_ReturnsTrue(string value, Type type, object expected)
    {
        var result = ValueCoercer.TryCoerce(value, type, out var parsed);
        result.Should().BeTrue();
        
        // Use object.Equals or similar because decimal and double might have type mismatches in InlineData
        if (type == typeof(decimal) || type == typeof(decimal?))
        {
            parsed.Should().Be(Convert.ToDecimal(expected));
        }
        else
        {
            parsed.Should().Be(expected);
        }
    }

    [Fact]
    public void TryCoerce_Guid_ReturnsTrue()
    {
        var guid = Guid.NewGuid();
        var result = ValueCoercer.TryCoerce(guid.ToString(), typeof(Guid), out var parsed);
        result.Should().BeTrue();
        parsed.Should().Be(guid);
        
        var resultNullable = ValueCoercer.TryCoerce(guid.ToString(), typeof(Guid?), out var parsedNullable);
        resultNullable.Should().BeTrue();
        parsedNullable.Should().Be(guid);
    }

    [Fact]
    public void TryCoerce_DateTime_ReturnsTrue()
    {
        var dt = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var result = ValueCoercer.TryCoerce(dt.ToString("O"), typeof(DateTime), out var parsed);
        result.Should().BeTrue();
        parsed.Should().Be(dt);
    }
    
    [Fact]
    public void TryCoerce_DateTimeOffset_ReturnsTrue()
    {
        var dt = new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var result = ValueCoercer.TryCoerce(dt.ToString("O"), typeof(DateTimeOffset), out var parsed);
        result.Should().BeTrue();
        parsed.Should().Be(dt);
    }

    [Fact]
    public void TryCoerce_DateOnly_ReturnsTrue()
    {
        var dt = new DateOnly(2020, 1, 1);
        var result = ValueCoercer.TryCoerce(dt.ToString("O"), typeof(DateOnly), out var parsed);
        result.Should().BeTrue();
        parsed.Should().Be(dt);
    }

    [Fact]
    public void TryCoerce_TimeSpan_ReturnsTrue()
    {
        var ts = new TimeSpan(1, 2, 3);
        var result = ValueCoercer.TryCoerce(ts.ToString("c"), typeof(TimeSpan), out var parsed);
        result.Should().BeTrue();
        parsed.Should().Be(ts);
    }

    public enum TestEnum
    {
        First = 1,
        Second = 2
    }

    [Fact]
    public void TryCoerce_Enum_ReturnsTrue()
    {
        var result = ValueCoercer.TryCoerce("Second", typeof(TestEnum), out var parsed);
        result.Should().BeTrue();
        parsed.Should().Be(TestEnum.Second);
        
        var resultIgnore = ValueCoercer.TryCoerce("second", typeof(TestEnum), out var parsedIgnore);
        resultIgnore.Should().BeTrue();
        parsedIgnore.Should().Be(TestEnum.Second);
    }

    [Fact]
    public void TryCoerce_InvalidEnum_ReturnsFalse()
    {
        var result = ValueCoercer.TryCoerce("NotAnEnum", typeof(TestEnum), out var parsed);
        result.Should().BeFalse();
        parsed.Should().BeNull();
    }
    
    [Theory]
    [InlineData("not-an-int", typeof(int))]
    [InlineData("not-a-guid", typeof(Guid))]
    [InlineData("not-a-bool", typeof(bool))]
    [InlineData("not-a-date", typeof(DateTime))]
    [InlineData("not-a-date", typeof(DateTimeOffset))]
    [InlineData("not-a-date", typeof(DateOnly))]
    [InlineData("not-a-timespan", typeof(TimeSpan))]
    [InlineData("not-a-decimal", typeof(decimal))]
    [InlineData("not-a-float", typeof(float))]
    [InlineData("not-a-double", typeof(double))]
    [InlineData("not-a-long", typeof(long))]
    public void TryCoerce_InvalidValues_ReturnsFalse(string value, Type type)
    {
        var result = ValueCoercer.TryCoerce(value, type, out var parsed);
        result.Should().BeFalse();
        parsed.Should().BeNull();
    }

    [Fact]
    public void TryCoerce_IConvertibleFallback_ReturnsTrue()
    {
        // byte is IConvertible but not specifically checked in TryCoerce
        var result = ValueCoercer.TryCoerce("255", typeof(byte), out var parsed);
        result.Should().BeTrue();
        parsed.Should().Be((byte)255);
    }
    
    [Fact]
    public void TryCoerce_IConvertibleFallback_Invalid_ReturnsFalse()
    {
        var result = ValueCoercer.TryCoerce("not-a-byte", typeof(byte), out var parsed);
        result.Should().BeFalse();
        parsed.Should().BeNull();
    }

    public struct NonConvertibleStruct
    {
        public int Value { get; set; }
    }

    [Fact]
    public void TryCoerce_NonConvertible_ReturnsFalse()
    {
        var result = ValueCoercer.TryCoerce("123", typeof(NonConvertibleStruct), out var parsed);
        result.Should().BeFalse();
        parsed.Should().BeNull();
    }
}



