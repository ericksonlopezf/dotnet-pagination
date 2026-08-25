# GraphQL & Relay Cursor Connection Interoperability Guide

`EricksonLopez.Pagination` follows the [GraphQL Cursor Connections Specification (Relay Specification)](https://relay.dev/graphql/connections.htm) for its cursor models (`First`, `After`, `Last`, `Before`, `StartCursor`, `EndCursor`, `HasNextPage`, `HasPreviousPage`).

---

## 1. Direct Mapping to Relay `Connection<T>`

If your application exposes GraphQL endpoints (e.g. via HotChocolate, GraphQL.NET, or custom resolvers), `ICursorPagedList<T>` maps 1:1 to Relay Connection models:

```csharp
using EricksonLopez.Pagination.Abstractions;

public sealed record PageInfo(
    bool HasNextPage,
    bool HasPreviousPage,
    string? StartCursor,
    string? EndCursor);

public sealed record Edge<T>(T Node, string Cursor);

public sealed record Connection<T>(
    IReadOnlyList<Edge<T>> Edges,
    PageInfo PageInfo,
    int? TotalCount = null);
```

### Extension Helper

```csharp
public static class RelayExtensions
{
    public static Connection<T> ToConnection<T>(this ICursorPagedList<T> page)
    {
        var edges = page.Select((item, idx) => 
            new Edge<T>(item, idx == page.Count - 1 ? page.EndCursor! : $"{page.StartCursor}_{idx}"))
            .ToList();

        var pageInfo = new PageInfo(
            page.HasNextPage,
            page.HasPreviousPage,
            page.StartCursor,
            page.EndCursor);

        return new Connection<T>(edges, pageInfo, page.TotalCount);
    }
}
```

---

## 2. Parameter Translation

GraphQL query parameters:
```graphql
query GetProducts($first: Int, $after: String, $last: Int, $before: String) {
  products(first: $first, after: $after, last: $last, before: $before) {
    edges {
      node {
        id
        name
      }
      cursor
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
  }
}
```

Map directly into `CursorPaginationParameters`:

```csharp
var parameters = new CursorPaginationParameters
{
    First = first,
    After = after,
    Last = last,
    Before = before
};

var paged = await dbContext.Products
    .Keyset(parameters)
    .Ascending(p => p.Id)
    .ToCursorPagedListAsync();

return paged.ToConnection();
```

---

## 3. HotChocolate Integration

When using HotChocolate, you can use `CursorPagedList<T>` directly inside query resolvers without requiring competing pagination middleware:

```csharp
public class Query
{
    public async Task<Connection<Product>> GetProducts(
        [Service] AppDbContext db,
        int? first,
        string? after,
        CancellationToken ct)
    {
        var paged = await db.Products
            .Keyset(new CursorPaginationParameters { First = first ?? 20, After = after })
            .Ascending(p => p.Id)
            .ToCursorPagedListAsync(ct);

        return paged.ToConnection();
    }
}
```
