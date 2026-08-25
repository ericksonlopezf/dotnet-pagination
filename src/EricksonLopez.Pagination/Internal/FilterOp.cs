// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Pagination.Internal;

internal enum FilterOp
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    Custom
}
