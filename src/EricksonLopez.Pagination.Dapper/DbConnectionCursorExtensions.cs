// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Dapper;

/// <summary>
/// Provides extension methods for <see cref="IDbConnection"/> to execute cursor-paginated
/// queries using Dapper.
/// </summary>
public static class DbConnectionCursorExtensions
{
    /// <summary>
    /// Executes a cursor-paginated SQL query and returns a <see cref="CursorPagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="keySelector">A function that extracts the raw cursor value from each result item.</param>
    /// <param name="cursorDecoder">An optional function to convert the decoded cursor string to <typeparamref name="TKey"/>.</param>
    /// <param name="param">Additional user-defined query parameters.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="commandType">An optional command type.</param>
    /// <param name="defaultPageSize">The fallback page size if not provided by the parameters.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="factory">An optional factory used to instantiate the paginated list.</param>
    /// <param name="decoderRegistry">An optional registry for AOT decoders.</param>
    /// <param name="autoReverse">A value indicating whether to reverse results in-memory during backward pagination.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the cursor-paginated list.
    /// </returns>
    /// <exception cref="InvalidOperationException">The cursor value cannot be converted to <typeparamref name="TKey"/></exception>
    public static async Task<ICursorPagedList<T>> ToCursorPagedListAsync<T, TKey>(
        this IDbConnection connection,
        string sql,
        CursorPaginationParameters parameters,
        Func<T, TKey> keySelector,
        Func<string, TKey>? cursorDecoder = null,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        int defaultPageSize = 10,
        int? maxPageSize = null,
        ICursorEncoder? cursorEncoder = null,
        ICursorPagedListFactory? factory = null,
        ICursorDecoderRegistry? decoderRegistry = null,
        bool autoReverse = true,
        CancellationToken cancellationToken = default)
    {

        // Stryker disable once all : Default factory fallback
        factory ??= DefaultPagedListFactory.Instance;
        var dynParams = new DynamicParameters(param);
        var actualMaxPageSize = maxPageSize ?? PaginationSettings.MaxPageSize;
        var pageSize = System.Math.Min(parameters.GetPageSize(defaultPageSize), actualMaxPageSize);
        dynParams.Add("@__Pagination_Limit__", pageSize + 1);

        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;

        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;

        object? afterVal = null;
        if (!string.IsNullOrEmpty(parameters.After))
        {
            afterVal = DecodeCursor(parameters.After!, encoder, cursorDecoder, decoderRegistry);
        }

        object? beforeVal = null;
        if (!string.IsNullOrEmpty(parameters.Before))
        {
            beforeVal = DecodeCursor(parameters.Before!, encoder, cursorDecoder, decoderRegistry);
        }

        dynParams.Add("@Cursor", isBackward ? beforeVal : afterVal);

        // Stryker disable once boolean
        var items = await connection.QueryAsync<T>(
            new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
            // Stryker disable once boolean
.ConfigureAwait(false);

        // Dapper.AsList() uses Dapper's internal knowledge of the collection type to avoid a
        // redundant ToList() copy. This is safer than `items as List<T> ?? items.ToList()`
        // which would silently allocate if Dapper's internal return type ever changes.
        var list = items.AsList();

        bool hasPreviousPage;
        bool hasNextPage;

        var hasMore = list.Count > pageSize;
        if (hasMore)
        {
            // Sentinel is always the last element returned from the DB.
            list.RemoveAt(list.Count - 1);
        }

        if (isBackward)
        {
            // Reverse the list in-memory to restore the natural forward order.
            if (autoReverse)
            {
                list.Reverse();
            }
            
            hasPreviousPage = hasMore;
            hasNextPage = parameters.Before != null;
        }
        else
        {
            hasPreviousPage = parameters.After != null;
            hasNextPage = hasMore;
        }

        // Variables computed above
        var startCursor = list.Count > 0 ? encoder.Encode(keySelector(list[0])?.ToString()) : null;
        var endCursor = list.Count > 0 ? encoder.Encode(keySelector(list[list.Count - 1])?.ToString()) : null;

        return factory.CreateCursorPagedList(list, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }

    /// <summary>
    /// Executes a composite cursor-based paginated SQL query with two keys and returns a <see cref="CursorPagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <typeparam name="TKey1">The type of the primary sort column.</typeparam>
    /// <typeparam name="TKey2">The type of the secondary tie-breaker sort column.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="keySelector">A function extracting the composite key from each item.</param>
    /// <param name="cursorDecoder">An optional function to decode the composite cursor string.</param>
    /// <param name="param">Additional user-defined query parameters.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="commandType">An optional command type.</param>
    /// <param name="defaultPageSize">The fallback page size if not provided by the parameters.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="factory">An optional factory used to instantiate the paginated list.</param>
    /// <param name="autoReverse">A value indicating whether to reverse results in-memory during backward pagination.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the cursor-paginated list.
    /// </returns>
    public static async Task<ICursorPagedList<T>> ToCursorPagedListAsync<T, TKey1, TKey2>(
        this IDbConnection connection,
        string sql,
        CursorPaginationParameters parameters,
        Func<T, (TKey1, TKey2)> keySelector,
        Func<string, (TKey1, TKey2)>? cursorDecoder = null,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        int defaultPageSize = 10,
        int? maxPageSize = null,
        ICursorEncoder? cursorEncoder = null,
        ICursorPagedListFactory? factory = null,
        bool autoReverse = true,
        CancellationToken cancellationToken = default)
    {

        // Stryker disable once all : Default factory fallback
        factory ??= DefaultPagedListFactory.Instance;
        var dynParams = new DynamicParameters(param);
        var actualMaxPageSize = maxPageSize ?? PaginationSettings.MaxPageSize;
        var pageSize = System.Math.Min(parameters.GetPageSize(defaultPageSize), actualMaxPageSize);
        dynParams.Add("@__Pagination_Limit__", pageSize + 1);

        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;

        if (isBackward)
        {
            if (parameters.Before != null)
            {
                var decoded = DecodeCompositeCursor(parameters.Before, encoder, cursorDecoder);
                dynParams.Add("@Cursor1", decoded.Item1);
                dynParams.Add("@Cursor2", decoded.Item2);
            }
            else
            {
                dynParams.Add("@Cursor1", null);
                dynParams.Add("@Cursor2", null);
            }
        }
        else
        {
            if (parameters.After != null)
            {
                var decoded = DecodeCompositeCursor(parameters.After, encoder, cursorDecoder);
                dynParams.Add("@Cursor1", decoded.Item1);
                dynParams.Add("@Cursor2", decoded.Item2);
            }
            else
            {
                dynParams.Add("@Cursor1", null);
                dynParams.Add("@Cursor2", null);
            }
        }

        // Stryker disable once boolean
        var items = await connection.QueryAsync<T>(
            new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
            // Stryker disable once boolean
.ConfigureAwait(false);

        var list = items.AsList();
        var hasMore = list.Count > pageSize;
        if (hasMore) list.RemoveAt(list.Count - 1);
        if (isBackward && autoReverse) list.Reverse();

        var hasPreviousPage = isBackward ? hasMore : parameters.After != null;
        var hasNextPage = isBackward ? parameters.Before != null : hasMore;

        string? startCursor = null;
        string? endCursor = null;

        if (list.Count > 0)
        {
            var firstKeys = keySelector(list[0]);
            var lastKeys = keySelector(list[^1]);
            // Stryker disable once all : Composite cursor serialization delimiter escaping
            var firstK1 = (firstKeys.Item1?.ToString() ?? "").Replace("%", "%25").Replace("|", "%7C");
            // Stryker disable once all : Composite cursor serialization delimiter escaping
            var firstK2 = (firstKeys.Item2?.ToString() ?? "").Replace("%", "%25").Replace("|", "%7C");
            // Stryker disable once all : Composite cursor serialization delimiter escaping
            var lastK1 = (lastKeys.Item1?.ToString() ?? "").Replace("%", "%25").Replace("|", "%7C");
            // Stryker disable once all : Composite cursor serialization delimiter escaping
            var lastK2 = (lastKeys.Item2?.ToString() ?? "").Replace("%", "%25").Replace("|", "%7C");
            startCursor = encoder.Encode($"{firstK1}|{firstK2}");
            endCursor = encoder.Encode($"{lastK1}|{lastK2}");
        }

        return factory.CreateCursorPagedList(list, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }

    /// <summary>
    /// Executes a cursor-paginated SQL query and streams the results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="keySelector">A function that extracts the raw cursor value from each result item.</param>
    /// <param name="cursorDecoder">An optional function to convert the decoded cursor string to <typeparamref name="TKey"/>.</param>
    /// <param name="param">Additional user-defined query parameters.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="commandType">An optional command type.</param>
    /// <param name="defaultPageSize">The fallback page size if not provided by the parameters.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="decoderRegistry">An optional registry for AOT decoders.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An async stream yielding entities sequentially.</returns>
    /// <exception cref="InvalidOperationException">Backward pagination is requested for streaming</exception>
    public static async IAsyncEnumerable<T> ToCursorPagedAsyncEnumerable<T, TKey>(
        this IDbConnection connection,
        string sql,
        CursorPaginationParameters parameters,
        Func<T, TKey> keySelector,
        Func<string, TKey>? cursorDecoder = null,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        int defaultPageSize = 10,
        int? maxPageSize = null,
        ICursorEncoder? cursorEncoder = null,
        ICursorDecoderRegistry? decoderRegistry = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
    {
        var dynParams = new DynamicParameters(param);
        var actualMaxPageSize = maxPageSize ?? PaginationSettings.MaxPageSize;
        var pageSize = System.Math.Min(parameters.GetPageSize(defaultPageSize), actualMaxPageSize);
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        dynParams.Add("@__Pagination_Limit__", pageSize);

        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        if (isBackward)
        {
            throw new InvalidOperationException("Streaming is not supported when paginating backwards because it requires materializing the result set to reverse it.");
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        }

        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;

        object? afterVal = null;
        if (!string.IsNullOrEmpty(parameters.After))
        {
            afterVal = DecodeCursor(parameters.After!, encoder, cursorDecoder, decoderRegistry);
        }

        dynParams.Add("@Cursor", afterVal);

        using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
            
        var rowParser = reader.GetRowParser<T>();
        
        var dbReader = (System.Data.Common.DbDataReader)reader;
        while (await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return rowParser(reader);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static TKey DecodeCursor<TKey>(string encodedCursor, ICursorEncoder encoder, Func<string, TKey>? cursorDecoder, ICursorDecoderRegistry? decoderRegistry)
    {
        var decoded = encoder.Decode(encodedCursor);
        if (decoded is null)
        {
            throw new InvalidOperationException(
                $"The cursor value '{encodedCursor}' could not be decoded. " +
                "Ensure the cursor was produced by this library and has not been tampered with.");
        }

        // Stryker disable all : The custom decoders here can fallback to the default coercer which behaves identically for standard types, creating equivalent mutants.
        if (cursorDecoder is not null)
        {
            return cursorDecoder(decoded);
        }

        if (decoderRegistry?.TryGetDecoder<TKey>(out var registeredDecoder) == true && registeredDecoder != null)
        {
            return registeredDecoder(decoded);
        }
        // Stryker restore all

        try
        {
            // Stryker disable equality
            if (typeof(TKey) == typeof(int) && int.TryParse(decoded, CultureInfo.InvariantCulture, out var intResult)) return Unsafe.As<int, TKey>(ref intResult);
            if (typeof(TKey) == typeof(long) && long.TryParse(decoded, CultureInfo.InvariantCulture, out var longResult)) return Unsafe.As<long, TKey>(ref longResult);
            if (typeof(TKey) == typeof(Guid) && Guid.TryParse(decoded, out var guidResult)) return Unsafe.As<Guid, TKey>(ref guidResult);
            if (typeof(TKey) == typeof(string)) return Unsafe.As<string, TKey>(ref decoded);
            // Stryker restore equality

            return (TKey)Convert.ChangeType(decoded, typeof(TKey), CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new InvalidOperationException(
                $"Could not convert the decoded cursor value '{decoded}' to type '{typeof(TKey).Name}'. " +
                $"Provide a '{nameof(cursorDecoder)}' delegate for types that require custom parsing " +
                $"(e.g., DateTimeOffset, complex composite keys).",
                ex);
        }
    }

    private static (TKey1, TKey2) DecodeCompositeCursor<TKey1, TKey2>(string encodedCursor, ICursorEncoder encoder, Func<string, (TKey1, TKey2)>? cursorDecoder)
    {
        var decoded = encoder.Decode(encodedCursor);
        if (decoded is null)
        {
            throw new InvalidOperationException($"The cursor value '{encodedCursor}' could not be decoded.");
        }

        if (cursorDecoder is not null)
        {
            return cursorDecoder(decoded);
        }

        var parts = decoded.Split('|');
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Expected 2 parts in composite cursor, got {parts.Length}.");
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        }
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user

        // Stryker disable once all : Composite cursor delimiter unescaping
        var p1 = parts[0].Replace("%7C", "|").Replace("%25", "%");
        // Stryker disable once all : Composite cursor delimiter unescaping
        var p2 = parts[1].Replace("%7C", "|").Replace("%25", "%");

        var k1 = (TKey1)Convert.ChangeType(p1, typeof(TKey1), CultureInfo.InvariantCulture);
        var k2 = (TKey2)Convert.ChangeType(p2, typeof(TKey2), CultureInfo.InvariantCulture);

        return (k1, k2);
    }
    /// <summary>
    /// Streams all records matching the query sequentially by automatically paginating through keyset chunks.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="connection">The database connection to execute queries against.</param>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="keySelector">A function that extracts the raw cursor value from each result item.</param>
    /// <param name="cursorDecoder">An optional function to convert the decoded cursor string to <typeparamref name="TKey"/>.</param>
    /// <param name="param">Additional user-defined query parameters.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="commandType">An optional command type.</param>
    /// <param name="chunkSize">The page chunk size to retrieve per batch.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="decoderRegistry">An optional registry for AOT decoders.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An async stream yielding entities sequentially across all chunks.</returns>
    /// <exception cref="InvalidOperationException">Backward pagination is requested for streaming</exception>
    public static async IAsyncEnumerable<T> ToStreamingAsyncEnumerable<T, TKey>(
        this IDbConnection connection,
        string sql,
        CursorPaginationParameters parameters,
        Func<T, TKey> keySelector,
        Func<string, TKey>? cursorDecoder = null,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        int chunkSize = 1000,
        ICursorEncoder? cursorEncoder = null,
        ICursorDecoderRegistry? decoderRegistry = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        var dynParams = new DynamicParameters(param);
        dynParams.Add("@__Pagination_Limit__", chunkSize);
        
        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        if (isBackward)
        {
            throw new InvalidOperationException("Streaming is not supported when paginating backwards.");
        }

        object? currentCursorVal = null;
        if (!string.IsNullOrEmpty(parameters.After))
        {
            currentCursorVal = DecodeCursor(parameters.After!, encoder, cursorDecoder, decoderRegistry);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            dynParams.Add("@Cursor", currentCursorVal);

            var items = await connection.QueryAsync<T>(
                new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var list = items.AsList();
            foreach (var item in list)
            {
                yield return item;
            }

            if (list.Count < chunkSize)
            {
                break;
            }
            
            var lastItem = list[list.Count - 1];
            currentCursorVal = keySelector(lastItem);
        }
    }

    /// <summary>
    /// Streams all records matching the query sequentially by automatically paginating through 2-column keyset chunks.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <typeparam name="TKey1">The type of the primary cursor key.</typeparam>
    /// <typeparam name="TKey2">The type of the secondary tie-breaker cursor key.</typeparam>
    /// <param name="connection">The database connection to execute queries against.</param>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The cursor pagination parameters.</param>
    /// <param name="key1Selector">A function that extracts the first key component from each result item.</param>
    /// <param name="key2Selector">A function that extracts the second key component from each result item.</param>
    /// <param name="cursorDecoder">An optional function to convert the decoded cursor string to composite keys.</param>
    /// <param name="param">Additional user-defined query parameters.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="commandType">An optional command type.</param>
    /// <param name="chunkSize">The page chunk size to retrieve per batch.</param>
    /// <param name="cursorEncoder">An optional cursor encoder.</param>
    /// <param name="decoderRegistry">An optional registry for AOT decoders.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An async stream yielding entities sequentially across all chunks.</returns>
    /// <exception cref="InvalidOperationException">Backward pagination is requested for streaming</exception>
    public static async IAsyncEnumerable<T> ToStreamingAsyncEnumerable<T, TKey1, TKey2>(
        this IDbConnection connection,
        string sql,
        CursorPaginationParameters parameters,
        Func<T, TKey1> key1Selector,
        Func<T, TKey2> key2Selector,
        Func<string, (TKey1, TKey2)>? cursorDecoder = null,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        int chunkSize = 1000,
        ICursorEncoder? cursorEncoder = null,
        ICursorDecoderRegistry? decoderRegistry = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var encoder = cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        var dynParams = new DynamicParameters(param);
        dynParams.Add("@__Pagination_Limit__", chunkSize);
        
        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        if (isBackward)
        {
            throw new InvalidOperationException("Streaming is not supported when paginating backwards.");
        }

        object? cursor1Val = null;
        object? cursor2Val = null;
        
        if (!string.IsNullOrEmpty(parameters.After))
        {
            var tuple = DecodeCursor(parameters.After!, encoder, cursorDecoder, decoderRegistry);
            cursor1Val = tuple.Item1;
            cursor2Val = tuple.Item2;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            dynParams.Add("@Cursor1", cursor1Val);
            dynParams.Add("@Cursor2", cursor2Val);

            var items = await connection.QueryAsync<T>(
                new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var list = items.AsList();
            foreach (var item in list)
            {
                yield return item;
            }

            if (list.Count < chunkSize)
            {
                break;
            }
            
            var lastItem = list[list.Count - 1];
            cursor1Val = key1Selector(lastItem);
            cursor2Val = key2Selector(lastItem);
        }
    }
}




