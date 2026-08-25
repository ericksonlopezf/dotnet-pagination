# Cookbook — Practical Recipes for EricksonLopez.Pagination

A collection of end-to-end, production-ready recipes based entirely on the public API inventory of `EricksonLopez.Pagination`.

---

## Recipe 1: Basic Offset Pagination in ASP.NET Core Minimal APIs

### Problem
You need to expose a paginated REST endpoint returning an offset-paginated JSON array with clean metadata.

### Solution
```csharp
app.MapGet("/api/products", async (
    [AsParameters] PaginationParameters pagination,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});
```

### Explanation
- `[AsParameters] PaginationParameters` binds `page` and `pageSize` automatically from query parameters.
- `ToPagedListAsync` executes the query and generates an `IPagedList<Product>`.
- `ToPagedResponse` serializes the items and adds pagination metadata (`page`, `pageSize`, `totalPages`, `totalCount`, `hasNextPage`, `hasPreviousPage`).

---

## Recipe 2: Keyset (Cursor) Pagination for High-Traffic Feeds

### Problem
In large tables (>100,000 rows), `OFFSET` queries degrade in performance. You need $O(\log N)$ constant latency keyset pagination.

### Solution
```csharp
app.MapGet("/api/feed", async (
    [AsParameters] CursorPaginationParameters cursor,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var page = await db.Products
        .Keyset(cursor)
        .Descending(p => p.CreatedAt)
        .Ascending(p => p.Id) // Unique tie-breaker
        .ToCursorPagedListAsync(cancellationToken: ct);

    return Results.Ok(page.ToCursorPagedResponse(p => $"{p.CreatedAt:O}|{p.Id}"));
});
```

### Best Practices
- Always append a unique tie-breaker column (e.g. `.Ascending(p => p.Id)`) as the final column.
- Index the keyset columns in the database: `CREATE INDEX IX_Products_CreatedAt_Id ON Products(CreatedAt DESC, Id ASC)`.

---

## Recipe 3: Secure Dynamic Filtering and Multi-Column Sorting

### Problem
Clients need to dynamically filter and sort on arbitrary fields without exposing the application to SQL injection.

### Solution
```csharp
app.MapGet("/api/products/search", async (
    [AsParameters] PaginationParameters pagination,
    [AsParameters] FilterParameters filter,
    [AsParameters] SortParameters sort,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .ApplyFilter(filter)
        .ApplySort(sort, defaultSort: p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});
```

### Filter Syntax
- Equality: `name=Laptop`
- Comparison: `price>=100`, `price<500`
- Contains: `name~=Pro`
- Multiple: `filter=name~=Pro,price>=100`

---

## Recipe 4: Cryptographically Signed Cursors with Expiration (TTL)

### Problem
Prevent users from forging cursors and invalidate cursors older than a specified duration.

### Solution
```csharp
// In Program.cs:
builder.Services.AddSingleton<ICursorEncoder>(sp =>
    new HmacCursorEncoder(
        secretKey: builder.Configuration["Pagination:SecretKey"]!,
        timeToLive: TimeSpan.FromMinutes(15),
        clockSkewTolerance: TimeSpan.FromSeconds(30)));

// In Endpoint:
app.MapGet("/api/secure-feed", async (
    [AsParameters] CursorPaginationParameters cursor,
    ICursorEncoder encoder,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var page = await db.Products
        .Keyset(cursor, cursorEncoder: encoder)
        .Ascending(p => p.Id)
        .ToCursorPagedListAsync(cancellationToken: ct);

    return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
});
```

---

## Recipe 5: Preventing Cursor Replay Attacks (`ICursorReplayStore`)

### Problem
Prevent malicious consumers from executing the exact same cursor multiple times.

### Solution
```csharp
var replayStore = new InMemoryCursorReplayStore();
using var replayEncoder = new HmacCursorEncoder(
    secretKey: "Your32ByteSecretKeyForHmacSigning12345",
    replayStore: replayStore,
    timeToLive: TimeSpan.FromMinutes(5));

try
{
    var page = await db.Products
        .Keyset(cursor, cursorEncoder: replayEncoder)
        .Ascending(p => p.Id)
        .ToCursorPagedListAsync(cancellationToken: ct);

    return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
}
catch (ReplayedPaginationCursorException ex)
{
    return Results.Problem(
        title: "Cursor Replay Detected",
        detail: $"Cursor nonce {ex.Nonce} has already been consumed.",
        statusCode: StatusCodes.Status409Conflict);
}
```

