// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace EricksonLopez.Pagination.Analyzers.Tests;

public static class AnalyzerTestVerifier<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    private const string PaginationMockSource = """
        namespace EricksonLopez.Pagination
        {
            public struct SortParameters
            {
                public string? Sort { get; set; }
                public SortParameters(string? sort) => Sort = sort;
            }

            public struct PaginationParameters
            {
                public int Page { get; set; }
                public int PageSize { get; set; }
            }

            public struct CursorPaginationParameters
            {
                public int? First { get; set; }
                public string? After { get; set; }
            }

            public class KeysetBuilder
            {
                public KeysetBuilder Ascending<T>(System.Linq.Expressions.Expression<System.Func<string, T>> expr) => this;
                public KeysetBuilder Descending<T>(System.Linq.Expressions.Expression<System.Func<string, T>> expr) => this;
                public System.Threading.Tasks.Task<object> ToCursorPagedListAsync() => System.Threading.Tasks.Task.FromResult<object>(null!);
            }

            public static class PaginationExtensions
            {
                public static System.Threading.Tasks.Task<object> ToPagedListAsync<T>(this System.Linq.IQueryable<T> query, PaginationParameters parameters, string? sort = null) => System.Threading.Tasks.Task.FromResult<object>(null!);
                public static System.Threading.Tasks.Task<object> ToPagedListAsync<T>(this System.Linq.IQueryable<T> query, PaginationParameters parameters, SortParameters sort) => System.Threading.Tasks.Task.FromResult<object>(null!);
                public static System.Threading.Tasks.Task<object> ToPagedListAsync<T>(this System.Collections.Generic.IEnumerable<T> query, PaginationParameters parameters, string? sort = null) => System.Threading.Tasks.Task.FromResult<object>(null!);
                public static System.Threading.Tasks.Task<object> ToPagedListAsync<T>(this System.Collections.Generic.IEnumerable<T> query, PaginationParameters parameters, SortParameters sort) => System.Threading.Tasks.Task.FromResult<object>(null!);
                public static System.Threading.Tasks.Task<object> ToPagedListDeferredAsync<T>(this System.Linq.IQueryable<T> query, PaginationParameters parameters) => System.Threading.Tasks.Task.FromResult<object>(null!);
                public static System.Threading.Tasks.Task<object> ToCursorPagedListAsync<T>(this System.Linq.IQueryable<T> query, CursorPaginationParameters parameters) => System.Threading.Tasks.Task.FromResult<object>(null!);
                public static object ToCursorPagedList<T>(this System.Linq.IQueryable<T> query, CursorPaginationParameters parameters) => null!;
                public static KeysetBuilder Keyset<T>(this System.Linq.IQueryable<T> query, CursorPaginationParameters parameters) => new KeysetBuilder();
            }
        }
        """;

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>.Diagnostic(diagnosticId);

    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            CompilerDiagnostics = CompilerDiagnostics.None
        };
        test.TestState.Sources.Add(PaginationMockSource);
        
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}




