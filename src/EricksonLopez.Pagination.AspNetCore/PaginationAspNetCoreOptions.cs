// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Represents configuration options for integrating pagination with the ASP.NET Core pipeline.
/// </summary>
public class PaginationAspNetCoreOptions
{
    /// <summary>
    /// Gets or sets the zero-based index at which the model binder provider is inserted into the MVC provider list.
    /// </summary>
    public int? ModelBinderProviderInsertIndex { get; set; } = 0;
}
