// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Bson;
using Xunit;

namespace EricksonLopez.Pagination.MongoDB.Tests;

public class DefaultMongoCursorDecoderRegistryTests
{
    [Fact]
    public void Instance_ProvidesObjectIdDecoder()
    {
        var registry = DefaultMongoCursorDecoderRegistry.Instance;
        registry.Should().NotBeNull();

        var success = registry.TryGetDecoder<ObjectId>(out var decoder);
        success.Should().BeTrue();
        decoder.Should().NotBeNull();

        var oid = ObjectId.GenerateNewId();
        var decoded = decoder!(oid.ToString());
        decoded.Should().Be(oid);
    }

    [Fact]
    public void Instance_ProvidesGuidDecoder()
    {
        var registry = DefaultMongoCursorDecoderRegistry.Instance;
        var success = registry.TryGetDecoder<Guid>(out var decoder);
        success.Should().BeTrue();
        decoder.Should().NotBeNull();

        var guid = Guid.NewGuid();
        var decoded = decoder!(guid.ToString());
        decoded.Should().Be(guid);
    }

    [Fact]
    public void Instance_ProvidesDateTimeOffsetDecoder()
    {
        var registry = DefaultMongoCursorDecoderRegistry.Instance;
        var success = registry.TryGetDecoder<DateTimeOffset>(out var decoder);
        success.Should().BeTrue();
        decoder.Should().NotBeNull();

        var now = DateTimeOffset.UtcNow;
        var str = now.ToString("o", CultureInfo.InvariantCulture);
        var decoded = decoder!(str);
        decoded.Should().Be(DateTimeOffset.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    [Fact]
    public void Instance_ProvidesIntDecoder()
    {
        var registry = DefaultMongoCursorDecoderRegistry.Instance;
        var success = registry.TryGetDecoder<int>(out var decoder);
        success.Should().BeTrue();
        decoder.Should().NotBeNull();

        var decoded = decoder!("12345");
        decoded.Should().Be(12345);
    }

    [Fact]
    public void Instance_ProvidesLongDecoder()
    {
        var registry = DefaultMongoCursorDecoderRegistry.Instance;
        var success = registry.TryGetDecoder<long>(out var decoder);
        success.Should().BeTrue();
        decoder.Should().NotBeNull();

        var decoded = decoder!("9876543210");
        decoded.Should().Be(9876543210L);
    }

    [Fact]
    public void Instance_ProvidesStringDecoder()
    {
        var registry = DefaultMongoCursorDecoderRegistry.Instance;
        var success = registry.TryGetDecoder<string>(out var decoder);
        success.Should().BeTrue();
        decoder.Should().NotBeNull();

        var decoded = decoder!("hello-world");
        decoded.Should().Be("hello-world");
    }

    [Fact]
    public void GetEffectiveRegistry_WithCustomRegistry_ReturnsCustomRegistry()
    {
        var custom = new InMemoryCursorDecoderRegistry();
        var effective = DefaultMongoCursorDecoderRegistry.GetEffectiveRegistry(custom);
        effective.Should().BeSameAs(custom);
    }

    [Fact]
    public void GetEffectiveRegistry_WithNull_ReturnsDefaultInstance()
    {
        var effective = DefaultMongoCursorDecoderRegistry.GetEffectiveRegistry(null);
        effective.Should().BeSameAs(DefaultMongoCursorDecoderRegistry.Instance);
    }
}

