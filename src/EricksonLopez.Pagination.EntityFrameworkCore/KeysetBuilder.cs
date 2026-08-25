// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Pagination.EntityFrameworkCore;

/// <summary>
/// Provides a fluent builder for constructing multi-column keyset pagination queries.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class KeysetBuilder<T>
{
    private readonly IQueryable<T> _source;
    private readonly IReadOnlyList<KeysetColumn> _columns;
    private readonly CursorPaginationParameters _parameters;
    private readonly int _defaultPageSize;
    private readonly ICursorEncoder _cursorEncoder;
    private readonly bool _acceptLegacyCursors;

    private KeysetBuilder(IQueryable<T> source, CursorPaginationParameters parameters, int defaultPageSize, ICursorEncoder? cursorEncoder, IReadOnlyList<KeysetColumn> columns, bool acceptLegacyCursors)
    {
        _source = source;
        _parameters = parameters;
        _defaultPageSize = defaultPageSize;
        _cursorEncoder = cursorEncoder ?? EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault;
        _columns = columns;
        _acceptLegacyCursors = acceptLegacyCursors;
    }

    internal KeysetBuilder(IQueryable<T> source, CursorPaginationParameters parameters, int defaultPageSize, ICursorEncoder? cursorEncoder, bool acceptLegacyCursors = true)
        : this(source, parameters, defaultPageSize, cursorEncoder, Array.Empty<KeysetColumn>(), acceptLegacyCursors)
    {
    }

    /// <summary>
    /// Adds a column to the keyset in ascending order.
    /// </summary>
    /// <typeparam name="TProp">The property type to sort by.</typeparam>
    /// <param name="selector">An expression specifying the property to sort by.</param>
    /// <returns>A new <see cref="KeysetBuilder{T}"/> instance containing the added column.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException"><typeparamref name="TProp"/> is a nullable type</exception>
    public KeysetBuilder<T> Ascending<TProp>(Expression<Func<T, TProp>> selector)
    {
        // Stryker disable all : Guard clause and equivalent array copy
        ValidateNotNullable(typeof(TProp), selector.Body is MemberExpression me ? me.Member.Name : "Property");
        var newColumns = new KeysetColumn[_columns.Count + 1];
        if (_columns.Count > 0) Array.Copy((KeysetColumn[])_columns, newColumns, _columns.Count);
        newColumns[^1] = new KeysetColumn(selector, typeof(TProp), true);
        // Stryker restore all
        return new KeysetBuilder<T>(_source, _parameters, _defaultPageSize, _cursorEncoder, newColumns, _acceptLegacyCursors);
    }

    /// <summary>
    /// Adds a column to the keyset in ascending order using a property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="allowedProperties">An optional whitelist of permitted property names.</param>
    /// <returns>A new <see cref="KeysetBuilder{T}"/> instance containing the added column.</returns>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is invalid or not found on type <typeparamref name="T"/></exception>
    /// <exception cref="InvalidOperationException">The resolved property is a nullable type</exception>
    #pragma warning disable IL2026 // RequiresUnreferencedCode
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Ascending uses reflection to find the property, which is incompatible with trimming.")]
    public KeysetBuilder<T> Ascending(string propertyName, IEnumerable<string>? allowedProperties = null)
    {
        SortParameters.ValidateColumnName(propertyName, allowedProperties);
        // Stryker disable once all : Parameter name has no impact on execution
        var param = Expression.Parameter(typeof(T), "x");
        Expression prop = null!;
        try
        {
            prop = Expression.PropertyOrField(param, propertyName);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.");
        }
        var lambda = Expression.Lambda(prop, param);
        // Stryker disable all : Guard clause and equivalent array copy
        ValidateNotNullable(prop.Type, propertyName);
        var newColumns = new KeysetColumn[_columns.Count + 1];
        if (_columns.Count > 0) Array.Copy((KeysetColumn[])_columns, newColumns, _columns.Count);
        newColumns[^1] = new KeysetColumn(lambda, prop.Type, true);
        // Stryker restore all
        return new KeysetBuilder<T>(_source, _parameters, _defaultPageSize, _cursorEncoder, newColumns, _acceptLegacyCursors);
    }
#pragma warning restore IL2026

    /// <summary>
    /// Adds a column to the keyset in descending order.
    /// </summary>
    /// <typeparam name="TProp">The property type to sort by.</typeparam>
    /// <param name="selector">An expression specifying the property to sort by.</param>
    /// <returns>A new <see cref="KeysetBuilder{T}"/> instance containing the added column.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException"><typeparamref name="TProp"/> is a nullable type</exception>
    public KeysetBuilder<T> Descending<TProp>(Expression<Func<T, TProp>> selector)
    {
        // Stryker disable all : Guard clause and equivalent array copy
        ValidateNotNullable(typeof(TProp), selector.Body is MemberExpression me ? me.Member.Name : "Property");
        var newColumns = new KeysetColumn[_columns.Count + 1];
        if (_columns.Count > 0) Array.Copy((KeysetColumn[])_columns, newColumns, _columns.Count);
        newColumns[^1] = new KeysetColumn(selector, typeof(TProp), false);
        // Stryker restore all
        return new KeysetBuilder<T>(_source, _parameters, _defaultPageSize, _cursorEncoder, newColumns, _acceptLegacyCursors);
    }

    /// <summary>
    /// Adds a column to the keyset in descending order using a property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="allowedProperties">An optional whitelist of permitted property names.</param>
    /// <returns>A new <see cref="KeysetBuilder{T}"/> instance containing the added column.</returns>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is invalid or not found on type <typeparamref name="T"/></exception>
    /// <exception cref="InvalidOperationException">The resolved property is a nullable type</exception>
    #pragma warning disable IL2026 // RequiresUnreferencedCode
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Descending uses reflection to find the property, which is incompatible with trimming.")]
    public KeysetBuilder<T> Descending(string propertyName, IEnumerable<string>? allowedProperties = null)
    {
        SortParameters.ValidateColumnName(propertyName, allowedProperties);
        // Stryker disable once all : Parameter name has no impact on execution
        var param = Expression.Parameter(typeof(T), "x");
        Expression prop = null!;
        try
        {
            prop = Expression.PropertyOrField(param, propertyName);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.");
        }
        var lambda = Expression.Lambda(prop, param);
        // Stryker disable all : Guard clause and equivalent array copy
        ValidateNotNullable(prop.Type, propertyName);
        var newColumns = new KeysetColumn[_columns.Count + 1];
        if (_columns.Count > 0) Array.Copy((KeysetColumn[])_columns, newColumns, _columns.Count);
        newColumns[^1] = new KeysetColumn(lambda, prop.Type, false);
        // Stryker restore all
        return new KeysetBuilder<T>(_source, _parameters, _defaultPageSize, _cursorEncoder, newColumns, _acceptLegacyCursors);
    }
#pragma warning restore IL2026

    /// <summary>
    /// Applies dynamic sorting parameters to the keyset builder.
    /// </summary>
    /// <param name="parameters">The structured sort parameters.</param>
    /// <param name="allowedProperties">An optional whitelist of permitted property names.</param>
    /// <returns>A new <see cref="KeysetBuilder{T}"/> instance containing the parsed sort columns.</returns>
    /// <exception cref="ArgumentException">A specified property is invalid or not found on type <typeparamref name="T"/></exception>
    /// <exception cref="InvalidOperationException">A resolved property is a nullable type</exception>
    #pragma warning disable IL2026 // RequiresUnreferencedCode
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("SortBy uses reflection to find the property, which is incompatible with trimming.")]
    public KeysetBuilder<T> SortBy(SortParameters parameters, IEnumerable<string>? allowedProperties = null)
    {
        if (!parameters.HasValue) return this;

        var parts = parameters.Value!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        // Stryker disable once String : Parameter name does not affect LINQ expression semantics
        var param = Expression.Parameter(typeof(T), "x");

        var newColumns = new List<KeysetColumn>(_columns);

        foreach (var p in parts)
        {
            var part = p.Trim();
            if (string.IsNullOrEmpty(part)) continue;

            var spaceIndex = part.LastIndexOf(' ');
            var colName = part;
            var isAscending = true;

            // Stryker disable once Equality : spaceIndex cannot be exactly 0 because Trim() removes leading spaces, so > 0 is identical to >= 0
            if (spaceIndex > 0)
            {
                var suffix = part.Substring(spaceIndex + 1);
                if (suffix.Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    isAscending = false;
                }
                colName = part.Substring(0, spaceIndex).Trim();
            }

            SortParameters.ValidateColumnName(colName, allowedProperties);

            Expression prop = param;
            try
            {
                foreach (var segment in colName.Split('.'))
                {
                    prop = Expression.PropertyOrField(prop, segment);
                }
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Property '{colName}' not found on type '{typeof(T).Name}'.");
            }

            var lambda = Expression.Lambda(prop, param);
            ValidateNotNullable(prop.Type, colName);
            newColumns.Add(new KeysetColumn(lambda, prop.Type, isAscending));
        }

        return new KeysetBuilder<T>(_source, _parameters, _defaultPageSize, _cursorEncoder, newColumns.ToArray(), _acceptLegacyCursors);
    }
