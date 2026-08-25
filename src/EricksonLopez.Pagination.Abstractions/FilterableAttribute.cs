// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Specifies that a property is explicitly permitted for dynamic filtering operations.
/// </summary>
/// <remarks>
/// When applied to one or more properties in a type, only properties decorated with this attribute can be used in dynamic filter expressions.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class FilterableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterableAttribute"/> class.
    /// </summary>
    public FilterableAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterableAttribute"/> class with a custom alias name.
    /// </summary>
    /// <param name="name">The custom alias name used to reference this property in filter expressions.</param>
    public FilterableAttribute(string? name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets or sets the custom alias name for the property when referenced in dynamic filter expressions.
    /// </summary>
    public string? Name { get; set; }
}

