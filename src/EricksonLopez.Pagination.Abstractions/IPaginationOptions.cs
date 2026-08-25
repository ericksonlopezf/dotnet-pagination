// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines core options for configuring pagination limits and behavior.
/// </summary>
public interface IPaginationOptions
{
    /// <summary>
    /// Gets the maximum allowed page size.
    /// </summary>
    int MaxPageSize { get; }

    /// <summary>
    /// Gets the default page size applied when no page size is specified.
    /// </summary>
    int DefaultPageSize { get; }

    /// <summary>
    /// Gets the maximum character length permitted for a filter expression string.
    /// </summary>
    int MaxFilterStringLength { get; }

    /// <summary>
    /// Gets the maximum character length permitted for an individual filter value operand.
    /// </summary>
    int MaxFilterValueLength { get; }

    /// <summary>
    /// Gets the maximum character length permitted for a sort expression string.
    /// </summary>
    int MaxSortStringLength { get; }

    /// <summary>
    /// Gets the maximum number of individual filter clauses permitted in a filter expression.
    /// </summary>
    int MaxFilterComplexity { get; }

    /// <summary>
    /// Gets the row offset threshold above which a deep-offset log warning is emitted.
    /// </summary>
    int DeepOffsetWarningThreshold { get; }

    /// <summary>
    /// Gets the maximum allowed depth of nested property navigation in a filter expression.
    /// </summary>
    int MaxPropertyDepth { get; }

    /// <summary>
    /// Gets the registry for AOT-compatible cursor decoders.
    /// </summary>
    ICursorDecoderRegistry? CursorDecoderRegistry { get; }
}