---

## Recipe 6: Micro-ORM Keyset Pagination with `DapperKeysetBuilder<T>`

### Problem
Implement multi-column keyset pagination using Dapper and raw SQL.

### Solution
```csharp
app.MapGet("/api/dapper-keyset", async (
    [AsParameters] CursorPaginationParameters cursor,
    IDbConnection connection) =>
{
    var page = await new DapperKeysetBuilder<ProductDto>(connection, cursor)
        .Select("Id, Name, Price")
        .From("Products")
        .OrderBy("Price", SortDirection.Ascending)
        .ThenBy("Id", SortDirection.Ascending)
        .WithCursorColumns(
            p => p.Price.ToString(CultureInfo.InvariantCulture),
            p => p.Id.ToString(CultureInfo.InvariantCulture))
        .WithCursorDecoder(
            parts => decimal.Parse(parts[0], CultureInfo.InvariantCulture),
            parts => int.Parse(parts[1], CultureInfo.InvariantCulture))
        .UseDialect(DatabaseDialect.PostgreSql)
        .ExecuteAsync();

    return Results.Ok(page.ToCursorPagedResponse(p => $"{p.Price}|{p.Id}"));
});
```

---

## Recipe 7: Background Batch Processing with `ToPagedListBatchedAsync`

### Problem
Process an entire table of millions of rows in memory-efficient chunks without loading all rows at once.

### Solution
```csharp
public async Task ReindexAllProductsAsync(ApplicationDbContext db, CancellationToken ct)
{
    const int batchSize = 500;

    await foreach (var batch in db.Products
        .OrderBy(p => p.Id)
        .ToPagedListBatchedAsync(batchSize, cancellationToken: ct))
    {
        foreach (var product in batch)
        {
            await ProcessProductAsync(product, ct);
        }
    }
}
```

---

## Recipe 8: Parallel Keyset Partitioning for Concurrent Workers

### Problem
Split a dataset across $N$ worker threads to process in parallel without data overlap.

### Solution
```csharp
public async Task ProcessInParallelAsync(ApplicationDbContext db, int workerCount, CancellationToken ct)
{
    var partitions = await db.Products
        .SplitKeysetPartitionsAsync(p => p.Id, partitionCount: workerCount, cancellationToken: ct);

    var tasks = partitions.Select(async partition =>
    {
        using var scope = serviceProvider.CreateScope();
        var localDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = localDb.Products.AsQueryable();
        if (partition.LowerBound.HasValue) query = query.Where(p => p.Id >= partition.LowerBound.Value);
        if (partition.UpperBound.HasValue) query = query.Where(p => p.Id <= partition.UpperBound.Value);

        var items = await query.ToListAsync(ct);
        await ProcessWorkerChunkAsync(items, ct);
    });

    await Task.WhenAll(tasks);
}
```

---

## Recipe 9: HTTP Caching with Deterministic ETags & 304 Not Modified

### Problem
Save bandwidth by returning `304 Not Modified` when page contents have not changed.

### Solution
```csharp
app.MapGet("/api/products/etag", async (
    [AsParameters] PaginationParameters pagination,
    HttpContext httpContext,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return pagedList.ToPagedResult(httpContext.Request, maxAge: TimeSpan.FromMinutes(5));
});
```

---

## Recipe 10: Global Exception Handling with `PaginationExceptionHandler`

### Problem
Translate pagination exceptions into standardized RFC 7807 ProblemDetails responses globally.

### Solution
```csharp
// Program.cs
builder.Services.AddExceptionHandler<PaginationExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
```

---

## Recipe 11: FilterParameters DSL — OR Conditions Within a Field

### Problem
You need to filter for records matching **any one of multiple values** on the same field (OR semantics), while keeping AND semantics between different fields.

### Solution
Use the pipe character `|` within a filter clause to specify OR conditions. Multiple clauses separated by commas are always AND-combined.

