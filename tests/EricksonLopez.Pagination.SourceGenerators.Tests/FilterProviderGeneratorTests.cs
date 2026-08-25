// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace EricksonLopez.Pagination.SourceGenerators.Tests;

public class FilterProviderGeneratorTests
{
    private static CSharpCompilation CreateCompilation(string source)
    {
        var normalizedSource = source;
        if (!normalizedSource.Contains("using System;", StringComparison.Ordinal))
        {
            normalizedSource = "using System;\nusing EricksonLopez.Pagination.Abstractions;\n" + normalizedSource;
        }
        var syntaxTree = CSharpSyntaxTree.ParseText(normalizedSource);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IFilterProvider<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateFilterProviderAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(FilterExpressionExtensions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location)
            });

        return CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void Generator_GeneratesFilterProvider_ForDecoratedClass_AllTypes()
    {
        var userSource = @"

namespace TestNamespace;

[EricksonLopez.Pagination.Abstractions.GenerateFilterProviderAttribute]
public class ComprehensiveEntity
{
    public int IntProp { get; set; }
    public long LongProp { get; set; }
    public double DoubleProp { get; set; }
    public decimal DecimalProp { get; set; }
    public string StringProp { get; set; } = string.Empty;
    public bool BoolProp { get; set; }
    public Guid GuidProp { get; set; }
    public DateTime DateTimeProp { get; set; }
    public System.DateTimeOffset DateTimeOffsetProp { get; set; }

    // Ignored members
    public static int StaticProp { get; set; }
    private int PrivateProp { get; set; }
    public int WriteOnlyProp { set { } }
}
";

        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var errors = outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        errors.Should().BeEmpty();

        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().HaveCount(1);

        var generatedSource = runResult.GeneratedTrees[0].ToString();
        generatedSource.Should().Contain("public sealed class ComprehensiveEntityFilterProvider : IFilterProvider<global::TestNamespace.ComprehensiveEntity>");
        generatedSource.Should().Contain("IntProp");
        generatedSource.Should().Contain("LongProp");
        generatedSource.Should().Contain("DoubleProp");
        generatedSource.Should().Contain("DecimalProp");
        generatedSource.Should().Contain("StringProp");
        generatedSource.Should().Contain("BoolProp");
        generatedSource.Should().Contain("GuidProp");
        generatedSource.Should().Contain("DateTimeProp");
        generatedSource.Should().Contain("DateTimeOffsetProp");
        generatedSource.Should().NotContain("StaticProp");
        generatedSource.Should().NotContain("PrivateProp");
        generatedSource.Should().NotContain("WriteOnlyProp");
    }

    [Fact]
    public void Generator_GeneratesFilterProvider_ForGlobalNamespaceClass()
    {
        var userSource = @"

[GenerateFilterProvider]
public class GlobalEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
";

        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var errors = outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        errors.Should().BeEmpty();

        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().HaveCount(1);
        var generatedSource = runResult.GeneratedTrees[0].ToString();
        generatedSource.Should().Contain("namespace EricksonLopez.Pagination.Generated");
    }

    [Fact]
    public void Generator_MultipleClasses_GeneratesDistinctProviders()
    {
        var userSource = @"

namespace TestNamespace;

[GenerateFilterProvider]
public class Order
{
    public int OrderId { get; set; }
}

[GenerateFilterProviderAttribute]
public class Customer
{
    public string Name { get; set; } = string.Empty;
}
";

        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var errors = outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        errors.Should().BeEmpty();

        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().HaveCount(2);
    }

    [Fact]
    public void Generator_NoDecoratedClasses_EmitsNoFiles()
    {
        var userSource = @"
namespace TestNamespace;

public class PlainProduct
{
    public int Id { get; set; }
}
";

        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Generator_OtherAttribute_DoesNotTriggerGeneration()
    {
        var userSource = @"

namespace TestNamespace;

[Serializable]
public class PlainOrder
{
    public int Id { get; set; }
}
";

        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Generator_ShortAttributeName_GeneratesFilterProvider()
    {
        var userSource = @"

namespace TestNamespace;

[GenerateFilterProvider]
public class ShortAttrEntity
{
    public int Id { get; set; }
}
";

        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().HaveCount(1);
    }

    [Fact]
    public void Generator_AttributeSuffix_GeneratesFilterProvider()
    {
        var userSource = @"

namespace TestNamespace;

[GenerateFilterProviderAttribute]
public class AttrSuffixEntity
{
    public string Name { get; set; } = string.Empty;
}
";

        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().HaveCount(1);
    }

    [Fact]
    public void Generator_NestedNamespace_ProducesCorrectHintNameAndNamespace()
    {
        var userSource = @"

namespace My.Deep.Nested.Namespace;

[GenerateFilterProvider]
public class NestedProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";
        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().HaveCount(1);

        var tree = runResult.GeneratedTrees[0];
        tree.FilePath.Should().EndWith("My_Deep_Nested_Namespace_NestedProduct_FilterProvider.g.cs");

        var source = tree.ToString();
        source.Should().Contain("namespace My.Deep.Nested.Namespace.Generated");
        source.Should().Contain("public sealed class NestedProductFilterProvider : IFilterProvider<global::My.Deep.Nested.Namespace.NestedProduct>");
    }

    [Fact]
    public void Generator_GlobalNamespace_ProducesCorrectHintName()
    {
        var userSource = @"

[GenerateFilterProvider]
public class GlobalItem
{
    public int Id { get; set; }
}
";
        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().HaveCount(1);

        var tree = runResult.GeneratedTrees[0];
        tree.FilePath.Should().EndWith("GlobalItem_FilterProvider.g.cs");
    }

    [Fact]
    public void Generator_ClassWithUnrelatedAttribute_GeneratesNothing()
    {
        var userSource = @"

[System.Serializable]
public class UnrelatedClass
{
    public int Id { get; set; }
}
";
        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Generator_ClassWithSameNameAttributeDifferentNamespace_GeneratesNothing()
    {
        var userSource = @"
namespace Custom
{
    public class GenerateFilterProviderAttribute : System.Attribute {}
}

namespace App
{
    using Custom;

    [GenerateFilterProvider]
    public class DifferentNamespaceItem
    {
        public int Id { get; set; }
    }
}
";
        var compilation = CreateCompilation(userSource);
        var generator = new FilterProviderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        diagnostics.Should().BeEmpty();
        var runResult = driver.GetRunResult();
        runResult.GeneratedTrees.Should().BeEmpty();
    }
}


