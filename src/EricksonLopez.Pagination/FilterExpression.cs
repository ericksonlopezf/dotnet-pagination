// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using EricksonLopez.Pagination.Internal;

namespace EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Internal engine that translates a <see cref="FilterParameters"/> DSL string into a
/// composable <see cref="Expression{TDelegate}"/> that EF Core can translate to SQL.
/// </summary>
/// <remarks>
/// <para>
/// <b>Supported operators</b>: <c>=</c> (equals), <c>!=</c> (not equals),
/// <c>&gt;=</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&lt;</c> (numeric/date comparisons),
/// <c>~=</c> (string contains), <c>^=</c> (string starts with),
/// <c>$=</c> (string ends with).
/// </para>
/// <para>
/// <b>High-Concurrency Caching</b>: The filter expression cache uses a thread-safe LRU cache.
/// The cache key includes both the <c>maxComplexity</c> and <c>unknownFieldBehavior</c> constraints to prevent cache poisoning.
/// </para>
/// <para>
/// <b>Warning on string comparison</b>: The <c>~=</c>, <c>^=</c>, and <c>$=</c> operators use <c>string.Contains</c>,
/// <c>string.StartsWith</c>, and <c>string.EndsWith</c> respectively. When translated by EF Core, 
/// the case-sensitivity depends entirely on the database column's collation.
/// </para>
/// <para>
/// <b>Multiple conditions</b>: Comma-separated filters are AND-combined.
/// Example: <c>"name~=John,age&gt;=18,isActive=true"</c>
/// </para>
/// <para>
/// <b>OR conditions</b>: Pipe-separated filters are OR-combined.
/// Example: <c>"name=John|name=Jane"</c>
/// Note: The <c>|</c> character is a reserved operator. If your filter value contains a literal pipe, 
/// it must be URL-encoded as <c>%7C</c> (e.g., <c>"name=Value%7CWithPipe"</c>).
/// </para>
/// <para>
/// <b>Type coercion</b>: Values are coerced to the property type via a fast-path
/// switch for BCL primitives (string, int, long, bool, Guid, DateTime, DateTimeOffset, decimal, double, float).
/// Unsupported types fall back to <see cref="Convert.ChangeType(object, Type)"/>.
/// </para>
/// </remarks>
[RequiresUnreferencedCode("FilterExpression uses reflection to locate entity properties by name.")]
internal static class FilterExpression
{


    /// <summary>
    /// Translates a <see cref="FilterParameters"/> DSL string into a strongly typed expression predicate.
    /// </summary>
    /// <typeparam name="T">The entity type to filter.</typeparam>
    /// <param name="filter">The filter parameters containing the raw filter string.</param>
    /// <param name="maxComplexity">The maximum number of clauses allowed to prevent DoS attacks.</param>
    /// <param name="unknownFieldBehavior">The behavior when an unknown field is encountered.</param>
    /// <param name="allowedProperties">An optional allowlist of properties that are allowed to be filtered. Property names are matched case-insensitively. If <see langword="null"/>, all properties are allowed.</param>
    /// <param name="maxFilterValueLength">The maximum length allowed for any filter value string to prevent DoS attacks.</param>
    /// <param name="maxPropertyDepth">The maximum allowed depth of nested property navigation (e.g., depth 3 = A.B.C). Defaults to 3. Prevents unbounded JOIN chains.</param>
    /// <param name="customOperatorProvider">An optional custom operator provider defining additional filter operators.</param>
    /// <returns>A strongly typed predicate expression, or <see langword="null"/> if no filter is applied.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxComplexity"/> is less than or equal to zero, or filter complexity/value length exceeds configured limits</exception>
    /// <exception cref="InvalidOperationException">Filtering is attempted on a property not permitted by <paramref name="allowedProperties"/> or unknown property handling is set to throw</exception>
    [RequiresUnreferencedCode("FilterExpression uses reflection to locate properties, which is incompatible with trimming.")]
    [RequiresDynamicCode("FilterExpression uses MakeGenericType and MakeGenericMethod, which is incompatible with Native AOT.")]
    public static Expression<Func<T, bool>>? Build<T>(FilterParameters filter, int maxComplexity = 20, FilterUnknownFieldBehavior unknownFieldBehavior = FilterUnknownFieldBehavior.ThrowException, IEnumerable<string>? allowedProperties = null, int maxFilterValueLength = 1000, int maxPropertyDepth = 3, IFilterOperatorProvider<T>? customOperatorProvider = null)
    {
        if (!filter.HasValue) return null;

        var rawValue = filter.Value!;
        if (customOperatorProvider == null)
        {
            var key = new PaginationExpressionCache.FilterCacheKey(typeof(T), rawValue, unknownFieldBehavior, maxComplexity, allowedProperties, maxFilterValueLength);
            return (Expression<Func<T, bool>>?)PaginationExpressionCache.Filters.GetOrAdd(key, _ => BuildInternal<T>(rawValue, maxComplexity, unknownFieldBehavior, allowedProperties, maxFilterValueLength, maxPropertyDepth, null)!);
        }

        return BuildInternal<T>(rawValue, maxComplexity, unknownFieldBehavior, allowedProperties, maxFilterValueLength, maxPropertyDepth, customOperatorProvider);
    }

