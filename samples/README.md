# EricksonLopez.Pagination — Showcase & Reference Implementation

**EricksonLopez.Pagination** is a high-performance, enterprise-grade pagination library for .NET supporting both *Offset* and *Keyset (Cursor)* pagination across EF Core, Dapper, MongoDB, Azure Cosmos DB, LinqToDB, and gRPC.

---

## Showcase Demo Application (`DemoApp`)

The `DemoApp` is a runnable reference implementation structured in Clean Architecture layers:
- `DemoApp.Domain`: Domain models (`Product`)
- `DemoApp.Application`: CQRS queries and handlers (MediatR)
- `DemoApp.Infrastructure`: EF Core `ApplicationDbContext` with in-memory SQLite and benchmark services
- `DemoApp.Api`: Minimal API endpoints organized by progressive complexity levels

---

## Showcase Progressive Learning Path (Levels 0 to 10)

| Level | Title | Code File | Documentation Guide | Focus Areas |
|---|---|---|---|---|
| **Level 0** | Conceptual | [`Level0Conceptual.cs`](DemoApp/DemoApp.Api/Levels/Level0Conceptual.cs) | [`level-00-conceptual.md`](docs/showcase/level-00-conceptual.md) | What is the library, Offset vs Keyset, tradeoffs, comparison with alternatives |
| **Level 1** | Quick Start | [`Level1QuickStart.cs`](DemoApp/DemoApp.Api/Levels/Level1QuickStart.cs) | [`level-01-quick-start.md`](docs/showcase/level-01-quick-start.md) | Minimal API setup, DI registration, `ToPagedListAsync`, `ToPagedResponse` |
| **Level 2** | Full Configuration | [`Level2FullConfiguration.cs`](DemoApp/DemoApp.Api/Levels/Level2FullConfiguration.cs) | [`level-02-full-configuration.md`](docs/showcase/level-02-full-configuration.md) | `FilterParameters`, `SortParameters`, `ApplyFilter`, `ApplySort`, validation |
| **Level 3** | Real Use Cases | [`Level3RealUseCases.cs`](DemoApp/DemoApp.Api/Levels/Level3RealUseCases.cs) | [`level-03-real-use-cases.md`](docs/showcase/level-03-real-use-cases.md) | Keyset pagination, counted vs countless, `Map()`, `LazyMap()`, manual factories |
| **Level 4** | Advanced Integration | [`Level4AdvancedIntegration.cs`](DemoApp/DemoApp.Api/Levels/Level4AdvancedIntegration.cs) | [`level-04-advanced-integration.md`](docs/showcase/level-04-advanced-integration.md) | CQRS / MediatR, HATEOAS relative links, `AddPaginationValidation`, SQL projection |
| **Level 5** | Processing | [`Level5Processing.cs`](DemoApp/DemoApp.Api/Levels/Level5Processing.cs) | [`level-05-processing.md`](docs/showcase/level-05-processing.md) | Streaming `IAsyncEnumerable`, `ToPagedListBatchedAsync`, Keyset streaming, `CancellationToken` |
| **Level 6** | Error Handling | [`Level6ErrorHandling.cs`](DemoApp/DemoApp.Api/Levels/Level6ErrorHandling.cs) | [`level-06-error-handling.md`](docs/showcase/level-06-error-handling.md) | `InvalidPaginationCursorException`, `ExpiredPaginationCursorException`, Replay Protection (`ICursorReplayStore`), `PaginationExceptionHandler` |
| **Level 7** | Scalability | [`Level7Scalability.cs`](DemoApp/DemoApp.Api/Levels/Level7Scalability.cs) | [`level-07-scalability.md`](docs/showcase/level-07-scalability.md) | Dapper single/dual result sets, Dapper streaming, `DapperKeysetBuilder<T>` |
| **Level 8** | Customization & Security | [`Level8Customization.cs`](DemoApp/DemoApp.Api/Levels/Level8Customization.cs) | [`level-08-customization-security.md`](docs/showcase/level-08-customization-security.md) | Custom `ICursorEncoder`, `HmacCursorEncoder` (with/without TTL), `ICursorReplayStore`, `FilterableAttribute` |
| **Level 9** | Complex Architecture | [`Level9Extensions.cs`](DemoApp/DemoApp.Api/Levels/Level9Extensions.cs) | [`level-09-complex-architecture.md`](docs/showcase/level-09-complex-architecture.md) | Multi-column Keyset, descending keyset, backward pagination, `SplitKeysetPartitionsAsync`, PostgreSQL approximate count |
| **Level 10** | Enterprise Best Practices | [`Level10EnterpriseArchitecture.cs`](DemoApp/DemoApp.Api/Levels/Level10EnterpriseArchitecture.cs) | [`level-10-enterprise-practices.md`](docs/showcase/level-10-enterprise-practices.md) | `ToPagedResult` with deterministic ETag (304 Not Modified), `OutputCaching`, endpoint `maxPageSize`, `PaginationMetrics` & `PaginationDiagnostics` |
| **Level 11** | Comprehensive API Verification | [`Level11ComprehensiveApiCoverage.cs`](DemoApp/DemoApp.Api/Levels/Level11ComprehensiveApiCoverage.cs) | [`api-inventory.md`](docs/api-inventory.md) | Runtime contracts and end-to-end execution testing all 104 public APIs across all 15 Core & Infrastructure packages |

---

## Running the Showcase

```bash
cd samples/DemoApp/DemoApp.Api
dotnet run
```

Running without arguments executes the self-testing verification suite across all 104 public APIs (exiting with code 0).  
To run as an interactive web server hosting the Swagger UI and REST endpoints:

```bash
dotnet run -- --server
```

Then open `http://localhost:5000/swagger` or navigate to `/health`.

---

## Documentation Index

- [Public API Inventory](docs/api-inventory.md)
- [Functional Architecture Map](docs/functional-map.md)
- [Conceptual Map](docs/conceptual-map.md)
- [Quick Start Guide](docs/quick-start.md)
- [Getting Started Guide](docs/getting-started.md)
- [Cookbook & Recipes (22 Production Recipes)](docs/cookbook.md)
- [Architecture Diagrams (12 Mermaid Diagrams)](docs/diagrams.md)
- [Architecture & Design Guide](docs/architecture-guide.md)
- [Performance & Optimization Guide](docs/performance-guide.md)
- [Best Practices Guide](docs/best-practices.md)
- [Troubleshooting & Diagnostics](docs/troubleshooting.md)
- [Migration Guide](docs/migration-guide.md)
- [Frequently Asked Questions (FAQ)](docs/faq.md)
- [Advanced Scenarios](docs/advanced-scenarios.md)
- [API Reference Index](docs/api-reference/index.md)
  - [Abstractions](docs/api-reference/abstractions.md)
  - [Core Library](docs/api-reference/core.md)
  - [ASP.NET Core Integration](docs/api-reference/aspnetcore.md)
  - [Entity Framework Core](docs/api-reference/efcore.md)
  - [Dapper Micro-ORM](docs/api-reference/dapper.md)
  - [NoSQL & Specialized](docs/api-reference/nosql.md)
  - [Extensions & Ecosystem](docs/api-reference/extensions.md)
- [CHANGELOG Release Example](docs/changelog-example.md)

