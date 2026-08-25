// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

#pragma warning disable CA1063
#pragma warning disable CA1819
#pragma warning disable CA1816
namespace EricksonLopez.Pagination.MongoDB.Tests;

public class MockMongoQueryable<T> : IOrderedQueryable<T>, IAsyncCursorSource<T>
{
    private readonly IQueryable<T> _queryable;

    public MockMongoQueryable(IEnumerable<T> items)
    {
        _queryable = items.AsQueryable();
        Provider = new MockMongoQueryProvider<T>(_queryable.Provider);
    }

    public MockMongoQueryable(IQueryable<T> queryable)
    {
        _queryable = queryable;
        Provider = new MockMongoQueryProvider<T>(_queryable.Provider);
    }

    public Type ElementType => _queryable.ElementType;
    public Expression Expression => _queryable.Expression;
    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IAsyncCursor<T> ToCursor(CancellationToken cancellationToken = default)
    {
        return new MockAsyncCursor<T>(_queryable);
    }

    public Task<IAsyncCursor<T>> ToCursorAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IAsyncCursor<T>>(new MockAsyncCursor<T>(_queryable));
    }
}

public class MockMongoQueryProvider<T> : IMongoQueryProvider
{
    private readonly IQueryProvider _provider;

    public global::MongoDB.Bson.BsonDocument[]? LoggedStages => null;

    public MockMongoQueryProvider(IQueryProvider provider)
    {
        _provider = provider;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new MockMongoQueryable<T>(_provider.CreateQuery<T>(expression));
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new MockMongoQueryable<TElement>(_provider.CreateQuery<TElement>(expression));
    }

    public object? Execute(Expression expression)
    {
        return _provider.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _provider.Execute<TResult>(expression);
    }

    public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Execute<TResult>(expression));
    }
}

public class MockAsyncCursor<T> : IAsyncCursor<T>
{
    private readonly IEnumerable<T> _data;
    private bool _moved;

    public MockAsyncCursor(IEnumerable<T> data)
    {
        _data = data;
    }

    public IEnumerable<T> Current => _moved ? _data : Enumerable.Empty<T>();

    public void Dispose() { }

    public bool MoveNext(CancellationToken cancellationToken = default)
    {
        var wasMoved = _moved;
        _moved = true;
        return !wasMoved;
    }

    public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MoveNext(cancellationToken));
    }
}



