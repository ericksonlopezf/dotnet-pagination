// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a contract for providing custom filter operators to extend dynamic filter DSL evaluation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IFilterOperatorProvider<TEntity>
{
    /// <summary>
    /// Gets the dictionary of custom operators registered for the entity type, keyed by operator symbol (e.g. "%=", "in", "geo_near").
    /// </summary>
    IReadOnlyDictionary<string, FilterOperatorHandler<TEntity>> Operators { get; }
}
