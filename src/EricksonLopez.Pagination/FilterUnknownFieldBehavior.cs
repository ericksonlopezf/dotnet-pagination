// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Specifies the behavior when a filter parameter references a field that does not exist on the target entity.
/// </summary>
public enum FilterUnknownFieldBehavior
{
    /// <summary>
    /// Ignores unknown fields and continues evaluating remaining filters.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Throws an exception when an unknown field is encountered.
    /// </summary>
    ThrowException = 1
}


