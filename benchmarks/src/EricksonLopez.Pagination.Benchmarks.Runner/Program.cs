// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using EricksonLopez.Pagination.Benchmarks.Benchmarks;
using EricksonLopez.Pagination.Benchmarks.Common;
using EricksonLopez.Pagination.Benchmarks.Database;
using EricksonLopez.Pagination.Benchmarks.Libraries;
using EricksonLopez.Pagination.Benchmarks.Seed;

namespace EricksonLopez.Pagination.Benchmarks.Runner;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Pagination Benchmark Runner (Multi-Engine)");
        
        var engines = Enum.GetValues<DatabaseEngine>();
        var targetCount = 1_000_000; // Standard 1M records for full benchmarks

        // 1. Seed All Databases
        foreach (var engine in engines)
        {
            try
            {
                await DatabaseSeeder.EnsureSeededAsync(engine, targetCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to seed {engine}: {ex.Message}");
                Console.WriteLine("Ensure the Docker containers are running.");
                return;
            }
        }

        // 3. Run Benchmarks
        Console.WriteLine("Starting BenchmarkDotNet...");
        _ = BenchmarkRunner.Run<OffsetPaginationBenchmark>();
        _ = BenchmarkRunner.Run<KeysetPaginationBenchmark>();
        _ = BenchmarkRunner.Run<TotalCountPaginationBenchmark>();
        _ = BenchmarkRunner.Run<FilterPaginationBenchmark>();
        _ = BenchmarkRunner.Run<SortPaginationBenchmark>();
        _ = BenchmarkRunner.Run<ProjectionPaginationBenchmark>();

        // 4. Generate Docs
        Console.WriteLine("Aggregating documentation...");
        MarkdownGenerator.Generate();
    }

}



