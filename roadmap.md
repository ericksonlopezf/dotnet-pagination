# Roadmap — EricksonLopez.Pagination

> Technical Implementation Roadmap · August 2026


---

## Roadmap Philosophy

This roadmap is **feature-driven, not time-driven**. Phases are defined by architectural dependencies and acceptance criteria, not calendar dates. Each phase has a clear Definition of Done.

The critical path runs through: **Core stability → Keyset correctness → Cursor security → Provider breadth → AOT → Ecosystem polish**.

---

## Phase 1: Core Stability (MVP v1.0)

### Objective
Establish a production-ready, correct, and well-tested core. All subsequent phases depend on this foundation.

### Features
| Feature | Status | Package |
|---|---|---|
| `PagedList<T>` / `CountedPagedList<T>` with factory methods | ✅ Done | Core |
| `CursorPagedList<T>` / `CountedCursorPagedList<T>` | ✅ Done | Core |
| `IPagedList<T>` / `ICursorPagedList<T>` interfaces | ✅ Done | Abstractions |
| `ICursorEncoder` interface + Base64CursorEncoder | ✅ Done | Core |
| `HmacCursorEncoder` with TTL + constant-time comparison | ✅ Done | Core |
| Cursor versioning (v1/v2) + AcceptLegacyCursors | ✅ Done | Core |
| FNV-1a deterministic keyset fingerprint | ✅ Done | Core |
| `PaginationCoreOptions` with validation | ✅ Done | Core |
| EF Core offset pagination (ToPagedListAsync) | ✅ Done | EFCore |
| EF Core count-less offset (ToPagedListWithoutCountAsync) | ✅ Done | EFCore |
| EF Core keyset pagination (KeysetBuilder<T>) | ✅ Done | EFCore |
| EF Core keyset bounding condition optimization | ✅ Done | EFCore |
| EF Core row-value predicate (PostgreSQL/SQL Server) | ✅ Done | EFCore |
| EF Core deferred join (ToPagedListDeferredAsync) | ✅ Done | EFCore |
| EF Core filter DSL (ApplyFilter) | ✅ Done | EFCore |
| EF Core dynamic sort (ApplySort) | ✅ Done | EFCore |
| ASP.NET Core model binders | ✅ Done | AspNetCore |
| ASP.NET Core response wrappers | ✅ Done | AspNetCore |
| Dapper offset pagination | ✅ Done | Dapper |
| Dapper keyset builder (DapperKeysetBuilder<T>) | ✅ Done | Dapper |
| PostgreSQL approximate count | ✅ Done | EFCore |
| Roslyn analyzers PAG001-007 | ✅ Done | Analyzers |
| Source generators for AOT cursor decoders | ✅ Done | SourceGenerators |
| MongoDB offset + cursor pagination | ✅ Done | MongoDB |
| Cosmos DB cursor pagination | ✅ Done | Cosmos |
| OpenAPI / Swagger transformer | ✅ Done | OpenApi |
| Blazor PagedListPager component | ✅ Done | Blazor |
| gRPC Protobuf converters | ✅ Done | Grpc |

### Tests
- Unit tests: >90% branch coverage
- Mutation testing: Stryker.NET ≥95% on Core packages (EFCore and MongoDB excluded per ADR-0004)
- Integration tests: PostgreSQL, SQL Server, SQLite, MongoDB via Testcontainers
- AOT publish test in CI: `dotnet publish -r linux-x64 --aot`

### Benchmarks
- Offset vs keyset at 100K, 1M, 5M rows
- Cursor encode/decode throughput
- Expression compilation (warm/cold)

### ADRs Required
- ADR-0001 through ADR-0020 (all completed)

### Acceptance Criteria
- [x] All 12 packages published to NuGet.org
- [x] Zero `dotnet publish --aot` warnings in core/abstractions
- [x] Stryker.NET mutation score >95% on core
- [x] BenchmarkDotNet benchmark suite published in `docs/benchmark.md`
- [x] README benchmark claims verified against actual benchmark output

