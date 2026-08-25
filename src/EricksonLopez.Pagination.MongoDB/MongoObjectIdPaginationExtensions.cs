// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EricksonLopez.Pagination.MongoDB;

/// <summary>
/// Provides keyset pagination extension methods for MongoDB using <see cref="ObjectId"/> cursor keys.
/// </summary>
public static class MongoObjectIdPaginationExtensions
{
    /// <summary>
    /// Materializes a cursor-paginated keyset query on an <see cref="IMongoCollection{TDocument}"/> using an <see cref="ObjectId"/> key.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <param name="collection">The Mongo collection.</param>
    /// <param name="filter">An optional filter definition.</param>
    /// <param name="keySelector">An expression specifying the <see cref="ObjectId"/> cursor property.</param>
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
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    [RequiresUnreferencedCode("This method uses reflection to decode cursor keys which is not compatible with AOT.")]
    public static Task<ICursorPagedList<TDocument>> ToCursorPagedListAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filter,
        Expression<Func<TDocument, ObjectId>> keySelector,
        CursorPaginationParameters parameters,
        EricksonLopez.Pagination.Abstractions.SortDirection direction = EricksonLopez.Pagination.Abstractions.SortDirection.Ascending,
        int defaultPageSize = 10,
        int? maxPageSize = null,
        ICursorEncoder? cursorEncoder = null,
        ICursorPagedListFactory? factory = null,
        ICursorDecoderRegistry? decoderRegistry = null,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable all : Defensive guard clause
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        // Stryker restore all

        var find = collection.Find(filter ?? Builders<TDocument>.Filter.Empty);
        return find.ToCursorPagedListAsync(
            keySelector,
            parameters,
            direction,
            defaultPageSize,
            maxPageSize,
            cursorEncoder,
            factory,
            decoderRegistry,
            cancellationToken);
    }
}



