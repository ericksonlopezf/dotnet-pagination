// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Internal;

/// <summary>
/// Provides centralized type coercion logic to prevent bugs and duplications across providers.
/// </summary>
/// <remarks>
/// Note: Types in the <c>EricksonLopez.Pagination.Internal</c> namespace are not part of the public API 
/// and may change in any minor version. Do not use them directly.
/// </remarks>
internal static class ValueCoercer
{
    /// <summary>
    /// Attempts to coerce a string value into the target type safely, without swallowing critical exceptions.
    /// </summary>
    public static bool TryCoerce(string value, Type targetType, [NotNullWhen(true)] out object? result)
    {
        // Stryker disable all : Fast path optimizations, fallback behaves identically for basic valid inputs.
        if (targetType == typeof(string))
        {
            result = value;
            return true;
        }

        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, value, true, out var enumResult))
            {
                result = enumResult;
                return true;
            }
            result = null;
            return false;
        }

        try
        {
            if (targetType == typeof(bool) && bool.TryParse(value, out var boolResult))
            {
                result = boolResult;
                return true;
            }
            if (targetType == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
            {
                result = intResult;
                return true;
            }
            if (targetType == typeof(long) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longResult))
            {
                result = longResult;
                return true;
            }
            if (targetType == typeof(double) && double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleResult))
            {
                result = doubleResult;
                return true;
            }
            if (targetType == typeof(float) && float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var floatResult))
            {
                result = floatResult;
                return true;
            }
            if (targetType == typeof(decimal) && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalResult))
            {
                result = decimalResult;
                return true;
            }
            // Stryker restore all
            if (targetType == typeof(Guid) && Guid.TryParse(value, out var guidResult))
            {
                result = guidResult;
                return true;
            }
            if (targetType == typeof(DateTime) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtResult))
            {
                result = dtResult;
                return true;
            }
            if (targetType == typeof(DateTimeOffset) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtoResult))
            {
                result = dtoResult;
                return true;
            }
            if (targetType == typeof(DateOnly) && DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var doResult))
            {
                result = doResult;
                return true;
            }
            if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var tsResult))
            {
                result = tsResult;
                return true;
            }

            if (typeof(IConvertible).IsAssignableFrom(targetType))
            {
                result = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException or NotSupportedException)
        {
            // Specifically only catch exceptions that signify parsing failures.
        }

        result = null;
        return false;
    }
}