    [RequiresUnreferencedCode("FilterExpression uses reflection to locate properties, which is incompatible with trimming.")]
    [RequiresDynamicCode("FilterExpression uses MakeGenericType and MakeGenericMethod, which is incompatible with Native AOT.")]
    private static Expression<Func<T, bool>>? BuildInternal<T>(string rawValue, int maxComplexity, FilterUnknownFieldBehavior unknownFieldBehavior, IEnumerable<string>? allowedProperties, int maxFilterValueLength, int maxPropertyDepth = 3, IFilterOperatorProvider<T>? customOperatorProvider = null)
    {
        if (maxComplexity <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxComplexity), maxComplexity,
                "maxComplexity must be greater than zero. Check your PaginationCoreOptions.MaxFilterComplexity configuration.");

        // Stryker disable once String : Parameter name does not affect LINQ semantics in this context
        // Stryker disable once String : Parameter name does not affect LINQ semantics in this context
        var param = Expression.Parameter(typeof(T), "x");

        // FIX-19: Use List<Expression> instead of ArrayPool<Expression>.
        // ArrayPool benefits value-type byte[] arrays where pooling avoids GC pressure.
        // For managed Expression[] the GC must track each Expression reference individually
        // regardless of the array container — the pool only saves the tiny array wrapper
        // allocation on the (already-rare) cold path. List<Expression> is simpler, eliminates
        // the try/finally Return lifecycle, and has identical hot-path performance since
        // BuildInternal results are fully cached.
        // Stryker disable once linq : List capacity optimization is functionally untestable
        var clauses = new List<Expression>(capacity: Math.Min(maxComplexity, 16));
        var allowedSet = allowedProperties != null ? new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase) : null;
        var customTokens = customOperatorProvider?.Operators.Keys;

        var span = rawValue.AsSpan();
        int clausesCount = 0;

        while (!span.IsEmpty)
        {
            // Stryker disable all : Equality mutations on these slice indexes are functionally equivalent due to empty clause skipping
            int commaIdx = span.IndexOf(',');
            var andSegment = commaIdx >= 0 ? span[..commaIdx] : span;
            span = commaIdx >= 0 ? span[(commaIdx + 1)..] : ReadOnlySpan<char>.Empty;
            // Stryker restore all

            // Stryker disable once statement : Functionally identical because Parse returns null for whitespace anyway
            if (andSegment.IsWhiteSpace()) continue;

            var orClauses = new List<Expression>(capacity: 4);

            while (!andSegment.IsEmpty)
            {
                // Stryker disable all : Equality mutations on these slice indexes are functionally equivalent due to empty clause skipping
                int pipeIdx = andSegment.IndexOf('|');
                var orSegment = pipeIdx >= 0 ? andSegment[..pipeIdx] : andSegment;
                andSegment = pipeIdx >= 0 ? andSegment[(pipeIdx + 1)..] : ReadOnlySpan<char>.Empty;
                // Stryker restore all

                // Stryker disable once statement : Functionally identical because Parse returns null for whitespace anyway
                if (orSegment.IsWhiteSpace()) continue;

                clausesCount++;
                if (clausesCount > maxComplexity)
                {
                    // FIX-05: ArgumentOutOfRangeException is catchable by standard middleware
                    // (ArgumentException subclass) whereas InvalidOperationException is not.
                    // This prevents a 500 response when a client sends a filter with more
                    // clauses than MaxFilterComplexity allows but fewer than AbsoluteMaxComplexity.
                    throw new ArgumentOutOfRangeException(
                        nameof(rawValue),
                        clausesCount,
                        $"The filter expression exceeds the maximum allowed complexity of {maxComplexity} clauses. This limit exists to prevent Denial of Service (DoS) attacks. Configure PaginationCoreOptions.MaxFilterComplexity to increase the limit.");
                }

                // A-002: Delegate parsing and building to internal isolated classes
                var parsedClause = FilterDslParser.Parse(orSegment.Trim().ToString(), unknownFieldBehavior, customTokens);
                if (parsedClause is { } clauseData)
                {
                    if (allowedSet != null && !allowedSet.Contains(clauseData.FieldName))
                    {
                        if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
                        {
                            throw new InvalidOperationException($"Filtering on property '{clauseData.FieldName}' is not permitted.");
                        }
                        continue;
                    }

                    if (clauseData.Value?.Length > maxFilterValueLength)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(rawValue),
                            clauseData.Value.Length,
                            $"The filter value length exceeds the maximum allowed length of {maxFilterValueLength} characters. This limit exists to prevent Denial of Service (DoS) attacks.");
                    }

                    var clause = FilterPredicateBuilder.Build<T>(param, clauseData, unknownFieldBehavior, allowedSet != null, maxPropertyDepth, customOperatorProvider);
                    if (clause is not null)
                    {
                        orClauses.Add(clause);
                    }
                }
            }

            if (orClauses.Count > 0)
            {
                Expression combinedOr = orClauses[0];
                for (int i = 1; i < orClauses.Count; i++)
                {
                    combinedOr = Expression.OrElse(combinedOr, orClauses[i]);
                }
                clauses.Add(combinedOr);
            }
        }

        if (clauses.Count == 0) return null;

        // AND-combine all top-level clauses
        Expression body = clauses[0];
        for (int i = 1; i < clauses.Count; i++)
        {
            body = Expression.AndAlso(body, clauses[i]);
        }
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}




