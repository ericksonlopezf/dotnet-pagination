// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Grpc;

/// <summary>
/// Provides extension methods for converting between gRPC protobuf messages and pagination abstractions.
/// </summary>
public static class PaginationGrpcExtensions
{
    /// <summary>
    /// The absolute maximum page size for gRPC calls when no <see cref="IPaginationOptions"/> is provided.
    /// This fallback prevents unbounded result sets when options are not configured.
    /// </summary>
    /// <remarks>
    /// GR-1: Reduced from 100,000 to 1,000 to match <c>PaginationCoreOptions.MaxPageSize</c> default.
    /// A fallback of 100,000 rows is a potential DoS vector when <see cref="IPaginationOptions"/> is not registered.
    /// </remarks>
    private const int DefaultGrpcMaxPageSize = 1_000;
    /// <summary>
    /// Converts a <see cref="PaginationParametersMessage"/> to <see cref="PaginationParameters"/>.
    /// </summary>
    /// <param name="message">The gRPC message to convert.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>The equivalent <see cref="PaginationParameters"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="message"/> specifies a page size exceeding the maximum allowed limit</exception>
    public static PaginationParameters ToParameters(this PaginationParametersMessage? message, IPaginationOptions? options = null)
    {
        if (message == null) return PaginationParameters.Default;

        int maxPageSize = options?.MaxPageSize ?? DefaultGrpcMaxPageSize;
        if (message.PageSize > maxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(message), $"PageSize cannot exceed {maxPageSize}.");
        }

