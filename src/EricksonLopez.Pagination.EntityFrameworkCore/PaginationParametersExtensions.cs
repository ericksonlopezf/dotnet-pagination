// Copyright © Erickson Lopez. MIT License.
// Stryker disable all : Equivalent mutants and edge cases safely ignored.
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

internal static class PaginationParametersExtensions
{
    public static int GetSkip(this PaginationParameters parameters)
    {
        return (parameters.Page - 1) * parameters.PageSize;
    }
}


