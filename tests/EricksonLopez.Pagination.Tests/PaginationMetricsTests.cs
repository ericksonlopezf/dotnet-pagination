// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

[Collection("PaginationMetricsCollection")]
public class PaginationMetricsTests
{
    private const string SecretKey = "a-very-secret-32-byte-key-for-testing-purposes";

    [Fact]
    public void PaginationMetrics_RecordsOffsetQuery()
    {
        using var listener = new MeterListener();
        long queriesRecorded = 0;
        string? queriesStrategyTag = null;
        int lastPageSize = 0;
        string? pageSizeStrategyTag = null;
        int lastPageDepth = 0;
        string? pageDepthStrategyTag = null;

        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == PaginationMetrics.MeterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "pagination.queries.total")
            {
                queriesRecorded += measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "pagination.strategy" && tag.Value is string s)
                    {
                        queriesStrategyTag = s;
                    }
                }
            }
        });

        listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "pagination.page.size")
            {
                lastPageSize = measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "pagination.strategy" && tag.Value is string s)
                    {
                        pageSizeStrategyTag = s;
                    }
                }
            }
            else if (instrument.Name == "pagination.page.depth")
            {
                lastPageDepth = measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "pagination.strategy" && tag.Value is string s)
                    {
                        pageDepthStrategyTag = s;
                    }
                }
            }
        });

        listener.Start();

        PaginationMetrics.RecordOffsetQuery(page: 5, pageSize: 50);

        queriesRecorded.Should().BeGreaterThanOrEqualTo(1);
        queriesStrategyTag.Should().Be("offset");
        lastPageSize.Should().Be(50);
        pageSizeStrategyTag.Should().Be("offset");
        lastPageDepth.Should().Be(5);
        pageDepthStrategyTag.Should().Be("offset");
    }

    [Fact]
    public void PaginationMetrics_RecordsKeysetQuery()
    {
        using var listener = new MeterListener();
        long queriesRecorded = 0;
        string? queriesStrategyTag = null;
        int lastPageSize = 0;
        string? pageSizeStrategyTag = null;

        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == PaginationMetrics.MeterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "pagination.queries.total")
            {
                queriesRecorded += measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "pagination.strategy" && tag.Value is string s)
                    {
                        queriesStrategyTag = s;
                    }
                }
            }
        });

        listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "pagination.page.size")
            {
                lastPageSize = measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "pagination.strategy" && tag.Value is string s)
                    {
                        pageSizeStrategyTag = s;
                    }
                }
            }
        });

        listener.Start();

        PaginationMetrics.RecordKeysetQuery(pageSize: 100);

        queriesRecorded.Should().BeGreaterThanOrEqualTo(1);
        queriesStrategyTag.Should().Be("keyset");
        lastPageSize.Should().Be(100);
        pageSizeStrategyTag.Should().Be("keyset");
    }

    [Fact]
    public void HmacCursorEncoder_RecordsCursorErrorMetric_OnTamper()
    {
        using var listener = new MeterListener();
        var errorsRecorded = 0L;
        var recordedTypes = new System.Collections.Concurrent.ConcurrentBag<string>();

        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "EricksonLopez.Pagination" && instrument.Name == "pagination.cursor.errors")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "pagination.cursor.errors")
            {
                Interlocked.Add(ref errorsRecorded, measurement);
                foreach (var tag in tags)
                {
                    if (tag.Key == "error.type" && tag.Value is string s)
                    {
                        recordedTypes.Add(s);
                    }
                }
            }
        });

        listener.Start();

        using var encoder = new HmacCursorEncoder(SecretKey);
        var tamperedCursor = Base64CursorEncoder.Default.Encode("N:item_123.forgedSignature123456789012345678901234");

        Action act = () => encoder.Decode(tamperedCursor);
        act.Should().Throw<InvalidPaginationCursorException>();

        errorsRecorded.Should().BeGreaterThanOrEqualTo(1);
        recordedTypes.Should().Contain("tampered");
    }
}

[CollectionDefinition("PaginationMetricsCollection", DisableParallelization = true)]
public class PaginationMetricsCollectionDefinition
{
}





