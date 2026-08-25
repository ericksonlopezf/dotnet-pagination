// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.LinqToDB;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.LinqToDB.Tests;

public sealed partial class CursorPaginationLinqToDBExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CursorPaginationLinqToDBExtensionsTests()
    {
        _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private TestDataConnection GetDatabase()
    {
        var options = new DataOptions<TestDataConnection>(new DataOptions().UseSQLite(_connection.ConnectionString));
        var context = new TestDataConnection(options);

        try
        {
            context.CreateTable<TestEntity>();
            var entities = Enumerable.Range(1, 30).Select(i => new TestEntity
            {
                Id = i,
                Name = $"Item {i:D3}",
                AdditionalValue = i % 5,
                GuidValue = Guid.NewGuid(),
                StateValue = (TestState)(i % 2 + 1)
            });
            context.BulkCopy(entities);
        }
        catch
        {
            // Ignore if already created due to shared cache
        }

        return context;
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_Ascending_ForwardAndBackward()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        // Page 1
        var p1 = await query.Keyset(new CursorPaginationParameters { First = 10 })
                            .Ascending(e => e.Id)
                            .ToCursorPagedListAsync();

        p1.Count.Should().Be(10);
        p1[0].Id.Should().Be(1);
        p1[^1].Id.Should().Be(10);
        p1.HasNextPage.Should().BeTrue();
        p1.HasPreviousPage.Should().BeFalse();

        // Page 2
        var p2 = await query.Keyset(new CursorPaginationParameters { First = 10, After = p1.EndCursor })
                            .Ascending(e => e.Id)
                            .ToCursorPagedListAsync();

        p2.Count.Should().Be(10);
        p2[0].Id.Should().Be(11);
        p2[^1].Id.Should().Be(20);
        p2.HasNextPage.Should().BeTrue();
        p2.HasPreviousPage.Should().BeTrue();

        // Backward: from Page 2 to Page 1
        var p1Back = await query.Keyset(new CursorPaginationParameters { Last = 10, Before = p2.StartCursor })
                                .Ascending(e => e.Id)
                                .ToCursorPagedListAsync();

        p1Back.Count.Should().Be(10);
        p1Back[0].Id.Should().Be(1);
        p1Back[^1].Id.Should().Be(10);
        p1Back.HasNextPage.Should().BeTrue();
        p1Back.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_Descending_ForwardAndBackward()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        // Page 1 Descending
        var p1 = await query.Keyset(new CursorPaginationParameters { First = 10 })
                            .Descending(e => e.Id)
                            .ToCursorPagedListAsync();

        p1.Count.Should().Be(10);
        p1[0].Id.Should().Be(30);
        p1[^1].Id.Should().Be(21);
        p1.HasNextPage.Should().BeTrue();
        p1.HasPreviousPage.Should().BeFalse();

        // Page 2 Descending
        var p2 = await query.Keyset(new CursorPaginationParameters { First = 10, After = p1.EndCursor })
                            .Descending(e => e.Id)
                            .ToCursorPagedListAsync();

        p2.Count.Should().Be(10);
        p2[0].Id.Should().Be(20);
        p2[^1].Id.Should().Be(11);
        p2.HasNextPage.Should().BeTrue();
        p2.HasPreviousPage.Should().BeTrue();

        // Backward Descending: from Page 2 to Page 1
        var p1Back = await query.Keyset(new CursorPaginationParameters { Last = 10, Before = p2.StartCursor })
                                .Descending(e => e.Id)
                                .ToCursorPagedListAsync();

        p1Back.Count.Should().Be(10);
        p1Back[0].Id.Should().Be(30);
        p1Back[^1].Id.Should().Be(21);
        p1Back.HasNextPage.Should().BeTrue();
        p1Back.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Composite2Keys_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var p1 = await query.Keyset(new CursorPaginationParameters { First = 10 })
                            .Ascending(e => e.AdditionalValue)
                            .Ascending(e => e.Id)
                            .ToCursorPagedListAsync();

        p1.Count.Should().Be(10);
        p1.HasNextPage.Should().BeTrue();

        var p2 = await query.Keyset(new CursorPaginationParameters { First = 10, After = p1.EndCursor })
                            .Ascending(e => e.AdditionalValue)
                            .Ascending(e => e.Id)
                            .ToCursorPagedListAsync();

        p2.Count.Should().Be(10);
        p2.HasNextPage.Should().BeTrue();

        var p1Back = await query.Keyset(new CursorPaginationParameters { Last = 10, Before = p2.StartCursor })
                                .Ascending(e => e.AdditionalValue)
                                .Ascending(e => e.Id)
                                .ToCursorPagedListAsync();

        p1Back.Count.Should().Be(10);
        p1Back[0].Id.Should().Be(p1[0].Id);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Composite3Keys_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var p1 = await query.Keyset(new CursorPaginationParameters { First = 10 })
                            .Ascending(e => e.AdditionalValue)
                            .Descending(e => e.Name)
                            .Ascending(e => e.Id)
                            .ToCursorPagedListAsync();

        p1.Count.Should().Be(10);
        p1.HasNextPage.Should().BeTrue();

        var p2 = await query.Keyset(new CursorPaginationParameters { First = 10, After = p1.EndCursor })
                            .Ascending(e => e.AdditionalValue)
                            .Descending(e => e.Name)
                            .Ascending(e => e.Id)
                            .ToCursorPagedListAsync();

        p2.Count.Should().Be(10);

        var p1Back = await query.Keyset(new CursorPaginationParameters { Last = 10, Before = p2.StartCursor })
                                .Ascending(e => e.AdditionalValue)
                                .Descending(e => e.Name)
                                .Ascending(e => e.Id)
                                .ToCursorPagedListAsync();

        p1Back.Count.Should().Be(10);
        p1Back[0].Id.Should().Be(p1[0].Id);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_DynamicStringPropertyName_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var p1 = await query.Keyset(new CursorPaginationParameters { First = 5 })
                            .Ascending("Id")
                            .ToCursorPagedListAsync();

        p1.Count.Should().Be(5);
        p1[0].Id.Should().Be(1);

        var p2 = await query.Keyset(new CursorPaginationParameters { First = 5 })
                            .Descending("Id")
                            .ToCursorPagedListAsync();

        p2.Count.Should().Be(5);
        p2[0].Id.Should().Be(30);

        Action invalidName = () => query.Keyset(new CursorPaginationParameters()).Ascending("NonExistentProperty");
        invalidName.Should().Throw<ArgumentException>();

        Action invalidNameDesc = () => query.Keyset(new CursorPaginationParameters()).Descending("NonExistentProperty");
        invalidNameDesc.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithDefaultPageSize_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var p = await query.Keyset(new CursorPaginationParameters(), defaultPageSize: 7)
                           .Ascending(e => e.Id)
                           .ToCursorPagedListAsync();

        p.Count.Should().Be(7);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithCustomFactory_UsesFactory()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        var factory = new FakeCursorFactory();

        var p = await query.Keyset(new CursorPaginationParameters { First = 5 })
                           .Ascending(e => e.Id)
                           .ToCursorPagedListAsync(factory: factory);

        factory.WasCalled.Should().BeTrue();
        p.Count.Should().Be(5);
    }

    private sealed class FakeCursorFactory : ICursorPagedListFactory
    {
        public bool WasCalled { get; private set; }

        public ICursorPagedList<T> CreateCursorPagedList<T>(
            IReadOnlyList<T> items,
            long? totalCount,
            string? startCursor,
            string? endCursor,
            bool hasPreviousPage,
            bool hasNextPage)
        {
            WasCalled = true;
            return DefaultPagedListFactory.Instance.CreateCursorPagedList(items, totalCount, startCursor, endCursor, hasPreviousPage, hasNextPage);
        }
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_NavigatesProperly()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page1 = await query.Keyset(new CursorPaginationParameters { First = 10 })
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync(e => e.Name);

        page1.Count.Should().Be(10);
        page1[0].Should().Be("Item 001");
        page1.HasNextPage.Should().BeTrue();

        var page2 = await query.Keyset(new CursorPaginationParameters { First = 10, After = page1.EndCursor })
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync(e => e.Name);

        page2.Count.Should().Be(10);
        page2[0].Should().Be("Item 011");
        page2.HasNextPage.Should().BeTrue();

        // Backward with projection
        var page1Back = await query.Keyset(new CursorPaginationParameters { Last = 10, Before = page2.StartCursor })
                                   .Ascending(e => e.Id)
                                   .ToCursorPagedListAsync(e => e.Name);

        page1Back.Count.Should().Be(10);
        page1Back[0].Should().Be("Item 001");

        // Custom factory with projection
        var factory = new FakeCursorFactory();
        var pageProjFactory = await query.Keyset(new CursorPaginationParameters { First = 5 })
                                         .Ascending(e => e.Id)
                                         .ToCursorPagedListAsync(e => e.Name, factory: factory);

        factory.WasCalled.Should().BeTrue();
        pageProjFactory.Count.Should().Be(5);
    }

    [Fact]
    public async Task QueryableCursorProjectionExtensions_LegacyToCursorPagedListAsync_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        // Forward Ascending
        var p1 = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 10 },
            direction: SortDirection.Ascending);

        p1.Count.Should().Be(10);
        p1[0].Id.Should().Be(1);
        p1.HasNextPage.Should().BeTrue();
        p1.HasPreviousPage.Should().BeFalse();

        // Forward Descending
        var pDesc = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 10 },
            direction: SortDirection.Descending);

        pDesc.Count.Should().Be(10);
        pDesc[0].Id.Should().Be(30);

        // Backward Ascending
        var p1Back = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { Last = 10, Before = HmacCursorEncoder.DevelopmentDefault.Encode("20") },
            direction: SortDirection.Ascending);

        p1Back.Count.Should().Be(10);
        p1Back[0].Id.Should().Be(10);
        p1Back[^1].Id.Should().Be(19);

        // Backward Descending
        var pDescBack = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { Last = 10, Before = HmacCursorEncoder.DevelopmentDefault.Encode("10") },
            direction: SortDirection.Descending);

        pDescBack.Count.Should().Be(10);
        pDescBack[0].Id.Should().Be(20);
        pDescBack[^1].Id.Should().Be(11);

        // String Key (reference type IComparable)
        var pString = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Name,
            resultKeySelector: r => r.Name,
            parameters: new CursorPaginationParameters { First = 10 },
            direction: SortDirection.Ascending);

        pString.Count.Should().Be(10);

        // String Key Forward with After (covers IComparable<TKey> reference type in predicate)
        var pStringAfter = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Name,
            resultKeySelector: r => r.Name,
            parameters: new CursorPaginationParameters { First = 5, After = pString.EndCursor },
            direction: SortDirection.Ascending);

        pStringAfter.Count.Should().Be(5);
        pStringAfter[0].Name.Should().Be("Item 011");

        // String Key Backward with Before
        var pStringBefore = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Name,
            resultKeySelector: r => r.Name,
            parameters: new CursorPaginationParameters { Last = 3, Before = pStringAfter.StartCursor },
            direction: SortDirection.Ascending);

        pStringBefore.Count.Should().Be(3);

        // Forward Ascending with After
        var pAscAfter = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 5, After = HmacCursorEncoder.DevelopmentDefault.Encode("5") },
            direction: SortDirection.Ascending);

        pAscAfter.Count.Should().Be(5);
        pAscAfter[0].Id.Should().Be(6);

        // Forward Descending with After
        var pDescAfter = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 5, After = HmacCursorEncoder.DevelopmentDefault.Encode("25") },
            direction: SortDirection.Descending);

        pDescAfter.Count.Should().Be(5);
        pDescAfter[0].Id.Should().Be(24);
    }

    private sealed class NonComparableKeyType
    {
        public override string ToString() => "foo";
    }

    [Fact]
    public async Task QueryableCursorProjectionExtensions_NonComparableKey_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var decoderRegistry = Substitute.For<ICursorDecoderRegistry>();
        decoderRegistry.TryGetDecoder<NonComparableKeyType>(out Arg.Any<Func<string, NonComparableKeyType>>()!)
            .Returns(x => { x[0] = (Func<string, NonComparableKeyType>)(_ => new NonComparableKeyType()); return true; });

        var options = Substitute.For<IPaginationOptions>();
        options.CursorDecoderRegistry.Returns(decoderRegistry);

        Func<Task> act = async () =>
        {
            await query.ToCursorPagedListAsync(
                selector: e => new { e.Id, Key = new NonComparableKeyType() },
                keySelector: e => new NonComparableKeyType(),
                resultKeySelector: r => r.Key,
                parameters: new CursorPaginationParameters { First = 5, After = HmacCursorEncoder.DevelopmentDefault.Encode("foo") },
                options: options);
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot build a keyset cursor comparison for type*");
    }

    [Fact]
    public void Ascending_NullableProperty_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        Action act = () => query.Keyset(new CursorPaginationParameters()).Ascending(e => e.NullableId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Keyset pagination on nullable property*");
    }

    [Fact]
    public void Descending_NullableProperty_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        Action act = () => query.Keyset(new CursorPaginationParameters()).Descending(e => e.NullableId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Keyset pagination on nullable property*");
    }

    [Fact]
    public void Ascending_DynamicNullableProperty_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        Action act = () => query.Keyset(new CursorPaginationParameters()).Ascending("NullableId");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Descending_DynamicNullableProperty_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        Action act = () => query.Keyset(new CursorPaginationParameters()).Descending("NullableId");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Keyset_SingleColumnCursor_ThrowsInvalidPaginationCursorException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        var invalidCursor = Base64CursorEncoder.Default.Encode("S|10");

        var act = () => query.Keyset(new CursorPaginationParameters { First = 10, After = invalidCursor }, cursorEncoder: Base64CursorEncoder.Default)
                             .Ascending(e => e.Id)
                             .ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*Expected a multi-column keyset cursor, but received a single-column cursor.*");
    }

    [Fact]
    public async Task Keyset_LegacyV1Cursor_RejectsWhenAcceptLegacyCursorsFalse()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        var v1Cursor = Base64CursorEncoder.Default.Encode("M|10");

        var act = () => query.Keyset(new CursorPaginationParameters { First = 10, After = v1Cursor }, cursorEncoder: Base64CursorEncoder.Default, acceptLegacyCursors: false)
                             .Ascending(e => e.Id)
                             .ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*Legacy v1 cursor format is not accepted.*");
    }

    [Fact]
    public async Task Keyset_LegacyV1Cursor_AcceptsWhenAcceptLegacyCursorsTrue()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        var v1Cursor = Base64CursorEncoder.Default.Encode("M|10");

        var page = await query.Keyset(new CursorPaginationParameters { First = 10, After = v1Cursor }, cursorEncoder: Base64CursorEncoder.Default, acceptLegacyCursors: true)
                              .Ascending(e => e.Id)
                              .ToCursorPagedListAsync();

        page.Count.Should().Be(10);
        page[0].Id.Should().Be(11);
    }

    [Fact]
    public async Task Keyset_MismatchedFingerprint_ThrowsInvalidPaginationCursorException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        // Generate cursor with wrong fingerprint
        var forgedCursor = Base64CursorEncoder.Default.Encode("M|v2|DEADBEEF|10");

        var act = () => query.Keyset(new CursorPaginationParameters { First = 10, After = forgedCursor }, cursorEncoder: Base64CursorEncoder.Default)
                             .Ascending(e => e.Id)
                             .ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*Cursor was generated for a different keyset and cannot be used here.*");
    }

    [Fact]
    public async Task Keyset_MismatchedPartsCount_ThrowsInvalidPaginationCursorException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();
        var v1Cursor = Base64CursorEncoder.Default.Encode("M|10|ExtraPart");

        var act = () => query.Keyset(new CursorPaginationParameters { First = 10, After = v1Cursor }, cursorEncoder: Base64CursorEncoder.Default)
                             .Ascending(e => e.Id)
                             .ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("*parts but keyset expects*");
    }

    [Fact]
    public async Task Keyset_ToCursorPagedListAsync_NoColumns_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var act = () => query.Keyset(new CursorPaginationParameters { First = 10 })
                             .ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*At least one column must be specified*");
    }

    [Fact]
    public async Task Keyset_ToCursorPagedListAsync_Projection_NullSelector_ThrowsArgumentNullException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var act = () => query.Keyset(new CursorPaginationParameters { First = 10 })
                             .Ascending(e => e.Id)
                             .ToCursorPagedListAsync<string>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Keyset_SortBy_DynamicSort_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page = await query.Keyset(new CursorPaginationParameters { First = 5 })
                              .SortBy(SortParameters.From("Name desc, Id asc"))
                              .ToCursorPagedListAsync();

        page.Count.Should().Be(5);
        page[0].Name.Should().Be("Item 030");
    }

    [Fact]
    public void Keyset_SortBy_InvalidProperty_ThrowsArgumentException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        Action act = () => query.Keyset(new CursorPaginationParameters { First = 5 })
                                .SortBy(SortParameters.From("NonExistentProperty asc"));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*not found on type*");
    }

    [Fact]
    public async Task Keyset_SortBy_NullOrEmpty_ReturnsSameQuery()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page = await query.Keyset(new CursorPaginationParameters { First = 5 })
                              .Ascending(e => e.Id)
                              .SortBy(SortParameters.From(null))
                              .ToCursorPagedListAsync();

        page.Count.Should().Be(5);
        page[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task Keyset_Projection_Backward_WithBeforeCursor_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page1 = await query.Keyset(new CursorPaginationParameters { First = 5 })
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync(e => e.Name);

        page1.EndCursor.Should().NotBeNull();

        var pageBackward = await query.Keyset(new CursorPaginationParameters { Last = 2, Before = page1.EndCursor })
                                      .Ascending(e => e.Id)
                                      .ToCursorPagedListAsync(e => e.Name);

        pageBackward.Count.Should().Be(2);
        pageBackward[0].Should().Be("Item 003");
        pageBackward[1].Should().Be("Item 004");
    }

    [Fact]
    public async Task Keyset_Projection_Forward_WithAfterCursor_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page1 = await query.Keyset(new CursorPaginationParameters { First = 5 })
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync(e => e.Name);

        var page2 = await query.Keyset(new CursorPaginationParameters { First = 5, After = page1.EndCursor })
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync(e => e.Name);

        page2.Count.Should().Be(5);
        page2[0].Should().Be("Item 006");
    }

    [Fact]
    public async Task Keyset_Projection_MoreThan16Columns_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var builder = query.Keyset(new CursorPaginationParameters { First = 5 });
        for (int i = 0; i < 17; i++)
        {
            builder = builder.Ascending(e => e.Id);
        }

        var act = () => builder.ToCursorPagedListAsync(e => e.Name);
        await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage("*supports a maximum of 16 keyset columns*");
    }

    [Fact]
    public async Task Keyset_Projection_Exactly16Columns_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var builder = query.Keyset(new CursorPaginationParameters { First = 3 });
        for (int i = 0; i < 16; i++)
        {
            builder = builder.Ascending(e => e.Id);
        }

        var page = await builder.ToCursorPagedListAsync(e => e.Name);
        page.Count.Should().Be(3);
        page[0].Should().Be("Item 001");
    }

    [Fact]
    public async Task Keyset_Projection_StringColumn_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page = await query.Keyset(new CursorPaginationParameters { First = 5 })
                              .Ascending(e => e.Name)
                              .ToCursorPagedListAsync(e => e.Id);

        page.Count.Should().Be(5);
        page[0].Should().Be(1);
    }

    [Fact]
    public async Task Keyset_ToPagedAsyncEnumerable_Forward_StreamsItems()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var results = new List<TestEntity>();
        await foreach (var item in query.Keyset(new CursorPaginationParameters { First = 5 })
                                        .Ascending(e => e.Id)
                                        .ToPagedAsyncEnumerable())
        {
            results.Add(item);
        }

        results.Count.Should().Be(5);
        results[0].Id.Should().Be(1);
        results[^1].Id.Should().Be(5);
    }

    [Fact]
    public async Task Keyset_ToPagedAsyncEnumerable_ForwardWithAfter_StreamsItems()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page1 = await query.Keyset(new CursorPaginationParameters { First = 5 })
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync();

        var results = new List<TestEntity>();
        await foreach (var item in query.Keyset(new CursorPaginationParameters { First = 5, After = page1.EndCursor })
                                        .Ascending(e => e.Id)
                                        .ToPagedAsyncEnumerable())
        {
            results.Add(item);
        }

        results.Count.Should().Be(5);
        results[0].Id.Should().Be(6);
    }

    [Fact]
    public void Keyset_ToPagedAsyncEnumerable_Backward_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        Action act = () => query.Keyset(new CursorPaginationParameters { Last = 5 })
                                .Ascending(e => e.Id)
                                .ToPagedAsyncEnumerable();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Streaming (ToPagedAsyncEnumerable) is not supported when paginating backwards*");
    }

    [Fact]
    public void Keyset_ToPagedAsyncEnumerable_NoColumns_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        Action act = () => query.Keyset(new CursorPaginationParameters { First = 5 })
                                .ToPagedAsyncEnumerable();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*At least one column must be specified*");
    }

    [Fact]
    public async Task Keyset_ToStreamingAsyncEnumerable_StreamsEntireDatasetInChunks()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var results = new List<TestEntity>();
        await foreach (var item in query.Keyset(new CursorPaginationParameters { First = 10 })
                                        .Ascending(e => e.Id)
                                        .ToStreamingAsyncEnumerable(chunkSize: 7))
        {
            results.Add(item);
        }

        results.Count.Should().Be(30);
        results[0].Id.Should().Be(1);
        results[^1].Id.Should().Be(30);
    }

    [Fact]
    public async Task Keyset_ToStreamingAsyncEnumerable_WithInitialAfter_StreamsRemainder()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page1 = await query.Keyset(new CursorPaginationParameters { First = 10 })
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync();

        var results = new List<TestEntity>();
        await foreach (var item in query.Keyset(new CursorPaginationParameters { First = 10, After = page1.EndCursor })
                                        .Ascending(e => e.Id)
                                        .ToStreamingAsyncEnumerable(chunkSize: 5))
        {
            results.Add(item);
        }

        results.Count.Should().Be(20);
        results[0].Id.Should().Be(11);
        results[^1].Id.Should().Be(30);
    }

    [Fact]
    public async Task Keyset_ToStreamingAsyncEnumerable_Backward_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var enumerator = query.Keyset(new CursorPaginationParameters { Last = 5 })
                              .Ascending(e => e.Id)
                              .ToStreamingAsyncEnumerable()
                              .GetAsyncEnumerator();

        Func<Task> act = async () =>
        {
            await enumerator.MoveNextAsync();
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage("*Streaming is not supported when paginating backwards*");
    }

    [Fact]
    public async Task Keyset_ToStreamingAsyncEnumerable_NoColumns_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var enumerator = query.Keyset(new CursorPaginationParameters { First = 5 })
                              .ToStreamingAsyncEnumerable()
                              .GetAsyncEnumerator();

        Func<Task> act = async () =>
        {
            await enumerator.MoveNextAsync();
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage("*At least one column must be specified*");
    }

    [Fact]
    public async Task Keyset_EnumAndGuidColumns_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page = await query.Keyset(new CursorPaginationParameters { First = 5 })
                              .Ascending(e => e.StateValue)
                              .Ascending(e => e.GuidValue)
                              .Ascending(e => e.Id)
                              .ToCursorPagedListAsync();

        page.Count.Should().Be(5);
    }

    [Fact]
    public async Task Keyset_DateTimeColumn_Works()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page = await query.Keyset(new CursorPaginationParameters { First = 5 })
                              .Ascending(e => e.CreatedAt)
                              .Ascending(e => e.Id)
                              .ToCursorPagedListAsync();

        page.Count.Should().Be(5);
    }

    [Fact]
    public async Task Keyset_ToStreamingAsyncEnumerable_EmptyDataset_YieldsNothing()
    {
        using var context = GetDatabase();
        var query = context.Entities.Where(e => e.Id > 1000).AsQueryable();

        var results = new List<TestEntity>();
        await foreach (var item in query.Keyset(new CursorPaginationParameters { First = 5 })
                                        .Ascending(e => e.Id)
                                        .ToStreamingAsyncEnumerable(chunkSize: 10))
        {
            results.Add(item);
        }

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_MaxPageSize_ClampsEffectivePageSize()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var pageClamped = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 10 },
            maxPageSize: 3);

        pageClamped.Count.Should().Be(3);

        var pageUnclamped = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 10 },
            maxPageSize: null);

        pageUnclamped.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_FirstAndLastBothPresent_ForwardPrecedence()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 5, Last = 5 });

        page.Count.Should().Be(5);
        page[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_PageSizeExactMatch_HasNextPageIsFalse()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 30 });

        page.Count.Should().Be(30);
        page.HasNextPage.Should().BeFalse();
        page.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_EmptyResult_StartAndEndCursorNull()
    {
        using var context = GetDatabase();
        var query = context.Entities.Where(e => e.Id > 9999).AsQueryable();

        var page = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { First = 5 });

        page.Count.Should().Be(0);
        page.StartCursor.Should().BeNull();
        page.EndCursor.Should().BeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_Backward_NavigationFlags()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        // Backward without Before
        var pageBackWithoutBefore = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { Last = 5 });

        pageBackWithoutBefore.HasPreviousPage.Should().BeTrue();
        pageBackWithoutBefore.HasNextPage.Should().BeFalse();

        // Backward with Before
        var pageBackWithBefore = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Id,
            resultKeySelector: r => r.Id,
            parameters: new CursorPaginationParameters { Last = 5, Before = HmacCursorEncoder.DevelopmentDefault.Encode("20") });

        pageBackWithBefore.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_StringKey_Descending_Backward()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var pDescAfter = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Name,
            resultKeySelector: r => r.Name,
            parameters: new CursorPaginationParameters { First = 5, After = HmacCursorEncoder.DevelopmentDefault.Encode("Item 025") },
            direction: SortDirection.Descending);

        var pDescBack = await query.ToCursorPagedListAsync(
            selector: e => new { e.Id, e.Name },
            keySelector: e => e.Name,
            resultKeySelector: r => r.Name,
            parameters: new CursorPaginationParameters { Last = 3, Before = pDescAfter.StartCursor },
            direction: SortDirection.Descending);

        pDescBack.Count.Should().Be(3);
    }

    [Fact]
    public void Keyset_DynamicAscending_InvalidColumnName_ThrowsArgumentException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        Action actAllowed = () => query.Keyset(new CursorPaginationParameters()).Ascending("Invalid;Name", allowedProperties: new[] { "Name" });
        actAllowed.Should().Throw<InvalidOperationException>();

        Action actNotFound = () => query.Keyset(new CursorPaginationParameters()).Ascending("NonExistentProperty");
        actNotFound.Should().Throw<ArgumentException>()
            .WithMessage("Property 'NonExistentProperty' not found on type 'TestEntity'.");
    }

    [Fact]
    public void Keyset_DynamicDescending_InvalidColumnName_ThrowsArgumentException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        Action actAllowed = () => query.Keyset(new CursorPaginationParameters()).Descending("Invalid;Name", allowedProperties: new[] { "Name" });
        actAllowed.Should().Throw<InvalidOperationException>();

        Action actNotFound = () => query.Keyset(new CursorPaginationParameters()).Descending("NonExistentProperty");
        actNotFound.Should().Throw<ArgumentException>()
            .WithMessage("Property 'NonExistentProperty' not found on type 'TestEntity'.");
    }

    [Fact]
    public async Task Keyset_Projection_NoColumns_ThrowsInvalidOperationException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var act = () => query.Keyset(new CursorPaginationParameters { First = 5 })
                             .ToCursorPagedListAsync(e => e.Name);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("At least one column must be specified.");
    }

    [Fact]
    public async Task Keyset_CursorLength_4096Boundary()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var cursor4097 = new string('A', 4097);
        Func<Task> act4097 = () => query.Keyset(new CursorPaginationParameters { First = 5, After = cursor4097 })
                                        .Ascending(e => e.Id)
                                        .ToCursorPagedListAsync();

        await act4097.Should().ThrowAsync<InvalidPaginationCursorException>()
            .WithMessage("Cursor exceeds maximum allowed length of 4096 characters.");

        var cursor4096 = new string('A', 4096);
        Func<Task> act4096 = () => query.Keyset(new CursorPaginationParameters { First = 5, After = cursor4096 })
                                        .Ascending(e => e.Id)
                                        .ToCursorPagedListAsync();

        var ex = await act4096.Should().ThrowAsync<InvalidPaginationCursorException>();
        ex.Which.Message.Should().NotContain("exceeds maximum allowed length of 4096 characters");
    }

    [Fact]
    public async Task Keyset_ParseCursor_InvalidCoercion_ThrowsInvalidPaginationCursorException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var builder = query.Keyset(new CursorPaginationParameters()).Ascending(e => e.Id);
        var fingerprint = builder.GetKeysetSchemaFingerprint();
        var rawCursor = "M|v2|" + fingerprint + "|notAnInt";
        var encoded = HmacCursorEncoder.DevelopmentDefault.Encode(rawCursor);

        Func<Task> act = () => query.Keyset(new CursorPaginationParameters { First = 5, After = encoded })
                                    .Ascending(e => e.Id)
                                    .ToCursorPagedListAsync();

        await act.Should().ThrowAsync<InvalidPaginationCursorException>()
           .WithMessage("*Could not convert cursor part 'notAnInt' to Int32*");
    }

    [Fact]
    public async Task Keyset_ToPagedAsyncEnumerable_FirstAndLastBothPresent_DoesNotThrowBackwardException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var results = new List<TestEntity>();
        await foreach (var item in query.Keyset(new CursorPaginationParameters { First = 5, Last = 5 })
                                        .Ascending(e => e.Id)
                                        .ToPagedAsyncEnumerable())
        {
            results.Add(item);
        }

        results.Count.Should().Be(5);
    }

    [Fact]
    public async Task Keyset_ToStreamingAsyncEnumerable_FirstAndLastBothPresent_DoesNotThrowBackwardException()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var results = new List<TestEntity>();
        await foreach (var item in query.Keyset(new CursorPaginationParameters { First = 5, Last = 5 })
                                        .Ascending(e => e.Id)
                                        .ToStreamingAsyncEnumerable(chunkSize: 5))
        {
            results.Add(item);
        }

        results.Count.Should().Be(30);
    }

    [Fact]
    public async Task Keyset_Composite_EnumAndId_ForwardAndBackward()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page1 = await query.Keyset(new CursorPaginationParameters { First = 5 })
                               .Ascending(e => e.StateValue)
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync();

        page1.Count.Should().Be(5);
        page1.HasNextPage.Should().BeTrue();

        var page2 = await query.Keyset(new CursorPaginationParameters { First = 5, After = page1.EndCursor })
                               .Ascending(e => e.StateValue)
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync();

        page2.Count.Should().Be(5);
        page2.HasPreviousPage.Should().BeTrue();

        var backwardPage = await query.Keyset(new CursorPaginationParameters { Last = 5, Before = page2.StartCursor })
                                      .Ascending(e => e.StateValue)
                                      .Ascending(e => e.Id)
                                      .ToCursorPagedListAsync();

        backwardPage.Count.Should().Be(5);
        backwardPage.Select(x => x.Id).Should().Equal(page1.Select(x => x.Id));
    }

    [Fact]
    public async Task Keyset_Composite_StringFirst_ForwardAndBackward()
    {
        using var context = GetDatabase();
        var query = context.Entities.AsQueryable();

        var page1 = await query.Keyset(new CursorPaginationParameters { First = 5 })
                               .Ascending(e => e.Name)
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync();

        page1.Count.Should().Be(5);
        page1.HasNextPage.Should().BeTrue();

        var page2 = await query.Keyset(new CursorPaginationParameters { First = 5, After = page1.EndCursor })
                               .Ascending(e => e.Name)
                               .Ascending(e => e.Id)
                               .ToCursorPagedListAsync();

        page2.Count.Should().Be(5);
        page2.HasPreviousPage.Should().BeTrue();

        var backwardPage = await query.Keyset(new CursorPaginationParameters { Last = 5, Before = page2.StartCursor })
                                      .Ascending(e => e.Name)
                                      .Ascending(e => e.Id)
                                      .ToCursorPagedListAsync();

        backwardPage.Count.Should().Be(5);
        backwardPage.Select(x => x.Id).Should().Equal(page1.Select(x => x.Id));
    }
}



