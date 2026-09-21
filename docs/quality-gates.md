# Quality Gates & Static Analysis

This repository enforces strict code quality through a combination of static analysis, code coverage, and mutation testing.

---

## 1. Analyzers & Static Analysis

Central quality configuration in `Directory.Build.props` enforces compiler warnings as errors across all builds:
- `EnforceCodeStyleInBuild`: `true`
- `AnalysisLevel`: `latest`
- `AnalysisMode`: `All`
- `TreatWarningsAsErrors`: `true`

### Configured Analyzers

1. **Microsoft.CodeAnalysis.NetAnalyzers** (v9.0.0):
   - Included globally across all non-test projects via `Directory.Build.props`.
   - Pinned centrally via `Directory.Packages.props`.

2. **SonarAnalyzer.CSharp** (v9.25.1.91650):
   - Enforces SonarQube best practices natively in the build pipeline.
   - Pinned centrally via `Directory.Packages.props`.

3. **Custom Roslyn Analyzers** (`EricksonLopez.Pagination.Analyzers`):
   > [!NOTE]
   > Diagnostic code `PAG001` is **reserved** and not assigned to any active analyzer rule. The active diagnostic range is **PAG002–PAG008**.
   - `PAG002`: Warns when offset pagination (`ToPagedListAsync`) is invoked on an unsorted `IQueryable<T>`.
   - `PAG003`: Warns when `OrderBy` is used immediately prior to `ToCursorPagedListAsync` (cursor pagination manages its own keyset ordering).
   - `PAG004`: Warns when `OrderBy` is called before `Keyset()` (generates duplicate SQL `ORDER BY` clauses).
   - `PAG005`: Info — recommends installing `EricksonLopez.Pagination.SourceGenerators` when runtime cursor reflection is detected in a Native AOT project.
   - `PAG006`: Warns on calls to `ToCursorPagedListAsync` to ensure `HmacCursorEncoder` is configured in DI.
   - `PAG007`: Warns when a `KeysetBuilder<T>` fluent chain exceeds 5 column registrations.
   - `PAG008`: Warns when `Base64CursorEncoder` is instantiated or registered directly, recommending `HmacCursorEncoder` for tamper resistance (ADR-0031).

---

## 2. Warning Suppressions

All warning suppressions are declared centrally in `Directory.Build.props` with documented rationale:

### Global Suppressions (All Projects)
| Warning | Rationale |
|---|---|
| `NU1901`, `NU1902`, `NU1903`, `NU1904` | NuGet package vulnerability/audit advisory noise for development tools. |
| `NU1603` | Package dependency version resolution warnings. |

### Non-Test Projects Only
| Warning | Rationale |
|---|---|
| `CA1014` | Mark assemblies with `CLSCompliantAttribute` — library is C#-focused by design. |
| `CA1716` | Identifiers should not match keywords (VB.NET keywords) — irrelevant for C#-only ecosystem. |
| `CA1000` | Do not declare static members on generic types — intentionally used in fluent builder patterns (`KeysetBuilder<T>`). |

### Test Projects Only
Test projects suppress additional rules in `Directory.Build.props` that do not apply to unit or integration test suites:
- `CS1591`: Missing XML documentation comments (not required for test methods).
- `CA2007`: Consider calling `ConfigureAwait` on awaited tasks (not required in test runners).
- `CA1707`: Identifiers should not contain underscores (standard naming convention for test methods: `Method_Condition_ExpectedResult`).
- `xUnit1000`, `xUnit1030`: xUnit-specific analyzer nuances in dynamic test generation.

---

## 3. Code Coverage & Testing Stack

* **Collection Engine**: **Coverlet** (`coverlet.collector`) collects cross-platform code coverage during `dotnet test`.
* **CI Integration**: The `dotnet-build-test.yml` workflow collects dual formats (`opencover` and `cobertura`) and uploads reports to **Codecov** via `codecov/codecov-action@v4`.
* **Test Framework**: **xUnit** (`xunit` + `xunit.runner.visualstudio`).
* **Assertion Engine**: **AwesomeAssertions** fluent assertions.
* **Property-Based Testing**: **FsCheck.Xunit** for invariant testing across cursor encoders, token serialization, and filter AST parsing.
* **Mocking & Fixtures**: **NSubstitute**, **AutoFixture**, **AutoFixture.AutoNSubstitute**, and **Bogus**.
* **Integration Testing**: **Testcontainers** (MsSql, PostgreSql, MongoDb) for verifying real database dialect behavior.

### Coverage Exclusions

Configured centrally via `Directory.Build.props` and `.runsettings`:
- **Excluded Namespaces / Classes**:
  - `[*]__OptionValidationGeneratedAttributes`
  - `[*]__OptionValidationStaticInstances`
  - `[*]System.Text.RegularExpressions.Generated*`
  - `[*]EricksonLopez.Pagination.AspNetCore.PaginationJsonSerializerContext*`
  - `[*]EricksonLopez.Pagination.Grpc.*Message`
  - `[*]EricksonLopez.Pagination.Grpc.PaginationReflection`
- **Excluded Attributes**: `GeneratedCodeAttribute`, `CompilerGeneratedAttribute`, `ExcludeFromCodeCoverageAttribute`, `DebuggerNonUserCodeAttribute`.
- **Excluded File Patterns**: `**/*.g.cs`, `**/*.Designer.cs`, `**/obj/**/*.cs`, `**/Microsoft.CodeAnalysis.CodeGen/**/*.cs`.

---

## 4. Mutation Testing

Solution mutation testing is powered by **Stryker.NET** and governed by `stryker-config.json`:

* **Target Project**: `EricksonLopez.Pagination.csproj`
* **Test Project**: `EricksonLopez.Pagination.Tests.csproj`
* **Concurrency**: `2` parallel workers.
* **Reporters**: `html`, `json`, `cleartext`, `progress`.
* **Ignored Methods**: `ConfigureAwait`, `Dispose`.
* **Mutation Targets**:
  - Included: `**/*.cs`
  - Excluded: `!bin/**`, `!obj/**`, `!**/*.g.cs`, `!**/*.AssemblyInfo.cs`

### Quality Gate Thresholds
| Level | Score Threshold | Meaning & Enforcement |
|---|---|---|
| **High** | **100%** | Target quality score (✅ High). |
| **Low** | **98%** | Acceptable production score (🟡 Low). |
| **Break** | **95%** | Hard quality gate (❌ Break). Runs on `main` fail if score < 95.0%. |

### Deferred Release Gate
To prevent multi-hour release delays:
1. Stryker runs asynchronously on `main` merges and weekly cron schedules via `mutation-testing.yml`.
2. Commit status `mutation-testing/stryker` is posted on the commit SHA.
3. During release (`publish.yml`), `scripts/verify-mutation-gate.js` validates that the latest valid run on `main` satisfies the ≥95.0% gate before publishing packages to NuGet.org.
