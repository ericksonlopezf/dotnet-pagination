// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Pagination.Dapper;

/// <summary>
/// Specifies the database dialect for SQL generation.
/// </summary>
public enum DatabaseDialect
{
    /// <summary>
    /// Specifies the PostgreSQL database dialect.
    /// </summary>
    PostgreSql,
    /// <summary>
    /// Specifies the MySQL database dialect.
    /// </summary>
    MySql,
    /// <summary>
    /// Specifies the SQLite database dialect.
    /// </summary>
    Sqlite,
    /// <summary>
    /// Specifies the Microsoft SQL Server database dialect.
    /// </summary>
    SqlServer
}
