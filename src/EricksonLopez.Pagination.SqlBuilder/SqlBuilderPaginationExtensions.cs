// Copyright © Erickson Lopez. MIT License.
extern alias PagAbstractions;
extern alias PagCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.SqlBuilder;
using PaginationParameters = PagAbstractions::EricksonLopez.Pagination.Abstractions.PaginationParameters;
using CursorPaginationParameters = PagAbstractions::EricksonLopez.Pagination.Abstractions.CursorPaginationParameters;
using ICursorEncoder = PagCore::EricksonLopez.Pagination.ICursorEncoder;
using HmacCursorEncoder = PagCore::EricksonLopez.Pagination.HmacCursorEncoder;

namespace EricksonLopez.Pagination.SqlBuilder;

/// <summary>
/// Provides extension methods for applying offset and keyset cursor pagination directly to <see cref="SelectQuery{T}"/> AST queries.
/// </summary>
public static class SqlBuilderPaginationExtensions
{
    /// <summary>
    /// Applies offset pagination limits and offsets to the query based on <see cref="PaginationParameters"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="parameters">The pagination parameters specifying page number and page size.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with limit and offset applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/></exception>
    public static SelectQuery<T> Paginate<T>(this SelectQuery<T> query, PaginationParameters parameters)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(query);

        int pageSize = parameters.PageSize;
        int pageNumber = parameters.Page;
        int offset = (pageNumber - 1) * pageSize;

