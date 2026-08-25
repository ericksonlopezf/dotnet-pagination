// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DemoApp.Domain;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DemoApp.Infrastructure;

/// <summary>
/// Result of a single pagination strategy benchmark run.
/// </summary>
public sealed record BenchmarkResult
{
    /// <summary>Strategy identifier, e.g. "offset+count", "offset-no-count", "keyset-forward".</summary>
    public string Strategy { get; init; } = string.Empty;

    /// <summary>Total elapsed milliseconds for the pagination call.</summary>
    public long ElapsedMs { get; init; }

    /// <summary>Number of items returned in the page.</summary>
    public int ItemCount { get; init; }

    /// <summary>Total items in the data set (when available).</summary>
    public int? TotalCount { get; init; }

    /// <summary>Page or cursor info as a human-readable string.</summary>
    public string PageInfo { get; init; } = string.Empty;
}

/// <summary>
/// Benchmark service that compares offset vs keyset pagination strategies side-by-side.
/// </summary>
/// <remarks>
/// <para>
/// This is a <b>wall-clock benchmark</b> for demonstration purposes — not a microbenchmark.
/// It measures real query times against the configured database, giving a realistic picture
/// of performance differences at the chosen page depth. For microbenchmarking (allocations,
/// CPU cycles) use BenchmarkDotNet in a dedicated console project.
/// </para>
/// <para>
/// <b>How to read the results</b>: At shallow pages (page 1-2) offset and keyset perform
/// similarly. At deep pages, keyset time remains constant while offset degrades linearly.
/// The COUNT(*) query in offset+count mode is the most expensive operation on large tables.
/// </para>
/// </remarks>
public sealed class PaginationBenchmarkService
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes the benchmark service with a database context.</summary>
    public PaginationBenchmarkService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Runs all pagination strategies against the Products table and returns timing results.
    /// </summary>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="targetPage">Target page number for offset strategies (deep pages show largest difference).</param>
    /// <param name="cursorId">
    /// The Product.Id to use as the keyset cursor for forward traversal.
    /// Use <see langword="null"/> for the first page.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One <see cref="BenchmarkResult"/> per strategy.</returns>
    public async Task<IReadOnlyList<BenchmarkResult>> RunAsync(
        int pageSize = 20,
        int targetPage = 1,
        int? cursorId = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BenchmarkResult>(4)
        {
            await RunOffsetWithCountAsync(pageSize, targetPage, cancellationToken).ConfigureAwait(false),
            await RunOffsetNoCountAsync(pageSize, targetPage, cancellationToken).ConfigureAwait(false),
            await RunKeysetForwardAsync(pageSize, cancellationToken).ConfigureAwait(false),
            await RunOffsetProjectionAsync(pageSize, targetPage, cancellationToken).ConfigureAwait(false)
        };
        return results;
    }

    private async Task<BenchmarkResult> RunOffsetWithCountAsync(int pageSize, int targetPage, CancellationToken ct)
    {
        var parameters = PaginationParameters.Create(targetPage, pageSize);
        var sw = Stopwatch.StartNew();
        var page = await _context.Products
            .OrderBy(p => p.Id)
            .ToPagedListAsync(parameters, countTotal: true, cancellationToken: ct)
            .ConfigureAwait(false);
        sw.Stop();

        return new BenchmarkResult
        {
            Strategy = "offset+count",
            ElapsedMs = sw.ElapsedMilliseconds,
            ItemCount = page.Count,
            TotalCount = (int?)page.TotalCount,
            PageInfo = $"page={page.Page}/{page.TotalPages}, hasNext={page.HasNextPage}"
        };
    }

    private async Task<BenchmarkResult> RunOffsetNoCountAsync(int pageSize, int targetPage, CancellationToken ct)
    {
        var parameters = PaginationParameters.Create(targetPage, pageSize);
        var sw = Stopwatch.StartNew();
        var page = await _context.Products
            .OrderBy(p => p.Id)
            .ToPagedListAsync(parameters, countTotal: false, cancellationToken: ct)
            .ConfigureAwait(false);
        sw.Stop();

        return new BenchmarkResult
        {
            Strategy = "offset-no-count",
            ElapsedMs = sw.ElapsedMilliseconds,
            ItemCount = page.Count,
            TotalCount = null,
            PageInfo = $"page={page.Page}, hasNext={page.HasNextPage}"
        };
    }

    private async Task<BenchmarkResult> RunKeysetForwardAsync(int pageSize, CancellationToken ct)
    {
        var cursorParams = new CursorPaginationParameters { First = pageSize };
        var sw = Stopwatch.StartNew();
        var page = await _context.Products
            .Keyset(cursorParams)
            .Ascending(p => p.Id)
            .ToCursorPagedListAsync(cancellationToken: ct)
            .ConfigureAwait(false);
        sw.Stop();

        var endCursorDecoded = page.EndCursor is not null
            ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(page.EndCursor))
            : "null";

        return new BenchmarkResult
        {
            Strategy = "keyset-forward",
            ElapsedMs = sw.ElapsedMilliseconds,
            ItemCount = page.Count,
            TotalCount = null,
            PageInfo = $"hasPrev={page.HasPreviousPage}, hasNext={page.HasNextPage}, endCursor(id)={endCursorDecoded}"
        };
    }

    private async Task<BenchmarkResult> RunOffsetProjectionAsync(int pageSize, int targetPage, CancellationToken ct)
    {
        var parameters = PaginationParameters.Create(targetPage, pageSize);
        var sw = Stopwatch.StartNew();
        var page = await _context.Products
            .OrderBy(p => p.Id)
            .ToPagedListAsync(
                p => new { p.Id, p.Name },
                parameters,
                countTotal: true,
                cancellationToken: ct)
            .ConfigureAwait(false);
        sw.Stop();

        return new BenchmarkResult
        {
            Strategy = "offset+count+projection",
            ElapsedMs = sw.ElapsedMilliseconds,
            ItemCount = page.Count,
            TotalCount = (int?)page.TotalCount,
            PageInfo = $"page={page.Page}/{page.TotalPages}, columns=Id+Name only"
        };
    }
}





