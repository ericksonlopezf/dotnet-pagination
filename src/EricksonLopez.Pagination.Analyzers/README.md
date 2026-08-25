# EricksonLopez.Pagination.Analyzers

Roslyn analyzers for the `EricksonLopez.Pagination` ecosystem to ensure correctness, performance, and security when using cursor and keyset pagination.

## Overview

This package contains diagnostic analyzers that run during compilation to detect common mistakes, anti-patterns, and security vulnerabilities related to pagination.

## Installation

```xml
<PackageReference Include="EricksonLopez.Pagination.Analyzers" Version="[VERSION]" />
```

*Note: The analyzers are a development dependency and will not be included as a runtime dependency in your published output.*

## Diagnostics

| Diagnostic ID | Severity | Category | Description |
|---|---|---|---|
| **PAG002** | Warning | Usage | **Missing `OrderBy` before `ToPagedListAsync`**. Standard offset pagination requires a deterministic sort order to prevent skipping or duplicating items across pages. |
| **PAG003** | Warning | Usage | **Unexpected `OrderBy` before `ToCursorPagedListAsync`**. Cursor pagination applies its own sorting based on the cursor configuration. Applying `OrderBy` beforehand causes conflicting sorts. |
| **PAG004** | Warning | Usage | **`OrderBy` before `Keyset()`**. Calling `OrderBy` before building a keyset causes duplicate `ORDER BY` clauses in the generated SQL. Use `Keyset().Ascending()` instead. |
| **PAG005** | Info | Performance | **Consider using `EricksonLopez.Pagination.SourceGenerators`**. Without the source generator, cursor decoding relies on runtime reflection, which is slower and incompatible with Native AOT. |
| **PAG006** | Warning | Security | **`ToCursorPagedListAsync` called — verify HMAC encoder is configured**. This warning fires on every call to `ToCursorPagedListAsync` because analyzers cannot detect DI configuration at compile time. Suppress with `#pragma warning disable PAG006` if `HmacCursorEncoder` is already configured in `AddPagination()`. |
| **PAG007** | Warning | Performance | **KeysetBuilder has too many columns**. Using more than 5 columns in a keyset generates complex SQL predicates that degrade query plan performance on databases that don't support row-value syntax. Use a composite tie-breaker instead. |
| **PAG008** | Warning | Security | **Explicit use of insecure `Base64CursorEncoder`**. Base64 cursor encoding is vulnerable to tampering and manipulation. Use `HmacCursorEncoder` with a secret key instead. |

## Suppression

If you intentionally want to ignore a diagnostic, you can suppress it using standard C# pragmas or in your `.editorconfig`:

```csharp
#pragma warning disable PAG006 // I don't care about cursor tampering in this internal app
var paged = await query.ToCursorPagedListAsync(parameters);
#pragma warning restore PAG006
```
