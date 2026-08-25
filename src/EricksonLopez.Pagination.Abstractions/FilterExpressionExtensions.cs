// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Provides extension methods for combining and manipulating strongly typed filter expressions.
/// </summary>
public static class FilterExpressionExtensions
{
    /// <summary>
    /// Combines two predicate expressions using a logical conditional AND operator.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="first">The first predicate expression.</param>
    /// <param name="second">The second predicate expression to combine.</param>
    /// <returns>A new lambda expression representing the logical conjunction of both predicates.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is <see langword="null"/></exception>
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));

        return Combine(first, second, Expression.AndAlso);
    }

    /// <summary>
    /// Combines two predicate expressions using a logical conditional OR operator.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="first">The first predicate expression.</param>
    /// <param name="second">The second predicate expression to combine.</param>
    /// <returns>A new lambda expression representing the logical disjunction of both predicates.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is <see langword="null"/></exception>
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));

        return Combine(first, second, Expression.OrElse);
    }

    /// <summary>
    /// Negates a predicate expression using a logical NOT operator.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="expression">The predicate expression to negate.</param>
    /// <returns>A new lambda expression representing the logical negation of the specified predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/></exception>
    public static Expression<Func<T, bool>> Not<T>(
        this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var parameter = expression.Parameters[0];
        var body = Expression.Not(expression.Body);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        // Stryker disable String
        var parameter = Expression.Parameter(typeof(T), "x");
        // Stryker restore String

        var leftVisitor = new ReplaceParameterVisitor(first.Parameters[0], parameter);
        var left = leftVisitor.Visit(first.Body);

        var rightVisitor = new ReplaceParameterVisitor(second.Parameters[0], parameter);
        var right = rightVisitor.Visit(second.Body);

        return Expression.Lambda<Func<T, bool>>(merge(left, right), parameter);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}


