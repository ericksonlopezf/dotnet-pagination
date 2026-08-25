// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a registry for storing per-type cursor decoder delegates to enable trimming- and AOT-safe cursor decoding.
/// </summary>
public interface ICursorDecoderRegistry
{
    /// <summary>
    /// Attempts to retrieve a registered cursor decoder for the specified key type.
    /// </summary>
    /// <typeparam name="TKey">The cursor key type to decode.</typeparam>
    /// <param name="decoder">
    /// When this method returns, contains the registered decoder function if found;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a decoder for <typeparamref name="TKey"/> is registered;
    /// otherwise, <see langword="false"/>.
    /// </returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(Func<,>))]
#endif
    bool TryGetDecoder<TKey>(out Func<string, TKey>? decoder);

    /// <summary>
    /// Registers a cursor decoder delegate for the specified key type.
    /// </summary>
    /// <typeparam name="TKey">The cursor key type handled by this decoder.</typeparam>
    /// <param name="decoder">The function that converts an opaque cursor string to a <typeparamref name="TKey"/> value.</param>
    void Register<TKey>(Func<string, TKey> decoder);

    /// <summary>
    /// Removes all registered decoders from the registry.
    /// </summary>
    void Clear();

    /// <summary>
    /// Removes the registered cursor decoder for the specified key type.
    /// </summary>
    /// <typeparam name="TKey">The cursor key type whose decoder should be removed.</typeparam>
    /// <returns>
    /// <see langword="true"/> if the decoder was found and removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool Unregister<TKey>();
}

