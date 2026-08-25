# ADR-0026 — Null Sorting Behavior (NULLS FIRST / NULLS LAST)

## Status
**Accepted** — August 2026

## Context

Different databases sort NULL values differently:
- **PostgreSQL**: ASC → NULLS LAST; DESC → NULLS FIRST (standard SQL defaults)
- **SQL Server**: NULLs treated as minimum values (NULLs first for ASC, last for DESC)
- **MySQL**: NULLs treated as minimum values

For keyset pagination to be deterministic, NULL handling must be consistent. A consumer who has NULLable keyset columns may get incorrect results if the library does not handle NULLS consistently.

## Problem

Abstracting `NULLS FIRST`/`NULLS LAST` requires:
1. Provider-specific SQL generation (PostgreSQL supports `NULLS FIRST` syntax; SQL Server does not)
2. Corresponding nullable cursor position handling
3. Testing across all 4 database engines

This represents significant complexity for an edge case (most keyset columns should be NOT NULL).

## Decision

EricksonLopez.Pagination **does not abstract** NULLS FIRST/LAST behavior. The library:
1. Documents the database-specific behavior clearly
2. Recommends using NOT NULL columns for keyset pagination
3. If a consumer uses a nullable keyset column, they accept the database's default NULL ordering

## Why

1. **Complexity vs. value**: The benefit of abstracting NULL ordering is low; the implementation cost across 4 database engines is high.
2. **Best practice is NOT NULL keyset columns**: Any well-designed keyset column (especially `Id` tiebreakers) should be NOT NULL. The abstraction addresses a design smell.
3. **PostgreSQL `NULLS LAST` default is correct**: For ASC keyset, PostgreSQL's default (NULLS LAST) means NULLs appear after all non-NULL values, which is the correct behavior for append-heavy datasets.

## Consequences

- Consumers using nullable keyset columns are responsible for understanding their database's NULL sort order.
- Documentation explicitly warns: "Use NOT NULL columns for keyset pagination. If your keyset column is nullable, the NULL ordering behavior is database-specific and not abstracted by this library."

## Rejected Alternatives

- **Nullable keyset column support**: Would require significant complexity in cursor representation (how to encode a NULL position?) and SQL generation.
- **Force NOT NULL via generic constraint**: C# generics do not have a "non-nullable value type or reference type" constraint that would catch `string?` vs `string`.

## Reconsideration Criteria

If >10 GitHub issues report incorrect pagination due to NULL keyset columns, re-evaluate adding `NULLS LAST` support for PostgreSQL as an optional configuration.
