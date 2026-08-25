// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.LinqToDB;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Pagination.LinqToDB.Tests;

public sealed class LinqToDBAdvancedKeysetTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public LinqToDBAdvancedKeysetTests()
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
            var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entities = Enumerable.Range(1, 50).Select(i => new TestEntity
            {
                Id = i,
                Name = $"Entity {i:D3}",
                NullableId = i % 5 == 0 ? null : i,
                StateValue = i % 2 == 0 ? TestState.Two : TestState.One,
                CreatedAt = baseDate.AddDays(i % 10)
            });
            context.BulkCopy(entities);
        }
        catch
        {
            // Table might already exist
        }

        return context;
    }

    [Fact]
    public async Task Keyset_ThreeColumnComposite_Ascending_TraversesAllPages()
    {
        using var db = GetDatabase();

        var allItems = new List<TestEntity>();
        string? nextCursor = null;

        do
        {
            var pageCursor = new CursorPaginationParameters { First = 10, After = nextCursor };
            var page = await db.Entities
                .Keyset(pageCursor)
                .Ascending(e => e.CreatedAt)
                .Ascending(e => e.StateValue)
                .Ascending(e => e.Id)
                .ToCursorPagedListAsync();

            allItems.AddRange(page);
            nextCursor = page.EndCursor;
        }
        while (!string.IsNullOrEmpty(nextCursor) && allItems.Count < 50);

        allItems.Count.Should().Be(50);
        var sortedExpected = allItems
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.StateValue)
            .ThenBy(e => e.Id)
            .ToList();

        allItems.Select(e => e.Id).Should().Equal(sortedExpected.Select(e => e.Id));
    }

    [Fact]
    public async Task Keyset_BackwardNavigation_WithBeforeCursor_ReturnsPrecedingPage()
    {
        using var db = GetDatabase();

        // 1. Get first page
        var page1 = await db.Entities
            .Keyset(new CursorPaginationParameters { First = 10 })
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();

        // 2. Get second page
        var page2 = await db.Entities
            .Keyset(new CursorPaginationParameters { First = 10, After = page1.EndCursor })
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();

        page2[0].Id.Should().Be(11);

        // 3. Navigate backward from page 2 using Before cursor
        var backwardPage = await db.Entities
            .Keyset(new CursorPaginationParameters { Last = 10, Before = page2.StartCursor })
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();

        backwardPage.Select(e => e.Id).Should().Equal(page1.Select(e => e.Id));
    }

    [Fact]
    public async Task Keyset_WithCustomEncoder_EncodesAndDecodesSuccessfully()
    {
        using var db = GetDatabase();
        var customEncoder = new Base64CursorEncoder();

        var page = await db.Entities
            .Keyset(new CursorPaginationParameters { First = 5 }, cursorEncoder: customEncoder)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();

        page.Count.Should().Be(5);
        page.EndCursor.Should().NotBeNullOrEmpty();

        var page2 = await db.Entities
            .Keyset(new CursorPaginationParameters { First = 5, After = page.EndCursor }, cursorEncoder: customEncoder)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();

        page2[0].Id.Should().Be(6);
    }

    [Fact]
    public async Task Keyset_WithCancellationToken_HonorsCancellation()
    {
        using var db = GetDatabase();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = async () =>
        {
            await db.Entities
                .Keyset(new CursorPaginationParameters { First = 10 })
                .Ascending(e => e.Id)
                .ToCursorPagedListAsync(cancellationToken: cts.Token);
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
