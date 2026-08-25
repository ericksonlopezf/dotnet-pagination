# ADR-0009: PagedList\<T\> as Class vs Page\<T\> as Struct

## Status
Accepted — August 2026

## Context

An earlier design specification proposed `Page<T>` as a `struct` (value type) for zero-allocation pagination results. The current implementation uses `PagedList<T>` as a `class` (reference type).

## Decision

Use `PagedList<T>` as a **`class` (not `sealed`, to allow `CountedPagedList<T>` subclassing)** rather than a `struct`. `CountedPagedList<T>` is itself `sealed` because no further specialization is anticipated.

## Rationale

### Why not a `struct`?

A `struct` for pagination results appears appealing at first glance (zero boxing, stack allocation). In practice it introduces severe usability problems:

1. **Generic interface default**: `IPagedList<T>` is a generic interface. A `struct` implementing this interface causes **boxing on every interface upcast** (e.g., when stored in `IPagedList<T>` variables, returned from service methods, or passed to response mappers). The boxing cost exceeds the allocation cost of a heap object.

2. **Mutability hazard**: structs in C# are copied by value. A `Page<T>` passed to a method that "modifies" it silently modifies a copy. Defensive copying at interface boundaries is an invisible correctness footgun.

3. **Inheritance for count semantics**: `CountedPagedList<T>` (with exact count) vs `PagedList<T>` (without count) requires a clean type hierarchy. Struct inheritance is not supported in C#; the only option would be two separate unrelated structs, breaking polymorphism with `IPagedList<T>`.

4. **IReadOnlyList<T> wrapping**: The internal `_items` field wraps an `IReadOnlyList<T>` that is itself a reference type. A struct wrapping a reference type provides no allocation savings — it still requires a heap object for the inner list.

5. **Compatibility with async LINQ**: Methods like `ToPagedListAsync` and the Blazor data providers expect `IPagedList<T>` polymorphism. Struct-based results would require repetitive type-parameterized code at every consumption site.

### Why `class` with `internal` constructor?

The `internal` constructor enforces factory-method construction (`PagedList.WithCount`, `PagedList.WithoutCount`, `PagedList.Empty`), preventing consumers from constructing inconsistent states (e.g., `TotalCount=-1`). External subclassing is prevented by the internal constructor — this is a documented design choice noted in the XML docs.

### Why not `sealed` on `PagedList<T>`?

`PagedList<T>` is intentionally not `sealed` to allow `CountedPagedList<T>` to subclass it. `CountedPagedList<T>` is `sealed` because no further specialization is anticipated. The `internal` constructor on `PagedList<T>` prevents external subclassing without the `sealed` keyword, which would require reflection to bypass.

## Consequences

### Positive
- Clean `IPagedList<T>` / `ICountedPagedList<T>` interface hierarchy
- No implicit boxing on interface assignments
- `Map<TResult>()` is a clean virtual method with correct override in `CountedPagedList<T>`
- Consistent with community expectations (`X.PagedList`, `MR.EntityFrameworkCore.KeysetPagination`, `SmartList`, all use classes)

### Negative
- One heap allocation per paginated result (unavoidable for any approach wrapping `IReadOnlyList<T>`)
- External consumers cannot subclass (mitigated by factory methods and `Map<TResult>()`)

## Alternatives Rejected

| Alternative | Reason rejected |
|---|---|
| `readonly record struct Page<T>` | Boxing on every `IPagedList<T>` upcast; no inheritance for count semantics |
| `record class Page<T>` (renamed) | Name conflicts with ASP.NET Core's `Page` base class; breaking change |
| `struct` with two separate types (no inheritance) | Breaks polymorphism; doubles the API surface area |
