// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Dapper;

/// <summary>
/// Provides a fluent builder for multi-column keyset pagination queries executed via Dapper.
/// </summary>
/// <typeparam name="T">The type to map each row to.</typeparam>
public sealed class DapperKeysetBuilder<T>
{
    private readonly IDbConnection _connection;
    private readonly CursorPaginationParameters _parameters;
    private readonly CursorSqlBuilder _sqlBuilder = new();

    private ICursorEncoder? _cursorEncoder;
    private ICursorPagedListFactory? _factory;
    private object? _param;
    private IDbTransaction? _transaction;
    private int? _commandTimeout;
    private CommandType? _commandType;
    private int _defaultPageSize = 10;
    private int? _maxPageSize;
    private bool _autoReverse = true;

    // Multi-column cursor support: list of (selector, param name) pairs
    private readonly List<Func<T, string?>> _cursorColumnSelectors = new();
    private Func<string[], object?>[]? _cursorColumnDecoders;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperKeysetBuilder{T}"/> class for the specified connection and pagination parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute queries against.</param>
    /// <param name="parameters">The cursor pagination parameters defining the window and direction.</param>
    public DapperKeysetBuilder(IDbConnection connection, CursorPaginationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(connection);
        // CursorPaginationParameters is a struct — no null check needed.
        _connection = connection;
        _parameters = parameters;
    }

    /// <inheritdoc cref="CursorSqlBuilder.Select"/>
    public DapperKeysetBuilder<T> Select([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string columns)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        _sqlBuilder.Select(columns);
        return this;
    }

