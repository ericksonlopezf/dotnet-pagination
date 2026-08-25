// Copyright © Erickson Lopez. MIT License.
using System;
using System.Buffers.Text;
using System.Text;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Encapsulates a Base64-based cursor encoder that transforms cursor strings into URL-safe Base64 representations.
/// </summary>
public sealed class Base64CursorEncoder : ICursorEncoder
{
    /// <summary>
    /// Gets the singleton default instance of <see cref="Base64CursorEncoder"/>.
    /// </summary>
    public static Base64CursorEncoder Default { get; } = new();

    /// <inheritdoc/>
    public string? Encode(string? rawCursor)
    {
        if (string.IsNullOrEmpty(rawCursor)) return null;
        
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(rawCursor.Length);
        byte[]? rentedArray = null;

        // Stryker disable all : stackalloc micro-optimization, tested functionally
        Span<byte> byteBuffer = maxByteCount <= 1024 
            ? stackalloc byte[maxByteCount] 
            : GetRentedArray(maxByteCount, out rentedArray);
        // Stryker restore all

        try
        {
            int bytesWritten = Encoding.UTF8.GetBytes(rawCursor, byteBuffer);
            var utf8Data = byteBuffer.Slice(0, bytesWritten);
#if NET9_0_OR_GREATER
            return System.Buffers.Text.Base64Url.EncodeToString(utf8Data);
#else
            int maxBase64Length = System.Buffers.Text.Base64.GetMaxEncodedToUtf8Length(utf8Data.Length);
            byte[]? base64RentedArray = null;
            Span<byte> base64Buffer = maxBase64Length <= 1024
                ? stackalloc byte[maxBase64Length]
                : GetRentedArray(maxBase64Length, out base64RentedArray);

            try
            {
                System.Buffers.Text.Base64.EncodeToUtf8(utf8Data, base64Buffer, out _, out int base64Written);
                var base64Slice = base64Buffer.Slice(0, base64Written);
                
                // Make URL-safe
                for (int i = 0; i < base64Slice.Length; i++)
                {
                    if (base64Slice[i] == (byte)'+') base64Slice[i] = (byte)'-';
                    else if (base64Slice[i] == (byte)'/') base64Slice[i] = (byte)'_';
                }

                // Remove padding
                int padding = 0;
                while (base64Slice.Length - padding > 0 && base64Slice[base64Slice.Length - 1 - padding] == (byte)'=')
                {
                    padding++;
                }

                return Encoding.UTF8.GetString(base64Slice.Slice(0, base64Slice.Length - padding));
            }
            finally
            {
                if (base64RentedArray != null)
                    System.Buffers.ArrayPool<byte>.Shared.Return(base64RentedArray);
            }
#endif
        }
        // Stryker disable once block : Array pool cleanup cannot be functionally tested
        finally
        {
            // Stryker disable all : Array pool cleanup cannot be functionally tested
            if (rentedArray != null)
                System.Buffers.ArrayPool<byte>.Shared.Return(rentedArray);
            // Stryker restore all
        }
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidPaginationCursorException"><paramref name="opaqueCursor"/> is not a valid Base64-encoded string</exception>
    public string? Decode(string? opaqueCursor)
    {
        if (string.IsNullOrWhiteSpace(opaqueCursor)) return null;
        try
        {
#if NET9_0_OR_GREATER
            int maxByteCount = System.Buffers.Text.Base64Url.GetMaxDecodedLength(opaqueCursor.Length);
            byte[]? rentedArray = null;

            // Stryker disable all : stackalloc micro-optimization, tested functionally
            Span<byte> byteBuffer = maxByteCount <= 1024 
                ? stackalloc byte[maxByteCount] 
                : GetRentedArray(maxByteCount, out rentedArray);
            // Stryker restore all

            try
            {
                var operationStatus = System.Buffers.Text.Base64Url.DecodeFromChars(
                    opaqueCursor.AsSpan(), 
                    byteBuffer, 
                    out _, 
                    out int bytesWritten);

                if (operationStatus != System.Buffers.OperationStatus.Done)
                {
                    throw new FormatException($"Base64Url decode failed with status {operationStatus}");
                }

                return Encoding.UTF8.GetString(byteBuffer.Slice(0, bytesWritten));
            }
            // Stryker disable once block : Array pool cleanup cannot be functionally tested
            finally
            {
                // Stryker disable all : Array pool cleanup cannot be functionally tested
                if (rentedArray != null)
                    System.Buffers.ArrayPool<byte>.Shared.Return(rentedArray);
                // Stryker restore all
            }
#else
            int base64Length = opaqueCursor.Length;
            int padding = 4 - (base64Length % 4);
            if (padding < 4) base64Length += padding;

            char[]? rentedChars = null;
            if (base64Length > 1024) rentedChars = System.Buffers.ArrayPool<char>.Shared.Rent(base64Length);
            Span<char> base64 = rentedChars != null ? rentedChars : stackalloc char[base64Length];
            
            try
            {
                opaqueCursor.AsSpan().CopyTo(base64);
                for (int i = 0; i < opaqueCursor.Length; i++)
                {
                    if (base64[i] == '-') base64[i] = '+';
                    else if (base64[i] == '_') base64[i] = '/';
                }
                if (padding < 4)
                {
                    for (int i = 0; i < padding; i++)
                    {
                        base64[opaqueCursor.Length + i] = '=';
                    }
                }
                
                int maxByteCount = Encoding.UTF8.GetMaxByteCount(base64Length);
                byte[]? rentedBytes = null;
                if (maxByteCount > 1024) rentedBytes = System.Buffers.ArrayPool<byte>.Shared.Rent(maxByteCount);
                Span<byte> byteBuffer = rentedBytes != null ? rentedBytes : stackalloc byte[maxByteCount];
                    
                try
                {
                    if (!Convert.TryFromBase64Chars(base64[..base64Length], byteBuffer, out int bytesWritten))
                        throw new FormatException("Invalid Base64 format.");
                    return Encoding.UTF8.GetString(byteBuffer.Slice(0, bytesWritten));
                }
                finally
                {
                    if (rentedBytes != null) System.Buffers.ArrayPool<byte>.Shared.Return(rentedBytes);
                }
            }
            finally
            {
                if (rentedChars != null) System.Buffers.ArrayPool<char>.Shared.Return(rentedChars);
            }
#endif
        }
        catch (FormatException ex)
        {
            throw new InvalidPaginationCursorException(
                "The cursor value could not be decoded from Base64Url. " +
                "Ensure the cursor was produced by this library and has not been tampered with.",
                opaqueCursor,
                ex);
        }
    }

    private static Span<byte> GetRentedArray(int length, out byte[] rentedArray)
    {
        rentedArray = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
        return rentedArray;
    }
}

