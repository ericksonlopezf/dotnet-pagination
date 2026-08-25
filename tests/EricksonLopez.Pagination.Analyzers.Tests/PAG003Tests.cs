// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public class PAG003Tests
{
    [Fact]
    public async Task IOrderedQueryable_BeforeToCursorPagedListAsync_ReportsPAG003()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IOrderedQueryable<string> query)
    {
        var paged = query.ToCursorPagedListAsync(new CursorPaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG003").WithSpan(11, 27, 11, 49);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task IQueryable_BeforeToCursorPagedListAsync_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToCursorPagedListAsync(new CursorPaginationParameters());
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }
}



