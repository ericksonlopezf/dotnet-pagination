# ADR-0008: Cursor Decode Mechanism — ICursorDecoderRegistry over Unsafe.As

## Status
Accepted — August 2026

## Context

An earlier engineering document (the "CURRENT STATE" audit prompt) described the cursor decode path as using:

> `Unsafe.As<int, TKey>(ref i)` — zero-allocation boxing-free conversion in `CursorPaginationParametersExtensions.cs:86`

This claim was incorrect. The current implementation in `CursorPaginationParametersExtensions.cs` uses `ICursorDecoderRegistry` with a runtime fallback using `ValueCoercer.TryCoerce`. No `Unsafe.As` is used anywhere in the cursor decoding path.

## Decision

Use `ICursorDecoderRegistry` + `ValueCoercer.TryCoerce` as the cursor decode mechanism. Do **not** use `Unsafe.As<TSource, TDest>`.

## Rationale

### Why not `Unsafe.As<TSource, TDest>`?

`Unsafe.As` performs a **bitwise reinterpretation** of memory — it does not convert values. For cursor decoding, the input is always a `string` parsed from a URL-safe token. You cannot `Unsafe.As<int, TKey>` from a parsed string; the value must be deserialized from text first.

`Unsafe.As` is appropriate for:
- Reinterpreting reference types (e.g., `Unsafe.As<List<int>, List<uint>>(ref list)`) when the CLR layout is identical
- Low-level performance-critical span slicing

It is NOT appropriate for:
- Type-safe value coercion from string representation
- User-provided cursor data that must survive AOT/NativeAOT trimming

### Why `ICursorDecoderRegistry`?

The `ICursorDecoderRegistry` approach:

1. **AOT-safe**: Registered decoders are discovered at compile-time (via the `CursorDecoderGenerator` source generator), not at runtime via reflection.
2. **Extensible**: Consumers register their own cursor types without modifying the library.
3. **Testable**: The registry is an interface, allowing unit testing without constructing real encoder/decoder infrastructure.
4. **Allocation-transparent**: Boxing decisions are left to the registered decoder implementations, which can be zero-allocation for value types.

### Fallback path

When no decoder is registered for a type, `ValueCoercer.TryCoerce` provides a reflection-based fallback for development scenarios. This fallback is intentionally excluded from AOT builds via `[RequiresUnreferencedCode]`.

## Consequences

### Positive
- Architecture is AOT-safe and documented correctly.
- Consumers control cursor serialization through registered decoders.
- No hidden `Unsafe` usage that could mislead security reviewers.

### Negative
- Slightly more complex than a single `Unsafe.As` call for primitive key types.
- Requires source generator setup for AOT-safe custom cursor types.

## Documentation Action

All engineering documentation, ADR prompts, and audit documents that reference `Unsafe.As` in the context of cursor decoding should be updated to reflect the `ICursorDecoderRegistry` architecture. This ADR serves as the authoritative record.