```csharp
// GET /api/products?filter=name~=Laptop|name~=Tablet,price>=100
// Returns products whose name contains "Laptop" OR "Tablet", AND price >= 100

app.MapGet("/api/products", async (
    [AsParameters] PaginationParameters pagination,
    string? filter,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var filterParams = FilterParameters.From(filter);

    var pagedList = await db.Products
        .ApplyFilter(filterParams)
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});
```

### Filter DSL — OR Syntax Reference

| Example | Meaning |
|---------|---------|
| `name=Laptop\|name=Tablet` | name = 'Laptop' OR name = 'Tablet' |
| `name~=Pro\|name~=Max` | name LIKE '%Pro%' OR name LIKE '%Max%' |
| `status=active\|status=pending,price>=100` | (status='active' OR status='pending') AND price >= 100 |

### Common Mistakes
- Do not add spaces around `|` — the parser treats them literally.
- The pipe `|` separates OR conditions; comma `,` separates AND conditions.

---

## Recipe 12: Cursor-Based Pagination with Composite Keys (`RawCursorValue`)

### Problem
Your cursor key is composite (multiple columns) or uses a format that `IFormattable` cannot produce correctly (e.g., `Name|Id`).

### Solution
Use the `Func<T, RawCursorValue>` overload of `ToCursorPagedResponse`. `RawCursorValue` is a type-safe wrapper that signals "this string is already the raw cursor value — skip any culture or formatting transformation."

```csharp
// GET /api/products/composite-cursor?first=10
app.MapGet("/api/products/composite-cursor", async (
    [AsParameters] CursorPaginationParameters cursor,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var page = await db.Products
        .Keyset(cursor)
        .Ascending(p => p.Name)
        .Ascending(p => p.Id) // unique tiebreaker
        .ToCursorPagedListAsync(cancellationToken: ct);

    // Overload A: Func<T, TKey> — use for single typed keys (int, Guid, etc.)
    // IFormattable is used automatically for culture-safe formatting.
    //   page.ToCursorPagedResponse(p => p.Id)

    // Overload B: Func<T, RawCursorValue> — use for composite or custom-formatted keys.
    // The string you build is used as-is (no further transformation).
    return Results.Ok(page.ToCursorPagedResponse(
        rawCursorSelector: p => new RawCursorValue($"{p.Name}|{p.Id}")));
});
```

### When to Use `RawCursorValue` vs. typed `TKey`

| Scenario | Recommended Overload |
|----------|---------------------|
| Single `int` / `long` / `Guid` key | `Func<T, TKey>` — automatic IFormattable |
| Single `DateTimeOffset` key | `Func<T, TKey>` — automatic InvariantCulture |
| Composite key (two or more columns) | `Func<T, RawCursorValue>` — manual formatting |
| Custom format (epoch milliseconds, etc.) | `Func<T, RawCursorValue>` — manual formatting |

### Best Practices
- Always include a strictly unique column as the last component of a composite cursor.
- Use `CultureInfo.InvariantCulture` when formatting numeric or date parts: `$"{p.Price.ToString(null, CultureInfo.InvariantCulture)}|{p.Id}"`.

---

## Recipe 13: AOT-Safe ETag Generation with `PaginationETagOptions.CustomETagFactory`

### Problem
In Native AOT or trimmed deployments, `JsonSerializer` (used by the default ETag strategy) may throw `NotSupportedException`. You need a deterministic ETag without serialization.

### Solution
Provide a `CustomETagFactory` delegate in `PaginationETagOptions`. The factory receives the boxed response object and returns a raw string that becomes the ETag value.

```csharp
app.MapGet("/api/products", async (
    [AsParameters] PaginationParameters pagination,
    HttpContext httpContext,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    var response = pagedList.ToPagedResponse();

    // AOT-safe: ETag from Page + PageSize + first/last Id — no JsonSerializer
    var aotOptions = new PaginationETagOptions
    {
        CustomETagFactory = obj =>
        {
            if (obj is PagedResponse<Product> r && r.Items.Count > 0)
                return $"{r.Page}:{r.PageSize}:{r.Items[0].Id}-{r.Items[^1].Id}";
            return "empty";
        }
    };

    bool notModified = response.ApplyETagHeaders(httpContext, maxAge: TimeSpan.FromMinutes(5), etagOptions: aotOptions);
    if (notModified)
        return Results.StatusCode(304);

    return Results.Ok(response);
});
```

