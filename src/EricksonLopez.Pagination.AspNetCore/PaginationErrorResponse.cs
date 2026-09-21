// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Represents a structured error response returned by pagination filters.
/// </summary>
/// <param name="Error">The detailed error message.</param>
public sealed record PaginationErrorResponse(string Error)
{
    /// <inheritdoc/>
    public override string ToString() => $"{{ error = {Error} }}";
}
