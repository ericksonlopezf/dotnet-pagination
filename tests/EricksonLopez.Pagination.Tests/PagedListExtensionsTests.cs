// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class PagedListExtensionsTests
{
    private sealed class DummyPagedList<T> : IPagedList<T>
    {
        private readonly List<T> _items = new();

        public DummyPagedList(IEnumerable<T> items)
        {
            _items.AddRange(items);
        }

        public long? TotalCount => _items.Count;
        public int Page => 1;
        public int PageSize => 10;
        public long? TotalPages => 1;
        public bool HasPreviousPage => false;
        public bool HasNextPage => false;
        public int Count => _items.Count;

        public T this[int index] => _items[index];

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DummyCountedPagedList<T> : ICountedPagedList<T>
    {
        private readonly List<T> _items = new();

        public DummyCountedPagedList(IEnumerable<T> items)
        {
            _items.AddRange(items);
        }

        public long? TotalCount => _items.Count;
        long ICountedPagedList.TotalCount => _items.Count;   // non-nullable shadow required by ICountedPagedList
        public long ExactTotalCount => _items.Count;
        public int Page => 1;
        public int PageSize => 10;
        public long? TotalPages => 1;
        public bool HasPreviousPage => false;
        public bool HasNextPage => false;
        public int Count => _items.Count;

        public T this[int index] => _items[index];

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void Map_WithNullSource_ThrowsArgumentNullException()
    {
        IPagedList<int> source = null!;
        var act = () => source.Map(x => x * 2);
        act.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Fact]
    public void Map_WithNullSelector_ThrowsArgumentNullException()
    {
        var source = new DummyPagedList<int>(new[] { 1, 2, 3 });
        Func<int, string> selector = null!;
        var act = () => source.Map(selector);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Map_AppliesSelectorToItems()
    {
        var source = new DummyPagedList<int>(new[] { 1, 2, 3 });
        var mapped = source.Map(x => x * 2);
        
        mapped.Should().NotBeNull();
        mapped.Count.Should().Be(3);
        mapped[0].Should().Be(2);
        mapped[1].Should().Be(4);
        mapped[2].Should().Be(6);
        
        var list = new List<int>();
        foreach (var item in mapped)
        {
            list.Add(item);
        }
        
        list.Should().BeEquivalentTo(new[] { 2, 4, 6 });
        
        // Also test non-generic GetEnumerator
        var ngList = new List<int>();
        var ngEnumerator = ((IEnumerable)mapped).GetEnumerator();
        while (ngEnumerator.MoveNext())
        {
            ngList.Add((int)ngEnumerator.Current!);
        }
        ngList.Should().BeEquivalentTo(new[] { 2, 4, 6 });
    }
    
    [Fact]
    public void Map_DelegatesPropertiesToSource()
    {
        var source = new DummyPagedList<int>(new[] { 1, 2, 3 });
        var mapped = source.Map(x => x * 2);

        mapped.TotalCount.Should().Be(source.TotalCount);
        mapped.Page.Should().Be(source.Page);
        mapped.PageSize.Should().Be(source.PageSize);
        mapped.TotalPages.Should().Be(source.TotalPages);
        mapped.HasPreviousPage.Should().Be(source.HasPreviousPage);
        mapped.HasNextPage.Should().Be(source.HasNextPage);
    }

    [Fact]
    public void LazyMap_WithNullSource_ThrowsArgumentNullException()
    {
        IPagedList<int> source = null!;
        var act = () => source.LazyMap(x => x * 2);
        act.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Fact]
    public void LazyMap_WithNullSelector_ThrowsArgumentNullException()
    {
        var source = new DummyPagedList<int>(new[] { 1, 2, 3 });
        Func<int, string> selector = null!;
        var act = () => source.LazyMap(selector);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void LazyMap_AppliesSelectorToItems()
    {
        var source = new DummyPagedList<int>(new[] { 1, 2, 3 });
        var mapped = source.LazyMap(x => x * 2);
        
        mapped.Should().NotBeNull();
        mapped.Count.Should().Be(3);
        mapped[0].Should().Be(2);
        mapped[1].Should().Be(4);
        mapped[2].Should().Be(6);
        
        var list = new List<int>();
        foreach (var item in mapped)
        {
            list.Add(item);
        }
        
        list.Should().BeEquivalentTo(new[] { 2, 4, 6 });
        
        // Also test non-generic GetEnumerator
        var ngList = new List<int>();
        var ngEnumerator = ((IEnumerable)mapped).GetEnumerator();
        while (ngEnumerator.MoveNext())
        {
            ngList.Add((int)ngEnumerator.Current!);
        }
        ngList.Should().BeEquivalentTo(new[] { 2, 4, 6 });
    }
    
    [Fact]
    public void LazyMap_DelegatesPropertiesToSource()
    {
        var source = new DummyPagedList<int>(new[] { 1, 2, 3 });
        var mapped = source.LazyMap(x => x * 2);

        mapped.TotalCount.Should().Be(source.TotalCount);
        mapped.Page.Should().Be(source.Page);
        mapped.PageSize.Should().Be(source.PageSize);
        mapped.TotalPages.Should().Be(source.TotalPages);
        mapped.HasPreviousPage.Should().Be(source.HasPreviousPage);
        mapped.HasNextPage.Should().Be(source.HasNextPage);
    }

    [Fact]
    public void Map_IPagedList_WhenUnderlyingIsCountedPagedList_DelegatesToIt()
    {
        var parameters = PaginationParameters.Create(1, 10);
        IPagedList<int> source = PagedList<int>.WithCount(new[] { 1, 2 }, parameters, 100);
        var mapped = source.Map(x => x.ToString());
        mapped.Should().BeOfType<CountedPagedList<string>>();
    }

    [Fact]
    public void Map_IPagedList_WhenUnderlyingIsPagedList_DelegatesToIt()
    {
        var parameters = PaginationParameters.Create(1, 10);
        IPagedList<int> source = PagedList<int>.WithoutCount(new[] { 1, 2 }, parameters, true);
        var mapped = source.Map(x => x.ToString());
        mapped.Should().BeOfType<PagedList<string>>();
    }

    [Fact]
    public void Map_ICountedPagedList_WithNullSource_ThrowsArgumentNullException()
    {
        ICountedPagedList<int> source = null!;
        var act = () => source.Map(x => x * 2);
        act.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Fact]
    public void Map_ICountedPagedList_WithNullSelector_ThrowsArgumentNullException()
    {
        ICountedPagedList<int> source = new DummyCountedPagedList<int>(new[] { 1, 2, 3 });
        Func<int, string> selector = null!;
        var act = () => source.Map(selector);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Map_ICountedPagedList_WhenUnderlyingIsCountedPagedList_DelegatesToIt()
    {
        var parameters = PaginationParameters.Create(1, 10);
        ICountedPagedList<int> source = PagedList<int>.WithCount(new[] { 1, 2 }, parameters, 100);
        var mapped = source.Map(x => x.ToString());
        mapped.Should().BeOfType<CountedPagedList<string>>();
    }

    [Fact]
    public void Map_ICountedPagedList_AppliesSelectorToItems()
    {
        ICountedPagedList<int> source = new DummyCountedPagedList<int>(new[] { 1, 2, 3 });
        var mapped = source.Map(x => x * 2);
        
        mapped.Should().NotBeNull();
        mapped.Should().BeOfType<CountedPagedList<int>>();
        mapped.Count.Should().Be(3);
        mapped.ExactTotalCount.Should().Be(3);
        mapped.Page.Should().Be(1);
        mapped.PageSize.Should().Be(10);
        mapped.Should().BeEquivalentTo(new[] { 2, 4, 6 });
    }
}

