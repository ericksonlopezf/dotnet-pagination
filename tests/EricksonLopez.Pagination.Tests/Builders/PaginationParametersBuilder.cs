// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Tests.Builders;

/// <summary>
/// Fluent test data builder for <see cref="PaginationParameters"/>.
/// </summary>
public sealed class PaginationParametersBuilder
{
    private int _page = 1;
    private int _pageSize = 10;

    public PaginationParametersBuilder WithPage(int page)
    {
        _page = page;
        return this;
    }

    public PaginationParametersBuilder WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    public PaginationParameters Build() => PaginationParameters.Create(_page, _pageSize);

    public static implicit operator PaginationParameters(PaginationParametersBuilder builder) => builder.Build();
}

/// <summary>
/// Fluent test data builder for <see cref="CursorPaginationParameters"/>.
/// </summary>
public sealed class CursorPaginationParametersBuilder
{
    private int? _first;
    private int? _last;
    private string? _after;
    private string? _before;

    public CursorPaginationParametersBuilder WithFirst(int? first)
    {
        _first = first;
        return this;
    }

    public CursorPaginationParametersBuilder WithLast(int? last)
    {
        _last = last;
        return this;
    }

    public CursorPaginationParametersBuilder WithAfter(string? after)
    {
        _after = after;
        return this;
    }

    public CursorPaginationParametersBuilder WithBefore(string? before)
    {
        _before = before;
        return this;
    }

    public CursorPaginationParameters Build() => new()
    {
        First = _first,
        Last = _last,
        After = _after,
        Before = _before
    };

    public static implicit operator CursorPaginationParameters(CursorPaginationParametersBuilder builder) => builder.Build();
}
