# Level 6 — Error Handling and Typed Exceptions

> **Implementation Reference:** [`Level6ErrorHandling.cs`](../../DemoApp/DemoApp.Api/Levels/Level6ErrorHandling.cs)  
> **API Base Route:** `/api/level6`

---

## 1. Cursor Exception Hierarchy

The library defines a typed exception hierarchy in `EricksonLopez.Pagination.Abstractions`:

```
Exception
 └── InvalidPaginationCursorException (HTTP 400 Bad Request)
      ├── ExpiredPaginationCursorException (HTTP 410 Gone)
      └── ReplayedPaginationCursorException (HTTP 409 Conflict / 400 Bad Request)
```

- **`InvalidPaginationCursorException`**: Thrown when the Base64 format is corrupted, unparseable, or when the HMAC-SHA256 signature mismatch is detected (tampered cursor).
- **`ExpiredPaginationCursorException`**: Thrown when the HMAC signature is valid but the cursor timestamp exceeds the configured time-to-live (`timeToLive`).
- **`ReplayedPaginationCursorException`**: Thrown when a single-use cursor nonce is presented more than once to an active `ICursorReplayStore`.

---

## 2. Global Exception Handling with `PaginationExceptionHandler` (.NET 8+)

In `Program.cs`, register the native exception handler:

```csharp
builder.Services.AddExceptionHandler<PaginationExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
```

`PaginationExceptionHandler` intercepts cursor exceptions and generates standardized RFC 7807 `ProblemDetails` responses:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid Pagination Cursor",
  "status": 400,
  "detail": "The provided cursor is malformed, corrupted, or has an invalid HMAC signature."
}
```

---

## 3. Explicit Endpoint Handling

When an endpoint requires custom status codes (for instance, returning HTTP 410 Gone for expired cursors):

```csharp
try
{
    var page = await db.Products
        .Keyset(cursor, cursorEncoder: ttlEncoder)
        .Ascending(p => p.Id)
        .ToCursorPagedListAsync(cancellationToken: ct);

    return Results.Ok(page.ToCursorPagedResponse(p => p.Id));
}
catch (ExpiredPaginationCursorException ex)
{
    return Results.Problem(
        title: "Expired Cursor",
        detail: $"The pagination cursor expired at {ex.ExpiredAt:O}. Please restart navigation.",
        statusCode: StatusCodes.Status410Gone);
}
catch (InvalidPaginationCursorException ex)
{
    return Results.BadRequest(new { Error = ex.Message, Cursor = ex.OpaqueCursor });
}
```

---

## 4. `ReplayedPaginationCursorException` — Single-Use Cursor Replay Demo

`ReplayedPaginationCursorException` is thrown when a single-use cursor nonce is consumed more than once by an active `ICursorReplayStore`.

**Interactive demo** (`GET /api/level6/products/replay-demo?simulateReplay=true`):

```csharp
// In production: register RedisCursorReplayStore for distributed replay prevention.
// Here: InMemoryCursorReplayStore for development and self-contained demos.
var replayStore = new InMemoryCursorReplayStore();
using var replayEncoder = new HmacCursorEncoder(
    secretKey: "DemoKey-MustBe32BytesLong-ForHMACSHA256",
    replayStore: replayStore,
    timeToLive: TimeSpan.FromMinutes(5));

if (simulateReplay)
{
    var rawCursor   = "S|42";
    var encoded     = replayEncoder.Encode(rawCursor);
    var firstDecode = replayEncoder.Decode(encoded);  // ✅ OK — nonce registered

    try
    {
        var secondDecode = replayEncoder.Decode(encoded);  // 💥 ReplayedPaginationCursorException
    }
    catch (ReplayedPaginationCursorException ex)
    {
        return Results.Problem(
            title: "Cursor Already Consumed (Replay Detected)",
            detail: $"Nonce: {ex.Nonce}. First decode: '{firstDecode}'. Second decode rejected.",
            statusCode: StatusCodes.Status409Conflict);
    }
}
```

### `ReplayedPaginationCursorException` Properties

| Property | Type | Description |
|---|---|---|
| `Nonce` | `string` | The unique nonce of the replayed cursor (for logging/auditing) |
| `OpaqueCursor` | `string?` | The raw encoded cursor string (inherited from `InvalidPaginationCursorException`) |
| `Message` | `string` | Standard exception message |

> [!TIP]
> For distributed environments (multi-instance APIs, Kubernetes pods), replace `InMemoryCursorReplayStore` with `RedisCursorReplayStore` from `EricksonLopez.Pagination.Redis`. The TTL of the nonce in Redis should match or exceed the cursor's `timeToLive`.
