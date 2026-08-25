# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| `main` (HEAD) — pre-release | ✅ Active development, no published tags yet |
| 0.9.0 baseline | ⚠️ Code baseline only — no NuGet package or git tag published |

> **Pre-1.0 Status**: This repository has no published git tags yet. Version management is handled by [MinVer](https://github.com/adamralph/minver) based on git tags (format: `v*.*.*`). Security patches are applied to the `main` branch only. The first formal release will establish the supported version baseline.

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, use one of the following channels:
1. **GitHub Private Security Advisory** (preferred): Navigate to [Security → Advisories](https://github.com/ericksonlopezf/dotnet-pagination/security/advisories) and create a new draft advisory.
2. **Direct Security Email**: Send details directly to [ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com).

We will acknowledge receipt within **48 hours** and provide regular updates. For critical vulnerabilities, we aim to release a patch within **48 hours** of confirmation.

## Known Security Boundaries

- **Cursor Obfuscation vs. HMAC Signing**: By default, cursors are Base64-encoded (obfuscated, not signed). If cursors contain sensitive data or authorization bounds, you **must** configure `HmacCursorEncoder` to prevent client tampering. It is strongly recommended to also configure a `timeToLive` (e.g., `TimeSpan.FromMinutes(30)`) to prevent indefinite cursor replay attacks.
- **SQL Injection Prevention**: When using `EricksonLopez.Pagination.Dapper`, rely exclusively on parameterized queries or the built-in `CursorSqlBuilder` strict identifier validation for any dynamic `ORDER BY` or `FROM` clauses. Never interpolate user-provided strings into SQL directly.
- **ReDoS Protection**: `SortParameters.ValidateColumnName` applies an O(1) length pre-check (max 200 chars) before the regex to prevent crafted inputs from causing catastrophic backtracking.
- **Max Page Size Caps**: The library caps `MaxPageSize` at `1000` by default across all paginated endpoints to mitigate memory exhaustion and Denial-of-Service attacks.
- **Filter Length Caps**: `FilterParameters` enforces a maximum filter string length of 4096 characters across all entry points (HTTP and gRPC) to prevent megabyte-sized payloads.

## Supply Chain Security

| Mechanism | Status | Details |
|---|---|---|
| **Strong Name Signing** | ✅ Active | Development builds use `DummyDevelopmentKey.snk`. CI injects the production key from `SIGNING_KEY_BASE64` secret during publish. |
| **NuGet Trusted Publishing (OIDC)** | ⚠️ Partial | `publish.yml` requests `id-token: write` (OIDC-compatible permission) but currently pushes using `NUGET_API_KEY`. Full keyless Trusted Publishing is a planned upgrade. |
| **SBOM Generation** | ✅ Active | `<GenerateSBOM>true</GenerateSBOM>` is set globally in `Directory.Build.props` for all packable projects. |
| **NuGet Audit** | ✅ Active | NuGet Audit is enabled for all package restores to actively scan transitive dependencies for known CVEs. |
| **Dependabot** | ✅ Active | NuGet packages scanned weekly; GitHub Actions scanned monthly (see `.github/dependabot.yml`). |
| **Package Validation** | ✅ Active | `<EnablePackageValidation>true</EnablePackageValidation>` with `PackageValidationBaselineVersion=1.0.0` enforces API surface stability for all packable projects. |
