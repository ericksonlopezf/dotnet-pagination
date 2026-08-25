// Copyright © Erickson Lopez. MIT License.
#pragma warning disable
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class PostgreSqlPaginationExtensionsApproximateCountTests
{
    private sealed class FakeConnection : DbConnection
    {
        public override string ConnectionString { get => string.Empty; set { } }
        public override string Database => string.Empty;
        public override string DataSource => string.Empty;
        public override string ServerVersion => string.Empty;
        public override System.Data.ConnectionState State { get; }

        public object? ScalarResultToReturn { get; set; } = 42L;
        public FakeConnection(System.Data.ConnectionState state = System.Data.ConnectionState.Open)
        {
            State = state;
        }

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override Task CloseAsync() { WasClosedAsyncCalled = true; return Task.CompletedTask; }
        public bool WasClosedAsyncCalled { get; private set; }
        public override void Open() { }
        public override Task OpenAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => null!;
        protected override DbCommand CreateDbCommand() => new FakeCommand { Connection = this };
    }

    private sealed class FakeCommand : DbCommand
    {
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override System.Data.CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override System.Data.UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; } = null!;
        protected override DbParameterCollection DbParameterCollection { get; } = new FakeParameterCollection();
        protected override DbTransaction DbTransaction { get; set; } = null!;
        public object? ScalarResultToReturn => ((FakeConnection)DbConnection).ScalarResultToReturn;

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar()
        {
            var pCollection = (FakeParameterCollection)DbParameterCollection;
            var pTableName = pCollection.Parameters.FirstOrDefault(p => p.ParameterName == "@tableName");
            var pSchemaName = pCollection.Parameters.FirstOrDefault(p => p.ParameterName == "@schemaName");

            if (!CommandText.Contains("SELECT reltuples::bigint", StringComparison.Ordinal)) throw new InvalidOperationException("Bad command");

            return ScalarResultToReturn;
        }
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromResult(ExecuteScalar());
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeParameter();
        protected override DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) => null!;
    }
    
    private sealed class FakeParameter : DbParameter
    {
        public override System.Data.DbType DbType { get; set; }
        public override System.Data.ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = string.Empty;
        public override int Size { get; set; }
        public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; } = null!;
        public override void ResetDbType() { }
    }
    
    private sealed class FakeParameterCollection : DbParameterCollection
    {
        public List<FakeParameter> Parameters = [];
        public override int Count => Parameters.Count;
        public override object SyncRoot => null!;
        public override int Add(object value) { Parameters.Add((FakeParameter)value); return Parameters.Count - 1; }
        public override void AddRange(Array values) { }
        public override void Clear() => Parameters.Clear();
        public override bool Contains(object value) => false;
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) { }
        public override System.Collections.IEnumerator GetEnumerator() => Parameters.GetEnumerator();
        public override int IndexOf(object value) => 0;
        public override int IndexOf(string value) => 0;
        public override void Insert(int index, object value) { }
        public override void Remove(object value) { }
        public override void RemoveAt(int index) { }
        public override void RemoveAt(string parameterName) { }
        protected override DbParameter GetParameter(int index) => Parameters[index];
        protected override DbParameter GetParameter(string parameterName) => Parameters.FirstOrDefault(p => p.ParameterName == parameterName)!;
        protected override void SetParameter(int index, DbParameter value) { }
        protected override void SetParameter(string parameterName, DbParameter value) { }
    }

    private sealed class TestEntity { public int Id { get; set; } }

    private sealed class FakeDbContext : DbContext
    {
        public DbSet<TestEntity> Entities { get; set; } = null!;
        private readonly System.Data.ConnectionState _state;
        public FakeDbContext(System.Data.ConnectionState state = System.Data.ConnectionState.Open)
        {
            _state = state;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(new FakeConnection(_state));
        }
    }

    [Fact]
    public async Task GetApproximateCountAsync_ValidParameters_ReturnsCount()
    {
        using var ctx = new FakeDbContext();
        var count = await PostgreSqlPaginationExtensions.GetApproximateCountAsync(ctx, "TestTable", "TestSchema");
        count.Should().Be(42L);
    }

    [Fact]
    public async Task GetApproximateCountAsync_WhenConnectionClosed_OpensAndClosesConnection()
    {
        using var ctx = new FakeDbContext(System.Data.ConnectionState.Closed);
        var count = await PostgreSqlPaginationExtensions.GetApproximateCountAsync(ctx, "TestTable", "TestSchema");
        count.Should().Be(42L);
    }

    [Fact]
    public async Task GetApproximateCountAsync_WhenResultDbNull_ReturnsZero()
    {
        using var ctx = new FakeDbContext();
        var conn = (FakeConnection)ctx.Database.GetDbConnection();
        conn.ScalarResultToReturn = DBNull.Value;
        var count = await PostgreSqlPaginationExtensions.GetApproximateCountAsync(ctx, "TestTable", "TestSchema");
        count.Should().Be(0L);
    }

    [Fact]
    public async Task GetApproximateCountAsync_WhenResultNull_ReturnsZero()
    {
        using var ctx = new FakeDbContext();
        var conn = (FakeConnection)ctx.Database.GetDbConnection();
        conn.ScalarResultToReturn = null;
        var count = await PostgreSqlPaginationExtensions.GetApproximateCountAsync(ctx, "TestTable", "TestSchema");
        count.Should().Be(0L);
    }

    [Fact]
    public async Task GetApproximateCountAsync_WhenEmptyTableName_ThrowsArgumentException()
    {
        var act = async () =>
        {
            using var ctx = new FakeDbContext();
            await PostgreSqlPaginationExtensions.GetApproximateCountAsync(ctx, string.Empty);
        };
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Table name is required. (Parameter 'tableName')");
    }
}
