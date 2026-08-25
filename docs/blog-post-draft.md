# Enterprise-grade Pagination in .NET: Moving beyond OFFSET

If you're building APIs in .NET, chances are you've written a pagination endpoint using `Skip()` and `Take()`. It's the standard approach, taught in every Entity Framework Core tutorial. 

It's also a ticking time bomb for your database performance.

In this post, we'll look at exactly why `OFFSET` pagination degrades at scale, look at real benchmark data on a 1-million row dataset, and explore a modern, secure alternative: **Keyset (Cursor) Pagination**.

---

## The OFFSET Trap

Let's look at the classic EF Core pagination query:

```csharp
var page = await db.Products
    .OrderBy(p => p.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

Under the hood, this translates to SQL like:
```sql
SELECT * FROM Products ORDER BY CreatedAt OFFSET 999900 LIMIT 100;
```

**Here is the problem:** Relational databases (PostgreSQL, SQL Server, MySQL) do not have a magical way to jump to the 999,900th row. To satisfy this query, the database engine must read, sort (if not indexed), count, and then throw away 999,900 rows before returning the 100 rows you actually asked for.

This means `OFFSET` has an **O(N) time complexity**. The deeper the page, the slower the query.

## The Benchmark: 1 Million Rows

To prove this, we ran a benchmark using `EricksonLopez.Pagination` on a PostgreSQL database seeded with 1,000,000 records. We asked the database for 100 rows at different depths using both `OFFSET` and `Keyset` pagination.

Here are the results (running on an AMD Ryzen 7 9800X3D):

| Target Page | Offset (Rows Skipped) | OFFSET Time | Keyset Time | Speedup |
|-------------|----------------------:|------------:|------------:|--------:|
| Page 1      | 0                     | 16.82 ms    | **2.62 ms** | 6.4x    |
| Page 100    | 9,900                 | 14.78 ms    | **3.44 ms** | 4.3x    |
| Page 10,000 | 999,900               | 74.48 ms    | **5.66 ms** | **13.1x**|

*Notice the trend?* 
`OFFSET` latency increases linearly. Fetching page 10,000 is nearly **5x slower** than fetching page 1.
`Keyset` latency remains virtually flat. Fetching page 10,000 takes only 5.6 milliseconds.

## The Solution: Keyset (Cursor) Pagination

Keyset pagination abandons the concept of "page numbers". Instead of saying *"skip 999,900 rows"*, it says *"give me the next 100 rows that come strictly after this specific item."*

```sql
SELECT * FROM Products 
WHERE CreatedAt > '2023-10-01T12:00:00' 
ORDER BY CreatedAt 
LIMIT 100;
```

If you have an index on `CreatedAt`, the database engine performs a **B-Tree index seek**. It traverses the tree directly to that timestamp in O(log N) time, grabs the next 100 contiguous rows, and returns. No counting, no discarding. It is wildly efficient.

### The UX Problem with Cursors

If Keyset pagination is so much better, why doesn't everyone use it? 
Because it's traditionally hard to implement.

1. **Multi-column sorts:** If two products have the same `CreatedAt`, you need a tie-breaker (like `Id`). The SQL `WHERE` clause for multi-column keysets is notoriously complex to write by hand.
2. **Client UX:** You can't expose raw database values (like `CreatedAt=2023-10-01&Id=500`) directly to the client. It couples your frontend to your database schema and opens vectors for enumeration attacks. You need an opaque "cursor" string.
3. **Security:** If you just Base64-encode the cursor, a malicious client can decode it, alter the values, and forge a cursor to bypass access controls or scrape your database.

## Enter EricksonLopez.Pagination

We built `EricksonLopez.Pagination` to solve exactly this problem. It brings enterprise-grade, secure Keyset pagination to .NET without the boilerplate.

### 1. Zero-Boilerplate Keyset

Instead of writing complex dynamic LINQ or raw SQL, you use a fluent builder directly on your `IQueryable`:

```csharp
app.MapGet("/products/cursor", async (
    CursorPaginationParameters cursor,
    [FromServices] AppDbContext db) =>
{
    return await db.Products
        .Keyset(cursor)
        .Ascending(p => p.CreatedAt)    // primary sort key
        .Ascending(p => p.Id)           // tiebreaker (must be unique)
        .ToCursorPagedListAsync();
});
```

### 2. Secure, HMAC-Signed Cursors

By default, `EricksonLopez.Pagination` uses an `HmacCursorEncoder`. Cursors aren't just Base64 encoded; they are cryptographically signed using HMAC-SHA256. 

```csharp
builder.Services.AddPagination(options =>
{
    // Cursors cannot be forged or tampered with by the client
    options.Cursor.Encoder = new HmacCursorEncoder(
        secretKey: builder.Configuration["Pagination:CursorKey"],
        timeToLive: TimeSpan.FromMinutes(30) // Prevents replay attacks!
    );
});
```
If a client attempts to modify the cursor string, the signature validation fails and an `InvalidPaginationCursorException` is thrown.

### 3. Built for Modern .NET (Native AOT)

Performance is the whole point of this library. We didn't stop at the database layer. The library includes a **Roslyn Source Generator** that emits AOT-compatible cursor decoders at compile-time. There is zero runtime reflection used in the keyset parsing pipeline, making it fully compatible with .NET 8/9/10 Native AOT deployments.

## Summary

`OFFSET` pagination is perfectly fine for admin panels or small tables. But if you are building public-facing APIs or dealing with large datasets (100k+ rows), you need Keyset pagination to protect your database from DoS-by-pagination.

With `EricksonLopez.Pagination`, making the switch takes about 3 lines of code, and you get tamper-proof, AOT-compatible cursors out of the box.

Check out the [GitHub Repository](https://github.com/ericksonlopezf/dotnet-pagination) or install the NuGet package today:
`dotnet add package EricksonLopez.Pagination.EntityFrameworkCore`
