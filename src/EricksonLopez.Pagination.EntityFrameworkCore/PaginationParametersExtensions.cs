// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

internal static class PaginationParametersExtensions
{
    public static int GetSkip(this PaginationParameters parameters)
    {
        long skip = ((long)parameters.Page - 1L) * parameters.PageSize;
        // Stryker disable once all : Overflow clamping on skip > int.MaxValue
        return skip > int.MaxValue ? int.MaxValue : (int)skip;
    }
}


