// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace EricksonLopez.Pagination.Analyzers;

/// <summary>
/// Provides a diagnostic analyzer that warns when <c>Base64CursorEncoder</c> is explicitly instantiated or referenced.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExplicitBase64CursorEncoderAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Represents the diagnostic identifier for explicit Base64 cursor encoder usages.
    /// </summary>
    public const string DiagnosticId = "PAG008";

    private static readonly LocalizableString Title = "Explicit Use of Insecure Base64CursorEncoder";
    private static readonly LocalizableString MessageFormat = "Explicit use of Base64CursorEncoder is insecure and vulnerable to cursor tampering. Use HmacCursorEncoder with a secret key instead.";
    private static readonly LocalizableString Description = "Base64 cursor encoding is vulnerable to tampering and manipulation. Configure HmacCursorEncoder with a secret key for tamper-resistant cursors.";
    private const string Category = "Security";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/ericksonlopezf/dotnet-pagination/blob/main/SECURITY.md#known-security-boundaries");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // Stryker disable all : Roslyn analyzer registration boilerplate
    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeTypeOf, OperationKind.TypeOf);
    }
    // Stryker restore all

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        if (context.Operation is not IObjectCreationOperation objectCreation) return;

        if (IsBase64CursorEncoderType(objectCreation.Type))
        {
            var diagnostic = Diagnostic.Create(Rule, objectCreation.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeTypeOf(OperationAnalysisContext context)
    {
        if (context.Operation is not ITypeOfOperation typeOfOperation) return;

        if (IsBase64CursorEncoderType(typeOfOperation.TypeOperand))
        {
            var diagnostic = Diagnostic.Create(Rule, typeOfOperation.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation) return;

        if (invocation.TargetMethod.TypeArguments.Any(IsBase64CursorEncoderType))
        {
            var diagnostic = Diagnostic.Create(Rule, invocation.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsBase64CursorEncoderType(ITypeSymbol? typeSymbol)
    {
        // Stryker disable once all : Guard clause for malformed AST
        if (typeSymbol == null) return false;

        return typeSymbol.Name == "Base64CursorEncoder" &&
               typeSymbol.ContainingNamespace?.ToDisplayString() == "EricksonLopez.Pagination";
    }
}