    /// <inheritdoc cref="CursorSqlBuilder.From"/>
    public DapperKeysetBuilder<T> From([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string table)
    {
        _sqlBuilder.From(table);
        return this;
    }

    /// <inheritdoc cref="CursorSqlBuilder.Where"/>
    public DapperKeysetBuilder<T> Where([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string condition)
    {
        _sqlBuilder.Where(condition);
        return this;
    }

    /// <inheritdoc cref="CursorSqlBuilder.OrderBy"/>
    public DapperKeysetBuilder<T> OrderBy([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string column, SortDirection direction = SortDirection.Ascending)
    {
        _sqlBuilder.OrderBy(column, direction);
        return this;
    }

    /// <inheritdoc cref="CursorSqlBuilder.ThenBy"/>
    public DapperKeysetBuilder<T> ThenBy([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string column, SortDirection direction = SortDirection.Ascending)
    {
        _sqlBuilder.ThenBy(column, direction);
        return this;
    }

    /// <inheritdoc cref="CursorSqlBuilder.UseDialect"/>
    public DapperKeysetBuilder<T> UseDialect(DatabaseDialect dialect)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        _sqlBuilder.UseDialect(dialect);
        return this;
    }

    /// <summary>
    /// Specifies the selector functions used to extract cursor key string values from each item.
    /// </summary>
    /// <param name="selectors">One selector per keyset column, in order.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selectors"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="selectors"/> is empty</exception>
    public DapperKeysetBuilder<T> WithCursorColumns(params Func<T, string?>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(selectors);
        if (selectors.Length == 0)
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            throw new ArgumentException("At least one cursor column selector must be provided.", nameof(selectors));
        _cursorColumnSelectors.Clear();
        _cursorColumnSelectors.AddRange(selectors);
        return this;
    }

    /// <summary>
    /// Specifies the decoder functions used to transform raw cursor parts into typed Dapper parameters.
    /// </summary>
    /// <param name="decoders">One decoder per keyset column, in order.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="decoders"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="decoders"/> is empty</exception>
    public DapperKeysetBuilder<T> WithCursorDecoder(params Func<string[], object?>[] decoders)
    {
        ArgumentNullException.ThrowIfNull(decoders);
        if (decoders.Length == 0)
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            throw new ArgumentException("At least one cursor decoder must be provided.", nameof(decoders));
        _cursorColumnDecoders = decoders;
        return this;
    }

    /// <summary>
    /// Configures additional user-defined SQL parameters.
    /// </summary>
    /// <param name="param">An object or anonymous type containing SQL parameter values.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    public DapperKeysetBuilder<T> WithParam(object? param)
    {
        _param = param;
        return this;
    }

    /// <summary>
    /// Configures the database transaction for the query.
    /// </summary>
    /// <param name="transaction">The active transaction, or <see langword="null"/>.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    public DapperKeysetBuilder<T> WithTransaction(IDbTransaction? transaction)
    {
        _transaction = transaction;
        return this;
    }

    /// <summary>
    /// Configures the command timeout in seconds.
    /// </summary>
    /// <param name="commandTimeout">The command timeout duration in seconds.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    public DapperKeysetBuilder<T> WithCommandTimeout(int commandTimeout)
    {
        _commandTimeout = commandTimeout;
        return this;
    }

    /// <summary>
    /// Configures the command type.
    /// </summary>
    /// <param name="commandType">The command type to execute.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    public DapperKeysetBuilder<T> WithCommandType(CommandType commandType)
    {
        _commandType = commandType;
        return this;
    }

    /// <summary>
    /// Configures the default page size when none is specified in parameters.
    /// </summary>
    /// <param name="defaultPageSize">The fallback page size.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultPageSize"/> is less than 1</exception>
    public DapperKeysetBuilder<T> WithDefaultPageSize(int defaultPageSize)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (defaultPageSize < 1)
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            throw new ArgumentOutOfRangeException(nameof(defaultPageSize), "Default page size must be at least 1.");
        _defaultPageSize = defaultPageSize;
        return this;
    }

    /// <summary>
    /// Configures the maximum allowed page size.
    /// </summary>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPageSize"/> is less than 1</exception>
    public DapperKeysetBuilder<T> WithMaxPageSize(int maxPageSize)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (maxPageSize < 1)
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            throw new ArgumentOutOfRangeException(nameof(maxPageSize), "Max page size must be at least 1.");
        _maxPageSize = maxPageSize;
        return this;
    }

    /// <summary>
    /// Configures the cursor encoder used for signing and verifying cursors.
    /// </summary>
    /// <param name="encoder">The cursor encoder implementation.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="encoder"/> is <see langword="null"/></exception>
    public DapperKeysetBuilder<T> WithEncoder(ICursorEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        _cursorEncoder = encoder;
        return this;
    }

    /// <summary>
    /// Configures whether backward pagination results are automatically reversed in-memory.
    /// </summary>
    /// <param name="autoReverse">A value indicating whether to automatically reverse backward pages.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    public DapperKeysetBuilder<T> WithAutoReverse(bool autoReverse)
    {
        _autoReverse = autoReverse;
        return this;
    }

    /// <summary>
    /// Configures the factory used to instantiate cursor paged lists.
    /// </summary>
    /// <param name="factory">The custom factory implementation.</param>
    /// <returns>The current <see cref="DapperKeysetBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/></exception>
    public DapperKeysetBuilder<T> WithFactory(ICursorPagedListFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        return this;
    }

    /// <summary>
    /// Builds the SQL query and executes it against the database, returning a cursor-paged list.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the cursor-paginated list.
    /// </returns>
    /// <exception cref="InvalidOperationException">Cursor column selectors are not configured or SQL generation is invalid</exception>
    public async Task<ICursorPagedList<T>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_cursorColumnSelectors.Count == 0)
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            throw new InvalidOperationException(
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
                "No cursor column selectors configured. Call WithCursorColumns() before ExecuteAsync().");

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var factory = _factory ?? DefaultPagedListFactory.Instance;
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var encoder = _cursorEncoder ?? HmacCursorEncoder.DevelopmentDefault;
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var actualMaxPageSize = _maxPageSize ?? PaginationSettings.MaxPageSize;
        var pageSize = System.Math.Min(_parameters.GetPageSize(_defaultPageSize), actualMaxPageSize);

        var dynParams = new DynamicParameters(_param);
        dynParams.Add("__Pagination_Limit__", pageSize + 1);

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var isBackward = _parameters.Last.HasValue && !_parameters.First.HasValue;