### Definition of Done
All 12 packages pass CI, AOT verification passes, Stryker threshold met, NuGet published.

---

## Phase 2: Hardening and Correctness (v1.1)

### Objective
Fix known gaps, correct documentation claims, and improve correctness under adversarial conditions.

### Features
| Feature | Priority | Notes |
|---|---|---|
| HMAC as secure default (ADR-0017) | ✅ Done | Development key fallback + startup warning (ADR-0021) |
| PAG008 analyzer: warn on explicit Base64CursorEncoder | ✅ Done | Complement to ADR-0017 (ADR-0031) |
| SQL Server approximate count (sys.dm_db_partition_stats) | ✅ Done | Parity with PostgreSQL approx count |
| Filter DSL: AOT-safe IFilterProvider<T> interface | ✅ Done | ADR-0010 / ADR-0032 (ADR-0023) |
| MongoDB keyset pagination (ObjectId-based) | ✅ Done | Keyset with ObjectId support (ADR-0033) |
| Keyset latency benchmark vs raw SQL baseline | ✅ Done | Benchmark isolating SQL from codec latency |
| Deep offset degradation documentation update | ✅ Done | docs/deep-offset-degradation.md published |
| "22x faster" claim correction | ✅ Done | performance.md and docs corrected |

### Tests
- New: property-based tests via FsCheck for cursor encode/decode round-trips
- New: adversarial cursor tests (malformed, expired, wrong version, tampered)
- New: multi-instance cursor fingerprint tests (simulate pod restart)

### ADRs
- ADR-0017: HMAC as secure default ✅ Accepted
- ADR-0031: PAG008 Base64 encoder warning ✅ Accepted
- ADR-0032: Filter AOT safe path (IFilterProvider<T>) ✅ Accepted
- ADR-0033: MongoDB ObjectId-based keyset pagination ✅ Accepted

### Risks
- **HMAC as default is a breaking change**: Existing Base64 cursors in client bookmarks/caches become invalid. Mitigated by `AcceptLegacyCursors = true` migration window.

### Acceptance Criteria
- [x] HmacCursorEncoder is the default in `AddPagination()` without explicit configuration
- [x] Startup warning logged when development key is in use
- [x] PAG008 analyzer implemented and documented
- [x] MongoDB keyset tests passing with Testcontainers / Mock provider

---

## Phase 3: Performance and AOT Completeness (v1.2)

### Objective
Complete AOT story for filtering; publish verified benchmarks; eliminate remaining performance gaps.

### Features
| Feature | Priority | Notes |
|---|---|---|
| AOT-safe filter source generator | ✅ Done | Generator emits strongly typed IFilterProvider<T> (ADR-0032) |
| IAsyncEnumerable streaming keyset | ✅ Done | AsKeysetStreamAsync extension (ADR-0034) |
| Benchmark: keyset at 10M rows (PostgreSQL) | ✅ Done | EFCoreTenMillionKeysetBenchmark |
| Benchmark: cursor encode/decode vs raw Base64 | ✅ Done | CursorCodecBenchmark |
| stackalloc cursor path: .NET 10 span improvements | ✅ Done | Span Base64Url decode in HmacCursorEncoder |
| LinqToDB adapter | ✅ Done | Validated adapter implementation (ADR-0022) |

### Tests
- AOT publish test must pass with filter source generator enabled
- Streaming keyset integration test: 10M row dataset via Testcontainers
- Benchmark regression threshold: keyset page 1 < 5ms on PostgreSQL (100K rows, pageSize=100)

### ADRs
- ADR-0034: IAsyncEnumerable streaming keyset design ✅ Accepted
- ADR-0032: AOT filter source generator approach (IFilterProvider<T>) ✅ Accepted

### Acceptance Criteria
- [x] Filter DSL passes AOT publish test when source generator is used
- [x] Streaming keyset benchmark published
- [x] No benchmark regression from v1.1 (keyset: ±5% tolerance)

