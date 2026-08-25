# 29. LinqToDB Adapter — Production Readiness Status

Date: 2026-08-14

## Status

Accepted

## Context

The `EricksonLopez.Pagination.LinqToDB` package was implemented in the source tree
(`src/EricksonLopez.Pagination.LinqToDB/`) with four source files totaling ~72KB
of implementation code:

- `CursorPaginationLinqToDBExtensions.cs` (10.6KB)
- `KeysetBuilder.cs` (56.6KB — LinqToDB variant)
- `QueryableLinqToDBExtensions.cs` (4.8KB)

However, the documentation classified this package as:
- `features.md`: "Demand-gated; >10 votes required" (implies it does not exist yet)
- `roadmap.md`: Listed under Phase 4 as a future item
- README.md: Not listed in the package table

This mismatch between code reality and documentation creates consumer confusion and
hides a correctness risk: the package exists in `src/` and will be compiled,
but its test coverage is only **30.7%** (Stryker mutation score, 2026-08-14 run).

## Decision

1. **Classify LinqToDB as EXPERIMENTAL** — explicitly present in source, explicitly
   not production-ready. This is distinct from "not implemented" and distinct from
   "production-ready."

2. **Do not publish `EricksonLopez.Pagination.LinqToDB` to NuGet** until Stryker
   mutation score reaches ≥80% (minimum) and integration tests cover:
   - Offset pagination (count and count-less)
   - Keyset pagination (2-column, forward and backward)
   - Cursor encode/decode round-trip with KeysetBuilder
   - Error cases (expired cursor, wrong version, missing tiebreaker)

3. **Update all documentation** to reflect the honest state:
   - Package exists in source
   - Not published
   - Not production-tested
   - Requires coverage improvement before production use

4. **Do not gate on GitHub votes** — the vote gate is a roadmap planning mechanism
   for unimplemented features. Since the code already exists, the gate is now a
   quality gate (coverage threshold), not a demand gate.

## Consequences

- `src/EricksonLopez.Pagination.LinqToDB/` remains in the repository
- `EricksonLopez.Pagination.LinqToDB.csproj` must be excluded from the NuGet
  publish pipeline until the quality gate is met
- features.md, README.md, and roadmap.md updated to reflect experimental status
- `CursorPaginationLinqToDBExtensions.cs` remains in the Stryker `mutate` exclusion
  list (per existing stryker-config.json) until coverage improves

## Quality Gate for Graduation to Production

| Criterion | Threshold |
|---|---|
| Stryker mutation score (LinqToDB package) | ≥ 80% |
| Integration tests (Testcontainers or Docker) | Present and passing in CI |
| API review | Consistent with EFCore KeysetBuilder\<T\> API surface |
| Documentation | README section + cookbook examples |
| AOT status | Declared explicitly (not inferred) |
