// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Pagination.Benchmarks.Common;

public class PaginationParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // For keyset pagination
    public int? ReferenceId { get; set; }
    public DateTime? ReferenceDate { get; set; }
    
    // For sorting
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    
    // For filtering
    public string? FilterCategory { get; set; }
    public decimal? MinPrice { get; set; }
}