---

## Phase 4: Enterprise Expansion (v2.0)

### Objective
Expand to enterprise use cases while maintaining architectural purity.

### Features
| Feature | Priority | Notes |
|---|---|---|
| LinqToDB adapter | ✅ Done | Package documentation and hardening |
| Cursor replay protection (nonce store) | ✅ Done | ICursorReplayStore and nonce verification (ADR-0035) |
| GraphQL cursor interop package | ✅ Done | Relay Connections mapping & guide (ADR-0025) |
| Oracle pagination provider | ✅ Done | Oracle approx count & keyset optimization |
| Multi-cursor (parallel keyset) | ✅ Done | SplitKeysetPartitionsAsync (ADR-0036) |
| Cursor expiration observability (ILogger events) | ✅ Done | PaginationLogEvents (1001, 1002, 1003) |
| Pagination metrics (OpenTelemetry) | ✅ Done | PaginationMetrics System.Diagnostics.Metrics (ADR-0037) |

### Breaking Changes
- v2.0 may include breaking changes to `ICursorEncoder` interface if replay protection is added
- Versioned under SemVer; v1.x upgrade guide provided

### Acceptance Criteria
- [x] All v1.x consumers can upgrade to v2.0 with documented migration steps
- [x] No v2.0 feature violates core boundaries (no HTTP in core, no database in core)

---

## Release Gates

### Alpha Gate
- All packages compile
- Core unit tests pass
- AOT verification passes
- No public API marked [Experimental] leaks into stable paths

### Beta Gate
- Integration tests pass (PostgreSQL + SQL Server via Testcontainers)
- Mutation score >90%
- BenchmarkDotNet results match expected order of magnitude
- All public APIs have XML documentation
- Zero [RequiresUnreferencedCode] in core/abstractions packages

### Release Candidate Gate
- Mutation score >95%
- All benchmark claims verified against published data
- All ADRs complete and reviewed
- NuGet package validation (`EnablePackageValidation`) passes
- API baseline compatibility verified (no breaking changes from previous RC)
- Stryker threshold enforced in CI (`--threshold-break`)

### 1.0 Stable Gate
- All RC gates met
- Security review of HmacCursorEncoder complete
- Documentation published (README, API reference, cookbook, architecture, ADRs)
- CHANGELOG updated
- GitHub release created with release notes
- NuGet packages published with valid package icon, license, description, and tags

---

## Critical Path

```
FNV-1a fingerprint (ADR-0007)
    └── Cursor versioning (ADR-0006)
            └── HMAC encoder (ADR-0017)
                    └── Keyset pagination (ADR-0003, ADR-0019)
                            └── EF Core provider
                                    └── Dapper provider
                                            └── AOT source generator
                                                    └── 1.0 Release
```

## High-Risk Features

| Feature | Risk | Mitigation |
|---|---|---|
| HMAC as default | Breaking: existing Base64 cursors invalid | AcceptLegacyCursors migration window |
| FNV-1a fingerprint | One-time cursor invalidation on upgrade | Document in CHANGELOG; TTL expires naturally |
| Filter DSL AOT source generator | Complex Roslyn generator; may not cover all entity patterns | IFilterProvider<T> fallback documented |
| IAsyncEnumerable streaming keyset | Backpressure handling; memory management for long streams | Demand-gate; design review before implementation |
| LinqToDB adapter | LinqToDB evolution cycle; maintenance burden | >10 votes gate; separate package; optional |

## Parallelizable Work

The following can be developed in parallel without blocking each other:
- Blazor component improvements (does not depend on EFCore/Dapper)
- OpenAPI / Swagger transformer (does not depend on query execution)
- gRPC Protobuf converters (depends on Abstractions only)
- Roslyn analyzers (compile-time; no runtime dependency)
- BenchmarkDotNet suite (reads library; no changes required to library)
