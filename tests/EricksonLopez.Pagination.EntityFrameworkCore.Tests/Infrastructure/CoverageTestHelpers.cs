// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class MyCustomComparableClass : IComparable
{
    public int CompareTo(object? obj) => 0;
}

public class CustomCursorEncoder : ICursorEncoder
{
    public string? Encode(string? rawCursor) => rawCursor != null ? "CUSTOM-" + rawCursor : null;
    public string? Decode(string? opaqueCursor) => opaqueCursor != null && opaqueCursor.StartsWith("CUSTOM-") ? opaqueCursor.Substring(7) : opaqueCursor;
}

public class CustomPaginationOptions : IPaginationOptions
{
    public int DefaultPageSize { get; set; } = 10;
    public int MaxPageSize { get; set; } = 100;
    public int MaxFilterComplexity { get; set; } = 5;
    public int MaxFilterValueLength { get; set; } = 50;
    public int MaxFilterStringLength { get; set; } = 200;
    public int MaxSortStringLength { get; set; } = 200;
    public int DeepOffsetWarningThreshold { get; set; } = 100;
    public int MaxPropertyDepth { get; set; } = 3;
    public ICursorDecoderRegistry? CursorDecoderRegistry { get; set; } = null;
}

