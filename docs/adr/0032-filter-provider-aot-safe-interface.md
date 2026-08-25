# ADR-0032 — IFilterProvider<TEntity> Interface for AOT Filtering

## Status
**Accepted** — August 2026

## Context
As documented in ADR-0010, the runtime `FilterExpression` engine relies on reflection to discover entity properties by string name and `MakeGenericType`/`MakeGenericMethod` to construct expression trees at runtime. This causes linker trimming warnings (`IL2026`, `IL3050`) and runtime crashes under Native AOT when entity metadata is trimmed.

To provide consumers who target Native AOT with a standard, strongly typed, zero-reflection filtering mechanism, an interface-based filter provider contract is needed in `EricksonLopez.Pagination.Abstractions`.

## Decision
We introduce `IFilterProvider<TEntity>` in `EricksonLopez.Pagination.Abstractions`:

```csharp
namespace EricksonLopez.Pagination.Abstractions;

public interface IFilterProvider<TEntity>
{
    Expression<Func<TEntity, bool>>? Build(FilterParameters filter);
}
```

Along with:
1. Standard expression composition utilities (`FilterExpressionExtensions.And`, `Or`, `Not`) with parameter rebinding to easily chain multiple filter clauses without external dependencies.
2. An `ApplyFilter` overload on `IQueryable<T>` in `EricksonLopez.Pagination.EntityFrameworkCore` taking `IFilterProvider<T>`, which is marked without trimming warnings and runs completely reflection-free.

## Consequences
- Consumers building Native AOT applications can implement `IFilterProvider<TEntity>` manually or via compile-time source generators.
- Full AOT compatibility is achieved for the filtering pipeline without runtime reflection.
