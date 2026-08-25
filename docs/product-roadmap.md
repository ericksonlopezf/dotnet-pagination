# Product Roadmap — EricksonLopez.Pagination

> **Strategy**: Balanced — improve credibility and visibility without sacrificing technical differentiation.
> **North Star Metric**: NuGet downloads/week (target: 1,000/week by end of 12 months from initial public release)

> **Note**: This is a living document. Items listed here are *planned* future work — not yet implemented. See [CHANGELOG.md](../CHANGELOG.md) for completed items. Items are reviewed quarterly.

---

## NOW — 0–3 Months (Credibility & Honesty)

### - [x] 1. Keyset vs. Offset Benchmark (the killer demo)

| | |
|---|---|
| **Problem** | No public benchmark demonstrates the O(log N) keyset vs O(N) offset difference. Developers need data to justify migrating from OFFSET. |
| **Initiative** | Add keyset pagination to `docs/benchmark.md` with BenchmarkDotNet over 1M, 5M, 10M rows. Show offset degradation alongside keyset stability. |
| **Strategic rationale** | This is the definitive adoption argument for developers experiencing slow OFFSET in production. A verified benchmark is worth more than any README claim. |
| **Expected impact** | Converts keyset from "a feature on paper" to "the reason to adopt this library." |
| **Dependencies** | Keyset implementation in EF Core (already exists). Benchmark infrastructure (already exists). |
| **Risks** | None — if keyset underperforms expectations, better to know internally before publishing. |
| **Success metric** | Benchmark published and referenced in at least 3 GitHub discussions or Stack Overflow answers within 90 days. |

---

### - [x] 2. Document Deep Offset Pagination Degradation

| | |
|---|---|
| **Problem** | Benchmarks show EricksonLopez offset pagination is 10–17x slower than raw SQL at page 10,000 (pageSize=100). This is inherent to OFFSET SQL — not documenting this creates a trust crisis when developers discover it in production. |
| **Initiative** | Add "Performance Characteristics" section to README and `docs/benchmark.md`: (a) explain why OFFSET degrades, (b) when to use keyset instead, (c) migration guide from offset to keyset. |
| **Strategic rationale** | Honesty is the hardest-to-replicate differentiator in the developer tooling market. Proactive documentation of limitations builds long-term trust. |
| **Dependencies** | Keyset benchmark (Initiative 1) — the "solution" must be shown alongside the "problem." |
| **Risks** | Short-term: some developers may misread "offset degrades" as "the library is slow." Mitigated by framing as an inherent OFFSET SQL limitation, not a library bug. |
| **Success metric** | Zero GitHub issues of "I found your library is slow in deep pages" without a pre-documented response. |

---

### - [x] 3. HmacCursorEncoder as Secure Default

| | |
|---|---|
| **Problem** | `Base64CursorEncoder` is the default. Unsigned cursors in production APIs are a security vulnerability and a blocker for enterprise adoption. |
| **Initiative** | Change default to `HmacCursorEncoder` with a development-mode fallback key + `ILogger.LogWarning` on startup. See [ADR-0017](adr/0017-hmac-encoder-as-default.md). |
| **Strategic rationale** | "Secure by default" converts HMAC signing from an opt-in differentiator to an automatic benefit. Every new user gets cursor security without additional configuration. |
| **Dependencies** | `HmacCursorEncoder` (already production-grade). `AcceptLegacyCursors` migration mechanism (already exists). |
| **Risks** | Breaking change for users who rely on unsigned cursors in clients (bookmarks, caches). Mitigated by `AcceptLegacyCursors` and a 1-version deprecation window. |
| **Success metric** | >80% of projects using cursor pagination use signed cursors after v1.1 release. |

---

### - [x] 4. Correct Misleading "22x" Expression Compilation Claim

| | |
|---|---|
| **Problem** | The "22x faster than Gridify" claim is technically true (29ns vs 650ns in expression compilation) but misleading in context: the 621ns difference is 0.07% of a 900μs end-to-end query. An experienced developer who measures this will find the claim exaggerated. |
| **Initiative** | Update README, `docs/feature-matrix.md`, and `docs/benchmark.md` to: "Expression compilation 22x faster (29ns vs 650ns warm path); end-to-end query advantage ~5% at page 1. For deep pages, use keyset pagination." |
| **Strategic rationale** | A developer who catches a misleading benchmark claim loses trust in all other claims. Precision protects credibility. |
| **Dependencies** | None. |
| **Success metric** | No GitHub issues or discussions questioning the benchmark claim. |

---

## NEXT — 3–6 Months (Visible Differentiation)

### - [x] 5. Head-to-Head Benchmark: EricksonLopez vs. MR.EntityFrameworkCore.KeysetPagination