        return new PaginationParameters
        {
            Page = message.Page > 0 ? message.Page : 1,
            PageSize = message.PageSize > 0 ? message.PageSize : 10
        };
    }

    /// <summary>
    /// Converts a <see cref="CursorPaginationParametersMessage"/> to <see cref="CursorPaginationParameters"/>.
    /// </summary>
    /// <param name="message">The gRPC message to convert.</param>
    /// <param name="options">Optional pagination configuration options.</param>
    /// <returns>The equivalent <see cref="CursorPaginationParameters"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">First or Last is negative or exceeds the maximum allowed page size</exception>
    /// <exception cref="ArgumentException">First and Last are both specified</exception>
    public static CursorPaginationParameters ToParameters(this CursorPaginationParametersMessage? message, IPaginationOptions? options = null)
    {
        if (message == null) return new CursorPaginationParameters();

        if (message.First < 0 || message.Last < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(message), "Cursor connection parameters 'First' and 'Last' cannot be negative.");
        }

        if (message.First > 0 && message.Last > 0)
        {
            throw new ArgumentException("Cursor connection parameters 'First' and 'Last' are mutually exclusive.");
        }

        int maxPageSize = options?.MaxPageSize ?? DefaultGrpcMaxPageSize;
        if (message.First > maxPageSize || message.Last > maxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(message), $"Cursor connection parameters 'First' and 'Last' cannot exceed {maxPageSize}.");
        }

        return new CursorPaginationParameters
        {
            First = message.First > 0 ? message.First : null,
            After = string.IsNullOrEmpty(message.After) ? null : message.After,
            Last = message.Last > 0 ? message.Last : null,
            Before = string.IsNullOrEmpty(message.Before) ? null : message.Before
        };
    }

    /// <summary>
    /// Converts a <see cref="FilterParametersMessage"/> to <see cref="FilterParameters"/>.
    /// </summary>
    /// <param name="message">The gRPC message to convert.</param>
    /// <returns>The equivalent <see cref="FilterParameters"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="message"/> contains a filter value exceeding the maximum allowed length</exception>
    public static FilterParameters ToParameters(this FilterParametersMessage? message)
    {
        if (message == null) return FilterParameters.Empty;
        if (string.IsNullOrWhiteSpace(message.Value))
            return FilterParameters.Empty;

        // Enforce the same DoS-protection length limit as the HTTP/Minimal-API path.
        // Without this check, a malicious gRPC client can bypass the FilterParameters.AbsoluteMaxLength
        // guard (enforced in TryParse) and send megabyte-sized filter strings that exhaust CPU during parsing.
        if (message.Value.Length > FilterParameters.AbsoluteMaxLength)
        {
            throw new ArgumentException(
                $"The filter string exceeds the maximum allowed length of {FilterParameters.AbsoluteMaxLength} characters.",
                nameof(message));
        }

        return FilterParameters.From(message.Value);
    }

    /// <summary>
    /// Converts a <see cref="SortParametersMessage"/> to <see cref="SortParameters"/>.
    /// </summary>
    /// <param name="message">The gRPC message to convert.</param>
    /// <returns>The equivalent <see cref="SortParameters"/>.</returns>
    public static SortParameters ToParameters(this SortParametersMessage? message)
    {
        if (message == null) return SortParameters.Empty;
        if (string.IsNullOrWhiteSpace(message.Value))
            return SortParameters.Empty;

        return SortParameters.From(message.Value);
    }

    /// <summary>
    /// Converts an <see cref="IPagedList"/> to a <see cref="PagedListMetadataMessage"/>.
    /// </summary>
    /// <param name="pagedList">The paged list metadata source.</param>
    /// <returns>The gRPC metadata message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> is <see langword="null"/></exception>
    public static PagedListMetadataMessage ToMessage(this IPagedList pagedList)
    {
        if (pagedList == null) throw new ArgumentNullException(nameof(pagedList));

        var message = new PagedListMetadataMessage
        {
            Page = pagedList.Page,
            PageSize = pagedList.PageSize,
            HasPreviousPage = pagedList.HasPreviousPage,
            HasNextPage = pagedList.HasNextPage
        };
        
        if (pagedList.TotalCount.HasValue) message.TotalCount = pagedList.TotalCount.Value;
        if (pagedList.TotalPages.HasValue) message.TotalPages = pagedList.TotalPages.Value;

        return message;
    }

    /// <summary>
    /// Converts a strongly typed <see cref="IPagedList{T}"/> to a <see cref="PagedListMetadataMessage"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="pagedList">The paged list metadata source.</param>
    /// <returns>The gRPC metadata message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> is <see langword="null"/></exception>
    public static PagedListMetadataMessage ToMessage<T>(this IPagedList<T> pagedList)
    {
        return ToMessage((IPagedList)pagedList);
    }

    /// <summary>
    /// Converts an <see cref="ICursorPagedList"/> to a <see cref="CursorPagedListMetadataMessage"/>.
    /// </summary>
    /// <param name="pagedList">The cursor paged list metadata source.</param>
    /// <returns>The gRPC metadata message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> is <see langword="null"/></exception>
    public static CursorPagedListMetadataMessage ToMessage(this ICursorPagedList pagedList)
    {
        if (pagedList == null) throw new ArgumentNullException(nameof(pagedList));

        return new CursorPagedListMetadataMessage
        {
            StartCursor = pagedList.StartCursor ?? string.Empty,
            EndCursor = pagedList.EndCursor ?? string.Empty,
            HasPreviousPage = pagedList.HasPreviousPage,
            HasNextPage = pagedList.HasNextPage
        };
    }

    /// <summary>
    /// Converts a strongly typed <see cref="ICursorPagedList{T}"/> to a <see cref="CursorPagedListMetadataMessage"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="pagedList">The cursor paged list metadata source.</param>
    /// <returns>The gRPC metadata message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/> is <see langword="null"/></exception>
    public static CursorPagedListMetadataMessage ToMessage<T>(this ICursorPagedList<T> pagedList)
    {
        return ToMessage((ICursorPagedList)pagedList);
    }



    /// <summary>
    /// Maps pagination metadata onto a strongly typed gRPC response message.
    /// </summary>
    /// <typeparam name="TSource">The domain entity type.</typeparam>
    /// <typeparam name="TResponse">The protobuf response message type.</typeparam>
    /// <param name="pagedList">The paginated list.</param>
    /// <param name="response">The response message to populate.</param>
    /// <param name="configureMetadata">An action to assign the metadata to the response message.</param>
    /// <returns>The mutated response message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/>, <paramref name="response"/>, or <paramref name="configureMetadata"/> is <see langword="null"/></exception>
    public static TResponse ToMessage<TSource, TResponse>(
        this IPagedList<TSource> pagedList,
        TResponse response,
        Action<TResponse, PagedListMetadataMessage> configureMetadata)
        where TResponse : class, Google.Protobuf.IMessage
    {
        // Stryker disable once all : Defensive fail-fast null check, inner ToMessage extension also throws ArgumentNullException
        if (pagedList == null) throw new ArgumentNullException(nameof(pagedList));
        if (response == null) throw new ArgumentNullException(nameof(response));
        if (configureMetadata == null) throw new ArgumentNullException(nameof(configureMetadata));

        configureMetadata(response, (pagedList as IPagedList).ToMessage());
        return response;
    }

    /// <summary>
    /// Maps cursor pagination metadata onto a strongly typed gRPC response message.
    /// </summary>
    /// <typeparam name="TSource">The domain entity type.</typeparam>
    /// <typeparam name="TResponse">The protobuf response message type.</typeparam>
    /// <param name="pagedList">The cursor paginated list.</param>
    /// <param name="response">The response message to populate.</param>
    /// <param name="configureMetadata">An action to assign the metadata to the response message.</param>
    /// <returns>The mutated response message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pagedList"/>, <paramref name="response"/>, or <paramref name="configureMetadata"/> is <see langword="null"/></exception>
    public static TResponse ToMessage<TSource, TResponse>(
        this ICursorPagedList<TSource> pagedList,
        TResponse response,
        Action<TResponse, CursorPagedListMetadataMessage> configureMetadata)
        where TResponse : class, Google.Protobuf.IMessage
    {
        // Stryker disable once all : Defensive fail-fast null check, inner ToMessage extension also throws ArgumentNullException
        if (pagedList == null) throw new ArgumentNullException(nameof(pagedList));
        if (response == null) throw new ArgumentNullException(nameof(response));
        if (configureMetadata == null) throw new ArgumentNullException(nameof(configureMetadata));

        configureMetadata(response, (pagedList as ICursorPagedList).ToMessage());
        return response;
    }
}



