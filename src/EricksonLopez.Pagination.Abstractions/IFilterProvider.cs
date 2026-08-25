// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines an AOT-safe contract for building strongly typed filter expressions from filter parameters without relying on runtime reflection.
/// </summary>
/// <typeparam name="TEntity">The type of the entity to filter.</typeparam>
public interface IFilterProvider<TEntity>
{
    /// <summary>
    /// Builds a predicate expression for filtering entities based on the specified filter parameters.
    /// </summary>
    /// <param name="filter">The filter parameters containing field constraints and comparison expressions.</param>
    /// <returns>A lambda expression predicate representing the filtering logic, or <see langword="null"/> if no filter is specified.</returns>
    Expression<Func<TEntity, bool>>? Build(FilterParameters filter);
}


