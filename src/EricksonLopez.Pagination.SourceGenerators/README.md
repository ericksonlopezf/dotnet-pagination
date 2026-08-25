# EricksonLopez.Pagination.SourceGenerators

Roslyn source generator for Native AOT cursor decoders in the `EricksonLopez.Pagination` ecosystem.

## Overview

By default, cursor pagination relies on runtime reflection to dynamically decode strings into strongly typed keys (e.g. `string` -> `int`). This reflection-based approach is **not compatible with Native AOT** publishing or aggressive trimming.

This source generator solves that by intercepting calls to `ToCursorPagedListAsync` at compile time and generating zero-allocation, AOT-friendly parsers for your cursor types.

## Installation

Install the package into the project where you execute your queries:

```xml
<PackageReference Include="EricksonLopez.Pagination.SourceGenerators" Version="[VERSION]" />
```

*Note: The generator is a development dependency and will not be included as a runtime dependency in your published output.*

## Usage

### 1. Let the generator work
Simply write your pagination queries normally. The analyzer will automatically detect which types are being used as cursor keys.

```csharp
var paged = await dbContext.Users
    .Keyset(new CursorPaginationParameters { After = "..." })
    .Ascending(u => u.Id) // Id is int
    .ToCursorPagedListAsync();
```

### 2. Register the decoders

The source generator produces an extension method `AddAotCursorDecoders()` in the `EricksonLopez.Pagination.Generated` namespace. 
You must call this during your application startup to inject the decoders into the DI container:

```csharp
using EricksonLopez.Pagination.Generated;

var builder = WebApplication.CreateBuilder(args);

// Register the AOT-safe decoders generated at compile time
builder.Services.AddAotCursorDecoders();
```

## Filter Provider Generation (AOT-Safe Filtering)

Decorate your entity models with `[GenerateFilterProvider]` to automatically generate compile-time, zero-reflection `IFilterProvider<TEntity>` implementations for Native AOT:

```csharp
using EricksonLopez.Pagination.Abstractions;

[GenerateFilterProvider]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public bool IsActive { get; set; }
}
```

The generator emits `ProductFilterProvider` with a singleton `ProductFilterProvider.Instance` that translates `FilterParameters` strings (e.g. `"name~=Phone,price<=500"`) into strongly typed LINQ predicates without runtime reflection:

```csharp
var filter = FilterParameters.From("name~=Phone,price<=500");
var filteredQuery = dbContext.Products.ApplyFilter(ProductFilterProvider.Instance, filter);
```

## Supported Types for Keyset Cursor Decoders

The source generator automatically provides decoders for:
- Primitives (`int`, `long`, `string`, `DateTime`, `DateTimeOffset`, `Guid`)
- Any `Enum`
- Any type that implements `IParsable<T>` or `ISpanParsable<T>`
- Any type with a `Parse(string)` or `TryParse(string, IFormatProvider, out T)` method

If you attempt to paginate by a custom struct that does not meet these criteria, you will receive compile-time warning `PAG001`.
