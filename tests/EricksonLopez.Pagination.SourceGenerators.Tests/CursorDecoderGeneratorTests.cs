// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination.SourceGenerators;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace EricksonLopez.Pagination.SourceGenerators.Tests;

public class CursorDecoderGeneratorTests
{
    // ExpectedEmptyRegistry moved to GeneratorTestSnippets.cs

    private static CSharpCompilation CreateCompilation(string source, string assemblyName = "TestCompilation")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .Concat(new[]
            {
                // We need basic types like string, int, etc.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                // We need to simulate our EricksonLopez.Pagination types if possible, or just define them in source
            });

        return CSharpCompilation.Create(assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string GeneratedSource) RunGenerator(string source, string assemblyName = "TestCompilation")
    {
        var compilation = CreateCompilation(source + GeneratorTestSnippets.DummySource, assemblyName);
        var generator = new CursorDecoderGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        
        var generatedSyntaxTree = outputCompilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("CursorDecoderInitializer.g.cs", StringComparison.Ordinal));

        var compilationErrors = outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (compilationErrors.Count > 0)
        {
            throw new InvalidOperationException("Compilation errors: " + string.Join(", ", compilationErrors.Select(e => e.ToString())));
        }

        return (diagnostics, generatedSyntaxTree?.ToString() ?? string.Empty);
    }

    [Fact]
    public void Generator_WithoutPaginationInvocations_DoesNotGenerateSource()
    {
        var source = GeneratorTestSnippets.Generator_WithoutPaginationInvocations_DoesNotGenerateSource_Source;

        var (diagnostics, generatedSource) = RunGenerator(source);
        
        diagnostics.Should().BeEmpty();
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Generator_WithPrimitiveTypeInvocation_DoesNotGenerateRegistration()
    {
        var source = GeneratorTestSnippets.Generator_WithPrimitiveTypeInvocation_DoesNotGenerateRegistration_Source;

        var (diagnostics, generatedSource) = RunGenerator(source);
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Generator_WithCustomType_GeneratesRegistrationAndTryParse()
    {
        var source = GeneratorTestSnippets.Generator_WithCustomType_GeneratesRegistrationAndTryParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("registry.Register<global::Test.MyCustomType>");
        generatedSource.Should().Contain("global::Test.MyCustomType.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out global::Test.MyCustomType result)");
    }
    
    [Fact]
    public void Generator_WithCustomTypeWithoutTryParse_GeneratesRegistrationWithConvert()
    {
        var source = GeneratorTestSnippets.Generator_WithCustomTypeWithoutTryParse_GeneratesRegistrationWithConvert_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
        diagnostics.Should().ContainSingle();
        diagnostics[0].Id.Should().Be("PAG001");
        diagnostics[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_TargetMethodNamesAndAssembliesAreMatched()
    {
        var source = GeneratorTestSnippets.Generator_TargetMethodNamesAndAssembliesAreMatched_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.Dapper");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("Register<global::Test.DapperType>");
    }

    [Fact]
    public void Generator_DecodeAfterAndBefore_AreMatched()
    {
        var source = GeneratorTestSnippets.Generator_DecodeAfterAndBefore_AreMatched_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("Register<global::Test.AfterType>");
        generatedSource.Should().Contain("Register<global::Test.BeforeType>");
    }

    [Fact]
    public void Generator_WithParseMethod_GeneratesRegistrationWithParse()
    {
        var source = GeneratorTestSnippets.Generator_WithParseMethod_GeneratesRegistrationWithParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("global::Test.TypeWithParse.Parse(s, System.Globalization.CultureInfo.InvariantCulture)");
    }

    [Fact]
    public void Generator_WithIParsable_GeneratesRegistrationWithParse()
    {
        var source = GeneratorTestSnippets.Generator_WithIParsable_GeneratesRegistrationWithParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("global::Test.ParsableType.Parse(s, System.Globalization.CultureInfo.InvariantCulture)");
    }

    [Fact]
    public void Generator_TargetAssembliesAreMatched_MongoDB()
    {
        var source = GeneratorTestSnippets.Generator_TargetAssembliesAreMatched_MongoDB_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.MongoDB");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("Register<global::Test.MongoType>");
    }

    [Fact]
    public void Generator_WithPrimitiveTypeInvocations_DoesNotGenerateRegistration()
    {
        var source = GeneratorTestSnippets.Generator_WithPrimitiveTypeInvocations_DoesNotGenerateRegistration_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
    }
    [Fact]
    public void Generator_WithEnumType_GeneratesRegistrationWithEnumParse()
    {
        var source = GeneratorTestSnippets.Generator_WithEnumType_GeneratesRegistrationWithEnumParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("return System.Enum.Parse<global::Test.MyEnum>(s);");
    }

    [Fact]
    public void Generator_WithParseStringOnly_GeneratesRegistrationWithParseString()
    {
        var source = GeneratorTestSnippets.Generator_WithParseStringOnly_GeneratesRegistrationWithParseString_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("return global::Test.MyParseType.Parse(s);");
    }

    [Fact]
    public void Generator_WithKeysetBuilderAscending_GeneratesRegistration()
    {
        var source = GeneratorTestSnippets.Generator_WithKeysetBuilderAscending_GeneratesRegistration_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("Register<global::Test.AscType>");
    }

    [Fact]
    public void Generator_WithKeysetBuilderDescending_GeneratesRegistration()
    {
        var source = GeneratorTestSnippets.Generator_WithKeysetBuilderDescending_GeneratesRegistration_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("Register<global::Test.DescType>");
    }

    [Fact]
    public void Generator_ToCursorPagedList_IsMatched()
    {
        var source = GeneratorTestSnippets.Generator_ToCursorPagedList_IsMatched_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("Register<global::Test.SyncType>");
    }

    [Fact]
    public void Generator_TargetAssembliesAreMatched_MongoCursor()
    {
        var source = GeneratorTestSnippets.Generator_TargetAssembliesAreMatched_MongoCursor_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.MongoDB");
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("Register<global::Test.MongoCursorType>");
    }

    [Fact]
    public void Generator_WithFourTypeArguments_DoesNotThrow()
    {
        var source = GeneratorTestSnippets.Generator_WithFourTypeArguments_DoesNotThrow_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.MongoDB");
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Generator_WithFakePaginationMethod_DoesNotGenerate()
    {
        var source = GeneratorTestSnippets.Generator_WithFakePaginationMethod_DoesNotGenerate_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.Fake");
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Generator_WithTryParseInvalidSignature_DoesNotGenerateTryParse()
    {
        var source = GeneratorTestSnippets.Generator_WithTryParseInvalidSignature_DoesNotGenerateTryParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Generator_WithParseInvalidSignature_DoesNotGenerateParse()
    {
        var source = GeneratorTestSnippets.Generator_WithParseInvalidSignature_DoesNotGenerateParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        generatedSource.ReplaceLineEndings("\n").Should().Be(GeneratorTestSnippets.ExpectedEmptyRegistry.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Generator_WithISpanParsable_GeneratesParse()
    {
        var source = GeneratorTestSnippets.Generator_WithISpanParsable_GeneratesParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        diagnostics.Should().BeEmpty();
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("global::Test.SpanParsableType.Parse(s, System.Globalization.CultureInfo.InvariantCulture)");
    }

    [Fact]
    public void Generator_WithIParsable_GeneratesParse()
    {
        var source = GeneratorTestSnippets.Generator_WithIParsable_GeneratesParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        diagnostics.Should().BeEmpty();
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("global::Test.ParsableType.Parse(s, System.Globalization.CultureInfo.InvariantCulture)");
    }

    [Fact]
    public void Generator_WithFormatProviderParseOnly_GeneratesParse()
    {
        var source = GeneratorTestSnippets.Generator_WithFormatProviderParseOnly_GeneratesParse_Source;

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        diagnostics.Should().BeEmpty();
        generatedSource.Should().NotBeEmpty();
        generatedSource.Should().Contain("global::Test.FormatProviderParseType.Parse(s, System.Globalization.CultureInfo.InvariantCulture)");
    }
    [Property(MaxTest = 20)]
    public bool Generator_IsDeterministic_ForAnyValidTypeName(NonNull<string> typeNameInput)
    {
        var raw = typeNameInput.Get;
        var sanitized = new string(raw.Where(char.IsLetter).ToArray());
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length > 15)
        {
            sanitized = "GeneratedEntity";
        }
        var validTypeName = "Type" + sanitized;

        var source = $@"
        namespace EricksonLopez.Pagination.EntityFrameworkCore {{
            public static class QueryableExtensions {{
                public static void ToCursorPagedListAsync<T>(this object o) {{}}
            }}
        }}
        namespace TestNamespace
        {{
            using EricksonLopez.Pagination.EntityFrameworkCore;
            public class {validTypeName}
            {{
                public static bool TryParse(string s, System.IFormatProvider? p, out {validTypeName} r)
                {{
                    r = new {validTypeName}();
                    return true;
                }}
            }}
            public class Program
            {{
                public static void Main()
                {{
                    new object().ToCursorPagedListAsync<{validTypeName}>();
                }}
            }}
        }}";

        var (diag1, src1) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        var (diag2, src2) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");

        return diag1.Length == diag2.Length && src1 == src2 && src1.Contains(validTypeName);
    }

    [Property(MaxTest = 20)]
    public bool Generator_EmitsValidDecoders_ForCustomTypesWithTryParse(PositiveInt propertySeed)
    {
        var suffix = propertySeed.Get % 1000;
        var source = $@"
        namespace EricksonLopez.Pagination.EntityFrameworkCore {{
            public static class QueryableExtensions {{
                public static void ToCursorPagedListAsync<T>(this object o) {{}}
            }}
        }}
        namespace TestModel
        {{
            using EricksonLopez.Pagination.EntityFrameworkCore;
            public class CustomId{suffix}
            {{
                public static bool TryParse(string s, System.IFormatProvider? p, out CustomId{suffix} r)
                {{
                    r = new CustomId{suffix}();
                    return true;
                }}
            }}
            public class Program
            {{
                public static void Main()
                {{
                    new object().ToCursorPagedListAsync<CustomId{suffix}>();
                }}
            }}
        }}";

        var (diagnostics, generatedSource) = RunGenerator(source, "EricksonLopez.Pagination.EntityFrameworkCore");
        return diagnostics.IsEmpty && 
               generatedSource.Contains($"registry.Register<global::TestModel.CustomId{suffix}>") && 
               generatedSource.Contains($"global::TestModel.CustomId{suffix}.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out global::TestModel.CustomId{suffix} result)");
    }
}
