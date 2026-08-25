// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Result;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Result;

/// <summary>
/// Provides extension methods for executing pagination queries and mapping exceptions to <see cref="Result{T}"/>.
/// </summary>
public static class PaginationResultExtensions
{
    /// <summary>
    /// Executes a pagination query safely, catching cursor-related exceptions and returning a <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TList">The type of the paged list (e.g., <see cref="IPagedList{T}"/>, <see cref="ICursorPagedList{T}"/>).</typeparam>
    /// <param name="paginationQuery">The asynchronous pagination query to execute.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains a result containing the paged list or the specific pagination error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="paginationQuery"/> is <see langword="null"/></exception>
#pragma warning disable CA1031 // Do not catch general exception types
    public static async Task<Result<TList>> ExecuteResultAsync<TList>(Func<Task<TList>> paginationQuery)
    {
        ArgumentNullException.ThrowIfNull(paginationQuery);

        try
        {
            var result = await paginationQuery().ConfigureAwait(false);
            return EricksonLopez.Result.Result.Success(result);
        }
        catch (ExpiredPaginationCursorException)
        {
            return EricksonLopez.Result.Result.Failure<TList>(PaginationErrors.ExpiredCursor);
        }
        catch (ReplayedPaginationCursorException)
        {
            return EricksonLopez.Result.Result.Failure<TList>(PaginationErrors.ReplayedCursor);
        }
        catch (InvalidPaginationCursorException)
        {
            return EricksonLopez.Result.Result.Failure<TList>(PaginationErrors.InvalidCursor);
        }
        catch (Exception ex)
        {
            return EricksonLopez.Result.Result.Failure<TList>(Error.Failure("Pagination.Error", ex.Message));
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types
}
