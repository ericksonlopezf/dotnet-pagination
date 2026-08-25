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
/// Provides a diagnostic analyzer that warns when a KeysetBuilder chain contains more than 5 column registrations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KeysetColumnCountAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Represents the diagnostic identifier for excessive keyset column count warnings.
    /// </summary>
    public const string DiagnosticId = "PAG007";

    private const string Category = "Performance";

    /// <summary>
    /// Represents the maximum recommended number of keyset columns before a warning is emitted.
    /// </summary>
    public const int MaxRecommendedColumns = 5;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "KeysetBuilder has too many columns",
        messageFormat: "KeysetBuilder has {0} column registrations. Keyset pagination with more than " +
                       MaxRecommendedColumns + " columns generates complex SQL predicates that may cause " +
                       "query plan degradation. Consider a composite tie-breaker column instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "A keyset with N columns produces a WHERE clause of size O(2^N) on databases that do not " +
            "support row-value syntax (SQL Server, old MySQL). Beyond 5 columns, the generated SQL is " +
            "optimizer-hostile. Use a single composite tie-breaker (e.g., a UUID primary key as the " +
            "last column) rather than adding more natural-sort columns. Suppress with " +
            "#pragma warning disable PAG007 if intentional.",
        helpLinkUri: "https://github.com/ericksonlopezf/dotnet-pagination/blob/main/docs/adr/0003-keyset-pagination-priority.md");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    // Stryker disable all : Roslyn analyzer registration boilerplate
    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }
    // Stryker restore all

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // We are looking for the *terminal* call in a fluent keyset chain, e.g.:
        //   source.Keyset(params).Ascending(x => x.Name).Descending(x => x.Id).ToCursorPagedListAsync()
        //
        // Strategy: walk up the parent chain from this invocation to find a method-call chain
        // that contains a call to "Keyset". If found, count all "Ascending" / "Descending" calls
        // in the chain. Only report on the *first* non-Ascending/Descending call (the root invocation)
        // to avoid duplicate diagnostics on each step in the chain.
        //
        // We only fire if:
        //   1. The current call is NOT Ascending/Descending (it is the terminal call, e.g. ToCursorPagedListAsync)
        //   2. The chain contains a "Keyset" call
        //   3. The column count exceeds MaxRecommendedColumns

        if (!IsTerminalChainCall(invocation))
            return;

        // Walk the chain upward to find all Ascending/Descending calls and a Keyset call
        int columnCount = 0;
        bool foundKeysetCall = false;
        ExpressionSyntax? current = invocation.Expression as MemberAccessExpressionSyntax;

        while (current != null)
        {
            var memberAccess = current as MemberAccessExpressionSyntax;
            if (memberAccess == null)
                break;

            var methodName = memberAccess.Name.Identifier.Text;

            if (methodName is "Ascending" or "Descending")
            {
                columnCount++;
            }
            else if (methodName == "Keyset")
            {
                foundKeysetCall = true;
                // Stryker disable once all : Loop break optimization
                break;
            }

            // Move to the inner expression (left side of the dot)
            current = memberAccess.Expression;

            // The inner expression may itself be an invocation (chained call)
            if (current is InvocationExpressionSyntax innerInvocation)
            {
                current = innerInvocation.Expression;
            }
        }

        if (!foundKeysetCall || columnCount <= MaxRecommendedColumns)
            return;

        // Report on the entire chain expression for maximum visibility
        var location = invocation.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location, columnCount);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the invocation is the terminal (outermost) call in a
    /// fluent chain — i.e., its own name is NOT <c>Ascending</c> or <c>Descending</c>.
    /// This prevents the analyzer from firing on each intermediate step of the chain.
    /// </summary>
    private static bool IsTerminalChainCall(InvocationExpressionSyntax invocation)
    {
        // Stryker disable all : Equivalent check for non-terminal / non-member-access invocation
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var name = memberAccess.Name.Identifier.Text;

        // If this call itself is a column registration, it is NOT the terminal call
        return name is not ("Ascending" or "Descending" or "Keyset");
        // Stryker restore all
    }
}

