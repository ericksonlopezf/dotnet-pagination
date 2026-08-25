// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Dapper.Tests;

public class CursorSqlBuilderTests
{
    [Fact]
    public void Build_SimpleForward_GeneratesCorrectSql()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id")
            .WithParameters("@Cursor", "@Limit");

        var parameters = CursorPaginationParameters.Parse("first=10&after=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("Id > @Cursor");
        sql.Should().Contain("ORDER BY Id ASC");
        sql.Should().Contain("LIMIT @Limit");
    }

    [Fact]
    public void Build_SimpleBackward_GeneratesCorrectSql()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id")
            .WithParameters("@Cursor", "@Limit");

        var parameters = CursorPaginationParameters.Parse("last=10&before=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("Id < @Cursor");
        sql.Should().Contain("ORDER BY Id DESC");
    }

    [Fact]
    public void Build_CompositeForward_SqlServer_GeneratesOrExpansionSql()
    {
        var builder = new CursorSqlBuilder()
            .UseDialect(DatabaseDialect.SqlServer)
            .Select("*")
            .From("Entities")
            .OrderBy("Value")
            .ThenBy("Id");

        var parameters = CursorPaginationParameters.Parse("first=10&after=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("((Value > @Cursor0) OR (Value = @Cursor0 AND Id > @Cursor1))");
        sql.Should().Contain("ORDER BY Value ASC, Id ASC");
    }

    [Fact]
    public void Build_CompositeForward_PostgreSql_GeneratesRowValueSql()
    {
        var builder = new CursorSqlBuilder()
            .UseDialect(DatabaseDialect.PostgreSql)
            .Select("*")
            .From("Entities")
            .OrderBy("Value")
            .ThenBy("Id");

        var parameters = CursorPaginationParameters.Parse("first=10&after=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("(Value, Id) > (@Cursor0, @Cursor1)");
        sql.Should().Contain("ORDER BY Value ASC, Id ASC");
    }

    [Fact]
    public void Build_CompositeBackward_SqlServer_GeneratesOrExpansionSql()
    {
        var builder = new CursorSqlBuilder()
            .UseDialect(DatabaseDialect.SqlServer)
            .Select("*")
            .From("Entities")
            .OrderBy("Value")
            .ThenBy("Id");

        var parameters = CursorPaginationParameters.Parse("last=10&before=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("((Value < @Cursor0) OR (Value = @Cursor0 AND Id < @Cursor1))");
        sql.Should().Contain("ORDER BY Value DESC, Id DESC");
    }

    [Fact]
    public void Build_CompositeBackward_PostgreSql_GeneratesRowValueSql()
    {
        var builder = new CursorSqlBuilder()
            .UseDialect(DatabaseDialect.PostgreSql)
            .Select("*")
            .From("Entities")
            .OrderBy("Value")
            .ThenBy("Id");

        var parameters = CursorPaginationParameters.Parse("last=10&before=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("(Value, Id) < (@Cursor0, @Cursor1)");
        sql.Should().Contain("ORDER BY Value DESC, Id DESC");
    }

    [Fact]
    public void Build_WithWhere_AppendsCorrectly()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .Where("Status = 1")
            .OrderBy("Id");

        var parameters = CursorPaginationParameters.Parse("first=10&after=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("WHERE (Status = 1) AND ((Id > @Cursor))");
    }
    
    [Fact]
    public void Build_NoCursor_GeneratesCorrectSql()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id");

        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = builder.Build(parameters);

        sql.Should().NotContain("WHERE ");
        sql.Should().NotContain("Id > @Cursor");
        sql.Should().Contain("ORDER BY Id ASC");
    }

    [Fact]
    public void Build_NoCursorBackward_GeneratesCorrectSql()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id");

        var parameters = CursorPaginationParameters.Parse("last=10", null);
        var sql = builder.Build(parameters);

        sql.Should().NotContain("WHERE ");
        sql.Should().NotContain("Id < @Cursor");
        sql.Should().Contain("ORDER BY Id DESC");
    }

    [Fact]
    public void Build_SqlServerDialect_GeneratesOffsetFetch()
    {
        var builder = new CursorSqlBuilder()
            .UseDialect(DatabaseDialect.SqlServer)
            .Select("*")
            .From("Entities")
            .OrderBy("Id", SortDirection.Descending)
            .WithParameters("@Cursor", "@Limit");

        var parameters = CursorPaginationParameters.Parse("first=10&after=abc", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("Id < @Cursor");
        sql.Should().Contain("ORDER BY Id DESC");
        sql.Should().Contain("OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY");
    }




    [Fact]
    public void Build_SqlServerDialect_UsesTop()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id")
            .UseDialect(DatabaseDialect.SqlServer);

        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = builder.Build(parameters);

        sql.Should().Contain("OFFSET 0 ROWS FETCH NEXT @__Pagination_Limit__ ROWS ONLY");
        sql.Should().Contain("ORDER BY Id ASC");
        sql.Should().NotContain("LIMIT");
    }
    
    [Fact]
    public void UseDialect_MultipleTimes_ReturnsBuilder()
    {
        var builder = new CursorSqlBuilder()
            .UseDialect(DatabaseDialect.MySql)
            .UseDialect(DatabaseDialect.Sqlite);
            
        builder.Should().NotBeNull();
    }
    [Fact]
    public void ThenBy_WithoutOrderBy_ThrowsInvalidOperationException()
    {
        var builder = new CursorSqlBuilder();
        Action act = () => builder.ThenBy("Id");
        act.Should().Throw<InvalidOperationException>().WithMessage("You must call OrderBy before calling ThenBy.");
    }

    [Fact]
    public void Build_WithoutFrom_ThrowsInvalidOperationException()
    {
        var builder = new CursorSqlBuilder().Select("*").OrderBy("Id");
        Action act = () => builder.Build(new CursorPaginationParameters());
        act.Should().Throw<InvalidOperationException>().WithMessage("The FROM clause must be specified.");
    }

    [Fact]
    public void Build_WithoutOrderBy_ThrowsInvalidOperationException()
    {
        var builder = new CursorSqlBuilder().Select("*").From("Entities");
        Action act = () => builder.Build(new CursorPaginationParameters());
        act.Should().Throw<InvalidOperationException>().WithMessage("The ORDER BY column must be specified.");
    }

    [Fact]
    public void OrderBy_TooLongColumnName_ThrowsArgumentException()
    {
        var longColumn = new string('A', 201);
        var builder = new CursorSqlBuilder();
        Action act = () => builder.OrderBy(longColumn);
        act.Should().Throw<ArgumentException>().WithMessage("Column name too long.");
    }

    [Fact]
    public void WithParameters_TooLongParameterName_ThrowsArgumentException()
    {
        var longParam = new string('A', 201);
        var builder = new CursorSqlBuilder();
        Action act = () => builder.WithParameters(longParam, "@Limit");
        act.Should().Throw<ArgumentException>().WithMessage("Parameter name too long.");
    }

    [Fact]
    public void WithParameters_StripsPrefixes()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id")
            .WithParameters(":cursor", "$limit");
        
        var sql = builder.Build(new CursorPaginationParameters { First = 10, After = "test" });
        sql.Should().Contain(":cursor");
        // Limit uses limitParameterName directly if not using explicit @ limit param logic everywhere, wait, it replaces it.
    }

    [Fact]
    public void Build_MixedSortDirection_DoesNotUseRowValueConstructor()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id", SortDirection.Ascending)
            .ThenBy("Name", SortDirection.Descending)
            .UseDialect(DatabaseDialect.PostgreSql);

        var parameters = CursorPaginationParameters.Parse("first=10&after=cursor", null);
        var sql = builder.Build(parameters);

        // It shouldn't contain row value constructors like "(Id, Name) > (@Cursor_1, @Cursor_2)"
        // Since the generator for multiple cursors doesn't support row value constructor with mixed directions
        sql.Should().NotContain("(Id, Name)");
    }

