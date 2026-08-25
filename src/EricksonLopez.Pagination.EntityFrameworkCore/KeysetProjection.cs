// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Pagination.EntityFrameworkCore;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class KeysetProjection<T>
{
    public T Item { get; set; } = default!;
    // F-006 fix: expanded from 5 to 16 cursor column slots.
    // Each slot stores the string representation of one keyset column value.
    // Up to 16 composite keyset columns are supported for the server-side projection path.
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C1  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C2  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C3  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C4  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C5  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C6  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C7  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C8  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C9  { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C10 { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C11 { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C12 { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C13 { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C14 { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C15 { get; set; }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] public string? C16 { get; set; }
}
