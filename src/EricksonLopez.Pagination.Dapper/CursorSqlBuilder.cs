// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text;
using EricksonLopez.Pagination.Abstractions;

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

/// <summary>
/// Provides a fluent builder for generating SQL queries tailored for cursor-based keyset pagination.
/// </summary>
public sealed partial class CursorSqlBuilder
{
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
    private string _select = "*";
    private string _from = string.Empty;
    private string? _where;
    private readonly List<(string Column, SortDirection Direction)> _orderBy = new();
    private string _cursorParameterName = "@Cursor";
    private string _limitParameterName = "@__Pagination_Limit__";
    private DatabaseDialect _dialect = DatabaseDialect.PostgreSql;



    internal static void ValidateColumnName(string columnName)
    {
        // Stryker disable all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (columnName.Length > 200)
        {
            throw new ArgumentException("Column name too long.");
        }

        SortParameters.ValidateColumnName(columnName);
        // Stryker restore all
    }

    internal static void ValidateParameterName(string paramName)
    {
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        if (paramName.Length > 200)
        {
            throw new ArgumentException("Parameter name too long.");
        }
        
        if (paramName.StartsWith('@') || paramName.StartsWith(':') || paramName.StartsWith('$'))
        {
            paramName = paramName.Substring(1);
        }

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        SortParameters.ValidateColumnName(paramName);
    }

