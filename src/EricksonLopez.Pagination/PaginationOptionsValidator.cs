// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Extensions.Options;

namespace EricksonLopez.Pagination;

/// <summary>
/// Validates <see cref="PaginationCoreOptions"/> instances.
/// </summary>
[OptionsValidator]
[ExcludeFromCodeCoverage]
public partial class PaginationCoreOptionsValidator : IValidateOptions<PaginationCoreOptions>
{
}

