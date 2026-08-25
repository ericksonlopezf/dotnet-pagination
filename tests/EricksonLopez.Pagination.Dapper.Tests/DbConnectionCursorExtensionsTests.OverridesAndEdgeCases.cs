// Copyright © Erickson Lopez. MIT License.
#pragma warning disable CS0618
#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Pagination.Dapper.Tests;

public partial class DbConnectionCursorExtensionsTests
{
    private static Task<SqliteConnection> GetConnectionAsync() => DapperTestHelper.GetConnectionAsync();

    private sealed class CustomCursorEncoder : ICursorEncoder
    {
        public string? Encode(string? cursor) => "CUSTOM_ENCODED";
        public string? Decode(string? cursor) => "CUSTOM_DECODED";
    }

    private sealed class CustomFactory : ICursorPagedListFactory
    {
        public ICursorPagedList<T> CreateCursorPagedList<T>(IReadOnlyList<T> items, long? totalCount, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)
        {
            return new CursorPagedList<T>(items, "CUSTOM_START", "CUSTOM_END", hasPreviousPage, hasNextPage);
        }
    }

    private sealed class NullReturningEncoder : ICursorEncoder
    {
        public string? Encode(string? cursor) => null;
        public string? Decode(string? cursor) => null;
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithAllOptionalParameters_AppliesOverridesCorrectly()
    {
        using var connection = await GetConnectionAsync();
        using var transaction = connection.BeginTransaction();
        var parameters = new CursorPaginationParameters(); // No first/last to trigger defaultPageSize
        var sql = "SELECT * FROM Entities WHERE Name LIKE @SearchPattern AND (@Cursor IS NULL OR Id > @Cursor) ORDER BY Id LIMIT @__Pagination_Limit__;";
        var param = new { SearchPattern = "Entity %" };

        var encoder = new CustomCursorEncoder();
        var factory = new CustomFactory();
        var tokenSource = new CancellationTokenSource();

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s),
            param: param,
            transaction: transaction,
            commandTimeout: 30,
            commandType: CommandType.Text,
            defaultPageSize: 3,
            cursorEncoder: encoder,
            factory: factory,
            cancellationToken: tokenSource.Token
        );

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(3); // from defaultPageSize = 3
        pagedList.StartCursor.Should().Be("CUSTOM_START"); // custom factory
        pagedList.EndCursor.Should().Be("CUSTOM_END");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithCustomEncoder_EncodesCursorsSuccessfully()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=5", null);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__;";

        var encoder = new CustomCursorEncoder();

        var pagedList = await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s),
            cursorEncoder: encoder);

        pagedList.EndCursor.Should().Be("CUSTOM_ENCODED");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_InvalidCursor_ThrowsException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=5&after=INVALID", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        Func<Task> act = async () => await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id);

        await act.Should().ThrowAsync<InvalidPaginationCursorException>();
    }

    [Theory]
    [InlineData("NotAnInt", typeof(int))]
    [InlineData("NotAGuid", typeof(Guid))]
    [InlineData("999999999999999999999999", typeof(short))]
    [InlineData("SomeString", typeof(TimeSpan))]
    public async Task ToCursorPagedListAsync_SingleKey_ConvertFails_ThrowsException(string invalidValue, Type type)
    {
        using var connection = await GetConnectionAsync();
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode(invalidValue);
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        Func<Task> act;
        
        if (type == typeof(int))
            act = async () => await connection.ToCursorPagedListAsync<Entity, int>(sql, parameters, e => e.Id);
        else if (type == typeof(Guid))
            act = async () => await connection.ToCursorPagedListAsync<Entity, Guid>(sql, parameters, e => Guid.NewGuid());
        else if (type == typeof(short))
            act = async () => await connection.ToCursorPagedListAsync<Entity, short>(sql, parameters, e => 0);
        else if (type == typeof(TimeSpan))
            act = async () => await connection.ToCursorPagedListAsync<Entity, TimeSpan>(sql, parameters, e => TimeSpan.Zero);
        else 
            throw new InvalidOperationException("Unknown type");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Could not convert the decoded cursor value*Provide a 'cursorDecoder' delegate for types that require custom parsing (e.g., DateTimeOffset, complex composite keys).*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_DecoderReturnsNull_ThrowsException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=5&after=SOMETHING", null);
        var sql = "SELECT * FROM Entities WHERE Id > @Cursor ORDER BY Id LIMIT @__Pagination_Limit__;";

        Func<Task> act = async () => await connection.ToCursorPagedListAsync<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorEncoder: new NullReturningEncoder());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*The cursor value 'SOMETHING' could not be decoded. Ensure the cursor was produced by this library and has not been tampered with.*");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_SingleKey_WithRegisteredDecoder_UsesRegistry()
    {
        using var connection = await GetConnectionAsync();
        var registry = new InMemoryCursorDecoderRegistry();
        registry.Register<DateTime>(s => new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var after = EricksonLopez.Pagination.HmacCursorEncoder.DevelopmentDefault.Encode("DUMMY");
        var parameters = CursorPaginationParameters.Parse($"first=5&after={after}", null);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__;";

        var pagedList = await connection.ToCursorPagedListAsync<Entity, DateTime>(
            sql, 
            parameters, 
            keySelector: e => new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            decoderRegistry: registry);

        pagedList.Should().NotBeNull();
    }

    [Fact]
    public async Task ToCursorPagedAsyncEnumerable_Cancellation_ThrowsTaskCanceledException()
    {
        using var connection = await GetConnectionAsync();
        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = "SELECT * FROM Entities ORDER BY Id LIMIT @__Pagination_Limit__;";

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var asyncEnumerable = connection.ToCursorPagedAsyncEnumerable<Entity, int>(
            sql, 
            parameters, 
            keySelector: e => e.Id, 
            cursorDecoder: s => int.Parse(s),
            cancellationToken: cts.Token);

        var act = async () =>
        {
            await foreach (var item in asyncEnumerable)
            {
                _ = item;
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
