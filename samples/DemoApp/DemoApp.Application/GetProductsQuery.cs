// Copyright © Erickson Lopez. MIT License.
using DemoApp.Domain;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using MediatR;

namespace DemoApp.Application;

/// <summary>
/// Query to retrieve a paginated list of products.
/// </summary>
/// <param name="Pagination">The offset pagination parameters.</param>
/// <param name="SearchTerm">Optional search term to filter products by name.</param>
public record GetProductsQuery(PaginationParameters Pagination, string? SearchTerm) : IRequest<IPagedList<Product>>;

