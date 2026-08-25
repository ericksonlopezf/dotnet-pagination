// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class CursorDecoderRegistryTests
{
    private struct CustomType {}

    [Fact]
    public void RegisterAndTryGetDecoder_Works()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        Func<string, CustomType> expectedDecoder = s => new CustomType();
        registry.Register(expectedDecoder);

        var result = registry.TryGetDecoder<CustomType>(out var actualDecoder);

        result.Should().BeTrue();
        actualDecoder.Should().BeSameAs(expectedDecoder);
    }

    [Fact]
    public void TryGetDecoder_WhenNotRegistered_ReturnsFalse()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        var result = registry.TryGetDecoder<DateTime>(out var decoder);
        result.Should().BeFalse();
        decoder.Should().BeNull();
    }
}



