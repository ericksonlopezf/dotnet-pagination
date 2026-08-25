// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Pagination;

/// <summary>
/// Provides global diagnostic infrastructure including logging and metric instruments.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class PaginationDiagnostics
{
    /// <summary>
    /// Gets or sets the application-wide logger factory used for pagination logging.
    /// </summary>
    public static ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Gets or sets the application-wide meter factory used for pagination metrics.
    /// </summary>
    public static IMeterFactory? MeterFactory { get; set; }

    private static Meter? _meter;
    private static Counter<long>? _legacyCursorCounter;

    /// <summary>
    /// Gets a counter metric tracking the number of legacy cursor formats accepted.
    /// </summary>
    public static Counter<long> LegacyCursorCounter
    {
        get
        {
            if (_legacyCursorCounter == null)
            {
                _meter ??= MeterFactory?.Create("EricksonLopez.Pagination") ?? new Meter("EricksonLopez.Pagination");
                _legacyCursorCounter = _meter.CreateCounter<long>("pagination.legacy_cursor_accepted", description: "Number of legacy v1 cursors accepted.");
            }
            return _legacyCursorCounter;
        }
    }

    /// <summary>
    /// Creates a typed logger for the specified category type using the configured logger factory.
    /// </summary>
    /// <typeparam name="T">The type defining the logger category.</typeparam>
    /// <returns>A new <see cref="ILogger{TCategoryName}"/> instance, or <see langword="null"/> if no logger factory is configured.</returns>
    public static ILogger<T>? CreateLogger<T>() => LoggerFactory?.CreateLogger<T>();
}


