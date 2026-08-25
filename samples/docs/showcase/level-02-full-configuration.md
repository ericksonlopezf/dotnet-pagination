# Level 2 — Full Configuration and DSL

> **Implementation Reference:** [`Level2FullConfiguration.cs`](../../DemoApp/DemoApp.Api/Levels/Level2FullConfiguration.cs)  
> **API Base Route:** `/api/level2`

---

## 1. Dynamic Filtering with `FilterParameters` and `ApplyFilter`

`FilterParameters` allows clients to supply filter expressions in a query string that compiles to a safe, strongly-typed LINQ predicate:

```csharp
group.MapGet("/products/filter", async (
    [AsParameters] PaginationParameters pagination,
    [AsParameters] FilterParameters filter,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .ApplyFilter(filter)
        .OrderBy(p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});
```

### Filter DSL Syntax

| Operator | Meaning | Example |
|---|---|---|
| `=` | Exact equality | `filter=category=Electronics` |
| `!=` | Inequality | `filter=category!=Books` |
| `>=` | Greater than or equal | `filter=price>=100` |
| `<=` | Less than or equal | `filter=price<=500` |
| `>` | Strict greater than | `filter=price>50` |
| `<` | Strict less than | `filter=price<200` |
| `~=` | String contains (case-insensitive) | `filter=name~=Pro` |
| `,` | Logical AND conjunction | `filter=price>=50,name~=Clothing` |

### Expression Tree Cache (`ExpressionCache`)
Queries compiled from DSL strings are cached in a thread-safe cache (`ExpressionCache`). Subsequent queries with identical field structures resolve in **~32 ns** with zero heap allocations.

---

## 2. Dynamic Sorting with `SortParameters` and `ApplySort`

`SortParameters` encapsulates multi-column sort tokens (e.g., `"name asc, price desc"`):

```csharp
group.MapGet("/products/sort", async (
    [AsParameters] PaginationParameters pagination,
    [AsParameters] SortParameters sortBy,
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var pagedList = await db.Products
        .ApplySort(sortBy, defaultSort: p => p.Id)
        .ToPagedListAsync(pagination, cancellationToken: ct);

    return Results.Ok(pagedList.ToPagedResponse());
});
```

---

## 3. SQL Injection Defense & Column Allowlist: `SortParameters.ValidateColumnName`

To prevent Denial of Service attacks or internal column enumeration, define an explicit allowlist of sortable properties:

```csharp
var allowedSortColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "Name", "Price", "CreatedAt"
};

// Validates and throws InvalidOperationException if the column is not allowlisted
SortParameters.ValidateColumnName(sortBy.Value, allowedSortColumns);

var pagedList = await db.Products
    .ApplySort(sortBy, defaultSort: p => p.Id, allowedProperties: allowedSortColumns)
    .ToPagedListAsync(pagination, cancellationToken: ct);
```

---

## 4. Single-Call Unified Query

The integrated overload applies filtering, sorting, and pagination in a single call:

```csharp
var pagedList = await db.Products
    .ToPagedListAsync(filter, sortBy, pagination, cancellationToken: ct);
```

---

## 5. Custom Filter Operators: `IFilterOperatorProvider<T>`

`IFilterOperatorProvider<TEntity>` extends the built-in DSL engine with custom operators while preserving all standard operators (`=`, `!=`, `>=`, `~=`, etc.):

```csharp
// GET /api/level2/products/custom-operator?filter=name%25=Pro&page=1&pageSize=10
// %25 is the URL-encoded form of % (the custom operator symbol)

// Implementation
file sealed class ProductFuzzyFilterOperatorProvider : IFilterOperatorProvider<Product>
{
    public IReadOnlyDictionary<string, FilterOperatorHandler<Product>> Operators { get; } =
        new Dictionary<string, FilterOperatorHandler<Product>>(StringComparer.OrdinalIgnoreCase)
        {
            ["%="] = (propertyExpr, rawValue) =>
            {
                var constValue   = Expression.Constant(rawValue);
                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
                return Expression.Call(propertyExpr, containsMethod, constValue);
            }
        };
}

// Usage
var pagedList = await db.Products
    .ApplyFilter(filter, new ProductFuzzyFilterOperatorProvider())
    .OrderBy(p => p.Id)
    .ToPagedListAsync(pagination, cancellationToken: ct);
```

> [!IMPORTANT]
> `IFilterOperatorProvider<T>` extends the DSL engine but still requires `[RequiresUnreferencedCode]` because the core `ApplyFilter` uses reflection. For Native AOT, use `IFilterProvider<T>` (Level 8) instead.

**Difference summary:**
| Type | Approach | AOT | Use Case |
|---|---|---|---|
| `IFilterOperatorProvider<T>` | Extends DSL with new operators | ❌ | Add `%=`, `geo_near`, `in` to existing DSL |
| `IFilterProvider<T>` | Replaces the entire filter engine | ✅ | Full AOT-safe custom filtering |

---

## 6. Server-Side Projection Pipeline: `ToPagedListAsync<T,TResult>`

The 4-parameter overload applies filter + sort + SQL projection + pagination in one server-side call:

```csharp
// GET /api/level2/products/filter-sort-projected?filter=price>=50&sortBy=name+asc
var pagedDtos = await db.Products
    .ToPagedListAsync(
        filter,
        sortBy,
        p => new { p.Id, p.Name, p.Price }, // SQL SELECT — only transfers needed columns
        pagination,
        cancellationToken: ct);

return Results.Ok(pagedDtos.ToPagedResponse());
```

This is equivalent to calling `.ApplyFilter()` → `.ApplySort()` → `.Select(selector)` → `.ToPagedListAsync()` manually, but as a single fluent expression.

> [!NOTE]
> `useApproximateCount` and `countTotal` optional parameters are also supported in this overload.

---

## 7. `PaginationCoreOptions` — Live Configuration Introspection

`PaginationCoreOptions` is the public DI configuration class for the library. Inject it via `IOptions<PaginationCoreOptions>`:

```csharp
// GET /api/level2/pagination-options
group.MapGet("/pagination-options", (IOptions<PaginationCoreOptions> opts) =>
{
    var options = opts.Value;
    return Results.Ok(new
    {
        options.MaxPageSize,
        options.DefaultPageSize,
        options.DeepOffsetWarningThreshold,
        options.MaxFilterComplexity,
        options.MaxFilterStringLength,
        options.MaxFilterValueLength,
        options.MaxPropertyDepth
    });
});
```

Configure in `Program.cs`:
```csharp
builder.Services.AddPagination(options =>
{
    options.MaxPageSize  = 100;
    options.DefaultPageSize = 20;
    options.MaxFilterComplexity = 15;       // Anti-DoS: max filter DSL clauses
    options.MaxFilterStringLength = 500;    // Anti-DoS: max filter string length
    options.MaxFilterValueLength  = 100;    // Anti-DoS: max individual value length
    options.MaxPropertyDepth = 2;           // Anti-DoS: max nested property depth
    options.DeepOffsetWarningThreshold = 50_000; // Log warning after 50k skipped rows
});
```
