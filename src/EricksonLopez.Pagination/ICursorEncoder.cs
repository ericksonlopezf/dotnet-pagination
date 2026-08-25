// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Defines a contract for encoding and decoding opaque pagination cursors.
/// </summary>
public interface ICursorEncoder
{
    /// <summary>
    /// Encodes a raw cursor string into an opaque cursor string.
    /// </summary>
    /// <param name="rawCursor">The raw cursor value to encode.</param>
    /// <returns>The opaque, encoded cursor string, or <see langword="null"/> if the input is null or empty.</returns>
    string? Encode(string? rawCursor);

    /// <summary>
    /// Decodes an opaque cursor string back into its raw value.
    /// </summary>
    /// <param name="opaqueCursor">The opaque cursor string to decode.</param>
    /// <returns>The raw, decoded cursor value, or <see langword="null"/> if the input is null or empty.</returns>
    /// <exception cref="InvalidPaginationCursorException"><paramref name="opaqueCursor"/> is malformed or cannot be decoded</exception>
    string? Decode(string? opaqueCursor);
}

