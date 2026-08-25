# EricksonLopez.Pagination.LinqToDB

LinqToDB provider adapter for the `EricksonLopez.Pagination` ecosystem, providing both Offset and N-column Keyset pagination.

## Installation

```xml
<PackageReference Include="EricksonLopez.Pagination.LinqToDB" Version="[VERSION]" />
```

## Quick Start

### 1. Offset Pagination

```csharp
using LinqToDB;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.LinqToDB;

public async Task<IPagedList<Product>> GetProductsAsync(
    IDataContext db,
    PaginationParameters parameters,
    CancellationToken cancellationToken)
{
    return await db.GetTable<Product>()
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name)
        .ToPagedListAsync(parameters, cancellationToken);
}
```

### 2. High-Performance Keyset Pagination

```csharp
using LinqToDB;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.LinqToDB;

public async Task<ICursorPagedList<Product>> GetProductsKeysetAsync(
    IDataContext db,
    CursorPaginationParameters parameters,
    CancellationToken cancellationToken)
{
    return await db.GetTable<Product>()
        .Where(p => p.IsActive)
        .Keyset(parameters)
        .Ascending(p => p.CreatedAt)
        .Ascending(p => p.Id)
        .ToCursorPagedListAsync(cancellationToken);
}
```

### 3. Lightweight Projections

```csharp
var pagedDtos = await db.GetTable<Product>()
    .Keyset(parameters)
    .Ascending(p => p.Id)
    .ToCursorPagedListAsync(p => new ProductDto(p.Id, p.Name), cancellationToken);
```
