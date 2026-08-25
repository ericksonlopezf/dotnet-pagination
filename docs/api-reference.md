# Public API Reference — EricksonLopez.Pagination

## `Base64CursorEncoder` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `Encode`

**Signature:**
```csharp
public string? Encode(string? rawCursor)
```

**Parameters:**
- `string? rawCursor`

**Return:** `string?`

**When to use:** Use in standard implementations of Base64CursorEncoder.

---
#### `Decode`

**Signature:**
```csharp
public string? Decode(string? opaqueCursor)
```

**Parameters:**
- `string? opaqueCursor`

**Return:** `string?`

**When to use:** Use in standard implementations of Base64CursorEncoder.

---
## `CountedCursorPagedList` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `Map`

**Signature:**
```csharp
public CountedCursorPagedList<TResult> Map(Func<T, TResult> selector)
```

**Parameters:**
- `Func<T, TResult> selector`

**Return:** `CountedCursorPagedList<TResult>`

**When to use:** Use in standard implementations of CountedCursorPagedList.

---
## `CountedPagedList` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `Map`

**Signature:**
```csharp
public CountedPagedList<TResult> Map(Func<T, TResult> selector)
```

**Parameters:**
- `Func<T, TResult> selector`

**Return:** `CountedPagedList<TResult>`

**When to use:** Use in standard implementations of CountedPagedList.

---
## `CursorPagedList` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `GetEnumerator`

**Signature:**
```csharp
public IEnumerator<T> GetEnumerator()
```

**Parameters:**
- None

**Return:** `IEnumerator<T>`

**When to use:** Use in standard implementations of CursorPagedList.

---
#### `Create`

**Signature:**
```csharp
public CursorPagedList<T> Create(IReadOnlyList<T> items, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)
```

**Parameters:**
- `IReadOnlyList<T> items`
- `string? startCursor`
- `string? endCursor`
- `bool hasPreviousPage`
- `bool hasNextPage`

**Return:** `CursorPagedList<T>`

**When to use:** Use in standard implementations of CursorPagedList.

---
#### `Map`

**Signature:**
```csharp
public CursorPagedList<TResult> Map(Func<T, TResult> selector)
```

**Parameters:**
- `Func<T, TResult> selector`

**Return:** `CursorPagedList<TResult>`

**When to use:** Use in standard implementations of CursorPagedList.

---
## `CursorPaginationParametersExtensions` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `DecodeAfter`

