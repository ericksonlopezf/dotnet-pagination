# EricksonLopez.Pagination.Cosmos

Cosmos DB cursor pagination extensions for the `EricksonLopez.Pagination` ecosystem.

## Overview

This package extends the `Container` class from `Microsoft.Azure.Cosmos` to natively support cursor pagination. It leverages the Cosmos SDK's built-in `ContinuationToken` mechanism to provide efficient, index-backed cursor pagination.

## Installation

```xml
<PackageReference Include="EricksonLopez.Pagination.Cosmos" Version="[VERSION]" />
```

## Usage

Use the `ToCursorPagedListAsync` extension method on your Cosmos `Container`.

```csharp
using EricksonLopez.Pagination.Cosmos;

// 1. Define your query
var query = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = 'users'");

// 2. Define parameters (usually mapped from an API request)
var parameters = new CursorPaginationParameters 
{ 
    First = 10,
    After = "..." // Cursor from the previous page
};

// 3. Paginate
var pagedList = await container.ToCursorPagedListAsync<User>(query, parameters);

// pagedList.Items contains the results
// pagedList.EndCursor contains the token for the next page
```

## Limitations

> [!WARNING]
> **Backward Pagination is NOT supported.**
> 
> Cosmos DB's `ContinuationToken` only allows moving forward through a result set. Because of this architectural limitation of Cosmos DB, passing a `Last` or `Before` parameter to `ToCursorPagedListAsync` will throw a `NotSupportedException`.

## Cursor Format

The cursor (`EndCursor`) returned by this package is an opaque, Base64-encoded wrapper around the native Cosmos DB continuation token. When passed back in the `After` parameter, it is safely decoded and passed to the Cosmos DB SDK.
