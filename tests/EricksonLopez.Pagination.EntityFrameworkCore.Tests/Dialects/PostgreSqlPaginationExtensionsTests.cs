// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class PostgreSqlPaginationExtensionsTests
{
    private TestDbContext GetContext() => TestDbContext.CreateInMemory();

    private readonly DbContext _dbContext;
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbParameterCollection _parameters;

    public PostgreSqlPaginationExtensionsTests()
    {
        _connection = Substitute.For<DbConnection>();
        _command = Substitute.For<DbCommand>();
        _parameters = Substitute.For<DbParameterCollection>();

        _command.Parameters.Returns(_parameters);
        
        var param1 = Substitute.For<DbParameter>();
        var param2 = Substitute.For<DbParameter>();
        var toggle = true;
        _command.CreateParameter().Returns(x => 
        {
            var p = toggle ? param1 : param2;
            toggle = !toggle;
            return p;
        });

        _connection.CreateCommand().Returns(_command);

        var options = new DbContextOptionsBuilder<DbContext>()
            .UseSqlite(_connection) // Use Sqlite just to provide a relational connection
            .Options;

        _dbContext = new DbContext(options);
    }

    [Fact]
    public async Task GetApproximateCountAsync_NullContext_ThrowsArgumentNullException()
    {
        DbContext ctx = null!;
        var act = () => ctx.GetApproximateCountAsync("users");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetApproximateCountAsync_InvalidTableName_ThrowsArgumentException(string? tableName)
    {
        var act = () => _dbContext.GetApproximateCountAsync(tableName!);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Table name is required.*");
    }

    [Theory]
    [InlineData("invalid-name")]
    [InlineData("user;drop table")]
    public async Task GetApproximateCountAsync_InvalidTableChars_ThrowsArgumentException(string tableName)
    {
        var act = () => _dbContext.GetApproximateCountAsync(tableName);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Table name contains invalid characters.*");
    }

    [Theory]
    [InlineData("invalid-schema")]
    [InlineData("schema;drop table")]
    public async Task GetApproximateCountAsync_InvalidSchemaChars_ThrowsArgumentException(string schemaName)
    {
        var act = () => _dbContext.GetApproximateCountAsync("users", schemaName);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Schema name contains invalid characters.*");
    }

    [Fact]
    public async Task GetApproximateCountAsync_ValidExecution_ReturnsCount()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(1000L));

        var count = await _dbContext.GetApproximateCountAsync("users");
        
        count.Should().Be(1000);
        await _command.Received(1).ExecuteScalarAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task GetApproximateCountAsync_NullResult_ReturnsZero()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(null));

        var count = await _dbContext.GetApproximateCountAsync("users");
        
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetApproximateCountAsync_WithTransaction_UsesTransaction()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(1000L));

        // Let's mock a DbTransaction
        var dbTransaction = Substitute.For<DbTransaction>();
        dbTransaction.Connection.Returns(_connection);

#pragma warning disable S6966
        _connection.BeginTransaction().Returns(dbTransaction);
        _connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(dbTransaction);
