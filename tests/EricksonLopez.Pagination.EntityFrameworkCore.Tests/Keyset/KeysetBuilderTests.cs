// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class KeysetBuilderTests
{
    private static TestDbContext GetContext(int entityCount = 0) => TestDbContext.CreateInMemory(entityCount);

    

    
    


    [Fact]
    public async Task ToCursorPagedListAsync_NoColumns_ThrowsInvalidOperationException()
    {
        var context = GetContext(1);
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParameters());

        Func<Task> act = async () => await builder.ToCursorPagedListAsync();
        
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("At least one column must be specified for keyset pagination.");
    }

    [Fact]
    public async Task Descending_OrdersCorrectly()
    {
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithFirst(5).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Descending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        result.Count.Should().Be(5);
        result[0].Id.Should().Be(5); // Descending order
    }

    [Fact]
    public async Task Ascending_OrdersCorrectly()
    {
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithFirst(5).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        result.Count.Should().Be(5);
        result[0].Id.Should().Be(1); // Ascending order
    }

    [Fact]
    public async Task KeysetBuilder_WithLastAndFirst_TreatsAsForwardPagination()
    {
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithFirst(2).WithLast(2).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        // Since both First and Last are specified, First takes precedence
        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task BackwardPagination_ItemsReversedCorrectly()
    {
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithLast(2).Build(); // No First -> isBackward = true
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        // 5 items, we want Last 2. Should be 4 and 5 in ascending order.
        result.Count.Should().Be(2);
        result[0].Id.Should().Be(4);
        result[1].Id.Should().Be(5);
    }

    [Fact]
    public async Task ForwardPagination_HasNext_ReturnsTrue()
    {
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithFirst(2).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task BackwardPagination_HasPrevious_ReturnsTrue()
    {
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithLast(2).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ForwardPagination_WithAfterValue_SetsHasPreviousPageTrue()
    {
        var context = GetContext(5);
        // Pretend we are fetching First=2, but we also pass After cursor
        var parameters = new CursorPaginationParametersBuilder().WithFirst(2).WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("4")).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        // forward with afterCursor -> hasPrevious = true
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task BackwardPagination_WithBeforeValue_SetsHasNextPageTrue()
    {
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithLast(2).WithBefore(HmacCursorEncoder.DevelopmentDefault.Encode("1")).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync();
        // backward with beforeCursor -> hasNext = true
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidCursorValue_ThrowsInvalidPaginationCursorException_WithSpecificMessage()
    {
        var context = GetContext(5);
        // Valid cursor for Id (int), but we supply a string that cannot be parsed to int
        var badCursor = HmacCursorEncoder.DevelopmentDefault.Encode("NotAnInt");
        var parameters = new CursorPaginationParametersBuilder().WithFirst(2).WithAfter(badCursor).Build();
        var builder = context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id);

        Func<Task> act = async () => await builder.ToCursorPagedListAsync();
        var ex = await act.Should().ThrowAsync<InvalidPaginationCursorException>();
        ex.WithMessage("Could not convert cursor part 'NotAnInt' to Int32.");
    }

    [Fact]
    public void Ascending_WithNullableColumn_ShouldThrowInvalidOperationException()
    {
        // Tests fix for P1-001 (NullReferenceException in KeysetBuilder with nullable columns)
        var context = GetContext(5);
        var parameters = new CursorPaginationParametersBuilder().WithFirst(2).Build();
        
        Action act = () => context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.NullableId) // Contains nulls
            .Ascending(e => e.Id);        // Tie breaker

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Keyset pagination on nullable property*is not supported*");
    }

    // ----- Gap 3: GetKeysetSchemaFingerprint public API tests -----

    [Fact]
    public void GetKeysetSchemaFingerprint_SameSchemaTwice_ReturnsSameValue()
    {
        // Gap 3: The fingerprint must be deterministic — same schema always yields same fingerprint
        // across different builder instances, processes, and deployments.
        var context = GetContext(1);

        var b1 = context.Entities.AsQueryable()
            .Keyset(CursorPaginationParameters.Default)
            .Ascending(e => e.Id);

        var b2 = context.Entities.AsQueryable()
            .Keyset(CursorPaginationParameters.Default)
            .Ascending(e => e.Id);

        b1.GetKeysetSchemaFingerprint().Should().Be(b2.GetKeysetSchemaFingerprint());
    }

    [Fact]
    public void GetKeysetSchemaFingerprint_DifferentColumnType_ReturnsDifferentValue()
    {
        // Gap 3: Adding a column changes the schema and must change the fingerprint.
        var context = GetContext(1);

        var b1 = context.Entities.AsQueryable()
            .Keyset(CursorPaginationParameters.Default)
            .Ascending(e => e.Id);

        var b2 = context.Entities.AsQueryable()
            .Keyset(CursorPaginationParameters.Default)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name); // extra column → different schema

        b1.GetKeysetSchemaFingerprint().Should().NotBe(b2.GetKeysetSchemaFingerprint());
    }

    [Fact]
    public void GetKeysetSchemaFingerprint_DifferentSortDirection_ReturnsDifferentValue()
    {
        // Gap 3: Changing sort direction must change the fingerprint —
        // a cursor from an ASC keyset is invalid for a DESC keyset.
        var context = GetContext(1);

        var ascending = context.Entities.AsQueryable()
            .Keyset(CursorPaginationParameters.Default)
            .Ascending(e => e.Id);

        var descending = context.Entities.AsQueryable()
            .Keyset(CursorPaginationParameters.Default)
            .Descending(e => e.Id);

        ascending.GetKeysetSchemaFingerprint().Should().NotBe(descending.GetKeysetSchemaFingerprint());
    }

    [Fact]
    public void GetKeysetSchemaFingerprint_ReturnsEightCharHex()
    {
        // Gap 3: The fingerprint must be exactly 8 uppercase hex characters (FNV-1a 32-bit).
        var context = GetContext(1);

        var builder = context.Entities.AsQueryable()
            .Keyset(CursorPaginationParameters.Default)
            .Ascending(e => e.Id);

        var fingerprint = builder.GetKeysetSchemaFingerprint();

        fingerprint.Should().HaveLength(8);
        fingerprint.Should().MatchRegex("^[0-9A-F]{8}$");
    }
    [Fact]
    public async Task ToStreamingAsyncEnumerable_StreamsAllRecordsInChunks()
    {
        var context = GetContext(15);
        var parameters = new CursorPaginationParameters();

        var stream = context.Entities
            .Keyset(parameters)
            .Ascending(e => e.Id)
            .ToStreamingAsyncEnumerable(chunkSize: 5);

        var list = new List<TestEntity>();
        await foreach (var item in stream)
        {
            list.Add(item);
        }

        list.Should().HaveCount(15);
        list[0].Id.Should().Be(1);
        list[^1].Id.Should().Be(15);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenCancellationTokenCanceled_ThrowsOperationCanceledException()
    {
        var context = GetContext(10);
        var canceledToken = new CancellationToken(canceled: true);

        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(5).Build())
            .Ascending(e => e.Id);

        Func<Task> act = async () => await builder.ToCursorPagedListAsync(cancellationToken: canceledToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private class CustomPagedListFactory : ICursorPagedListFactory
    {
        public bool Called { get; private set; }
        public ICursorPagedList<T> CreateCursorPagedList<T>(IReadOnlyList<T> items, long? totalCount, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)
        {
            Called = true;
            return DefaultPagedListFactory.Instance.CreateCursorPagedList(items, totalCount, startCursor, endCursor, hasPreviousPage, hasNextPage);
        }
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithCustomFactory_UsesCustomFactory()
    {
        var context = GetContext(5);
        var customFactory = new CustomPagedListFactory();
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(2).Build())
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync(factory: customFactory);
        customFactory.Called.Should().BeTrue();
        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithCustomFactory_AndProjection_UsesCustomFactory()
    {
        var context = GetContext(5);
        var customFactory = new CustomPagedListFactory();
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(2).Build())
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync(e => e.Name, factory: customFactory);
        customFactory.Called.Should().BeTrue();
        result.Count.Should().Be(2);
    }

    private class TestEntityWithNullable
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime? NullableDate { get; set; }
    }

    [Fact]
    public void SortBy_WithAllowedProperties_ThrowsWhenDisallowed()
    {
        var context = GetContext(1);
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParameters());
        var act = () => builder.SortBy(SortParameters.From("Name"), new[] { "Id" });
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Sorting on property 'Name' is not permitted.*");
    }

    [Fact]
    public void SortBy_WithNullableProperty_ThrowsInvalidOperationException()
    {
        var data = new List<TestEntityWithNullable>().AsQueryable();
        var builder = data.Keyset(new CursorPaginationParameters());
        var act = () => builder.SortBy(SortParameters.From("NullableDate"));
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Keyset pagination on nullable property 'NullableDate' is not supported*");
    }

    [Fact]
    public void SortBy_WithNonExistentProperty_ThrowsArgumentException()
    {
        var context = GetContext(1);
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParameters());
        var act = () => builder.SortBy(SortParameters.From("NonExistentProperty"));
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Property 'NonExistentProperty' not found on type 'TestEntity'.*");
    }

    [Fact]
    public async Task SortBy_AscendingAndDescending_OrdersCorrectly()
    {
        var context = GetContext(5);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(5).Build())
            .SortBy(SortParameters.From("Id desc"));

        var result = await builder.ToCursorPagedListAsync();
        result[0].Id.Should().Be(5);
        result[4].Id.Should().Be(1);

        var builderAsc = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(5).Build())
            .SortBy(SortParameters.From("Id"));

        var resultAsc = await builderAsc.ToCursorPagedListAsync();
        resultAsc[0].Id.Should().Be(1);
        resultAsc[4].Id.Should().Be(5);
    }
}






