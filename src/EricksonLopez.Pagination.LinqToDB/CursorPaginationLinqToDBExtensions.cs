// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Internal;
using LinqToDB;

namespace EricksonLopez.Pagination.LinqToDB;

/// <summary>
/// Provides extension methods for LinqToDB <see cref="IQueryable{T}"/> to create cursor-paginated and projected results.
/// </summary>

public static class QueryableCursorProjectionExtensions
{
    // Stryker disable all : Deprecated legacy cursor format implementation (F-004). Logic tested in EF Core.
    /// <summary>
    /// Materializes the query into a projected <see cref="ICursorPagedList{TResult}"/> using keyset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type stored in the database.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="selector">A server-side projection expression from <typeparamref name="T"/> to <typeparamref name="TResult"/>.</param>
    /// <param name="keySelector">An expression specifying the cursor column on <typeparamref name="T"/>.</param>
    /// <param name="resultKeySelector">A delegate extracting the cursor key from a projected <typeparamref name="TResult"/> item.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="direction">The sort direction for the keyset column.</param>
    /// <param name="defaultPageSize">The fallback page size when none is specified.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the cursor paged list.
    /// </returns>
    public static async Task<ICursorPagedList<TResult>> ToCursorPagedListAsync<T, TResult, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> keySelector,
        Func<TResult, TKey> resultKeySelector,
        CursorPaginationParameters parameters,
        SortDirection direction = SortDirection.Ascending,
        int defaultPageSize = 10,
        int? maxPageSize = null,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default,
        IPaginationOptions? options = null)
    {
        var actualMaxPageSize = maxPageSize ?? options?.MaxPageSize ?? PaginationSettings.MaxPageSize;
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;

#pragma warning disable IL2026
        bool hasAfter = parameters.TryDecodeAfter<TKey>(out var afterKey, encoder, options?.CursorDecoderRegistry);
        bool hasBefore = parameters.TryDecodeBefore<TKey>(out var beforeKey, encoder, options?.CursorDecoderRegistry);
#pragma warning restore IL2026

        return await ExecuteCursorProjectedQueryAsync(
            source, selector, keySelector, resultKeySelector,
            hasAfter ? (object?)afterKey : null,
            hasBefore ? (object?)beforeKey : null,
            parameters, direction, defaultPageSize, actualMaxPageSize, encoder, cancellationToken)
            // Stryker disable once boolean
            .ConfigureAwait(false);
    }

    private static async Task<ICursorPagedList<TResult>> ExecuteCursorProjectedQueryAsync<T, TResult, TKey>(
        IQueryable<T> source,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> keySelector,
        Func<TResult, TKey> resultKeySelector,
        object? afterCursorValue,
        object? beforeCursorValue,
        CursorPaginationParameters parameters,
        SortDirection direction,
        int defaultPageSize,
        int maxPageSize,
        ICursorEncoder encoder,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(parameters.GetPageSize(defaultPageSize), maxPageSize);
        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        var isAscending = direction == SortDirection.Ascending;

        if (isBackward)
        {
            if (beforeCursorValue is not null)
            {
                var (parameter, property) = GetExpressionParts(keySelector);
                var cursorConstant = Expression.Constant(beforeCursorValue, typeof(TKey));
                var predicate = isAscending
                    ? BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: true)
                    : BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: false);
                source = source.Where(predicate);
            }
            source = isAscending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
        }
        else
        {
            if (afterCursorValue is not null)
            {
                var (parameter, property) = GetExpressionParts(keySelector);
                var cursorConstant = Expression.Constant(afterCursorValue, typeof(TKey));
                var predicate = isAscending
                    ? BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: false)
                    : BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: true);
                source = source.Where(predicate);
            }
            source = isAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
        }

        var projectedQuery = source.Select(selector);
        // Stryker disable once boolean
var items = await projectedQuery.Take(pageSize + 1).ToListAsync(cancellationToken).ConfigureAwait(false);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        if (isBackward)
        {
            items.Reverse();
        }

        string? startCursor = null;
        string? endCursor = null;

        if (items.Count > 0)
        {
            var firstKeyValue = resultKeySelector(items[0]);
            startCursor = encoder.Encode(firstKeyValue!.ToString()!);

            var lastKeyValue = resultKeySelector(items[^1]);
            endCursor = encoder.Encode(lastKeyValue!.ToString()!);
        }

        var hasPreviousPage = isBackward ? hasMore : afterCursorValue is not null;
        var hasNextPage = isBackward ? beforeCursorValue is not null : hasMore;

        return CursorPagedList<TResult>.Create(items, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }

    private static (ParameterExpression Parameter, Expression Body) GetExpressionParts<T, TKey>(
        Expression<Func<T, TKey>> keySelector)
    {
        return (keySelector.Parameters[0], keySelector.Body);
    }

    private static Expression<Func<T, bool>> BuildComparisonPredicate<T, TKey>(
        ParameterExpression parameter,
        Expression property,
        ConstantExpression cursorConstant,
        bool lessThan)
    {
        Expression comparison = null!;
        var keyType = typeof(TKey);

        if (keyType.IsValueType)
        {
            comparison = lessThan
                ? Expression.LessThan(property, cursorConstant)
                : Expression.GreaterThan(property, cursorConstant);
        }
        else
        {
            if (typeof(IComparable<TKey>).IsAssignableFrom(keyType))
            {
                var compareToMethod = typeof(IComparable<TKey>).GetMethod("CompareTo");
                var compareToCall = Expression.Call(property, compareToMethod!, cursorConstant);
                var zero = Expression.Constant(0);
                comparison = lessThan
                    ? Expression.LessThan(compareToCall, zero)
                    : Expression.GreaterThan(compareToCall, zero);
            }
            else
            {
                throw new InvalidOperationException($"Cannot build a keyset cursor comparison for type '{keyType.Name}'.");
            }
        }

        return Expression.Lambda<Func<T, bool>>(comparison, parameter);
    }
    // Stryker restore all
}






