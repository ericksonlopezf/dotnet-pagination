// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Pagination.AspNetCore;

/// <summary>
/// A minimal <see cref="IHostedService"/> that emits a one-time security warning at application
/// startup when the fallback development HMAC key is active for cursor signing.
/// </summary>
/// <remarks>
/// SEC-1: The development HMAC key provides NO tamper protection because it is a known fallback value.
/// To eliminate this warning, configure <c>HmacCursorEncoder</c> in <c>AddPagination</c>:
/// <code>
/// services.AddPagination(options => {
///     options.Cursor.Encoder = new HmacCursorEncoder(secretKey, timeToLive: TimeSpan.FromMinutes(30));
/// });
/// </code>
/// Generate a secure key with: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
/// and store it in a secrets manager (Azure Key Vault, AWS Secrets Manager, User Secrets).
/// </remarks>

[ExcludeFromCodeCoverage]
internal sealed class PaginationStartupSecurityWarning : IHostedService
{
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationStartupSecurityWarning"/> class.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create a logger for emitting the security warning.</param>
    public PaginationStartupSecurityWarning(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var logger = _loggerFactory?.CreateLogger("EricksonLopez.Pagination.Security");
        logger?.LogWarning(
            "[EricksonLopez.Pagination] SECURITY: The library is using a fallback development HMAC key for cursor signing. " +
            "This key is hardcoded and provides NO security against cursor forgery in a production environment. " +
            "Configure a secure key via HmacCursorEncoder to enable true HMAC-SHA256 signing: " +
            "services.AddPagination(o => o.Cursor.Encoder = new HmacCursorEncoder(secretKey));");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}




