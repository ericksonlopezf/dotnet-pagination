// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides a model binder for <see cref="PaginationParameters"/> that extracts page and page size parameters from the request.
/// </summary>
public sealed class PaginationParametersModelBinder : IModelBinder
{
    private readonly PaginationCoreOptions _options;
    private readonly Microsoft.Extensions.Logging.ILogger<PaginationParametersModelBinder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationParametersModelBinder"/> class with the specified options and logger.
    /// </summary>
    /// <param name="options">The pagination options snapshot resolved per-request.</param>
    /// <param name="logger">The logger used for emitting deep offset warnings.</param>
    public PaginationParametersModelBinder(IOptionsSnapshot<PaginationCoreOptions> options, Microsoft.Extensions.Logging.ILogger<PaginationParametersModelBinder> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var pageValue = bindingContext.ValueProvider.GetValue("page");
        var pageSizeValue = bindingContext.ValueProvider.GetValue("pageSize");

        int page = 1;
        if (pageValue.FirstValue is { } p && int.TryParse(p, out var pg))
        {
            if (pg < 1)
            {
                bindingContext.ModelState.AddModelError("page", "page must be greater than or equal to 1.");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            page = pg;
        }

        int pageSize = _options.DefaultPageSize;
        if (pageSizeValue.FirstValue is { } ps && int.TryParse(ps, out var pgs))
        {
            if (pgs < 1)
            {
                bindingContext.ModelState.AddModelError("pageSize", "pageSize must be greater than or equal to 1.");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            pageSize = pgs;
        }

        var maxPageSize = _options.MaxPageSize;
        // Stryker disable once all : Clamping at maxPageSize is equivalent for > vs >= (setting maxPageSize to maxPageSize is a no-op)
        if (pageSize > maxPageSize)
        {
            pageSize = maxPageSize;
        }

        if (_options.DeepOffsetWarningThreshold > 0)
        {
            long offset = (page - 1L) * pageSize;
            if (offset > _options.DeepOffsetWarningThreshold)
            {
                _logger.LogWarning("Pagination request requested an offset of {Offset} which exceeds the configured warning threshold of {Threshold}. This can cause severe performance degradation on large tables.", offset, _options.DeepOffsetWarningThreshold);
            }
        }

        bindingContext.Result = ModelBindingResult.Success(PaginationParameters.Create(page, pageSize));
        return Task.CompletedTask;
    }
}





