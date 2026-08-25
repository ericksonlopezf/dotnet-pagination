// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Specifies that a source generator should generate a strongly typed, reflection-free <see cref="IFilterProvider{TEntity}"/> for the decorated type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class GenerateFilterProviderAttribute : Attribute
{
}

