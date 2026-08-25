# ADR-0011: Publish Pipeline Scope

## Status

Accepted (Amended — See Amendment 2026-08-24)

## Date

2026-08-10 (Updated 2026-08-11, Amended 2026-08-24)

## Context

The `EricksonLopez.Pagination` ecosystem contains 12 source projects across the `src/` directory. Originally (as of 2026-08-10), the `publish.yml` GitHub Actions workflow only packed and pushed 5 of these to NuGet.org:

1. `EricksonLopez.Pagination`
2. `EricksonLopez.Pagination.Abstractions`
3. `EricksonLopez.Pagination.EntityFrameworkCore`
4. `EricksonLopez.Pagination.AspNetCore`
5. `EricksonLopez.Pagination.Dapper`

The other 7 projects were considered experimental or in-development and excluded.

However, a comprehensive technical audit on 2026-08-11 verified that all 7 remaining packages (MongoDB, Blazor, Grpc, OpenApi, Cosmos, SourceGenerators, and Analyzers) have achieved stability. They now feature:
- Multi-TFM support (`net8.0;net9.0;net10.0` or `netstandard2.0` as appropriate)
- `<IsPackable>true</IsPackable>` configuration
- Proper README documentation
- Comprehensive tests
- No pending `NotImplementedException` or experimental code blockers

## Decision

*(Original Decision 2026-08-10)* Limit the automated publish pipeline to the 5 core packages.

*(Update Decision 2026-08-11)* Expand the automated publish pipeline to include **all 12 packages** in the ecosystem. The experimental phase for the 7 additional projects is formally concluded. They are now considered stable and will be published to NuGet.org via the `publish.yml` workflow on every new release tag.

## Consequences

### Positive
- Consumers now have access to the full suite of pagination extensions (MongoDB, Cosmos, gRPC, Blazor, OpenAPI) via official NuGet packages.
- Native AOT support is fully unlocked for consumers via the newly published `SourceGenerators`.
- Pagination bugs can be caught at compile-time by all consumers using the newly published `Analyzers`.
- The README documentation aligns with the actual packages available on NuGet.

### Negative
- A larger release surface area. Any shared core updates require validating and potentially bumping versions across all 12 packages.
- The first release will require bypassing the `PackageValidationBaselineVersion` since baseline versions (1.0.0) do not exist yet for the newly promoted packages.

### Action Items
- [x] Apply `<DisablePackageValidationBaseline>true</DisablePackageValidationBaseline>` to the 7 newly promoted packages for their first release to bypass validation failures.
- [x] Ensure all 12 packages are present in the `publish.yml` pack steps.

---

## Amendment: 2026-08-24 — Six Additional Packages Exist Outside Publish Scope

**Context**: A subsequent documentation audit (2026-08-24) identified that 6 additional source packages were added to the `/src/` directory **after** the 2026-08-11 update of this ADR:

| Package | Status |
|---------|--------|
| `EricksonLopez.Pagination.SqlBuilder` | Source exists, `IsPackable=true`, **not in `publish.yml`** |
| `EricksonLopez.Pagination.LinqToDB` | Source exists, `IsPackable=true`, **not in `publish.yml`** |
| `EricksonLopez.Pagination.Redis` | Source exists, `IsPackable=true`, **not in `publish.yml`** |
| `EricksonLopez.Pagination.Relay` | Source exists, `IsPackable=true`, **not in `publish.yml`** |
| `EricksonLopez.Pagination.Elasticsearch` | Source exists, `IsPackable=true`, **not in `publish.yml`** |
| `EricksonLopez.Pagination.Result` | Source exists, `IsPackable=true`, `net10.0` only, **not in `publish.yml`** |

**Decision**: These 6 packages remain excluded from `publish.yml` until each meets these criteria:
1. Test coverage and mutation score parity with the 12 published packages.
2. Full public API documentation in `docs/api-reference.md`.
3. An explicit decision by the Lead Maintainer to promote them to stable status.

**Action Items**:
- [ ] Add `dotnet pack` steps to `publish.yml` for each package when it reaches release readiness.
- [ ] Add `EricksonLopez.Pagination.Result` target framework expansion to `net8.0;net9.0;net10.0` when dependency `EricksonLopez.Result` supports those frameworks.