| | |
|---|---|
| **Problem** | The most relevant competitor for keyset pagination is `MR.EntityFrameworkCore.KeysetPagination`. No public head-to-head benchmark exists. |
| **Initiative** | BenchmarkDotNet comparison of both libraries on identical N-column keyset queries, same dataset, same EF Core version. Publish as blog post + update `docs/benchmark.md`. |
| **Strategic rationale** | Winning this benchmark (or understanding where MR wins) establishes the library as "the best technical option" for keyset. Even a loss is valuable: it guides development priorities. |
| **Dependencies** | MR.EntityFrameworkCore.KeysetPagination is open source. Benchmark infrastructure already exists. |
| **Risks** | If we lose the benchmark, do not publish. Use the data internally to improve. |
| **Success metric** | Benchmark cited in at least 5 GitHub discussions or Stack Overflow comparisons within 60 days of publication. |

---

### - [x] 6. Canonical Blog Post: "Production-grade Pagination in .NET"

| | |
|---|---|
| **Problem** | Senior .NET developers discover libraries via high-quality technical content, not NuGet search. There is no canonical article positioning this library as the reference implementation for pagination in .NET. |
| **Initiative** | 2,500–4,000 word article on dev.to or Medium covering: (a) why OFFSET degrades at scale (with benchmark data), (b) keyset pagination as the solution, (c) cursor security threat model, (d) EricksonLopez as the reference implementation. |
| **Strategic rationale** | A single high-quality technical article can generate more adoption than 6 months of feature development. Content compounds: it remains discoverable for years. |
| **Dependencies** | Initiatives 1–4 must be complete. The article references benchmarks and corrected claims. |
| **Risks** | If published before claims are corrected, a senior developer who fact-checks will find inconsistencies. Sequential dependency on Initiatives 1–4 is mandatory. |
| **Success metric** | +50% NuGet downloads in the week after publication vs. the week before. |

---

---

## LATER — 6–12 Months (Strategic Expansion)

### - [x] 8. IAsyncEnumerable Streaming Keyset

| | |
|---|---|
| **Problem** | No .NET library offers keyset pagination + `IAsyncEnumerable` streaming in the same API. ETL, data export, and event-driven pipelines need stable, efficient streaming over large datasets without explicit pagination. |
| **Initiative** | `ToKeysetStreamAsync<T>(keysetConfig, batchSize, ct)` — internally uses keyset pagination, exposes `IAsyncEnumerable<T>`. Advances cursor automatically at batch exhaustion. |
| **Strategic rationale** | Opens a new market segment (ETL, export, streaming pipelines) beyond REST API pagination. Unique in the ecosystem. |
| **Dependencies** | Keyset implementation (already exists). Verify demand via GitHub issues/discussions before committing. |
| **Risks** | Backpressure handling is complex. Memory management for long-running streams requires careful design. |
| **Success metric** | >10 GitHub issues/discussions requesting this feature before implementation begins. Post-release: adopted in >5 publicly visible projects within 90 days. |

---

### - [x] 9. LinqToDB Adapter (Demand-Gated)

| | |
|---|---|
| **Problem** | LinqToDB (~10M NuGet downloads) is used in enterprise contexts where Dapper is too verbose and EF Core is too opinionated. No pagination library supports LinqToDB natively. |
| **Initiative** | `EricksonLopez.Pagination.LinqToDB` — following the same pattern as the existing Dapper adapter. |
| **Strategic rationale** | Completes the ORM coverage story for enterprise accounts. Removes a blocking objection for LinqToDB shops. |
| **Dependencies** | Demand verification: implementation is only justified if >10 GitHub votes on the tracking issue. |
| **Risks** | Ongoing maintenance cost of an additional package. LinqToDB has its own evolution cycle; the adapter must track it. |
| **Success metric** | >10 GitHub issue votes before implementation. >100 NuGet downloads/week within 60 days of release. |

---

## What We Will NOT Build

See [docs/feature-matrix.md](feature-matrix.md) and the relevant ADRs:

| Decision | ADR |
|---|---|
| No `IEnumerable` in-memory pagination | [ADR-0013](adr/0013-no-in-memory-pagination.md) |
| No cursor encryption (AES-256) | [ADR-0014](adr/0014-no-cursor-encryption.md) |
| No HATEOAS builder | [ADR-0015](adr/0015-no-hateoas-builder.md) |
| No OData-style query language | [ADR-0016](adr/0016-no-odata-query-language.md) |

---

## Completed Initiatives

The following items were previously listed as planned but are now fully implemented and verified in the codebase.

| Initiative | Completion Evidence |
|---|---|
| **Native AOT Verification in CI** | `build.yml` step "Verify Native AOT Compatibility" runs `dotnet publish tests/EricksonLopez.Pagination.AotTest/... -r linux-x64` on every push and PR. README has `[![AOT](...)](...)` badge. See [ADR-0010](adr/0010-filtering-aot-scope-and-roadmap.md). |
