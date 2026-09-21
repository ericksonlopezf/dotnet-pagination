// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents configuration options for keyset and cursor-based pagination.
/// </summary>
public class PaginationCursorOptions
{
    /// <summary>
    /// Gets or sets the encoder used to serialize and deserialize pagination cursors.
    /// When <see langword="null"/> (the default), <c>AddPagination()</c> substitutes
    /// <c>HmacCursorEncoder.DevelopmentDefault</c> and logs a startup warning reminding you
    /// to configure a production secret key before deploying.
    /// </summary>
    public ICursorEncoder? Encoder { get; set; }

    /// <summary>
    /// Gets or sets an optional delegate used to resolve the current tenant identifier.
    /// When configured, HMAC cursors are cryptographically bound to the current tenant,
    /// preventing cross-tenant cursor replay attacks.
    /// </summary>
    public System.Func<string?>? TenantContextProvider { get; set; }
}

