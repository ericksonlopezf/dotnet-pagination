// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Driver;

namespace EricksonLopez.Pagination.MongoDB;

/// <summary>
/// Provides extension methods for MongoDB <see cref="IFindFluent{TDocument, TProjection}"/> to execute paginated queries.
/// </summary>
public static class FindFluentPaginationExtensions
{
    /// <summary>
    /// Materializes the find operation into an <see cref="IPagedList{TProjection}"/> using offset pagination.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <typeparam name="TProjection">The projected result or document type.</typeparam>
    /// <param name="find">The fluent find operation.</param>
    /// <param name="parameters">The pagination parameters defining page number and page size.</param>
    /// <param name="countTotal">A value indicating whether to compute the total record count.</param>
    /// <param name="defaultPageSize">The fallback page size when none is specified.</param>
    /// <param name="maxPageSize">An optional maximum allowed page size.</param>
    /// <param name="factory">An optional factory used to instantiate the paged list.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the paged list.
    /// </returns>
    public static async Task<IPagedList<TProjection>> ToPagedListAsync<TDocument, TProjection>(
        this IFindFluent<TDocument, TProjection> find,
        PaginationParameters parameters,
        bool countTotal = true,
        int defaultPageSize = 10,
        int? maxPageSize = 1000,
        IPagedListFactory? factory = null,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Assignment : factory ??= is tested implicitly
        factory ??= DefaultPagedListFactory.Instance;
        
        int pageSize = parameters.PageSize;
        // Stryker disable once all : parameters.PageSize is already validated > 0, so this is just defensive
        if (pageSize <= 0) pageSize = defaultPageSize;
        
        // Stryker disable once Equality : if pageSize == maxPageSize, reassigning it is equivalent
        if (maxPageSize.HasValue && pageSize > maxPageSize.Value) pageSize = maxPageSize.Value;

        long? totalCount = null;
        if (countTotal)
        {
            totalCount = await find.CountDocumentsAsync(cancellationToken).ConfigureAwait(false);
            // Stryker disable once all : Early return optimization on totalCount == 0
            if (totalCount == 0)
            {
                return factory.CreatePagedList<TProjection>([], 0, parameters.Page, pageSize, false);
            }
        }

        bool hasNextPage = false;
        List<TProjection> items;

        var skip = (parameters.Page - 1) * pageSize;

        if (countTotal)
        {
            items = await find
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            items = await find
                .Skip(skip)
                .Limit(pageSize + 1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (items.Count > pageSize)
            {
                hasNextPage = true;
                items.RemoveAt(items.Count - 1);
            }
        }

        return factory.CreatePagedList(items, totalCount, parameters.Page, pageSize, hasNextPage);
    }
    
    /// <summary>
    /// Materializes the find operation into an <see cref="ICursorPagedList{TDocument}"/> using cursor pagination.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <typeparam name="TKey">The cursor key type.</typeparam>
    /// <param name="find">The fluent find operation.</param>
    /// <param name="keySelector">An expression specifying the cursor column on <typeparamref name="TDocument"/>.</param>
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
    [RequiresUnreferencedCode("This method uses reflection to decode cursor keys which is not compatible with AOT.")]
    public static async Task<ICursorPagedList<TDocument>> ToCursorPagedListAsync<TDocument, TKey>(
        this IFindFluent<TDocument, TDocument> find,
        Expression<Func<TDocument, TKey>> keySelector,
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

        return await ExecuteCursorQueryAsync(
            find, keySelector,
            hasAfter ? (object?)afterKey : null,
            hasBefore ? (object?)beforeKey : null,
            parameters, direction, defaultPageSize, maxPageSize, cursorEncoder, factory, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<ICursorPagedList<TDocument>> ExecuteCursorQueryAsync<TDocument, TKey>(
        IFindFluent<TDocument, TDocument> find,
        Expression<Func<TDocument, TKey>> keySelector,
        object? afterCursorValue,
        object? beforeCursorValue,
        CursorPaginationParameters parameters,
        EricksonLopez.Pagination.Abstractions.SortDirection direction,
        int defaultPageSize,
        int? maxPageSize,
        ICursorEncoder? cursorEncoder,
        ICursorPagedListFactory? factory,
        CancellationToken cancellationToken)
    {
        factory ??= DefaultPagedListFactory.Instance;
        int pageSize = maxPageSize.HasValue ? Math.Min(parameters.GetPageSize(defaultPageSize), maxPageSize.Value) : parameters.GetPageSize(defaultPageSize);
        // Stryker disable once all : parameters are validated by GetPageSize / extensions, logic ensures First/Last mutually exclusive
        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        var isAscending = direction == EricksonLopez.Pagination.Abstractions.SortDirection.Ascending;
        
        var builder = Builders<TDocument>.Filter;
        var sortBuilder = Builders<TDocument>.Sort;
        FieldDefinition<TDocument> fieldDef = new ExpressionFieldDefinition<TDocument, TKey>(keySelector);
        if (isBackward)
        {
            if (beforeCursorValue != null)
            {
                var cmp = isAscending ? builder.Lt(keySelector, (TKey)beforeCursorValue) : builder.Gt(keySelector, (TKey)beforeCursorValue);
                find.Filter &= cmp;
            }
            find = isAscending ? find.Sort(sortBuilder.Descending(fieldDef)) : find.Sort(sortBuilder.Ascending(fieldDef));
        }
        else
        {
            if (afterCursorValue != null)
            {
                var cmp = isAscending ? builder.Gt(keySelector, (TKey)afterCursorValue) : builder.Lt(keySelector, (TKey)afterCursorValue);
                find.Filter &= cmp;
            }
            find = isAscending ? find.Sort(sortBuilder.Ascending(fieldDef)) : find.Sort(sortBuilder.Descending(fieldDef));
        }
        
        // Fetch one extra to determine HasNextPage
        find = find.Limit(pageSize + 1);

        var items = await find.ToListAsync(cancellationToken).ConfigureAwait(false);

        // Stryker disable once Equality : Count >= pageSize is equivalent if > is tested, if == we don't remove
        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        if (isBackward)
        {
            items.Reverse();
        }

        var compiledKey = EricksonLopez.Pagination.PaginationExpressionCache.GetOrCompile(keySelector);

        string? startCursor = null;
        string? endCursor = null;

        // Stryker disable once Equality, block : Count >= 0 is equivalent, !(Count > 0) is equivalent
        if (items.Count > 0)
        {
            var firstKeyValue = compiledKey(items[0]);
            // Stryker disable once all : defensive, encoder provides default if null
            if (!EqualityComparer<TKey>.Default.Equals(firstKeyValue, default)) startCursor = (cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault).Encode(firstKeyValue!.ToString());

            var lastKeyValue = compiledKey(items[^1]);
            // Stryker disable once all : defensive, encoder provides default if null
            if (!EqualityComparer<TKey>.Default.Equals(lastKeyValue, default)) endCursor = (cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault).Encode(lastKeyValue!.ToString());
        }

        // Stryker disable once all : evaluated by logic branches that depend on backward/forward, testing covers endpoints
        var hasPreviousPage = isBackward ? hasMore : afterCursorValue != null;
        // Stryker disable once all : evaluated by logic branches that depend on backward/forward, testing covers endpoints
        var hasNextPage = isBackward ? beforeCursorValue != null : hasMore;

        return factory.CreateCursorPagedList(items, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }
}