**Signature:**
```csharp
public TKey? DecodeAfter(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `ICursorEncoder? cursorEncoder`
- `ICursorDecoderRegistry? decoderRegistry`

**Return:** `TKey?`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
#### `DecodeBefore`

**Signature:**
```csharp
public TKey? DecodeBefore(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `ICursorEncoder? cursorEncoder`
- `ICursorDecoderRegistry? decoderRegistry`

**Return:** `TKey?`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
#### `DecodeAfterString`

**Signature:**
```csharp
public string? DecodeAfterString(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `ICursorEncoder? cursorEncoder`

**Return:** `string?`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
#### `DecodeBeforeString`

**Signature:**
```csharp
public string? DecodeBeforeString(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `ICursorEncoder? cursorEncoder`

**Return:** `string?`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
#### `DecodeAfterReference`

**Signature:**
```csharp
public TKey? DecodeAfterReference(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `ICursorEncoder? cursorEncoder`
- `ICursorDecoderRegistry? decoderRegistry`

**Return:** `TKey?`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
#### `DecodeBeforeReference`

**Signature:**
```csharp
public TKey? DecodeBeforeReference(this CursorPaginationParameters parameters, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `ICursorEncoder? cursorEncoder`
- `ICursorDecoderRegistry? decoderRegistry`

**Return:** `TKey?`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
#### `TryDecodeAfter`

**Signature:**
```csharp
public bool TryDecodeAfter(this CursorPaginationParameters parameters, TKey? value, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `TKey? value`
- `ICursorEncoder? cursorEncoder`
- `ICursorDecoderRegistry? decoderRegistry`

**Return:** `bool`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
#### `TryDecodeBefore`

**Signature:**
```csharp
public bool TryDecodeBefore(this CursorPaginationParameters parameters, TKey? value, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry)
```

**Parameters:**
- `this CursorPaginationParameters parameters`
- `TKey? value`
- `ICursorEncoder? cursorEncoder`
- `ICursorDecoderRegistry? decoderRegistry`

**Return:** `bool`

**When to use:** Use in standard implementations of CursorPaginationParametersExtensions.

---
## `DefaultPagedListFactory` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `CreatePagedList`

**Signature:**
```csharp
public IPagedList<T> CreatePagedList(IReadOnlyList<T> items, long? totalCount, int page, int pageSize, bool? hasNextPage)
```

**Parameters:**
- `IReadOnlyList<T> items`
- `long? totalCount`
- `int page`
- `int pageSize`
- `bool? hasNextPage`

**Return:** `IPagedList<T>`

**When to use:** Use in standard implementations of DefaultPagedListFactory.

---
#### `CreateCursorPagedList`

**Signature:**
```csharp
public ICursorPagedList<T> CreateCursorPagedList(IReadOnlyList<T> items, long? totalCount, string? startCursor, string? endCursor, bool hasPreviousPage, bool hasNextPage)
```

**Parameters:**
- `IReadOnlyList<T> items`
- `long? totalCount`
- `string? startCursor`
- `string? endCursor`
- `bool hasPreviousPage`
- `bool hasNextPage`

**Return:** `ICursorPagedList<T>`

**When to use:** Use in standard implementations of DefaultPagedListFactory.

---
## `HmacCursorEncoder` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `Encode`

**Signature:**
```csharp
public string? Encode(string? rawCursor)
```

**Parameters:**
- `string? rawCursor`

**Return:** `string?`

**When to use:** Use in standard implementations of HmacCursorEncoder.

---
#### `Decode`

**Signature:**
```csharp
public string? Decode(string? opaqueCursor)
```

**Parameters:**
- `string? opaqueCursor`

**Return:** `string?`

**When to use:** Use in standard implementations of HmacCursorEncoder.

---
#### `Dispose`

**Signature:**
```csharp
public void Dispose()
```

**Parameters:**
- None

**Return:** `void`

**When to use:** Use in standard implementations of HmacCursorEncoder.

---
## `ICursorEncoder` (interface)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

No public methods documented for this type.

## `InMemoryCursorDecoderRegistry` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `Register`

**Signature:**
```csharp
public void Register(Func<string, TKey> decoder)
```

**Parameters:**
- `Func<string, TKey> decoder`

**Return:** `void`

**When to use:** Use in standard implementations of InMemoryCursorDecoderRegistry.

---
#### `TryGetDecoder`

**Signature:**
```csharp
public bool TryGetDecoder(Func<string, TKey>? decoder)
```

**Parameters:**
- `Func<string, TKey>? decoder`

**Return:** `bool`

**When to use:** Use in standard implementations of InMemoryCursorDecoderRegistry.

---
#### `Clear`

**Signature:**
```csharp
public void Clear()
```

**Parameters:**
- None

**Return:** `void`

**When to use:** Use in standard implementations of InMemoryCursorDecoderRegistry.

---
#### `Unregister`

**Signature:**
```csharp
public bool Unregister()
```

**Parameters:**
- None

**Return:** `bool`

**When to use:** Use in standard implementations of InMemoryCursorDecoderRegistry.

---
## `PagedList` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `GetEnumerator`

**Signature:**
```csharp
public IEnumerator<T> GetEnumerator()
```

**Parameters:**
- None

**Return:** `IEnumerator<T>`

**When to use:** Use in standard implementations of PagedList.

---
#### `WithCount`

**Signature:**
```csharp
public CountedPagedList<T> WithCount(IReadOnlyList<T> items, PaginationParameters parameters, long totalCount)
```

**Parameters:**
- `IReadOnlyList<T> items`
- `PaginationParameters parameters`
- `long totalCount`

**Return:** `CountedPagedList<T>`

**When to use:** Use in standard implementations of PagedList.

---
#### `WithoutCount`

**Signature:**
```csharp
public PagedList<T> WithoutCount(IReadOnlyList<T> items, PaginationParameters parameters, bool hasNextPage, bool? hasPreviousPage)
```

**Parameters:**
- `IReadOnlyList<T> items`
- `PaginationParameters parameters`
- `bool hasNextPage`
- `bool? hasPreviousPage`

**Return:** `PagedList<T>`

**When to use:** Use in standard implementations of PagedList.

---
#### `Empty`

**Signature:**
```csharp
public CountedPagedList<T> Empty(PaginationParameters parameters)
```

**Parameters:**
- `PaginationParameters parameters`

**Return:** `CountedPagedList<T>`

**When to use:** Use in standard implementations of PagedList.

---
#### `Map`

**Signature:**
```csharp
public PagedList<TResult> Map(Func<T, TResult> selector)
```

**Parameters:**
- `Func<T, TResult> selector`

**Return:** `PagedList<TResult>`

**When to use:** Use in standard implementations of PagedList.

---
## `PagedListExtensions` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `LazyMap`

**Signature:**
```csharp
public IPagedList<TResult> LazyMap(this IPagedList<TSource> source, Func<TSource, TResult> selector)
```

**Parameters:**
- `this IPagedList<TSource> source`
- `Func<TSource, TResult> selector`

**Return:** `IPagedList<TResult>`

**When to use:** Use in standard implementations of PagedListExtensions.

---
#### `Map`

**Signature:**
```csharp
public IPagedList<TResult> Map(this IPagedList<TSource> source, Func<TSource, TResult> selector)
```

**Parameters:**
- `this IPagedList<TSource> source`
- `Func<TSource, TResult> selector`

**Return:** `IPagedList<TResult>`

**When to use:** Use in standard implementations of PagedListExtensions.

---
#### `Map`

**Signature:**
```csharp
public ICountedPagedList<TResult> Map(this ICountedPagedList<TSource> source, Func<TSource, TResult> selector)
```

**Parameters:**
- `this ICountedPagedList<TSource> source`
- `Func<TSource, TResult> selector`

**Return:** `ICountedPagedList<TResult>`

**When to use:** Use in standard implementations of PagedListExtensions.

---
## `PaginationCoreOptions` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `Validate`

**Signature:**
```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
```

**Parameters:**
- `ValidationContext validationContext`

**Return:** `IEnumerable<ValidationResult>`

**When to use:** Use in standard implementations of PaginationCoreOptions.

---
## `PaginationCoreOptionsValidator` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

No public methods documented for this type.

## `PaginationCursorOptions` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

No public methods documented for this type.

## `PaginationDiagnostics` (class)
**Namespace:** `EricksonLopez.Pagination`

### Public Methods

#### `CreateLogger`

**Signature:**
```csharp
public ILogger<T>? CreateLogger()
```

**Parameters:**
- None

**Return:** `ILogger<T>?`

**When to use:** Use in standard implementations of PaginationDiagnostics.

---
## `CursorPaginationParameters` (record)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

#### `GetPageSize`

**Signature:**
```csharp
public int GetPageSize(int defaultSize)
```

**Parameters:**
- `int defaultSize`

**Return:** `int`

**When to use:** Use in standard implementations of CursorPaginationParameters.

---
## `ExpiredPaginationCursorException` (class)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `FilterableAttribute` (class)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `FilterParameters` (record)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

#### `From`

**Signature:**
```csharp
public FilterParameters From(string? filter)
```

**Parameters:**
- `string? filter`

**Return:** `FilterParameters`

**When to use:** Use in standard implementations of FilterParameters.

---
#### `ToString`

**Signature:**
```csharp
public string ToString()
```

**Parameters:**
- None

**Return:** `string`

**When to use:** Use in standard implementations of FilterParameters.

---
## `ICountedCursorPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `ICountedCursorPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `ICountedPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `ICountedPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `ICursorDecoderRegistry` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `ICursorPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `ICursorPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `ICursorPagedListExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

#### `Map`

**Signature:**
```csharp
public ICursorPagedList<TResult> Map(this ICursorPagedList<TSource> source, Func<TSource, TResult> selector)
```

**Parameters:**
- `this ICursorPagedList<TSource> source`
- `Func<TSource, TResult> selector`

**Return:** `ICursorPagedList<TResult>`

**When to use:** Use in standard implementations of ICursorPagedListExtensions.

---
## `ICursorPagedListFactory` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `InvalidPaginationCursorException` (class)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `IPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `IPagedList` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `IPagedListFactory` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `IPaginationOptions` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `PaginationParameters` (record)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

#### `Create`

**Signature:**
```csharp
public PaginationParameters Create(int page, int pageSize)
```

**Parameters:**
- `int page`
- `int pageSize`

**Return:** `PaginationParameters`

**When to use:** Use in standard implementations of PaginationParameters.

---
## `RawCursorValue` (struct)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

No public methods documented for this type.

## `SortParameters` (record)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

### Public Methods

#### `From`

**Signature:**
```csharp
public SortParameters From(string? sort)
```

**Parameters:**
- `string? sort`

**Return:** `SortParameters`

**When to use:** Use in standard implementations of SortParameters.

---
#### `ToString`

**Signature:**
```csharp
public string ToString()
```

**Parameters:**
- None

**Return:** `string`

**When to use:** Use in standard implementations of SortParameters.

---
#### `ValidateColumnName`

**Signature:**
```csharp
public void ValidateColumnName(string columnName, System.Collections.Generic.IEnumerable<string>? allowedProperties)
```

**Parameters:**
- `string columnName`
- `System.Collections.Generic.IEnumerable<string>? allowedProperties`

**Return:** `void`

**When to use:** Use in standard implementations of SortParameters.

---
#### `ValidateColumnName`

**Signature:**
```csharp
public void ValidateColumnName(string columnName, System.Collections.Generic.IReadOnlySet<string> allowedProperties)
```

**Parameters:**
- `string columnName`
- `System.Collections.Generic.IReadOnlySet<string> allowedProperties`

**Return:** `void`

**When to use:** Use in standard implementations of SortParameters.

---
## `CursorSecurityAnalyzer` (class)
**Namespace:** `EricksonLopez.Pagination.Analyzers`

### Public Methods

#### `Initialize`

**Signature:**
```csharp
public void Initialize(AnalysisContext context)
```

**Parameters:**
- `AnalysisContext context`

**Return:** `void`

**When to use:** Use in standard implementations of CursorSecurityAnalyzer.

---
## `KeysetColumnCountAnalyzer` (class)
**Namespace:** `EricksonLopez.Pagination.Analyzers`

### Public Methods

#### `Initialize`

**Signature:**
```csharp
public void Initialize(AnalysisContext context)
```

**Parameters:**
- `AnalysisContext context`

**Return:** `void`

**When to use:** Use in standard implementations of KeysetColumnCountAnalyzer.

---
## `MissingOrderByAnalyzer` (class)
**Namespace:** `EricksonLopez.Pagination.Analyzers`

### Public Methods

#### `Initialize`

**Signature:**
```csharp
public void Initialize(AnalysisContext context)
```

**Parameters:**
- `AnalysisContext context`

**Return:** `void`

**When to use:** Use in standard implementations of MissingOrderByAnalyzer.

---
## `OrderByBeforeCursorAnalyzer` (class)
**Namespace:** `EricksonLopez.Pagination.Analyzers`

### Public Methods

#### `Initialize`

**Signature:**
```csharp
public void Initialize(AnalysisContext context)
```

**Parameters:**
- `AnalysisContext context`

**Return:** `void`

**When to use:** Use in standard implementations of OrderByBeforeCursorAnalyzer.

---
## `SourceGeneratorRecommendationAnalyzer` (class)
**Namespace:** `EricksonLopez.Pagination.Analyzers`

### Public Methods

#### `Initialize`

**Signature:**
```csharp
public void Initialize(AnalysisContext context)
```

**Parameters:**
- `AnalysisContext context`

**Return:** `void`

**When to use:** Use in standard implementations of SourceGeneratorRecommendationAnalyzer.

---
## `CursorPagedResponse` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

No public methods documented for this type.

## `CursorPaginationParametersModelBinder` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `BindModelAsync`

**Signature:**
```csharp
public Task BindModelAsync(ModelBindingContext bindingContext)
```

**Parameters:**
- `ModelBindingContext bindingContext`

**Return:** `Task`

**When to use:** Use in standard implementations of CursorPaginationParametersModelBinder.

---
## `Edge` (record)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

No public methods documented for this type.

## `FilterParametersModelBinder` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `BindModelAsync`

**Signature:**
```csharp
public Task BindModelAsync(ModelBindingContext bindingContext)
```

**Parameters:**
- `ModelBindingContext bindingContext`

**Return:** `Task`

**When to use:** Use in standard implementations of FilterParametersModelBinder.

---
## `PagedResponse` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

No public methods documented for this type.

## `PaginationAspNetCoreOptions` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

No public methods documented for this type.

## `PaginationEndpointExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `AddPaginationValidation`

**Signature:**
```csharp
public RouteHandlerBuilder AddPaginationValidation(this RouteHandlerBuilder builder)
```

**Parameters:**
- `this RouteHandlerBuilder builder`

**Return:** `RouteHandlerBuilder`

**When to use:** Use in standard implementations of PaginationEndpointExtensions.

---
#### `AddPaginationValidation`

**Signature:**
```csharp
public RouteGroupBuilder AddPaginationValidation(this RouteGroupBuilder builder)
```

**Parameters:**
- `this RouteGroupBuilder builder`

**Return:** `RouteGroupBuilder`

**When to use:** Use in standard implementations of PaginationEndpointExtensions.

---
## `PaginationEndpointFilter` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `InvokeAsync`

**Signature:**
```csharp
public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
```

**Parameters:**
- `EndpointFilterInvocationContext context`
- `EndpointFilterDelegate next`

**Return:** `ValueTask<object?>`

**When to use:** Use in standard implementations of PaginationEndpointFilter.

---
## `PaginationETagOptions` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

No public methods documented for this type.

## `PaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `ToPagedResponse`

**Signature:**
```csharp
public PagedResponse<T> ToPagedResponse(this IPagedList<T> pagedList, Microsoft.AspNetCore.Http.HttpRequest request)
```

**Parameters:**
- `this IPagedList<T> pagedList`
- `Microsoft.AspNetCore.Http.HttpRequest request`

**Return:** `PagedResponse<T>`

**When to use:** Use in standard implementations of PaginationExtensions.

---
#### `ToPagedResponse`

**Signature:**
```csharp
public PagedResponse<T> ToPagedResponse(this IPagedList<T> pagedList, string? requestUri)
```

**Parameters:**
- `this IPagedList<T> pagedList`
- `string? requestUri`

**Return:** `PagedResponse<T>`

**When to use:** Use in standard implementations of PaginationExtensions.

---
#### `ToCursorPagedResponse`

**Signature:**
```csharp
public CursorPagedResponse<T> ToCursorPagedResponse(this ICursorPagedList<T> pagedList, Func<T, RawCursorValue> rawCursorSelector, ICursorEncoder? cursorEncoder)
```

**Parameters:**
- `this ICursorPagedList<T> pagedList`
- `Func<T, RawCursorValue> rawCursorSelector`
- `ICursorEncoder? cursorEncoder`

**Return:** `CursorPagedResponse<T>`

**When to use:** Use in standard implementations of PaginationExtensions.

---
#### `ToCursorPagedResponse`

**Signature:**
```csharp
public CursorPagedResponse<T> ToCursorPagedResponse(this ICursorPagedList<T> pagedList, Func<T, TKey> keySelector, ICursorEncoder? cursorEncoder)
```

**Parameters:**
- `this ICursorPagedList<T> pagedList`
- `Func<T, TKey> keySelector`
- `ICursorEncoder? cursorEncoder`

**Return:** `CursorPagedResponse<T>`

**When to use:** Use in standard implementations of PaginationExtensions.

---
## `PaginationJsonSerializerContext` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

No public methods documented for this type.

## `PaginationParametersModelBinder` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `BindModelAsync`

**Signature:**
```csharp
public Task BindModelAsync(ModelBindingContext bindingContext)
```

**Parameters:**
- `ModelBindingContext bindingContext`

**Return:** `Task`

**When to use:** Use in standard implementations of PaginationParametersModelBinder.

---
## `PaginationParametersModelBinderProvider` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `GetBinder`

**Signature:**
```csharp
public IModelBinder? GetBinder(ModelBinderProviderContext context)
```

**Parameters:**
- `ModelBinderProviderContext context`

**Return:** `IModelBinder?`

**When to use:** Use in standard implementations of PaginationParametersModelBinderProvider.

---
## `PaginationResultExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `ToPagedResult`

**Signature:**
```csharp
public IResult ToPagedResult(this IPagedList<T> pagedList, HttpRequest request, TimeSpan? maxAge)
```

**Parameters:**
- `this IPagedList<T> pagedList`
- `HttpRequest request`
- `TimeSpan? maxAge`

**Return:** `IResult`

**When to use:** Use in standard implementations of PaginationResultExtensions.

---
#### `ToCursorPagedResult`

**Signature:**
```csharp
public IResult ToCursorPagedResult(this ICursorPagedList<T> pagedList, Func<T, TKey> keySelector, ICursorEncoder? cursorEncoder, TimeSpan? maxAge)
```

**Parameters:**
- `this ICursorPagedList<T> pagedList`
- `Func<T, TKey> keySelector`
- `ICursorEncoder? cursorEncoder`
- `TimeSpan? maxAge`

**Return:** `IResult`

**When to use:** Use in standard implementations of PaginationResultExtensions.

---
#### `ApplyETagHeaders`

**Signature:**
```csharp
public bool ApplyETagHeaders(this PagedResponse<T> response, HttpContext httpContext, TimeSpan? maxAge, PaginationETagOptions? etagOptions)
```

**Parameters:**
- `this PagedResponse<T> response`
- `HttpContext httpContext`
- `TimeSpan? maxAge`
- `PaginationETagOptions? etagOptions`

**Return:** `bool`

**When to use:** Use in standard implementations of PaginationResultExtensions.

---
#### `ApplyETagHeaders`

**Signature:**
```csharp
public bool ApplyETagHeaders(this CursorPagedResponse<T> response, HttpContext httpContext, TimeSpan? maxAge, PaginationETagOptions? etagOptions)
```

**Parameters:**
- `this CursorPagedResponse<T> response`
- `HttpContext httpContext`
- `TimeSpan? maxAge`
- `PaginationETagOptions? etagOptions`

**Return:** `bool`

**When to use:** Use in standard implementations of PaginationResultExtensions.

---
## `PaginationServiceCollectionExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `AddPagination`

**Signature:**
```csharp
public IServiceCollection AddPagination(this IServiceCollection services, Action<PaginationCoreOptions>? configure, Action<PaginationAspNetCoreOptions>? configureAspNetCore)
```

**Parameters:**
- `this IServiceCollection services`
- `Action<PaginationCoreOptions>? configure`
- `Action<PaginationAspNetCoreOptions>? configureAspNetCore`

**Return:** `IServiceCollection`

**When to use:** Use in standard implementations of PaginationServiceCollectionExtensions.

---
#### `AddPaginationCursorEncoder`

**Signature:**
```csharp
public IServiceCollection AddPaginationCursorEncoder(this IServiceCollection services)
```

**Parameters:**
- `this IServiceCollection services`

**Return:** `IServiceCollection`

**When to use:** Use in standard implementations of PaginationServiceCollectionExtensions.

---
## `RelayPageInfo` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

No public methods documented for this type.

## `SortParametersModelBinder` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

### Public Methods

#### `BindModelAsync`

**Signature:**
```csharp
public Task BindModelAsync(ModelBindingContext bindingContext)
```

**Parameters:**
- `ModelBindingContext bindingContext`

**Return:** `Task`

**When to use:** Use in standard implementations of SortParametersModelBinder.

---
## `PaginationUIOptions` (class)
**Namespace:** `EricksonLopez.Pagination.Blazor`

### Public Methods

No public methods documented for this type.

## `CosmosPaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.Cosmos`

### Public Methods

#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(this Container container, QueryDefinition queryDefinition, CursorPaginationParameters parameters, int? maxPageSize, CancellationToken cancellationToken)
```

**Parameters:**
- `this Container container`
- `QueryDefinition queryDefinition`
- `CursorPaginationParameters parameters`
- `int? maxPageSize`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of CosmosPaginationExtensions.

---
## `CursorSqlBuilder` (class)
**Namespace:** `EricksonLopez.Pagination.Dapper`

### Public Methods

#### `Select`

**Signature:**
```csharp
public CursorSqlBuilder Select(string columns)
```

**Parameters:**
- `string columns`

**Return:** `CursorSqlBuilder`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
#### `UseDialect`

**Signature:**
```csharp
public CursorSqlBuilder UseDialect(DatabaseDialect dialect)
```

**Parameters:**
- `DatabaseDialect dialect`

**Return:** `CursorSqlBuilder`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
#### `From`

**Signature:**
```csharp
public CursorSqlBuilder From(string table)
```

**Parameters:**
- `string table`

**Return:** `CursorSqlBuilder`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
#### `Where`

**Signature:**
```csharp
public CursorSqlBuilder Where(string condition)
```

**Parameters:**
- `string condition`

**Return:** `CursorSqlBuilder`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
#### `OrderBy`

**Signature:**
```csharp
public CursorSqlBuilder OrderBy(string column, SortDirection direction)
```

**Parameters:**
- `string column`
- `SortDirection direction`

**Return:** `CursorSqlBuilder`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
#### `ThenBy`

**Signature:**
```csharp
public CursorSqlBuilder ThenBy(string column, SortDirection direction)
```

**Parameters:**
- `string column`
- `SortDirection direction`

**Return:** `CursorSqlBuilder`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
#### `WithParameters`

**Signature:**
```csharp
public CursorSqlBuilder WithParameters(string cursorParam, string limitParam)
```

**Parameters:**
- `string cursorParam`
- `string limitParam`

**Return:** `CursorSqlBuilder`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
#### `Build`

**Signature:**
```csharp
public string Build(CursorPaginationParameters parameters)
```

**Parameters:**
- `CursorPaginationParameters parameters`

**Return:** `string`

**When to use:** Use in standard implementations of CursorSqlBuilder.

---
## `DbConnectionCursorExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.Dapper`

### Public Methods

#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(this IDbConnection connection, string sql, CursorPaginationParameters parameters, Func<T, TKey> keySelector, Func<string, TKey>? cursorDecoder, object? param, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorPagedListFactory? factory, ICursorDecoderRegistry? decoderRegistry, bool autoReverse, CancellationToken cancellationToken)
```

**Parameters:**
- `this IDbConnection connection`
- `string sql`
- `CursorPaginationParameters parameters`
- `Func<T, TKey> keySelector`
- `Func<string, TKey>? cursorDecoder`
- `object? param`
- `IDbTransaction? transaction`
- `int? commandTimeout`
- `CommandType? commandType`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorPagedListFactory? factory`
- `ICursorDecoderRegistry? decoderRegistry`
- `bool autoReverse`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of DbConnectionCursorExtensions.

---
#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(this IDbConnection connection, string sql, CursorPaginationParameters parameters, Func<T, (TKey1, TKey2)> keySelector, Func<string, (TKey1, TKey2)>? cursorDecoder, object? param, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorPagedListFactory? factory, bool autoReverse, CancellationToken cancellationToken)
```

**Parameters:**
- `this IDbConnection connection`
- `string sql`
- `CursorPaginationParameters parameters`
- `Func<T, (TKey1, TKey2)> keySelector`
- `Func<string, (TKey1, TKey2)>? cursorDecoder`
- `object? param`
- `IDbTransaction? transaction`
- `int? commandTimeout`
- `CommandType? commandType`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorPagedListFactory? factory`
- `bool autoReverse`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of DbConnectionCursorExtensions.

---
#### `ToCursorPagedAsyncEnumerable`

**Signature:**
```csharp
public IAsyncEnumerable<T> ToCursorPagedAsyncEnumerable(this IDbConnection connection, string sql, CursorPaginationParameters parameters, Func<T, TKey> keySelector, Func<string, TKey>? cursorDecoder, object? param, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorDecoderRegistry? decoderRegistry, CancellationToken cancellationToken)
```

**Parameters:**
- `this IDbConnection connection`
- `string sql`
- `CursorPaginationParameters parameters`
- `Func<T, TKey> keySelector`
- `Func<string, TKey>? cursorDecoder`
- `object? param`
- `IDbTransaction? transaction`
- `int? commandTimeout`
- `CommandType? commandType`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorDecoderRegistry? decoderRegistry`
- `CancellationToken cancellationToken`

**Return:** `IAsyncEnumerable<T>`

**When to use:** Use in standard implementations of DbConnectionCursorExtensions.

---
## `DbConnectionPaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.Dapper`

### Public Methods

#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<T>> ToPagedListAsync(this IDbConnection connection, string sql, PaginationParameters parameters, object? param, bool countTotal, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType, int? maxPageSize, IPagedListFactory? factory, CancellationToken cancellationToken)
```

**Parameters:**
- `this IDbConnection connection`
- `string sql`
- `PaginationParameters parameters`
- `object? param`
- `bool countTotal`
- `IDbTransaction? transaction`
- `int? commandTimeout`
- `CommandType? commandType`
- `int? maxPageSize`
- `IPagedListFactory? factory`
- `CancellationToken cancellationToken`

**Return:** `Task<IPagedList<T>>`

**When to use:** Use in standard implementations of DbConnectionPaginationExtensions.

---
#### `ToPagedAsyncEnumerable`

**Signature:**
```csharp
public IAsyncEnumerable<T> ToPagedAsyncEnumerable(this IDbConnection connection, string sql, PaginationParameters parameters, object? param, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType, int? maxPageSize, CancellationToken cancellationToken)
```

**Parameters:**
- `this IDbConnection connection`
- `string sql`
- `PaginationParameters parameters`
- `object? param`
- `IDbTransaction? transaction`
- `int? commandTimeout`
- `CommandType? commandType`
- `int? maxPageSize`
- `CancellationToken cancellationToken`

**Return:** `IAsyncEnumerable<T>`

**When to use:** Use in standard implementations of DbConnectionPaginationExtensions.

---
## `GridReaderPaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.Dapper`

### Public Methods

#### `ReadPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<T>> ReadPagedListAsync(this SqlMapper.GridReader multi, PaginationParameters parameters, bool countTotal, int? maxPageSize, IPagedListFactory? factory)
```

**Parameters:**
- `this SqlMapper.GridReader multi`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `IPagedListFactory? factory`

**Return:** `Task<IPagedList<T>>`

**When to use:** Use in standard implementations of GridReaderPaginationExtensions.

---
## `CompositeQueryableExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

### Public Methods

#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(this IQueryable<T> source, Expression<Func<T, TKey1>> key1Selector, Expression<Func<T, TKey2>> key2Selector, CursorPaginationParameters parameters, SortDirection direction, int defaultPageSize, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TKey1>> key1Selector`
- `Expression<Func<T, TKey2>> key2Selector`
- `CursorPaginationParameters parameters`
- `SortDirection direction`
- `int defaultPageSize`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of CompositeQueryableExtensions.

---
## `KeysetBuilder` (class)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

### Public Methods

#### `Ascending`

**Signature:**
```csharp
public KeysetBuilder<T> Ascending(Expression<Func<T, TProp>> selector)
```

**Parameters:**
- `Expression<Func<T, TProp>> selector`

**Return:** `KeysetBuilder<T>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `Ascending`

**Signature:**
```csharp
public KeysetBuilder<T> Ascending(string propertyName, IEnumerable<string>? allowedProperties)
```

**Parameters:**
- `string propertyName`
- `IEnumerable<string>? allowedProperties`

**Return:** `KeysetBuilder<T>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `Descending`

**Signature:**
```csharp
public KeysetBuilder<T> Descending(Expression<Func<T, TProp>> selector)
```

**Parameters:**
- `Expression<Func<T, TProp>> selector`

**Return:** `KeysetBuilder<T>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `Descending`

**Signature:**
```csharp
public KeysetBuilder<T> Descending(string propertyName, IEnumerable<string>? allowedProperties)
```

**Parameters:**
- `string propertyName`
- `IEnumerable<string>? allowedProperties`

**Return:** `KeysetBuilder<T>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `SortBy`

**Signature:**
```csharp
public KeysetBuilder<T> SortBy(SortParameters parameters, IEnumerable<string>? allowedProperties)
```

**Parameters:**
- `SortParameters parameters`
- `IEnumerable<string>? allowedProperties`

**Return:** `KeysetBuilder<T>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(ICursorPagedListFactory? factory, CancellationToken cancellationToken)
```

**Parameters:**
- `ICursorPagedListFactory? factory`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<TResult>> ToCursorPagedListAsync(Expression<Func<T, TResult>> selector, ICursorPagedListFactory? factory, CancellationToken cancellationToken)
```

**Parameters:**
- `Expression<Func<T, TResult>> selector`
- `ICursorPagedListFactory? factory`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<TResult>>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `ToPagedAsyncEnumerable`

**Signature:**
```csharp
public IAsyncEnumerable<T> ToPagedAsyncEnumerable()
```

**Parameters:**
- None

**Return:** `IAsyncEnumerable<T>`

**When to use:** Use in standard implementations of KeysetBuilder.

---
#### `GetKeysetSchemaFingerprint`

**Signature:**
```csharp
public string GetKeysetSchemaFingerprint()
```

**Parameters:**
- None

**Return:** `string`

**When to use:** Use in standard implementations of KeysetBuilder.

---
## `PostgreSqlPaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

### Public Methods

#### `GetApproximateCountAsync`

**Signature:**
```csharp
public Task<long> GetApproximateCountAsync(this DbContext dbContext, string tableName, string schemaName, CancellationToken cancellationToken)
```

**Parameters:**
- `this DbContext dbContext`
- `string tableName`
- `string schemaName`
- `CancellationToken cancellationToken`

**Return:** `Task<long>`

**When to use:** Use in standard implementations of PostgreSqlPaginationExtensions.

---
#### `BuildRowValuePredicate`

**Signature:**
```csharp
public (string Sql, System.Collections.Generic.Dictionary<string, object> Parameters) BuildRowValuePredicate(System.Collections.Generic.IReadOnlyList<string> columns, System.Collections.Generic.IReadOnlyList<object> values, bool lessThan)
```

**Parameters:**
- `System.Collections.Generic.IReadOnlyList<string> columns`
- `System.Collections.Generic.IReadOnlyList<object> values`
- `bool lessThan`

**Return:** `(string Sql, System.Collections.Generic.Dictionary<string, object> Parameters)`

**When to use:** Use in standard implementations of PostgreSqlPaginationExtensions.

---
## `QueryableCursorProjectionExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

### Public Methods

#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<TResult>> ToCursorPagedListAsync(this IQueryable<T> source, Expression<Func<T, TResult>> selector, Expression<Func<T, TKey>> keySelector, Func<TResult, TKey> resultKeySelector, CursorPaginationParameters parameters, SortDirection direction, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, CancellationToken cancellationToken, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TResult>> selector`
- `Expression<Func<T, TKey>> keySelector`
- `Func<TResult, TKey> resultKeySelector`
- `CursorPaginationParameters parameters`
- `SortDirection direction`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `CancellationToken cancellationToken`
- `IPaginationOptions? options`

**Return:** `Task<ICursorPagedList<TResult>>`

**When to use:** Use in standard implementations of QueryableCursorProjectionExtensions.

---
## `QueryableExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

### Public Methods

#### `Keyset`

**Signature:**
```csharp
public KeysetBuilder<T> Keyset(this IQueryable<T> source, CursorPaginationParameters parameters, int defaultPageSize, ICursorEncoder? cursorEncoder, bool acceptLegacyCursors)
```

**Parameters:**
- `this IQueryable<T> source`
- `CursorPaginationParameters parameters`
- `int defaultPageSize`
- `ICursorEncoder? cursorEncoder`
- `bool acceptLegacyCursors`

**Return:** `KeysetBuilder<T>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<T>> ToPagedListAsync(this IQueryable<T> source, PaginationParameters parameters, bool countTotal, int? maxPageSize, bool useApproximateCount, IPaginationOptions? options, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<T> source`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `bool useApproximateCount`
- `IPaginationOptions? options`
- `CancellationToken cancellationToken`

**Return:** `Task<IPagedList<T>>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<TResult>> ToPagedListAsync(this IQueryable<T> source, Expression<Func<T, TResult>> selector, PaginationParameters parameters, bool countTotal, int? maxPageSize, bool useApproximateCount, CancellationToken cancellationToken, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TResult>> selector`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `bool useApproximateCount`
- `CancellationToken cancellationToken`
- `IPaginationOptions? options`

**Return:** `Task<IPagedList<TResult>>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<T>> ToPagedListAsync(this IQueryable<T> source, FilterParameters filter, SortParameters sortBy, PaginationParameters parameters, bool countTotal, bool useApproximateCount, int? maxPageSize, IEnumerable<string>? allowedProperties, CancellationToken cancellationToken, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `FilterParameters filter`
- `SortParameters sortBy`
- `PaginationParameters parameters`
- `bool countTotal`
- `bool useApproximateCount`
- `int? maxPageSize`
- `IEnumerable<string>? allowedProperties`
- `CancellationToken cancellationToken`
- `IPaginationOptions? options`

**Return:** `Task<IPagedList<T>>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<TResult>> ToPagedListAsync(this IQueryable<T> source, FilterParameters filter, SortParameters sortBy, Expression<Func<T, TResult>> selector, PaginationParameters parameters, bool countTotal, bool useApproximateCount, int? maxPageSize, IEnumerable<string>? allowedProperties, CancellationToken cancellationToken, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `FilterParameters filter`
- `SortParameters sortBy`
- `Expression<Func<T, TResult>> selector`
- `PaginationParameters parameters`
- `bool countTotal`
- `bool useApproximateCount`
- `int? maxPageSize`
- `IEnumerable<string>? allowedProperties`
- `CancellationToken cancellationToken`
- `IPaginationOptions? options`

**Return:** `Task<IPagedList<TResult>>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToPagedListBatchedAsync`

**Signature:**
```csharp
public System.Collections.Generic.IAsyncEnumerable<IPagedList<T>> ToPagedListBatchedAsync(this IQueryable<T> source, int batchSize, CancellationToken cancellationToken, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `int batchSize`
- `CancellationToken cancellationToken`
- `IPaginationOptions? options`

**Return:** `System.Collections.Generic.IAsyncEnumerable<IPagedList<T>>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, CursorPaginationParameters parameters, SortDirection direction, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorPagedListFactory? factory, CancellationToken cancellationToken, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TKey>> keySelector`
- `CursorPaginationParameters parameters`
- `SortDirection direction`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorPagedListFactory? factory`
- `CancellationToken cancellationToken`
- `IPaginationOptions? options`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, TKey? afterKey, CursorPaginationParameters parameters, SortDirection direction, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorPagedListFactory? factory, CancellationToken cancellationToken, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TKey>> keySelector`
- `TKey? afterKey`
- `CursorPaginationParameters parameters`
- `SortDirection direction`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorPagedListFactory? factory`
- `CancellationToken cancellationToken`
- `IPaginationOptions? options`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ToPagedAsyncEnumerable`

**Signature:**
```csharp
public IAsyncEnumerable<T> ToPagedAsyncEnumerable(this IQueryable<T> source, PaginationParameters parameters, int? maxPageSize, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `PaginationParameters parameters`
- `int? maxPageSize`
- `IPaginationOptions? options`

**Return:** `IAsyncEnumerable<T>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `AsPagedAsyncEnumerable`

**Signature:**
```csharp
public IAsyncEnumerable<T> AsPagedAsyncEnumerable(this IQueryable<T> source, PaginationParameters parameters, int? maxPageSize)
```

**Parameters:**
- `this IQueryable<T> source`
- `PaginationParameters parameters`
- `int? maxPageSize`

**Return:** `IAsyncEnumerable<T>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ApplySort`

**Signature:**
```csharp
public IQueryable<T> ApplySort(this IQueryable<T> source, SortParameters sortBy, SortDirection direction, Expression<Func<T, object>>? defaultSort, IEnumerable<string>? allowedProperties)
```

**Parameters:**
- `this IQueryable<T> source`
- `SortParameters sortBy`
- `SortDirection direction`
- `Expression<Func<T, object>>? defaultSort`
- `IEnumerable<string>? allowedProperties`

**Return:** `IQueryable<T>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
#### `ApplyFilter`

**Signature:**
```csharp
public IQueryable<T> ApplyFilter(this IQueryable<T> source, FilterParameters parameters, int maxComplexity, FilterUnknownFieldBehavior unknownFieldBehavior, System.Collections.Generic.IEnumerable<string>? allowedProperties, IPaginationOptions? options)
```

**Parameters:**
- `this IQueryable<T> source`
- `FilterParameters parameters`
- `int maxComplexity`
- `FilterUnknownFieldBehavior unknownFieldBehavior`
- `System.Collections.Generic.IEnumerable<string>? allowedProperties`
- `IPaginationOptions? options`

**Return:** `IQueryable<T>`

**When to use:** Use in standard implementations of QueryableExtensions.

---
## `QueryableOptimizedExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

### Public Methods

#### `ToPagedListDeferredAsync`

**Signature:**
```csharp
public Task<IPagedList<T>> ToPagedListDeferredAsync(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, PaginationParameters parameters, bool countTotal, int? maxPageSize, CancellationToken cancellationToken, int deferredMaxPageSize)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TKey>> keySelector`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `CancellationToken cancellationToken`
- `int deferredMaxPageSize`

**Return:** `Task<IPagedList<T>>`

**When to use:** Use in standard implementations of QueryableOptimizedExtensions.

---
## `PaginationGrpcExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.Grpc`

### Public Methods

#### `ToParameters`

**Signature:**
```csharp
public PaginationParameters ToParameters(this PaginationParametersMessage? message, IPaginationOptions? options)
```

**Parameters:**
- `this PaginationParametersMessage? message`
- `IPaginationOptions? options`

**Return:** `PaginationParameters`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToParameters`

**Signature:**
```csharp
public CursorPaginationParameters ToParameters(this CursorPaginationParametersMessage? message, IPaginationOptions? options)
```

**Parameters:**
- `this CursorPaginationParametersMessage? message`
- `IPaginationOptions? options`

**Return:** `CursorPaginationParameters`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToParameters`

**Signature:**
```csharp
public FilterParameters ToParameters(this FilterParametersMessage? message)
```

**Parameters:**
- `this FilterParametersMessage? message`

**Return:** `FilterParameters`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToParameters`

**Signature:**
```csharp
public SortParameters ToParameters(this SortParametersMessage? message)
```

**Parameters:**
- `this SortParametersMessage? message`

**Return:** `SortParameters`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToMessage`

**Signature:**
```csharp
public PagedListMetadataMessage ToMessage(this IPagedList pagedList)
```

**Parameters:**
- `this IPagedList pagedList`

**Return:** `PagedListMetadataMessage`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToMessage`

**Signature:**
```csharp
public PagedListMetadataMessage ToMessage(this IPagedList<T> pagedList)
```

**Parameters:**
- `this IPagedList<T> pagedList`

**Return:** `PagedListMetadataMessage`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToMessage`

**Signature:**
```csharp
public CursorPagedListMetadataMessage ToMessage(this ICursorPagedList pagedList)
```

**Parameters:**
- `this ICursorPagedList pagedList`

**Return:** `CursorPagedListMetadataMessage`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToMessage`

**Signature:**
```csharp
public CursorPagedListMetadataMessage ToMessage(this ICursorPagedList<T> pagedList)
```

**Parameters:**
- `this ICursorPagedList<T> pagedList`

**Return:** `CursorPagedListMetadataMessage`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToMessage`

**Signature:**
```csharp
public TResponse ToMessage(this IPagedList<TSource> pagedList, TResponse response, Action<TResponse, PagedListMetadataMessage> configureMetadata)
```

**Parameters:**
- `this IPagedList<TSource> pagedList`
- `TResponse response`
- `Action<TResponse, PagedListMetadataMessage> configureMetadata`

**Return:** `TResponse`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
#### `ToMessage`

**Signature:**
```csharp
public TResponse ToMessage(this ICursorPagedList<TSource> pagedList, TResponse response, Action<TResponse, CursorPagedListMetadataMessage> configureMetadata)
```

**Parameters:**
- `this ICursorPagedList<TSource> pagedList`
- `TResponse response`
- `Action<TResponse, CursorPagedListMetadataMessage> configureMetadata`

**Return:** `TResponse`

**When to use:** Use in standard implementations of PaginationGrpcExtensions.

---
## `FindFluentPaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.MongoDB`

### Public Methods

#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<TProjection>> ToPagedListAsync(this IFindFluent<TDocument, TProjection> find, PaginationParameters parameters, bool countTotal, int defaultPageSize, int? maxPageSize, IPagedListFactory? factory, CancellationToken cancellationToken)
```

**Parameters:**
- `this IFindFluent<TDocument, TProjection> find`
- `PaginationParameters parameters`
- `bool countTotal`
- `int defaultPageSize`
- `int? maxPageSize`
- `IPagedListFactory? factory`
- `CancellationToken cancellationToken`

**Return:** `Task<IPagedList<TProjection>>`

**When to use:** Use in standard implementations of FindFluentPaginationExtensions.

---
#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<TDocument>> ToCursorPagedListAsync(this IFindFluent<TDocument, TDocument> find, Expression<Func<TDocument, TKey>> keySelector, CursorPaginationParameters parameters, EricksonLopez.Pagination.Abstractions.SortDirection direction, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorPagedListFactory? factory, ICursorDecoderRegistry? decoderRegistry, CancellationToken cancellationToken)
```

**Parameters:**
- `this IFindFluent<TDocument, TDocument> find`
- `Expression<Func<TDocument, TKey>> keySelector`
- `CursorPaginationParameters parameters`
- `EricksonLopez.Pagination.Abstractions.SortDirection direction`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorPagedListFactory? factory`
- `ICursorDecoderRegistry? decoderRegistry`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<TDocument>>`

**When to use:** Use in standard implementations of FindFluentPaginationExtensions.

---
## `MongoAsyncEnumerableExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.MongoDB`

### Public Methods

#### `ToPagedAsyncEnumerable`

**Signature:**
```csharp
public IAsyncEnumerable<T> ToPagedAsyncEnumerable(this IQueryable<T> source, PaginationParameters parameters, int? maxPageSize)
```

**Parameters:**
- `this IQueryable<T> source`
- `PaginationParameters parameters`
- `int? maxPageSize`

**Return:** `IAsyncEnumerable<T>`

**When to use:** Use in standard implementations of MongoAsyncEnumerableExtensions.

---
## `MongoCursorPaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.MongoDB`

### Public Methods

#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<T>> ToCursorPagedListAsync(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, CursorPaginationParameters parameters, EricksonLopez.Pagination.Abstractions.SortDirection direction, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorPagedListFactory? factory, ICursorDecoderRegistry? decoderRegistry, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TKey>> keySelector`
- `CursorPaginationParameters parameters`
- `EricksonLopez.Pagination.Abstractions.SortDirection direction`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorPagedListFactory? factory`
- `ICursorDecoderRegistry? decoderRegistry`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<T>>`

**When to use:** Use in standard implementations of MongoCursorPaginationExtensions.

---
#### `ToCursorPagedListAsync`

**Signature:**
```csharp
public Task<ICursorPagedList<TProjection>> ToCursorPagedListAsync(this IQueryable<TDocument> source, Expression<Func<TDocument, TKey>> keySelector, Expression<Func<TDocument, TProjection>> projection, CursorPaginationParameters parameters, EricksonLopez.Pagination.Abstractions.SortDirection direction, int defaultPageSize, int? maxPageSize, ICursorEncoder? cursorEncoder, ICursorPagedListFactory? factory, ICursorDecoderRegistry? decoderRegistry, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<TDocument> source`
- `Expression<Func<TDocument, TKey>> keySelector`
- `Expression<Func<TDocument, TProjection>> projection`
- `CursorPaginationParameters parameters`
- `EricksonLopez.Pagination.Abstractions.SortDirection direction`
- `int defaultPageSize`
- `int? maxPageSize`
- `ICursorEncoder? cursorEncoder`
- `ICursorPagedListFactory? factory`
- `ICursorDecoderRegistry? decoderRegistry`
- `CancellationToken cancellationToken`

**Return:** `Task<ICursorPagedList<TProjection>>`

**When to use:** Use in standard implementations of MongoCursorPaginationExtensions.

---
## `MongoOffsetPaginationExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.MongoDB`

### Public Methods

#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<T>> ToPagedListAsync(this IQueryable<T> source, PaginationParameters parameters, bool countTotal, int? maxPageSize, IPagedListFactory? factory, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<T> source`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `IPagedListFactory? factory`
- `CancellationToken cancellationToken`

**Return:** `Task<IPagedList<T>>`

**When to use:** Use in standard implementations of MongoOffsetPaginationExtensions.

---
#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<TResult>> ToPagedListAsync(this IQueryable<T> source, Expression<Func<T, TResult>> selector, PaginationParameters parameters, bool countTotal, int? maxPageSize, IPagedListFactory? factory, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<T> source`
- `Expression<Func<T, TResult>> selector`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `IPagedListFactory? factory`
- `CancellationToken cancellationToken`

**Return:** `Task<IPagedList<TResult>>`

**When to use:** Use in standard implementations of MongoOffsetPaginationExtensions.

---
#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<T>> ToPagedListAsync(this IQueryable<T> source, FilterParameters filter, SortParameters sortBy, PaginationParameters parameters, bool countTotal, int? maxPageSize, IPagedListFactory? factory, FilterUnknownFieldBehavior unknownFieldBehavior, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<T> source`
- `FilterParameters filter`
- `SortParameters sortBy`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `IPagedListFactory? factory`
- `FilterUnknownFieldBehavior unknownFieldBehavior`
- `CancellationToken cancellationToken`

**Return:** `Task<IPagedList<T>>`

**When to use:** Use in standard implementations of MongoOffsetPaginationExtensions.

---
#### `ToPagedListAsync`

**Signature:**
```csharp
public Task<IPagedList<TResult>> ToPagedListAsync(this IQueryable<T> source, FilterParameters filter, SortParameters sortBy, Expression<Func<T, TResult>> selector, PaginationParameters parameters, bool countTotal, int? maxPageSize, IPagedListFactory? factory, FilterUnknownFieldBehavior unknownFieldBehavior, CancellationToken cancellationToken)
```

**Parameters:**
- `this IQueryable<T> source`
- `FilterParameters filter`
- `SortParameters sortBy`
- `Expression<Func<T, TResult>> selector`
- `PaginationParameters parameters`
- `bool countTotal`
- `int? maxPageSize`
- `IPagedListFactory? factory`
- `FilterUnknownFieldBehavior unknownFieldBehavior`
- `CancellationToken cancellationToken`

**Return:** `Task<IPagedList<TResult>>`

**When to use:** Use in standard implementations of MongoOffsetPaginationExtensions.

---
## `MongoQueryableFilterExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.MongoDB`

### Public Methods

#### `ApplySort`

**Signature:**
```csharp
public IQueryable<T> ApplySort(this IQueryable<T> source, SortParameters sortBy, EricksonLopez.Pagination.Abstractions.SortDirection direction, Expression<Func<T, object>>? defaultSort)
```

**Parameters:**
- `this IQueryable<T> source`
- `SortParameters sortBy`
- `EricksonLopez.Pagination.Abstractions.SortDirection direction`
- `Expression<Func<T, object>>? defaultSort`

**Return:** `IQueryable<T>`

**When to use:** Use in standard implementations of MongoQueryableFilterExtensions.

---
#### `ApplyFilter`

**Signature:**
```csharp
public IQueryable<T> ApplyFilter(this IQueryable<T> source, FilterParameters filter, int maxComplexity, FilterUnknownFieldBehavior unknownFieldBehavior)
```

**Parameters:**
- `this IQueryable<T> source`
- `FilterParameters filter`
- `int maxComplexity`
- `FilterUnknownFieldBehavior unknownFieldBehavior`

**Return:** `IQueryable<T>`

**When to use:** Use in standard implementations of MongoQueryableFilterExtensions.

---
## `PaginationOperationFilter` (class)
**Namespace:** `EricksonLopez.Pagination.OpenApi`

### Public Methods

#### `Apply`

**Signature:**
```csharp
public void Apply(OpenApiOperation operation, OperationFilterContext context)
```

**Parameters:**
- `OpenApiOperation operation`
- `OperationFilterContext context`

**Return:** `void`

**When to use:** Use in standard implementations of PaginationOperationFilter.

---
## `PaginationSwaggerGenOptionsExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.OpenApi`

### Public Methods

#### `AddPaginationSupport`

**Signature:**
```csharp
public void AddPaginationSupport(this SwaggerGenOptions options)
```

**Parameters:**
- `this SwaggerGenOptions options`

**Return:** `void`

**When to use:** Use in standard implementations of PaginationSwaggerGenOptionsExtensions.

---
## `CursorDecoderGenerator` (class)
**Namespace:** `EricksonLopez.Pagination.SourceGenerators`

### Public Methods

#### `Initialize`

**Signature:**
```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
```

**Parameters:**
- `IncrementalGeneratorInitializationContext context`

**Return:** `void`

**When to use:** Use in standard implementations of CursorDecoderGenerator.

---

## `ICursorReplayStore` (interface)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

Defines a store for recording and validating single-use cursor nonces to protect against replay attacks.

### Public Methods

#### `TryAcquireNonce`
**Signature:**
```csharp
public bool TryAcquireNonce(string nonce, TimeSpan timeToLive)
```
**Parameters:**
- `string nonce`: The unique nonce embedded within the cursor.
- `TimeSpan timeToLive`: The duration during which the nonce must be remembered.

**Return:** `bool` (`true` if nonce was fresh and recorded; `false` if replayed).

**When to use:** In cursor verification pipelines to ensure single-use replay protection.

#### `TryAcquireNonceAsync`
**Signature:**
```csharp
public Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default)
```
**Parameters:**
- `string nonce`
- `TimeSpan timeToLive`
- `CancellationToken cancellationToken`

**Return:** `Task<bool>`

---

## `InMemoryCursorReplayStore` (class)
**Namespace:** `EricksonLopez.Pagination`

In-memory reference implementation of `ICursorReplayStore` with thread-safe nonce eviction.

### Public Methods

#### `TryAcquireNonce`
**Signature:**
```csharp
public bool TryAcquireNonce(string nonce, TimeSpan timeToLive)
```
**Return:** `bool`

---

## `ReplayedPaginationCursorException` (class)
**Namespace:** `EricksonLopez.Pagination.Abstractions`

Exception thrown when a single-use pagination cursor is consumed more than once (replay attack detected). Inherits from `InvalidPaginationCursorException`.

### Public Properties
- `string? Nonce`: The extracted nonce that was detected as replayed.
- `string? OpaqueCursor`: The opaque cursor string that triggered the replay detection.

---

## `DapperKeysetBuilder<T>` (class)
**Namespace:** `EricksonLopez.Pagination.Dapper`

A fluent builder for multi-column keyset (cursor) pagination queries executed via Dapper. Combines `CursorSqlBuilder` SQL generation with typed cursor encoding/decoding.

### Public Methods

#### `Select`
**Signature:**
```csharp
public DapperKeysetBuilder<T> Select(string columns)
```
**Return:** `DapperKeysetBuilder<T>`

#### `From`
**Signature:**
```csharp
public DapperKeysetBuilder<T> From(string table)
```
**Return:** `DapperKeysetBuilder<T>`

#### `Where`
**Signature:**
```csharp
public DapperKeysetBuilder<T> Where(string condition)
```
**Return:** `DapperKeysetBuilder<T>`

#### `OrderBy`
**Signature:**
```csharp
public DapperKeysetBuilder<T> OrderBy(string column, SortDirection direction = SortDirection.Ascending)
```
**Return:** `DapperKeysetBuilder<T>`

#### `ThenBy`
**Signature:**
```csharp
public DapperKeysetBuilder<T> ThenBy(string column, SortDirection direction = SortDirection.Ascending)
```
**Return:** `DapperKeysetBuilder<T>`

#### `WithCursorColumns`
**Signature:**
```csharp
public DapperKeysetBuilder<T> WithCursorColumns(params Func<T, string?>[] selectors)
```
**Return:** `DapperKeysetBuilder<T>`

#### `WithCursorDecoder`
**Signature:**
```csharp
public DapperKeysetBuilder<T> WithCursorDecoder(params Func<string[], object?>[] decoders)
```
**Return:** `DapperKeysetBuilder<T>`

#### `UseDialect`
**Signature:**
```csharp
public DapperKeysetBuilder<T> UseDialect(DatabaseDialect dialect)
```
**Return:** `DapperKeysetBuilder<T>`

#### `ExecuteAsync`
**Signature:**
```csharp
public Task<ICursorPagedList<T>> ExecuteAsync(CancellationToken cancellationToken = default)
```
**Return:** `Task<ICursorPagedList<T>>`

**When to use:** For high-performance, multi-column keyset queries in Dapper / micro-ORM environments.

---

## `KeysetPartition<TKey>` (record)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

Represents a bounded keyset partition for parallel execution.

### Propiedades
- `int PartitionIndex`
- `TKey? LowerBound`
- `TKey? UpperBound`
- `string? StartCursor`
- `string? EndCursor`

---

## `KeysetPartitioningExtensions` (class)
**Namespace:** `EricksonLopez.Pagination.EntityFrameworkCore`

Provides extension methods to partition a keyset queryable into multiple disjoint ranges for parallel worker processing.

### Public Methods

#### `SplitKeysetPartitionsAsync` (int key)
**Signature:**
```csharp
public static Task<IReadOnlyList<KeysetPartition<int>>> SplitKeysetPartitionsAsync<T>(
    this IQueryable<T> source,
    Expression<Func<T, int>> keySelector,
    int partitionCount,
    ICursorEncoder? cursorEncoder = null,
    CancellationToken cancellationToken = default)
```
**Return:** `Task<IReadOnlyList<KeysetPartition<int>>>`

#### `SplitKeysetPartitionsAsync` (long key)
**Signature:**
```csharp
public static Task<IReadOnlyList<KeysetPartition<long>>> SplitKeysetPartitionsAsync<T>(
    this IQueryable<T> source,
    Expression<Func<T, long>> keySelector,
    int partitionCount,
    ICursorEncoder? cursorEncoder = null,
    CancellationToken cancellationToken = default)
```
**Return:** `Task<IReadOnlyList<KeysetPartition<long>>>`

---

## `PaginationMetrics` (class)
**Namespace:** `EricksonLopez.Pagination`

Provides OpenTelemetry-compatible metrics instruments for the library.

### Public Fields & Methods
- `const string MeterName`: "EricksonLopez.Pagination"
- `static readonly Meter Meter`: OpenTelemetry Meter instance
- `static readonly Counter<long> QueriesTotal`
- `static readonly Histogram<int> PageSize`
- `static readonly Histogram<int> PageDepth`
- `static readonly Counter<long> CursorErrors`
- `static void RecordCursorError(string errorType)`
- `static void RecordOffsetQuery(int page, int pageSize)`
- `static void RecordKeysetQuery(int pageSize)`

---

## `PaginationDiagnostics` (class)
**Namespace:** `EricksonLopez.Pagination`

Provides global static access to diagnostics (Logging and Metrics) for non-DI extension method contexts.

### Public Properties & Methods
- `static ILoggerFactory? LoggerFactory { get; set; }`
- `static IMeterFactory? MeterFactory { get; set; }`
- `static Counter<long> LegacyCursorCounter { get; }`
- `static ILogger<T>? CreateLogger<T>()`

---

## `PaginationETagOptions` (class)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

Configuration options for ETag generation in `ApplyETagHeaders`, `ToPagedResult`, and `ToCursorPagedResult`.

### Public Properties

#### `CustomETagFactory`

**Signature:**
```csharp
public Func<object, string>? CustomETagFactory { get; init; }
```

**Return:** `string` — the raw ETag value (will be quoted per RFC 7232 automatically)

**Observaciones:**
When `null` (default), the ETag is computed by serializing the response with `JsonSerializer`. When non-null, the factory delegate is invoked with the boxed response object (`PagedResponse<T>` or `CursorPagedResponse<T>`) and must return a deterministic string.

Use `CustomETagFactory` in the following scenarios:
- **Native AOT / Trimming**: `JsonSerializer` requires reflection metadata that may not be available.
- **Performance**: Computing a hash of IDs is cheaper than serializing the entire response.
- **Custom schemas**: When the ETag should reflect a database row version or `UpdatedAt` timestamp.

**Example — AOT-safe ID-based ETag:**
```csharp
var etagOptions = new PaginationETagOptions
{
    CustomETagFactory = obj =>
    {
        if (obj is PagedResponse<Product> r && r.Items.Count > 0)
            return $"{r.Page}:{r.PageSize}:{r.Items[0].Id}-{r.Items[^1].Id}";
        return "empty";
    }
};

bool notModified = pagedResponse.ApplyETagHeaders(
    httpContext,
    maxAge: TimeSpan.FromMinutes(5),
    etagOptions: etagOptions);
```

**When NOT to use:**
- When `JsonSerializer` is available and performance is not a concern — the default strategy is simpler and fully correct.
- Avoid returning non-deterministic values (e.g., `DateTime.UtcNow.Ticks`) as the ETag value — this disables caching effectiveness.

---

## `AddPaginationValidation` (extension method)
**Namespace:** `EricksonLopez.Pagination.AspNetCore`

Registers the `PaginationEndpointFilter` that validates `pageSize` against `PaginationCoreOptions.MaxPageSize`, returning HTTP 400 if the limit is exceeded.

### Overloads

#### Overload A — Individual Endpoint

**Signature:**
```csharp
public static RouteHandlerBuilder AddPaginationValidation(this RouteHandlerBuilder builder)
```

**Parameters:**
- `this RouteHandlerBuilder builder` — the builder returned by `MapGet/MapPost/...`

**Return:** `RouteHandlerBuilder` (for chaining)

**Example:**
```csharp
app.MapGet("/api/products", handler)
   .AddPaginationValidation();
```

---

#### Overload B — RouteGroupBuilder (Group-Level)

**Signature:**
```csharp
public static RouteGroupBuilder AddPaginationValidation(this RouteGroupBuilder group)
```

**Parameters:**
- `this RouteGroupBuilder group` — the builder returned by `app.MapGroup(...)`

**Return:** `RouteGroupBuilder` (for chaining)

**Observaciones:**
When applied to a `RouteGroupBuilder`, all endpoints registered on that group inherit the `PaginationEndpointFilter`. This is the recommended approach for functional areas (export, admin, reporting) where every endpoint must enforce the same page size limit.

**Example:**
```csharp
var exportGroup = app.MapGroup("/api/export")
    .AddPaginationValidation();  // <- all endpoints below are protected

exportGroup.MapGet("/products", handler1);
exportGroup.MapGet("/summary", handler2);
```

**When to use Overload A vs. Overload B:**

| Use Case | Recommended |
|----------|------------|
| Single endpoint requiring protection | Overload A |
| Entire functional area (export, admin) | Overload B |
| Mix: group baseline + per-endpoint override | Both |

**When NOT to use:**
- Do not apply both to the same endpoint — the filter will execute twice and the second check is redundant.
- Do not use for public read endpoints that intentionally expose large page sizes (e.g., internal data-export APIs with authentication bypass).

---

## `EricksonLopez.Pagination.Elasticsearch` — API Reference

**Namespace:** `EricksonLopez.Pagination.Elasticsearch`

### `ElasticsearchCursorHelper` (static class)

Utilities for managing Elasticsearch `search_after` cursor values.

| Method | Signature | Description |
|--------|-----------|-------------|
| `EncodeSort` | `static string? EncodeSort(IReadOnlyCollection<FieldValue>? sortFields, ICursorEncoder? encoder = null)` | Encodes Elasticsearch sort field values into an opaque cursor string. |
| `DecodeSort` | `static FieldValue[]? DecodeSort(string? cursor, ICursorEncoder? encoder = null)` | Decodes a cursor string back into Elasticsearch `FieldValue` array for `search_after`. |

### `ElasticsearchCursorPaginationExtensions` (static class)

Extension methods for Elasticsearch `SearchRequestDescriptor<T>`.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyCursorPagination<T>` | `static SearchRequestDescriptor<T> ApplyCursorPagination<T>(this SearchRequestDescriptor<T> descriptor, CursorPaginationParameters parameters, ICursorEncoder? encoder = null)` | Applies `search_after` + `size` to the search request from decoded cursor parameters. |
| `ToCursorPagedList<T>` | `static CursorPagedList<T> ToCursorPagedList<T>(SearchResponse<T> response, int pageSize, ICursorEncoder? encoder = null)` | Converts an Elasticsearch `SearchResponse<T>` to a `CursorPagedList<T>`. |

---

## `EricksonLopez.Pagination.Redis` — API Reference

**Namespace:** `EricksonLopez.Pagination.Redis`

### `RedisCursorReplayStore` (class)

Distributed implementation of `ICursorReplayStore` using Redis SETNX for atomic nonce registration.

| Method | Signature | Description |
|--------|-----------|-------------|
| `TryAcquireNonce` | `bool TryAcquireNonce(string nonce, TimeSpan timeToLive)` | Synchronously attempts to register a cursor nonce. Returns `false` if the nonce was already used. |
| `TryAcquireNonceAsync` | `Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default)` | Asynchronously attempts to register a cursor nonce. Returns `false` if already used. |

### `RedisCursorReplayStoreOptions` (class)

Configuration options for `RedisCursorReplayStore`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `KeyPrefix` | `string` | `"pagination:replay:"` | Redis key prefix for all nonce entries. |
| `DatabaseIndex` | `int?` | `null` | Optional Redis database index. |

### `RedisPaginationServiceCollectionExtensions` (static class)

| Method | Description |
|--------|-------------|
| `AddPaginationRedisReplayStore(IConnectionMultiplexer)` | Registers `RedisCursorReplayStore` using an existing `IConnectionMultiplexer`. |
| `AddPaginationRedisReplayStore(string connectionString, Action<RedisCursorReplayStoreOptions>?)` | Registers and configures `RedisCursorReplayStore` using a connection string. |

---

## `EricksonLopez.Pagination.Relay` — API Reference

**Namespace:** `EricksonLopez.Pagination.Relay`

### `Connection<TNode>` (class)

GraphQL Relay Connections specification container.

| Member | Type | Description |
|--------|------|-------------|
| `Edges` | `IReadOnlyList<Edge<TNode>>` | The paginated list of edge containers. |
| `PageInfo` | `PageInfo` | Relay pagination metadata. |
| `TotalCount` | `long?` | Optional total item count. |

### `Edge<TNode>` (class)

| Member | Type | Description |
|--------|------|-------------|
| `Node` | `TNode` | The wrapped item. |
| `Cursor` | `string` | Opaque cursor identifying this item's position. |

### `PageInfo` (class)

| Member | Type | Description |
|--------|------|-------------|
| `HasNextPage` | `bool` | Whether more items exist after the current page. |
| `HasPreviousPage` | `bool` | Whether more items exist before the current page. |
| `StartCursor` | `string?` | Cursor of the first item in the current page. |
| `EndCursor` | `string?` | Cursor of the last item in the current page. |

### `RelayPaginationExtensions` (static class)

| Method | Description |
|--------|-------------|
| `ToRelayConnection<T>(this ICursorPagedList<T>, Func<T, string>)` | Converts an `ICursorPagedList<T>` to a Relay `Connection<T>`, applying a cursor selector per item. |
| `ToRelayConnection<TSource, TNode>(this ICursorPagedList<TSource>, Func<TSource, TNode>, Func<TSource, string>)` | Projects source items to a different node type while building the Relay connection. |
| `ToRelayConnection<T>(this ICursorPagedList<T>, ICursorEncoder)` | Converts to Relay connection using the provided `ICursorEncoder` for cursor formatting. |

---

## `EricksonLopez.Pagination.SqlBuilder` — API Reference

**Namespace:** `EricksonLopez.Pagination.SqlBuilder`

### `SqlBuilderPaginationExtensions` (static class)

Extension methods for `EricksonLopez.SqlBuilder.SelectQuery<T>`.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Paginate<T>` | `static SelectQuery<T> Paginate<T>(this SelectQuery<T> query, PaginationParameters parameters)` | Applies OFFSET/FETCH pagination to a SQL AST query using `PaginationParameters`. |
| `Paginate<T>` | `static SelectQuery<T> Paginate<T>(this SelectQuery<T> query, int pageNumber, int pageSize)` | Applies OFFSET/FETCH pagination with explicit page number and size. |
| `ApplyCursor<T, TKey>` | `static SelectQuery<T> ApplyCursor<T, TKey>(...)` | Applies a keyset `WHERE` predicate to the SQL AST query from decoded cursor parameters. |
| `ToCursorPagedList<T, TKey>` | `static CursorPagedList<T> ToCursorPagedList<T, TKey>(...)` | Executes the query and wraps results in a `CursorPagedList<T>`. |
| `ToPagedList<T>` | `static PagedList<T> ToPagedList<T>(...)` | Executes the query and wraps results in a `PagedList<T>`. |

---

## `EricksonLopez.Pagination.LinqToDB` — API Reference

**Namespace:** `EricksonLopez.Pagination.LinqToDB`

### `QueryableLinqToDBExtensions` (static class)

Offset pagination extensions for LinqToDB `IQueryable<T>`.

| Method | Description |
|--------|-------------|
| `ToPagedListAsync<T>(IQueryable<T>, PaginationParameters, CancellationToken)` | Returns an `IPagedList<T>` from a LinqToDB query using offset pagination. |
| `ToPagedListAsync<T, TResult>(IQueryable<T>, Func<T, TResult>, PaginationParameters, CancellationToken)` | Returns a projected `IPagedList<TResult>`. |

### `QueryableCursorProjectionExtensions` (static class)

Cursor-based pagination for LinqToDB.

| Method | Description |
|--------|-------------|
| `ToCursorPagedListAsync<T, TResult, TKey>(...)` | Returns a `ICursorPagedList<TResult>` using keyset cursor projection. |
| `Keyset<T>(IQueryable<T>, CursorPaginationParameters)` | Returns a `KeysetBuilder<T>` for fluent keyset column registration. |

### `KeysetBuilder<T>` (LinqToDB variant)

| Method | Description |
|--------|-------------|
| `Ascending<TProp>(Expression<Func<T, TProp>>)` | Registers an ascending sort column. |
| `Descending<TProp>(Expression<Func<T, TProp>>)` | Registers a descending sort column. |
| `SortBy(SortParameters, IEnumerable<string>?)` | Registers sort columns from dynamic `SortParameters`. |
| `ToCursorPagedListAsync(CancellationToken)` | Executes the keyset query and returns the paged result. |

---

## `EricksonLopez.Pagination.Result` — API Reference

**Namespace:** `EricksonLopez.Pagination.Result`

> [!NOTE]
> This package targets `net10.0` only. It requires the `EricksonLopez.Result` package and uses the `Result<T>` / `Error` types from that library.

### `PaginationErrors` (static class)

Pre-defined `Error` instances for pagination-specific failure scenarios.

| Field | Type | Code | Description |
|-------|------|------|-------------|
| `InvalidCursor` | `Error` | Validation | The cursor format is malformed or the HMAC signature is invalid. |
| `ExpiredCursor` | `Error` | Validation | The cursor was valid but its TTL has elapsed. |
| `ReplayedCursor` | `Error` | Validation | The cursor nonce was already consumed (replay attack detected). |

### `PaginationResultExtensions` (static class)

| Method | Signature | Description |
|--------|-----------|-------------|
| `ExecuteResultAsync<TList>` | `static Task<Result<TList>> ExecuteResultAsync<TList>(Func<Task<TList>> paginationQuery)` | Executes a pagination query and catches `InvalidPaginationCursorException`, `ExpiredPaginationCursorException`, and `ReplayedPaginationCursorException`, mapping them to typed `Error` results. Returns `Result<TList>.Ok` on success. |

