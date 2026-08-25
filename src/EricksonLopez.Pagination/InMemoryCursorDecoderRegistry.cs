// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Threading;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides an in-memory, thread-safe implementation of <see cref="ICursorDecoderRegistry"/>.
/// </summary>
/// <remarks>
/// This type is thread-safe and suitable for singleton lifetime registration.
/// </remarks>
public sealed class InMemoryCursorDecoderRegistry : ICursorDecoderRegistry
{
    private readonly ConcurrentDictionary<Type, Delegate> _decoders = new();

    /// <inheritdoc/>
    public void Register<TKey>(Func<string, TKey> decoder)
    {
        _decoders[typeof(TKey)] = decoder;
    }

    /// <inheritdoc/>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(Func<,>))]
#endif
    public bool TryGetDecoder<TKey>(out Func<string, TKey>? decoder)
    {
        if (_decoders.TryGetValue(typeof(TKey), out var del) && del is Func<string, TKey> typed)
        {
            decoder = typed;
            return true;
        }
        
        decoder = null;
        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _decoders.Clear();
    }

    /// <inheritdoc/>
    public bool Unregister<TKey>()
    {
        return _decoders.TryRemove(typeof(TKey), out _);
    }
}