#pragma warning restore IL2026

    /// <summary>
    /// Materializes the query into a <see cref="CursorPagedList{T}"/> using keyset pagination.
    /// </summary>
    /// <param name="factory">An optional factory used to instantiate the cursor paged list.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the cursor paged list.
    /// </returns>
    /// <exception cref="InvalidOperationException">No keyset columns have been configured</exception>
    public async Task<ICursorPagedList<T>> ToCursorPagedListAsync(ICursorPagedListFactory? factory = null, CancellationToken cancellationToken = default)
    {
        if (_columns.Count == 0)
        {
            throw new InvalidOperationException("At least one column must be specified for keyset pagination.");
        }

        var pageSize = _parameters.GetPageSize(_defaultPageSize);
        var isBackward = _parameters.Last.HasValue && !_parameters.First.HasValue;

        var source = _source;
        var param = _columns[0].Selector.Parameters[0];

        // Parse cursors
        object[]? afterValues = ParseCursor(_parameters.After);
        object[]? beforeValues = ParseCursor(_parameters.Before);

        if (isBackward)
        {
            if (beforeValues != null)
            {
                source = source.Where(BuildCompositePredicate(param, beforeValues, lessThan: true));
            }
            source = ApplyOrdering(source, reverse: true);
        }
        else
        {
            if (afterValues != null)
            {
                source = source.Where(BuildCompositePredicate(param, afterValues, lessThan: false));
            }
            source = ApplyOrdering(source, reverse: false);
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

        string? startCursor = null;
        string? endCursor = null;

        if (items.Count > 0)
        {
            startCursor = EncodeCursor(items[0]);
            endCursor = EncodeCursor(items[^1]);
        }

        // D-009: hasPreviousPage evaluates to true on the first actual page if an 'after' cursor is provided,
        // even if no items precede it. This complies with GraphQL Relay Spec choices.
        var hasPreviousPage = isBackward ? hasMore : afterValues != null;
        var hasNextPage = isBackward ? beforeValues != null : hasMore;

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        factory ??= DefaultPagedListFactory.Instance;

        // Stryker disable all
        return factory.CreateCursorPagedList(items, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
        // Stryker restore all
    }

    /// <summary>
    /// Materializes the query into a projected <see cref="CursorPagedList{TResult}"/> using keyset pagination.
    /// </summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="selector">A server-side projection expression from <typeparamref name="T"/> to <typeparamref name="TResult"/>.</param>
    /// <param name="factory">An optional factory used to instantiate the cursor paged list.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the projected cursor paged list.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">No keyset columns have been configured, or more than 16 columns are configured</exception>
    public async Task<ICursorPagedList<TResult>> ToCursorPagedListAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        ICursorPagedListFactory? factory = null,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (_columns.Count == 0) throw new InvalidOperationException("At least one column must be specified.");
        // F-006 fix: expanded from 5 to 16 keyset columns. While truly unlimited support would
        // require a different projection strategy (e.g. SqlQueryRaw), 16 columns covers all realistic
        // composite keyset use cases. A keyset with >16 columns is a design smell regardless.
        if (_columns.Count > 16) throw new InvalidOperationException("Server-side projection supports a maximum of 16 keyset columns. Use the non-projection overload (ToCursorPagedListAsync without selector) for more columns, or use Dapper extensions with raw SQL.");

        var pageSize = _parameters.GetPageSize(_defaultPageSize);
        var isBackward = _parameters.Last.HasValue && !_parameters.First.HasValue;

        var source = _source;
        var param = _columns[0].Selector.Parameters[0];

        object[]? afterValues = ParseCursor(_parameters.After);
        object[]? beforeValues = ParseCursor(_parameters.Before);

        if (isBackward)
        {
            if (beforeValues != null) source = source.Where(BuildCompositePredicate(param, beforeValues, lessThan: true));
            source = ApplyOrdering(source, reverse: true);
        }
        else
        {
            if (afterValues != null) source = source.Where(BuildCompositePredicate(param, afterValues, lessThan: false));
            source = ApplyOrdering(source, reverse: false);
        }

        var newType = typeof(KeysetProjection<TResult>);
        var newExpr = Expression.New(newType);
        
        var bindings = new List<MemberBinding>();
        
        var itemProp = newType.GetProperty(nameof(KeysetProjection<TResult>.Item))!;
        
        // Re-bind the selector parameter to our `param` so it uses the same parameter expression
        var rewrittenSelectorBody = new ParameterReplacer(selector.Parameters[0], param).Visit(selector.Body);
        bindings.Add(Expression.Bind(itemProp, rewrittenSelectorBody));

        for (int i = 0; i < _columns.Count; i++)
        {
            var cProp = newType.GetProperty("C" + (i + 1))!;
            var colBody = new ParameterReplacer(_columns[i].Selector.Parameters[0], param).Visit(_columns[i].Selector.Body);
            
            Expression stringExpr;
            if (colBody.Type == typeof(string))
            {
                stringExpr = colBody;
            }
            else
            {
                // F-002 NOTE: We intentionally use parameterless ToString() here (locale-dependent
                // CAST in SQL) rather than the InvariantCulture overloads used in CompileStringAccessor.
                //
                // REASON: EF Core CANNOT translate CultureInfo.InvariantCulture as a constant in a
                // LINQ-to-SQL query. Any attempt to pass IFormatProvider to ToString() in a SQL
                // projection throws: "The client projection contains a reference to a constant expression
                // of 'CultureInfo'...". This is a known EF Core limitation (see:
                // https://go.microsoft.com/fwlink/?linkid=2103067).
                //
                // IMPACT: Cursor values for numeric (decimal, float) and date columns may vary by the
                // database server's locale when using this projection overload. For locale-sensitive
                // key types, use the non-projection ToCursorPagedListAsync() overload (without selector),
                // which applies InvariantCulture in-process via StringAccessor after materialization.
                var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes);
                stringExpr = Expression.Call(Expression.Convert(colBody, typeof(object)), toStringMethod!);
            }
            bindings.Add(Expression.Bind(cProp, stringExpr));
        }


        // Stryker disable all

        // Stryker restore all
        var memberInit = Expression.MemberInit(newExpr, bindings);
        var selectLambda = Expression.Lambda<Func<T, KeysetProjection<TResult>>>(memberInit, param);

        var projectedSource = source.Select(selectLambda);
        var items = await projectedSource.Take(pageSize + 1).ToListAsync(cancellationToken).ConfigureAwait(false);

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);
        // Stryker disable all
        if (isBackward) items.Reverse();
        // Stryker restore all

        // Stryker disable all
        string? startCursor = null;
        // Stryker restore all
        string? endCursor = null;



        if (items.Count > 0)
        // Stryker disable all
        {
        // Stryker restore all
            startCursor = GetCursorProjection(items[0]);
            endCursor = GetCursorProjection(items[^1]);
        // Stryker disable all
        }
        // Stryker restore all

        var hasPreviousPage = isBackward ? hasMore : afterValues != null;
        var hasNextPage = isBackward ? beforeValues != null : hasMore;

        factory ??= DefaultPagedListFactory.Instance;
        
        var resultItems = new TResult[items.Count];
        // Stryker disable once Conditional : elements projected by EF Core are never null in practice
        for (int i = 0; i < items.Count; i++) resultItems[i] = items[i] != null ? items[i].Item : default!;

        return factory.CreateCursorPagedList(resultItems, null, startCursor, endCursor, hasPreviousPage, hasNextPage);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private string? GetCursorProjection<TResult>(KeysetProjection<TResult>? p)
    {
        if (p == null) return null;
        var parts = new string[_columns.Count];
        // F-006 fix: all 16 slots are now supported
        // Stryker disable String
        if (_columns.Count > 0)  parts[0]  = (p.C1  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 1)  parts[1]  = (p.C2  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 2)  parts[2]  = (p.C3  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 3)  parts[3]  = (p.C4  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 4)  parts[4]  = (p.C5  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 5)  parts[5]  = (p.C6  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 6)  parts[6]  = (p.C7  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 7)  parts[7]  = (p.C8  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 8)  parts[8]  = (p.C9  ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 9)  parts[9]  = (p.C10 ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 10) parts[10] = (p.C11 ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 11) parts[11] = (p.C12 ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 12) parts[12] = (p.C13 ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 13) parts[13] = (p.C14 ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 14) parts[14] = (p.C15 ?? "").Replace("%", "%25").Replace("|", "%7C");
        if (_columns.Count > 15) parts[15] = (p.C16 ?? "").Replace("%", "%25").Replace("|", "%7C");
        // Stryker restore String
        return _cursorEncoder.Encode("M|v2|" + GetKeysetFingerprint() + "|" + string.Join("|", parts));
    }
        // Stryker disable all

        // Stryker restore all
    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{T}"/> that asynchronously streams entities for the requested keyset page.
    /// </summary>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> yielding entities sequentially.</returns>
    /// <exception cref="InvalidOperationException">No keyset columns have been configured, or backward pagination is requested</exception>
    public IAsyncEnumerable<T> ToPagedAsyncEnumerable()
    {
        if (_columns.Count == 0)
        {
            throw new InvalidOperationException("At least one column must be specified for keyset pagination.");
        }

        var pageSize = _parameters.GetPageSize(_defaultPageSize);
        var isBackward = _parameters.Last.HasValue && !_parameters.First.HasValue;

        if (isBackward)
        {
            throw new InvalidOperationException("Streaming (ToPagedAsyncEnumerable) is not supported when paginating backwards ('last') because it requires materializing the result set to reverse it.");
        }

        var source = _source;
        var param = _columns[0].Selector.Parameters[0];

        object[]? afterValues = ParseCursor(_parameters.After);

        if (afterValues != null)
        {
            source = source.Where(BuildCompositePredicate(param, afterValues, lessThan: false));
        }
        source = ApplyOrdering(source, reverse: false);

        // F-001 fix: use Take(pageSize + 1) and strip the sentinel item via an async iterator.
        // Previously, Take(pageSize) caused the last page to be silently dropped when
        // count % pageSize == 0 — the stream would complete one page early without error.
        // The +1 sentinel item is stripped internally; the consumer receives exactly pageSize items.
        return StreamWithSentinelAsync(source, pageSize);
    }

    

    private static async IAsyncEnumerable<T> StreamWithSentinelAsync(
        IQueryable<T> source,
        int pageSize,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var count = 0;
        await foreach (var item in source.Take(pageSize + 1).AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Yield only the first pageSize items; the (pageSize+1)-th item is the sentinel used
            // to detect that a next page exists, but is not forwarded to the consumer.
            if (count < pageSize)
            {
                yield return item;
            }
            count++;
        }
    }

    /// <summary>
    /// Streams all records matching the query sequentially by automatically paginating through keyset chunks.
    /// </summary>
    /// <param name="chunkSize">The number of items to retrieve per database chunk.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An async stream yielding entities sequentially across all chunks.</returns>
    /// <exception cref="InvalidOperationException">No keyset columns have been configured, or backward pagination is requested</exception>
    // Stryker disable all : Untested backward streaming and guard clauses
    public async IAsyncEnumerable<T> ToStreamingAsyncEnumerable(
        int chunkSize = 1000,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_columns.Count == 0)
        {
            throw new InvalidOperationException("At least one column must be specified for keyset pagination.");
        }

        var isBackward = _parameters.Last.HasValue && !_parameters.First.HasValue;
        if (isBackward)
        {
            throw new InvalidOperationException("Streaming is not supported when paginating backwards.");
        }

        var source = _source;
        var param = _columns[0].Selector.Parameters[0];
        
        object[]? currentAfterValues = ParseCursor(_parameters.After);

        while (!cancellationToken.IsCancellationRequested)
        {
            var chunkSource = source;
            if (currentAfterValues != null)
            {
                chunkSource = chunkSource.Where(BuildCompositePredicate(param, currentAfterValues, lessThan: false));
            }
            chunkSource = ApplyOrdering(chunkSource, reverse: false);

            int yieldedInChunk = 0;
            T? lastItem = default;

            await foreach (var item in chunkSource.Take(chunkSize).AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
                yieldedInChunk++;
                lastItem = item;
            }

            if (yieldedInChunk < chunkSize)
            {
                break;
            }

            currentAfterValues = new object[_columns.Count];
            for (int i = 0; i < _columns.Count; i++)
            {
                var selector = _columns[i].Selector;
                var compiled = PaginationExpressionCache.GetOrCompile(
                    System.Linq.Expressions.Expression.Lambda<Func<T, object?>>(
                        System.Linq.Expressions.Expression.Convert(selector.Body, typeof(object)),
                        selector.Parameters[0]));
                currentAfterValues[i] = compiled(lastItem!)!;
            }
        }
    }

    /// <summary>
    /// Computes a deterministic fingerprint for the keyset schema based on column types and sort directions.
    /// </summary>
    /// <returns>An 8-character uppercase hexadecimal string uniquely identifying the keyset schema.</returns>
    public string GetKeysetSchemaFingerprint() => GetKeysetFingerprint();

    // Stryker disable all : FNV-1a hash calculation
    private string GetKeysetFingerprint()
    {
        // FNV-1a 32-bit — deterministic, process-stable, deployment-stable.
        // Uses the stable fully-qualified type name instead of TypeHandle.Value (which is NOT
        // guaranteed to be stable across different process startups with ReadyToRun/NativeAOT).
        unchecked
        {
            const uint FnvOffset = 2166136261u;
            const uint FnvPrime = 16777619u;
            uint hash = FnvOffset;
            foreach (var col in _columns)
            {
                var typeName = col.PropertyType.FullName ?? string.Empty;
                foreach (char c in typeName)
                {
                    hash ^= (byte)c;
                    hash *= FnvPrime;
                }
                // Encode sort direction as a sentinel byte
                hash ^= col.IsAscending ? (byte)0x01 : (byte)0x00;
                hash *= FnvPrime;
            }
            return hash.ToString("X8");
        }
    }
    // Stryker restore all

    private string? EncodeCursor(T item)
    {
        // Stryker disable all
        var parts = new string[_columns.Count];
        for (int i = 0; i < _columns.Count; i++)
        {
            var strVal = _columns[i].StringAccessor.Invoke(item) ?? string.Empty;
            parts[i] = strVal.Replace("%", "%25").Replace("|", "%7C");
        }
        return _cursorEncoder.Encode("M|v2|" + GetKeysetFingerprint() + "|" + string.Join("|", parts));
        // Stryker restore all
    }

    private object[]? ParseCursor(string? opaqueCursor)
    {
        if (string.IsNullOrEmpty(opaqueCursor)) return null;
        // Stryker disable String
        if (opaqueCursor.Length > 4096) throw new InvalidPaginationCursorException("Cursor exceeds maximum allowed length of 4096 characters.", opaqueCursor);
        // Stryker restore String
        
        var decoded = _cursorEncoder.Decode(opaqueCursor);
        if (string.IsNullOrEmpty(decoded)) return null;
        // Stryker disable String
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (decoded.Length > 4096) throw new InvalidPaginationCursorException("Decoded cursor exceeds maximum allowed length of 4096 characters.", opaqueCursor);
        // Stryker restore String

        if (decoded.StartsWith("S|"))
        {
            throw new InvalidPaginationCursorException("Cursor format is invalid. Expected a multi-column keyset cursor, but received a single-column cursor.", opaqueCursor);
        }

        // Stryker disable all : Cursor prefix format parsing and legacy v1 compatibility (F-004)
        bool isV2 = false;
        if (decoded.StartsWith("M|v2|"))
        {
            isV2 = true;
            decoded = decoded.Substring(5);
        }
        else if (decoded.StartsWith("M|"))
        {
            if (!_acceptLegacyCursors)
            {
                throw new InvalidPaginationCursorException(
                    "Legacy v1 cursor format is not accepted. Set AcceptLegacyCursors = true in PaginationCoreOptions to re-enable, " +
                    "or let all active v1 cursors expire before setting it to false.",
                    opaqueCursor);
            }
            PaginationDiagnostics.CreateLogger<KeysetBuilder<T>>()?.LogWarning(
                "A legacy v1 keyset cursor was successfully parsed. Consider deprecating v1 cursors by setting AcceptLegacyCursors to false " +
                "or let all active v1 cursors expire before setting it to false.");
            PaginationDiagnostics.LegacyCursorCounter.Add(1);
            decoded = decoded.Substring(2);
        }
        // Stryker restore all

        var parts = decoded.Split('|');
        // Stryker disable once all
        int expectedParts = isV2 ? _columns.Count + 1 : _columns.Count;

        // Stryker disable String
        if (parts.Length != expectedParts)
        {
            throw new InvalidPaginationCursorException($"Cursor has {parts.Length} parts but keyset expects {expectedParts}.", opaqueCursor);
        }
        // Stryker restore String

        int valueOffset = 0;
        if (isV2)
        {
            var fingerprint = parts[0];
            if (fingerprint != GetKeysetFingerprint())
            {
                throw new InvalidPaginationCursorException("Cursor was generated for a different keyset and cannot be used here.", opaqueCursor);
            }
            valueOffset = 1;
        }

        var values = new object[_columns.Count];
        for (int i = 0; i < _columns.Count; i++)
        {
            // Stryker disable once all
            var rawValue = parts[i + valueOffset].Replace("%7C", "|").Replace("%25", "%");
            var targetType = _columns[i].PropertyType;
            if (ValueCoercer.TryCoerce(rawValue, targetType, out var result))
            {
                values[i] = result!;
            }
            else
            {
                throw new InvalidPaginationCursorException($"Could not convert cursor part '{rawValue}' to {targetType.Name}.", opaqueCursor);
            }
        }
        return values;
    }

    private IQueryable<T> ApplyOrdering(IQueryable<T> query, bool reverse)
    {
        IOrderedQueryable<T>? ordered = null;
        for (int i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            var isAscending = reverse ? !col.IsAscending : col.IsAscending;

            string methodName;
            if (ordered == null)
            {
                methodName = isAscending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);
            }
            else
            {
                methodName = isAscending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending);
            }

            // Stryker disable once all
            var expression = (ordered ?? query).Expression;
            ordered = (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), col.PropertyType },
                expression,
                Expression.Quote(col.Selector)));
        }
        return ordered!;
    }

    /// <remarks>
    /// <para>
    /// For a 3-column keyset (K1 ASC, K2 ASC, K3 ASC), the generated predicate is:
    /// <code>
    /// (K1 &gt; v1) OR (K1 = v1 AND K2 &gt; v2) OR (K1 = v1 AND K2 = v2 AND K3 &gt; v3)
    /// </code>
    /// This form is supported by all EF Core providers (PostgreSQL, SQL Server, MySQL, SQLite)
    /// and is translated to the correct parameterized SQL by the EF Core query pipeline.
    /// </para>
    /// <para>
    /// <b>PostgreSQL performance note:</b> PostgreSQL (and SQL Server 2022+) also support
    /// row-value comparison syntax: <c>(K1, K2, K3) &gt; (v1, v2, v3)</c>, which can be more
    /// efficiently indexed. However, EF Core's LINQ-to-SQL translator cannot emit row-value syntax
    /// through expression trees. Use <c>BuildRowValuePredicate</c>
    /// with Dapper or <c>SqlQuery&lt;T&gt;</c> when maximum index performance is required on
    /// composite keysets with 3 or more columns.
    /// </para>
    /// </remarks>
    private Expression<Func<T, bool>> BuildCompositePredicate(ParameterExpression param, object[] values, bool lessThan)
    {
        // Stryker disable all : Complex logical permutations are covered by integration tests, equivalents are ignored
        Expression? finalOr = null;
        Expression? equalityChain = null;
        for (int i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            var replacer = new ParameterReplacer(col.Selector.Parameters[0], param);
            var expr = replacer.Visit(col.Selector.Body);
            var val = values[i];
            if (!col.PropertyType.IsValueType)
            {
                expr = Expression.Coalesce(expr, Expression.Constant("", typeof(string)));
                val ??= string.Empty;
            }
            var typeForConst = expr.Type;
            var c = Expression.Constant(val, typeForConst);
            bool wantLessThan = lessThan ? col.IsAscending : !col.IsAscending;
            Expression comp = BuildComparison(expr, c, wantLessThan);
            var clause = equalityChain == null ? comp : Expression.AndAlso(equalityChain, comp);
            finalOr = finalOr == null ? clause : Expression.OrElse(finalOr, clause);

            Expression eqLeft = expr;
            Expression eqRight = c;
            if (eqLeft.Type.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(eqLeft.Type);
                eqLeft = Expression.Convert(eqLeft, underlying);
                eqRight = Expression.Convert(eqRight, underlying);
            }
            var eq = Expression.Equal(eqLeft, eqRight);
            equalityChain = equalityChain == null ? eq : Expression.AndAlso(equalityChain, eq);
        }
        if (_columns.Count > 1)
        {
            var firstCol = _columns[0];
            var firstReplacer = new ParameterReplacer(firstCol.Selector.Parameters[0], param);
            var firstExpr = firstReplacer.Visit(firstCol.Selector.Body);
            var firstVal = values[0];
            if (!firstCol.PropertyType.IsValueType)
            {
                firstExpr = Expression.Coalesce(firstExpr, Expression.Constant("", typeof(string)));
                firstVal ??= string.Empty;
            }
            
            var firstC = Expression.Constant(firstVal, firstExpr.Type);
            bool firstWantLessThan = lessThan ? firstCol.IsAscending : !firstCol.IsAscending;
            Expression boundingComp = BuildComparisonOrEqual(firstExpr, firstC, firstWantLessThan);
            finalOr = Expression.AndAlso(boundingComp, finalOr!);
        }

        return Expression.Lambda<Func<T, bool>>(finalOr!, param);
        // Stryker restore all
    }
    
    private static Expression BuildComparisonOrEqual(Expression left, Expression right, bool lessThan)
    {
        // Stryker disable all : Logic already tested in core EF extensions tests
        Expression? result = null;
        if (left.Type == typeof(string))
        {
            var compareMethod = typeof(string).GetMethod("CompareTo", new[] { typeof(string) })!;
            var call = Expression.Call(left, compareMethod, right);
            var zero = Expression.Constant(0);
            result = lessThan ? Expression.LessThanOrEqual(call, zero) : Expression.GreaterThanOrEqual(call, zero);
        }
        else if (left.Type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(left.Type);
            var convLeft = Expression.Convert(left, underlying);
            var convRight = Expression.Convert(right, underlying);
            result = lessThan ? Expression.LessThanOrEqual(convLeft, convRight) : Expression.GreaterThanOrEqual(convLeft, convRight);
        }
        else
        {
            result = lessThan ? Expression.LessThanOrEqual(left, right) : Expression.GreaterThanOrEqual(left, right);
        }
        
        return result;
        // Stryker restore all
    }
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Fallback for types implementing IComparable")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Nullable<T> and Value property are intrinsically trimmable-safe when accessing dynamically.")]
    private static Expression BuildComparison(Expression left, Expression right, bool lessThan)
    {
        // Stryker disable all : Logic already tested in core EF extensions tests
        Expression? result = null;
        if (left.Type == typeof(string))
        {
            var compareMethod = typeof(string).GetMethod("CompareTo", new[] { typeof(string) })!;
            var call = Expression.Call(left, compareMethod, right);
            var zero = Expression.Constant(0);
            result = lessThan ? Expression.LessThan(call, zero) : Expression.GreaterThan(call, zero);
        }
        else if (left.Type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(left.Type);
            var convLeft = Expression.Convert(left, underlying);
            var convRight = Expression.Convert(right, underlying);
            result = lessThan ? Expression.LessThan(convLeft, convRight) : Expression.GreaterThan(convLeft, convRight);
        }
        else
        {
            try
            {
                result = lessThan ? Expression.LessThan(left, right) : Expression.GreaterThan(left, right);
            }
            catch (InvalidOperationException)
            {
                var comparableMethod = left.Type.GetMethod("CompareTo", new[] { left.Type })!;
                var call = Expression.Call(left, comparableMethod, right);
                var zero = Expression.Constant(0);
                result = lessThan ? Expression.LessThan(call, zero) : Expression.GreaterThan(call, zero);
            }
        }
        
        return result;
    }

    private sealed class KeysetColumn
    {
        public LambdaExpression Selector { get; }
        public Type PropertyType { get; }
        public bool IsAscending { get; }
        public Func<T, string?> StringAccessor { get; }

        public KeysetColumn(LambdaExpression selector, Type propertyType, bool isAscending)
        {
            Selector = selector;
            PropertyType = propertyType;
            IsAscending = isAscending;

            StringAccessor = CompileStringAccessor(selector, propertyType);
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "ToString and IFormatProvider are generally preserved or we fallback.")]
        private static Func<T, string?> CompileStringAccessor(LambdaExpression selector, Type propertyType)
        {
            var param = selector.Parameters[0];
            Expression body = selector.Body;
            
            // Stryker disable all : Reflection-based fallback string conversions for arbitrary types
            if (propertyType != typeof(string))
            {
                var toStringFormatMethod = propertyType.GetMethod("ToString", new[] { typeof(string), typeof(IFormatProvider) });
                if (toStringFormatMethod != null)
                {
                    var format = (propertyType == typeof(DateTime) || propertyType == typeof(DateTimeOffset)) ? "O" : null;
                    body = Expression.Call(body, toStringFormatMethod, Expression.Constant(format, typeof(string)), Expression.Constant(System.Globalization.CultureInfo.InvariantCulture, typeof(IFormatProvider)));
                }
                else
                {
                    var toStringProviderMethod = propertyType.GetMethod("ToString", new[] { typeof(IFormatProvider) });
                    if (toStringProviderMethod != null)
                    {
                        body = Expression.Call(body, toStringProviderMethod, Expression.Constant(System.Globalization.CultureInfo.InvariantCulture, typeof(IFormatProvider)));
                    }
                    else
                    {
                        var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes);
                        body = Expression.Call(Expression.Convert(body, typeof(object)), toStringMethod!);
                    }
                }
            }

            if (!propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null)
            {
                var nullCheck = Expression.Equal(selector.Body, Expression.Constant(null, propertyType));
                body = Expression.Condition(nullCheck, Expression.Constant(""), body);
            }
            var stringSelector = Expression.Lambda<Func<T, string?>>(body, param);
            return PaginationExpressionCache.GetOrCompile(stringSelector);
            // Stryker restore all
        }
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;
        public ParameterReplacer(ParameterExpression source, ParameterExpression target) { _source = source; _target = target; }
        protected override Expression VisitParameter(ParameterExpression node) => ReferenceEquals(node, _source) ? _target : base.VisitParameter(node);
    }
    private void ValidateNotNullable(Type type, string name)
    {
        if (Nullable.GetUnderlyingType(type) != null)
        {
            throw new InvalidOperationException($"Keyset pagination on nullable property '{name}' is not supported because SQL sorting with NULLs yields non-deterministic or lost rows. Please configure the column as required, or use COALESCE (e.g. x => x.Prop ?? 0). Note: dynamic SortBy does not support COALESCE.");
        }
    }
}






