// Copyright © Erickson Lopez. MIT License.
using System;

namespace DemoApp.Domain;

/// <summary>
/// Represents a product domain entity for pagination demonstrations.
/// </summary>
public class Product
{
    /// <summary>Gets or sets the primary identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the product price.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; set; }
}


