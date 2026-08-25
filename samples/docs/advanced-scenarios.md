# Advanced Scenarios

This document outlines advanced integration, concurrency, and scaling patterns based on the public API of `EricksonLopez.Pagination`.

---

## 1. Bidirectional Keyset Pagination (Forward & Backward)

The library's cursor protocol supports both forward navigation (`First` / `After`) and backward navigation (`Last` / `Before`):

```csharp
// Backward navigation: requests the last 10 elements before the specified cursor
var parameters = new CursorPaginationParameters
{
    Last = 10,
    Before = "S|eyJpZCI6NDV9"
};

var page = await db.Products
    .Keyset(parameters)
    .Ascending(p => p.Id)
    .ToCursorPagedListAsync(cancellationToken: ct);

// Returns a response conforming to the Relay specification
return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
```

---

## 2. Multi-Column Keyset with Composite Keys

When data is ordered by non-unique columns (such as `CreatedAt` or `Price`), `KeysetBuilder<T>` enables chaining multiple ordering columns with a mandatory unique tie-breaker at the end:

```csharp
var page = await db.Products
    .Keyset(cursorParameters)
    .Descending(p => p.CreatedAt) // Non-unique primary sort column
    .Ascending(p => p.Id)         // Mandatory unique tie-breaker
    .ToCursorPagedListAsync(cancellationToken: ct);
```

---

## 3. GraphQL Relay Compatibility

`EricksonLopez.Pagination.AspNetCore` implements the standard types defined by the GraphQL Relay Cursor Connections Specification:
- `CursorPagedResponse<T>`: Represents the Relay `Connection`.
- `Edge<T>`: Wrapper for each node (`Node`) and its corresponding cursor (`Cursor`).
- `RelayPageInfo`: Page metadata (`StartCursor`, `EndCursor`, `HasNextPage`, `HasPreviousPage`).

```csharp
// Automatic conversion from ICursorPagedList<T> to CursorPagedResponse<T>
CursorPagedResponse<Product> relayResponse = page.ToCursorPagedResponse(p => p.Id);
```

---

## 4. Asynchronous Streaming for Massive Datasets

For high-volume data transfers with minimal memory overhead:

```csharp
// Reactive streaming with IAsyncEnumerable<T>
await foreach (var product in db.Products
    .Keyset(cursorParameters)
    .Ascending(p => p.Id)
    .ToPagedAsyncEnumerable())
{
    // Process or stream item in real time without buffering full pages in RAM
}
```

---

## 5. Parallel Partitioning for Concurrent Processing

To divide a massive table across multiple workers or parallel background threads:

```csharp
var partitions = await db.Products
    .SplitKeysetPartitionsAsync(p => p.Id, partitionCount: 8, cancellationToken: ct);

Parallel.ForEach(partitions, partition =>
{
    // Each worker processes its isolated [LowerBound, UpperBound] partition
});
```
