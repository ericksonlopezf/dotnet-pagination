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
/// Provides Oracle-specific optimization helpers for pagination.
/// </summary>
public static partial class OraclePaginationExtensions
{
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static partial Regex IdentifierRegex();

    /// <summary>
    /// Retrieves the approximate total count of rows in an Oracle table using database table statistics.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    /// <param name="tableName">The name of the table in Oracle.</param>
    /// <param name="schemaName">An optional schema owner name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the approximate row count.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> or <paramref name="schemaName"/> is invalid</exception>
    public static async Task<long> GetOracleApproximateCountAsync(
        this DbContext dbContext,
        string tableName,
        string? schemaName = null,
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

            if (string.IsNullOrEmpty(schemaName))
            {
                cmd.CommandText = "SELECT NVL(NUM_ROWS, 0) FROM USER_TABLES WHERE TABLE_NAME = UPPER(:tableName)";
                var pTable = cmd.CreateParameter();
                pTable.ParameterName = "tableName";
                pTable.Value = tableName;
                cmd.Parameters.Add(pTable);
            }
            else
            {
                cmd.CommandText = "SELECT NVL(NUM_ROWS, 0) FROM ALL_TABLES WHERE OWNER = UPPER(:schemaName) AND TABLE_NAME = UPPER(:tableName)";
                var pSchema = cmd.CreateParameter();
                pSchema.ParameterName = "schemaName";
                pSchema.Value = schemaName;
                cmd.Parameters.Add(pSchema);

                var pTable = cmd.CreateParameter();
                pTable.ParameterName = "tableName";
                pTable.Value = tableName;
                cmd.Parameters.Add(pTable);
            }

            var currentTx = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            if (currentTx != null)
            {
                cmd.Transaction = currentTx;
            }

            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result == null || result is DBNull) return 0;

            return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (wasClosed)
            {
#if NET8_0_OR_GREATER
                await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
#else
                dbContext.Database.CloseConnection();
#endif
            }
        }
        // Stryker restore all
    }
}





