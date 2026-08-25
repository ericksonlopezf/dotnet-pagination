# 30. FilterExpression Location — Core Package vs EFCore Provider

Date: 2026-08-14

## Status

Accepted (status quo formalized)

## Context

`FilterExpression.cs` (10.7KB) lives in `src/EricksonLopez.Pagination/` — the core
package. However, the project architecture declares:

> "Filtering data is not a pagination responsibility."
> "Filtering predicates: EXTENSION / EFCore OWNS" (features.md §5)

This appears to violate the boundary: filter parsing logic is in the core package,
not in the EFCore provider where it is applied via `ApplyFilter()`.

## Analysis

The actual split is:

| Component | Location | Role |
|---|---|---|
| `FilterExpression.cs` | Core package | DSL **parser**: tokenizes and compiles `"name~=John,age>=18"` to `Expression<Func<T, bool>>` |
| `ApplyFilter()` | EFCore provider | DSL **executor**: applies the compiled expression to `IQueryable<T>` via EF Core |

The **parser** does not import EF Core. It produces an `Expression<Func<T, bool>>`
which is a general C# type. It could theoretically be used with any LINQ provider.

The **executor** (`ApplyFilter`) is the EF Core-specific step and correctly lives
in the EFCore package.

## Decision

**Accept the current location.** `FilterExpression.cs` in the core package is
defensible because:

1. It produces a `System.Linq.Expressions` tree, not a database query.
2. It does not import EF Core, Dapper, or any database package.
3. It belongs to the filter DSL feature, which is attached to (not core to)
   pagination — but the expression compiler itself has no database dependency.

However, to avoid future confusion, the core package should:

1. Mark all filter-related types with `[RequiresUnreferencedCode]` (already done).
2. Include a comment header on `FilterExpression.cs` stating: "This file implements
   the DSL parser for the filter extension. It does not belong to pagination
   semantics; it is co-located in Core because it produces a general
   `Expression<Func<T, bool>>` without database dependencies."
3. NOT add any database, HTTP, or EF Core imports to filter-related types in Core.

## Consequences

- No code moves.
- `FilterExpression.cs` gains an explanatory comment header.
- This ADR documents the architectural reasoning and prevents future attempts to
  move the parser to EFCore (which would force all consumers to depend on EFCore
  just to parse filter strings).
- If a future Dapper or LinqToDB filter DSL is implemented, the shared parser in
  Core makes sense.
