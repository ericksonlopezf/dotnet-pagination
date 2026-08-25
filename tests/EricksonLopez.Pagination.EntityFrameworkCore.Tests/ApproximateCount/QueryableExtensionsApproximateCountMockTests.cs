// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
#pragma warning disable CS8765
#pragma warning disable CS8767
#pragma warning disable S3881
#pragma warning disable S4136
#pragma warning disable S927
#pragma warning disable S6966
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class QueryableExtensionsApproximateCountMockTests
{
    public class TestEntity
    {
        public int Id { get; set; }
    }
    private class MockDbCommand : DbCommand
    {
        private readonly DbCommand _inner;
        private readonly object _returnValue;
        private readonly Exception? _exceptionToThrow;

        public MockDbCommand(DbCommand inner, object returnValue, Exception? exceptionToThrow)
        {
            _inner = inner;
            _returnValue = returnValue;
            _exceptionToThrow = exceptionToThrow;
        }

        public override void Cancel() => _inner.Cancel();
        public override int ExecuteNonQuery() => _inner.ExecuteNonQuery();
        public override object? ExecuteScalar() => _inner.ExecuteScalar();
        public override void Prepare() => _inner.Prepare();
        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => _inner.ExecuteReader(behavior);
        
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (CommandText != null && CommandText.Contains("pg_class"))
            {
                if (_exceptionToThrow != null) throw _exceptionToThrow;
                return Task.FromResult<object?>(_returnValue);
            }
            return _inner.ExecuteScalarAsync(cancellationToken);
        }

        public override string CommandText { get => _inner.CommandText; set => _inner.CommandText = value; }
        public override int CommandTimeout { get => _inner.CommandTimeout; set => _inner.CommandTimeout = value; }
        public override CommandType CommandType { get => _inner.CommandType; set => _inner.CommandType = value; }
        public override bool DesignTimeVisible { get => _inner.DesignTimeVisible; set => _inner.DesignTimeVisible = value; }
        public override UpdateRowSource UpdatedRowSource { get => _inner.UpdatedRowSource; set => _inner.UpdatedRowSource = value; }
        protected override DbConnection? DbConnection { get => _inner.Connection; set => _inner.Connection = value; }
        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;
        protected override DbTransaction? DbTransaction 
        { 
            get => _inner.Transaction; 
            set => _inner.Transaction = (value as MockDbTransaction)?._inner ?? value; 
        }
    }

    private class MockDbTransaction : DbTransaction
    {
        internal readonly DbTransaction _inner;
        private readonly DbConnection _connection;

        public MockDbTransaction(DbTransaction inner, DbConnection connection)
        {
            _inner = inner;
            _connection = connection;
        }

        public override void Commit() => _inner.Commit();
        public override void Rollback() => _inner.Rollback();
        protected override DbConnection? DbConnection => _connection;
        public override IsolationLevel IsolationLevel => _inner.IsolationLevel;
    }

    private class MockDbConnection : DbConnection
    {
        private readonly DbConnection _inner;
        private readonly object _returnValue;
        private readonly Exception? _exceptionToThrow;

        public MockDbConnection(DbConnection inner, object returnValue, Exception? exceptionToThrow)
        {
            _inner = inner;
            _returnValue = returnValue;
            _exceptionToThrow = exceptionToThrow;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new MockDbTransaction(_inner.BeginTransaction(isolationLevel), this);
        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public override void Close() => _inner.Close();
        public override void Open() => _inner.Open();
        protected override DbCommand CreateDbCommand() => new MockDbCommand(_inner.CreateCommand(), _returnValue, _exceptionToThrow);
        
        public override string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value; }
        public override string Database => _inner.Database;
        public override ConnectionState State => _inner.State;
        public override string DataSource => _inner.DataSource;
        public override string ServerVersion => _inner.ServerVersion;
    }

    private class DelegatingRelationalConnection : IRelationalConnection
    {
        private readonly IRelationalConnection _mock;
        public DelegatingRelationalConnection(IRelationalConnection mock) => _mock = mock;
        public DbConnection DbConnection { get => _mock.DbConnection; set => _mock.DbConnection = value; }
        public string? ConnectionString { get => _mock.ConnectionString; set => _mock.ConnectionString = value; }
        public Guid ConnectionId => _mock.ConnectionId;
        public int? CommandTimeout { get => _mock.CommandTimeout; set => _mock.CommandTimeout = value; }
        public IDbContextTransaction? CurrentTransaction => _mock.CurrentTransaction;
        public DbContext Context => _mock.Context;
        public IDbContextTransaction BeginTransaction() => _mock.BeginTransaction();
        public IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel) => _mock.BeginTransaction(isolationLevel);
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => _mock.BeginTransactionAsync(cancellationToken);
        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => _mock.BeginTransactionAsync(isolationLevel, cancellationToken);
        public bool Close() => _mock.Close();
        public Task<bool> CloseAsync() => _mock.CloseAsync();
        public void CommitTransaction() => _mock.CommitTransaction();
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => _mock.CommitTransactionAsync(cancellationToken);
        public void Dispose() => _mock.Dispose();
        public ValueTask DisposeAsync() => _mock.DisposeAsync();
        public bool Open(bool errorsExpected = false) => _mock.Open(errorsExpected);
        public Task<bool> OpenAsync(CancellationToken cancellationToken, bool errorsExpected = false) => _mock.OpenAsync(cancellationToken, errorsExpected);
        public void ResetState() => _mock.ResetState();
        public Task ResetStateAsync(CancellationToken cancellationToken = default) => _mock.ResetStateAsync(cancellationToken);
        public void RollbackTransaction() => _mock.RollbackTransaction();
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => _mock.RollbackTransactionAsync(cancellationToken);
        public IDbContextTransaction? UseTransaction(DbTransaction? transaction) => _mock.UseTransaction(transaction);
        public IDbContextTransaction? UseTransaction(DbTransaction? transaction, Guid transactionId) => _mock.UseTransaction(transaction, transactionId);
        public Task<IDbContextTransaction?> UseTransactionAsync(DbTransaction? transaction, CancellationToken cancellationToken = default) => _mock.UseTransactionAsync(transaction, cancellationToken);
        public Task<IDbContextTransaction?> UseTransactionAsync(DbTransaction? transaction, Guid transactionId, CancellationToken cancellationToken = default) => _mock.UseTransactionAsync(transaction, transactionId, cancellationToken);
        public void SetDbConnection(DbConnection? value, bool contextOwnsConnection) => _mock.SetDbConnection(value, contextOwnsConnection);
        public IRelationalCommand RentCommand() => _mock.RentCommand();
        public void ReturnCommand(IRelationalCommand command) => _mock.ReturnCommand(command);
    }
    
    public static IRelationalConnection? CurrentMockRelationalConnection { get; set; }

    private class FakeRelationalConnection : DelegatingRelationalConnection
    {
        public FakeRelationalConnection() : base(CurrentMockRelationalConnection!) { }
    }

    private class FakeNpgsqlDatabaseProvider : Microsoft.EntityFrameworkCore.Storage.IDatabaseProvider
    {
        public string Name => "Npgsql.EntityFrameworkCore.PostgreSQL";
        public bool IsConfigured(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions options) => true;
    }

    private class MockDbContext : DbContext
    {
        public MockDbContext() { }
        public DbSet<TestEntity> Entities { get; set; } = null!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
            optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Storage.IDatabaseProvider, FakeNpgsqlDatabaseProvider>();
        }
    }

    private static IQueryable<TestEntity> CreateMockQueryable(DbContext ctx, int expectedFallbackCount = 0)
    {
        // Add dummy records if we need fallback to return a non-zero count (SQLite real execution)
        if (expectedFallbackCount > 0)
        {
            ctx.Database.EnsureCreated();
            for (int i = 0; i < expectedFallbackCount; i++)
            {
                ctx.Add(new TestEntity { Id = i + 1 });
            }
            ctx.SaveChanges();
        }
        else
        {
            ctx.Database.EnsureCreated();
        }

        return ctx.Set<TestEntity>().AsQueryable();
    }

    [Fact]
    public async Task GetTotalCountAsync_ApproxCountGreaterThanZero_ReturnsApproxCount()
    {
        var ctx = new MockDbContext();
        var mockConnection = new MockDbConnection(ctx.Database.GetDbConnection(), 50, null);
        ctx.Database.GetService<IRelationalConnection>().SetDbConnection(mockConnection, false);

        var mockQueryable = CreateMockQueryable(ctx);
        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        var result = await mockQueryable.ToPagedListAsync(parameters, useApproximateCount: true);
        result.TotalCount.Should().Be(50);
    }
    
    [Fact]
    public async Task GetTotalCountAsync_ApproxCountZero_FallsBackToStandardCount()
    {
        var ctx = new MockDbContext();
        var mockConnection = new MockDbConnection(ctx.Database.GetDbConnection(), 0, null);
        ctx.Database.GetService<IRelationalConnection>().SetDbConnection(mockConnection, false);

        var mockQueryable = CreateMockQueryable(ctx, expectedFallbackCount: 99);
        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        var result = await mockQueryable.ToPagedListAsync(parameters, useApproximateCount: true);
        result.TotalCount.Should().Be(99);
    }
    
    [Fact]
    public async Task GetTotalCountAsync_OverflowException_FallsBackToExactCount()
    {
        var ctx = new MockDbContext();
        var mockConnection = new MockDbConnection(ctx.Database.GetDbConnection(), 0, new OverflowException());
        ctx.Database.GetService<IRelationalConnection>().SetDbConnection(mockConnection, false);

        var mockQueryable = CreateMockQueryable(ctx, expectedFallbackCount: 99);
        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        var result = await mockQueryable.ToPagedListAsync(parameters, useApproximateCount: true);
        result.TotalCount.Should().Be(99);
    }
    
    [Fact]
    public async Task GetTotalCountAsync_InvalidOperationException_FallsBack()
    {
        var ctx = new MockDbContext();
        var mockConnection = new MockDbConnection(ctx.Database.GetDbConnection(), 0, new InvalidOperationException());
        ctx.Database.GetService<IRelationalConnection>().SetDbConnection(mockConnection, false);

        var mockQueryable = CreateMockQueryable(ctx, expectedFallbackCount: 42);
        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        var result = await mockQueryable.ToPagedListAsync(parameters, useApproximateCount: true);
        result.TotalCount.Should().Be(42);
    }
}





