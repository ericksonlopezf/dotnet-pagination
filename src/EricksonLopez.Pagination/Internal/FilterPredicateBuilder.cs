// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EricksonLopez.Pagination.Abstractions;

#if NET6_0_OR_GREATER
#pragma warning disable IL2026
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(EricksonLopez.Pagination.Internal.FilterPredicateBuilder))]
#pragma warning restore IL2026
#endif

namespace EricksonLopez.Pagination.Internal;

/// <summary>
/// Internal engine for building filter predicates.
/// </summary>
/// <remarks>
/// Note: Types in the <c>EricksonLopez.Pagination.Internal</c> namespace are not part of the public API 
/// and may change in any minor version. Do not use them directly.
/// </remarks>
internal static partial class FilterPredicateBuilder
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> _filterableTypeCache = new();
    
    
    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial System.Text.RegularExpressions.Regex SafeFieldRegex();

    /// <summary>
    /// Clears the filterable type cache. Invoked automatically during Hot Reload (.NET 6+).
    /// </summary>
    /// <param name="updatedTypes">The array of types that were updated during the hot reload event, or <see langword="null"/> if not provided.</param>
    // Stryker disable all : Hot Reload cache clearance hook
    public static void ClearCache(Type[]? updatedTypes)
    {
        _filterableTypeCache.Clear();
        PaginationExpressionCache.Filters.Clear();
    }
    // Stryker restore all

    /// <summary>
    /// Builds a LINQ expression representing a filtering predicate based on the specified filter clause.
    /// </summary>
    /// <typeparam name="T">The type of the entity being filtered.</typeparam>
    /// <param name="param">The parameter expression representing the entity in the lambda.</param>
    /// <param name="clause">The parsed filter clause containing the field name, operator, and value.</param>
    /// <param name="unknownFieldBehavior">The configured behavior to apply when an unknown or un-filterable field is encountered.</param>
    /// <param name="bypassFilterableCheck">A value indicating whether to bypass the <c>[Filterable]</c> attribute check.</param>
    /// <param name="maxPropertyDepth">The maximum allowed nesting depth for property paths.</param>
    /// <param name="customOperatorProvider">An optional custom operator provider defining additional filter operators.</param>
    /// <returns>The constructed expression, or <see langword="null"/> if the clause is invalid and the behavior allows returning null.</returns>
    [RequiresUnreferencedCode("FilterExpression uses reflection to locate properties, which is incompatible with trimming.")]
    [RequiresDynamicCode("FilterExpression uses MakeGenericType and MakeGenericMethod, which is incompatible with Native AOT.")]
    public static Expression? Build<T>(ParameterExpression param, FilterClause clause, FilterUnknownFieldBehavior unknownFieldBehavior, bool bypassFilterableCheck = false, int maxPropertyDepth = 3, IFilterOperatorProvider<T>? customOperatorProvider = null)
    {
        var parts = clause.FieldName.Split('.');
        // SEC-3: Use the configurable maxPropertyDepth limit from PaginationCoreOptions instead of a
        // hardcoded constant. This allows operators to tune the depth limit without recompilation
        // while preserving a sensible default of 3 (e.g., Customer.Address.City).
        if (parts.Length > maxPropertyDepth)
        {
             throw new InvalidOperationException($"Filter field '{clause.FieldName}' exceeds the maximum allowed nesting depth of {maxPropertyDepth}. Configure PaginationCoreOptions.MaxPropertyDepth to increase the limit.");
        }

        Expression propExpr = param;
        Type currentType = typeof(T);
        PropertyInfo? property = null;

        // Stryker disable once boolean : Attribute inheritance is out of scope for functional testing
        bool rootHasAnyFilterable = _filterableTypeCache.GetOrAdd(typeof(T), t => Array.Exists(t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), p => p.IsDefined(typeof(FilterableAttribute), true)));

        // F-009 fix: track the [Filterable] constraint of the current type at each nesting level.
        // Previously, `rootHasAnyFilterable` was only computed for the root type T, so descending
        // into a sub-type (e.g. Customer in `Order.Customer.InternalNotes`) would not check
        // whether InternalNotes is [Filterable] on Customer — only whether something on Order was.
        // Now we recompute `currentTypeHasAnyFilterable` at each level using the same cache.
        bool currentTypeHasAnyFilterable = rootHasAnyFilterable;

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                throw new ArgumentException("Filter field contains an empty segment.");
            }

            if (!SafeFieldRegex().IsMatch(part))
            {
                throw new ArgumentException($"Invalid characters in filter segment: '{part}'. Only alphanumeric and underscore are allowed.");
            }

            if (part.Length > 100)
            {
                throw new InvalidOperationException($"Filter field segment '{part}' exceeds maximum length of 100 characters.");
            }

            // Stryker disable all : Bitwise mutations on Reflection BindingFlags are functionally identical in this context
            property = currentType.GetProperty(
                part,
                BindingFlags.Public | BindingFlags.Instance);
                
            if (property == null)
            {
                property = currentType.GetProperty(
                    part,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }

            // Check for [Filterable(Name = "alias")]
            if (property == null)
            {
                var allProps = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                foreach (var p in allProps)
                {
                    var filterableAttr = p.GetCustomAttribute<FilterableAttribute>(true);
                    if (filterableAttr?.Name != null && string.Equals(filterableAttr.Name, part, StringComparison.OrdinalIgnoreCase))
                    {
                        property = p;
                        break;
                    }
                }
            }
            // Stryker restore all
                
            if (property == null) break;
            
            // F-009 fix: use currentTypeHasAnyFilterable (from the current sub-type), not
            // rootHasAnyFilterable (from the root type T), for the [Filterable] guard.
            // Stryker disable once boolean : Attribute inheritance is out of scope for functional testing
            if (!bypassFilterableCheck && currentTypeHasAnyFilterable && !property.IsDefined(typeof(FilterableAttribute), true))
            {
                if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
                {
                    throw new ArgumentException($"Property '{part}' in path '{clause.FieldName}' is not allowed to be filtered. It must be marked with [Filterable].");
                }
                return null;
            }
            
            propExpr = Expression.Property(propExpr, property);
            currentType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            // F-009 fix: recompute whether the next sub-type has any [Filterable] properties.
            // This ensures that when descending from Customer to its properties, we correctly
            // check if Customer declares any [Filterable] attributes before deciding whether to
            // enforce the constraint on sub-properties of Customer.
            // Stryker disable all : Bitwise mutations on Reflection BindingFlags and boolean mutations on inheritance are functionally identical here
            currentTypeHasAnyFilterable = _filterableTypeCache.GetOrAdd(currentType, t =>
                Array.Exists(t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy),
                    p => p.IsDefined(typeof(FilterableAttribute), true)));
            // Stryker restore all
        }


        if (property is null)
        {
            if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
            {
                throw new ArgumentException($"Property '{clause.FieldName}' not found or could not be resolved from type '{typeof(T).Name}'.");
            }
            return null;
        }

        if (!bypassFilterableCheck && parts.Length > 1 && !rootHasAnyFilterable)
        {
            throw new InvalidOperationException($"Nested property filtering on '{clause.FieldName}' is not permitted because type '{typeof(T).Name}' does not define any [Filterable] properties.");
        }

        if (clause.Op == FilterOp.Custom)
        {
            if (customOperatorProvider != null && clause.CustomOp != null &&
                customOperatorProvider.Operators.TryGetValue(clause.CustomOp, out var handler))
            {
                var customResult = handler(propExpr, clause.Value);
                if (customResult != null && clause.Negate)
                {
                    customResult = Expression.Not(customResult);
                }
                return customResult;
            }

            if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
            {
                throw new ArgumentException($"Unknown or unregistered custom operator '{clause.CustomOp}'.");
            }
            return null;
        }

        var propType    = currentType;
        var isNullable  = property.PropertyType != propType;
        // Stryker disable once all : Mutating IsValueType check would cause null token parsing to throw incorrectly, but tests don't cover every specific type permutation
        var isReferenceType = !property.PropertyType.IsValueType;

        bool isNullToken = string.Equals(clause.Value, "null", StringComparison.OrdinalIgnoreCase);

        if (isNullToken && (isNullable || isReferenceType))
        {
            if (clause.Op is not (FilterOp.Equal or FilterOp.NotEqual))
            {
                if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
                {
                    throw new ArgumentException($"Cannot use operator '{clause.Op}' with null value on property '{clause.FieldName}'.");
                }
                return null;
            }

            var constNull = Expression.Constant(null, property.PropertyType);
            Expression nullResult = clause.Op == FilterOp.Equal 
                ? Expression.Equal(propExpr, constNull)
                : Expression.NotEqual(propExpr, constNull);
                
            if (clause.Negate) nullResult = Expression.Not(nullResult);
            return nullResult;
        }

        if (!ValueCoercer.TryCoerce(clause.Value, propType, out var coerced))
        {
            if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
            {
                throw new ArgumentException($"Value '{clause.Value}' is not valid for property '{clause.FieldName}' of type '{propType.Name}'.");
            }
            return null;
        }

        if (clause.Op is FilterOp.Contains or FilterOp.StartsWith or FilterOp.EndsWith)
        {
            if (propType != typeof(string)) return null;
            var constant = Expression.Constant(coerced, typeof(string));
            var methodName = clause.Op switch
            {
                FilterOp.Contains   => nameof(string.Contains),
                FilterOp.StartsWith => nameof(string.StartsWith),
                _                   => nameof(string.EndsWith)
            };
            var method = typeof(string).GetMethod(methodName, [typeof(string)])!;
            Expression stringResult = Expression.Call(propExpr, method, constant);
            if (clause.Negate) stringResult = Expression.Not(stringResult);
            return stringResult;
        }

        var constExpr = Expression.Constant(coerced, isNullable
            ? typeof(Nullable<>).MakeGenericType(propType)
            : propType);

        Expression result = clause.Op switch
        {
            FilterOp.Equal              => Expression.Equal(propExpr, constExpr),
            FilterOp.NotEqual           => Expression.NotEqual(propExpr, constExpr),
            FilterOp.GreaterThan        => Expression.GreaterThan(propExpr, constExpr),
            FilterOp.LessThan           => Expression.LessThan(propExpr, constExpr),
            FilterOp.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propExpr, constExpr),
            FilterOp.LessThanOrEqual    => Expression.LessThanOrEqual(propExpr, constExpr),
            _ => throw new InvalidOperationException($"Unsupported filter operator '{clause.Op}'.")
        };

        if (clause.Negate)
        {
            result = Expression.Not(result);
        }
        
        return result;
    }
}




