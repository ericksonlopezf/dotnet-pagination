# EricksonLopez.Pagination.MongoDB

MongoDB Driver offset and cursor pagination extensions for the `EricksonLopez.Pagination` ecosystem.

## Overview

This package provides native, highly optimized pagination extension methods for `MongoDB.Driver`. It supports both `IMongoQueryable<T>` and `IFindFluent<T, P>` interfaces, allowing seamless integration whether you use LINQ or the MongoDB driver's builder API.

## Installation

```xml
<PackageReference Include="EricksonLopez.Pagination.MongoDB" Version="[VERSION]" />
```

## Usage

### Offset Pagination

```csharp
using EricksonLopez.Pagination.MongoDB;

var parameters = new PaginationParameters { Page = 2, PageSize = 20 };
var paged = await collection.AsQueryable()
    .Where(u => u.IsActive)
    .ToPagedListAsync(parameters);
```

### Cursor Pagination

```csharp
using EricksonLopez.Pagination.MongoDB;

var parameters = new CursorPaginationParameters { First = 20, After = "..." };
var paged = await collection.AsQueryable()
    .Where(u => u.IsActive)
    .ToCursorPagedListAsync(parameters, u => u.Id);
```

### FindFluent Support

```csharp
var paged = await collection.Find(u => u.IsActive)
    .ToCursorPagedListAsync(parameters, Builders<User>.Sort.Ascending(u => u.Id));
```

## Limitations

> [!WARNING]
> **Native AOT Compatibility**
> 
> The MongoDB driver's `IMongoQueryable` implementation relies on `Expression.Compile()`, which is incompatible with Native AOT. Therefore, this package cannot be fully AOT-compatible.

> [!CAUTION]
> **String Key Collation**
>
> When using string keys for cursor pagination in MongoDB, ensure that the database collection collation matches the .NET string comparison behavior, otherwise you may experience missed or duplicated documents across pages.
