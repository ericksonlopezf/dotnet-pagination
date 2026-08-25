// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using Elastic.Clients.Elasticsearch;
using EricksonLopez.Pagination;

namespace EricksonLopez.Pagination.Elasticsearch;

/// <summary>
/// Provides methods for encoding and decoding Elasticsearch <c>search_after</c> sort tokens.
/// </summary>
public static class ElasticsearchCursorHelper
{
    private static readonly Base64CursorEncoder FallbackEncoder = new();

    /// <summary>
    /// Encodes an Elasticsearch sort array into an opaque pagination cursor string.
    /// </summary>
    /// <param name="sortFields">The sort values returned on a search hit.</param>
    /// <param name="encoder">The cursor encoder to use, or <see langword="null"/> for default base64.</param>
    /// <returns>An opaque cursor string, or <see langword="null"/> if <paramref name="sortFields"/> is empty.</returns>
    public static string? EncodeSort(IReadOnlyCollection<FieldValue>? sortFields, ICursorEncoder? encoder = null)
    {
        if (sortFields is null || sortFields.Count == 0)
        {
            return null;
        }

        var activeEncoder = encoder ?? FallbackEncoder;
        var parts = new string[sortFields.Count];
        int i = 0;
        foreach (var field in sortFields)
        {
            parts[i++] = field.ToString();
        }

        string joined = string.Join("\x1F", parts);
        return activeEncoder.Encode(joined);
    }

    /// <summary>
    /// Decodes an opaque pagination cursor into Elasticsearch <see cref="FieldValue"/> elements for <c>search_after</c>.
    /// </summary>
    /// <param name="cursor">The opaque cursor string.</param>
    /// <param name="encoder">The cursor encoder to use, or <see langword="null"/> for default base64.</param>
    /// <returns>An array of <see cref="FieldValue"/> objects, or <see langword="null"/> if cursor is null or empty.</returns>
    public static FieldValue[]? DecodeSort(string? cursor, ICursorEncoder? encoder = null)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        var activeEncoder = encoder ?? FallbackEncoder;
        string? decoded = activeEncoder.Decode(cursor);
        if (string.IsNullOrEmpty(decoded))
        {
            return null;
        }

        string[] parts = decoded.Split('\x1F');
        var fieldValues = new FieldValue[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            string raw = parts[i];
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
            {
                fieldValues[i] = FieldValue.Long(l);
            }
            else if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                fieldValues[i] = FieldValue.Double(d);
            }
            else if (bool.TryParse(raw, out bool b))
            {
                fieldValues[i] = FieldValue.Boolean(b);
            }
            else
            {
                fieldValues[i] = FieldValue.String(raw);
            }
        }

        return fieldValues;
    }
}
