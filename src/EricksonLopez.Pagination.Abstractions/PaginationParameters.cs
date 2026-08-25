// Copyright © Erickson Lopez. MIT License.
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents parameters for requesting a paginated result set using offset pagination.
/// </summary>
/// <remarks>
/// Page numbers are 1-indexed.
/// <para>
/// <b>Validation contract</b>: The <c>init</c> accessor of <see cref="Page"/> and
/// <see cref="PageSize"/> throw <see cref="ArgumentOutOfRangeException"/> for values
/// below 1. The parameterless default constructor produces a zero-initialized struct
/// whose getters clamp to the defaults (page = 1, pageSize = 10); this is intentional
/// to allow zero-initialization for framework infrastructure (e.g., model binding
/// before validation runs). Consumer code should always use <see cref="Create"/> or
/// the object-initializer syntax.
/// </para>
/// <para>
/// <b>Minimal API binding</b>: On .NET 7 and later, this struct implements
/// <c>IParsable{T}</c> which enables native query-string binding in
/// ASP.NET Core Minimal APIs via <c>[AsParameters]</c>. The parse format is
/// <c>page={n}&amp;pageSize={m}</c>. Invalid values produce <see cref="FormatException"/>.
/// </para>
/// </remarks>
#if NET7_0_OR_GREATER
public readonly record struct PaginationParameters : IParsable<PaginationParameters>
#else
public readonly record struct PaginationParameters
#endif
{
    private readonly int _pageSize;
    private readonly int _page;

    /// <summary>
    /// Gets the current page number (1-indexed).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1</exception>
    public int Page
    {
        get => _page <= 0 ? 1 : _page;
        [System.Diagnostics.StackTraceHidden]
        init
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Page must be greater than or equal to 1.");
            _page = value;
        }
    }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1 or greater than 100,000</exception>
    public int PageSize
    {
        get => _pageSize <= 0 ? 10 : _pageSize;
        [System.Diagnostics.StackTraceHidden]
        init
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), value, "PageSize must be greater than or equal to 1.");
            if (value > 100_000) throw new ArgumentOutOfRangeException(nameof(value), value, "PageSize cannot exceed the absolute maximum of 100,000 to prevent memory exhaustion.");
            _pageSize = value;
        }
    }

    /// <summary>
    /// Represents the default pagination parameters configured for page 1 with 10 items per page.
    /// </summary>
    public static readonly PaginationParameters Default = new() { Page = 1, PageSize = 10 };

    /// <summary>
    /// Creates a new <see cref="PaginationParameters"/> instance with the specified page number and page size.
    /// </summary>
    /// <param name="page">The page number (1-indexed). Must be at least 1.</param>
    /// <param name="pageSize">The number of items per page. Must be between 1 and 100,000.</param>
    /// <returns>A new <see cref="PaginationParameters"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="page"/> or <paramref name="pageSize"/> is less than 1, or <paramref name="pageSize"/> is greater than 100,000</exception>
    public static PaginationParameters Create(int page, int pageSize) => new()
    {
        Page = page,
        PageSize = pageSize
    };


#if NET7_0_OR_GREATER
    /// <summary>
    /// Converts the string representation of pagination parameters to a <see cref="PaginationParameters"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the pagination parameters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <returns>The parsed <see cref="PaginationParameters"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/></exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not in a valid format</exception>
    public static PaginationParameters Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException(
                $"Cannot parse '{s}' as {nameof(PaginationParameters)}. " +
                "Expected format: 'page=<n>&pageSize=<m>' where n and m are positive integers.");
        }
        return result;
    }

    /// <summary>
    /// Attempts to convert the string representation of pagination parameters to a <see cref="PaginationParameters"/> instance.
    /// </summary>
    /// <param name="s">The string representation of the pagination parameters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information. Ignored.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="PaginationParameters"/> instance if the conversion succeeded, or the default value if it failed.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was converted successfully or was null or whitespace; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out PaginationParameters result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            // Returning Default + true aligns with the IParsable<T> contract expectation
            // used by ASP.NET Core Minimal API binding: a missing query parameter should
            // produce a valid default result, not a binding failure.
            result = Default;
            return true;
        }

        int page = 1;
        int pageSize = 10;

        // Parse key=value pairs separated by '&'
        var span = s.AsSpan();
        while (!span.IsEmpty)
        {
            int ampIndex = span.IndexOf('&');
            // Stryker disable once all : Unkillable mutation because processing the whole string instead of splitting by & results in invalid keys that are ignored anyway
            var pair = ampIndex >= 0 ? span[..ampIndex] : span;
            // Stryker disable once all : Unkillable mutation because true causes infinite loop when ampIndex is -1
            span = ampIndex >= 0 ? span[(ampIndex + 1)..] : ReadOnlySpan<char>.Empty;

            int eqIndex = pair.IndexOf('=');
            // Stryker disable once Equality : Unkillable mutation because when eqIndex is 0 the key is empty, which is ignored later anyway
            if (eqIndex < 0) continue;

            var key = pair[..eqIndex].Trim();
            var value = pair[(eqIndex + 1)..].Trim();

            if (key.Equals("page", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPage) || parsedPage < 1)
                {
                    result = default;
                    return false;
                }
                page = parsedPage;
            }
            else if (key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedSize) || parsedSize < 1 || parsedSize > 100_000)
                {
                    result = default;
                    return false;
                }
                pageSize = parsedSize;
            }
        }

        result = Create(page, pageSize);
        return true;
    }
#endif
}



