// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public class PAG005Tests
{
    [Fact]
    public async Task ToCursorPagedListAsync_WithoutSourceGenerator_ReportsPAG005()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToCursorPagedListAsync(new CursorPaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<SourceGeneratorRecommendationAnalyzer>.Diagnostic("PAG005").WithSpan(11, 27, 11, 49);
        await AnalyzerTestVerifier<SourceGeneratorRecommendationAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithSourceGenerator_DoesNotReport()
    {
        var code = @"

namespace EricksonLopez.Pagination.Generated
{
    public static class CursorDecoderServiceCollectionExtensions { }
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToCursorPagedListAsync(new CursorPaginationParameters());
    }
}";
        await AnalyzerTestVerifier<SourceGeneratorRecommendationAnalyzer>.VerifyAnalyzerAsync(code);
    }
    [Fact]
    public async Task ToCursorPagedList_WithoutSourceGenerator_ReportsPAG005()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToCursorPagedList(new CursorPaginationParameters());
    }
}";
        var expected = AnalyzerTestVerifier<SourceGeneratorRecommendationAnalyzer>.Diagnostic("PAG005").WithSpan(11, 27, 11, 44);
        await AnalyzerTestVerifier<SourceGeneratorRecommendationAnalyzer>.VerifyAnalyzerAsync(code, expected);
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
        await AnalyzerTestVerifier<SourceGeneratorRecommendationAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task OtherMethod_DoesNotReport()
    {
        var code = @"

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var list = query.ToList();
    }
}";
        await AnalyzerTestVerifier<SourceGeneratorRecommendationAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public void DiagnosticDescriptor_HasExpectedProperties()
    {
        var analyzer = new SourceGeneratorRecommendationAnalyzer();
        var rule = analyzer.SupportedDiagnostics[0];
        
        AwesomeAssertions.AssertionExtensions.Should(rule.Id).Be("PAG005");
        AwesomeAssertions.AssertionExtensions.Should(rule.Title.ToString()).Be("Consider using EricksonLopez.Pagination.SourceGenerators");
        AwesomeAssertions.AssertionExtensions.Should(rule.MessageFormat.ToString()).Be("Cursor pagination methods rely on reflection if the EricksonLopez.Pagination.SourceGenerators package is not installed. Install the package to enable AOT support and zero-allocation decoders.");
        AwesomeAssertions.AssertionExtensions.Should(rule.Description.ToString()).Be("Source generators generate optimized cursor decoders at compile time, eliminating runtime reflection and making the code compatible with NativeAOT.");
        AwesomeAssertions.AssertionExtensions.Should(rule.Category).Be("Performance");
        AwesomeAssertions.AssertionExtensions.Should(rule.DefaultSeverity).Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Info);
        AwesomeAssertions.AssertionExtensions.Should(rule.IsEnabledByDefault).BeTrue();
    }
}



