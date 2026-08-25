// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Internal;

/// <summary>
/// Provides an ActivitySource for OpenTelemetry tracing.
/// </summary>

    internal static class PaginationActivity
{
    /// <summary>
    /// The ActivitySource used for emitting traces.
    /// </summary>
    // Stryker disable once string : Activity source name is an implementation detail for OpenTelemetry
    public static readonly ActivitySource Source = new("EricksonLopez.Pagination");
}

