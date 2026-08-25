# ADR 0019: Keyset Index Seek Bounding Condition

## Status

Accepted

## Date

2026-08-11

## Context
Keyset pagination traditionally generates an OR-expanded predicate for multi-column cursors. For example, a 2-column keyset on `(Age, Id)` generates the following SQL predicate:
`WHERE Age > @p1 OR (Age = @p1 AND Id > @p2)`

During benchmarking, we noticed that `EricksonLopez.Pagination` was taking ~10ms for a deep page query, while competitors were completing it in ~3ms. Analysis of the SQL execution plans in PostgreSQL revealed that the database optimizer struggles to efficiently use composite indexes when the top-level predicate is an `OR` condition. Instead of performing a fast Index Seek to jump to the exact starting node in the B-Tree, PostgreSQL was falling back to a less efficient Index Scan, evaluating the OR condition across tens of thousands of rows.

## Decision
We will explicitly inject a **Bounding Condition** to the top-level of the keyset predicate for multi-column queries.
The new predicate structure will be:
`WHERE Age >= @p1 AND (Age > @p1 OR (Age = @p1 AND Id > @p2))`

The logic explicitly evaluates the direction of the first column (e.g. `>=` for Ascending, `<=` for Descending) and prepends it to the final OR-chain using an `AND` operator.

## Consequences
- **Positive:** PostgreSQL and SQL Server query optimizers immediately recognize the top-level `AND` condition and perform an optimal **Index Seek** on the primary sort column.
- **Positive:** Benchmark times for deep pagination dropped by nearly 80% (from ~10ms to ~2.2ms), effectively matching raw database speed limits.
- **Negative:** The generated SQL is slightly more verbose, but the performance gains overwhelmingly justify it.

## Justification
Relying on the database engine to infer optimization boundaries from complex OR-trees is inconsistent across different relational engines. By explicitly declaring the boundary limit on the most significant column of the index, we guarantee that the database engine can seek the B-Tree efficiently, upholding our commitment to maximum performance.
