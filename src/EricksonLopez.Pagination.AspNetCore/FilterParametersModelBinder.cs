// Copyright © Erickson Lopez. MIT License.
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides a model binder for <see cref="FilterParameters"/> that extracts filter expressions from the request.
/// </summary>
public sealed class FilterParametersModelBinder : IModelBinder
{
    private readonly PaginationCoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterParametersModelBinder"/> class with the specified options.
    /// </summary>
    /// <param name="options">The pagination options snapshot resolved per-request.</param>
    public FilterParametersModelBinder(IOptionsSnapshot<PaginationCoreOptions> options)
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

            if (value != null && value.Length > _options.MaxFilterStringLength)
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"The filter expression exceeds the maximum allowed length of {_options.MaxFilterStringLength} characters.");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

#if NET7_0_OR_GREATER
            if (FilterParameters.TryParse(value, null, out var filter))
            {
                bindingContext.Result = ModelBindingResult.Success(filter);
            }
            else
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"The value '{value}' is not a valid filter expression.");
                bindingContext.Result = ModelBindingResult.Failed();
            }
#else
            bindingContext.Result = ModelBindingResult.Success(FilterParameters.From(value));
#endif
        }
        return Task.CompletedTask;
    }
}