#pragma warning restore S6966
        _connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(dbTransaction));
        _connection.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(dbTransaction));
        
        // This is safe because _dbContext is using our mocked connection
        await _dbContext.Database.UseTransactionAsync(dbTransaction);

        var count = await _dbContext.GetApproximateCountAsync("users");
        
        count.Should().Be(1000);
        
        // Check that the command's transaction was set
        _command.Received(1).Transaction = dbTransaction;
    }

    [Fact]
    public async Task GetApproximateCountAsync_NegativeResult_ReturnsZero()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(-5L));

        var count = await _dbContext.GetApproximateCountAsync("users");
        
        count.Should().Be(0);
    }

    // ----- Gap 2: BuildRowValuePredicate tests -----

    [Fact]
    public void BuildRowValuePredicate_Forward_GeneratesCorrectSql()
    {
        // Gap 2: Row-value syntax for PostgreSQL. Forward pagination uses '>'.
        var (sql, parameters) = PostgreSqlPaginationExtensions.BuildRowValuePredicate(
            columns: new[] { "created_at", "id" },
            values: new object[] { "2024-01-01", 42 },
            lessThan: false);

        sql.Should().Be("(\"created_at\", \"id\") > (@__ksp_created_at_0__, @__ksp_id_1__)");
        parameters.Should().HaveCount(2);
        parameters["__ksp_created_at_0__"].Should().Be("2024-01-01");
        parameters["__ksp_id_1__"].Should().Be(42);
    }

    [Fact]
    public void BuildRowValuePredicate_Backward_GeneratesLessThanSql()
    {
        // Gap 2: Backward pagination uses '<'.
        var (sql, _) = PostgreSqlPaginationExtensions.BuildRowValuePredicate(
            columns: new[] { "id" },
            values: new object[] { 10 },
            lessThan: true);

        sql.Should().Be("(\"id\") < (@__ksp_id_0__)");
    }

    [Fact]
    public void BuildRowValuePredicate_InvalidColumnName_ThrowsArgumentException()
    {
        // Gap 2: SQL injection prevention — invalid identifiers must be rejected.
        Action act = () => PostgreSqlPaginationExtensions.BuildRowValuePredicate(
            columns: new[] { "valid", "bad; DROP TABLE users--" },
            values: new object[] { 1, 2 },
            lessThan: false);

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("bad; DROP TABLE users--");
    }

    [Fact]
    public void BuildRowValuePredicate_NullColumns_ThrowsArgumentNullException()
    {
        Action act = () => PostgreSqlPaginationExtensions.BuildRowValuePredicate(null!, new object[] { 1 }, false);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildRowValuePredicate_NullValues_ThrowsArgumentNullException()
    {
        Action act = () => PostgreSqlPaginationExtensions.BuildRowValuePredicate(new[] { "id" }, null!, false);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildRowValuePredicate_EmptyColumns_ThrowsArgumentException()
    {
        Action act = () => PostgreSqlPaginationExtensions.BuildRowValuePredicate(
            Array.Empty<string>(), Array.Empty<object>(), false);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildRowValuePredicate_MismatchedLengths_ThrowsArgumentException()
    {
        Action act = () => PostgreSqlPaginationExtensions.BuildRowValuePredicate(
            new[] { "id", "name" }, new object[] { 1 }, false);
        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("2").And.Contain("1");
    }

[Fact]
    public async Task PostgreSqlPaginationExtensions_GetApproximateCountAsync_ThrowsOnSqlite()
    {
        var ctx = GetContext();
        // Since we are running on SQLite, it should throw a SqliteException when executing pg_class query.
        // We catch it to cover the code path up to execution.
        var act = () => ctx.GetApproximateCountAsync("Table");
        await act.Should().ThrowAsync<SqliteException>();
        
        
        var act2 = () => ctx.GetApproximateCountAsync("Table");
        await act2.Should().ThrowAsync<SqliteException>();
    }

[Fact]
    public async Task PostgreSqlPaginationExtensions_GetApproximateCountAsync_ArgumentExceptions()
    {
        var act = () => ((DbContext)null!).GetApproximateCountAsync("Table");
        await act.Should().ThrowAsync<ArgumentNullException>();
        
        var ctx = GetContext();
        var act2 = () => ctx.GetApproximateCountAsync("");
        await act2.Should().ThrowAsync<ArgumentException>();
    }

[Fact]
    public async Task PostgreSqlPaginationExtensions_GetApproximateCountAsync_Arguments()
    {
        var act1 = () => PostgreSqlPaginationExtensions.GetApproximateCountAsync(null!, "table");
        await act1.Should().ThrowAsync<ArgumentNullException>();
        var act2 = () => PostgreSqlPaginationExtensions.GetApproximateCountAsync(new TestDbContext(), "");
        await act2.Should().ThrowAsync<ArgumentException>();
    }

[Fact]
    public void PostgreSqlPaginationExtensions_BuildRowValuePredicate_HappyPath()
    {
        var columns = new List<string> { "col1", "col2" };
        var values = new List<object> { 1, "test" };
        var result = PostgreSqlPaginationExtensions.BuildRowValuePredicate(columns, values, lessThan: true);
        result.Sql.Should().NotBeNullOrWhiteSpace();
        result.Parameters.Should().HaveCount(2);
    }
}






