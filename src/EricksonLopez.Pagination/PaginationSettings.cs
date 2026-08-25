// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Pagination;

/// <summary>
/// Provides global constant defaults for pagination. 
/// These settings are used as fallbacks by extension methods when no specific value is provided 
/// and DI configuration is unavailable.
/// </summary>
internal static class PaginationSettings
{
    /// <summary>
    /// The default absolute maximum allowed page size.
    /// Default is 1000.
    /// </summary>
    public const int MaxPageSize = 1000;

    /// <summary>
    /// The default deep offset warning threshold.
    /// Default is 0 (disabled).
    /// </summary>
    public const int DeepOffsetWarningThreshold = 0;
}
