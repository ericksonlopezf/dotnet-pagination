// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents core options for configuring pagination limits and behavior.
/// </summary>
public class PaginationCoreOptions : IValidatableObject, IPaginationOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed page size.
    /// </summary>
    [Range(1, 100_000, ErrorMessage = "MaxPageSize must be between 1 and 100,000.")]
    public int MaxPageSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the default page size applied when none is specified.
    /// </summary>
    [Range(1, 10_000, ErrorMessage = "DefaultPageSize must be between 1 and 10,000.")]
    public int DefaultPageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the options specific to keyset and cursor-based pagination.
    /// </summary>
    public PaginationCursorOptions Cursor { get; set; } = new();

    /// <summary>
    /// Gets or sets the registry for AOT-compatible cursor decoders.
    /// </summary>
    public ICursorDecoderRegistry? CursorDecoderRegistry { get; set; }

    /// <summary>
    /// Gets or sets the row offset threshold above which a warning log is generated.
    /// </summary>
    public int DeepOffsetWarningThreshold { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum allowed length of a filter DSL string.
    /// </summary>
    [Range(1, 4096, ErrorMessage = "MaxFilterStringLength must be between 1 and 4096 (the absolute binding-layer cap).")]
    public int MaxFilterStringLength { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum allowed depth of nested property navigation in filter expressions.
    /// </summary>
    [Range(1, 20, ErrorMessage = "MaxPropertyDepth must be between 1 and 20.")]
    public int MaxPropertyDepth { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum allowed length of an individual filter value.
    /// </summary>
    [Range(1, 2_000, ErrorMessage = "MaxFilterValueLength must be between 1 and 2,000.")]
    public int MaxFilterValueLength { get; set; } = 200;

    /// <summary>
    /// Gets or sets the maximum allowed length of a sort DSL string.
    /// </summary>
    [Range(1, 5_000, ErrorMessage = "MaxSortStringLength must be between 1 and 5,000.")]
    public int MaxSortStringLength { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum allowed clause complexity for filter expressions.
    /// </summary>
    [Range(1, 100, ErrorMessage = "MaxFilterComplexity must be between 1 and 100.")]
    public int MaxFilterComplexity { get; set; } = 20;

    /// <summary>
    /// Gets or sets a value indicating whether legacy cursor formats are accepted.
    /// </summary>
    public bool AcceptLegacyCursors { get; set; } = true;

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DefaultPageSize > MaxPageSize)
        {
            yield return new ValidationResult("DefaultPageSize cannot be greater than MaxPageSize.", new[] { nameof(DefaultPageSize), nameof(MaxPageSize) });
        }
    }
}

