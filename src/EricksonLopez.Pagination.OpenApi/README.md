# EricksonLopez.Pagination.OpenApi

Swagger and OpenAPI pagination documentation extensions for the `EricksonLopez.Pagination` ecosystem.

## Overview

This package enriches your auto-generated OpenAPI documentation to correctly describe pagination parameters, filtering, and sorting parameters for methods that use `PaginationParameters`, `CursorPaginationParameters`, `FilterParameters`, or `SortParameters`.

It supports both the popular `Swashbuckle.AspNetCore` library and the native `.NET 9+` `Microsoft.AspNetCore.OpenApi` implementation.

## Installation

```xml
<PackageReference Include="EricksonLopez.Pagination.OpenApi" Version="[VERSION]" />
```

## Usage

### Swashbuckle (ASP.NET Core 8+)

If you are using `Swashbuckle.AspNetCore`, register the pagination operation filter in your SwaggerGen options:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    
    // Add EricksonLopez.Pagination support to Swashbuckle
    c.AddPaginationSupport();
});
```

*Note: Swashbuckle relies heavily on reflection and is **not compatible with Native AOT** or aggressive trimming.*

### Microsoft.AspNetCore.OpenApi (.NET 9+)

If you are using the native .NET 9 OpenAPI implementation (`Microsoft.AspNetCore.OpenApi`), register the operation transformer in your OpenApi options:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    // Add EricksonLopez.Pagination support to native OpenAPI
    options.AddPaginationSupport();
});
```

*Note: The native OpenAPI implementation in .NET 9 is AOT-friendly and trimming-safe.*

## Features

- **Parameter Descriptions:** Automatically adds human-readable descriptions to `page`, `pageSize`, `first`, `last`, `after`, and `before` query parameters.
- **Filter and Sort injection:** If you accept `FilterParameters` or `SortParameters` via a model binder, but the OpenAPI generator fails to discover them, this package will automatically inject `filter` and `sortBy` as string query parameters with proper examples and descriptions (e.g., `name~=John,age>=18`).
