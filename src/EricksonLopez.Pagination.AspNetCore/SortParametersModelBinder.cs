// Copyright © Erickson Lopez. MIT License.
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides a model binder for <see cref="SortParameters"/> that extracts sort expressions from the request.
/// </summary>
public sealed class SortParametersModelBinder : IModelBinder
{
    private readonly PaginationCoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SortParametersModelBinder"/> class with the specified options.
    /// </summary>
    /// <param name="options">The pagination options snapshot resolved per-request.</param>
    public SortParametersModelBinder(IOptionsSnapshot<PaginationCoreOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult != ValueProviderResult.None)
        {
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            var value = valueProviderResult.FirstValue;

            if (value != null && value.Length > _options.MaxSortStringLength)
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"The sort expression exceeds the maximum allowed length of {_options.MaxSortStringLength} characters.");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

#if NET7_0_OR_GREATER
            if (SortParameters.TryParse(value, null, out var sort))
            {
                bindingContext.Result = ModelBindingResult.Success(sort);
            }
            else
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"The value '{value}' is not a valid sort expression.");
                bindingContext.Result = ModelBindingResult.Failed();
            }
#else
            bindingContext.Result = ModelBindingResult.Success(SortParameters.From(value));
#endif
        }
        return Task.CompletedTask;
    }
}
