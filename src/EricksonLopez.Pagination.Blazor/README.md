# EricksonLopez.Pagination.Blazor

Blazor UI components for the `EricksonLopez.Pagination` ecosystem.

## Overview

This package provides accessible, customizable, and headless-capable pagination components for both offset-based and cursor-based pagination in Blazor (Server, WebAssembly, and Auto modes).

## Installation

```xml
<PackageReference Include="EricksonLopez.Pagination.Blazor" Version="[VERSION]" />
```

## Usage

### 1. Offset Pagination Component (`PagedListPager`)

```razor
@using EricksonLopez.Pagination.Blazor

<PagedListPager 
    PagedList="@myPagedList" 
    OnPageChanged="LoadPage" 
    Preset="PaginationUIOptions.Bootstrap" />
```

### 2. Cursor Pagination Component (`CursorPagedListPager`)

```razor
@using EricksonLopez.Pagination.Blazor

<CursorPagedListPager 
    PagedList="@myCursorPagedList" 
    OnNextPage="LoadNextPage" 
    OnPreviousPage="LoadPreviousPage" 
    Preset="PaginationUIOptions.Tailwind" />
```

## Styling & Presets

You can apply CSS classes directly via parameters or globally via dependency injection.

### Global Configuration (Program.cs)

**Step 1:** Call `AddPaginationBlazor()` to ensure `IOptions<PaginationUIOptions>` is registered:

```csharp
// Program.cs — required for global theming to work
builder.Services.AddPaginationBlazor();
```

**Step 2 (optional):** Override the default CSS classes:

```csharp
builder.Services.Configure<PaginationUIOptions>(options =>
{
    var preset = PaginationUIOptions.Bootstrap;
    options.ContainerClass = preset.ContainerClass;
    options.PaginationClass = preset.PaginationClass;
    options.ListClass = preset.ListClass;
    options.ItemClass = preset.ItemClass;
    options.LinkClass = preset.LinkClass;
    options.ActiveClass = preset.ActiveClass;
    options.DisabledClass = preset.DisabledClass;
});
```

Or use the shorthand delegate overload:

```csharp
builder.Services.AddPaginationBlazor(options =>
{
    var preset = PaginationUIOptions.Bootstrap;
    options.ListClass = preset.ListClass;
    options.ItemClass = preset.ItemClass;
    options.LinkClass = preset.LinkClass;
    options.ActiveClass = preset.ActiveClass;
    options.DisabledClass = preset.DisabledClass;
});
```

> **Note:** If `AddPaginationBlazor()` is not called, the components still work — they fall back to headless/unstyled mode. The per-component `Preset` parameter can always be used without any DI registration.


### Pre-defined Presets
- `PaginationUIOptions.Bootstrap` (Bootstrap 5)
- `PaginationUIOptions.Tailwind` (Tailwind CSS)

## Headless Mode

If the default markup doesn't fit your needs, you can use the `HeadlessTemplate` to render your own custom UI while the component manages the state and events.

```razor
<PagedListPager PagedList="@myPagedList" OnPageChanged="LoadPage">
    <HeadlessTemplate Context="pager">
        <button disabled="@(!pager.PagedList.HasPreviousPage)" @onclick="() => pager.GoToPage(pager.PagedList.Page - 1)">
            Prev
        </button>
        <span>Page @pager.PagedList.Page</span>
        <button disabled="@(!pager.PagedList.HasNextPage)" @onclick="() => pager.GoToPage(pager.PagedList.Page + 1)">
            Next
        </button>
    </HeadlessTemplate>
</PagedListPager>
```