        return query.Limit(pageSize).Offset(offset);
    }

    /// <summary>
    /// Applies offset pagination limits and offsets to the query using explicit page number and page size.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with limit and offset applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than 1</exception>
    public static SelectQuery<T> Paginate<T>(this SelectQuery<T> query, int pageNumber, int pageSize)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        int offset = (pageNumber - 1) * pageSize;
        return query.Limit(pageSize).Offset(offset);
    }

    /// <summary>
    /// Applies keyset cursor pagination to the query using the specified cursor key selector.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The cursor key type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="parameters">The cursor pagination parameters containing after/before tokens.</param>
    /// <param name="keySelector">The expression identifying the unique ordering key column.</param>
    /// <param name="encoder">The optional cursor encoder. If null, the development default HMAC encoder is used.</param>
    /// <param name="ascending">True for ascending order (default); false for descending order.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with cursor predicates, ordering, and limit (pageSize + 1) applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    public static SelectQuery<T> ApplyCursor<T, TKey>(
        this SelectQuery<T> query,
        CursorPaginationParameters parameters,
        Expression<Func<T, TKey>> keySelector,
        ICursorEncoder? encoder = null,
        bool ascending = true)
        where T : class, new()
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);

        var cursorEncoder = encoder ?? HmacCursorEncoder.DevelopmentDefault;
        int pageSize = parameters.First ?? parameters.Last ?? 10;

        // Determine seek criteria based on forward / backward cursors
        if (!string.IsNullOrEmpty(parameters.After))
        {
            var rawValue = cursorEncoder.Decode(parameters.After);
            if (!string.IsNullOrEmpty(rawValue))
            {
                var convertedValue = (TKey)Convert.ChangeType(rawValue, typeof(TKey), System.Globalization.CultureInfo.InvariantCulture);
                var predicate = BuildSeekPredicate(keySelector, convertedValue, isGreaterThan: ascending);
                query = query.Where(predicate);
            }
        }
        else if (!string.IsNullOrEmpty(parameters.Before))
        {
            var rawValue = cursorEncoder.Decode(parameters.Before);
            if (!string.IsNullOrEmpty(rawValue))
            {
                var convertedValue = (TKey)Convert.ChangeType(rawValue, typeof(TKey), System.Globalization.CultureInfo.InvariantCulture);
                var predicate = BuildSeekPredicate(keySelector, convertedValue, isGreaterThan: !ascending);
                query = query.Where(predicate);
            }
        }

        // Convert keySelector to Expression<Func<T, object>> for OrderBy
        var orderSelector = ConvertKeySelector(keySelector);

        query = ascending
            ? query.OrderBy(orderSelector)
            : query.OrderByDescending(orderSelector);

        // Fetch pageSize + 1 to reliably detect HasNextPage / HasPreviousPage
        return query.Limit(pageSize + 1);
    }

    /// <summary>
    /// Materializes a fetched collection into a strongly-typed <see cref="PagCore::EricksonLopez.Pagination.CursorPagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The cursor key type.</typeparam>
    /// <param name="items">The items fetched from the database (query limit should have been pageSize + 1).</param>
    /// <param name="parameters">The cursor pagination parameters used for the query.</param>
    /// <param name="keySelector">The delegate for extracting the cursor key from an item.</param>
    /// <param name="encoder">The optional cursor encoder.</param>
    /// <returns>A new <see cref="PagCore::EricksonLopez.Pagination.CursorPagedList{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    public static PagCore::EricksonLopez.Pagination.CursorPagedList<T> ToCursorPagedList<T, TKey>(
        this IReadOnlyList<T> items,
        CursorPaginationParameters parameters,
        Func<T, TKey> keySelector,
        ICursorEncoder? encoder = null)
        where T : class
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);
        var cursorEncoder = encoder ?? HmacCursorEncoder.DevelopmentDefault;
        int pageSize = parameters.GetPageSize(10);
        bool hasNextPage = items.Count > pageSize;

        // Stryker disable once all : Equivalent LINQ Take optimization
        var pageItems = hasNextPage
            ? items.Take(pageSize).ToList()
            : items.ToList();

        string? startCursor = pageItems.Count > 0
            ? cursorEncoder.Encode(keySelector(pageItems[0])?.ToString() ?? string.Empty)
            : null;

        string? endCursor = pageItems.Count > 0
            ? cursorEncoder.Encode(keySelector(pageItems[^1])?.ToString() ?? string.Empty)
            : null;

        bool hasPreviousPage = !string.IsNullOrEmpty(parameters.After);

        return new PagCore::EricksonLopez.Pagination.CursorPagedList<T>(
            pageItems,
            startCursor,
            endCursor,
            hasPreviousPage,
            hasNextPage);
    }

    /// <summary>
    /// Materializes a collection and total count into a strongly-typed <see cref="PagCore::EricksonLopez.Pagination.PagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="items">The items of the current page.</param>
    /// <param name="totalCount">The total count of matching records across all pages.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <returns>A new <see cref="PagCore::EricksonLopez.Pagination.PagedList{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    public static PagCore::EricksonLopez.Pagination.PagedList<T> ToPagedList<T>(
        this IEnumerable<T> items,
        int totalCount,
        PaginationParameters parameters)
    {
        // Stryker disable once Statement : Guard clause redundant with PagedList constructor validation
        ArgumentNullException.ThrowIfNull(items);

        // Stryker disable once NullCoalescing : Micro-optimization avoiding allocation when items is already IReadOnlyList<T>
        var list = items as IReadOnlyList<T> ?? items.ToList();
        return new PagCore::EricksonLopez.Pagination.PagedList<T>(list, totalCount, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// Materializes a collection and total count into a strongly-typed <see cref="PagCore::EricksonLopez.Pagination.PagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="items">The items of the current page.</param>
    /// <param name="totalCount">The total count of matching records across all pages.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>A new <see cref="PagCore::EricksonLopez.Pagination.PagedList{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than 1</exception>
    public static PagCore::EricksonLopez.Pagination.PagedList<T> ToPagedList<T>(
        this IEnumerable<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        // Stryker disable once Statement : Guard clause redundant with PagedList constructor validation
        ArgumentNullException.ThrowIfNull(items);
        // Stryker disable once Statement : Guard clause redundant with PagedList constructor validation
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        // Stryker disable once Statement : Guard clause redundant with PagedList constructor validation
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        // Stryker disable once NullCoalescing : Micro-optimization avoiding allocation when items is already IReadOnlyList<T>
        var list = items as IReadOnlyList<T> ?? items.ToList();
        return new PagCore::EricksonLopez.Pagination.PagedList<T>(list, totalCount, pageNumber, pageSize);
    }

    private static Expression<Func<T, object>> ConvertKeySelector<T, TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var parameter = keySelector.Parameters[0];
        var body = Expression.Convert(keySelector.Body, typeof(object));
        return Expression.Lambda<Func<T, object>>(body, parameter);
    }

    private static Expression<Func<T, bool>> BuildSeekPredicate<T, TKey>(
        Expression<Func<T, TKey>> keySelector,
        TKey comparisonValue,
        bool isGreaterThan)
    {
        var parameter = keySelector.Parameters[0];
        var constant = Expression.Constant(comparisonValue, typeof(TKey));
        var comparison = isGreaterThan
            ? Expression.GreaterThan(keySelector.Body, constant)
            : Expression.LessThan(keySelector.Body, constant);

        return Expression.Lambda<Func<T, bool>>(comparison, parameter);
    }
}


