// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Defines a store for recording and validating single-use cursor nonces to protect against replay attacks.
/// </summary>
public interface ICursorReplayStore
{
    /// <summary>
    /// Attempts to acquire and record a cursor nonce.
    /// </summary>
    /// <param name="nonce">The unique nonce string embedded within the cursor.</param>
    /// <param name="timeToLive">The duration for which the nonce must be retained in the store.</param>
    /// <returns><see langword="true"/> if the nonce was fresh and successfully recorded; otherwise, <see langword="false"/> if it was previously consumed.</returns>
    bool TryAcquireNonce(string nonce, TimeSpan timeToLive);

    /// <summary>
    /// Attempts to acquire and record a cursor nonce asynchronously.
    /// </summary>
    /// <param name="nonce">The unique nonce string embedded within the cursor.</param>
    /// <param name="timeToLive">The duration for which the nonce must be retained in the store.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains <see langword="true"/> if the nonce was fresh and successfully recorded; otherwise, <see langword="false"/> if it was previously consumed.
    /// </returns>
    Task<bool> TryAcquireNonceAsync(string nonce, TimeSpan timeToLive, CancellationToken cancellationToken = default);
}





