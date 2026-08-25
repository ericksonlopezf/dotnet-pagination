// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace EricksonLopez.Pagination.MongoDB;

/// <summary>
/// Provides sorting and filtering extensions for MongoDB <see cref="IQueryable{T}"/>.
/// </summary>
[RequiresUnreferencedCode("EricksonLopez.Pagination.MongoDB relies on reflection-heavy features which are not fully compatible with NativeAOT.")]
public static class MongoQueryableFilterExtensions
{

    /// <summary>
    /// Applies dynamic multi-column sorting to the queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to sort.</param>
    /// <param name="sortBy">The sort parameters containing property names and directions.</param>
    /// <param name="direction">The default sorting direction if not specified in sort parameters.</param>
    /// <param name="defaultSort">An optional fallback sorting expression.</param>
    /// <returns>An <see cref="IQueryable{T}"/> with ordering applied.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("ApplySort uses reflection to find the property, which is incompatible with trimming.")]
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        SortParameters sortBy,
        EricksonLopez.Pagination.Abstractions.SortDirection direction = EricksonLopez.Pagination.Abstractions.SortDirection.Ascending,
        Expression<Func<T, object>>? defaultSort = null)
    {
        if (!sortBy.HasValue)
        {
            if (defaultSort != null)
            {
                return direction == EricksonLopez.Pagination.Abstractions.SortDirection.Ascending
                    ? source.OrderBy(defaultSort)
                    : source.OrderByDescending(defaultSort);
            }
            return source;
        }

        var parts = sortBy.Value!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        IQueryable<T> currentSource = source;
        bool first = true;

        foreach (var p in parts)
        {
            var part = p.Trim();
            // Stryker disable once all : Short-circuit empty part optimization
            if (string.IsNullOrEmpty(part)) continue;

            var colName = part;
            var colDir = direction;

            var spaceIndex = part.LastIndexOf(' ');
            // Stryker disable once all : Space index prefix boundary check
            if (spaceIndex > 0)
            {
                var suffix = part.Substring(spaceIndex + 1).ToLowerInvariant();
                if (suffix == "asc")
                {
                    colDir = EricksonLopez.Pagination.Abstractions.SortDirection.Ascending;
                    colName = part.Substring(0, spaceIndex).Trim();
                }
                else if (suffix == "desc")
                {
                    colDir = EricksonLopez.Pagination.Abstractions.SortDirection.Descending;
                    colName = part.Substring(0, spaceIndex).Trim();
                }
            }

            LambdaExpression lambda = null!;
            try
            {
                // Stryker disable once all : Guard clause
                if (colName.Length > 128)
                {
                    throw new InvalidOperationException($"Field '{colName}' exceeds maximum allowed length of 128 characters.");
                }

                var cacheKey = new EricksonLopez.Pagination.PaginationExpressionCache.SortCacheKey(typeof(T), colName);
                lambda = PaginationExpressionCache.SortLambdas.GetOrAdd(cacheKey, _ =>
                {
                    // Stryker disable once all : Expression parameter name string literal
                    var p = Expression.Parameter(typeof(T), "x");
                    try
                    {
                        var property = Expression.PropertyOrField(p, colName);
                        return Expression.Lambda(property, p);
                    }
                    catch (ArgumentException ex)
                    {
                        // Stryker disable once all : Exception message formatting
                        throw new InvalidOperationException($"Field '{colName}' not found.", ex);
                    }
                })!;
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            
            string methodName;
            if (first)
            {
                methodName = colDir == EricksonLopez.Pagination.Abstractions.SortDirection.Ascending ? "OrderBy" : "OrderByDescending";
                first = false;
            }
            else
            {
                methodName = colDir == EricksonLopez.Pagination.Abstractions.SortDirection.Ascending ? "ThenBy" : "ThenByDescending";
            }

            // Stryker disable all : Dynamic reflection-based Queryable Expression call construction
            var methodCallExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), lambda.ReturnType },
                currentSource.Expression,
                Expression.Quote(lambda));

            // IQueryable implements IQueryable, so Provider.CreateQuery works
            currentSource = currentSource.Provider.CreateQuery<T>(methodCallExpression);
            // Stryker restore all
        }

        if (first && defaultSort != null)
        {
            return direction == EricksonLopez.Pagination.Abstractions.SortDirection.Ascending
                ? source.OrderBy(defaultSort)
                : source.OrderByDescending(defaultSort);
        }

        return currentSource;
    }

    /// <summary>
    /// Applies dynamic string filtering to the queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to filter.</param>
    /// <param name="filter">The filter parameters.</param>
    /// <param name="maxComplexity">The maximum allowed clause complexity for the filter expression.</param>
    /// <param name="unknownFieldBehavior">The behavior when an unrecognized field is encountered.</param>
    /// <returns>The filtered <see cref="IQueryable{T}"/>, or the original queryable if no filter is specified.</returns>
    [RequiresUnreferencedCode("ApplyFilter uses reflection to locate entity properties by name, which is incompatible with trimming.")]
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> source,
        FilterParameters filter,
        int maxComplexity = 20,
        FilterUnknownFieldBehavior unknownFieldBehavior = FilterUnknownFieldBehavior.ThrowException)
    {
        if (!filter.HasValue) return source;
        // Use the centralized FilterExpression from Core
        var predicate = FilterExpression.Build<T>(filter, maxComplexity, unknownFieldBehavior);
        return predicate is not null ? source.Where(predicate) : source;
    }
}

