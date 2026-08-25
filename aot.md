# aot.md — EricksonLopez.Pagination
### Native AOT Strategy · Trimming · Source Generators · August 2026

---

## 1. AOT Design Philosophy

> **AOT-first in core; AOT-compatible with documented limitations in providers.**

The library distinguishes two levels:

- **AOT-first**: The component was designed from the ground up for AOT. Hot paths are reflection-free.
- **AOT-compatible**: The component works with `PublishAot=true` but has isolated paths with `[RequiresDynamicCode]` that are documented and annotated.
- **Not AOT-compatible**: The component uses runtime reflection or dynamic code generation that cannot be used in AOT contexts.

---

## 2. AOT Status by Component

| Component | AOT Level | Notes |
|---|---|---|
| `EricksonLopez.Pagination.Abstractions` | **AOT-FIRST** | Pure interfaces; zero reflection |
| `EricksonLopez.Pagination` (Core) | **AOT-FIRST** | Cursor encoding, result models, options |
| `EricksonLopez.Pagination.AspNetCore` | **AOT-FIRST** | IParsable<T> model binders; no reflection |
| `EricksonLopez.Pagination.Blazor` | **AOT-FIRST** | Depends on Abstractions only |
| `EricksonLopez.Pagination.Grpc` | **AOT-FIRST** | Depends on Abstractions only |
| `EricksonLopez.Pagination.EntityFrameworkCore` (keyset) | **AOT-COMPATIBLE** | KeysetBuilder<T> uses compile-time expression trees |
| `EricksonLopez.Pagination.EntityFrameworkCore` (filter DSL) | **NOT AOT-SAFE** | `[RequiresUnreferencedCode]` + `MakeGenericType` |
| `EricksonLopez.Pagination.Dapper` | **NOT DECLARED** | Dapper itself uses reflection for type mapping |
| `EricksonLopez.Pagination.MongoDB` | **NOT AOT-SAFE** | MongoDB.Driver uses reflection |
| `EricksonLopez.Pagination.OpenApi` | **NOT DECLARED** | Swagger tooling is not AOT-safe |

---

## 3. What "AOT-First" Means

### 3.1 Core cursor encoding: stackalloc + no reflection

```csharp
// .NET 8+ path — zero heap allocation
Span<char> buffer = stackalloc char[256];
// Base64URL encoding via Span<char> without string allocation
```

No runtime type lookup, no `Type.GetProperties()`, no dynamic dispatch.

### 3.2 KeysetBuilder<T>: compile-time expression trees

```csharp
// Compile-time: expression tree built from lambda
builder.Ascending(x => x.CreatedAt)  // Expression<Func<T, DateTimeOffset>>
       .Ascending(x => x.Id)          // Expression<Func<T, int>>
```

The lambda is captured as an `Expression<Func<T, TKey>>`. EF Core translates these expression trees to SQL at runtime, but the property access is determined at compile time — no runtime reflection.

### 3.3 Source generators for cursor decoders

Without source generators, the cursor decoder registry would need runtime reflection to discover types:

```csharp
// Without source generator (NOT AOT-safe):
var decoder = Activator.CreateInstance(typeof(Decoder<>).MakeGenericType(columnType));
```

With source generators:
```csharp
// Generated [ModuleInitializer] (fully AOT-safe):
[ModuleInitializer]
internal static void RegisterCursorDecoders()
{
    CursorDecoderRegistry.Register<DateTimeOffset>(s => DateTimeOffset.Parse(s, null, DateTimeStyles.RoundtripKind));
    CursorDecoderRegistry.Register<int>(s => int.Parse(s, CultureInfo.InvariantCulture));
    // ...
}
```

The source generator reads `Ascending(x => x.CreatedAt)` calls and emits the corresponding typed registration.

---

## 4. What Is NOT AOT-Safe

### 4.1 Filter DSL (FilterExpression)

```csharp
[RequiresUnreferencedCode("FilterExpression uses reflection to locate entity properties by name.")]
internal static class FilterExpression
{
    // Uses:
    // - typeof(T).GetProperties() — trimmer may remove property metadata
    // - Expression.Call(MakeGenericMethod(...)) — [RequiresDynamicCode]
}
```

**Why not fixable with annotations alone**: `[UnconditionalSuppressMessage]` without a corresponding `[DynamicDependency]` would cause the linker to trim property metadata, causing `NullReferenceException` in published AOT binaries.

### 4.2 AOT-Safe Alternative for Filtering

Consumers who need AOT-safe filtering can implement `IFilterProvider<TEntity>`:

```csharp
// Manual, fully AOT-safe — no reflection
public class ProductFilterProvider : IFilterProvider<Product>
{
    public Expression<Func<Product, bool>> Build(FilterParameters filter)
    {
        Expression<Func<Product, bool>> expr = p => true;
        
        if (filter.TryGet("status", out var status))
            expr = expr.And(p => p.Status == status);
            
        return expr;
    }
}
```

This is the recommended pattern for Native AOT publishing.

---

## 5. AOT Forbidden Patterns

The following are explicitly forbidden in core/abstractions:

