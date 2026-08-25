// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace EricksonLopez.Pagination.Analyzers;

/// <summary>
/// Provides a diagnostic analyzer that warns about the use of insecure default cursor encoders.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CursorSecurityAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Represents the diagnostic identifier for insecure default cursor encoder usages.
    /// </summary>
    public const string DiagnosticId = "PAG006";
    
    private static readonly LocalizableString Title = "Insecure Default Cursor Encoder";
    private static readonly LocalizableString MessageFormat = "Ensure a secure ICursorEncoder (like HmacCursorEncoder) is configured. Base64CursorEncoder is insecure and can allow cursor tampering.";
    private static readonly LocalizableString Description = "By default, cursor pagination uses an insecure Base64 encoder. You must explicitly configure HmacCursorEncoder to prevent cursor tampering.";
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

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }
    // Stryker restore all

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation) return;

        var methodName = invocation.TargetMethod.Name;
        
        // We only care about ToCursorPagedListAsync methods
        if (methodName != "ToCursorPagedListAsync") return;
        
        // Check if the declaring type is from EricksonLopez.Pagination
        var declaringType = invocation.TargetMethod.ContainingType;
        if (declaringType?.ContainingNamespace?.ToDisplayString().StartsWith("EricksonLopez.Pagination") != true) return;

        // Since analyzers run per-file, we cannot definitively know if they configured it in Startup.cs.
        // However, we raise the warning to force them to suppress it explicitly if they have, or to 
        // pass an explicit ICursorEncoder if they haven't. This fulfills SEC-1.
        
        var diagnostic = Diagnostic.Create(Rule, invocation.Syntax.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}


