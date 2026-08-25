// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text.Json.Serialization;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// Provides a source-generated JSON serializer context for non-generic pagination metadata types.
/// </summary>
[JsonSerializable(typeof(RelayPageInfo))]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class PaginationJsonSerializerContext : JsonSerializerContext
{
}

