// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Pagination.Analyzers;

/// <summary>
/// Provides a diagnostic analyzer that warns when ordering methods are invoked before cursor pagination.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OrderByBeforeCursorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Represents the diagnostic identifier for ordering before cursor pagination.
    /// </summary>
    public const string DiagnosticId = "PAG004";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "OrderBy before Cursor Pagination",
        "Calling '{0}' before '{1}' causes multiple ORDER BY clauses. Use Keyset().Ascending() instead.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Cursor pagination applies its own ordering. Do not use OrderBy on the query before applying cursor pagination.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // Stryker disable all : Roslyn analyzer registration boilerplate
    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }
    // Stryker restore all

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "Keyset" && methodName != "ToCursorPagedListAsync")
            return;

        // Check if there is an OrderBy in the chain BEFORE this
        var hasOrderBy = HasInvalidOrderMethodInChain(memberAccess.Expression, out var invalidMethod);

        if (hasOrderBy)
        {
            var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), invalidMethod, methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool HasInvalidOrderMethodInChain(ExpressionSyntax expression, out string invalidMethod)
    {
        // Stryker disable once all : Default out parameter when returning false
        invalidMethod = "";
        while (expression != null)
        {
            if (expression is InvocationExpressionSyntax invocation && 
                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var name = memberAccess.Name.Identifier.Text;
                if (name == "OrderBy" || name == "OrderByDescending" || name == "ThenBy" || name == "ThenByDescending" || name == "ApplySort")
                {
                    invalidMethod = name;
                    return true;
                }

                expression = memberAccess.Expression;
            }
            else if (expression is MemberAccessExpressionSyntax member)
            {
                expression = member.Expression;
            }
            else
            {
                break;
            }
        }
        return false;
    }
}

