// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Internal;


internal struct FilterClause
{
    public string FieldName;
    public FilterOp Op;
    public string? CustomOp;
    public string Value;
    public bool Negate;
}

