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

public class SqlServerPaginationExtensionsTests
{
    private readonly DbContext _dbContext;
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbParameterCollection _parameters;

    public SqlServerPaginationExtensionsTests()
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
    public async Task GetApproximateCountAsync_NullContext_ThrowsArgumentNullException()
    {
        DbContext ctx = null!;
        var act = () => ctx.GetSqlServerApproximateCountAsync("users");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetApproximateCountAsync_InvalidTableName_ThrowsArgumentException(string? tableName)
    {
        var act = () => _dbContext.GetSqlServerApproximateCountAsync(tableName!);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Table name is required.*");
    }

    [Theory]
    [InlineData("invalid-name")]
    [InlineData("user;drop table")]
    public async Task GetApproximateCountAsync_InvalidTableChars_ThrowsArgumentException(string tableName)
    {
        var act = () => _dbContext.GetSqlServerApproximateCountAsync(tableName);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Table name contains invalid characters.*");
    }

    [Theory]
    [InlineData("invalid-schema")]
    [InlineData("schema;drop table")]
    public async Task GetApproximateCountAsync_InvalidSchemaChars_ThrowsArgumentException(string schemaName)
    {
        var act = () => _dbContext.GetSqlServerApproximateCountAsync("users", schemaName);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Schema name contains invalid characters.*");
    }

    [Fact]
    public async Task GetApproximateCountAsync_ValidExecution_ReturnsCount()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(5000L));

        var count = await _dbContext.GetSqlServerApproximateCountAsync("users", "dbo");
        
        count.Should().Be(5000);
        await _command.Received(1).ExecuteScalarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetApproximateCountAsync_StaticMethod_ReturnsCount()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(3000L));

        var count = await SqlServerPaginationExtensions.GetApproximateCountAsync(_dbContext, "users");
        
        count.Should().Be(3000);
    }
    
    [Fact]
    public async Task GetApproximateCountAsync_NullResult_ReturnsZero()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(null));

        var count = await _dbContext.GetSqlServerApproximateCountAsync("users");
        
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetApproximateCountAsync_DbNullResult_ReturnsZero()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(DBNull.Value));

        var count = await _dbContext.GetSqlServerApproximateCountAsync("users");
        
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetApproximateCountAsync_NegativeResult_ReturnsZero()
    {
        _connection.State.Returns(ConnectionState.Closed);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(-10L));

        var count = await _dbContext.GetSqlServerApproximateCountAsync("users");
        
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetApproximateCountAsync_WhenConnectionAlreadyOpen_DoesNotOpenOrClose()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(1234L));

        var count = await _dbContext.GetSqlServerApproximateCountAsync("users");
        count.Should().Be(1234L);
    }

    [Fact]
    public async Task GetApproximateCountAsync_WithTransaction_UsesTransaction()
    {
        _connection.State.Returns(ConnectionState.Open);
        _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(5000L));

        var dbTransaction = Substitute.For<DbTransaction>();
        dbTransaction.Connection.Returns(_connection);

#pragma warning disable S6966
        _connection.BeginTransaction().Returns(dbTransaction);
        _connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(dbTransaction);
#pragma warning restore S6966
        _connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(dbTransaction));
        _connection.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(dbTransaction));

        await _dbContext.Database.UseTransactionAsync(dbTransaction);

        var count = await _dbContext.GetSqlServerApproximateCountAsync("users");

        count.Should().Be(5000L);
        _command.Received(1).Transaction = dbTransaction;
    }
}



