// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class RawCursorValueTests
{
    [Fact]
    public void Constructor_ShouldSetValue()
    {
        var val = "my-cursor-value";
        var raw = new RawCursorValue(val);
        raw.Value.Should().Be(val);
    }

    [Fact]
    public void Constructor_ShouldAllowNullValue()
    {
        var raw = new RawCursorValue(null!);
        raw.Value.Should().BeNull();
    }

    [Fact]
    public void DefaultConstructor_ShouldHaveNullValue()
    {
        var raw = default(RawCursorValue);
        raw.Value.Should().BeNull();
    }

    [Fact]
    public void GetHashCode_WhenValueIsNotNull_ReturnsOrdinalHashCode()
    {
        var raw = new RawCursorValue("test");
        raw.GetHashCode().Should().Be(System.StringComparer.Ordinal.GetHashCode("test"));
    }

    [Fact]
    public void GetHashCode_WhenValueIsNull_ReturnsZero()
    {
        var raw = new RawCursorValue(null!);
        raw.GetHashCode().Should().Be(0);

        var defaultRaw = default(RawCursorValue);
        defaultRaw.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void Equals_ShouldReturnTrueForSameValue()
    {
        var raw1 = new RawCursorValue("test");
        var raw2 = new RawCursorValue("test");
        raw1.Equals(raw2).Should().BeTrue();
        (raw1 == raw2).Should().BeTrue();
        (raw1 != raw2).Should().BeFalse();
        raw1.GetHashCode().Should().Be(raw2.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentValue()
    {
        var raw1 = new RawCursorValue("test1");
        var raw2 = new RawCursorValue("test2");
        raw1.Equals(raw2).Should().BeFalse();
        (raw1 == raw2).Should().BeFalse();
        (raw1 != raw2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_WorksCorrectly()
    {
        var raw = new RawCursorValue("test");
        raw.Equals((object)new RawCursorValue("test")).Should().BeTrue();
        raw.Equals((object)new RawCursorValue("other")).Should().BeFalse();
        raw.Equals((object)"test").Should().BeFalse();
        raw.Equals(null).Should().BeFalse();
    }
}
