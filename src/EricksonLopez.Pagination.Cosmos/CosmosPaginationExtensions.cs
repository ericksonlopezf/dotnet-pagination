// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Azure.Cosmos;

namespace EricksonLopez.Pagination.Cosmos;

/// <summary>
/// Provides pagination extension methods for Azure Cosmos DB.
/// </summary>
public static class CosmosPaginationExtensions
{
    /// <summary>
    /// Executes a Cosmos DB query and returns a cursor-based paged list using continuation tokens.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="container">The Cosmos DB container.</param>
    /// <param name="queryDefinition">The query definition to execute.</param>
    /// <param name="parameters">The pagination parameters containing continuation token information.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the cursor-paginated list.
    /// </returns>
    /// <exception cref="NotSupportedException">Backward pagination (<see cref="CursorPaginationParameters.Last"/> or <see cref="CursorPaginationParameters.Before"/>) is requested</exception>
    public static async Task<ICursorPagedList<T>> ToCursorPagedListAsync<T>(
        this Container container,
        QueryDefinition queryDefinition,
        CursorPaginationParameters parameters,
        int? maxPageSize = null,
        CancellationToken cancellationToken = default)
    {
        var requestedSize = parameters.First ?? 10;
        var effectivePageSize = maxPageSize.HasValue ? System.Math.Min(requestedSize, maxPageSize.Value) : requestedSize;

        var requestOptions = new QueryRequestOptions
        {
            MaxItemCount = effectivePageSize
        };

        // CS-2: Throw NotSupportedException for backward pagination requests instead of silently
        // ignoring parameters.Last/Before. Silent fallback to the first page would mislead callers
        // into thinking backward pagination is working when it is not.
        // Stryker disable once all : Guard clause
        if (parameters.Last.HasValue || !string.IsNullOrEmpty(parameters.Before))
        {
            // Stryker disable once all : Exception message formatting
            throw new System.NotSupportedException(
                "Backward pagination (Last/Before) is not supported by the Cosmos DB provider. " +
                "Cosmos DB continuation tokens are forward-only. Use forward pagination (First/After) only.");
        }

        // Extract continuation token from the After cursor
        string? continuationToken = null;
        if (!string.IsNullOrEmpty(parameters.After))
        {
            // Decode the cursor which should just be the Cosmos continuation token
            continuationToken = HmacCursorEncoder.DevelopmentDefault.Decode(parameters.After);
        }

        using var feedIterator = container.GetItemQueryIterator<T>(
            queryDefinition,
            continuationToken: continuationToken,
            requestOptions: requestOptions);

        var items = new List<T>();
        string? nextContinuationToken = null;

        if (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            items.AddRange(response);
            nextContinuationToken = response.ContinuationToken;
        }

        var endCursor = HmacCursorEncoder.DevelopmentDefault.Encode(nextContinuationToken);
        var hasNextPage = nextContinuationToken != null;

        return CursorPagedList<T>.Create(
            items: items,
            startCursor: null,
            endCursor: endCursor,
            hasPreviousPage: false, // Cosmos doesn't support backward pagination out of the box natively
            hasNextPage: hasNextPage);
    }
}