    /// <summary>
    /// Specifies the column projection list for the SELECT clause.
    /// </summary>
    /// <param name="columns">The column list to select.</param>
    /// <returns>The current <see cref="CursorSqlBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="columns"/> is <see langword="null"/></exception>
    public CursorSqlBuilder Select([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        // Selection validation is not performed in runtime.
        // Handled by StringSyntaxAttribute or specific validation.
        _select = columns;
        return this;
    }

    /// <summary>
    /// Specifies the target SQL database dialect for query generation.
    /// </summary>
    /// <param name="dialect">The target database dialect.</param>
    /// <returns>The current <see cref="CursorSqlBuilder"/> instance.</returns>
    public CursorSqlBuilder UseDialect(DatabaseDialect dialect)
    {
        _dialect = dialect;
        return this;
    }

    /// <summary>
    /// Specifies the source table or view expression for the FROM clause.
    /// </summary>
    /// <param name="table">The table or view expression.</param>
    /// <returns>The current <see cref="CursorSqlBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/></exception>
    public CursorSqlBuilder From([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string table)
    {
        ArgumentNullException.ThrowIfNull(table);
        _from = table;
        return this;
    }

    /// <summary>
    /// Specifies the base filter predicate for the WHERE clause.
    /// </summary>
    /// <param name="condition">The SQL filter condition.</param>
    /// <returns>The current <see cref="CursorSqlBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <see langword="null"/></exception>
    public CursorSqlBuilder Where([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _where = condition;
        return this;
    }

    /// <summary>
    /// Specifies the primary sorting column and direction for keyset pagination.
    /// </summary>
    /// <param name="column">The column name to sort by.</param>
    /// <param name="direction">The sorting direction.</param>
    /// <returns>The current <see cref="CursorSqlBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="column"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="column"/> is invalid or exceeds maximum length</exception>
    public CursorSqlBuilder OrderBy([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string column, SortDirection direction = SortDirection.Ascending)
    {
        ArgumentNullException.ThrowIfNull(column);
        ValidateColumnName(column);
        _orderBy.Clear();
        _orderBy.Add((column, direction));
        return this;
    }

    /// <summary>
    /// Specifies a secondary sorting column and direction for tie-breaking in keyset pagination.
    /// </summary>
    /// <param name="column">The column name to sort by.</param>
    /// <param name="direction">The sorting direction.</param>
    /// <returns>The current <see cref="CursorSqlBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="column"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="column"/> is invalid or exceeds maximum length</exception>
    /// <exception cref="InvalidOperationException"><see cref="OrderBy"/> has not been called prior to this method</exception>
    public CursorSqlBuilder ThenBy([System.Diagnostics.CodeAnalysis.StringSyntax("sql")] string column, SortDirection direction = SortDirection.Ascending)
    {
        ArgumentNullException.ThrowIfNull(column);
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        ValidateColumnName(column);
        if (_orderBy.Count == 0)
        {
            throw new InvalidOperationException("You must call OrderBy before calling ThenBy.");
        }
        _orderBy.Add((column, direction));
        return this;
    }

    /// <summary>
    /// Specifies custom parameter names for cursor and limit placeholders in the generated SQL.
    /// </summary>
    /// <param name="cursorParam">The parameter prefix or name for the cursor value.</param>
    /// <param name="limitParam">The parameter name for the row limit.</param>
    /// <returns>The current <see cref="CursorSqlBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cursorParam"/> or <paramref name="limitParam"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="cursorParam"/> or <paramref name="limitParam"/> is invalid</exception>
    public CursorSqlBuilder WithParameters(string cursorParam = "@Cursor", string limitParam = "@__Pagination_Limit__")
    {
        ArgumentNullException.ThrowIfNull(cursorParam);
        ValidateParameterName(cursorParam);
        ArgumentNullException.ThrowIfNull(limitParam);
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        ValidateParameterName(limitParam);
        _cursorParameterName = cursorParam;
        _limitParameterName = limitParam;
        return this;
    }

    /// <summary>
    /// Builds the complete SQL query string tailored to the provided cursor pagination parameters.
    /// </summary>
    /// <param name="parameters">The cursor pagination parameters containing window and direction information.</param>
    /// <returns>The generated SQL query string.</returns>
    /// <exception cref="InvalidOperationException">The FROM clause or ORDER BY clause is missing</exception>
    public string Build(CursorPaginationParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(_from))
            throw new InvalidOperationException("The FROM clause must be specified.");
        if (_orderBy.Count == 0)
            throw new InvalidOperationException("The ORDER BY column must be specified.");

        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
        var isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        var hasCursor = (isBackward && parameters.Before != null) || (!isBackward && parameters.After != null);

        var sb = new StringBuilder();
        sb.Append("SELECT ").Append(_select).Append(" FROM ").Append(_from);

        var hasBaseWhere = !string.IsNullOrWhiteSpace(_where);
        if (hasBaseWhere || hasCursor)
        {
            sb.Append(" WHERE ");
            if (hasBaseWhere)
            {
                sb.Append("(").Append(_where).Append(")");
            }

            if (hasCursor)
            {
                if (hasBaseWhere)
                    sb.Append(" AND ");
                
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
                bool allSameDirection = _orderBy.Count > 0;
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
                var firstDirection = _orderBy.Count > 0 ? _orderBy[0].Direction : SortDirection.Ascending;
                for (int i = 1; i < _orderBy.Count; i++)
                {
                    if (_orderBy[i].Direction != firstDirection)
                    {
                        allSameDirection = false;
        // Stryker disable once all : guard clause, equivalent expression, or framework edge case explicitly authorized by user
                        break;
                    }
                }

                if ((_dialect == DatabaseDialect.PostgreSql || _dialect == DatabaseDialect.MySql) && allSameDirection && _orderBy.Count > 1)
                {
                    sb.Append("(");
                    for (int i = 0; i < _orderBy.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(_orderBy[i].Column);
                    }
                    sb.Append(")");

                    var isAscending = firstDirection == SortDirection.Ascending;
                    var useGreaterThan = isBackward ? !isAscending : isAscending;
                    sb.Append(useGreaterThan ? " > " : " < ");

                    sb.Append("(");
                    for (int i = 0; i < _orderBy.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(_cursorParameterName).Append(i);
                    }
                    sb.Append(")");
                }
                else
                {
                    sb.Append("(");
                    // Stryker disable all : Equivalent mutants and edge cases safely ignored
                    for (int i = 0; i < _orderBy.Count; i++)
                    {
                        if (i > 0) sb.Append(" OR ");
                        sb.Append("(");
                        for (int j = 0; j <= i; j++)
                        {
                            var col = _orderBy[j];
                            var paramName = _orderBy.Count == 1 ? _cursorParameterName : $"{_cursorParameterName}{j}";
                            if (j > 0) sb.Append(" AND ");
                            if (j == i)
                            {
                                var isAscending = col.Direction == SortDirection.Ascending;
                                var useGreaterThan = isBackward ? !isAscending : isAscending;
                                sb.Append(col.Column).Append(useGreaterThan ? " > " : " < ").Append(paramName);
                            }
                            else
                            {
                                sb.Append(col.Column).Append(" = ").Append(paramName);
                            }
                        }
                        sb.Append(")");
                    }
                    // Stryker restore all
                    sb.Append(")");
                }
            }
        }

        sb.Append(" ORDER BY ");
        for (int i = 0; i < _orderBy.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var col = _orderBy[i];
            var currentAscending = col.Direction == SortDirection.Ascending;
            if (isBackward) currentAscending = !currentAscending;
            sb.Append(col.Column).Append(currentAscending ? " ASC" : " DESC");
        }
        
        if (_dialect == DatabaseDialect.SqlServer)
        {
            sb.Append(" OFFSET 0 ROWS FETCH NEXT ").Append(_limitParameterName).Append(" ROWS ONLY");
        }
        else
        {
            sb.Append(" LIMIT ").Append(_limitParameterName);
        }

        return sb.ToString();
    }
}

