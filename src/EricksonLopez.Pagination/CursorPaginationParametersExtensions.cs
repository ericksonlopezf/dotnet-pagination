// Copyright © Erickson Lopez. MIT License.
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides extension methods for decoding cursors from <see cref="CursorPaginationParameters"/>.
/// </summary>

public static class CursorPaginationParametersExtensions
{
    /// <summary>
    /// Decodes and parses the forward pagination cursor into the specified value type.
    /// </summary>
    /// <typeparam name="TKey">The value type of the cursor key.</typeparam>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <param name="decoderRegistry">An optional registry for AOT-safe decoders.</param>
    /// <returns>The parsed key, or <see langword="null"/> if no forward cursor is present.</returns>
    /// <exception cref="InvalidPaginationCursorException">The cursor format is invalid or cannot be parsed</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    public static TKey? DecodeAfter<TKey>(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder = null, ICursorDecoderRegistry? decoderRegistry = null)
        where TKey : struct
    {
        return Decode<TKey>(parameters.After, cursorEncoder, decoderRegistry);
    }

    /// <summary>
    /// Decodes and parses the backward pagination cursor into the specified value type.
    /// </summary>
    /// <typeparam name="TKey">The value type of the cursor key.</typeparam>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <param name="decoderRegistry">An optional registry for AOT-safe decoders.</param>
    /// <returns>The parsed key, or <see langword="null"/> if no backward cursor is present.</returns>
    /// <exception cref="InvalidPaginationCursorException">The cursor format is invalid or cannot be parsed</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    public static TKey? DecodeBefore<TKey>(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder = null, ICursorDecoderRegistry? decoderRegistry = null)
        where TKey : struct
    {
        return Decode<TKey>(parameters.Before, cursorEncoder, decoderRegistry);
    }

    /// <summary>
    /// Decodes the forward pagination cursor into a plain string representation.
    /// </summary>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <returns>The decoded plain string, or <see langword="null"/> if no forward cursor is present.</returns>
    public static string? DecodeAfterString(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder = null)
    {
        if (string.IsNullOrEmpty(parameters.After)) return null;
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        return encoder.Decode(parameters.After);
    }

    /// <summary>
    /// Decodes the backward pagination cursor into a plain string representation.
    /// </summary>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <returns>The decoded plain string, or <see langword="null"/> if no backward cursor is present.</returns>
    public static string? DecodeBeforeString(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder = null)
    {
        if (string.IsNullOrEmpty(parameters.Before)) return null;
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        return encoder.Decode(parameters.Before);
    }

    /// <summary>
    /// Decodes and parses the forward pagination cursor into the specified reference type.
    /// </summary>
    /// <typeparam name="TKey">The reference type of the cursor key.</typeparam>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <param name="decoderRegistry">An optional registry for AOT-safe decoders.</param>
    /// <returns>The parsed key instance, or <see langword="null"/> if no forward cursor is present.</returns>
    /// <exception cref="InvalidPaginationCursorException">The cursor format is invalid or cannot be parsed</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    public static TKey? DecodeAfterReference<TKey>(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder = null, ICursorDecoderRegistry? decoderRegistry = null)
        where TKey : class
    {
        return DecodeReference<TKey>(parameters.After, cursorEncoder, decoderRegistry);
    }

