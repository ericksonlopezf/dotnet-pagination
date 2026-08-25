# 4. Mutation Testing Exclusions

Date: 2026-08-05

## Status

Accepted

## Context

As part of our continuous effort to achieve a 100% Mutation Testing Score with Stryker across the `EricksonLopez.Pagination` solution, we encountered two significant bottlenecks that compromised build times, stability, and the overall cost-benefit ratio of the testing strategy. These issues manifested in the `EntityFrameworkCore` and `MongoDB` integration libraries.

### Entity Framework Core (Expression Trees)
The `EricksonLopez.Pagination.EntityFrameworkCore` package relies heavily on building dynamic `Expression` trees and evaluating queries using EF Core's translation engine. Stryker creates hundreds of equivalent mutants (mutations that change the code but result in the exact same logical behavior when translated to SQL or due to EF Core optimizations). 
Furthermore, EF Core's asynchronous execution (e.g., `CountAsync`) and complex expression manipulation are deeply intertwined with internal EF state. Attempting to kill these mutants requires writing overly brittle tests that assert on the exact shape of the Expression tree rather than its behavioral outcome, violating our testing principles.

### MongoDB (Testcontainers and Infrastructure Cost)
The `EricksonLopez.Pagination.MongoDB.Tests` suite relies on `Testcontainers` to spin up isolated MongoDB instances. Mutation testing requires running the test suite repeatedly against mutated assemblies. The overhead of parallelizing and tearing down Docker containers for each mutation run causes the mutation testing process to become disproportionately expensive (often hanging or taking >40 minutes without progress). The infrastructure cost of achieving 100% mutation coverage here vastly outweighs the confidence gained, especially since the core pagination logic is already exhaustively tested in the base library.

## Decision

We have decided to explicitly exclude the `EricksonLopez.Pagination.EntityFrameworkCore` and `EricksonLopez.Pagination.MongoDB` projects from our Stryker Mutation Testing enforcement.

The philosophy behind this decision is: **It is perfectly defensible to exclude specific execution paths from Mutation Testing, provided the justification is technical and well-documented.**

Specifically:
1. **EntityFrameworkCore**: Excluded due to the high probability of equivalent mutants, the limitations of mutation analysis on Expression Trees, and an unfavorable cost-benefit ratio.
2. **MongoDB**: Excluded because the infrastructure required for the test suite (Testcontainers) makes Mutation Testing disproportionately expensive and unstable, without an equivalent increase in confidence regarding the system's behavior.
3. **OpenApi**: Excluded because of its strong dependence on the internal `SwaggerGen` and `ApiExplorer` engines. Mutations in schema definitions often result in semantic equivalents that are unreachable via traditional unit tests without fragile reflection assertions.
4. **ConcurrentLruCache**: Excluded because testing micro-optimizations for high-performance concurrency (like LRU eviction or Interlocked operations) leads to flaky or unkillable mutants where thread scheduling outpaces deterministic verification.
5. **CursorPaginationLinqToDBExtensions**: Excluded due to the experimental status of LINQ to DB integration (ADR-0029) and equivalent mutants resulting from expression tree translation to SQL providers.

These exclusions are implemented in the root `stryker-config.json` via the `mutate` array.

## Consequences

- **Positive**: The Stryker mutation testing pipeline is now stable, deterministic, and executes in a reasonable timeframe. We can strictly enforce a 100% mutation score on the core domain, abstractions, and standard data access layers (like Dapper).
- **Positive**: Developers will not waste time trying to kill equivalent mutants generated within Expression Tree builders.
- **Negative**: We lose automated verification of test completeness for the EF Core and MongoDB specific extensions. We mitigate this by ensuring 100% standard Code Coverage (Line/Branch) is maintained for these projects and relying on robust integration tests.
