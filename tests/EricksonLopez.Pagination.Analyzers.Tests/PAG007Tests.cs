// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public class PAG007Tests
{
    [Fact]
    public async Task KeysetBuilder_With6Columns_ReportsPAG007()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var keyset = query.Keyset(new CursorPaginationParameters())
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .ToCursorPagedListAsync();
    }
}";
        // The analyzer reports on the entire chain expression for maximum visibility.
        // It provides the column count as an argument.
        var expected = AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.Diagnostic("PAG007").WithSpan(11, 22, 18, 38).WithArguments("6");
        await AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task KeysetBuilder_With6DescendingColumns_ReportsPAG007()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var keyset = query.Keyset(new CursorPaginationParameters())
            .Descending(x => x)
            .Descending(x => x)
            .Descending(x => x)
            .Descending(x => x)
            .Descending(x => x)
            .Descending(x => x)
            .ToCursorPagedListAsync();
    }
}";
        var expected = AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.Diagnostic("PAG007").WithSpan(11, 22, 18, 38).WithArguments("6");
        await AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task KeysetBuilder_With5Columns_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var keyset = query.Keyset(new CursorPaginationParameters())
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .ToCursorPagedListAsync();
    }
}";
        await AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task WithoutKeysetCall_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public static class FakeExt {
    public static IQueryable<T> Ascending<T>(this IQueryable<T> q, System.Func<T, object> f) => q;
}

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var keyset = query.Where(x => true)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .Ascending(x => x)
            .ToCursorPagedListAsync();
    }
}";
        await AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task OnlyKeysetCall_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var builder = query.Keyset(new CursorPaginationParameters());
    }
}";
        await AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DirectInvocation_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(Action act)
    {
        act();
    }
}";
        await AnalyzerTestVerifier<KeysetColumnCountAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public void DiagnosticDescriptor_HasExpectedProperties()
    {
        var analyzer = new KeysetColumnCountAnalyzer();
        var rule = analyzer.SupportedDiagnostics[0];
        
        rule.Id.Should().Be("PAG007");
        rule.Title.ToString().Should().Be("KeysetBuilder has too many columns");
        rule.MessageFormat.ToString().Should().Be("KeysetBuilder has {0} column registrations. Keyset pagination with more than 5 columns generates complex SQL predicates that may cause query plan degradation. Consider a composite tie-breaker column instead.");
        rule.Description.ToString().Should().Be("A keyset with N columns produces a WHERE clause of size O(2^N) on databases that do not support row-value syntax (SQL Server, old MySQL). Beyond 5 columns, the generated SQL is optimizer-hostile. Use a single composite tie-breaker (e.g., a UUID primary key as the last column) rather than adding more natural-sort columns. Suppress with #pragma warning disable PAG007 if intentional.");
        rule.Category.Should().Be("Performance");
        rule.DefaultSeverity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        rule.IsEnabledByDefault.Should().BeTrue();
        rule.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-pagination/blob/main/docs/adr/0003-keyset-pagination-priority.md");
    }
}




