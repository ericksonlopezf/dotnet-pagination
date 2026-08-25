// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public class PAG002Tests
{
    [Fact]
    public async Task IQueryable_WithoutOrderBy_BeforeToPagedListAsync_ReportsPAG002()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToPagedListAsync(new PaginationParameters());
    }
};";
        var expected = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(11, 27, 11, 43);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task IOrderedQueryable_BeforeToPagedListAsync_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IOrderedQueryable<string> query)
    {
        var paged = query.ToPagedListAsync(new PaginationParameters());
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IQueryable_WithVariousOrderMethods_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public static class OrderExtensions {
    public static IQueryable<T> Ascending<T>(this IQueryable<T> q) => q;
    public static IQueryable<T> Descending<T>(this IQueryable<T> q) => q;
}

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged1 = query.OrderByDescending(x => x).ToPagedListAsync(new PaginationParameters());
        var paged2 = query.OrderBy(x => x).ThenBy(x => x).ToPagedListAsync(new PaginationParameters());
        var paged3 = query.OrderBy(x => x).ThenByDescending(x => x).ToPagedListAsync(new PaginationParameters());
        var paged4 = query.Ascending().ToPagedListAsync(new PaginationParameters());
        var paged5 = query.Descending().ToPagedListAsync(new PaginationParameters());
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IQueryable_WithSortByString_BeforeToPagedListAsync_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToPagedListAsync(new PaginationParameters(), ""name asc"");
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IQueryable_WithSortParameters_BeforeToPagedListAsync_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToPagedListAsync(new PaginationParameters(), new SortParameters(""name asc""));
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IQueryable_WithOrderByAssignedToVariable_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        IQueryable<string> ordered = query.OrderBy(x => x);
        var paged = ordered.ToPagedListAsync(new PaginationParameters());
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IEnumerable_WithoutOrderBy_ReportsPAG002()
    {
        var code = @"
using System.Collections.Generic;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IEnumerable<string> query)
    {
        var paged = query.ToPagedListAsync(new PaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(11, 27, 11, 43);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }
    [Fact]
    public async Task ToPagedListDeferredAsync_WithoutOrderBy_ReportsPAG002()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToPagedListDeferredAsync(new PaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(11, 27, 11, 51);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task IOrderedMongoQueryable_BeforeToPagedListAsync_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public interface IOrderedMongoQueryable<T> : IQueryable<T> {}

public class TestClass
{
    public void TestMethod(IOrderedMongoQueryable<string> query)
    {
        var paged = query.ToPagedListAsync(new PaginationParameters());
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IQueryable_WithSortBy_Method_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public static class Extensions {
    public static IQueryable<T> SortBy<T>(this IQueryable<T> q) => q;
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> q) => q;
}

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.SortBy().ToPagedListAsync(new PaginationParameters());
        var paged2 = query.ApplySort().ToPagedListAsync(new PaginationParameters());
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task WithoutLinqUsing_FallbackToSyntax_DoesNotReport()
    {
        var code = @"
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(object query)
    {
        var paged = ((dynamic)query).OrderBy().ToPagedListAsync(new PaginationParameters());
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task EdgeCases_Coverage()
    {
        var code = @"
using System.Collections.Generic;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    // Local method to hit non-MemberAccessExpressionSyntax invocation
    public void ToPagedListAsync() { }

    public void TestMethod()
    {
        ToPagedListAsync(); // Hits line 50
    }
}

public class NonQueryableClass
{
    // Hits line 78 (!isQueryable)
    public void ToPagedListAsync(PaginationParameters p) { }

    public void TestMethod()
    {
        this.ToPagedListAsync(new PaginationParameters());
    }
}

public class UnknownTypeClass
{
    public void TestMethod()
    {
        UnknownType query = null;
        query.ToPagedListAsync(new PaginationParameters()); // Hits line 61 (type == null)
    }
}

public class MyList : List<string>
{
    // Hits line 75 (AllInterfaces fallback since it's not directly IEnumerable<T>)
    public void TestMethod()
    {
        this.ToPagedListAsync(new PaginationParameters());
    }
}
";
        var expected = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(43, 14, 43, 30);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task KeysetBuilder_BeforeToPagedListAsync_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class KeysetBuilder {}

public static class Ext {
    public static void ToPagedListAsync(this KeysetBuilder b, PaginationParameters p) { }
}

public class TestClass
{
    public void TestMethod(KeysetBuilder b)
    {
        b.ToPagedListAsync(new PaginationParameters());
    }
}

public class LocalVarCoverageClass
{
    public IQueryable<string> QueryField;

    public void TestMemberAccess(IQueryable<string> query)
    {
        this.QueryField = query;
        var paged = this.QueryField.OrderBy(x => x).ToPagedListAsync(new PaginationParameters());
        var paged2 = this.QueryField.Where(x => true).ToPagedListAsync(new PaginationParameters());
    }

    public void TestUnorderedLocal(IQueryable<string> query)
    {
        var unordered = query.Where(x => true);
        var paged = unordered.ToPagedListAsync(new PaginationParameters());
    }
}";
        var expected1 = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(29, 55, 29, 71);
        var expected2 = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(35, 31, 35, 47);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected1, expected2);
    }

    [Fact]
    public async Task IQueryable_Where_Then_Ascending_DoesNotReport_And_Where_Only_Reports()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public static class OrderExtensions {
    public static IQueryable<T> Ascending<T>(this IQueryable<T> q) => q;
}

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged1 = query.Where(x => true).Ascending().ToPagedListAsync(new PaginationParameters());
        var paged2 = query.Where(x => true).ToPagedListAsync(new PaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(16, 45, 16, 61);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task IQueryable_WithSortParameters_ExplicitType_DoesNotReport()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        SortParameters sort = new SortParameters(""name"");
        var paged = query.ToPagedListAsync(new PaginationParameters(), sort);
    }
}";
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IQueryable_WithoutOrderBy_BeforeToPagedListDeferredAsync_ReportsPAG002()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class Service
{
    public IQueryable<string> Items { get; set; }
}

public class TestClass
{
    public void TestMethod(Service service)
    {
        var paged = service.Items.ToPagedListAsync(new PaginationParameters());
    }
};";
        var expected = AnalyzerTestVerifier<MissingOrderByAnalyzer>.Diagnostic("PAG002").WithSpan(16, 35, 16, 51);
        await AnalyzerTestVerifier<MissingOrderByAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public void DiagnosticDescriptor_HasExpectedProperties()
    {
        var analyzer = new MissingOrderByAnalyzer();
        var diagnostics = analyzer.SupportedDiagnostics;

        var rule = diagnostics.First(d => d.Id == "PAG002");
        rule.Title.ToString().Should().Be("Missing OrderBy before ToPagedListAsync");
        rule.MessageFormat.ToString().Should().Be("The method 'ToPagedListAsync' requires the IQueryable to be ordered. Ensure 'OrderBy' or 'OrderByDescending' is called before.");
        rule.Category.Should().Be("Usage");
        rule.DefaultSeverity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        rule.IsEnabledByDefault.Should().BeTrue();
        rule.Description.ToString().Should().Be("Pagination methods require an ordered queryable to guarantee deterministic results. Any property used for implicit ordering must exist and have a public getter. If ordered via an external method, suppress with #pragma warning disable PAG002.");

        var cursorRule = diagnostics.First(d => d.Id == "PAG003");
        cursorRule.Title.ToString().Should().Be("Unexpected OrderBy before ToCursorPagedListAsync");
        cursorRule.MessageFormat.ToString().Should().Be("The method 'ToCursorPagedListAsync' applies its own ordering internally. Do not call 'OrderBy' or 'OrderByDescending' before calling it.");
        cursorRule.Category.Should().Be("Usage");
        cursorRule.DefaultSeverity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        cursorRule.IsEnabledByDefault.Should().BeTrue();
        cursorRule.Description.ToString().Should().Be("Cursor pagination manages ordering internally and pre-ordering the queryable may cause SQL translation issues. If intentional, suppress with #pragma warning disable PAG003.");
    }
}





