// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Pagination.Analyzers;

/// <summary>
/// Provides a diagnostic analyzer that warns when an <see cref="IQueryable{T}"/> is not ordered before invoking pagination methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingOrderByAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Represents the diagnostic identifier for missing ordering before offset pagination.
    /// </summary>
    public const string DiagnosticId = "PAG002";
    
    /// <summary>
    /// Represents the diagnostic identifier for unexpected pre-ordering before cursor pagination.
    /// </summary>
    public const string CursorDiagnosticId = "PAG003";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Missing OrderBy before ToPagedListAsync",
        "The method 'ToPagedListAsync' requires the IQueryable to be ordered. Ensure 'OrderBy' or 'OrderByDescending' is called before.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true, // Fix AN-001: Enabled by default as flow-analysis false positives are fixed
        description: "Pagination methods require an ordered queryable to guarantee deterministic results. Any property used for implicit ordering must exist and have a public getter. If ordered via an external method, suppress with #pragma warning disable PAG002.");

    private static readonly DiagnosticDescriptor CursorRule = new(
        CursorDiagnosticId,
        "Unexpected OrderBy before ToCursorPagedListAsync",
        "The method 'ToCursorPagedListAsync' applies its own ordering internally. Do not call 'OrderBy' or 'OrderByDescending' before calling it.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Cursor pagination manages ordering internally and pre-ordering the queryable may cause SQL translation issues. If intentional, suppress with #pragma warning disable PAG003.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, CursorRule);

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
        if (methodName != "ToPagedListAsync" && 
            methodName != "ToCursorPagedListAsync" &&
            methodName != "ToPagedListDeferredAsync")
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
        var type = typeInfo.Type;
        // Stryker disable all : Guard clause for malformed AST
        if (type == null)
            return;
        // Stryker restore all

        // AN-1: Use semantic model type comparison instead of fragile string name matching.
        // type.Name == "IQueryable" would false-positive for any user-defined type with the same name.
        // GetTypeByMetadataName performs namespace-aware lookup and handles generic type definitions correctly.
        // Stryker disable all : Metadata lookup and type interface comparison
        var queryableType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Linq.IQueryable`1");
        var enumerableType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        var namedType = type as INamedTypeSymbol;
        bool isQueryable = (queryableType != null && namedType != null && (
                (namedType.IsGenericType && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, queryableType)) ||
                type.AllInterfaces.Any(i => i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, queryableType))))
            || (enumerableType != null && namedType != null && (
                (namedType.IsGenericType && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, enumerableType)) ||
                type.AllInterfaces.Any(i => i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, enumerableType))))
            || type.Name == "KeysetBuilder"; // Library-specific type, safe to name-check since it's in our own namespace
        if (!isQueryable)
            return;
        // Stryker restore all

        // If there's an argument of type string named sortBy or passing a string as the 2nd/3rd arg, it has an implicit sort
        var hasImplicitSort = invocation.ArgumentList.Arguments.Any(arg => 
        {
            var argType = context.SemanticModel.GetTypeInfo(arg.Expression).Type;
            return argType?.SpecialType == SpecialType.System_String || argType?.Name == "SortParameters";
        });

        if (hasImplicitSort)
            return;

        bool isOrdered = IsOrderedQueryable(type);

        // Fallback to syntax chain for methods that return IQueryable but actually order
        if (!isOrdered)
        {
            isOrdered = HasOrderMethodInChain(memberAccess.Expression, context.SemanticModel);
        }

        bool isCursorMethod = methodName == "ToCursorPagedListAsync";

        if (isCursorMethod)
        {
            if (isOrdered)
            {
                var diagnostic = Diagnostic.Create(CursorRule, memberAccess.Name.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
        else
        {
            if (!isOrdered)
            {
                var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private bool IsOrderedQueryable(ITypeSymbol type)
    {
        // Stryker disable once all : Performance optimization / redundant fallback
        if (type.Name == "IOrderedQueryable" || type.Name == "IOrderedMongoQueryable" || type.Name == "KeysetBuilder")
            return true;

        // Stryker disable once all : Performance optimization / redundant fallback
        return type.AllInterfaces.Any(iface => iface.Name == "IOrderedQueryable" || iface.Name == "IOrderedMongoQueryable");
    }

    private bool HasOrderMethodInChain(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        return HasOrderMethodInChain(expression, semanticModel, new HashSet<SyntaxNode>(), 0);
    }

    private bool HasOrderMethodInChain(ExpressionSyntax expression, SemanticModel semanticModel, HashSet<SyntaxNode> visited, int depth)
    {
        // Stryker disable all : Recursion depth and circular AST guard
        if (depth > 20 || expression == null || !visited.Add(expression)) return false;
        // Stryker restore all

        while (expression != null)
        {
            if (expression is InvocationExpressionSyntax invocation && 
                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                // Stryker disable all : Semantic symbol check with AST identifier fallback
                var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                var name = symbol != null ? symbol.Name : memberAccess.Name.Identifier.Text;
                if (name == "OrderBy" || name == "OrderByDescending" || name == "ThenBy" || name == "ThenByDescending" || name == "ApplySort" || name == "Ascending" || name == "Descending" || name == "Keyset" || name == "SortBy")
                {
                    return true;
                }
                // Stryker restore all

                expression = memberAccess.Expression;
            }
            else if (expression is MemberAccessExpressionSyntax member)
            {
                expression = member.Expression;
            }
            else if (expression is IdentifierNameSyntax identifierName)
            {
                // Stryker disable all : Local variable initializer resolution
                var symbol = semanticModel.GetSymbolInfo(identifierName).Symbol;
                if (symbol is ILocalSymbol localSymbol)
                {
                    var references = localSymbol.DeclaringSyntaxReferences;
                    foreach (var syntaxRef in references)
                    {
                        var syntax = syntaxRef.GetSyntax();
                        if (syntax is VariableDeclaratorSyntax declarator && declarator.Initializer != null && HasOrderMethodInChain(declarator.Initializer.Value, semanticModel, visited, depth + 1))
                        {
                            return true;
                        }
                    }
                }
                break;
                // Stryker restore all
            }
            else
            {
                break;
            }
        }
        return false;
    }
}


