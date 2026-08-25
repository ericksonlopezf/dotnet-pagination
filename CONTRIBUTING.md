# Contributing to EricksonLopez.Pagination

Thank you for considering contributing to `EricksonLopez.Pagination`! This document is the comprehensive guide for contributing to this project. We aim to keep this library enterprise-grade, highly performant, and secure.

## 1. Code of Conduct

By participating in this project, you are expected to uphold our [Code of Conduct](CODE_OF_CONDUCT.md). Please report unacceptable behavior to the repository administrators via a private GitHub security advisory (see [SECURITY.md](SECURITY.md)).

## 2. Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later. The project targets `net8.0`, `net9.0`, and `net10.0` across most packages.
- Visual Studio 2022 (17.12+), JetBrains Rider, or VS Code with the C# Dev Kit.
- Optional: Docker (required to run integration tests against PostgreSQL / MongoDB / SQL Server locally via Testcontainers).

> **Note on local tools**: `dotnet-tools.json` registers `dotnet-stryker` (v4.16.0). Run `dotnet tool restore` after cloning to install it locally. The tool is available as `dotnet stryker` without a global installation.

### Setting up your local environment

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/your-username/dotnet-pagination.git
   cd dotnet-pagination
   ```
3. **Restore local tools**:
   ```bash
   dotnet tool restore
   ```
4. **Restore dependencies and build**:
   ```bash
   dotnet restore
   dotnet build --no-restore -c Release
   ```
5. **Run tests** to ensure everything is working:
   ```bash
   dotnet test --no-build -c Release --verbosity normal --collect:"XPlat Code Coverage" --settings .runsettings
   ```

## 3. Contribution Workflow

### Finding an Issue

Look for issues tagged with `good first issue` or `help wanted`. If you want to work on something that is not listed, open a new issue first to discuss it with the maintainers. This ensures your time is well spent.

### Branching Strategy

We follow **GitHub Flow**:
- `main` is the primary branch and must always be in a deployable state.
- Create feature branches from `main`: `feature/your-feature-name` or `bugfix/issue-id`.

### Commit Convention

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification (e.g., `feat: add new API`, `fix: resolve offset issue`, `docs: update readme`). The automated release pipeline (`publish.yml`) uses conventional commit prefixes to determine version bumps.

### Writing Code

Our goal is zero-allocation (where possible), Native AOT compatibility, and absolute security against injection or data leakage.
- **Performance:** Avoid LINQ allocations in hot paths (`CursorDecoder`, `KeysetBuilder`). Use `Span<T>`, `ReadOnlyMemory<T>`, and `stackalloc` where appropriate.
- **AOT Compatibility:** Avoid `System.Reflection.Emit`. Ensure all dynamic code uses Source Generators or provides strict `[DynamicDependency]` attributes.
- **Security:** Do not suppress static analysis warnings like `CA2007` in production code. Never use `Stryker disable all` without an explicit and verifiable justification documented in an ADR.

### Writing Tests

All new features **must** be accompanied by tests.
- **Unit Tests:** Placed in the corresponding `tests/EricksonLopez.Pagination.<Package>.Tests` project.
- **Integration Tests:** Placed in `tests/EricksonLopez.Pagination.IntegrationTests`. These require Docker (Testcontainers) for PostgreSQL, SQL Server, and MongoDB.
- **Property-based Tests:** Use `FsCheck.Xunit` for invariant testing of encoding, decoding, and filter parsing.
- **Blazor Component Tests:** Use `bUnit` in `tests/EricksonLopez.Pagination.Blazor.Tests`.

### Mutation Testing

We use [Stryker.NET](https://stryker-mutator.io/) for mutation testing. The tool is included in `dotnet-tools.json` and is restored automatically with `dotnet tool restore`.

```bash
# Restore local tools (includes dotnet-stryker v4.16.0)
dotnet tool restore

# Run from repository root (uses stryker-config.json targeting EricksonLopez.Pagination.slnx)
dotnet stryker
```

> **Note**: The CI Stryker workflow (`stryker.yml`) runs `dotnet stryker -s EricksonLopez.Pagination.slnx` (the main solution). The default local `dotnet stryker` command uses `stryker-config.json`, which also targets `EricksonLopez.Pagination.slnx`. A separate `stryker.slnx` file exists in the repository root but is not currently used by CI or the default config — it is a smaller subset solution for targeted manual runs.

Thresholds (from `stryker-config.json`): `high=100`, `low=98`, `break=95`. A mutation score below **95** will block the pull request review.

### Benchmarks

If you are modifying hot paths (e.g., Cursor Encoders, Expression Caching, `FilterParameters`), run benchmarks to prove your changes do not degrade performance:

```bash
dotnet run -c Release --project tests/EricksonLopez.Pagination.Benchmarks/EricksonLopez.Pagination.Benchmarks.csproj
```

Benchmark results are uploaded as CI artifacts on every push to `main`.

## 4. Pull Request Process

1. Ensure your code builds cleanly: `dotnet build -c Release` with zero warnings (`TreatWarningsAsErrors=true`).
2. Ensure all tests pass: `dotnet test --settings .runsettings`.
3. Update `CHANGELOG.md` under the `[Unreleased]` section with your change.
4. Open a Pull Request against the `main` branch using the PR template.

**PR Checklist (must be completed):**
- [ ] Code follows style guidelines (`TreatWarningsAsErrors=true`)
- [ ] Self-review of code performed
- [ ] Code is commented in hard-to-understand areas
- [ ] Documentation has been updated
- [ ] No new warnings are generated
- [ ] Tests added that prove the fix/feature
- [ ] Unit tests pass locally with code coverage
- [ ] Mutation score remains above **95%**
- [ ] `CHANGELOG.md` updated

5. A maintainer will review your code. You may be asked to make changes before approval.
6. Once approved, your PR will be squash-merged into `main`.

## 5. Backward Compatibility

We strictly adhere to Semantic Versioning (SemVer 2.0.0).
- **Breaking Changes:** Must be targeted for the next major release. `<EnablePackageValidation>` with `PackageValidationBaselineVersion=1.0.0` will enforce API surface stability during CI once a `1.0.0` NuGet baseline is published.
- **Deprecations:** Use the `[Obsolete]` attribute with a clear message, diagnostic ID, and migration guidance before removing any API.

## 6. Proposing Features

If you have a feature idea, open an Issue with the `enhancement` label. Include:
- A clear use case.
- Proposed API design (preferably with code examples).
- Alternative solutions considered.
- Whether the change is a breaking API change.

See [roadmap.md](roadmap.md) for planned work and [docs/adr/](docs/adr/) for architectural decisions that define scope boundaries.

Thank you for contributing!
