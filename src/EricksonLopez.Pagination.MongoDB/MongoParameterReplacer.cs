// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;

namespace EricksonLopez.Pagination.MongoDB;

internal sealed class MongoParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly ParameterExpression _newParam;

    public MongoParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
    {
        _oldParam = oldParam;
        _newParam = newParam;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // Stryker disable once Conditional : _newParam vs base.VisitParameter tested correctly by replacement
        return node == _oldParam ? _newParam : base.VisitParameter(node);
    }
}
