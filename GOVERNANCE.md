# Governance Policy

## Overview
This document outlines the governance model for `EricksonLopez.Pagination`. As a foundational package intended to be a "Standard of Record" for paginated API responses across enterprise platforms, this library adheres to strict backward compatibility, performance budgets, and security principles.

## Maintainer Roles
- **BDFL / Lead Maintainer**: Erickson Lopez. Final decision on architectural shifts, breaking changes, and major versions.
- **Active Maintainers**: Erickson Lopez. Responsible for PR reviews, issue triage, and releases.
- **Performance Guild**: Ensures zero-allocation paths remain intact and GC pressure is minimal. Every PR modifying hot paths must include BenchmarkDotNet reports.
- **API Designers**: Reviews all public signatures to ensure compliance with the BCL design guidelines.

## Contribution Guidelines
1. **Zero-Defect Goal**: All new code must be accompanied by unit tests covering 100% of the new paths. Mutation testing is encouraged.
2. **AOT First**: No new APIs should introduce reflection that breaks NativeAOT compatibility. All logic relying on `MakeGenericType` or `MakeGenericMethod` must be flagged or replaced with Source Generators.
3. **No Hidden Dependencies**: The `Abstractions` package will remain dependency-free.
4. **Deprecation Policy**: Obsolete APIs must be marked with `[Obsolete]` for at least one major version before removal.

## Release Process
- **Patch Releases**: Security fixes, minor bugs, documentation updates.
- **Minor Releases**: Non-breaking new features (e.g., new `IEnumerable` extensions).
- **Major Releases**: Architectural overhauls, removal of deprecated APIs. Requires an RFC and a 2-week comment period.

## RFC Process
For any major architectural change, breaking API change, or significant new feature:
1. An issue must be opened with the `RFC` label.
2. The issue must detail the motivation, proposed design, breaking changes, and alternatives considered.
3. A mandatory 2-week comment period allows community and maintainers to discuss the proposal.
4. The BDFL / Lead Maintainer must explicitly approve the RFC before implementation begins.

## Security Policy
1. **Reporting Vulnerabilities**: Do not open a public issue. Use a private GitHub Security Advisory or contact the maintainers directly. See [SECURITY.md](SECURITY.md) for the full vulnerability reporting process.
2. **Supported Versions**: Security patches are applied to the `main` branch only. No backport policy exists until a stable `1.0.0` release establishes a supported baseline. See [SECURITY.md](SECURITY.md) for the current supported versions table.
3. **Disclosure Timeline**: We aim to resolve critical vulnerabilities within 48 hours and coordinate a public advisory on GitHub.
