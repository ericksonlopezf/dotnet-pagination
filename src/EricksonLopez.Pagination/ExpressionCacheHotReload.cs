// Copyright © Erickson Lopez. MIT License.
// Hot Reload support: When .NET's MetadataUpdater fires (triggered by dotnet watch / VS Hot Reload),
// entity type metadata may have changed (properties added, renamed, removed). The compiled expression
// cache stores expressions keyed partly on `Type.TypeHandle.Value`, which may remain the same after
// a Hot Reload, causing the cache to serve stale compiled predicates against the OLD type definition.
// This results in MemberNotFoundException at runtime or silently-wrong filter behavior.
//
// [MetadataUpdateHandler] requires:
//   1. The assembly-level attribute must appear at the TOP of a file (CS1730 constraint).
//   2. The handler type must have static ClearCache(Type[]?) and/or UpdateApplication(Type[]?) methods.
//
// See: https://learn.microsoft.com/dotnet/api/system.reflection.metadata.metadataupdatehandlerattribute
//
// This attribute is only meaningful in development; in production it is a no-op (Hot Reload is disabled).
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

#if NET6_0_OR_GREATER
[assembly: MetadataUpdateHandler(typeof(EricksonLopez.Pagination.PaginationExpressionCacheHotReloadHandler))]

namespace EricksonLopez.Pagination;


[ExcludeFromCodeCoverage]
internal static class PaginationExpressionCacheHotReloadHandler
{
    /// <summary>
    /// Invoked by the Hot Reload infrastructure when application metadata is updated.
    /// Clears all compiled expression caches so that subsequent filter/sort operations
    /// rebuild expressions against the updated types.
    /// </summary>
    /// <param name="updatedTypes">
    /// The types that were updated (not used; we clear all caches defensively).
    /// </param>
    /// <remarks>
    /// We clear all caches defensively regardless of which types were updated.
    /// The cost of a cache miss is one expression recompilation per unique filter string —
    /// this is negligible in development and this method is never called in production.
    /// </remarks>
    internal static void UpdateApplication(Type[]? updatedTypes)
    {
        PaginationExpressionCache.Clear();
    }

    /// <summary>
    /// Invoked by the Hot Reload infrastructure during a clear-metadata phase.
    /// </summary>
    internal static void ClearCache(Type[]? updatedTypes)
    {
        PaginationExpressionCache.Clear();
    }
}
#endif

