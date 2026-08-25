// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents a structured filter expression for paginated queries, following the same
/// convention as <see cref="PaginationParameters"/> and <see cref="CursorPaginationParameters"/>.
/// </summary>
/// <remarks>
/// <para>
/// The filter string uses a simple, URL-friendly DSL:
/// <c>field=value</c> for equality, <c>field!=value</c> for inequality,
/// <c>field&gt;value</c>, <c>field&lt;value</c>, <c>field&gt;=value</c>, <c>field&lt;=value</c>
/// for comparisons, <c>field~=value</c> for string contains, <c>field^=value</c> for starts-with,
/// <c>field$=value</c> for ends-with. Multiple filters are separated by commas.
/// </para>
/// <para>
/// Example: <c>name~=John,age&gt;=18,isActive=true</c>
/// </para>
/// <para>
/// <b>Zero-dependency contract</b>: This type lives in the Abstractions package and has no EF Core
/// dependency. The expression translation is performed by <c>QueryableExtensions.ApplyFilter</c>
/// in the <c>EricksonLopez.Pagination.EntityFrameworkCore</c> package.
/// </para>
/// <para>
/// <b>String Case-Sensitivity</b>: String matching operators (<c>~=</c>, <c>^=</c>, <c>$=</c>) are translated directly to
/// standard framework methods (like <c>string.Contains</c>) without explicit <c>StringComparison</c>. When executed against
/// a database (e.g., via EF Core or Dapper), the case-sensitivity of the search is entirely determined by the
/// <b>database column's collation</b> (e.g., case-insensitive in SQL Server's default collation, but case-sensitive
/// in PostgreSQL's <c>C</c> collation). When executed in-memory against <c>IEnumerable&lt;T&gt;</c>, the search
/// is natively case-sensitive.
/// </para>
/// <para>
/// <b>Design decision</b>: Implements <c>IParsable&lt;T&gt;</c> (on .NET 7+) to support automatic 
/// binding in ASP.NET Core Minimal APIs via <c>[AsParameters]</c> or from query strings. The 
/// <c>TryParse</c> implementation applies basic syntax validation to ensure it doesn't fail silently.
/// </para>
/// </remarks>
#if NET7_0_OR_GREATER

public readonly record struct FilterParameters : IParsable<FilterParameters>
#else

public readonly record struct FilterParameters
#endif
{
    /// <summary>
    /// Gets the raw filter expression string.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Represents the maximum character length permitted for a filter expression string.
    /// </summary>
    public const int AbsoluteMaxLength = 4096;
    
    /// <summary>
    /// Represents the maximum number of filter clauses permitted in an expression.
    /// </summary>
    public const int AbsoluteMaxComplexity = 50;

    /// <summary>
    /// Gets a value indicating whether this instance contains a non-empty filter expression.
    /// </summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Represents an empty filter parameter instance with no filter applied.
    /// </summary>
    public static readonly FilterParameters Empty = new();

    /// <summary>
    /// Creates a new <see cref="FilterParameters"/> instance initialized with the specified raw filter string.
    /// </summary>
    /// <param name="filter">The filter expression string.</param>
    /// <returns>A new <see cref="FilterParameters"/> instance.</returns>
    public static FilterParameters From(string? filter) => new() { Value = filter };

#if NET7_0_OR_GREATER
    /// <summary>
    /// Converts the string representation of filter parameters to a <see cref="FilterParameters"/> instance.
    /// </summary>
    /// <param name="s">The filter DSL string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <returns>A <see cref="FilterParameters"/> instance representing the parsed filter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/></exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid filter expression</exception>
    public static FilterParameters Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (TryParse(s, provider, out var result))
            return result;
        throw new FormatException($"The string '{s}' is not a valid filter expression.");
    }

    /// <summary>
    /// Attempts to convert the string representation of filter parameters to a <see cref="FilterParameters"/> instance.
    /// </summary>
    /// <param name="s">The filter DSL string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="FilterParameters"/> if the conversion succeeded;
    /// otherwise, the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="s"/> was successfully parsed; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Two-layer validation design:</b> This method performs syntax-only validation and total string
    /// length checks (up to <see cref="AbsoluteMaxLength"/> = 4096 characters). Individual filter value
    /// length validation (e.g., limiting LIKE patterns to prevent DoS attacks) is performed at expression
    /// build time by <c>QueryableExtensions.ApplyFilter</c> using the runtime-configured
    /// <c>PaginationCoreOptions.MaxFilterValueLength</c>. This separation keeps the Abstractions package
    /// free of runtime dependencies.
    /// </para>
    /// <para>
    /// When this method returns <see langword="false"/>, <paramref name="result"/> contains
    /// <see langword="default"/> (<see cref="Empty"/>). Both are equivalent because
    /// <c>FilterParameters.default</c> and <c>FilterParameters.Empty</c> have identical runtime
    /// behaviour (<see cref="HasValue"/> returns <see langword="false"/> in both cases).
    /// </para>
    /// </remarks>
    public static bool TryParse(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out FilterParameters result)
    {
        if (string.IsNullOrWhiteSpace(s) || s.Length > AbsoluteMaxLength)
        {
            result = default;
            return false;
        }

        var span = s.AsSpan();
        bool hasAnyValidSegment = false;
        int clauseCount = 0;
        
        while (!span.IsEmpty)
        {
            int commaIdx = span.IndexOf(',');
            // Stryker disable all : Unkillable mutations because this is syntax validation and edge cases result in the same boolean outcome
            var clause = commaIdx >= 0 ? span[..commaIdx] : span;
            span = commaIdx >= 0 ? span[(commaIdx + 1)..] : ReadOnlySpan<char>.Empty;
            // Stryker restore all
            
            if (clause.IsWhiteSpace()) continue;
            if (++clauseCount > AbsoluteMaxComplexity)
            {
                result = default;
                return false;
            }

            bool hasAnyOrSegment = false;
            var segment = clause;
            while (!segment.IsEmpty)
            {
                int pipeIdx = segment.IndexOf('|');
                // Stryker disable all : Unkillable mutations for the same reason
                var orSegment = pipeIdx >= 0 ? segment[..pipeIdx] : segment;
                segment = pipeIdx >= 0 ? segment[(pipeIdx + 1)..] : ReadOnlySpan<char>.Empty;
                // Stryker restore all
                
                if (orSegment.IsWhiteSpace()) continue;
                hasAnyOrSegment = true;
                hasAnyValidSegment = true;
                
                var trimmed = orSegment.TrimStart('!').Trim();
                // We just need to verify that a valid operator symbol is present and not at index 0.
                // Operators involve: =, >, <, ~, ^, $
                var idx = trimmed.IndexOfAny("=><~^$".AsSpan());

                if (idx <= 0)
                {
                    result = default;
                    return false;
                }
            }
            if (!hasAnyOrSegment)
            {
                result = default;
                return false;
            }
        }
        
        if (!hasAnyValidSegment)
        {
            result = default;
            return false;
        }

        result = From(s);
        return true;
    }
#endif

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}