    /// <summary>
    /// Decodes and parses the backward pagination cursor into the specified reference type.
    /// </summary>
    /// <typeparam name="TKey">The reference type of the cursor key.</typeparam>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <param name="decoderRegistry">An optional registry for AOT-safe decoders.</param>
    /// <returns>The parsed key instance, or <see langword="null"/> if no backward cursor is present.</returns>
    /// <exception cref="InvalidPaginationCursorException">The cursor format is invalid or cannot be parsed</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    public static TKey? DecodeBeforeReference<TKey>(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder = null, ICursorDecoderRegistry? decoderRegistry = null)
        where TKey : class
    {
        return DecodeReference<TKey>(parameters.Before, cursorEncoder, decoderRegistry);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarLint", "S4144", Justification = "Public API compatibility")]
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    private static TKey? DecodeReference<TKey>(string? opaqueCursor, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry) where TKey : class
    {
        if (TryDecode<TKey>(opaqueCursor, out var value, cursorEncoder, decoderRegistry))
        {
            return value;
        }
        return null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarLint", "S4144", Justification = "Public API compatibility")]
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    private static TKey? Decode<TKey>(string? opaqueCursor, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry) where TKey : struct
    {
        if (TryDecode<TKey>(opaqueCursor, out var value, cursorEncoder, decoderRegistry))
        {
            return value;
        }
        return null;
    }

    /// <summary>
    /// Attempts to decode and parse the forward pagination cursor into the specified key type.
    /// </summary>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="value">When this method returns, contains the parsed key if decoding succeeded; otherwise, the default value.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <param name="decoderRegistry">An optional registry for AOT-safe decoders.</param>
    /// <returns><see langword="true"/> if the cursor was successfully decoded and parsed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidPaginationCursorException">The cursor format is invalid or cannot be parsed</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    public static bool TryDecodeAfter<TKey>(this CursorPaginationParameters parameters, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TKey? value, ICursorEncoder? cursorEncoder = null, ICursorDecoderRegistry? decoderRegistry = null)
    {
        return TryDecode<TKey>(parameters.After, out value, cursorEncoder, decoderRegistry);
    }

    /// <summary>
    /// Attempts to decode and parse the backward pagination cursor into the specified key type.
    /// </summary>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="parameters">The cursor pagination parameters containing the cursor.</param>
    /// <param name="value">When this method returns, contains the parsed key if decoding succeeded; otherwise, the default value.</param>
    /// <param name="cursorEncoder">An optional cursor encoder. If <see langword="null"/>, the default encoder is used.</param>
    /// <param name="decoderRegistry">An optional registry for AOT-safe decoders.</param>
    /// <returns><see langword="true"/> if the cursor was successfully decoded and parsed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidPaginationCursorException">The cursor format is invalid or cannot be parsed</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    public static bool TryDecodeBefore<TKey>(this CursorPaginationParameters parameters, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TKey? value, ICursorEncoder? cursorEncoder = null, ICursorDecoderRegistry? decoderRegistry = null)
    {
        return TryDecode<TKey>(parameters.Before, out value, cursorEncoder, decoderRegistry);
    }

    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("This method uses Convert.ChangeType as a fallback, which is incompatible with AOT. Use the source generator to register decoders.")]
    private static bool TryDecode<TKey>(string? opaqueCursor, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TKey? value, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry)
    {
        if (string.IsNullOrWhiteSpace(opaqueCursor))
        {
            value = default;
            return false;
        }

        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        var decoded = encoder.Decode(opaqueCursor);

        if (string.IsNullOrEmpty(decoded))
        {
            throw new InvalidPaginationCursorException("Decoded cursor is empty.", opaqueCursor);
        }

        if (decoded.StartsWith("M|"))
        {
            throw new InvalidPaginationCursorException("Cursor format is invalid. Expected a single-column keyset cursor, but received a multi-column cursor.", opaqueCursor);
        }

        if (decoded.StartsWith("S|"))
        {
            decoded = decoded.Substring(2);
        }

        // Stryker disable once logical : equivalent mutant; if TryGetDecoder returns false, decoder is null, so || decoder != null evaluates to false.
        if (decoderRegistry?.TryGetDecoder<TKey>(out var decoder) == true && decoder != null)
        {
            value = decoder(decoded)!;
            return true;
        }

        if (typeof(TKey) == typeof(string))
        {
            value = Unsafe.As<string, TKey>(ref decoded)!;
            return true;
        }

        try
        {
            // Stryker disable all : Fast path optimizations, fallback behaves identically for basic valid inputs.
            if (typeof(TKey) == typeof(int) && int.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var i))
            {
                value = Unsafe.As<int, TKey>(ref i)!;
                return true;
            }

            if (typeof(TKey) == typeof(long) && long.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var l))
            {
                value = Unsafe.As<long, TKey>(ref l)!;
                return true;
            }

            if (typeof(TKey) == typeof(short) && short.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var s))
            {
                value = Unsafe.As<short, TKey>(ref s)!;
                return true;
            }

            if (typeof(TKey) == typeof(uint) && uint.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var ui))
            {
                value = Unsafe.As<uint, TKey>(ref ui)!;
                return true;
            }

            if (typeof(TKey) == typeof(ulong) && ulong.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var ul))
            {
                value = Unsafe.As<ulong, TKey>(ref ul)!;
                return true;
            }

            if (typeof(TKey) == typeof(byte) && byte.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var b))
            {
                value = Unsafe.As<byte, TKey>(ref b)!;
                return true;
            }

            if (typeof(TKey) == typeof(float) && float.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var f))
            {
                value = Unsafe.As<float, TKey>(ref f)!;
                return true;
            }

            if (typeof(TKey) == typeof(double) && double.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var dbl))
            {
                value = Unsafe.As<double, TKey>(ref dbl)!;
                return true;
            }

            if (typeof(TKey) == typeof(decimal) && decimal.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, out var dec))
            {
                value = Unsafe.As<decimal, TKey>(ref dec)!;
                return true;
            }
            // Stryker restore all

            if (typeof(TKey) == typeof(Guid) && Guid.TryParse(decoded, out var g))
            {
                value = Unsafe.As<Guid, TKey>(ref g)!;
                return true;
            }

            if (typeof(TKey) == typeof(DateTimeOffset) && DateTimeOffset.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dto))
            {
                value = Unsafe.As<DateTimeOffset, TKey>(ref dto)!;
                return true;
            }

#if NET6_0_OR_GREATER
            if (typeof(TKey) == typeof(DateOnly) && DateOnly.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateOnly))
            {
                value = Unsafe.As<DateOnly, TKey>(ref dateOnly)!;
                return true;
            }

            if (typeof(TKey) == typeof(TimeOnly) && TimeOnly.TryParse(decoded, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var timeOnly))
            {
                value = Unsafe.As<TimeOnly, TKey>(ref timeOnly)!;
                return true;
            }
#endif
            
            value = (TKey)Convert.ChangeType(decoded, typeof(TKey), System.Globalization.CultureInfo.InvariantCulture)!;
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidPaginationCursorException($"Failed to parse decoded cursor '{decoded}' to type {typeof(TKey).Name}.", opaqueCursor, ex);
        }
    }
}

