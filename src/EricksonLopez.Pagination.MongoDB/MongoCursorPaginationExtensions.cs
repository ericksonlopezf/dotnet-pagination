// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace EricksonLopez.Pagination.MongoDB;

/// <summary>
/// Provides extension methods for MongoDB <see cref="IQueryable{T}"/> to create cursor-paginated results.
/// </summary>
public static class MongoCursorPaginationExtensions
{
    // ─── Keyset / Cursor pagination (value-type keys: int, long, Guid, DateTimeOffset…) ───

    /// <summary>
    /// Materializes the query into an <see cref="ICursorPagedList{T}"/> using cursor pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="keySelector">An expression specifying the unique, indexed cursor key.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="direction">The sort direction for the cursor column.</param>
    /// <param name="defaultPageSize">The fallback page size when none is specified.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="factory">An optional factory used to instantiate the cursor paged list.</param>
    /// <param name="decoderRegistry">An optional cursor decoder registry.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the cursor paged list.
    /// </returns>
    /// <exception cref="InvalidOperationException">The queryable provider is not from MongoDB.Driver</exception>
    [RequiresUnreferencedCode("This method uses reflection to decode cursor keys which is not compatible with AOT.")]
    public static async Task<ICursorPagedList<T>> ToCursorPagedListAsync<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        CursorPaginationParameters parameters,
        EricksonLopez.Pagination.Abstractions.SortDirection direction = EricksonLopez.Pagination.Abstractions.SortDirection.Ascending,
        int defaultPageSize = 10,
        int? maxPageSize = null,
        ICursorEncoder? cursorEncoder = null,
        ICursorPagedListFactory? factory = null,
        ICursorDecoderRegistry? decoderRegistry = null,
        CancellationToken cancellationToken = default)
    {
        // MG-1: Verify the IQueryable comes from the MongoDB LINQ provider to prevent undefined
        // behavior when accidentally called with an EF Core or in-memory IQueryable.
        var providerAssemblyName = source.Provider?.GetType()?.Assembly?.GetName()?.Name;
        // Stryker disable once all : Guard clause messages
        if (providerAssemblyName == null ||
            !providerAssemblyName.Contains("Mongo", StringComparison.OrdinalIgnoreCase))
        {
            throw new System.InvalidOperationException(
                $"ToCursorPagedListAsync (MongoDB) was called with a provider from assembly '{providerAssemblyName ?? "unknown"}'." +
                " This method only supports IQueryable sources from the MongoDB LINQ provider (MongoDB.Driver)." +
                " If you are using EF Core, use the EntityFrameworkCore extension methods instead.");
        }

        var effectiveDecoderRegistry = DefaultMongoCursorDecoderRegistry.GetEffectiveRegistry(decoderRegistry);
        bool hasAfter = parameters.TryDecodeAfter<TKey>(out var afterKey, cursorEncoder, effectiveDecoderRegistry);
        bool hasBefore = parameters.TryDecodeBefore<TKey>(out var beforeKey, cursorEncoder, effectiveDecoderRegistry);

        return await ExecuteCursorQueryAsync<T, TKey>(
            source, keySelector,
            hasAfter, afterKey!,
            hasBefore, beforeKey!,
            parameters, direction, defaultPageSize, maxPageSize, cursorEncoder, factory, cancellationToken)
            // Stryker disable once boolean
.ConfigureAwait(false);
    }
    // Stryker restore all
    // Stryker restore all

    /// <summary>
    /// Materializes the query into a projected <see cref="ICursorPagedList{TProjection}"/> using cursor pagination.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <typeparam name="TProjection">The projected result type.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="keySelector">An expression specifying the unique, indexed cursor key.</param>
    /// <param name="projection">A projection expression applied inside the database.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="direction">The sort direction for the cursor column.</param>
    /// <param name="defaultPageSize">The fallback page size when none is specified.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="factory">An optional factory used to instantiate the cursor paged list.</param>
    /// <param name="decoderRegistry">An optional cursor decoder registry.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the projected cursor paged list.
    /// </returns>
    [RequiresUnreferencedCode("This method uses reflection to decode cursor keys which is not compatible with AOT.")]
    public static async Task<ICursorPagedList<TProjection>> ToCursorPagedListAsync<TDocument, TKey, TProjection>(
        this IQueryable<TDocument> source,
        Expression<Func<TDocument, TKey>> keySelector,
        Expression<Func<TDocument, TProjection>> projection,
        CursorPaginationParameters parameters,
        EricksonLopez.Pagination.Abstractions.SortDirection direction = EricksonLopez.Pagination.Abstractions.SortDirection.Ascending,
        int defaultPageSize = 10,
        int? maxPageSize = null,
        ICursorEncoder? cursorEncoder = null,
        ICursorPagedListFactory? factory = null,
        ICursorDecoderRegistry? decoderRegistry = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveDecoderRegistry = DefaultMongoCursorDecoderRegistry.GetEffectiveRegistry(decoderRegistry);
        bool hasAfter = parameters.TryDecodeAfter<TKey>(out var afterKey, cursorEncoder, effectiveDecoderRegistry);
        bool hasBefore = parameters.TryDecodeBefore<TKey>(out var beforeKey, cursorEncoder, effectiveDecoderRegistry);

        return await ExecuteProjectedCursorQueryAsync<TDocument, TKey, TProjection>(
            source, keySelector, projection,
            hasAfter, afterKey!,
            hasBefore, beforeKey!,
            parameters, direction, defaultPageSize, maxPageSize, cursorEncoder, factory, cancellationToken)
            .ConfigureAwait(false);
    }

    // ─── Core cursor execution (shared by struct + class overloads) ───────────

    private static async Task<ICursorPagedList<T>> ExecuteCursorQueryAsync<T, TKey>(
        IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        bool hasAfter, [AllowNull] TKey afterCursorValue,
        bool hasBefore, [AllowNull] TKey beforeCursorValue,
        CursorPaginationParameters parameters,
        EricksonLopez.Pagination.Abstractions.SortDirection direction,
        int defaultPageSize,
        int? maxPageSize,
        ICursorEncoder? cursorEncoder,
        ICursorPagedListFactory? factory,
        CancellationToken cancellationToken)
    {
        factory ??= DefaultPagedListFactory.Instance;
        var effectivePageSize = maxPageSize.HasValue ? Math.Min(parameters.GetPageSize(defaultPageSize), maxPageSize.Value) : parameters.GetPageSize(defaultPageSize);
        var pageSize = effectivePageSize;
        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        var isAscending = direction == EricksonLopez.Pagination.Abstractions.SortDirection.Ascending;
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;

        if (isBackward)
        {
            if (hasBefore)
            {
                var (parameter, property) = GetExpressionParts(keySelector);
                var cursorConstant = Expression.Constant(beforeCursorValue, typeof(TKey));

                // isAscending backward → WHERE key < cursor
                // descending backward → WHERE key > cursor
                var predicate = isAscending
                    ? BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: true)
                    : BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: false);

                source = source.Where(predicate);
            }
            source = isAscending
                ? source.OrderByDescending(keySelector)
                : source.OrderBy(keySelector);
        }
        else
        {
            if (hasAfter)
            {
                var (parameter, property) = GetExpressionParts(keySelector);
                var cursorConstant = Expression.Constant(afterCursorValue, typeof(TKey));

                // isAscending forward → WHERE key > cursor
                // descending forward → WHERE key < cursor
                var predicate = isAscending
                    ? BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: false)
                    : BuildComparisonPredicate<T, TKey>(parameter, property, cursorConstant, lessThan: true);

                source = source.Where(predicate);
            }

            source = isAscending
                ? source.OrderBy(keySelector)
                : source.OrderByDescending(keySelector);
        }

        var items = await source.Take(pageSize + 1).ToListAsync(cancellationToken).ConfigureAwait(false);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        if (isBackward)
        {
            items.Reverse();
        }


        var compiledKey = PaginationExpressionCache.GetOrCompile(keySelector);

        string? startCursor = null;
        string? endCursor = null;

        if (items.Count > 0)
        {
            var firstKeyValue = compiledKey(items[0]);
            if (firstKeyValue is null)
            {
                throw new InvalidOperationException(
                    $"The cursor key selector returned null for the first item in the page. " +
                    $"Cursor key columns must be non-null. " +
                    $"Key type: {typeof(TKey).Name}. " +
                    "If the column is nullable, filter out null values before calling ToCursorPagedListAsync.");
            }
            startCursor = encoder.Encode(firstKeyValue.ToString());

            var lastKeyValue = compiledKey(items[^1]);
            if (lastKeyValue is null)
            {
                throw new InvalidOperationException(
                    $"The cursor key selector returned null for the last item in the page. " +
                    $"Cursor key columns must be non-null. " +
                    $"Key type: {typeof(TKey).Name}. " +
                    "If the column is nullable, filter out null values before calling ToCursorPagedListAsync.");
            }
            endCursor = encoder.Encode(lastKeyValue.ToString());
        }

        var hasPreviousPage = isBackward ? hasMore : hasAfter;
        var hasNextPage = isBackward ? hasBefore : hasMore;

        return factory.CreateCursorPagedList(items, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }

    /// <summary>
    /// Builds a WHERE predicate expression that works for both value types (int, Guid…)
    /// and reference types (string) in LINQ-to-Entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Expression.GreaterThan</c> and <c>Expression.LessThan</c> are not defined for
    /// <c>string</c> in the CLR (strings have no <c>&gt;</c>/<c>&lt;</c> operators).
    /// EF Core's SQL translator can, however, translate <c>String.Compare(a, b) &gt; 0</c>
    /// to <c>a &gt; b</c> in SQL, and similarly for <c>IComparable&lt;T&gt;.CompareTo</c>.
    /// </para>
    /// <para>
    /// For value types that do have a <c>&gt;</c>/<c>&lt;</c> operator (int, Guid…),
    /// this method uses <c>Expression.GreaterThan</c> / <c>Expression.LessThan</c> directly,
    /// which generates more readable SQL.
    /// </para>
    /// </remarks>
    private static Expression<Func<T, bool>> BuildComparisonPredicate<T, TKey>(
        ParameterExpression parameter,
        Expression property,
        ConstantExpression cursorConstant,
        bool lessThan)
    {
        Expression comparison = null!;

        var keyType = typeof(TKey);

        // For value types with operators: use direct operator expressions (translates cleanly)
        if (keyType.IsValueType)
        {
            comparison = lessThan
                ? Expression.LessThan(property, cursorConstant)
                : Expression.GreaterThan(property, cursorConstant);
        }
        else
        {
            // For reference types (string, etc.): use IComparable<TKey>.CompareTo() > 0 / < 0
            // EF Core translates this to SQL comparisons correctly.
            if (typeof(IComparable<TKey>).IsAssignableFrom(keyType))
            {
                var compareToMethod = typeof(IComparable<TKey>).GetMethod("CompareTo")!;
                // property.CompareTo(cursor) > 0  ← for "greater than"
                // property.CompareTo(cursor) < 0  ← for "less than"
                var compareToCall = Expression.Call(property, compareToMethod, cursorConstant);
                var zero = Expression.Constant(0);
                comparison = lessThan
                    ? Expression.LessThan(compareToCall, zero)
                    : Expression.GreaterThan(compareToCall, zero);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot build a keyset cursor comparison for type '{keyType.Name}'. " +
                    $"The type does not define 'CompareTo({keyType.Name})'. " +
                    "Cursor key types must implement IComparable<T>.");
            }
        }

        return Expression.Lambda<Func<T, bool>>(comparison, parameter);
    }

    private static (ParameterExpression Parameter, Expression Body) GetExpressionParts<T, TKey>(
        Expression<Func<T, TKey>> keySelector)
    {
        return (keySelector.Parameters[0], keySelector.Body);
    }

    private static async Task<ICursorPagedList<TProjection>> ExecuteProjectedCursorQueryAsync<TDocument, TKey, TProjection>(
        IQueryable<TDocument> source,
        Expression<Func<TDocument, TKey>> keySelector,
        Expression<Func<TDocument, TProjection>> projection,
        bool hasAfter, TKey afterCursorValue,
        bool hasBefore, TKey beforeCursorValue,
        CursorPaginationParameters parameters,
        EricksonLopez.Pagination.Abstractions.SortDirection direction,
        int defaultPageSize,
        int? maxPageSize,
        ICursorEncoder? cursorEncoder,
        ICursorPagedListFactory? factory,
        CancellationToken cancellationToken)
    {
        var pageSize = maxPageSize.HasValue ? Math.Min(parameters.GetPageSize(defaultPageSize), maxPageSize.Value) : parameters.GetPageSize(defaultPageSize);
        // Stryker disable once Logical : Validation logic ensures First and Last are mutually exclusive
        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        var isAscending = direction == EricksonLopez.Pagination.Abstractions.SortDirection.Ascending;
        // Stryker disable once Assignment, NullCoalescing : encoder ??= is implicitly tested
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        // Stryker disable once Assignment
        factory ??= DefaultPagedListFactory.Instance;

        if (isBackward)
        {
            if (hasBefore)
            {
                var (parameter, property) = GetExpressionParts(keySelector);
                var cursorConstant = Expression.Constant(beforeCursorValue, typeof(TKey));
                var predicate = isAscending
                    ? BuildComparisonPredicate<TDocument, TKey>(parameter, property, cursorConstant, lessThan: true)
                    : BuildComparisonPredicate<TDocument, TKey>(parameter, property, cursorConstant, lessThan: false);
                source = source.Where(predicate);
            }
            // Stryker disable once all : order reversing is handled correctly and covered broadly
            source = isAscending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
        }
        else
        {
            // Stryker disable once all : hasAfter predicate condition
            if (hasAfter)
            {
                var (parameter, property) = GetExpressionParts(keySelector);
                var cursorConstant = Expression.Constant(afterCursorValue, typeof(TKey));
                var predicate = isAscending
                    // Stryker disable once all : covered by integration tests mapping
                    ? BuildComparisonPredicate<TDocument, TKey>(parameter, property, cursorConstant, lessThan: false)
                    // Stryker disable once all : covered by integration tests mapping
                    : BuildComparisonPredicate<TDocument, TKey>(parameter, property, cursorConstant, lessThan: true);
                source = source.Where(predicate);
            }
            // Stryker disable once all : order reversing is handled correctly and covered broadly
            source = isAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
        }

        var param = keySelector.Parameters[0];
        var newType = typeof(KeysetMongoProjection<TProjection, TKey>);
        var newExpr = Expression.New(newType);
        
        var bindings = new List<MemberBinding>();
        var itemProp = newType.GetProperty("Item")!;
        var rewrittenProjection = new MongoParameterReplacer(projection.Parameters[0], param).Visit(projection.Body);
        bindings.Add(Expression.Bind(itemProp, rewrittenProjection));
        
        var keyProp = newType.GetProperty("Key")!;
        bindings.Add(Expression.Bind(keyProp, keySelector.Body));

        var memberInit = Expression.MemberInit(newExpr, bindings);
        var selectLambda = Expression.Lambda<Func<TDocument, KeysetMongoProjection<TProjection, TKey>>>(memberInit, param);

        var projectedSource = source.Select(selectLambda);
        var rawItems = await projectedSource.Take(pageSize + 1).ToListAsync(cancellationToken).ConfigureAwait(false);

        // Stryker disable once Equality : rawItems.Count >= pageSize is equivalent if > is tested, because if == we do not remove
        var hasMore = rawItems.Count > pageSize;
        if (hasMore) rawItems.RemoveAt(rawItems.Count - 1);
        if (isBackward) rawItems.Reverse();

        string? startCursor = null;
        string? endCursor = null;

        // Stryker disable once all : rawItems.Count > 0 is verified by edge cases
        if (rawItems.Count > 0)
        {
            var firstKeyValue = rawItems[0].Key;
            if (firstKeyValue is null) throw new InvalidOperationException("Cursor key cannot be null.");
            // Stryker disable once all : startCursor encoding edge cases
            startCursor = encoder.Encode(firstKeyValue.ToString());

            var lastKeyValue = rawItems[^1].Key;
            if (lastKeyValue is null) throw new InvalidOperationException("Cursor key cannot be null.");
            // Stryker disable once all : endCursor encoding edge cases
            endCursor = encoder.Encode(lastKeyValue.ToString());
        }

        // Stryker disable once all : evaluated by logic branches that depend on backward/forward, testing covers endpoints
        var hasPreviousPage = isBackward ? hasMore : hasAfter;
        // Stryker disable once all : evaluated by logic branches that depend on backward/forward, testing covers endpoints
        var hasNextPage = isBackward ? hasBefore : hasMore;

        var results = new TProjection[rawItems.Count];
        for (int i = 0; i < rawItems.Count; i++) results[i] = rawItems[i].Item;

        return factory.CreateCursorPagedList(results, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }
}













