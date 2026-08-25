// Copyright © Erickson Lopez. MIT License.
using DemoApp.Domain;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using MediatR;

#pragma warning disable CS1591
namespace DemoApp.Application;

public record GetProductsQuery(PaginationParameters Pagination, string? SearchTerm) : IRequest<IPagedList<Product>>;

