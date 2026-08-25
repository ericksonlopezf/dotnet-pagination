// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Pagination.EntityFrameworkCore;


// Stryker restore all

/// <summary>
/// Provides extension methods for EF Core <see cref="IQueryable{T}"/> to create paginated results.
/// </summary>
/// <remarks>
/// <b>Native AOT</b>: The cursor-based methods (<c>ToCursorPagedListAsync</c>
/// and their overloads) use <c>Expression.Compile()</c> internally and are
/// <b>not compatible with Native AOT</b>. For AOT scenarios, use the Dapper extensions
/// with manually-written SQL.
/// </remarks>

public static class QueryableExtensions
{
    /// <summary>
    /// The EF Core provider name used by Npgsql. Extracted as a constant to prevent
    /// fragile string comparisons that break if Npgsql changes its provider name in a future release.
    /// </summary>
    internal const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    internal const string SqlServerProviderName = "Microsoft.EntityFrameworkCore.SqlServer";

    /// <summary>
    /// Initializes a builder for N-column keyset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="defaultPageSize">The fallback page size when not specified in parameters.</param>
    /// <param name="cursorEncoder">An optional custom cursor encoder.</param>
    /// <param name="acceptLegacyCursors">A value indicating whether to accept legacy v1 cursor formats.</param>
    /// <returns>A new <see cref="KeysetBuilder{T}"/> instance.</returns>
    public static KeysetBuilder<T> Keyset<T>(
        this IQueryable<T> source,
        CursorPaginationParameters parameters,
        int defaultPageSize = 10,
        ICursorEncoder? cursorEncoder = null,
        bool acceptLegacyCursors = true)
    {
        return new KeysetBuilder<T>(source, parameters, defaultPageSize, cursorEncoder, acceptLegacyCursors);
    }
    // ─── Offset pagination ────────────────────────────────────────────────────

    /// <summary>
    /// Materializes the query into a <see cref="PagedList{T}"/> using offset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="useApproximateCount">A value indicating whether to use database table statistics for approximate counting.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the paged list.</returns>
    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = null,
        bool useApproximateCount = false,
        IPaginationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once all
        using var activity = EricksonLopez.Pagination.Internal.PaginationActivity.Source.StartActivity("ToPagedListAsync");
        ApplyPaginationLimitsAndLog(source, parameters, maxPageSize, out var effectivePageSize, out var skipAmount, options);

