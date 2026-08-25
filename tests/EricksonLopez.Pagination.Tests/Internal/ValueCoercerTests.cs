// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination.Internal;
using Xunit;

namespace EricksonLopez.Pagination.Tests.Internal;

public class ValueCoercerTests
{
    private enum TestEnum { A, B }
    
    [Theory]
    [InlineData("True", typeof(bool), true)]
    [InlineData("123", typeof(int), 123)]
    [InlineData("1234567890", typeof(long), 1234567890L)]
    [InlineData("12.34", typeof(double), 12.34d)]
    [InlineData("12.34", typeof(float), 12.34f)]
    [InlineData("12.34", typeof(decimal), 12.34)]
    public void TryCoerce_BasicTypes_ReturnsTrue(string value, Type type, object expected)
    {
        // For decimal, the expected type in attribute might be boxed as double, so we need a workaround
        if (type == typeof(decimal)) expected = 12.34m;
        
        var result = ValueCoercer.TryCoerce(value, type, out var coerced);
        
        result.Should().BeTrue();
        coerced.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void TryCoerce_String_ReturnsTrue()
    {
        var result = ValueCoercer.TryCoerce("Test", typeof(string), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be("Test");
    }

    [Fact]
    public void TryCoerce_Enum_ReturnsTrue()
    {
        var result = ValueCoercer.TryCoerce("B", typeof(TestEnum), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be(TestEnum.B);
    }
    
    [Fact]
    public void TryCoerce_InvalidEnum_ReturnsFalse()
    {
        var result = ValueCoercer.TryCoerce("Invalid", typeof(TestEnum), out var coerced);
        result.Should().BeFalse();
        coerced.Should().BeNull();
    }

    [Fact]
    public void TryCoerce_Guid_ReturnsTrue()
    {
        var guid = Guid.NewGuid();
        var result = ValueCoercer.TryCoerce(guid.ToString(), typeof(Guid), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be(guid);
    }
    
    [Fact]
    public void TryCoerce_InvalidGuid_ReturnsFalse()
    {
        var result = ValueCoercer.TryCoerce("invalid-guid", typeof(Guid), out var coerced);
        result.Should().BeFalse();
        coerced.Should().BeNull();
    }

    [Fact]
    public void TryCoerce_DateTime_ReturnsTrue()
    {
        var dt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = ValueCoercer.TryCoerce(dt.ToString("O"), typeof(DateTime), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be(dt);
    }
    
    [Fact]
    public void TryCoerce_DateTimeOffset_ReturnsTrue()
    {
        var dto = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = ValueCoercer.TryCoerce(dto.ToString("O"), typeof(DateTimeOffset), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be(dto);
    }
    
    [Fact]
    public void TryCoerce_TimeSpan_ReturnsTrue()
    {
        var ts = TimeSpan.FromHours(1);
        var result = ValueCoercer.TryCoerce(ts.ToString(), typeof(TimeSpan), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be(ts);
    }
    
#if NET6_0_OR_GREATER
    [Fact]
    public void TryCoerce_DateOnly_ReturnsTrue()
    {
        var date = new DateOnly(2023, 1, 1);
        var result = ValueCoercer.TryCoerce(date.ToString("O"), typeof(DateOnly), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be(date);
    }
#endif

    [Fact]
    public void TryCoerce_ConvertibleType_ReturnsTrue()
    {
        var result = ValueCoercer.TryCoerce("12", typeof(short), out var coerced);
        result.Should().BeTrue();
        coerced.Should().Be((short)12);
    }

    [Fact]
    public void TryCoerce_InvalidConvertibleType_ReturnsFalse()
    {
        var result = ValueCoercer.TryCoerce("not-a-number", typeof(int), out var coerced);
        result.Should().BeFalse();
        coerced.Should().BeNull();
    }
}


