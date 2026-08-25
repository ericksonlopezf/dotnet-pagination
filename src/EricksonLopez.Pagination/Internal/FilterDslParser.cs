// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Internal;

internal static class FilterDslParser
{
    // Ordered from longest to shortest to avoid prefix conflicts (e.g., >= before >)
    private static readonly (string Token, FilterOp Op)[] Operators =
    [
        ("!=",  FilterOp.NotEqual),
        (">=",  FilterOp.GreaterThanOrEqual),
        ("<=",  FilterOp.LessThanOrEqual),
        ("~=",  FilterOp.Contains),
        ("^=",  FilterOp.StartsWith),
        ("$=",  FilterOp.EndsWith),
        (">",   FilterOp.GreaterThan),
        ("<",   FilterOp.LessThan),
        ("=",   FilterOp.Equal),
    ];

    public static FilterClause? Parse(string segment, FilterUnknownFieldBehavior unknownFieldBehavior, IEnumerable<string>? customOperatorTokens = null)
    {
        bool negate = false;
        if (segment.StartsWith('!'))
        {
            negate = true;
            segment = segment.Substring(1).TrimStart();
        }

        int opIdx = 0;
        while (opIdx < segment.Length && (char.IsLetterOrDigit(segment[opIdx]) || segment[opIdx] == '_' || segment[opIdx] == '.'))
        {
            opIdx++;
        }

        if (opIdx == 0 || opIdx == segment.Length) return null;

        var fieldName = segment[..opIdx].Trim();
        
        if (fieldName.Length > 128)
        {
            if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
            {
                throw new ArgumentException($"Invalid filter segment: property name exceeds maximum allowed length of 128 characters.");
            }
            return null;
        }

        var remainder = segment[opIdx..].TrimStart();

        FilterOp? matchedOp = null;
        string? matchedCustomOp = null;
        string? valueStr = null;

        if (customOperatorTokens != null)
        {
            var matchedToken = customOperatorTokens.FirstOrDefault(token => remainder.StartsWith(token, StringComparison.Ordinal));
            if (matchedToken != null)
            {
                matchedOp = FilterOp.Custom;
                matchedCustomOp = matchedToken;
                valueStr = remainder[matchedToken.Length..].Trim();
            }
        }

        if (matchedOp is null)
        {
            foreach (var (token, op) in Operators)
            {
                if (remainder.StartsWith(token, StringComparison.Ordinal))
                {
                    matchedOp = op;
                    valueStr = remainder[token.Length..].Trim();
                    break;
                }
            }
        }

        if (matchedOp is null)
        {
            if (unknownFieldBehavior == FilterUnknownFieldBehavior.ThrowException)
            {
                throw new ArgumentException($"Invalid filter segment: '{segment}'. Unknown operator.");
            }
            return null;
        }

        if (valueStr!.Length >= 2 && valueStr[0] == '"' && valueStr[^1] == '"')
        {
            valueStr = valueStr[1..^1].Replace("\\\"", "\"");
        }

        valueStr = valueStr.Replace("%7C", "|", StringComparison.OrdinalIgnoreCase);

        return new FilterClause
        {
            FieldName = fieldName,
            Op = matchedOp.Value,
            CustomOp = matchedCustomOp,
            Value = valueStr,
            Negate = negate
        };
    }
}