### ETag Strategies Comparison

| Strategy | Deterministic | AOT-Safe | Cost |
|----------|--------------|----------|------|
| Default (JsonSerializer) | ✅ | ❌ | Medium |
| First/Last ID hash | ✅ | ✅ | Low |
| Row version / UpdatedAt | ✅ | ✅ | Requires DB column |
| Timestamp | ⚠️ Approximate | ✅ | Very low |

### Common Mistakes
- The factory receives `object` (the boxed `PagedResponse<T>`). Cast it safely before accessing typed properties.
- The returned string is normalized to RFC 7232 format (quoted). Do not double-quote the return value.

---

## Recipe 14: Group-Level Pagination Validation with `RouteGroupBuilder`

### Problem
You have a group of export/reporting endpoints that must all enforce a strict page size limit. You want a single protection declaration instead of annotating each endpoint individually.

### Solution
Call `AddPaginationValidation()` on the `RouteGroupBuilder` returned by `MapGroup()`. All endpoints in the group inherit the filter automatically.

```csharp
// Declare the group once — all endpoints inside are protected
var exportGroup = app.MapGroup("/api/export")
    .WithTags("Export")
    .AddPaginationValidation();  // <- protects ALL endpoints in this group

exportGroup.MapGet("/products", async (
    [AsParameters] PaginationParameters pagination,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, maxPageSize: 100, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});

exportGroup.MapGet("/summary", async (
    [AsParameters] PaginationParameters pagination,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .OrderBy(p => p.Name)
        .ToPagedListAsync(pagination, maxPageSize: 100, cancellationToken: ct);

    return Results.Ok(pagedList.Map(p => new { p.Id, p.Name }).ToPagedResponse());
});
// Both /api/export/products and /api/export/summary return HTTP 400 if pageSize > MaxPageSize
```

### Overloads

| Target | Method |
|--------|--------|
| Individual endpoint | `endpoint.MapGet(...).AddPaginationValidation()` |
| All endpoints in a group | `app.MapGroup(path).AddPaginationValidation()` |

### Best Practices
- Use group-level validation for functional areas (export, admin, reporting) where you want uniform enforcement.
- Use endpoint-level validation for one-off overrides where only one endpoint needs specific page size enforcement.

---

## Recipe 15: Parallel Keyset Partitioning for Background ETL Workers

### Problem
You need to process a 50-million-row database table in background workers. Offset pagination causes lock contention and severe degradation. You need to partition the keyset range evenly across $N$ parallel background workers.

### Solution
Use `PartitionByKeysetAsync` to split the primary key space into independent, non-overlapping partitions processed concurrently:

```csharp
// 1. Partition the dataset into 8 balanced keyset ranges
var partitions = await db.Orders
    .PartitionByKeysetAsync(
        keySelector: o => o.Id,
        partitionCount: 8,
        cancellationToken: ct);

// 2. Process each partition in parallel with flat O(log N) seek speed
await Parallel.ForEachAsync(partitions, async (partition, token) =>
{
    using var scope = serviceProvider.CreateScope();
    var scopedDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var workerCursor = new CursorPaginationParameters { PageSize = 500 };
    bool hasMore = true;

    while (hasMore)
    {
        var page = await scopedDb.Orders
            .Where(o => o.Id >= partition.MinKey && o.Id <= partition.MaxKey)
            .Keyset(workerCursor)
            .Ascending(o => o.Id)
            .ToCursorPagedListAsync(cancellationToken: token);

        await ProcessBatchAsync(page.Items, token);

        hasMore = page.HasNextPage;
        workerCursor.After = page.NextCursor;
    }
});
```

---

## Recipe 16: Native AOT Cursor Decoding with `ICursorDecoderRegistry`

### Problem
When publishing with `dotnet publish -r linux-x64 -c Release /p:PublishAot=true`, reflection-based cursor deserialization causes runtime trimmer warnings or exceptions.