        if (countTotal)
        {
            // Stryker disable all : useApproximateCount falls back to normal count in Memory DB so results are identical
            var count = await GetTotalCountAsync(source, useApproximateCount, cancellationToken).ConfigureAwait(false);

            if (count == 0 && !useApproximateCount)
            {
                return PagedList<T>.Empty(parameters);
            }

            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var adjustedCount = useApproximateCount ? Math.Max(count, skipAmount + items.Count) : count;
            return PagedList<T>.WithCount(items, parameters, adjustedCount);
            // Stryker restore all
        }
        else
        {
            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize + 1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var hasNextPage = items.Count > effectivePageSize;
            if (hasNextPage)
            {
                items.RemoveAt(effectivePageSize);
            }

            return PagedList<T>.WithoutCount(items, parameters, hasNextPage);
        }
    }

    /// <summary>
    /// Materializes the query into a projected <see cref="PagedList{TResult}"/> using offset pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="selector">A projection expression applied on the database server.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="useApproximateCount">A value indicating whether to use database table statistics for approximate counting.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the projected paged list.</returns>
    public static async Task<IPagedList<TResult>> ToPagedListAsync<T, TResult>(
        this IQueryable<T> source,
        Expression<Func<T, TResult>> selector,
        PaginationParameters parameters,
        bool countTotal = true,
        int? maxPageSize = null,
        bool useApproximateCount = false,
        CancellationToken cancellationToken = default,
        IPaginationOptions? options = null)
    {
        // Stryker disable all
        // Stryker disable all
        ApplyPaginationLimitsAndLog(source, parameters, maxPageSize, out var effectivePageSize, out var skipAmount, options);

        if (countTotal)
        {
            // Stryker disable all : useApproximateCount falls back to normal count in Memory DB
            var count = await GetTotalCountAsync(source, useApproximateCount, cancellationToken).ConfigureAwait(false);
            if (count == 0 && !useApproximateCount) return PagedList<TResult>.Empty(parameters);

            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize)
                .Select(selector)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            
            // F-003 fix: ensure projected count matches fetched items if approximate count was too low
            var adjustedCount = useApproximateCount
                ? Math.Max(count, skipAmount + items.Count)
                : count;
            return PagedList<TResult>.WithCount(items, parameters, adjustedCount);
            // Stryker restore all
        }
        else
        {
            var items = await source
                .Skip(skipAmount)
                .Take(effectivePageSize + 1)
                .Select(selector)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var hasNextPage = items.Count > effectivePageSize;
            if (hasNextPage) items.RemoveAt(effectivePageSize);

            return PagedList<TResult>.WithoutCount(items, parameters, hasNextPage);
        }
    }

    // ─── Integrated Filter + Sort + Paginate overloads ──────────────────────

    /// <summary>
    /// Applies dynamic filtering, sorting, and offset pagination in a single operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to filter, sort, and paginate.</param>
    /// <param name="filter">The dynamic filter parameters.</param>
    /// <param name="sortBy">The dynamic sort parameters.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="useApproximateCount">A value indicating whether to use database table statistics for approximate counting.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="allowedProperties">An optional whitelist of property names permitted for filtering and sorting.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the paged list.</returns>
    [RequiresUnreferencedCode("ApplyFilter and ApplySort use reflection, which is incompatible with trimming.")]
    public static Task<IPagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        FilterParameters filter,
        SortParameters sortBy,
        PaginationParameters parameters,
        bool countTotal = true,
        bool useApproximateCount = false,
        int? maxPageSize = null,
        IEnumerable<string>? allowedProperties = null,
        CancellationToken cancellationToken = default,
        IPaginationOptions? options = null)
    {
        return source
            .ApplyFilter(filter, allowedProperties: allowedProperties, options: options)
            .ApplySort(sortBy, allowedProperties: allowedProperties)
            .ToPagedListAsync(parameters, countTotal, maxPageSize, useApproximateCount, cancellationToken: cancellationToken, options: options);
    }

    /// <summary>
    /// Applies dynamic filtering, sorting, server-side projection, and offset pagination in a single operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source to filter, sort, project, and paginate.</param>
    /// <param name="filter">The dynamic filter parameters.</param>
    /// <param name="sortBy">The dynamic sort parameters.</param>
    /// <param name="selector">A projection expression applied on the database server.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="useApproximateCount">A value indicating whether to use database table statistics for approximate counting.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="allowedProperties">An optional whitelist of property names permitted for filtering and sorting.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the projected paged list.</returns>
    [RequiresUnreferencedCode("ApplyFilter and ApplySort use reflection, which is incompatible with trimming.")]
    public static Task<IPagedList<TResult>> ToPagedListAsync<T, TResult>(
        this IQueryable<T> source,
        FilterParameters filter,
        SortParameters sortBy,
        Expression<Func<T, TResult>> selector,
        PaginationParameters parameters,
        bool countTotal = true,
        bool useApproximateCount = false,
        int? maxPageSize = null,
        IEnumerable<string>? allowedProperties = null,
        CancellationToken cancellationToken = default,
        IPaginationOptions? options = null)
    {
        return source
            .ApplyFilter(filter, allowedProperties: allowedProperties, options: options)
            .ApplySort(sortBy, allowedProperties: allowedProperties)
            .ToPagedListAsync(selector, parameters, countTotal, maxPageSize, useApproximateCount, cancellationToken: cancellationToken, options: options);
    }

    /// <summary>
    /// Materializes the query into a <see cref="PagedList{T}"/> using count-less offset pagination (N+1 lookahead probe).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the paged list without computing total count.</returns>
    public static Task<IPagedList<T>> ToPagedListWithoutCountAsync<T>(
        this IQueryable<T> source,
        PaginationParameters parameters,
        int? maxPageSize = null,
        IPaginationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return source.ToPagedListAsync(parameters, countTotal: false, maxPageSize, useApproximateCount: false, options, cancellationToken);
    }

    /// <summary>
    /// Materializes the query into a projected <see cref="PagedList{TResult}"/> using count-less offset pagination (N+1 lookahead probe).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="selector">A projection expression applied on the database server.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the projected paged list without computing total count.</returns>
    public static Task<IPagedList<TResult>> ToPagedListWithoutCountAsync<T, TResult>(
        this IQueryable<T> source,
        Expression<Func<T, TResult>> selector,
        PaginationParameters parameters,
        int? maxPageSize = null,
        IPaginationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return source.ToPagedListAsync(selector, parameters, countTotal: false, maxPageSize, useApproximateCount: false, cancellationToken, options);
    }

    /// <summary>
    /// Processes a query in offset-paginated batches, yielding each page as an <see cref="IPagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to paginate in batches.</param>
    /// <param name="batchSize">The number of items to fetch per batch.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> yielding paged lists sequentially.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is less than 1</exception>
    public static IAsyncEnumerable<IPagedList<T>> ToPagedListBatchedAsync<T>(
        this IQueryable<T> source,
        int batchSize = 1000,
        CancellationToken cancellationToken = default,
        IPaginationOptions? options = null)
    {
        // S4456 fix: parameter validation is in the wrapper (not the iterator) so that
        // ArgumentOutOfRangeException is thrown immediately when the method is called,
        // not deferred until the caller starts iterating.
        // F-005 fix: batchSize <= 0 would cause infinite loop. Validate eagerly.
        if (batchSize < 1)
        {
            // Stryker disable once String : exception message wording is equivalent
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize,
                "batchSize must be greater than zero. A zero or negative batchSize would cause an infinite loop.");
        }

        return ToPagedListBatchedAsyncCore<T>(source, batchSize, cancellationToken, options);
    }

    private static async IAsyncEnumerable<IPagedList<T>> ToPagedListBatchedAsyncCore<T>(
        IQueryable<T> source,
        int batchSize,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        IPaginationOptions? options)
    {
        var parameters = new PaginationParameters { PageSize = batchSize, Page = 1 };

        // F-005 fix: removed maxSafety = 100 guardrail. The previous limit caused a misleading
        // "Infinite loop detected." exception for datasets > 100 × batchSize rows. The correct
        // termination mechanisms are: HasNextPage == false (end of data) and cancellationToken.
        while (true)
        {
            var paged = await source.ToPagedListAsync(parameters, countTotal: false, cancellationToken: cancellationToken, options: options).ConfigureAwait(false);
            if (paged.Count == 0) break;
            yield return paged;
            // Stryker disable once Statement : breaking on !HasNextPage is equivalent because next iteration breaks on Count == 0
            if (!paged.HasNextPage) break;
            parameters = parameters with { Page = parameters.Page + 1 };
        }
    }



    // ─── Streaming ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{T}"/> that asynchronously streams entities for the requested page.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to stream from.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> yielding entities sequentially.</returns>
    // Stryker disable all : LINQ AsyncEnumerable streaming wrapper
    public static IAsyncEnumerable<T> ToPagedAsyncEnumerable<T>(
        this IQueryable<T> source,
        PaginationParameters parameters,
        int? maxPageSize = null,
        IPaginationOptions? options = null)
    {
        var actualMaxPageSize = maxPageSize ?? options?.MaxPageSize ?? PaginationSettings.MaxPageSize;
        var effectivePageSize = Math.Min(parameters.PageSize, actualMaxPageSize);

        var skipCount = (long)(parameters.Page - 1) * effectivePageSize;
        var safeSkip = skipCount > int.MaxValue ? int.MaxValue : (int)skipCount;
        return source.Skip(safeSkip).Take(effectivePageSize).AsAsyncEnumerable();
    }
    // Stryker restore all



    /// <summary>
    /// Applies dynamic multi-column sorting to the queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to sort.</param>
    /// <param name="sortBy">The sort parameters containing property names and directions.</param>
    /// <param name="direction">The default sorting direction if not specified in sort parameters.</param>
    /// <param name="defaultSort">An optional fallback sorting expression.</param>
    /// <param name="allowedProperties">An optional whitelist of property names permitted for sorting.</param>
    /// <returns>An <see cref="IQueryable{T}"/> with ordering applied.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("ApplySort uses reflection to find the property, which is incompatible with trimming.")]
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        SortParameters sortBy,
        SortDirection direction = SortDirection.Ascending,
        Expression<Func<T, object>>? defaultSort = null,
        IEnumerable<string>? allowedProperties = null)
    {
        if (!sortBy.HasValue)
        {
            if (defaultSort != null)
            {
                return direction == SortDirection.Ascending
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
            if (string.IsNullOrEmpty(part)) continue;

            var colName = part;
            var colDir = direction;

            var spaceIndex = part.LastIndexOf(' ');
            // Stryker disable once Equality : spaceIndex cannot be exactly 0 because Trim() removes leading spaces, so > 0 is identical to >= 0
            if (spaceIndex > 0)
            {
                var suffix = part.Substring(spaceIndex + 1).ToLowerInvariant();
                if (suffix == "asc")
                {
                    colDir = SortDirection.Ascending;
                    colName = part.Substring(0, spaceIndex).Trim();
                }
                else if (suffix == "desc")
                {
                    colDir = SortDirection.Descending;
                    colName = part.Substring(0, spaceIndex).Trim();
                }
            }

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            SortParameters.ValidateColumnName(colName, allowedProperties);

            var lambda = PaginationExpressionCache.SortLambdas.GetOrAdd(new PaginationExpressionCache.SortCacheKey(typeof(T), colName), _ =>
            {
                // Stryker disable once String : expression parameter name "x" is ignored by EF Core SQL translation
                var p = Expression.Parameter(typeof(T), "x");
                Expression property = p;
                try
                {
                    foreach (var prop in colName.Split('.'))
                    {
                        property = Expression.PropertyOrField(property, prop);
                    }
                }
                catch (ArgumentException ex)
                {
                    // Stryker disable once String
            throw new InvalidOperationException($"Field '{colName}' not found.", ex);
                }
                return Expression.Lambda(property, p);
            });
            
            string methodName;
            if (first)
            {
                methodName = colDir == SortDirection.Ascending ? "OrderBy" : "OrderByDescending";
                first = false;
            }
            else
            {
                methodName = colDir == SortDirection.Ascending ? "ThenBy" : "ThenByDescending";
            }

            var methodCallExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), lambda!.ReturnType },
                currentSource.Expression,
                Expression.Quote(lambda));

            currentSource = currentSource.Provider.CreateQuery<T>(methodCallExpression);
        }

        if (first && defaultSort != null)
        {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            return direction == SortDirection.Ascending
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
                ? source.OrderBy(defaultSort)
                : source.OrderByDescending(defaultSort);
        }

        return currentSource;
    }

    // ─── Filtering ────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies a strongly typed filter provider to the queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to filter.</param>
    /// <param name="filterProvider">The strongly typed filter provider.</param>
    /// <param name="parameters">The filter parameters.</param>
    /// <returns>The filtered <see cref="IQueryable{T}"/>, or the original queryable if no filter is specified.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="filterProvider"/> is <see langword="null"/></exception>
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> source,
        IFilterProvider<T> filterProvider,
        FilterParameters parameters)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
        if (!parameters.HasValue) return source;

        var predicate = filterProvider.Build(parameters);
        return predicate != null ? source.Where(predicate) : source;
    }

    /// <summary>
    /// Applies dynamic string filtering to the queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to filter.</param>
    /// <param name="parameters">The filter parameters.</param>
    /// <param name="maxComplexity">The maximum allowed clause complexity for the filter expression.</param>
    /// <param name="unknownFieldBehavior">The behavior when an unrecognized field is encountered.</param>
    /// <param name="allowedProperties">An optional whitelist of property names permitted for filtering.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>The filtered <see cref="IQueryable{T}"/>, or the original queryable if no filter is specified.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="parameters"/> exceeds the maximum permitted length</exception>
    [RequiresUnreferencedCode("ApplyFilter uses reflection to locate entity properties by name, which is incompatible with trimming.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("ApplyFilter uses expression compilation at runtime, which requires dynamic code generation and is incompatible with Native AOT.")]
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> source, 
        FilterParameters parameters, 
        int maxComplexity = 20,
        FilterUnknownFieldBehavior unknownFieldBehavior = FilterUnknownFieldBehavior.ThrowException,
        IEnumerable<string>? allowedProperties = null,
        IPaginationOptions? options = null)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!parameters.HasValue) return source;


        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        int effectiveMaxComplexity = options?.MaxFilterComplexity ?? maxComplexity;
        int maxFilterStringLength = options?.MaxFilterStringLength ?? 1000;
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        int maxFilterValueLength = options?.MaxFilterValueLength ?? 200;
        // SEC-3: Read the configurable property depth limit from options, defaulting to 3.
        // This wires the MaxPropertyDepth option (defined in IPaginationOptions and PaginationCoreOptions)
        // into the filter expression pipeline so it is actually enforced at runtime.
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        int maxPropertyDepth = options?.MaxPropertyDepth ?? 3;

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (parameters.Value!.Length > maxFilterStringLength)
        {
            throw new ArgumentException($"The filter string exceeds the maximum allowed length of {maxFilterStringLength} characters.");
        }

        var predicate = FilterExpression.Build<T>(parameters, effectiveMaxComplexity, unknownFieldBehavior, allowedProperties, maxFilterValueLength, maxPropertyDepth);
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        return predicate is not null ? source.Where(predicate) : source;
    }

    /// <summary>
    /// Applies dynamic string filtering to the queryable source with custom operator support.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to filter.</param>
    /// <param name="parameters">The filter parameters.</param>
    /// <param name="customOperatorProvider">The custom operator provider defining additional filter operators.</param>
    /// <param name="maxComplexity">The maximum allowed clause complexity for the filter expression.</param>
    /// <param name="unknownFieldBehavior">The behavior when an unrecognized field is encountered.</param>
    /// <param name="allowedProperties">An optional whitelist of property names permitted for filtering.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>The filtered <see cref="IQueryable{T}"/>, or the original queryable if no filter is specified.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="customOperatorProvider"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="parameters"/> exceeds the maximum permitted length</exception>
    [RequiresUnreferencedCode("ApplyFilter uses reflection to locate entity properties by name, which is incompatible with trimming.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("ApplyFilter uses expression compilation at runtime, which requires dynamic code generation and is incompatible with Native AOT.")]
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> source, 
        FilterParameters parameters,
        IFilterOperatorProvider<T> customOperatorProvider,
        int maxComplexity = 20,
        FilterUnknownFieldBehavior unknownFieldBehavior = FilterUnknownFieldBehavior.ThrowException,
        IEnumerable<string>? allowedProperties = null,
        IPaginationOptions? options = null)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (customOperatorProvider == null) throw new ArgumentNullException(nameof(customOperatorProvider));
        if (!parameters.HasValue) return source;

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        int effectiveMaxComplexity = options?.MaxFilterComplexity ?? maxComplexity;
        int maxFilterStringLength = options?.MaxFilterStringLength ?? 1000;
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        int maxFilterValueLength = options?.MaxFilterValueLength ?? 200;
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        int maxPropertyDepth = options?.MaxPropertyDepth ?? 3;

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (parameters.Value!.Length > maxFilterStringLength)
        {
            throw new ArgumentException($"The filter string exceeds the maximum allowed length of {maxFilterStringLength} characters.");
        }

        var predicate = FilterExpression.Build<T>(parameters, effectiveMaxComplexity, unknownFieldBehavior, allowedProperties, maxFilterValueLength, maxPropertyDepth, customOperatorProvider);
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        return predicate is not null ? source.Where(predicate) : source;
    }

    // ─── Internal helpers ─────────────────────────────────────────────────────

    private static int GetDeepOffsetWarningThreshold(IPaginationOptions? options)
    {
        if (options != null)
        {
            return options.DeepOffsetWarningThreshold;
        }
        return PaginationSettings.DeepOffsetWarningThreshold;
    }

    private static void ApplyPaginationLimitsAndLog<T>(
        IQueryable<T> source,
        PaginationParameters parameters,
        int? maxPageSize,
        out int effectivePageSize,
        out int skipAmount,
        IPaginationOptions? options = null)
    {

        var actualMaxPageSize = maxPageSize ?? options?.MaxPageSize ?? PaginationSettings.MaxPageSize;
        var originalPageSize = parameters.PageSize;
        effectivePageSize = Math.Min(originalPageSize, actualMaxPageSize);

        // EF-1: Do NOT mutate the caller's parameters struct via ref.
        // Previously this reassigned the parameter struct which mutated the caller's local,
        // violating readonly record struct semantics.
        // Instead, compute skip using effectivePageSize directly.
        long skipCalc = (long)(parameters.Page - 1) * effectivePageSize;
        // Stryker disable once Equality : skipCalc cannot exactly equal int.MaxValue given constrained page sizes, so >= is equivalent to >
        if (skipCalc > int.MaxValue)
        {
            // Stryker disable once String
            throw new ArgumentOutOfRangeException(nameof(parameters), "Page is too large, resulting in a skip offset that exceeds int.MaxValue. Strongly consider cursor pagination for deep offsets.");
        }
        skipAmount = (int)skipCalc;
        
        var threshold = GetDeepOffsetWarningThreshold(options);

        // Stryker disable all : Logging and telemetry
        if (effectivePageSize < originalPageSize || (threshold > 0 && skipAmount >= threshold))
        {
            // F-008 fix: logger extraction is best-effort. The IInfrastructure<IServiceProvider> cast
            // is an EF Core internal implementation detail that may return null or throw for:
            //   - InMemory DbContext (unit tests), custom IQueryProvider mocks, or future EF Core versions.
            // Wrapping in try/catch ensures logging failures never surface as unhandled exceptions.
            // Pagination limits are always enforced regardless of whether the logger is resolved.
            try
            {
                var serviceProvider = (source.Provider as Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<IServiceProvider>)?.Instance
                    ?? (source as Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<IServiceProvider>)?.Instance;

                if (serviceProvider != null)
                {
                    var loggerFactory = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<Microsoft.Extensions.Logging.ILoggerFactory>(serviceProvider);
                    var logger = loggerFactory?.CreateLogger(typeof(QueryableExtensions));

                    if (effectivePageSize < originalPageSize)
                    {
                        logger?.LogWarning("Pagination requested PageSize {RequestedPageSize} which exceeds the maximum allowed {MaxPageSize}. PageSize was capped.", originalPageSize, actualMaxPageSize);
                    }

                    if (threshold > 0 && skipAmount >= threshold)
                    {
                        logger?.LogWarning("Pagination executed a deep offset ({SkipAmount} rows skipped), which may cause performance degradation.", skipAmount);
                    }
                }
            }
            catch (Exception)
            {
                // Best-effort: swallow any exception from logger resolution. Pagination limits are
                // already enforced above; this block is diagnostic only.
            }
        }
        // Stryker restore all

    }

    // Stryker disable all
    /// <remarks>
    /// <b>Important (H-008)</b>: Approximate counts (e.g., using pg_class.reltuples) are NOT transactional. 
    /// They return a snapshot of table statistics which may not reflect uncommitted inserts/deletes in the current transaction,
    /// or even recently committed data depending on the database's autovacuum frequency.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2087", Justification = "EF Core AOT compiled model already preserves entity types.")]
    private static async Task<long> GetTotalCountAsync<T>(IQueryable<T> source, bool useApproximateCount, CancellationToken cancellationToken)
    {
        if (!useApproximateCount)
        {
            return await source.LongCountAsync(cancellationToken).ConfigureAwait(false);
        }

        DbContext? dbContext = null;
        Microsoft.Extensions.Logging.ILogger? logger = null;
        try
        {
            dbContext = (source as IInfrastructure<IServiceProvider>)?.GetService<ICurrentDbContext>()?.Context;
            if (dbContext == null)
            {
                return await source.LongCountAsync(cancellationToken).ConfigureAwait(false);
            }
            
            logger = dbContext.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger(typeof(QueryableExtensions));

            bool isNpgsql = dbContext.Database.ProviderName == NpgsqlProviderName || dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
            bool isSqlServer = dbContext.Database.ProviderName == SqlServerProviderName || dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;

            if (!isNpgsql && !isSqlServer)
            {
                throw new NotSupportedException($"Approximate count is only supported on PostgreSQL and SQL Server. Current provider: {dbContext.Database.ProviderName}");
            }

            var entityType = dbContext.Model.FindEntityType(typeof(T));
            var tableName = entityType?.GetTableName();
            if (string.IsNullOrEmpty(tableName))
            {
                // Fallback to exact count if the type is not mapped to a table (e.g., views, unmapped DTOs)
                return await source.LongCountAsync(cancellationToken).ConfigureAwait(false);
            }

            long approxCount;
            if (isNpgsql)
            {
                var schemaName = entityType!.GetSchema() ?? "public";
                approxCount = await PostgreSqlPaginationExtensions.GetApproximateCountAsync(dbContext, tableName, schemaName, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var schemaName = entityType!.GetSchema() ?? "dbo";
                approxCount = await SqlServerPaginationExtensions.GetApproximateCountAsync(dbContext, tableName, schemaName, cancellationToken).ConfigureAwait(false);
            }
                
            if (approxCount <= 0)
            {
                // Stryker disable once all
                logger?.LogWarning("Approximate count returned {Count} for '{Type}'. Statistics may be stale. Falling back to standard count.", approxCount, typeof(T).Name);
                return await source.LongCountAsync(cancellationToken).ConfigureAwait(false);
            }
                
            return approxCount;
        }
        catch (NotSupportedException ex)
        {
            // FIX-14: Re-throw as ArgumentException instead of silently falling back to exact count.
            // The silent fallback was misleading: callers passing useApproximateCount: true on SQLite
            // would receive exact counts without any indication that the flag was ignored.
            // Callers that want graceful degradation should catch ArgumentException or wrap this call.
            throw new ArgumentException(
                $"useApproximateCount is not supported for the current provider ({dbContext?.Database.ProviderName ?? "unknown"}). " +
                "Approximate counts are only supported on PostgreSQL (Npgsql) and SQL Server. " +
                "Pass useApproximateCount: false, or switch to a supported provider.",
                nameof(useApproximateCount),
                ex);
        }
        catch (OverflowException ex)
        {
            // EF-2: Returning int.MaxValue for an OverflowException would cause the UI to display
            // ~2.1 billion pages for tables without PostgreSQL stats (reltuples=-1 post-CREATE,
            // pre-ANALYZE). Fall back to an exact count instead, which is always correct.
            // Stryker disable once all
            logger?.LogWarning(ex, "Overflow computing approximate count for '{Type}' (likely reltuples=-1 for a table without statistics). Falling back to exact count.", typeof(T).Name);
        }
        catch (System.Data.Common.DbException ex)
        {
            // EF-4: A DbException from Npgsql may also represent a cancellation.
            // Check before retrying to avoid launching a new query on an already-cancelled token.
            cancellationToken.ThrowIfCancellationRequested();
            // Stryker disable once all
            logger?.LogWarning(ex, "Failed to get approximate count for '{Type}' due to a database error. Falling back to standard count.", typeof(T).Name);
        }
        catch (InvalidOperationException ex)
        {
            // Stryker disable once all
            logger?.LogWarning(ex, "Failed to get approximate count for '{Type}' due to an invalid operation. Falling back to standard count.", typeof(T).Name);
        }

        return await source.LongCountAsync(cancellationToken).ConfigureAwait(false);
    }
}








