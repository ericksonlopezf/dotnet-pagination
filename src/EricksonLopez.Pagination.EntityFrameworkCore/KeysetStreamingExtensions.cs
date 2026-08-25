// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for streaming keyset-paginated queries using <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class KeysetStreamingExtensions
{
    /// <summary>
    /// Streams all records matching the query using single-column keyset pagination with on-demand batch fetching.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The cursor key type.</typeparam>
    /// <param name="source">The source queryable to stream from.</param>
    /// <param name="keySelector">An expression selecting the cursor key column.</param>
    /// <param name="direction">The sorting direction.</param>
    /// <param name="batchSize">The number of items to fetch per database query.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> yielding entities sequentially.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="keySelector"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is less than or equal to zero</exception>
    [RequiresUnreferencedCode("This method uses reflection to decode cursor keys which is not compatible with AOT.")]
    public static IAsyncEnumerable<T> AsKeysetStreamAsync<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction = SortDirection.Ascending,
        int batchSize = 1000,
        ICursorEncoder? cursorEncoder = null,
        CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

        return StreamCoreAsync(source, keySelector, direction, batchSize, cursorEncoder, cancellationToken);
    }

    private static async IAsyncEnumerable<T> StreamCoreAsync<T, TKey>(
        IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction,
        int batchSize,
        ICursorEncoder? cursorEncoder,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? currentCursor = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var parameters = new CursorPaginationParameters
            {
                First = batchSize,
                After = currentCursor
            };

            var builder = source.Keyset(parameters, batchSize, cursorEncoder);
            var configuredBuilder = direction == SortDirection.Ascending
                ? builder.Ascending(keySelector)
                : builder.Descending(keySelector);

            var page = await configuredBuilder.ToCursorPagedListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var item in page)
            {
                yield return item;
            }

            // Stryker disable once all : Stream termination on last page or empty cursor
            if (!page.HasNextPage || string.IsNullOrEmpty(page.EndCursor))
            {
                yield break;
            }

            currentCursor = page.EndCursor;
        }
    }
}