### Solution
Register typed cursor decoders explicitly at startup using `ICursorDecoderRegistry`:

```csharp
// 1. Register with Pagination options in Program.cs
builder.Services.AddPagination(options =>
{
    var registry = new InMemoryCursorDecoderRegistry();
    
    // Register decoders for the types used in your composite keysets
    registry.Register<int>(raw => int.Parse(raw, CultureInfo.InvariantCulture));
    registry.Register<Guid>(raw => Guid.Parse(raw));
    registry.Register<DateTimeOffset>(raw => DateTimeOffset.Parse(raw, null, DateTimeStyles.RoundtripKind));

    options.CursorDecoderRegistry = registry;
});
```

> **Tip:** You can also use `EricksonLopez.Pagination.SourceGenerators` to automatically generate and register these decoders at compile time via `[ModuleInitializer]`.

---

## Recipe 17: AOT-Safe Dynamic Filtering with `IFilterProvider<T>` and Source Generators

### Problem
Standard `ApplyFilter("name~=John,age>=18")` uses reflection and expression tree compilation incompatible with strict Native AOT trimming.

### Solution
Generate an AOT-safe filter provider using `[GenerateFilterProvider]` or implement `IFilterProvider<T>`:

```csharp
[GenerateFilterProvider]
public class Customer
{
    public int Id { get; set; }
    [Filterable]
    public string Name { get; set; } = string.Empty;
    [Filterable]
    public int Age { get; set; }
}

// In Endpoint:
app.MapGet("/api/customers", async (
    [AsParameters] PaginationParameters pagination,
    [AsParameters] FilterParameters filter,
    IFilterProvider<Customer> filterProvider,
    ApplicationDbContext db) =>
{
    var query = db.Customers
        .ApplyFilter(filterProvider, filter) // Zero reflection, 100% AOT-safe
        .OrderBy(c => c.Id);

    return Results.Ok(await query.ToPagedListAsync(pagination));
});
```

---

## Recipe 18: Zero-Downtime Cursor Migration via FNV-1a Keyset Schema Fingerprint

### Problem
During rolling deployments, pod $A$ might produce a cursor for a 2-column keyset (`CreatedAt`, `Id`), while pod $B$ (deploying new code) expects a 3-column keyset (`TenantId`, `CreatedAt`, `Id`). Passing the old cursor to pod $B$ causes runtime SQL exceptions.

### Solution
The library computes an internal FNV-1a schema fingerprint (`GetKeysetSchemaFingerprint()`) embedded in the cursor header. If the schema changes between deployments, `InvalidPaginationCursorException` is thrown cleanly instead of corrupting query execution:

```csharp
try
{
    var page = await db.Orders
        .Keyset(cursor)
        .Ascending(o => o.CreatedAt)
        .Ascending(o => o.Id)
        .ToCursorPagedListAsync();
}
catch (InvalidPaginationCursorException)
{
    // Cleanly instruct client to reset cursor to the beginning
    return Results.Problem(
        title: "Cursor Incompatible",
        detail: "The pagination cursor schema has evolved. Please restart pagination from the first page.",
        statusCode: StatusCodes.Status400BadRequest);
}
```

---

## Recipe 19: Multi-Node Cursor Replay Protection in Kubernetes (`ICursorReplayStore`)

### Problem
`InMemoryCursorReplayStore` is designed for single-node and local development. In a multi-pod Kubernetes cluster, a cursor replayed on pod $B$ will not be detected if it was only recorded in the memory of pod $A$.

### Solution
Implement `ICursorReplayStore` backed by a distributed cache like Redis:

```csharp
public class RedisCursorReplayStore : ICursorReplayStore
{
    private readonly IDatabase _redis;

    public RedisCursorReplayStore(IConnectionMultiplexer connection)
    {
        _redis = connection.GetDatabase();
    }

    public async ValueTask<bool> TryAcquireAsync(string nonce, TimeSpan ttl, CancellationToken ct = default)
    {
        // Set NX (Not Exists) with TTL in Redis
        return await _redis.StringSetAsync($"cursor:nonce:{nonce}", "1", ttl, When.NotExists);
    }
}

// In Program.cs:
builder.Services.AddSingleton<ICursorReplayStore, RedisCursorReplayStore>();
builder.Services.AddSingleton<ICursorEncoder>(sp =>
    new HmacCursorEncoder(
        secretKey: builder.Configuration["Pagination:SecretKey"]!,
        replayStore: sp.GetRequiredService<ICursorReplayStore>(),
        timeToLive: TimeSpan.FromMinutes(10)));
```

