// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public class PAG006Tests
{
    [Fact]
    public async Task ToCursorPagedListAsync_Invocation_ReportsPAG006()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace EricksonLopez.Pagination
{
    public static class Extensions 
    {
        public static Task ToCursorPagedListAsync(this IQueryable<string> query) => null;
    }
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToCursorPagedListAsync();
    }
}";
        // The analyzer triggers on any ToCursorPagedListAsync from EricksonLopez.Pagination
        var expected = AnalyzerTestVerifier<CursorSecurityAnalyzer>.Diagnostic("PAG006").WithSpan(19, 21, 19, 51);
        await AnalyzerTestVerifier<CursorSecurityAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_OtherNamespace_DoesNotReport()
    {
        var code = @"

namespace OtherNamespace
{
    public static class Extensions 
    {
        public static Task ToCursorPagedListAsync(this IQueryable<string> query) => null;
    }
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = OtherNamespace.Extensions.ToCursorPagedListAsync(query);
    }
}";
        await AnalyzerTestVerifier<CursorSecurityAnalyzer>.VerifyAnalyzerAsync(code);
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
        await AnalyzerTestVerifier<CursorSecurityAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ToPagedListAsync_Invocation_DoesNotReportPAG006()
    {
        var code = @"
using System.Linq;
using EricksonLopez.Pagination;

namespace EricksonLopez.Pagination
{
    public static class Extensions 
    {
        public static Task ToPagedListAsync(this IQueryable<string> query) => null;
    }
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(IQueryable<string> query)
    {
        var paged = query.ToPagedListAsync();
    }
}";
        await AnalyzerTestVerifier<CursorSecurityAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public void DiagnosticDescriptor_HasExpectedProperties()
    {
        var analyzer = new CursorSecurityAnalyzer();
        var rule = analyzer.SupportedDiagnostics[0];
        
        rule.Id.Should().Be("PAG006");
        rule.Title.ToString().Should().Be("Insecure Default Cursor Encoder");
        rule.MessageFormat.ToString().Should().Be("Ensure a secure ICursorEncoder (like HmacCursorEncoder) is configured. Base64CursorEncoder is insecure and can allow cursor tampering.");
        rule.Description.ToString().Should().Be("By default, cursor pagination uses an insecure Base64 encoder. You must explicitly configure HmacCursorEncoder to prevent cursor tampering.");
        rule.Category.Should().Be("Security");
        rule.DefaultSeverity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        rule.IsEnabledByDefault.Should().BeTrue();
        rule.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-pagination/blob/main/SECURITY.md#known-security-boundaries");
    }
}




