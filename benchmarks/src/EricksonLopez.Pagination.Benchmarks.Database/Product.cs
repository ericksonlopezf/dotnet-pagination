// Copyright © Erickson Lopez. MIT License.
using System;
using Sieve.Attributes;

namespace EricksonLopez.Pagination.Benchmarks.Database;

public class Product
{
    [Sieve(CanFilter = true, CanSort = true)]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    
    [Sieve(CanFilter = true, CanSort = true)]
    public string Category { get; set; } = null!;
    
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal Price { get; set; }
    
    [Sieve(CanSort = true)]
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public string Sku { get; set; } = null!;
    public string Manufacturer { get; set; } = null!;
    public string Country { get; set; } = null!;
    public double Weight { get; set; }
    public double Rating { get; set; }
    public int TotalSales { get; set; }
}
