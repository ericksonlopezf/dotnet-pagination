# Quality Gates & Static Analysis

This repository enforces strict code quality through a combination of static analysis, code coverage, and mutation testing.

## Analyzers Configured

The central `Directory.Build.props` enforces the following rules across all non-test source projects:

1. **Microsoft.CodeAnalysis.NetAnalyzers** (v9.0.0):
   - `AnalysisLevel`: `latest`
   - `AnalysisMode`: `All`
   - `TreatWarningsAsErrors`: `true`

2. **SonarAnalyzer.CSharp** (v9.25.1.91650):
   - Enforces SonarQube best practices natively in the build pipeline.
   - Version pinned via `Directory.Packages.props`.

3. **Custom Roslyn Analyzers** (`EricksonLopez.Pagination.Analyzers`):
   > [!NOTE]
   > Diagnostic code `PAG001` is **reserved** and not currently assigned to any analyzer. The active range is **PAG002–PAG008**.
   - `PAG002`: Warns when offset pagination (`ToPagedListAsync`) is called on an unsorted `IQueryable<T>`.
   - `PAG003`: Warns when `OrderBy` is used before `ToCursorPagedListAsync` (cursor pagination manages its own ordering).
   - `PAG004`: Warns when `OrderBy` is called before `Keyset()` (produces duplicate SQL `ORDER BY` clauses).
   - `PAG005`: Info — recommends installing `EricksonLopez.Pagination.SourceGenerators` when runtime cursor reflection is detected in a Native AOT project.
   - `PAG006`: Warns on every call to `ToCursorPagedListAsync` to verify that `HmacCursorEncoder` is configured in `AddPagination()`. Suppress with `#pragma warning disable PAG006` if already configured.
   - `PAG007`: Warns when a `KeysetBuilder<T>` fluent chain contains more than 5 column registrations.
   - `PAG008`: Warns when `Base64CursorEncoder` is used explicitly — recommends `HmacCursorEncoder` as the secure default (see ADR-0031).

## Warning Suppressions

The following warnings are suppressed in `Directory.Build.props` with documented justification:

### All Projects
| Warning | Reason |
|---------|--------|
| `NU1902`, `NU1903`, `NU1904` | NuGet package version advisory noise |

### Non-Test Projects Only
| Warning | Reason |
|---------|--------|
| `CA1014` | CLS compliance — library is C#-only by design |
| `CA1716` | VB.NET keyword conflicts — irrelevant for C#-only library |
| `CA1000` | Static members on generic types — used intentionally in fluent builder pattern |
| `S1133` | Sonar "remove deprecated code" — `[Obsolete]` attributes include migration guidance and must not be force-removed |

### Test Projects Only
| Warning | Reason |
|---------|--------|
| `CS1591` | Missing XML documentation comments — not required in test code |
| `CA2007` | `ConfigureAwait(false)` — scoped exclusively to test projects |

> [!NOTE]
> Additional per-project suppressions exist in individual `.csproj` files (e.g., `CA1707` for test naming conventions, `IL3050`/`IL2026` for MongoDB AOT warnings). These are scoped to specific projects and do not affect the global quality baseline.

## Code Coverage

* All test projects use **Coverlet** (`coverlet.collector`) for code coverage collection.
* Coverage reports are uploaded to **Codecov** via the `build.yml` workflow using `codecov/codecov-action@v4.0.1`.
* Coverage is collected in **Cobertura** format (configured via `.runsettings` at the repository root).
* **xUnit** (`xunit` + `xunit.runner.visualstudio`) is the test framework.
* **AwesomeAssertions** is used for fluent assertions.
* Additional testing tools (all centrally versioned in `Directory.Packages.props`):
  - **NSubstitute** — mocking
  - **AutoFixture** / **AutoFixture.AutoNSubstitute** — test data generation
  - **FsCheck.Xunit** — property-based testing (invariants for encoding, decoding, filter parsing)
  - **bUnit** — Blazor component testing
  - **Bogus** — realistic fake data generation
  - **Testcontainers** (MsSql, PostgreSql, MongoDb) — integration tests with real databases

