// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents parameters for requesting a cursor-based paginated result set.
/// Supports both forward (<c>First</c>/<c>After</c>) and backward (<c>Last</c>/<c>Before</c>) pagination,
/// following the GraphQL Relay Cursor Connections Specification.
/// </summary>
#if NET7_0_OR_GREATER
public readonly record struct CursorPaginationParameters : IParsable<CursorPaginationParameters>
#else
public readonly record struct CursorPaginationParameters
#endif
{
    private readonly int? _first;
    private readonly int? _last;

    /// <summary>
    /// Gets the number of items to take from the beginning of the result set in forward pagination.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Last"/>. If both are specified, <see cref="First"/> takes precedence.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1 or greater than 100,000</exception>
    public int? First
    {
        get => _first;
        init
        {
            if (value.HasValue && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(value), value.Value, "First must be greater than or equal to 1.");
            if (value.HasValue && value.Value > 100_000) throw new ArgumentOutOfRangeException(nameof(value), value.Value, "First cannot exceed the absolute maximum of 100,000 to prevent memory exhaustion.");
            _first = value;
        }
    }

    /// <summary>
    /// Gets the opaque cursor after which to take items in forward pagination.
    /// </summary>
    /// <remarks>
    /// When specified, only items positioned after the element identified by this cursor are returned.
    /// </remarks>
    public string? After { get; init; }

    /// <summary>
    /// Gets the number of items to take from the end of the result set in backward pagination.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="First"/>. Used in conjunction with <see cref="Before"/> for backward traversal.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1 or greater than 100,000</exception>
    public int? Last
    {
        get => _last;
        init
        {
            if (value.HasValue && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(value), value.Value, "Last must be greater than or equal to 1.");
            if (value.HasValue && value.Value > 100_000) throw new ArgumentOutOfRangeException(nameof(value), value.Value, "Last cannot exceed the absolute maximum of 100,000 to prevent memory exhaustion.");
            _last = value;
        }
    }

    /// <summary>
    /// Gets the opaque cursor before which to take items in backward pagination.
    /// </summary>
    /// <remarks>
    /// When specified, only items positioned before the element identified by this cursor are returned.
    /// </remarks>
    public string? Before { get; init; }

    /// <summary>
    /// Calculates the effective page size resolved from <see cref="First"/> or <see cref="Last"/>, falling back to the specified default size.
    /// </summary>
    /// <remarks>
    /// Pass the application-configured default rather than a hardcoded value to preserve configuration-driven policy.
    /// </remarks>
    /// <param name="defaultSize">The fallback page size when neither <see cref="First"/> nor <see cref="Last"/> is set. Must be at least 1.</param>
    /// <returns>The resolved page size.</returns>
    public int GetPageSize(int defaultSize = 10)
    {
        return First ?? Last ?? defaultSize;
    }

    /// <summary>
    /// Represents the default cursor pagination parameters configured for forward pagination with 10 items.
    /// </summary>
    public static readonly CursorPaginationParameters Default = new() { First = 10 };

#if NET7_0_OR_GREATER
    /// <summary>
    /// Converts the string representation of cursor pagination parameters to a <see cref="CursorPaginationParameters"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the cursor pagination parameters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <returns>The parsed <see cref="CursorPaginationParameters"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/></exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not in a valid format</exception>
    public static CursorPaginationParameters Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException($"Cannot parse '{s}' as {nameof(CursorPaginationParameters)}.");
        }
        return result;
    }

    /// <summary>
    /// Attempts to convert the string representation of cursor pagination parameters to a <see cref="CursorPaginationParameters"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the cursor pagination parameters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="CursorPaginationParameters"/> instance if the conversion succeeded, or the default value if it failed.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was converted successfully; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This implementation does not accept null or whitespace strings.</remarks>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out CursorPaginationParameters result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        int? first = null;
        string? after = null;
        int? last = null;
        string? before = null;

        // anyValuefulParam: at least one recognized key produced a non-null value
        // anySeenKeyValuePair: at least one key=value pair was parsed (including unknown keys)
        bool anyValuefulParam = false;
        bool anySeenKeyValuePair = false;

        var span = s.AsSpan();
        while (!span.IsEmpty)
        {
            int ampIndex = span.IndexOf('&');
            // Stryker disable once all : Unkillable mutation because processing the whole string instead of splitting by & results in invalid keys that are ignored anyway
            var pair = ampIndex >= 0 ? span[..ampIndex] : span;
            // Stryker disable once all : Unkillable mutation for the same reason
            span = ampIndex >= 0 ? span[(ampIndex + 1)..] : ReadOnlySpan<char>.Empty;

            int eqIndex = pair.IndexOf('=');
            if (eqIndex < 0) continue;

            var key = pair[..eqIndex].Trim();
            if (key.Length == 0) return false;

            var valueStr = pair[(eqIndex + 1)..].Trim().ToString();
            anySeenKeyValuePair = true; // we have a valid key=value pair

            if (key.Equals("first", StringComparison.OrdinalIgnoreCase))

            {
                if (valueStr.Length == 0) continue; // skip empty; checked via anyValuefulParam later
                if (!int.TryParse(valueStr, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var f)) return false;
                if (f < 1) return false;
                first = f;
                anyValuefulParam = true;
            }
            else if (key.Equals("after", StringComparison.OrdinalIgnoreCase))
            {
                after = Uri.UnescapeDataString(valueStr);
                anyValuefulParam = true;
            }
            else if (key.Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                if (valueStr.Length == 0) continue; // skip empty; checked via anyValuefulParam later
                if (!int.TryParse(valueStr, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var l)) return false;
                if (l < 1) return false;
                last = l;
                anyValuefulParam = true;
            }
            else if (key.Equals("before", StringComparison.OrdinalIgnoreCase))
            {
                before = Uri.UnescapeDataString(valueStr);
                anyValuefulParam = true;
            }
            else
            {
                // Unknown key — skip it gracefully.
                // Unknown keys are tolerated when they appear alongside recognized cursor keys
                // (e.g. "first=10&unknown=5&after=token" parses successfully with first=10, after=token).
                // However, if NO recognized cursor keys appear in the entire string, TryParse returns
                // false below via the !anyRecognizedKey check (e.g. "unknown=value" → false).
            }
        }

        if (first.HasValue && last.HasValue)
        {
            result = default;
            return false;
        }

        if (!anyValuefulParam)
        {
            if (!anySeenKeyValuePair)
            {
                // No key=value pairs at all (input was only "&&&" or similar with no '=').
                // Treat as "first page, forward direction" for HTTP binding compatibility.
                result = CursorPaginationParameters.Default;
                return true;
            }
            // Recognized cursor keys were present but all had empty values (e.g. "first=" alone)
            // OR unknown keys were seen. There is no actionable pagination intent — return false.
            return false;
        }

        result = new CursorPaginationParameters
        {
            First = first,
            After = after,
            Last = last,
            Before = before
        };
        return true;
    }

#endif
}



