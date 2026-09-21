# Architecture & Design Guide — EricksonLopez.Pagination

> Structural design principles, Clean Architecture boundaries, Native AOT design, and package topography.

---

## 1. Clean Architecture & Layer Decoupling

`EricksonLopez.Pagination` is organized into strictly separated architectural layers to uphold the Inversion of Control principle:

```
┌────────────────────────────────────────────────────────┐
│             Presentation (Web / UI Layer)              │
│   • EricksonLopez.Pagination.AspNetCore                │
│   • EricksonLopez.Pagination.Blazor                    │
│   • EricksonLopez.Pagination.OpenApi                   │
└──────────────────────────┬─────────────────────────────┘
                           │ references
                           ▼
┌────────────────────────────────────────────────────────┐
│             Domain & Application Core Layer            │
│   • EricksonLopez.Pagination.Abstractions              │
│     (IPagedList, CursorPaginationParameters)           │
│   • EricksonLopez.Pagination                           │
│     (PagedList, HmacCursorEncoder, Options)            │
└──────────────────────────▲─────────────────────────────┘
                           │ references
┌──────────────────────────┴─────────────────────────────┐
│               Infrastructure Layer                     │
│   • EricksonLopez.Pagination.EntityFrameworkCore       │
│   • EricksonLopez.Pagination.Dapper                    │
│   • EricksonLopez.Pagination.MongoDB                   │
│   • EricksonLopez.Pagination.Cosmos                    │
│   • EricksonLopez.Pagination.Redis                     │
└────────────────────────────────────────────────────────┘
```

- **Domain and Application Layers** reference ONLY `EricksonLopez.Pagination.Abstractions` (or `Core`). They remain completely decoupled from ASP.NET Core, EF Core, or database drivers.
- **Infrastructure Layer** references both Core/Abstractions and specific database drivers to implement the queryable extension methods.
- **Presentation Layer** maps incoming HTTP query parameters to parameter structs and formats outgoing DTOs.

---

## 2. Native AOT & Trimming Compatibility

The core abstractions and models of `EricksonLopez.Pagination` are designed from the ground up to support .NET Native AOT compilation:
1. **Zero-Reflection Parameter Binding**: Record structs implement `IParsable<T>`, which ASP.NET Core Minimal APIs bind natively without runtime reflection.
2. **Deterministic Serialization Context**: `PaginationJsonSerializerContext` in `AspNetCore` provides `System.Text.Json` source generator metadata for `PagedResponse<T>` and `CursorPagedResponse<T>`.
3. **Compile-Time Filter Generation**: Dynamic filtering via `ApplyFilter` relies on runtime reflection, but can be replaced with `[GenerateFilterProvider]` and `IFilterProvider<T>` to generate reflection-free filter evaluators at build time.

---

## 3. Cryptographic Threat Modeling

When designing pagination for public APIs, unsigned cursors introduce distinct attack vectors:
1. **Enumeration & Tampering**: Malicious users tamper with decoded cursor numbers to read unauthorized ranges.
   - *Mitigation*: HMAC-SHA256 signature sealing ensures payload modifications result in an immediate signature mismatch.
2. **Replay Attacks**: Attackers capture valid cursors and replay them repeatedly to harvest data or scrape endpoints.
   - *Mitigation*: `ICursorReplayStore` records single-use nonces embedded in cursors. Second submissions are rejected with `ReplayedPaginationCursorException`.
3. **Timing Side-Channel Attacks**: Cryptographic comparison could theoretically leak signature bytes via execution duration.
   - *Mitigation*: All signature comparisons use constant-time `CryptographicOperations.FixedTimeEquals`.
