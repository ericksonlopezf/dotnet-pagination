// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DemoApp.Domain;
using DemoApp.Infrastructure;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using MediatR;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS1591
namespace DemoApp.Application;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, IPagedList<Product>>
{
    private readonly ApplicationDbContext _context;

    public GetProductsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IPagedList<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p => p.Name.Contains(request.SearchTerm));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToPagedListAsync(request.Pagination, cancellationToken: cancellationToken);
    }
}