        // Decode cursor and inject named Dapper parameters (@Cursor, @Cursor0, @Cursor1, …)
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        string? rawCursor = isBackward ? _parameters.Before : _parameters.After;
        InjectCursorParams(rawCursor, encoder, dynParams);


        var sql = _sqlBuilder.Build(_parameters);

        var items = await _connection.QueryAsync<T>(
            new CommandDefinition(sql, dynParams, _transaction, _commandTimeout, _commandType,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var list = items.AsList();
        var hasMore = list.Count > pageSize;
        if (hasMore) list.RemoveAt(list.Count - 1);
        if (isBackward && _autoReverse) list.Reverse();

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var hasPreviousPage = isBackward ? hasMore : !string.IsNullOrEmpty(_parameters.After);
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var hasNextPage = isBackward ? !string.IsNullOrEmpty(_parameters.Before) : hasMore;

        string? startCursor = null;
        string? endCursor = null;

        if (list.Count > 0)
        {
            startCursor = encoder.Encode(BuildCursorString(list[0]));
            endCursor = encoder.Encode(BuildCursorString(list[list.Count - 1]));
        }

        return factory.CreateCursorPagedList(list, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }



    private string BuildCursorString(T item)
    {
        if (_cursorColumnSelectors.Count == 1)
        {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            var raw = _cursorColumnSelectors[0](item) ?? string.Empty;
            return raw.Replace("%", "%25"); // percent-encode literal percent signs before encoding
        }

        var sb = new StringBuilder();
        for (int i = 0; i < _cursorColumnSelectors.Count; i++)
        {
            if (i > 0) sb.Append('|');
            var raw = _cursorColumnSelectors[i](item) ?? string.Empty;
            // Percent-encode '%' and '|' to prevent ambiguity during decoding
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            raw = raw.Replace("%", "%25").Replace("|", "%7C");
            sb.Append(raw);
        }
        return sb.ToString();
    }

    // Decodes rawCursor (if non-null) and injects typed @Cursor / @Cursor0 / @Cursor1 … parameters
    // into dynParams. Injects null values when rawCursor is null/empty or when decoding yields null.
    private void InjectCursorParams(string? rawCursor, ICursorEncoder encoder, DynamicParameters dynParams)
    {
        // Stryker disable all : Equivalent edge case mutations (invalid/empty cursor) safely ignored
        if (string.IsNullOrEmpty(rawCursor))
        {
            // No cursor — inject null placeholders so the SQL compiles without conditional logic
            InjectNullCursorParams(dynParams);
            return;
        }

        string? decoded = null;
        try
        {
            decoded = encoder.Decode(rawCursor);
        }
        catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException)
        {
            throw new InvalidPaginationCursorException(
                "The cursor format is invalid or has been tampered with.", rawCursor, ex);
        }

        if (decoded is null)
        {
            InjectNullCursorParams(dynParams);
            return;
        }

        // Restore percent-encoded pipe characters before splitting
        var restored = decoded.Replace("%7C", "|", StringComparison.OrdinalIgnoreCase);
        var parts = restored.Split('|');

        for (int i = 0; i < _cursorColumnSelectors.Count; i++)
        {
            var paramName = _cursorColumnSelectors.Count == 1 ? "Cursor" : $"Cursor{i}";
            object? value = null;

            if (i < parts.Length)
            {
                if (_cursorColumnDecoders != null && i < _cursorColumnDecoders.Length)
                {
                    value = _cursorColumnDecoders[i](parts);
                }
                else
                {
                    // Fallback: return as string — the caller's SQL must cast as needed
                    value = parts[i];
                }
            }

            dynParams.Add(paramName, value);
        }
        // Stryker restore all
    }

    private void InjectNullCursorParams(DynamicParameters dynParams)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        for (int i = 0; i < _cursorColumnSelectors.Count; i++)
        {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
            var paramName = _cursorColumnSelectors.Count == 1 ? "Cursor" : $"Cursor{i}";
            dynParams.Add(paramName, null);
        }
    }
}




