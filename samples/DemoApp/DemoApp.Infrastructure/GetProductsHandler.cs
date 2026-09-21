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

namespace DemoApp.Application;

/// <summary>
/// Handles retrieving paginated products for the demo application.
/// </summary>
public class GetProductsHandler : IRequestHandler<GetProductsQuery, IPagedList<Product>>
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProductsHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public GetProductsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
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




