# ADR-0037 — OpenTelemetry Pagination Metrics

## Status
**Accepted** — August 2026

## Context
Production telemetry systems need visibility into pagination patterns:
- Are clients requesting excessive page sizes?
- Are clients performing deep offset scans (>1,000 pages) that degrade database performance?
- Is there a spike in cursor tampering, expiration, or replay attacks?

## Decision
We introduce native .NET `System.Diagnostics.Metrics` instruments under the meter `"EricksonLopez.Pagination"` in `EricksonLopez.Pagination`:

```csharp
namespace EricksonLopez.Pagination;

public static class PaginationMetrics
{
    public const string MeterName = "EricksonLopez.Pagination";

    public static readonly Meter Meter = new(MeterName, "1.2.0");

    public static readonly Counter<long> QueriesTotal;
    public static readonly Histogram<int> PageSize;
    public static readonly Histogram<int> PageDepth;
    public static readonly Counter<long> CursorErrors;
}
```

### Metrics Export
OpenTelemetry collectors and Prometheus scrapers can enable pagination metrics without modifying application logic:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(PaginationMetrics.MeterName)
        .AddPrometheusExporter());
```

## Consequences
- Zero-overhead when no metric listeners are attached.
- First-class observability into query strategies, deep page degradation, and security anomalies.
