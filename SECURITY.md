# Security Policy

## Supported Versions

| Version | Supported | Notes |
|---------|-----------|-------|
| `2.0.x` | ✅ Supported | Current production release baseline (Released: 2026-09-21) |
| `1.0.x` | ⚠️ Security Only | Previous major version; critical security patches only |
| `main` (HEAD) | ✅ Active | Active development branch; security patches applied directly |
| `< 1.0.0` | ❌ End of Life | Pre-release code baselines superseded by 1.0.0 |

> **Release Baseline**: Version `2.0.0` (Released: 2026-09-21) establishes the current public API and production baseline. Versioning is automated by [Release Please](https://github.com/googleapis/release-please) based on Conventional Commits and git tags (format: `v*.*.*`). Security patches are backported or released as patch updates on `main`.

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
| **Strong Name Signing** | ✅ Active | Assemblies are signed with `EricksonLopez.snk`. CI restores the signing key from the `SNK_KEY` secret during release packaging. |
| **NuGet Trusted Publishing (OIDC)** | ✅ Active | Keyless OIDC publishing configured in `publish.yml` using `NuGet/login@v1`. No static API keys stored in secrets. |
| **Sigstore Provenance Attestation** | ✅ Active | Cryptographic build provenance attestations generated via `actions/attest-build-provenance@v2` for all `.nupkg` artifacts. |
| **SBOM Generation** | ✅ Active | `<GenerateSBOM>true</GenerateSBOM>` is enabled globally in `Directory.Build.props` for all packable projects. |
| **NuGet Audit** | ✅ Active | Scans all direct and transitive dependencies for known vulnerabilities during package restore. |
| **Dependabot** | ✅ Active | Automated dependency updates configured weekly for NuGet and monthly for GitHub Actions (`.github/dependabot.yml`). |
| **Package Validation** | ✅ Active | `<EnablePackageValidation>true</EnablePackageValidation>` with `PackageValidationBaselineVersion=1.0.0` guards against unintended breaking binary changes. |
