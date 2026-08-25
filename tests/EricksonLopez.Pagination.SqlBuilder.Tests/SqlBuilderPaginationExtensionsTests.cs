// Copyright © Erickson Lopez. MIT License.
extern alias PagAbstractions;
extern alias PagCore;

using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination.SqlBuilder;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;
using PaginationParameters = PagAbstractions::EricksonLopez.Pagination.Abstractions.PaginationParameters;
using CursorPaginationParameters = PagAbstractions::EricksonLopez.Pagination.Abstractions.CursorPaginationParameters;
using ICursorEncoder = PagCore::EricksonLopez.Pagination.ICursorEncoder;
using HmacCursorEncoder = PagCore::EricksonLopez.Pagination.HmacCursorEncoder;

namespace EricksonLopez.Pagination.SqlBuilder.Tests;

internal sealed class TestEntity : ISqlEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public string GetTableName() => "test_entities";
    public string[] GetColumnNames() => new[] { "id", "name", "price" };
    public object?[] GetValues() => new object?[] { Id, Name, Price };
    public string[] GetAllColumnNames() => new[] { "id", "name", "price" };
    public object?[] GetAllValues() => new object?[] { Id, Name, Price };
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Id"] = "id",
        ["Name"] = "name",
        ["Price"] = "price"
    };
    public string[] GetIndexedColumns() => new[] { "id" };
}

public class SqlBuilderPaginationExtensionsTests
{
    private readonly PostgreSqlCompiler _compiler = new();

    #region Paginate Tests

    [Fact]
    public void Paginate_NullQuery_ThrowsArgumentNullException()
    {
        SelectQuery<TestEntity> query = null!;
        var act1 = () => query.Paginate(new PaginationParameters { Page = 1, PageSize = 10 });
        var act2 = () => query.Paginate(1, 10);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Paginate_WithParameters_AppliesLimitAndOffsetCorrectly()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new PaginationParameters { Page = 3, PageSize = 25 };

        var paginatedQuery = query.Paginate(parameters);
        var sqlResult = paginatedQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 25");
        sqlResult.Sql.Should().Contain("OFFSET 50");
    }

    [Fact]
    public void Paginate_WithExplicitNumbers_AppliesLimitAndOffsetCorrectly()
    {
        var query = Sql.From<TestEntity>();

        var paginatedQuery = query.Paginate(pageNumber: 2, pageSize: 10);
        var sqlResult = paginatedQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 10");
        sqlResult.Sql.Should().Contain("OFFSET 10");
    }

