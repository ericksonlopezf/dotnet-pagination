# Level 8 — Customization, Security, and Cryptography

> **Implementation Reference:** [`Level8Customization.cs`](../../DemoApp/DemoApp.Api/Levels/Level8Customization.cs)  
> **API Base Route:** `/api/level8`

---

## 1. Custom `ICursorEncoder` Implementation

The `ICursorEncoder` interface allows swapping the cursor encoding and decoding algorithm:

```csharp
public sealed class PlainTextCursorEncoder : ICursorEncoder
{
    public string? Encode(string? rawCursor) => rawCursor;
    public string? Decode(string? opaqueCursor) => opaqueCursor;
}
```

---

## 2. HMAC-SHA256 Cryptography with `HmacCursorEncoder`

`HmacCursorEncoder` prevents client-side cursor tampering by attaching a cryptographic HMAC signature:

```csharp
using var hmacEncoder = new HmacCursorEncoder(
    secretKey: "Your-32-Byte-Long-Secret-Key-For-HMAC-256",
    timeToLive: TimeSpan.FromMinutes(15),
    clockSkewTolerance: TimeSpan.FromSeconds(30));

var page = await db.Products
    .Keyset(cursor, cursorEncoder: hmacEncoder)
    .Ascending(p => p.Id)
    .ToCursorPagedListAsync(cancellationToken: ct);
```

### Security Features
- **Constant-Time Verification**: Uses `CryptographicOperations.FixedTimeEquals` to prevent side-channel timing attacks.
- **Time-to-Live (TTL)**: Rejects cursors whose timestamp exceeds the configured validity window.
- **Clock Skew Tolerance (`clockSkewTolerance`)**: Mitigates clock drift between multiple nodes in distributed clusters.

---

## 3. Replay Attack Prevention (`ICursorReplayStore`)

To prevent captured single-use cursors from being consumed multiple times:

```csharp
var replayStore = new InMemoryCursorReplayStore();

using var replayEncoder = new HmacCursorEncoder(
    secretKey: "Your-32-Byte-Long-Secret-Key-For-HMAC-256",
    replayStore: replayStore,
    timeToLive: TimeSpan.FromMinutes(5));
```

If a client attempts to present the same cursor nonce more than once, the store flags the replay and throws `ReplayedPaginationCursorException`.

---

## 4. Property Filtering Exposure Control with `[Filterable]`

To prevent private, internal, or sensitive entity properties from being queried via the dynamic filter DSL:

```csharp
public class UserAccount
{
    public int Id { get; set; }

    [Filterable]
    public string Username { get; set; } = string.Empty;

    // Inaccessible via ?filter=PasswordHash=...
    public string PasswordHash { get; set; } = string.Empty;
}
```
