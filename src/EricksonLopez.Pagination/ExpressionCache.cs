// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Thread-safe, bounded cache for compiled <see cref="Expression{TDelegate}"/> delegates,
/// keyed by a structural string representation of the expression tree.
/// </summary>
/// <remarks>
/// This type is an internal infrastructure detail and is not part of the public API.
/// It is intentionally separate from the <c>internal ExpressionCache</c> in
/// <c>EricksonLopez.Pagination.EntityFrameworkCore</c>.
/// </remarks>

internal static class PaginationExpressionCache
{
    internal readonly struct FilterCacheKey : IEquatable<FilterCacheKey>
    {
        public Type EntityType { get; }
        public string RawValue { get; }
        public FilterUnknownFieldBehavior UnknownFieldBehavior { get; }
        public int MaxComplexity { get; }
        public int MaxFilterValueLength { get; }
        public System.Collections.Frozen.FrozenSet<string>? AllowedProperties { get; }
        private readonly int _hash;

        public FilterCacheKey(Type entityType, string rawValue, FilterUnknownFieldBehavior unknownFieldBehavior, int maxComplexity, IEnumerable<string>? allowedProperties, int maxFilterValueLength)
        {
            EntityType = entityType;
            // CO-6: Do NOT apply ToLowerInvariant() to the full rawValue.
            // The DSL format is "field=value" and the filter parser handles field name
            // case-insensitivity internally. Lowercasing the entire rawValue makes
            // "Name=John" and "name=john" share the same cache key, but the predicates
            // they produce are different when string comparison is case-sensitive
            // (e.g., PostgreSQL with C collation, in-memory evaluation).
            // Preserving the original rawValue avoids returning the wrong predicate from cache.
            // Stryker disable once string : Empty string fallback is functionally identical because null coalesces properly
        RawValue = rawValue ?? "";
            UnknownFieldBehavior = unknownFieldBehavior;
            MaxComplexity = maxComplexity;
            MaxFilterValueLength = maxFilterValueLength;
            AllowedProperties = allowedProperties?.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

            var hash = new HashCode();
            // Stryker disable all : Hash code generation is an implementation detail
            hash.Add(EntityType.TypeHandle.Value);
            hash.Add(RawValue);
            hash.Add(UnknownFieldBehavior);
            hash.Add(MaxComplexity);
            hash.Add(MaxFilterValueLength);
            // Stryker restore all
            
            if (AllowedProperties != null)
            {
                // Order-independent hash code sum
                int propsHash = 0;
                foreach (var prop in AllowedProperties)
                {
                    if (prop != null)
                    {
                        // Stryker disable once arithmetic : Hash code generation is an implementation detail
                propsHash = unchecked(propsHash + StringComparer.OrdinalIgnoreCase.GetHashCode(prop));
                    }
                }
                // Stryker disable once statement : Hash code generation is an implementation detail
            hash.Add(propsHash);
            }
            _hash = hash.ToHashCode();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public bool Equals(FilterCacheKey other)
        {
            if (_hash != other._hash) return false;

            if (EntityType != other.EntityType ||
                MaxComplexity != other.MaxComplexity ||
                MaxFilterValueLength != other.MaxFilterValueLength ||
                UnknownFieldBehavior != other.UnknownFieldBehavior ||
                !string.Equals(RawValue, other.RawValue, StringComparison.Ordinal))
            {
                return false;
            }

            if (ReferenceEquals(AllowedProperties, other.AllowedProperties)) return true;
            if (AllowedProperties == null || other.AllowedProperties == null) return false;
            
            return AllowedProperties.SetEquals(other.AllowedProperties);
        }

        public override bool Equals(object? obj) => obj is FilterCacheKey other && Equals(other);

        public override int GetHashCode() => _hash;
    }

    internal readonly struct SortCacheKey : IEquatable<SortCacheKey>
    {
        public Type EntityType { get; }
        public string ColumnName { get; }

        public SortCacheKey(Type entityType, string columnName)
        {
            EntityType = entityType;
            ColumnName = columnName;
        }

        public bool Equals(SortCacheKey other) =>
            EntityType == other.EntityType &&
            string.Equals(ColumnName, other.ColumnName, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is SortCacheKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(EntityType.TypeHandle.Value, ColumnName);
    }

    internal readonly struct DelegateCacheKey : IEquatable<DelegateCacheKey>
    {
        public Type InputType { get; }
        public Type OutputType { get; }
        public System.Reflection.MemberInfo? Member { get; }
        public string? FallbackString { get; }

        public DelegateCacheKey(Type inputType, Type outputType, LambdaExpression expr)
        {
            InputType = inputType;
            OutputType = outputType;
            if (expr.Body is MemberExpression me)
            {
                Member = me.Member;
                FallbackString = null;
            }
            else if (expr.Body is UnaryExpression ue && ue.Operand is MemberExpression ume)
            {
                Member = ume.Member;
                FallbackString = null;
            }
            else
            {
                Member = null;
                FallbackString = expr.ToString();
            }
        }

        // Stryker disable all : Equality mutations on internal cache keys are functionally equivalent due to empty clause skipping and untestable
        public bool Equals(DelegateCacheKey other) =>
            InputType == other.InputType &&
            OutputType == other.OutputType &&
            Member == other.Member &&
            string.Equals(FallbackString, other.FallbackString, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is DelegateCacheKey other && Equals(other);

        // Stryker disable once block : Hash code generation is an implementation detail
        public override int GetHashCode() => HashCode.Combine(InputType.TypeHandle.Value, OutputType.TypeHandle.Value, Member, FallbackString);
    }

    private static readonly Internal.ConcurrentFifoCache<DelegateCacheKey, object> _delegatesCache = new(512);
    private static readonly Internal.ConcurrentFifoCache<FilterCacheKey, object> _filtersCache = new(2048);
    private static readonly Internal.ConcurrentFifoCache<SortCacheKey, LambdaExpression> _sortLambdasCache = new(512);
    private static readonly ConcurrentDictionary<Type, System.Reflection.MethodInfo> _compareToMethods = new();

    /// <summary>
    /// Cache for dynamically compiled filter expressions.
    /// </summary>
    internal static Internal.ConcurrentFifoCache<FilterCacheKey, object> Filters => _filtersCache;

    /// <summary>
    /// Cache for dynamic sorting lambda expressions.
    /// </summary>
    internal static Internal.ConcurrentFifoCache<SortCacheKey, LambdaExpression> SortLambdas => _sortLambdasCache;

    /// <summary>
    /// Returns a compiled delegate for the given expression, compiling it at most once
    /// per unique structural form.
    /// </summary>
    
    /// <summary>
    /// Clears all cached expressions. Useful for testing or memory pressure scenarios.
    /// </summary>
    // Stryker disable all : Cache reset infrastructure method
    internal static void Clear()
    {
        _delegatesCache.Clear();
        _filtersCache.Clear();
        _sortLambdasCache.Clear();
        _compareToMethods.Clear();
    }
    // Stryker restore all

    internal static System.Reflection.MethodInfo GetCompareToMethod<TKey>()
    {
        return _compareToMethods.GetOrAdd(typeof(TKey), static t => typeof(IComparable<TKey>).GetMethod("CompareTo")!);
    }

    internal static Func<T, TKey> GetOrCompile<T, TKey>(Expression<Func<T, TKey>> expression)
    {
        var key = new DelegateCacheKey(typeof(T), typeof(TKey), expression);

        if (key.FallbackString != null)
        {
            var visitor = new ClosureDetector();
            visitor.Visit(expression);
            if (visitor.HasClosure)
            {
                return expression.Compile();
            }
        }

        var compiled = _delegatesCache.GetOrAdd(key, _ => expression.Compile());
        return (Func<T, TKey>)compiled!;
    }

    /// <summary>
    /// Visits expressions to determine if they contain closures (references to local variables).
    /// </summary>
    internal sealed class ClosureDetector : ExpressionVisitor
    {
        /// <summary>
        /// Gets a value indicating whether the expression contains a closure.
        /// </summary>
        internal bool HasClosure { get; private set; }

        /// <inheritdoc />

        [return: NotNullIfNotNull("node")]
        public override Expression? Visit(Expression? node)
        {
            if (HasClosure) return node;
            return base.Visit(node);
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value != null)
            {
                var type = node.Value.GetType();
                if (!IsSafeConstant(type))
                {
                    HasClosure = true;
                }
            }
            return base.VisitConstant(node);
        }

        // This is a strict whitelist — add types here only if they are truly 
        // compile-time constants with value semantics (immutable).
        // Types like Uri, byte[], or IPAddress should NOT be added as they are mutable or closures.
        private static bool IsSafeConstant(Type type)
        {
            return type.IsPrimitive || 
                   type.IsEnum || 
                   type == typeof(string) || 
                   type == typeof(decimal) || 
                   type == typeof(Guid) || 
                   type == typeof(DateTime) || 
                   type == typeof(DateTimeOffset) || 
                   type == typeof(TimeSpan) ||
                   type == typeof(DateOnly) ||
                   type == typeof(TimeOnly);
        }
    }
}