    [Fact]
    public void Paginate_WithInvalidArguments_ThrowsArgumentOutOfRangeException()
    {
        var query = Sql.From<TestEntity>();

        var act1 = () => query.Paginate(pageNumber: 0, pageSize: 10);
        var act2 = () => query.Paginate(pageNumber: 1, pageSize: 0);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region ApplyCursor Tests

    [Fact]
    public void ApplyCursor_NullQueryOrKeySelector_ThrowsArgumentNullException()
    {
        SelectQuery<TestEntity> query = null!;
        var validQuery = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { First = 10 };

        var act1 = () => query.ApplyCursor(parameters, x => x.Id);
        var act2 = () => validQuery.ApplyCursor<TestEntity, int>(parameters, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyCursor_WithoutCursor_DefaultsToTen_AppliesOrderingAscending()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters();

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 11"); // 10 + 1
        sqlResult.Sql.Should().Contain("ORDER BY");
        sqlResult.Sql.Should().NotContain("WHERE");
    }

    [Fact]
    public void ApplyCursor_WithoutCursor_AppliesOrderingDescending()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { Last = 15 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, ascending: false);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 16"); // 15 + 1
        sqlResult.Sql.Should().Contain("ORDER BY");
        sqlResult.Sql.Should().NotContain("WHERE");
    }

    [Fact]
    public void ApplyCursor_WithAfterToken_Ascending_AppliesGreaterThanPredicate()
    {
        var query = Sql.From<TestEntity>();
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        string cursorToken = encoder.Encode("42")!;
        var parameters = new CursorPaginationParameters { After = cursorToken, First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 11");
        sqlResult.Sql.Should().Contain("WHERE (id > @p0)");
        sqlResult.Sql.Should().Contain("ORDER BY \"id\"");
        sqlResult.Sql.Should().NotContain("DESC");
    }

    [Fact]
    public void ApplyCursor_WithAfterToken_Descending_AppliesLessThanPredicate()
    {
        var query = Sql.From<TestEntity>();
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        string cursorToken = encoder.Encode("42")!;
        var parameters = new CursorPaginationParameters { After = cursorToken, First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder, ascending: false);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 11");
        sqlResult.Sql.Should().Contain("WHERE (id < @p0)");
        sqlResult.Sql.Should().Contain("ORDER BY \"id\" DESC");
    }

    [Fact]
    public void ApplyCursor_WithBeforeToken_Ascending_AppliesLessThanPredicate()
    {
        var query = Sql.From<TestEntity>();
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        string cursorToken = encoder.Encode("99")!;
        var parameters = new CursorPaginationParameters { Before = cursorToken, Last = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 11");
        sqlResult.Sql.Should().Contain("WHERE (id < @p0)");
        sqlResult.Sql.Should().Contain("ORDER BY \"id\"");
        sqlResult.Sql.Should().NotContain("DESC");
    }

    [Fact]
    public void ApplyCursor_WithBeforeToken_Descending_AppliesGreaterThanPredicate()
    {
        var query = Sql.From<TestEntity>();
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        string cursorToken = encoder.Encode("99")!;
        var parameters = new CursorPaginationParameters { Before = cursorToken, Last = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder, ascending: false);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 11");
        sqlResult.Sql.Should().Contain("WHERE (id > @p0)");
        sqlResult.Sql.Should().Contain("ORDER BY \"id\" DESC");
    }

    [Fact]
    public void ApplyCursor_WithCustomEncoder_DecodesUsingCustomEncoder()
    {
        var query = Sql.From<TestEntity>();
        var fakeEncoder = new CustomFakeEncoder();
        var parameters = new CursorPaginationParameters { After = "CUSTOM_TOKEN", First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, fakeEncoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("WHERE (id > @p0)");
    }

    [Fact]
    public void ApplyCursor_WithFirstAndLast_PrioritizesFirst()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { First = 5, Last = 20 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 6");
    }

    private class CustomFakeEncoder : ICursorEncoder
    {
        public string? Encode(string? plainText) => plainText == null ? null : $"ENC_{plainText}";
        public string? Decode(string? encodedCursor) => "77";
    }

    #endregion

    #region ToCursorPagedList Tests

    [Fact]
    public void ToCursorPagedList_NullArguments_ThrowsArgumentNullException()
    {
        IReadOnlyList<TestEntity> items = null!;
        var validItems = new List<TestEntity> { new() { Id = 1 } };
        var parameters = new CursorPaginationParameters { First = 5 };

        var act1 = () => items.ToCursorPagedList(parameters, x => x.Id);
        var act2 = () => validItems.ToCursorPagedList<TestEntity, int>(parameters, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCursorPagedList_WhenItemsExceedPageSize_CalculatesHasNextPageTrue()
    {
        var items = Enumerable.Range(1, 6)
            .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
            .ToList();

        var parameters = new CursorPaginationParameters { First = 5 };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().NotBeNullOrEmpty();
        result.EndCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToCursorPagedList_WithFirstAndLast_PrioritizesFirst()
    {
        var items = Enumerable.Range(1, 6)
            .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
            .ToList();

        var parameters = new CursorPaginationParameters { First = 3, Last = 5 };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Count.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ToCursorPagedList_WithLastOnly_UsesLast()
    {
        var items = Enumerable.Range(1, 6)
            .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
            .ToList();

        var parameters = new CursorPaginationParameters { Last = 3 };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Count.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ToCursorPagedList_WhenItemsDoNotExceedPageSize_CalculatesHasNextPageFalse()
    {
        var items = Enumerable.Range(1, 5)
            .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
            .ToList();

        var parameters = new CursorPaginationParameters { First = 5, After = "some-token" };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ToCursorPagedList_EmptyList_ReturnsEmptyWithNullCursors()
    {
        var items = new List<TestEntity>();
        var parameters = new CursorPaginationParameters();

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Count.Should().Be(0);
        result.StartCursor.Should().BeNull();
        result.EndCursor.Should().BeNull();
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToCursorPagedList_CustomEncoder_EncodesCorrectly()
    {
        var items = new List<TestEntity> { new() { Id = 10, Name = "Item 10" } };
        var parameters = new CursorPaginationParameters { Last = 5 };
        var encoder = new CustomFakeEncoder();

        var result = items.ToCursorPagedList(parameters, x => x.Id, encoder);

        result.StartCursor.Should().Be("ENC_10");
        result.EndCursor.Should().Be("ENC_10");
    }

    [Fact]
    public void ToCursorPagedList_NullKeySelectorReturnValue_EncodesEmptyString()
    {
        var items = new List<TestEntity> { new() { Id = 1, Name = null! } };
        var parameters = new CursorPaginationParameters();
        var result = items.ToCursorPagedList(parameters, x => x.Name);
        result.StartCursor.Should().NotBeNull();
        result.EndCursor.Should().NotBeNull();

        HmacCursorEncoder.DevelopmentDefault.Decode(result.StartCursor!).Should().Be(string.Empty);
        HmacCursorEncoder.DevelopmentDefault.Decode(result.EndCursor!).Should().Be(string.Empty);
    }

    #endregion

    #region ToPagedList Tests

    [Fact]
    public void ToPagedList_WithParameters_NullItems_ThrowsArgumentNullException()
    {
        IEnumerable<TestEntity> items = null!;
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };

        var act = () => items.ToPagedList(totalCount: 50, parameters);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToPagedList_WithParameters_EnumerableNotList_CreatesValidPagedList()
    {
        static IEnumerable<TestEntity> Generate()
        {
            yield return new TestEntity { Id = 1, Name = "Item 1" };
            yield return new TestEntity { Id = 2, Name = "Item 2" };
        }

        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var pagedList = Generate().ToPagedList(totalCount: 50, parameters);

        pagedList.TotalCount.Should().Be(50);
        pagedList.Page.Should().Be(1);
        pagedList.PageSize.Should().Be(10);
        pagedList.Count.Should().Be(2);
    }

    [Fact]
    public void ToPagedList_WithParameters_ValidList_CreatesValidPagedList()
    {
        var items = new List<TestEntity>
        {
            new() { Id = 1, Name = "Item 1" },
            new() { Id = 2, Name = "Item 2" }
        };
        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };

        var pagedList = items.ToPagedList(totalCount: 50, parameters);

        pagedList.TotalCount.Should().Be(50);
        pagedList.Page.Should().Be(1);
        pagedList.PageSize.Should().Be(10);
        pagedList.TotalPages.Should().Be(5);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToPagedList_WithExplicitNumbers_NullItems_ThrowsArgumentNullException()
    {
        IEnumerable<TestEntity> items = null!;

        var act = () => items.ToPagedList(totalCount: 50, pageNumber: 1, pageSize: 10);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToPagedList_WithExplicitNumbers_InvalidArguments_ThrowsArgumentOutOfRangeException()
    {
        var items = new List<TestEntity>();

        var act1 = () => items.ToPagedList(totalCount: 50, pageNumber: 0, pageSize: 10);
        var act2 = () => items.ToPagedList(totalCount: 50, pageNumber: 1, pageSize: 0);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToPagedList_WithExplicitNumbers_EnumerableNotList_CreatesValidPagedList()
    {
        static IEnumerable<TestEntity> Generate()
        {
            yield return new TestEntity { Id = 1, Name = "Item 1" };
            yield return new TestEntity { Id = 2, Name = "Item 2" };
        }

        var pagedList = Generate().ToPagedList(totalCount: 50, pageNumber: 2, pageSize: 2);

        pagedList.TotalCount.Should().Be(50);
        pagedList.Page.Should().Be(2);
        pagedList.PageSize.Should().Be(2);
        pagedList.Count.Should().Be(2);
    }

    [Fact]
    public void ToPagedList_WithExplicitNumbers_ValidList_CreatesValidPagedList()
    {
        var items = new List<TestEntity>
        {
            new() { Id = 1, Name = "Item 1" },
            new() { Id = 2, Name = "Item 2" }
        };

        var pagedList = items.ToPagedList(totalCount: 50, pageNumber: 1, pageSize: 10);

        pagedList.TotalCount.Should().Be(50);
        pagedList.Page.Should().Be(1);
        pagedList.PageSize.Should().Be(10);
        pagedList.TotalPages.Should().Be(5);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();
    }

    #endregion

    #region ToCursorPagedList Tests

    [Fact]
    public void ToCursorPagedList_NullItems_ThrowsArgumentNullException()
    {
        IReadOnlyList<TestEntity> items = null!;
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => items.ToCursorPagedList(parameters, x => x.Id);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCursorPagedList_NullKeySelector_ThrowsArgumentNullException()
    {
        var items = new List<TestEntity> { new() { Id = 1, Name = "Item 1" } };
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => items.ToCursorPagedList<TestEntity, int>(parameters, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCursorPagedList_ForwardPagination_ComputesCursorsAndHasNextPage()
    {
        // 11 items fetched when limit was pageSize + 1 = 11
        var items = Enumerable.Range(1, 11).Select(i => new TestEntity { Id = i, Name = $"Item {i}" }).ToList();
        var parameters = new CursorPaginationParameters { First = 10 };

        var pagedList = items.ToCursorPagedList(parameters, x => x.Id);

        pagedList.Count.Should().Be(10);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();
        pagedList.StartCursor.Should().NotBeNull();
        pagedList.EndCursor.Should().NotBeNull();
        pagedList[0].Id.Should().Be(1);
        pagedList[^1].Id.Should().Be(10);
    }

    [Fact]
    public void ToCursorPagedList_WithAfterParameter_SetsHasPreviousPageTrue()
    {
        var items = Enumerable.Range(11, 5).Select(i => new TestEntity { Id = i, Name = $"Item {i}" }).ToList();
        var parameters = new CursorPaginationParameters { First = 10, After = "some_cursor" };

        var pagedList = items.ToCursorPagedList(parameters, x => x.Id);

        pagedList.Count.Should().Be(5);
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeTrue();
        pagedList[0].Id.Should().Be(11);
        pagedList[^1].Id.Should().Be(15);
    }

    [Fact]
    public void ToCursorPagedList_WhenEmptyItems_ReturnsEmptyListAndNullCursors()
    {
        var items = new List<TestEntity>();
        var parameters = new CursorPaginationParameters { First = 10 };

        var pagedList = items.ToCursorPagedList(parameters, x => x.Id);

        pagedList.Count.Should().Be(0);
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeFalse();
        pagedList.StartCursor.Should().BeNull();
        pagedList.EndCursor.Should().BeNull();
    }

    #endregion
}