| Pattern | Reason |
|---|---|
| `Type.GetProperties()` / `Type.GetMethods()` | Trimmer removes metadata → runtime NullReferenceException |
| `Activator.CreateInstance(type)` | Requires public parameterless constructor; trimmer may remove it |
| `Expression.Call(MakeGenericMethod(...))` | [RequiresDynamicCode] |
| `typeof(T).MakeGenericType(...)` | [RequiresDynamicCode] |
| `Assembly.GetTypes()` | Trimmer removes type list |
| `dynamic` keyword | Generates IL that AOTC cannot process |
| `Reflection.Emit` / `ILGenerator` | Not supported in AOT |

---

## 6. AOT Allowed and Recommended Patterns

| Pattern | Notes |
|---|---|
| `Expression<Func<T, TKey>>` (compile-time) | EF Core translates; no runtime reflection |
| `stackalloc Span<T>` | Stack allocation; zero heap |
| `[JsonSerializable(typeof(T))]` source gen | System.Text.Json AOT serialization |
| `[ModuleInitializer]` | AOT-safe static registration |
| `IParsable<T>` | ASP.NET Core model binding without reflection |
| `Func<T, TResult>` compiled delegates (cached) | Compiled once; no runtime reflection |
| `typeof(T).FullName` (for FNV-1a fingerprint) | Safe; FullName is stable and AOT-preserved |
| Generic type constraints at compile time | Fully safe |

---

## 7. CI AOT Verification

```yaml
# .github/workflows/build.yml
- name: Verify Native AOT Compatibility
  run: |
    dotnet publish tests/EricksonLopez.Pagination.AotTest \
      -r linux-x64 \
      --configuration Release \
      -p:PublishAot=true \
      -warnaserror
```

The AOT test project:
- References `EricksonLopez.Pagination` + `AspNetCore` + `EFCore` (keyset path only)
- Exercises cursor encode/decode, PagedList<T> construction, KeysetBuilder<T>
- Zero [RequiresUnreferencedCode] warnings expected in this path
- Filter DSL path is excluded from AOT test (documented in ADR-0010)

---

## 8. Source Generator Architecture

### 8.1 Generator Input

The `CursorDecoderGenerator` (Roslyn ISourceGenerator) analyzes:
- `KeysetBuilder<T>.Ascending(Expression<Func<T, TKey>>)` invocations
- The `TKey` type parameter from each `Ascending`/`Descending` call

### 8.2 Generator Output

```csharp
// Generated: EricksonLopez.Pagination.Generated/CursorDecoderRegistrations.g.cs
using System.Runtime.CompilerServices;
using EricksonLopez.Pagination;

[ModuleInitializer]
internal static partial class GeneratedCursorDecoderRegistrations
{
    internal static void Initialize()
    {
        InMemoryCursorDecoderRegistry.Instance.Register<DateTimeOffset>(
            s => DateTimeOffset.Parse(s, null, DateTimeStyles.RoundtripKind));
        InMemoryCursorDecoderRegistry.Instance.Register<int>(
            s => int.Parse(s, CultureInfo.InvariantCulture));
        InMemoryCursorDecoderRegistry.Instance.Register<Guid>(
            s => Guid.Parse(s));
    }
}
```

### 8.3 When Is the Source Generator Required?

| Scenario | Generator Required? |
|---|---|
| Standard .NET 8+ (non-AOT) | Optional; fallback uses reflection |
| Publishing with `PublishAot=true` | **REQUIRED** — reflection path is trimmed |
| Blazor WASM | Optional; WASM AOT recommended |
| Blazor Server | Not required |

---

## 9. Trimming Strategy

### 9.1 Trimming Annotations Used

```csharp
// FilterExpression.cs — honest annotation
[RequiresUnreferencedCode("FilterExpression uses reflection to locate entity properties.")]
internal static class FilterExpression { ... }

// Dynamic sort — partial trimming concern
[RequiresUnreferencedCode("ApplySort uses reflection for dynamic property lookup.")]
public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, SortParameters sort) { ... }
```

### 9.2 [UnconditionalSuppressMessage] Policy

The library does NOT use `[UnconditionalSuppressMessage]` to silence trimming warnings without corresponding `[DynamicDependency]` annotations. This policy prevents false-safe AOT builds.

### 9.3 Trimming-Safe Serialization

```csharp
// JsonSerializerContext for AOT serialization:
[JsonSerializable(typeof(PagedResponse<MyDto>))]
[JsonSerializable(typeof(CursorPagedResponse<MyDto>))]
internal partial class PaginationJsonContext : JsonSerializerContext { }
```

Consumers should generate their own `JsonSerializerContext` for their DTO types. The library provides base contexts for its own response envelopes.

---

## 10. AOT Roadmap

| Item | Status | ADR |
|---|---|---|
| Core + Abstractions: full AOT | ✅ Done | ADR-0010 |
| Cursor encoding: stackalloc path | ✅ Done | — |
| Source generator: cursor decoders | ✅ Done | — |
| CI AOT verification gate | ✅ Done | — |
| Filter DSL: AOT-safe source generator | ⬜ Planned v1.2 | ADR-0010 roadmap |
| Filter DSL: IFilterProvider<T> documentation | ✅ Done | ADR-0010 |
| MongoDB: AOT path | ⬜ Blocked on MongoDB.Driver AOT support | — |
