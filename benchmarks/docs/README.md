# .NET Pagination Benchmarks

This is a comprehensive, objective, and reproducible benchmark project for pagination libraries in the .NET 10 ecosystem.

## Goal
To compare the performance, memory footprint, and scalability of different pagination libraries across **5 different database engines** against raw Entity Framework Core implementations (both Offset and Keyset).

## Supported Database Engines
- PostgreSQL 16
- SQL Server 2022
- MySQL 8.0
- Oracle (Free)
- SQLite

## Libraries Compared
- **Raw SQL (EF Core Offset)** - Baseline for offset pagination
- **Raw SQL (EF Core Keyset)** - Baseline for keyset pagination
- **X.PagedList**
- **Sieve**
- **Ardalis.Specification**
- **ReFilter**
- **MR.EntityFrameworkCore.KeysetPagination** (Architecture placeholder)
- **OData** (Architecture placeholder - skipped in raw IQueryable benchmarks)
- **AutoQueryable** (Architecture placeholder - skipped in raw IQueryable benchmarks)

## Quick Start

### 1. Start PostgreSQL
```bash
docker-compose up -d
```

### 2. Run Benchmarks
This will automatically create the databases, seed them with 1,000,000 realistic `Product` records each using `Bogus` and high-performance EF Core Batching, validate that all libraries return equivalent data across all engines, and then execute BenchmarkDotNet.
```bash
cd src/EricksonLopez.Pagination.Benchmarks.Runner
dotnet run -c Release
```

### 3. View Results
Results are automatically aggregated and saved as a markdown file:
- [docs/benchmark.md](../docs/benchmark.md)

## Interpreting the Results
- **Operations/sec**: Higher is better.
- **Allocated Memory**: Lower is better.
- **Deep Pagination**: Observe how offset pagination degrades drastically compared to keyset pagination on deeper pages (e.g., Page 10,000).
