// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Pagination.Analyzers;

/// <summary>
/// Provides a diagnostic analyzer that recommends installing the source generators package for optimal performance.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SourceGeneratorRecommendationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Represents the diagnostic identifier for source generator installation recommendations.
    /// </summary>
    public const string DiagnosticId = "PAG005";
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Consider using EricksonLopez.Pagination.SourceGenerators",
        "Cursor pagination methods rely on reflection if the EricksonLopez.Pagination.SourceGenerators package is not installed. Install the package to enable AOT support and zero-allocation decoders.",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Source generators generate optimized cursor decoders at compile time, eliminating runtime reflection and making the code compatible with NativeAOT.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // Stryker disable all : Roslyn analyzer registration boilerplate
    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Check if the source generator type exists in the compilation
            var generatedType = compilationContext.Compilation.GetTypeByMetadataName("EricksonLopez.Pagination.Generated.CursorDecoderServiceCollectionExtensions");
            
            // If it exists, the source generator is installed and running
            if (generatedType != null)
                return;

            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        });
    }
    // Stryker restore all

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "ToCursorPagedListAsync" && methodName != "ToCursorPagedList")
            return;

        // If we reach here, a cursor pagination method is called, but the source generator output is missing.
        // We report the diagnostic on the method name identifier.
        context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation()));
    }
}


