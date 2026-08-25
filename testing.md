# Testing Strategy and Engineering Quality Guide — EricksonLopez.Pagination

Welcome to the quality engineering and testing guide for **`EricksonLopez.Pagination`**. This document describes the test architecture, design guidelines, mutation testing policies (Stryker.NET), and quality workflows for developers and contributors.

> **Reading Goal**: A new developer should be able to understand the global test strategy and run the complete test suite in **under 30 minutes**.

---

## 1. General Testing Philosophy

Our quality engineering perspective adheres to a strict rule:
$$\text{Pragmatism} > \text{Coverage} > \text{Architectural Purism}$$

1. **Behavior-First**: We test contracts, observable results, invariants, and security boundaries — **not internal implementation details** or private method names.
2. **Zero Private Reflection**: Tests must never rely on `BindingFlags.NonPublic` to invoke business logic. If logic warrants testing, it must be verified through its public API or via `internal` visibility with `InternalsVisibleTo`.
3. **Determinism and Isolation**: Every test must produce the same result regardless of execution order, thread, or machine (local or CI). Zero non-deterministic tests based on unseeded random loops or unpredictable state.

---

## 2. Testing Pyramid

The test suite is structured according to the standard testing pyramid:

```
          / \
         /   \     Performance Tests (BenchmarkDotNet) (~5%)
        /     \    EricksonLopez.Pagination.Benchmarks
       /-------\
      /         \    Integration Tests (Testcontainers / DB) (~10%)
     /           \   EricksonLopez.Pagination.IntegrationTests
    /-------------\
   /               \   Unit Tests & Component Tests (~85%)
  /                 \  EricksonLopez.Pagination.*.Tests
 /-------------------\
```

| Level | Project(s) | Purpose | Technology |
|---|---|---|---|
| **Unit / Component** | `*.Tests` (20 projects) | Algorithm validation (HMAC, filter DSL, keyset expressions, binders, caches, AOT). | xUnit, AwesomeAssertions, NSubstitute |
| **Integration** | `EricksonLopez.Pagination.IntegrationTests` | End-to-end validation with real database engines (PostgreSQL, SQLite, SQL Server, MongoDB). | Testcontainers, Npgsql, SQLite, `SkippableFact` |
| **Performance** | `EricksonLopez.Pagination.Benchmarks` | Memory regressions, throughput, micro-allocations, and zero-allocation guarantees. | BenchmarkDotNet |

---

## 3. Project Organization and Structure

A 1:1 correlation exists between `src/` packages and `tests/` test projects:

```
dotnet-pagination/
├── src/
│   ├── EricksonLopez.Pagination/
│   ├── EricksonLopez.Pagination.Abstractions/
│   ├── EricksonLopez.Pagination.AspNetCore/
│   ├── EricksonLopez.Pagination.EntityFrameworkCore/
│   └── ...
├── tests/
│   ├── EricksonLopez.Pagination.Tests/
│   ├── EricksonLopez.Pagination.Abstractions.Tests/
│   ├── EricksonLopez.Pagination.AspNetCore.Tests/
│   ├── EricksonLopez.Pagination.EntityFrameworkCore.Tests/
│   │   ├── Keyset/
│   │   ├── Queryable/
│   │   ├── Filtering/
│   │   ├── ApproximateCount/
│   │   ├── Dialects/
│   │   └── Infrastructure/
│   ├── EricksonLopez.Pagination.IntegrationTests/
│   └── ...
```

### Test Naming Convention
All test method names must follow the **`Method_Scenario_Result`** pattern (ADR-0038):
- ✅ `Decode_WhenCursorSignatureIsInvalid_ThrowsInvalidPaginationCursorException`
- ✅ `ToPagedListAsync_WithCount_ReturnsCorrectMetadata`
- ✅ `BindModelAsync_BindsDefaultValues_WhenNoProvidersSupplied`
- ❌ `Test1`, `CheckCursor`, `ValidatePagination`

---

## 4. Test Design Patterns

### 4.1 Fakes vs. Mocks (NSubstitute)
- **Use Fakes** (lightweight in-memory implementations) when test double behavior is part of the test semantics:
  - `InMemoryCursorReplayStore` for replay attack validation.
  - `SimpleValueProvider` / `CursorValueProvider` for model binding.
  - `TestDbContext.CreateInMemory()` for EF Core queries.
- **Use NSubstitute** exclusively for external infrastructure interfaces:
  - `ILogger` / `ILoggerFactory`
  - `IOptionsSnapshot<T>`
  - `HttpRequest` / `HttpContext`

### 4.2 Test Data Builders
To avoid brittle tests overburdened with manual constructor initializations, use the fluent builder infrastructure located in `Infrastructure/Builders`:

```csharp
// Fluent entity creation with sensible defaults:
var entity = new TestEntityBuilder()
    .WithId(10)
    .WithName("Product A")
    .WithState(TestState.Active)
    .Build();

// Pagination parameters:
PaginationParameters parameters = new PaginationParametersBuilder()
    .WithPage(2)
    .WithPageSize(25);

CursorPaginationParameters cursorParams = new CursorPaginationParametersBuilder()
    .WithFirst(10)
    .WithAfter(cursorString);
```

### 4.3 Isolation and Parallelization
- **In-Memory SQLite**: Test contexts use isolated connection strings:
  `DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared`, enabling 100% collision-free parallel execution.
