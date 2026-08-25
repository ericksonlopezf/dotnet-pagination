// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EricksonLopez.Pagination.EntityFrameworkCore;

/// <summary>
/// Provides PostgreSQL-specific optimization helpers for pagination.
/// </summary>
public static partial class PostgreSqlPaginationExtensions
{
    
    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static partial System.Text.RegularExpressions.Regex IdentifierRegex();

    // Stryker disable all
    /// <summary>
    /// Retrieves the approximate total count of rows in a PostgreSQL table using catalog statistics.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    /// <param name="tableName">The name of the table in PostgreSQL.</param>
    /// <param name="schemaName">The schema name containing the table.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the approximate row count.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> or <paramref name="schemaName"/> is invalid</exception>
    public static async Task<long> GetApproximateCountAsync(
        this DbContext dbContext,
        string tableName,
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        if (dbContext is null) throw new ArgumentNullException(nameof(dbContext));
        if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Table name is required.", nameof(tableName));
        
        if (!IdentifierRegex().IsMatch(tableName)) throw new ArgumentException("Table name contains invalid characters. Only [a-zA-Z_][a-zA-Z0-9_]* identifiers are supported.", nameof(tableName));
        if (!string.IsNullOrEmpty(schemaName) && !IdentifierRegex().IsMatch(schemaName)) throw new ArgumentException("Schema name contains invalid characters. Only [a-zA-Z_][a-zA-Z0-9_]* identifiers are supported.", nameof(schemaName));

        var conn = dbContext.Database.GetDbConnection();
        var wasClosed = conn.State == System.Data.ConnectionState.Closed;
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
                SELECT reltuples::bigint
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relname = @tableName 
                  AND n.nspname = @schemaName;";
            
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
    }
    // Stryker disable all
    /// <summary>
    /// Builds a parameterized PostgreSQL row-value comparison predicate for multi-column keyset pagination.
    /// </summary>
    /// <param name="columns">The ordered list of column identifiers.</param>
    /// <param name="values">The cursor values to compare against.</param>
    /// <param name="lessThan">A value indicating whether to generate a less-than comparison for backward pagination.</param>
    /// <returns>A tuple containing the generated SQL predicate fragment and dictionary of query parameters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="columns"/> or <paramref name="values"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="columns"/> is empty, lengths mismatch, or column names are invalid</exception>
    public static (string Sql, Dictionary<string, object> Parameters) BuildRowValuePredicate(
        IReadOnlyList<string> columns,
        IReadOnlyList<object> values,
        bool lessThan)
    {
        if (columns is null) throw new ArgumentNullException(nameof(columns));
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (columns.Count == 0) throw new ArgumentException("At least one column is required.", nameof(columns));
        if (columns.Count != values.Count)
            throw new ArgumentException($"Columns ({columns.Count}) and values ({values.Count}) must have the same length.", nameof(values));

        var identifierRegex = IdentifierRegex();
        foreach (var col in columns)
        {
            if (string.IsNullOrWhiteSpace(col) || !identifierRegex.IsMatch(col))
                throw new ArgumentException($"Column name '{col}' contains invalid characters. Only [a-zA-Z0-9_] are allowed.", nameof(columns));
        }

        // EF-5: Double-quote all column names to handle PostgreSQL reserved words (e.g., "order",
        // "where", "select"). The identifier regex already validates that names contain only
        // [a-zA-Z0-9_] so double-quoting cannot introduce SQL injection here.
        var colList = string.Join(", ", columns.Select(c => $"\"{c}\""));
        var paramNames = new string[columns.Count];
        var paramDict = new Dictionary<string, object>(columns.Count);

        for (int i = 0; i < columns.Count; i++)
        {
            var paramName = $"__ksp_{columns[i]}_{i}__";
            paramNames[i] = "@" + paramName;
            paramDict[paramName] = values[i];
        }

        var paramList = string.Join(", ", paramNames);
        var op = lessThan ? "<" : ">";
        var sql = $"({colList}) {op} ({paramList})";

        return (sql, paramDict);
    }
    // Stryker restore all
}






