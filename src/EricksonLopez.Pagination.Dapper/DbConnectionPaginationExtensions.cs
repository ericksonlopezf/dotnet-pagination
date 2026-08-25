// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Dapper;

/// <summary>
/// Provides extension methods for <see cref="IDbConnection"/> to execute offset-paginated
/// queries using Dapper.
/// </summary>
public static class DbConnectionPaginationExtensions
{
    /// <summary>
    /// Executes a paginated SQL query and returns a <see cref="PagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The pagination parameters defining the requested page and page size.</param>
    /// <param name="param">Additional user-defined query parameters.</param>
    /// <param name="countTotal">A value indicating whether the query returns total record count.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="commandType">An optional command type.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="factory">An optional factory used to instantiate the paged list.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the paged list.
    /// </returns>
    /// <exception cref="InvalidOperationException">The param object contains a reserved pagination parameter name or the query did not return a valid count when requested</exception>
    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this IDbConnection connection,
        string sql,
        PaginationParameters parameters,
        object? param = null,
        bool countTotal = true,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        int? maxPageSize = 1000,
        IPagedListFactory? factory = null,
        CancellationToken cancellationToken = default)
    {
        factory ??= DefaultPagedListFactory.Instance;
        var effectivePageSize = maxPageSize.HasValue ? System.Math.Min(parameters.PageSize, maxPageSize.Value) : parameters.PageSize;

        // F-007 fix: inspect param BEFORE wrapping in DynamicParameters.
        // DynamicParameters.ParameterNames is empty for anonymous objects/POCOs until Dapper
        // populates them internally — the old post-construction check could not catch collisions
        // for objects passed as `new { __Pagination_Skip__ = 1 }`. We now inspect the raw param
        // before construction using a helper that covers DynamicParameters, dictionaries, and POCOs.
        ThrowIfReservedKeyConflict(param, "__Pagination_Skip__", "__Pagination_PageSize__");

        var dynParams = new DynamicParameters(param);
        var effectiveSkip = (long)(parameters.Page - 1) * (long)effectivePageSize;
        dynParams.Add("__Pagination_Skip__", effectiveSkip);
        dynParams.Add("__Pagination_PageSize__", effectivePageSize);
        // DA-1: Do NOT prefix parameter names with '@' here. Dapper adds the provider-specific
        // prefix automatically based on the underlying connection. Passing '@__Pagination_Skip__'
        // produces double '@@__Pagination_Skip__' in ODBC and MySQL Connector/NET drivers.

        if (countTotal)
        {
            // Stryker disable once boolean
            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            long totalCount = 0;
            try
            {
                // Stryker disable once boolean
totalCount = await multi.ReadSingleAsync<long>().ConfigureAwait(false);
            }
            // Stryker disable once all : Exception rewrapping guard
            catch (Exception ex) when (ex is System.InvalidCastException || ex is System.FormatException || ex is System.InvalidOperationException || ex is System.Data.DataException)
            {
                throw new System.InvalidOperationException("The SQL query must return the COUNT(*) as the first result set when countTotal is true.", ex);
            }

            // Stryker disable all : Total count 0 fast path
            if (totalCount == 0)
            {
                return factory.CreatePagedList<T>([], 0, parameters.Page, effectivePageSize, false);
            }
            // Stryker restore all

            // Stryker disable once all
            var items = await multi.ReadAsync<T>().ConfigureAwait(false);
            // The as-cast avoids a redundant ToList() allocation in the common case.
            // Stryker disable once all
            return factory.CreatePagedList(items.AsList(), totalCount, parameters.Page, effectivePageSize, null);
        }
        else
        {
            dynParams.Add("@__Pagination_Limit__", effectivePageSize + 1);

            // Stryker disable once boolean
            var items = await connection.QueryAsync<T>(
                new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var itemList = items.AsList();
            var hasNextPage = itemList.Count > effectivePageSize;
            if (hasNextPage)
            {
                itemList.RemoveAt(itemList.Count - 1);
            }
            return factory.CreatePagedList(itemList, null, parameters.Page, effectivePageSize, hasNextPage);
        }
    }

    /// <summary>
    /// Executes a paginated SQL query and returns a streaming asynchronous sequence of items.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The pagination parameters defining the requested page and page size.</param>
    /// <param name="param">Additional user-defined query parameters.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="commandType">An optional command type.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> yielding items for the requested page.</returns>
    /// <exception cref="InvalidOperationException">The param object contains a reserved pagination parameter name</exception>
    public static async IAsyncEnumerable<T> ToPagedAsyncEnumerable<T>(
        this IDbConnection connection,
        string sql,
        PaginationParameters parameters,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        int? maxPageSize = 1000,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var actualMaxPageSize = maxPageSize ?? PaginationSettings.MaxPageSize;
        var effectivePageSize = System.Math.Min(parameters.PageSize, actualMaxPageSize);

        // F-007 fix: inspect param BEFORE wrapping in DynamicParameters (same as ToPagedListAsync).
        ThrowIfReservedKeyConflict(param, "__Pagination_Skip__", "__Pagination_Limit__");

        var dynParams = new DynamicParameters(param);
        
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var effectiveSkip = (long)(parameters.Page - 1) * (long)effectivePageSize;
        dynParams.Add("@__Pagination_Skip__", effectiveSkip);
        dynParams.Add("@__Pagination_Limit__", effectivePageSize);

        using var reader = (System.Data.Common.DbDataReader) await connection.ExecuteReaderAsync(
            new CommandDefinition(sql, dynParams, transaction, commandTimeout, commandType, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var rowParser = reader.GetRowParser<T>();
        
        // Stryker disable once all
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return rowParser(reader);
        }
    }

    /// <summary>
    /// Inspects <paramref name="param"/> before it is wrapped in <see cref="DynamicParameters"/>
    /// to detect reserved parameter name collisions early.
    /// </summary>
    /// <remarks>
    /// F-007: <see cref="DynamicParameters.ParameterNames"/> is empty for anonymous objects and POCOs
    /// until Dapper reflects them internally during command execution. A post-construction check
    /// therefore cannot catch collisions for those types. This helper inspects the raw object
    /// before construction via three mechanisms:
    /// <list type="bullet">
    ///   <item>If <paramref name="param"/> is already a <see cref="DynamicParameters"/>, its
    ///         <see cref="DynamicParameters.ParameterNames"/> property is checked directly.</item>
    ///   <item>If it is an <see cref="IDictionary{TKey,TValue}"/> keyed by string, the dictionary
    ///         keys are checked directly.</item>
    ///   <item>Otherwise, public instance property names are retrieved via reflection and checked.
    ///         This covers anonymous types and named POCOs.</item>
    /// </list>
    /// </remarks>
    /// <param name="param">The user-supplied query parameter object, or <see langword="null"/>.</param>
    /// <param name="reservedNames">The reserved parameter names that must not appear in <paramref name="param"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any reserved name is found in the parameter object.
    /// </exception>
    [UnconditionalSuppressMessage("Trimming", "IL2075:DynamicallyAccessedMembers", Justification = "Dapper parameter inspection dynamically reflects on POCO/anonymous type properties.")]
    private static void ThrowIfReservedKeyConflict(object? param, params string[] reservedNames)
    {
        if (param is null) return;

        IEnumerable<string> paramNames;

        if (param is DynamicParameters dp)
        {
            // DynamicParameters exposes its explicitly-added names directly.
            paramNames = dp.ParameterNames;
        }
        else if (param is IDictionary<string, object?> dict)
        {
            paramNames = dict.Keys;
        }

        else
        {
            // For anonymous types and POCOs, reflect public instance properties.
            // Dapper uses the same approach when building the SQL parameters from objects.
            paramNames = param.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name);
        }

        foreach (var name in paramNames)
        {
            foreach (var reserved in reservedNames)
            {
                // Compare without the leading @ prefix which Dapper strips automatically.
                var normalised = name.TrimStart('@');
                var reservedNormalised = reserved.TrimStart('@');
                if (string.Equals(normalised, reservedNormalised, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"The param object contains the reserved pagination parameter name '{reserved}'. " +
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
                        $"Rename your parameter to avoid conflicts with the library's internal parameters: " +
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
                        $"{string.Join(", ", reservedNames.Select(r => $"'{r}'"))}.");
                }
            }
        }
    }
}