- **Mutable Global State**: When a component shares static metrics or caches (`MeterListener`, `PaginationExpressionCache`), the test class must belong to a non-parallel collection:
  ```csharp
  [Collection("PaginationMetricsCollection")]
  public class PaginationMetricsTests { ... }
  ```
- **Optional Infrastructure (Docker)**: Integration tests using Testcontainers use `[SkippableFact]`:
  ```csharp
  [SkippableFact]
  public async Task KeysetBuilder_ForwardPagination_WorksInPostgreSql()
  {
      Skip.If(!_dockerAvailable || _dbContext == null, "Docker is not available.");
      // ...
  }
  ```

---

## 5. Mutation Testing with Stryker.NET (Deferred Quality Gate)

The project applies a **high-standard Mutation Testing policy** to ensure that tests actively validate code behavior against intentional faults (mutants).

### 5.1 CI/CD Quality Gate Architecture
To prevent full Stryker runs (+1 hour) from blocking agile development:
* **Pull Requests (PRs)**: The fast CI workflow (`ci.yml`) executes restore, build, unit tests, coverage, and static analysis in minutes. **It does not run the heavy Stryker suite**.
* **Main (Post-Merge)**: Every push to `main`, weekly scheduled run, or manual dispatch executes the asynchronous and independent `mutation-testing.yml` workflow.
* **Release Gate (`publish.yml`)**: Validates the status of the latest mutation testing score associated with the commit. If the score is **≥ 95%**, release proceeds immediately without re-running Stryker. If it is **< 95%** or no valid run exists, release is blocked.

### 5.2 Official Thresholds (Single Source of Truth in `stryker-config.json`)
* **`high: 100`**: Excellence target (`✅ HIGH`).
* **`low: 98`**: Standard quality threshold (`🟡 LOW` if score < 100% and ≥ 98%).
* **`break: 95`**: Hard gate automatic failure (`❌ FAILED` if score < 95%, `🟠 WARNING` if score ≥ 95% and < 98%). **This threshold must not be lowered under any circumstance.**

### 5.3 Test Co-location Policy
Before creating a separate `*CoverageTests.cs` or `*GapsTests.cs` file, assess whether the scenario naturally belongs to the main feature test file (e.g., `HmacCursorEncoderTests.cs`, `KeysetBuilderTests.cs`, `QueryableExtensionsTests.cs`). Separate files are only permitted when justified by clear semantic partitioning (e.g., adversarial security tests or specific dialect test suites).

### 5.4 Justified Exclusions in `stryker-config.json`

| Exclusion | Type | Technical Justification |
|---|---|---|
| `*ConfigureAwait*` | Method | `ConfigureAwait(false)` is a synchronization optimization not functionally observable in unit tests. |
| `Dispose*` / `SuppressFinalize` | Method | Resource cleanup and GC optimizations that do not alter observable business logic. |
| `*Log*` | Method | Telemetry and diagnostic logging covered via dedicated observability tests (`CursorObservabilityTests`). |
| `!**/CursorPaginationLinqToDBExtensions.cs` | File | Legacy cursor projection extensions, preserved for backward compatibility. Modern LinqToDB keyset pagination is evaluated in `KeysetBuilder.cs`. |
| `!**/*Generated*.cs`, `!**/*.g.cs` | File | Code generated by Roslyn Source Generators. |
| `!**/Microsoft.CodeAnalysis.CodeGen/**/*.cs` | File | Embedded Roslyn compiler helper classes. |

---

## 6. Test Execution Commands

### 6.1 Run the Full Test Suite
```pwsh
# Build and run all test projects
dotnet test EricksonLopez.Pagination.slnx -c Release
```

### 6.2 Run a Specific Project
```pwsh
# Core Tests
dotnet test tests/EricksonLopez.Pagination.Tests/EricksonLopez.Pagination.Tests.csproj

# Entity Framework Core Tests
dotnet test tests/EricksonLopez.Pagination.EntityFrameworkCore.Tests/EricksonLopez.Pagination.EntityFrameworkCore.Tests.csproj

# ASP.NET Core Tests
dotnet test tests/EricksonLopez.Pagination.AspNetCore.Tests/EricksonLopez.Pagination.AspNetCore.Tests.csproj
```

### 6.3 Run with Filter by Name or Category
```pwsh
# Filter by class or method name
dotnet test EricksonLopez.Pagination.slnx --filter "FullyQualifiedName~HmacCursorEncoderTests"

# Run integration tests
dotnet test tests/EricksonLopez.Pagination.IntegrationTests/EricksonLopez.Pagination.IntegrationTests.csproj
```

### 6.4 Run Stryker.NET (Mutation Testing)
```pwsh
# Run Stryker across the full solution
dotnet stryker -s EricksonLopez.Pagination.slnx

# Run Stryker on an individual project (fast mode)
cd tests/EricksonLopez.Pagination.Tests
dotnet stryker
```

---

## 7. Quality Checklist for New Tests

Before submitting a Pull Request, verify:
- [ ] Test method names follow the `Method_Scenario_Result` convention.
- [ ] No private reflection (`BindingFlags.NonPublic`) is used.
- [ ] No non-deterministic random data in assert loops.
- [ ] Metrics and log assertions validate structured tags (`pagination.strategy`, `error.type`, `EventId`).
- [ ] Full solution compiles and passes: `dotnet test EricksonLopez.Pagination.slnx`.
- [ ] Code formatting passes: `dotnet format --verify-no-changes EricksonLopez.Pagination.slnx`.
- [ ] Mutation score meets the CI threshold (≥ 95%).
