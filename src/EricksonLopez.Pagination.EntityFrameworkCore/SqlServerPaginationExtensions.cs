// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EricksonLopez.Pagination.EntityFrameworkCore;

/// <summary>
/// Provides SQL Server-specific optimization helpers for pagination.
/// </summary>
public static partial class SqlServerPaginationExtensions
{
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static partial Regex IdentifierRegex();

    /// <summary>
    /// Retrieves the approximate total count of rows in a SQL Server table using partition statistics.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    /// <param name="tableName">The name of the table in SQL Server.</param>
    /// <param name="schemaName">The schema name containing the table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the approximate row count.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> or <paramref name="schemaName"/> is invalid</exception>
    public static async Task<long> GetSqlServerApproximateCountAsync(
        this DbContext dbContext,
        string tableName,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default)
    {
        if (dbContext is null) throw new ArgumentNullException(nameof(dbContext));
        if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Table name is required.", nameof(tableName));

        if (!IdentifierRegex().IsMatch(tableName))
            throw new ArgumentException("Table name contains invalid characters. Only [a-zA-Z_][a-zA-Z0-9_]* identifiers are supported.", nameof(tableName));
        if (!string.IsNullOrEmpty(schemaName) && !IdentifierRegex().IsMatch(schemaName))
            throw new ArgumentException("Schema name contains invalid characters. Only [a-zA-Z_][a-zA-Z0-9_]* identifiers are supported.", nameof(schemaName));

        // Stryker disable all : Raw ADO.NET provider-specific query execution against live server catalogs
        var conn = dbContext.Database.GetDbConnection();
        var wasClosed = conn.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
#if NET8_0_OR_GREATER
            await using var cmd = conn.CreateCommand();
#else
            using var cmd = conn.CreateCommand();
#endif

            var currentTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            if (currentTransaction != null)
            {
                cmd.Transaction = currentTransaction;
            }

            cmd.CommandText = @"
                SELECT CAST(COALESCE(SUM(st.row_count), 0) AS bigint)
                FROM sys.dm_db_partition_stats st
                INNER JOIN sys.tables t ON t.object_id = st.object_id
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE t.name = @tableName 
                  AND s.name = @schemaName
                  AND st.index_id < 2;";

            var pTableName = cmd.CreateParameter();
            pTableName.ParameterName = "@tableName";
            pTableName.Value = tableName;
            cmd.Parameters.Add(pTableName);

            var pSchemaName = cmd.CreateParameter();
            pSchemaName.ParameterName = "@schemaName";
            pSchemaName.Value = schemaName;
            cmd.Parameters.Add(pSchemaName);

            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            var count = result != DBNull.Value && result != null ? Convert.ToInt64(result) : 0;
            return count < 0 ? 0 : count;
        }
        finally
        {
            if (wasClosed)
            {
                await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
        // Stryker restore all
    }

    /// <summary>
    /// Retrieves the approximate total count of rows in a SQL Server table using partition statistics.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    /// <param name="tableName">The name of the table in SQL Server.</param>
    /// <param name="schemaName">The schema name containing the table. Defaults to <c>dbo</c>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the approximate row count.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> or <paramref name="schemaName"/> is invalid</exception>
    public static Task<long> GetApproximateCountAsync(
        DbContext dbContext,
        string tableName,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default)
    {
        return GetSqlServerApproximateCountAsync(dbContext, tableName, schemaName, cancellationToken);
    }
}





