// Copyright © Erickson Lopez. MIT License.
#nullable enable
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Dapper;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.Dapper.Tests;

public class DapperKeysetBuilderTests
{
    private async Task<SqliteConnection> GetConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE Products (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                CategoryId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            )");

        var baseDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 1; i <= 25; i++)
        {
            await connection.ExecuteAsync(
                "INSERT INTO Products (Id, Name, CategoryId, CreatedAt) VALUES (@Id, @Name, @CategoryId, @CreatedAt)",
                new
                {
                    Id = i,
                    Name = $"Product {i}",
                    CategoryId = i % 5,
                    CreatedAt = baseDate.AddDays(i).ToString("O")
                });
        }

        return connection;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        var act = () => new DapperKeysetBuilder<Product>(null!, new CursorPaginationParameters());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithoutCursorColumns_ThrowsInvalidOperationException()
    {
        using var connection = await GetConnectionAsync();
        var builder = new DapperKeysetBuilder<Product>(connection, new CursorPaginationParameters())
            .Select("*").From("Products");

        var act = () => builder.ExecuteAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void WithCursorColumns_EmptySelectors_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var builder = new DapperKeysetBuilder<Product>(connection, new CursorPaginationParameters());

        var act = () => builder.WithCursorColumns();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithCursorDecoder_EmptyDecoders_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var builder = new DapperKeysetBuilder<Product>(connection, new CursorPaginationParameters());

        var act = () => builder.WithCursorDecoder();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithDefaultPageSize_LessThanOne_ThrowsArgumentOutOfRangeException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var builder = new DapperKeysetBuilder<Product>(connection, new CursorPaginationParameters());

        var act = () => builder.WithDefaultPageSize(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithMaxPageSize_LessThanOne_ThrowsArgumentOutOfRangeException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var builder = new DapperKeysetBuilder<Product>(connection, new CursorPaginationParameters());

        var act = () => builder.WithMaxPageSize(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ExecuteAsync_1ColumnKeyset_FirstPage_ReturnsResults()
    {
        using var connection = await GetConnectionAsync();
        
        var page = await new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=10", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            .WithCursorColumns(p => p.Id.ToString())
            .ExecuteAsync();

        page.Should().NotBeNull();
        page.Count.Should().Be(10);
        page[0].Id.Should().Be(1);
        page[^1].Id.Should().Be(10);
    }

    [Fact]
    public async Task ExecuteAsync_2ColumnKeyset_FiltersCorrectly()
    {
        using var connection = await GetConnectionAsync();
        
        var page = await new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=5", null))
            .Select("*")
            .From("Products")
            .Where("CategoryId = @CategoryId")
            .OrderBy("CreatedAt")
            .ThenBy("Id")
            .UseDialect(DatabaseDialect.Sqlite)
            .WithCursorColumns(p => p.CreatedAt, p => p.Id.ToString())
            .WithCursorDecoder(parts => parts[0], parts => int.Parse(parts[1], CultureInfo.InvariantCulture))
            .WithParam(new { CategoryId = 3 })
            .WithCommandTimeout(30)
            .WithCommandType(CommandType.Text)
            .WithAutoReverse(true)
            .ExecuteAsync();

        page.Count.Should().Be(5); // There are exactly 5 items with CategoryId 3 (3, 8, 13, 18, 23)
        page[0].Id.Should().Be(3);
        page[1].Id.Should().Be(8);
        
        // Let's get the next page
        var nextPage = await new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=5&after=" + page.EndCursor, null))
            .Select("*")
            .From("Products")
            .Where("CategoryId = @CategoryId")
            .OrderBy("CreatedAt")
            .ThenBy("Id")
            .UseDialect(DatabaseDialect.Sqlite)
            .WithCursorColumns(p => p.CreatedAt, p => p.Id.ToString())
            .WithCursorDecoder(parts => parts[0], parts => int.Parse(parts[1], CultureInfo.InvariantCulture))
            .WithParam(new { CategoryId = 3 })
            .ExecuteAsync();

        nextPage.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_BackwardPagination_ReversesResults()
    {
        using var connection = await GetConnectionAsync();
        
        // Get last page (ids 16..25)
        var lastPage = await new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("last=10", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            .WithCursorColumns(p => p.Id.ToString())
            .ExecuteAsync();

        lastPage.Count.Should().Be(10);
        lastPage[0].Id.Should().Be(16);
        lastPage[^1].Id.Should().Be(25);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCursor_ThrowsInvalidPaginationCursorException()
    {
        using var connection = await GetConnectionAsync();
        
        // Use an invalid cursor string
        var act = () => new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=10&after=INVALID_CURSOR", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            .WithCursorColumns(p => p.Id.ToString())
            // Custom encoder that throws FormatException
            .WithEncoder(new TestInvalidEncoder())
            .ExecuteAsync();

        await act.Should().ThrowAsync<InvalidPaginationCursorException>();
    }
    
    private class TestInvalidEncoder : ICursorEncoder
    {
        public string? Encode(string? cursor) => cursor;
        public string? Decode(string? cursor) => throw new FormatException("Invalid");
    }
    
    [Fact]
    public async Task BuildCursorString_WithPercent_IsEscaped()
    {
        using var connection = await GetConnectionAsync();
        
        var page = await new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=1", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            // Manually inject a percent to test encoding
            .WithCursorColumns(p => "%" + p.Id.ToString())
            .ExecuteAsync();

        // The default encoder is Base64, let's decode
        var raw = HmacCursorEncoder.DevelopmentDefault.Decode(page.StartCursor);
        raw.Should().Be("%251");
    }
    [Fact]
    public async Task BuilderConfiguration_AndNullCursor_AreCovered()
    {
        using var connection = await GetConnectionAsync();
        using var transaction = connection.BeginTransaction();
        
        var factory = Substitute.For<ICursorPagedListFactory>();
        var encoder = Substitute.For<ICursorEncoder>();
        encoder.Decode(Arg.Any<string>()).Returns((string?)null); // Force decoded to null

        var builder = new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=10&after=ANY", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            .WithCursorColumns(p => p.Id.ToString())
            .WithTransaction(transaction)
            .WithCommandTimeout(30)
            .WithCommandType(System.Data.CommandType.Text)
            .WithDefaultPageSize(15)
            .WithMaxPageSize(100)
            .WithEncoder(encoder)
            .WithFactory(factory);

        await builder.ExecuteAsync();

        factory.ReceivedWithAnyArgs().CreateCursorPagedList<Product>(default!, default, default, default, default, default);
    }

    [Fact]
    public async Task WithConnectionFactory_NullFactory_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<System.Data.IDbConnection>();
        var builder = new DapperKeysetBuilder<Product>(connection, new CursorPaginationParameters());
        
        var act = () => builder.WithFactory(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Builder_WithoutDecoders_UsesStringFallback()
    {
        using var connection = await GetConnectionAsync();
        
        var encoder = Substitute.For<ICursorEncoder>();
        encoder.Decode(Arg.Any<string>()).Returns("123|Test"); // Provide valid string

        var builder = new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=10&after=ANY", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            .ThenBy("Name")
            .WithCursorColumns(p => p.Id.ToString(), p => p.Name)
            // NO WithCursorDecoder
            .WithEncoder(encoder);

        var result = await builder.ExecuteAsync();
        
        // This will successfully execute the SQL query because Sqlite can implicitly cast the string "123" to integer!
        result.Count.Should().Be(0); // 123 is greater than any Id in seed data
    }

    [Fact]
    public void BuilderConfiguration_ThrowsOnInvalidPageSizes()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection();
        var builder = new DapperKeysetBuilder<Product>(connection, new CursorPaginationParameters());
        
        var act1 = () => builder.WithDefaultPageSize(0);
        act1.Should().Throw<ArgumentOutOfRangeException>();
        
        var act2 = () => builder.WithMaxPageSize(0);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ExecuteAsync_NullCursorSelector_UsesEmptyString()
    {
        using var connection = await GetConnectionAsync();
        
        var page = await new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=1", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            .ThenBy("Name")
            // Return null to cover the null coalescing operator in BuildCursorString
            .WithCursorColumns(p => null, p => null)
            .ExecuteAsync();

        var raw = HmacCursorEncoder.DevelopmentDefault.Decode(page.StartCursor);
        // Both columns return null, so they get converted to empty strings separated by pipe "|"
        raw.Should().Be("|");
    }

    [Fact]
    public async Task ExecuteAsync_SelectColumns_AppliesSelectClause()
    {
        var mockConn = NSubstitute.Substitute.For<IDbConnection>();
        var command = NSubstitute.Substitute.For<IDbCommand>();
        mockConn.CreateCommand().Returns(command);
        
        var builder = new DapperKeysetBuilder<Entity>(mockConn, CursorPaginationParameters.Parse("first=10", null))
            .Select("Id, Name")
            .From("Entities")
            .OrderBy("Id")
            .WithCursorColumns(e => e.Id.ToString());
            
        _ = await Record.ExceptionAsync(() => builder.ExecuteAsync());
        
        command.CommandText.Should().StartWith("SELECT Id, Name FROM Entities");
    }

    [Fact]
    public async Task ExecuteAsync_UseDialectSqlServer_GeneratesFetchNext()
    {
        var mockConn = NSubstitute.Substitute.For<IDbConnection>();
        var command = NSubstitute.Substitute.For<IDbCommand>();
        mockConn.CreateCommand().Returns(command);

        var builder = new DapperKeysetBuilder<Entity>(mockConn, CursorPaginationParameters.Parse("first=10", null))
            .Select("*")
            .From("Entities")
            .OrderBy("Id")
            .UseDialect(DatabaseDialect.SqlServer)
            .WithCursorColumns(e => e.Id.ToString());
            
        _ = await Record.ExceptionAsync(() => builder.ExecuteAsync());
        
        command.CommandText.Should().Contain("FETCH NEXT");
    }

    [Fact]
    public void WithCursorColumns_Null_ThrowsArgumentNullException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var builder = new DapperKeysetBuilder<Entity>(connection, new CursorPaginationParameters());
        Action act = () => builder.WithCursorColumns(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task WithCursorColumns_CalledTwice_ReplacesPreviousSelectors()
    {
        using var connection = await GetConnectionAsync();
        var page = await new DapperKeysetBuilder<Product>(connection, CursorPaginationParameters.Parse("first=1", null))
            .Select("*")
            .From("Products")
            .OrderBy("Id")
            .WithCursorColumns(p => "OLD1", p => "OLD2")
            .WithCursorColumns(p => p.Id.ToString())
            .ExecuteAsync();

        var raw = HmacCursorEncoder.DevelopmentDefault.Decode(page.StartCursor);
        raw.Should().Be("1");
    }

    [Fact]
    public void WithCursorDecoder_Null_ThrowsArgumentNullException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var builder = new DapperKeysetBuilder<Entity>(connection, new CursorPaginationParameters());
        Action act = () => builder.WithCursorDecoder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithEncoder_Null_ThrowsArgumentNullException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var builder = new DapperKeysetBuilder<Entity>(connection, new CursorPaginationParameters());
        Action act = () => builder.WithEncoder(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}






