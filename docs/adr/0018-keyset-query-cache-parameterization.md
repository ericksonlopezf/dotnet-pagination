# ADR 0018: Dynamic Query Cache Parameterization Strategy

## Status

Accepted

## Date

2026-08-11

## Context
During head-to-head benchmarking against `MR.EntityFrameworkCore.KeysetPagination`, we noticed our keyset implementation (`KeysetBuilder`) was significantly slower. Initial hypotheses suggested that dynamically building the Expression Tree using `Expression.Constant(val)` for cursor values caused EF Core to miss the Query Compilation Cache, leading to a recompilation penalty on every request.

To fix this, an attempt was made to wrap cursor values in a `KeysetClosure` class and access them via `Expression.Property` and `Expression.ArrayIndex` to simulate a C# compiler closure, forcing parameterization. 

However, this `KeysetClosure` approach added over 20 extra nodes to the Expression Tree for a multi-column keyset. This deep tree caused EF Core's `ExpressionEqualityComparer` to spend an extra ~1ms hashing the tree during query compilation, actively degrading performance.

Crucially, testing revealed that **EF Core natively and automatically parameterizes `ConstantExpression` nodes** if they represent primitive scalar types (like `int`, `string`, `Guid`, etc.). Our original approach was already hitting the query cache perfectly; the performance deficit was coming from elsewhere.

## Decision
We will **not** use custom closure wrappers (`KeysetClosure`) to force parameterization in dynamically built `IQueryable` expression trees.
Instead, we will rely on EF Core's native behavior to parameterize primitive `Expression.Constant` nodes.

## Consequences
- **Positive:** Expression trees remain shallow and efficient, minimizing the hashing cost for EF Core's Query Cache.
- **Positive:** Simplifies the codebase by removing complex `ParameterReplacer` logic and array index coercions.
- **Negative:** If a developer uses a non-primitive, non-scalar type as a keyset column (which is extremely rare and discouraged), EF Core might fail to parameterize it, leading to cache misses. This is an acceptable edge case.

## Justification
Over-engineering a closure simulation backfired by increasing the tree depth and slowing down the exact cache it was trying to optimize. "Clean Architecture" and "Technical Truth" demand that we leverage the framework's native optimizations (EF Core's automatic constant parameterization) rather than fighting against it with complex workarounds.
