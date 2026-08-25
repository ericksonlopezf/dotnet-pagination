// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public class PAG004Tests
{
    [Fact]
    public async Task OrderBy_BeforeKeyset_ReportsPAG004()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var keyset = query.OrderBy(x => x).Keyset(new CursorPaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.Diagnostic("PAG004").WithSpan(11, 44, 11, 50).WithArguments("OrderBy", "Keyset");
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task WithoutOrderBy_BeforeKeyset_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var keyset = query.Keyset(new CursorPaginationParameters());
    }
}";
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task OrderBy_BeforeToCursorPagedListAsync_ReportsPAG004()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.OrderBy(x => x).ToCursorPagedListAsync(new CursorPaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.Diagnostic("PAG004").WithSpan(11, 43, 11, 65).WithArguments("OrderBy", "ToCursorPagedListAsync");
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task OrderByDescending_ThenBy_ThenByDescending_ApplySort_BeforeKeyset_ReportsPAG004()
    {
        var code1 = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;
public class TestClass {
    public void TestMethod(IQueryable<string> query) {
        var keyset = query.OrderByDescending(x => x).Keyset(new CursorPaginationParameters());
    }
}";
        var expected1 = AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.Diagnostic("PAG004").WithSpan(8, 54, 8, 60).WithArguments("OrderByDescending", "Keyset");
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code1, expected1);

        var code2 = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;
public class TestClass {
    public void TestMethod(IOrderedQueryable<string> query) {
        var keyset = query.ThenBy(x => x).Keyset(new CursorPaginationParameters());
    }
}";
        var expected2 = AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.Diagnostic("PAG004").WithSpan(8, 43, 8, 49).WithArguments("ThenBy", "Keyset");
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code2, expected2);

        var code3 = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;
public class TestClass {
    public void TestMethod(IOrderedQueryable<string> query) {
        var keyset = query.ThenByDescending(x => x).Keyset(new CursorPaginationParameters());
    }
}";
        var expected3 = AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.Diagnostic("PAG004").WithSpan(8, 53, 8, 59).WithArguments("ThenByDescending", "Keyset");
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code3, expected3);

        var code4 = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;
public static class Ext {
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> q) => q;
}
public class TestClass {
    public void TestMethod(IQueryable<string> query) {
        var keyset = query.ApplySort().Keyset(new CursorPaginationParameters());
    }
}";
        var expected4 = AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.Diagnostic("PAG004").WithSpan(11, 40, 11, 46).WithArguments("ApplySort", "Keyset");
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code4, expected4);
    }
    [Fact]
    public async Task Where_BeforeKeyset_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var keyset = query.Where(x => true).Keyset(new CursorPaginationParameters());
    }
}";
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code);
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
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task PropertyAccess_BeforeKeyset_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public IQueryable<string> MyQuery { get; set; }

    public void TestMethod()
    {
        var keyset = this.MyQuery.Keyset(new CursorPaginationParameters());
    }
}";
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task PropertyAccess_WithOrderBy_BeforeKeyset_ReportsPAG004()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public IQueryable<string> MyQuery { get; set; }

    public void TestMethod()
    {
        var keyset = this.MyQuery.OrderBy(x => x).Keyset(new CursorPaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.Diagnostic("PAG004").WithSpan(13, 51, 13, 57).WithArguments("OrderBy", "Keyset");
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task OrderBy_BeforeToList_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var list = query.OrderBy(x => x).ToList();
    }
}";
        await AnalyzerTestVerifier<OrderByBeforeCursorAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public void DiagnosticDescriptor_HasExpectedProperties()
    {
        var analyzer = new OrderByBeforeCursorAnalyzer();
        var rule = analyzer.SupportedDiagnostics[0];
        
        AwesomeAssertions.AssertionExtensions.Should(rule.Id).Be("PAG004");
        AwesomeAssertions.AssertionExtensions.Should(rule.Title.ToString()).Be("OrderBy before Cursor Pagination");
        AwesomeAssertions.AssertionExtensions.Should(rule.MessageFormat.ToString()).Be("Calling '{0}' before '{1}' causes multiple ORDER BY clauses. Use Keyset().Ascending() instead.");
        AwesomeAssertions.AssertionExtensions.Should(rule.Description.ToString()).Be("Cursor pagination applies its own ordering. Do not use OrderBy on the query before applying cursor pagination.");
        AwesomeAssertions.AssertionExtensions.Should(rule.Category).Be("Usage");
        AwesomeAssertions.AssertionExtensions.Should(rule.DefaultSeverity).Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        AwesomeAssertions.AssertionExtensions.Should(rule.IsEnabledByDefault).BeTrue();
    }
}



