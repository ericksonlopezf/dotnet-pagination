# ADR-0039: Root-Level Orphaned Documentation Files — Technical Debt

## Status

Closed (Remediated & Consolidated — 2026-09-12)

## Date

2026-08-24 (Updated: 2026-09-12)

## Context

During the 2026-08-24 documentation audit, the following markdown files were found at the **repository root** that violated the established naming conventions:

- Root-level community health files: `SCREAMING_CASE.md` (e.g., `README.md`, `CHANGELOG.md`, `ROADMAP.md`)
- `/docs/` files: `kebab-case.md`

The following files existed at the root and did not match either convention:

| File | Content | Canonical Location in `/docs/` | Status |
|------|---------|--------------------------------|--------|
| `aot.md` | Native AOT compatibility guide | `docs/nuget-packages.md` | ✅ Consolidated & Removed from root |
| `architecture.md` | High-level architecture description | `docs/architecture.md` | ✅ Consolidated & Removed from root |
| `boundary.md` | `Abstractions` package boundary spec | `docs/abstraction-boundary.md` | ✅ Created in `/docs/` & Removed from root |
| `features.md` | Feature overview | `docs/feature-matrix.md` | ✅ Consolidated & Removed from root |
| `packages.md` | Package overview | `docs/nuget-packages.md` | ✅ Consolidated & Removed from root |
| `performance.md` | Performance claims and benchmarks | `docs/benchmark.md` & `docs/performance-guide.md` | ✅ Consolidated & Removed from root |
| `testing.md` | Testing strategy overview | `docs/quality-gates.md` | ✅ Consolidated & Removed from root |

## Remediation Decision (2026-09-12)

All unique content was audited and merged into the canonical documents in `/docs/`. The root duplicates were removed via `git rm`, leaving only official community health files in `SCREAMING_CASE.md` at the repository root.

## Consequences

- Repository root strictly adheres to OSS community health standards (`README.md`, `CHANGELOG.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `GOVERNANCE.md`, `SUPPORT.md`, `ROADMAP.md`, `LICENSE`).
- All technical documentation lives exclusively under `/docs/` in `kebab-case.md`.
- Zero duplication or divergent copies of architectural claims.

## Action Items

- [x] Merge `aot.md` content into `docs/nuget-packages.md` AOT section.
- [x] Merge unique content from root `architecture.md` into `docs/architecture.md`.
- [x] Move `boundary.md` to `docs/abstraction-boundary.md` with updated public API types.
- [x] Consolidate `features.md` into `docs/feature-matrix.md`.
- [x] Consolidate `packages.md` into `docs/nuget-packages.md`.
- [x] Consolidate `performance.md` into `docs/benchmark.md` and `docs/performance-guide.md`.
- [x] Consolidate `testing.md` into `docs/quality-gates.md`.
- [x] Remove orphaned lowercase markdown files from repository root.
