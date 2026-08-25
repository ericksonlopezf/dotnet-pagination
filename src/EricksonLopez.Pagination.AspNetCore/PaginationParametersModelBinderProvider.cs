// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides a model binder provider for pagination parameter types to the MVC model binding pipeline.
/// </summary>
public sealed class PaginationParametersModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (context.Metadata.ModelType == typeof(PaginationParameters))
        {
            // Use BinderTypeModelBinder so that ASP.NET Core DI resolves the binder
            // instance per request, enabling IOptionsSnapshot<PaginationCoreOptions> to work
            // correctly in multi-tenant scenarios.
            return new BinderTypeModelBinder(typeof(PaginationParametersModelBinder));
        }

        if (context.Metadata.ModelType == typeof(CursorPaginationParameters))
        {
            return new BinderTypeModelBinder(typeof(CursorPaginationParametersModelBinder));
        }

        if (context.Metadata.ModelType == typeof(FilterParameters))
        {
            return new BinderTypeModelBinder(typeof(FilterParametersModelBinder));
        }

        if (context.Metadata.ModelType == typeof(SortParameters))
        {
            return new BinderTypeModelBinder(typeof(SortParametersModelBinder));
        }

        return null;
    }
}