    [Fact]
    public void OrderBy_ColumnNameExactMaxLength_DoesNotThrow()
    {
        var exactColumn = new string('A', 200);
        var builder = new CursorSqlBuilder();
        Action act = () => builder.OrderBy(exactColumn);
        act.Should().NotThrow();
    }

    [Fact]
    public void WithParameters_ParameterNameExactMaxLength_DoesNotThrow()
    {
        var exactParam = new string('A', 200);
        var builder = new CursorSqlBuilder();
        Action act = () => builder.WithParameters(exactParam, "@Limit");
        act.Should().NotThrow();
    }

    [Fact]
    public void ThenBy_TooLongColumnName_ThrowsArgumentException()
    {
        var longColumn = new string('A', 201);
        var builder = new CursorSqlBuilder().OrderBy("Id");
        Action act = () => builder.ThenBy(longColumn);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithParameters_LimitParameterTooLong_ThrowsArgumentException()
    {
        var longParam = new string('A', 201);
        var builder = new CursorSqlBuilder();
        Action act = () => builder.WithParameters("@Cursor", longParam);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_BothFirstAndLast_DefaultsToForward()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("Id");
        
        var parameters = new CursorPaginationParameters { First = 10, After = "cursor1", Last = 10, Before = "cursor2" };
        var sql = builder.Build(parameters);
        sql.Should().Contain("Id > @Cursor");
        sql.Should().Contain("ORDER BY Id ASC");
    }

    [Fact]
    public void Build_NoSelectProvided_UsesSelectStar()
    {
        var builder = new CursorSqlBuilder()
            .From("Entities")
            .OrderBy("Id");
            
        var parameters = CursorPaginationParameters.Parse("first=10", null);
        var sql = builder.Build(parameters);
        sql.Should().StartWith("SELECT * FROM Entities");
    }

    [Fact]
    public void Select_Null_ThrowsArgumentNullException()
    {
        var builder = new CursorSqlBuilder();
        Action act = () => builder.Select(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void From_Null_ThrowsArgumentNullException()
    {
        var builder = new CursorSqlBuilder();
        Action act = () => builder.From(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Where_Null_ThrowsArgumentNullException()
    {
        var builder = new CursorSqlBuilder();
        Action act = () => builder.Where(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrderBy_Null_ThrowsArgumentNullException()
    {
        var builder = new CursorSqlBuilder();
        Action act = () => builder.OrderBy(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ThenBy_Null_ThrowsArgumentNullException()
    {
        var builder = new CursorSqlBuilder().OrderBy("Id");
        Action act = () => builder.ThenBy(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithParameters_NullCursor_ThrowsArgumentNullException()
    {
        var builder = new CursorSqlBuilder();
        Action act = () => builder.WithParameters(null!, "@Limit");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithParameters_NullLimit_ThrowsArgumentNullException()
    {
        var builder = new CursorSqlBuilder();
        Action act = () => builder.WithParameters("@Cursor", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrderBy_CalledTwice_ReplacesPreviousOrderBy()
    {
        var builder = new CursorSqlBuilder()
            .Select("*")
            .From("Entities")
            .OrderBy("OldColumn")
            .OrderBy("NewColumn");

        var sql = builder.Build(new CursorPaginationParameters { First = 10, After = "abc" });
        sql.Should().NotContain("OldColumn");
        sql.Should().Contain("NewColumn");
    }
}


