// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace EricksonLopez.Pagination;

// Stryker disable all : OpenTelemetry instrumentation metrics
/// <summary>
/// Provides OpenTelemetry-compatible metrics instruments for pagination operations.
/// </summary>
public static class PaginationMetrics
{
    /// <summary>
    /// Represents the name of the Meter for OpenTelemetry listener subscriptions.
    /// </summary>
    public const string MeterName = "EricksonLopez.Pagination";

    /// <summary>
    /// Represents the library meter instance used for metrics instrumentation.
    /// </summary>
    /// <remarks>
    /// The meter version is intentionally pinned to the API contract version ("1.2.0"), not the NuGet
    /// package release version. This keeps OpenTelemetry listener subscriptions stable across patch
    /// and minor releases that do not break the metrics schema. Update this string only when the
    /// metrics schema (instrument names or units) changes in a breaking way.
    /// </remarks>
    public static readonly Meter Meter = new(MeterName, "1.2.0");

    /// <summary>
    /// Represents the counter tracking total pagination queries executed.
    /// </summary>
    public static readonly Counter<long> QueriesTotal = Meter.CreateCounter<long>(
        "pagination.queries.total",
        description: "Total number of pagination queries executed.");

    /// <summary>
    /// Represents the histogram tracking the distribution of requested page sizes.
    /// </summary>
    public static readonly Histogram<int> PageSize = Meter.CreateHistogram<int>(
        "pagination.page.size",
        unit: "rows",
        description: "Distribution of requested page sizes.");

    /// <summary>
    /// Represents the histogram tracking the distribution of requested offset page depths.
    /// </summary>
    public static readonly Histogram<int> PageDepth = Meter.CreateHistogram<int>(
        "pagination.page.depth",
        description: "Distribution of requested offset page depths.");

    /// <summary>
    /// Represents the counter tracking total cursor validation errors.
    /// </summary>
    public static readonly Counter<long> CursorErrors = Meter.CreateCounter<long>(
        "pagination.cursor.errors",
        description: "Total number of cursor validation errors.");

    /// <summary>
    /// Records a cursor validation error metric.
    /// </summary>
    /// <param name="errorType">The error category indicating the failure reason.</param>
    public static void RecordCursorError(string errorType)
    {
        CursorErrors.Add(1, new KeyValuePair<string, object?>("error.type", errorType));
    }

    /// <summary>
    /// Records an offset pagination query execution metric.
    /// </summary>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    public static void RecordOffsetQuery(int page, int pageSize)
    {
        QueriesTotal.Add(1, new KeyValuePair<string, object?>("pagination.strategy", "offset"));
        PageSize.Record(pageSize, new KeyValuePair<string, object?>("pagination.strategy", "offset"));
        PageDepth.Record(page, new KeyValuePair<string, object?>("pagination.strategy", "offset"));
    }

    /// <summary>
    /// Records a keyset pagination query execution metric.
    /// </summary>
    /// <param name="pageSize">The requested page size.</param>
    public static void RecordKeysetQuery(int pageSize)
    {
        QueriesTotal.Add(1, new KeyValuePair<string, object?>("pagination.strategy", "keyset"));
        PageSize.Record(pageSize, new KeyValuePair<string, object?>("pagination.strategy", "keyset"));
    }
    // Stryker restore all
}




