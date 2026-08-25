// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

internal sealed class MappedPagedList<TSource, TResult> : IPagedList<TResult>
{
    private readonly IPagedList<TSource> _source;
    private readonly Func<TSource, TResult> _selector;

    public MappedPagedList(IPagedList<TSource> source, Func<TSource, TResult> selector)
    {
        _source = source;
        _selector = selector;
    }

    public long? TotalCount => _source.TotalCount;
    public int Page => _source.Page;
    public int PageSize => _source.PageSize;
    public long? TotalPages => _source.TotalPages;
    public bool HasPreviousPage => _source.HasPreviousPage;
    public bool HasNextPage => _source.HasNextPage;
    public int Count => _source.Count;

    /// <summary>
    /// Gets the element at the specified index.
    /// Warning: Each index access re-evaluates the selector. For eager mapping, call ToList().
    /// </summary>
    public TResult this[int index] => _selector(_source[index]);

    public IEnumerator<TResult> GetEnumerator()
    {
        foreach (var item in _source)
        {
            yield return _selector(item);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
