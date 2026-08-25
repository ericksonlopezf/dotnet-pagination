// Copyright © Erickson Lopez. MIT License.
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides a model binder for <see cref="CursorPaginationParameters"/> that extracts cursor pagination parameters from the request.
/// </summary>
public sealed class CursorPaginationParametersModelBinder : IModelBinder
{
    private readonly PaginationCoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorPaginationParametersModelBinder"/> class with the specified options.
    /// </summary>
    /// <param name="options">The pagination options snapshot resolved per-request.</param>
    public CursorPaginationParametersModelBinder(IOptionsSnapshot<PaginationCoreOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var firstValue = bindingContext.ValueProvider.GetValue("first");
        var lastValue = bindingContext.ValueProvider.GetValue("last");
        var afterValue = bindingContext.ValueProvider.GetValue("after");
        var beforeValue = bindingContext.ValueProvider.GetValue("before");

        int? first = null;
        if (firstValue.FirstValue is { } f && int.TryParse(f, out var fi))
        {
            if (fi < 1)
            {
                bindingContext.ModelState.AddModelError("first", "first must be greater than or equal to 1.");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            first = fi;
        }

        int? last = null;
        if (lastValue.FirstValue is { } l && int.TryParse(l, out var la))
        {
            if (la < 1)
            {
                bindingContext.ModelState.AddModelError("last", "last must be greater than or equal to 1.");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            last = la;
        }

        var maxPageSize = _options.MaxPageSize;
        // Stryker disable once all : Clamping at maxPageSize is equivalent for > vs >= (setting maxPageSize to maxPageSize is a no-op)
        if (first.HasValue && first.Value > maxPageSize)
        {
            first = maxPageSize;
        }
        // Stryker disable once all : Clamping at maxPageSize is equivalent for > vs >= (setting maxPageSize to maxPageSize is a no-op)
        if (last.HasValue && last.Value > maxPageSize)
        {
            last = maxPageSize;
        }

        bindingContext.Result = ModelBindingResult.Success(new CursorPaginationParameters
        {
            First = first,
            Last = last,
            After = afterValue.FirstValue,
            Before = beforeValue.FirstValue
        });

        return Task.CompletedTask;
    }
}
