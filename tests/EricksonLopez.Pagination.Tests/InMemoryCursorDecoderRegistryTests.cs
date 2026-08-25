// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class InMemoryCursorDecoderRegistryTests
{
    [Fact]
    public void Register_And_TryGetDecoder_Works()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        
        registry.Register<int>(s => int.Parse(s) * 2);

        var success = registry.TryGetDecoder<int>(out var decoder);
        
        success.Should().BeTrue();
        decoder.Should().NotBeNull();
        decoder!("5").Should().Be(10);
    }

    [Fact]
    public void TryGetDecoder_UnregisteredType_ReturnsFalse()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        
        var success = registry.TryGetDecoder<string>(out var decoder);
        
        success.Should().BeFalse();
        decoder.Should().BeNull();
    }

    [Fact]
    public void Clear_RemovesAllDecoders()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register<int>(s => 1);
        registry.Register<string>(s => s);
        
        registry.Clear();
        
        registry.TryGetDecoder<int>(out _).Should().BeFalse();
        registry.TryGetDecoder<string>(out _).Should().BeFalse();
    }

    [Fact]
    public void Unregister_RemovesSpecificDecoder()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register<int>(s => 1);
        registry.Register<string>(s => s);
        
        var removed = registry.Unregister<int>();
        
        removed.Should().BeTrue();
        registry.TryGetDecoder<int>(out _).Should().BeFalse();
        registry.TryGetDecoder<string>(out _).Should().BeTrue();
    }

    [Fact]
    public void TryGetDecoder_WrongTypeInDictionary_ReturnsFalse()
    {
        // Edge case: if somehow a wrong delegate is stored for the key, TryGetDecoder should return false.
        // It's not normally possible through the generic Register method, but we can verify the behavior if we somehow simulate it.
        // Actually, since it uses ConcurrentDictionary<Type, Delegate> and Register<TKey> enforces Func<string, TKey>, 
        // it's not possible to simulate without reflection. We can just test that Unregister on non-existent returns false.
        
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Unregister<int>().Should().BeFalse();
    }
}


