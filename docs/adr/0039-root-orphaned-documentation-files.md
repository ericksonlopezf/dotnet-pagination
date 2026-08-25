# ADR-0039: Root-Level Orphaned Documentation Files — Technical Debt

## Status

Open (Technical Debt Documented)

## Date

2026-08-24

## Context

During the 2026-08-24 documentation audit, the following markdown files were found at the **repository root** that violate the established naming conventions:

- Root-level community health files: `SCREAMING_CASE.md` (e.g., `README.md`, `CHANGELOG.md`)
- `/docs/` files: `kebab-case.md`

The following files exist at the root and do not match either convention:

| File | Content | Equivalent in `/docs/` |
|------|---------|------------------------|
| `aot.md` | Native AOT compatibility guide | No direct `/docs/` equivalent (partially covered by `docs/nuget-packages.md`) |
| `architecture.md` | High-level architecture description | `docs/architecture.md` ✅ |
| `boundary.md` | `Abstractions` package boundary spec | `docs/architecture.md` (partially) |
| `features.md` | Feature overview | `docs/feature-matrix.md` (partially) |
| `packages.md` | Package overview | `docs/nuget-packages.md` (partially) |
| `performance.md` | Performance claims and benchmarks | `docs/benchmark.md` (partially) |
| `testing.md` | Testing strategy overview | `docs/quality-gates.md` (partially) |

These files were likely generated during development as working documents and never moved/consolidated into `/docs/`. The root `architecture.md` directly conflicts with `docs/architecture.md` — both exist independently with different content.

## Problem

1. **Naming convention violations**: Root files that are not standard community health files (README, CHANGELOG, etc.) should not exist as lowercase `.md` files.
2. **Content duplication risk**: Divergent copies of architectural information create documentation inconsistency.
3. **Navigation confusion**: Contributors cannot tell whether to consult root-level or `/docs/` files.

## Decision

These files are **not removed or renamed** in this ADR because:
1. They may be referenced by external tooling, CI scripts, or GitHub links not visible in the local filesystem.
2. Moving them without updating all references would create broken links.
3. The audit tool (`build_html_report.py`) may generate reports based on these files.

The correct remediation requires a dedicated effort to:
1. Audit all inbound links to each file (GitHub Issues, external pages, etc.).
2. Merge content into the appropriate `/docs/` files.
3. Add HTTP 301 redirect hints or GitHub redirect notes in the old locations.

## Consequences

- These files remain as-is until explicit clean-up is scheduled.
- The README documentation navigation section does **not** link to these orphaned files.
- A future issue should be opened with label `tech-debt` to track the consolidation.

## Action Items

- [ ] Open GitHub issue tagged `tech-debt` to schedule consolidation of orphaned root docs.
- [ ] Determine if `build_html_report.py` references any of these files; update it accordingly.
- [ ] Merge `aot.md` content into `docs/nuget-packages.md` AOT section.
- [ ] Merge unique content from root `architecture.md` into `docs/architecture.md`.
- [ ] Move `boundary.md` to `docs/` as `abstraction-boundary.md`.
- [ ] Consolidate `features.md` into `docs/feature-matrix.md`.
- [ ] Consolidate `packages.md` into `docs/nuget-packages.md`.
- [ ] Consolidate `performance.md` into `docs/benchmark.md`.
- [ ] Consolidate `testing.md` into `docs/quality-gates.md`.
