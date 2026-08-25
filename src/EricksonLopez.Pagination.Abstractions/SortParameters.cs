// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents structured sorting parameters for paginated queries, following the same
/// convention as <see cref="FilterParameters"/>.
/// </summary>
/// <remarks>
/// <para>
/// The sort string uses a simple DSL: comma-separated property names, optionally followed
/// by 'asc' or 'desc'.
/// Example: <c>name asc,age desc</c>
/// </para>
/// <para>
/// <b>Zero-dependency contract</b>: This type lives in the Abstractions package and has no EF Core
/// dependency. The expression translation is performed by <c>QueryableExtensions.ApplySort</c>
/// in the <c>EricksonLopez.Pagination.EntityFrameworkCore</c> package.
/// </para>
/// </remarks>
#if NET7_0_OR_GREATER
public readonly partial record struct SortParameters : IParsable<SortParameters>
#else
public readonly partial record struct SortParameters
#endif
{
    /// <summary>
    /// Gets the raw sort expression string.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets a value indicating whether this instance contains a non-empty sort expression.
    /// </summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Represents an empty sort parameter instance with no sorting applied.
    /// </summary>
    public static readonly SortParameters Empty = new();

    /// <summary>
    /// Creates a new <see cref="SortParameters"/> instance initialized with the specified raw sort string.
    /// </summary>
    /// <param name="sort">The sort expression string.</param>
    /// <returns>A new <see cref="SortParameters"/> instance.</returns>
    public static SortParameters From(string? sort) => new() { Value = sort };

#if NET7_0_OR_GREATER
    /// <summary>
    /// Converts the string representation of sort parameters to a <see cref="SortParameters"/> instance.
    /// </summary>
    /// <param name="s">The sort DSL string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <returns>A <see cref="SortParameters"/> instance representing the parsed sort expression.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid sort expression</exception>
    public static SortParameters Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;
        throw new FormatException($"The string '{s}' is not a valid sort expression.");
    }


    /// <summary>
    /// Attempts to convert the string representation of sort parameters to a <see cref="SortParameters"/> instance.
    /// </summary>
    /// <param name="s">The sort DSL string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="SortParameters"/> if the conversion succeeded; otherwise, the default empty instance.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was converted successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out SortParameters result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = Empty;
            return false;
        }

        var span = s.AsSpan();
        bool hasValidPart = false;
        while (!span.IsEmpty)
        {
            int commaIdx = span.IndexOf(',');
            // Stryker disable all : Unkillable syntax validation mutation
            var segment = commaIdx != -1 ? span[..commaIdx] : span;
            span = commaIdx != -1 ? span[(commaIdx + 1)..] : ReadOnlySpan<char>.Empty;
            // Stryker restore all
            
            var part = segment.Trim();
            if (part.IsEmpty) continue;
            
            var spaceIndex = part.LastIndexOf(' ');
            if (spaceIndex != -1)
            {
                var suffix = part[(spaceIndex + 1)..];
                if (!suffix.Equals("asc", StringComparison.OrdinalIgnoreCase) && 
                    !suffix.Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    result = default;
                    return false;
                }
                var colName = part[..spaceIndex].Trim();
                if (colName.IndexOf(' ') != -1)
                {
                    result = default;
                    return false;
                }
                hasValidPart = true;
            }
            else
            {
                hasValidPart = true;
            }
        }

        if (!hasValidPart)
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

#if NET7_0_OR_GREATER
    
    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$")]
    private static partial System.Text.RegularExpressions.Regex ValidColumnNameRegex();
#else
    private static readonly System.Text.RegularExpressions.Regex _validColumnNameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static System.Text.RegularExpressions.Regex ValidColumnNameRegex() => _validColumnNameRegex;
#endif

    /// <summary>
    /// Validates that the specified column name is a permitted sort column.
    /// </summary>
    /// <param name="columnName">The dot-separated column name to validate.</param>
    /// <param name="allowedProperties">An optional collection of permitted column names.</param>
    /// <exception cref="ArgumentException"><paramref name="columnName"/> is null, whitespace, exceeds 200 characters, or contains invalid characters</exception>
    /// <exception cref="InvalidOperationException"><paramref name="allowedProperties"/> is provided and <paramref name="columnName"/> is not in the allowlist</exception>
    public static void ValidateColumnName(string columnName, IEnumerable<string>? allowedProperties = null)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(columnName));
        }

        if (allowedProperties != null)
        {
            // Fast path: if the caller already has a set, use it directly (O(1) per lookup, no delegate allocation).
            if (allowedProperties is IReadOnlySet<string> set)
            {
                ValidateColumnName(columnName, set);
            }
            else
            {
                // Slow path: materialize the enumerable into a temporary HashSet for consistent lookup semantics.
                var tempSet = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);
                ValidateColumnName(columnName, tempSet);
            }
            return;
        }

        // Reject implausibly long column names in O(1) before invoking the regex engine.
        // This guards against ReDoS: the pattern (\.[a-zA-Z_][a-zA-Z0-9_]*)* can exhibit
        // super-linear backtracking on crafted inputs of the form 'a.a.a...X' without this check.
        // No legitimate column name exceeds 200 characters.
        if (columnName.Length > 200)
        {
            throw new ArgumentException($"Sort column name exceeds the maximum allowed length of 200 characters: '{columnName.Substring(0, 20)}...'.");
        }

        if (!ValidColumnNameRegex().IsMatch(columnName))
        {
            throw new ArgumentException($"Invalid characters in sort column name: '{columnName}'. Column names must be alphanumeric.");
        }
    }

    /// <summary>
    /// Validates that the specified column name is present in the permitted property set.
    /// </summary>
    /// <param name="columnName">The dot-separated column name to validate.</param>
    /// <param name="allowedProperties">A non-null set of permitted column names or root segments.</param>
    /// <exception cref="ArgumentException"><paramref name="columnName"/> is null or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="allowedProperties"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException"><paramref name="columnName"/> is not in <paramref name="allowedProperties"/></exception>
    public static void ValidateColumnName(string columnName, IReadOnlySet<string> allowedProperties)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(columnName));
        if (allowedProperties is null)
            throw new ArgumentNullException(nameof(allowedProperties));

        // F-011 fix: check both exact match AND root-segment match.
        var dotIndex = columnName.IndexOf('.');
        var rootSegment = dotIndex != -1 ? columnName.Substring(0, dotIndex) : columnName;

        bool exactMatch = allowedProperties.Contains(columnName);
        bool rootMatch = dotIndex != -1 && allowedProperties.Contains(rootSegment);

        if (!exactMatch && !rootMatch)
        {
            throw new InvalidOperationException($"Sorting on property '{columnName}' is not permitted.");
        }
    }
}




