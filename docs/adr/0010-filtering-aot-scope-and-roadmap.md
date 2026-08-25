# ADR-0010: Filtering AOT Scope and Roadmap

## Status
Accepted — August 2026

## Context

The library markets itself as "AOT-compatible." The `CursorDecoderGenerator` source generator and `ICursorDecoderRegistry` infrastructure correctly supports Native AOT for cursor decoding. However, the filtering DSL (`FilterExpression`) uses:

- `[RequiresUnreferencedCode]` — uses reflection to locate entity properties by name
- `[RequiresDynamicCode]` — uses `MakeGenericType` and `MakeGenericMethod` for expression building

This means **filtering is NOT AOT-safe** in its current form.

## Decision

Acknowledge that filtering is partially AOT-incompatible, document the scope explicitly, and provide a roadmap for a future AOT-safe filter provider.

## Current AOT Status (Accurate as of v2.x)

| Feature | AOT Safe | Notes |
|---|---|---|
| Cursor encoding/decoding | ✅ | Source-generated `ICursorDecoderRegistry` |
| Offset pagination | ✅ | Pure LINQ expressions |
| Keyset pagination | ✅ | Compile-time expressions via `KeysetBuilder<T>` |
| `PagedList<T>` / serialization | ✅ | Plain record/class with `[JsonSerializable]` support |
| Filtering (`FilterExpression`) | ❌ | Reflection + `MakeGenericType` |
| Dynamic sort (`ApplySort`) | ⚠️ | Uses reflection for property lookup; `[RequiresUnreferencedCode]` annotated |

## Why Not Suppress the Attributes?

`[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` are not "warnings" — they are compiler-enforced contracts. Suppressing them with `[UnconditionalSuppressMessage]` without a corresponding `[DynamicDependency]` or source generator would cause the linker to trim the property metadata that the filter engine needs, causing `NullReferenceException` or silent wrong behavior in published NativeAOT binaries.

## Roadmap for AOT-Safe Filtering

A future version will provide an AOT-compatible filter provider via:

1. **Source Generator approach**: A Roslyn source generator reads `[AllowFiltering]` or `[FilterableProperty]` attributes on entity types at compile-time and generates a strongly-typed filter dispatch table. The `FilterExpression` engine can then use the generated table instead of runtime reflection.

2. **Interface-based approach**: Consumers implement `IFilterProvider<TEntity>` manually, providing their own `Func<FilterParameters, Expression<Func<TEntity, bool>>>`. The library provides helpers to compose these expressions (AND/OR/NOT). This is fully AOT-safe with zero library changes.

The interface-based approach is available today for consumers who need AOT-safe filtering. It requires manual implementation but provides full control over which properties are filterable.

## Consequences

### Positive
- Honest, accurate documentation of AOT compatibility scope
- Consumers can make informed decisions about whether to use the filtering DSL in AOT contexts
- The interface-based fallback path (`IFilterProvider<T>`) is documented as the AOT-safe alternative

### Negative
- The "AOT compatible" marketing claim must be qualified with "for pagination and cursor features; filtering requires a non-AOT path or manual implementation"
- Until the source generator approach is implemented, AOT filtering requires manual per-entity effort

## Action

Update all public documentation (README, `FilterExpression` XML docs, NuGet package description) to accurately state the filtering AOT limitation and reference this ADR for the roadmap.
