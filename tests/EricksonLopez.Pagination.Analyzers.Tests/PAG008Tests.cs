// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public class PAG008Tests
{
    [Fact]
    public async Task NewBase64CursorEncoder_ReportsPAG008()
    {
        var code = @"
namespace EricksonLopez.Pagination
{
    public interface ICursorEncoder {}
    public class Base64CursorEncoder : ICursorEncoder {}
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod()
    {
        var encoder = new EricksonLopez.Pagination.Base64CursorEncoder();
    }
}";
        var expected = AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.Diagnostic("PAG008").WithSpan(14, 23, 14, 73);
        await AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task GenericInvocation_WithBase64CursorEncoder_ReportsPAG008()
    {
        var code = @"
namespace EricksonLopez.Pagination
{
    public interface ICursorEncoder {}
    public class Base64CursorEncoder : ICursorEncoder {}
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceExtensions
    {
        public static void AddSingleton<TService, TImplementation>() {}
    }
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod()
    {
        Microsoft.Extensions.DependencyInjection.ServiceExtensions.AddSingleton<EricksonLopez.Pagination.ICursorEncoder, EricksonLopez.Pagination.Base64CursorEncoder>();
    }
}";
        var expected = AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.Diagnostic("PAG008").WithSpan(22, 9, 22, 169);
        await AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task TypeOf_Base64CursorEncoder_ReportsPAG008()
    {
        var code = @"
namespace EricksonLopez.Pagination
{
    public interface ICursorEncoder {}
    public class Base64CursorEncoder : ICursorEncoder {}
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod()
    {
        var t = typeof(EricksonLopez.Pagination.Base64CursorEncoder);
    }
}";
        var expected = AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.Diagnostic("PAG008").WithSpan(14, 17, 14, 69);
        await AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task NewHmacCursorEncoder_DoesNotReportPAG008()
    {
        var code = @"
namespace EricksonLopez.Pagination
{
    public interface ICursorEncoder {}
    public class HmacCursorEncoder : ICursorEncoder 
    {
        public HmacCursorEncoder(string key) {}
    }
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod()
    {
        var encoder = new EricksonLopez.Pagination.HmacCursorEncoder(""secret"");
    }
}";
        await AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task UnrelatedBase64Class_DoesNotReportPAG008()
    {
        var code = @"
namespace OtherCompany.Utils
{
    public class Base64CursorEncoder {}
}

namespace TestNamespace;

public class TestClass
{
    public void TestMethod()
    {
        var encoder = new OtherCompany.Utils.Base64CursorEncoder();
    }
}";
        await AnalyzerTestVerifier<ExplicitBase64CursorEncoderAnalyzer>.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public void DiagnosticDescriptor_HasExpectedProperties()
    {
        var analyzer = new ExplicitBase64CursorEncoderAnalyzer();
        var rule = analyzer.SupportedDiagnostics[0];
        
        AwesomeAssertions.AssertionExtensions.Should(rule.Id).Be("PAG008");
        AwesomeAssertions.AssertionExtensions.Should(rule.Title.ToString()).Be("Explicit Use of Insecure Base64CursorEncoder");
        AwesomeAssertions.AssertionExtensions.Should(rule.MessageFormat.ToString()).Be("Explicit use of Base64CursorEncoder is insecure and vulnerable to cursor tampering. Use HmacCursorEncoder with a secret key instead.");
        AwesomeAssertions.AssertionExtensions.Should(rule.Description.ToString()).Be("Base64 cursor encoding is vulnerable to tampering and manipulation. Configure HmacCursorEncoder with a secret key for tamper-resistant cursors.");
        AwesomeAssertions.AssertionExtensions.Should(rule.Category).Be("Security");
        AwesomeAssertions.AssertionExtensions.Should(rule.DefaultSeverity).Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        AwesomeAssertions.AssertionExtensions.Should(rule.IsEnabledByDefault).BeTrue();
        AwesomeAssertions.AssertionExtensions.Should(rule.HelpLinkUri).Be("https://github.com/ericksonlopezf/dotnet-pagination/blob/main/SECURITY.md#known-security-boundaries");
    }
}



