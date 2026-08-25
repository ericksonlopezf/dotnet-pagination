# ADR 0005: Exclusion of Dapper from the Benchmark Suite

## Status

Accepted

## Date

2026-08-10

## 1. Context

The benchmark suite of the `dotnet-pagination` project benchmarks and stress-tests various pagination libraries in the .NET ecosystem (such as `XPagedList`, `Sieve`, `Ardalis`, and `MR.EntityFrameworkCore.KeysetPagination`) to measure execution time and memory allocations.

The question arose whether **Dapper** should be included in the benchmark test suite to establish a performance baseline.

## 2. Decision

We have decided **not to include Dapper** or any Micro-ORM in the primary benchmark cycle. Benchmarking remains strictly focused on the Entity Framework Core ecosystem and `IQueryable<T>`-based abstractions.

## 3. Rationale

Including Dapper would distort benchmark comparisons and detract from the suite's purpose due to the following architectural factors:

1. **Apples-to-Oranges Comparison (EF Core vs. Micro-ORMs)**: 
   The libraries evaluated (including `EricksonLopez.Pagination`) are designed to interact with and extend `IQueryable<T>`. These tools naturally operate within the EF Core pipeline:
   - Expression Tree compilation.
   - Translation of expressions into SQL statements.
   - Result materialization through the EF Core mapping infrastructure.
   
   Dapper, by contrast, bypasses these phases and executes raw SQL directly against an `IDbConnection`. Comparing a LINQ-translating pagination library against raw SQL strings would merely measure the absence of EF Core, rather than the efficiency of the pagination algorithm itself.

2. **Interface Contract Differences (`IPaginationProvider`)**:
   The benchmark test harness requires competitors to implement the contract `Task<List<Product>> ExecuteOffsetPaginationAsync(IQueryable<Product> query, PaginationParameters parameters)`.
   Integrating Dapper would require dual-path harness refactoring to inject `IDbConnection` instances alongside `IQueryable`, breaking input uniformity.

3. **Multi-Engine Maintenance Overhead**:
   The benchmark executes across 5 different database engines (PostgreSQL, SQL Server, MySQL, Oracle, SQLite). Supporting Dapper would require maintaining, optimizing, and verifying 5 distinct raw SQL dialect templates (e.g., `LIMIT/OFFSET`, `OFFSET/FETCH`, `ROWNUM`), which exceeds the project's benchmarking scope.

## 4. Consequences

- **Positive**:
  - Keeps the benchmark suite focused on the core engineering problem: optimizing EF Core pagination and reducing the need to abandon EF Core for Micro-ORMs due to pagination performance concerns.
  - Avoids exponential maintenance complexity across multi-database raw SQL strings.

- **Negative**:
  - The theoretical bare-metal baseline is represented via EF Core's `RawSqlOffsetProvider` (which incurs minimal EF interception overhead) rather than pure unabstracted ADO.NET/Dapper.