---

## Recipe 20: Field Aliasing & Custom Filter Operators

### Problem
You want API query parameters (`?filter=customer_name=John,balance%=100`) to decouple from internal database entity property names and support custom domain operators (e.g. modulus, geo-distance, full-text).

### Solution
Use `[Filterable(Name = "alias")]` for field aliasing and implement `IFilterOperatorProvider<T>`:

```csharp
public class Account
{
    public int Id { get; set; }

    [Filterable(Name = "customer_name")]
    public string OwnerFullName { get; set; } = string.Empty;

    [Filterable(Name = "balance")]
    public decimal CurrentBalance { get; set; }
}

public class AccountCustomOperatorProvider : IFilterOperatorProvider<Account>
{
    public IReadOnlyDictionary<string, FilterOperatorHandler<Account>> Operators =>
        new Dictionary<string, FilterOperatorHandler<Account>>
        {
            // Custom operator: ?filter=balance%=100 (balance is multiple of 100)
            ["%="] = (propExpr, rawValue) =>
            {
                if (decimal.TryParse(rawValue, out var step))
                {
                    var modulo = Expression.Modulo(propExpr, Expression.Constant(step));
                    return Expression.Equal(modulo, Expression.Constant(0m));
                }
                return null;
            }
        };
}

// In Endpoint:
app.MapGet("/api/accounts", async (
    [AsParameters] PaginationParameters pagination,
    [AsParameters] FilterParameters filter,
    ApplicationDbContext db) =>
{
    var customOperators = new AccountCustomOperatorProvider();

    var paged = await db.Accounts
        .ApplyFilter(filter, customOperators) // supports customer_name=John and balance%=100
        .OrderBy(a => a.Id)
        .ToPagedListAsync(pagination);

    return Results.Ok(paged.ToPagedResponse());
});
```

---

## Recipe 21: Functional Error Handling with `EricksonLopez.Pagination.Result`

### Problem
You follow Railway-Oriented Programming and prefer returning explicit `Result<T>` instances instead of throwing and catching cursor exceptions.

### Solution
Use `PaginationResultExtensions.ExecuteResultAsync` to encapsulate cursor query execution:

```csharp
app.MapGet("/api/products/safe-keyset", async (
    [AsParameters] CursorPaginationParameters cursor,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    Result<ICursorPagedList<Product>> result = await PaginationResultExtensions.ExecuteResultAsync(async () =>
    {
        return await db.Products
            .Keyset(cursor)
            .Ascending(p => p.Id)
            .ToCursorPagedListAsync(cancellationToken: ct);
    });

    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            ErrorCode = result.Error.Code,
            Message = result.Error.Description
        });
    }

    return Results.Ok(result.Value.ToCursorPagedResponse(p => p.Id));
});
```

---

## Recipe 22: GraphQL Relay Specification Compliance with `EricksonLopez.Pagination.Relay`

### Problem
You need to expose a cursor pagination schema conforming to the official GraphQL Relay Connection specification (`Connection<T>`, `Edge<T>`, `PageInfo`).

### Solution
Use `ToRelayConnection` to transform `ICursorPagedList<T>` into a standard Relay `Connection<T>`:

```csharp
app.MapGet("/api/graphql/products-connection", async (
    [AsParameters] CursorPaginationParameters cursor,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .Keyset(cursor)
        .Ascending(p => p.Id)
        .ToCursorPagedListAsync(cancellationToken: ct);

    // Converts to Connection<Product> containing Edges, PageInfo (StartCursor, EndCursor, HasNextPage, HasPreviousPage)
    Connection<Product> connection = pagedList.ToRelayConnection(p => p.Id.ToString(CultureInfo.InvariantCulture));

    return Results.Ok(connection);
});
```

