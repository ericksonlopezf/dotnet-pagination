// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class OraclePaginationExtensionsTests
{
    private readonly DbContext _dbContext;
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbParameterCollection _parameters;

    public OraclePaginationExtensionsTests()
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
            .UseSqlite(_connection)
            .Options;

        _dbContext = new DbContext(options);
    }

    [Fact]
    public async Task GetOracleApproximateCountAsync_NullContext_ThrowsArgumentNullException()
    {
        DbContext ctx = null!;
        var act = () => ctx.GetOracleApproximateCountAsync("users");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetOracleApproximateCountAsync_InvalidTableName_ThrowsArgumentException(string? tableName)
    {
        var act = () => _dbContext.GetOracleApproximateCountAsync(tableName!);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Table name is required.*");
    }

    [Theory]
    [InlineData("users; DROP TABLE users;")]
    [InlineData("users--")]
    [InlineData("users/*comment*/")]
    [InlineData("123users")]
    public async Task GetOracleApproximateCountAsync_SqlInjectionInTableName_ThrowsArgumentException(string tableName)
    {
        var act = () => _dbContext.GetOracleApproximateCountAsync(tableName);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Only [a-zA-Z_][a-zA-Z0-9_]* identifiers are supported.*");
    }

    [Theory]
    [InlineData("schema; DROP TABLE users;")]
    [InlineData("schema--")]
    [InlineData("schema/*comment*/")]
    [InlineData("123schema")]
    public async Task GetOracleApproximateCountAsync_SqlInjectionInSchemaName_ThrowsArgumentException(string schemaName)
    {
        var act = () => _dbContext.GetOracleApproximateCountAsync("users", schemaName);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Only [a-zA-Z_][a-zA-Z0-9_]* identifiers are supported.*");
    }

    [Fact]
    public async Task GetOracleApproximateCountAsync_QueriesUserTables_WhenNoSchemaProvided()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(450000L));

        var count = await _dbContext.GetOracleApproximateCountAsync("CUSTOMERS");

        count.Should().Be(450000L);
        _command.CommandText.Should().Contain("USER_TABLES");
    }

    [Fact]
    public async Task GetOracleApproximateCountAsync_QueriesAllTables_WhenSchemaProvided()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(1250000L));

        var count = await _dbContext.GetOracleApproximateCountAsync("ORDERS", "SALES_SCHEMA");

        count.Should().Be(1250000L);
        _command.CommandText.Should().Contain("ALL_TABLES");
    }

    [Fact]
    public async Task GetOracleApproximateCountAsync_ReturnsZero_WhenResultIsNull()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(null));

        var count = await _dbContext.GetOracleApproximateCountAsync("ORDERS");

        count.Should().Be(0L);
    }

    [Fact]
    public async Task GetOracleApproximateCountAsync_ReturnsZero_WhenResultIsDBNull()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(DBNull.Value));

        var count = await _dbContext.GetOracleApproximateCountAsync("ORDERS");

        count.Should().Be(0L);
    }

    [Fact]
    public async Task GetOracleApproximateCountAsync_OpensAndClosesConnection_WhenClosed()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(999L));

        var count = await _dbContext.GetOracleApproximateCountAsync("ORDERS");

        count.Should().Be(999L);
    }

    [Fact]
    public async Task GetOracleApproximateCountAsync_WithTransaction_UsesTransaction()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(1000L));

        var dbTransaction = Substitute.For<DbTransaction>();
        dbTransaction.Connection.Returns(_connection);

#pragma warning disable S6966
        _connection.BeginTransaction().Returns(dbTransaction);
        _connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(dbTransaction);
#pragma warning restore S6966
        _connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(dbTransaction));
        _connection.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(dbTransaction));

        await _dbContext.Database.UseTransactionAsync(dbTransaction);

        var count = await _dbContext.GetOracleApproximateCountAsync("ORDERS");

        count.Should().Be(1000L);
        _command.Received(1).Transaction = dbTransaction;
    }
}



