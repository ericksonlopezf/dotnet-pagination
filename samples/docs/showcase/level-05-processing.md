# Level 5 — Batch Processing and Streaming

> **Implementation Reference:** [`Level5Processing.cs`](../../DemoApp/DemoApp.Api/Levels/Level5Processing.cs)  
> **API Base Route:** `/api/level5`

---

## 1. Asynchronous Streaming with `ToPagedAsyncEnumerable`

`ToPagedAsyncEnumerable` yields an `IAsyncEnumerable<T>` over a specified page, streaming elements reactively as they are read from the database:

```csharp
group.MapGet("/products/stream", (
    [AsParameters] PaginationParameters pagination,
    ApplicationDbContext db) =>
{
    var stream = db.Products
        .OrderBy(p => p.Id)
        .ToPagedAsyncEnumerable(pagination);

    return Results.Ok(stream); // ASP.NET Core streams JSON chunks via HTTP chunked transfer encoding
});
```

---

## 2. Batch Processing with `ToPagedListBatchedAsync`

For background workers, ETL migrations, bulk data exports, or synchronization pipelines:

```csharp
group.MapGet("/products/batch-process", async (
    ApplicationDbContext db,
    CancellationToken ct) =>
{
    var batchSize = 100;
    var totalProcessed = 0;

    await foreach (var batch in db.Products
        .OrderBy(p => p.Id)
        .ToPagedListBatchedAsync(batchSize, cancellationToken: ct))
    {
        foreach (var product in batch)
        {
            // Process each item in the current batch
            totalProcessed++;
        }
    }

    return Results.Ok(new { TotalProcessed = totalProcessed });
});
```

### Benefits of `ToPagedListBatchedAsync`
- Automatically advances across pages until all records in the queryable are consumed.
- Restricts memory allocation to the specified batch size (`batchSize`).
- Fully integrates cooperative cancellation via `CancellationToken`.

---

## 3. Unmaterialized Keyset Streaming

Combines B-Tree index seek efficiency with memory-bounded streaming:

```csharp
group.MapGet("/products/keyset-stream", (
    [AsParameters] CursorPaginationParameters cursor,
    ApplicationDbContext db) =>
{
    var stream = db.Products
        .Keyset(cursor)
        .Ascending(p => p.Id)
        .ToPagedAsyncEnumerable();

    return Results.Ok(stream);
});
```

> [!NOTE]
> Keyset streaming supports forward navigation (`First` / `After`). Backward navigation (`Last` / `Before`) requires inverting the materialized result set in memory before emission.