### Test Run Settings (`.runsettings`)

The `.runsettings` file at the repository root configures Coverlet:
* **Format**: Cobertura
* **Excluded namespaces** (not counted toward coverage):
  - `System.Text.RegularExpressions.Generated.*`
  - `*.PaginationJsonSerializerContext*`
  - `__OptionValidationGeneratedAttributes`, `__OptionValidationStaticInstances`
  - `*.Grpc.*Message`, `*.Grpc.*Reflection`
* **Excluded file patterns**: `**/*.g.cs`, `**/*.Designer.cs`, `**/obj/**/*.cs`
* **Excluded attributes**: `CompilerGeneratedAttribute`, `GeneratedCodeAttribute`, `ExcludeFromCodeCoverageAttribute`

Always pass `--settings .runsettings` when running tests locally to ensure coverage exclusions match CI:
```bash
dotnet test --no-build -c Release --collect:"XPlat Code Coverage" --settings .runsettings
```

### Coverage Exclusions (from `Directory.Build.props`)
* `[*.SourceGenerators]*` — Source generator assemblies
* `GeneratedCodeAttribute`, `CompilerGeneratedAttribute` — Compiler-generated code
* `**/*.g.cs`, `**/*.Designer.cs` — Generated files

## Mutation Testing

* **Stryker.NET** is configured via `stryker-config.json` (solution: `EricksonLopez.Pagination.slnx`).
* Thresholds (Single Source of Truth in `stryker-config.json`):
  - `high`: **100** (target quality score — ✅ HIGH)
  - `low`: **98** (acceptable score — 🟡 LOW)
  - `break`: **95** (hard quality gate — ❌ FAILED; enforced on `main` and blocks release; deferred from PRs)
  - Scores between 95% and 98% yield 🟠 WARNING, which satisfies the break gate (≥95%) and permits release.
* Concurrency: 4 parallel test runners.
* Mutation testing runs asynchronously on `main` pushes, weekly schedules, and manual dispatch. It does NOT block Pull Requests.
* The `publish.yml` workflow validates the latest mutation score for `main` before publishing.
* Results are tracked via `/StrykerOutput` and GitHub commit statuses (`mutation-testing/stryker`).

> [!NOTE]
> `EricksonLopez.Pagination.Cosmos.Tests` is included in the `stryker-config.json` test-projects list. The `Cosmos` package targets `net8.0;net9.0;net10.0`. Ensure a compatible .NET SDK is available when running Stryker.

### Stryker Exclusions
| Exclusion | Reason |
|-----------|--------|
| `**/CursorPaginationLinqToDBExtensions.cs` | Experimental LINQ to DB package (ADR-0029) — expression translation too complex for mutant validation |
| `**/*.g.cs`, `**/*.Designer.cs`, `**/*Generated*.cs` | Generated files — no source logic to mutate |
| `**/obj/**/*.cs` | Build output directory — compiler-restored copies of source files |
| `**/Microsoft.CodeAnalysis.CodeGen/**/*.cs` | Roslyn compiler-generated files |

> [!NOTE]
> The `mutate` exclusion list is defined in `stryker-config.json`. Additional per-file exclusions can be added using `// Stryker disable` comments with documented justification. Any such suppressions in production code must reference an ADR.

### Ignored Methods
`*ConfigureAwait*`, `Dispose*`, `Clear`, `SuppressFinalize`, `*Log*`

### Running Stryker Locally
```bash
dotnet tool restore
dotnet stryker
```

> [!NOTE]
> `dotnet-stryker` **is** listed in `dotnet-tools.json` at v4.16.0. Run `dotnet tool restore` after cloning to install it as a local tool. No global installation is required.
