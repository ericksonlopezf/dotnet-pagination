// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents a custom filter operator delegate that constructs an expression predicate given a target property expression and a raw filter value.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being filtered.</typeparam>
/// <param name="propertyExpression">The member expression representing the entity's property.</param>
/// <param name="rawValue">The raw string value supplied in the filter parameter.</param>
/// <returns>A boolean expression predicate, or <see langword="null"/> if the value cannot be converted or applied.</returns>
public delegate Expression? FilterOperatorHandler<TEntity>(Expression propertyExpression, string rawValue);
