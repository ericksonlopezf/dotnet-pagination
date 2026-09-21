// Copyright © Erickson Lopez. MIT License.
using DemoApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace DemoApp.Infrastructure;

/// <summary>
/// Entity Framework database context for the demo application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    /// <summary>
    /// Gets the set of products.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();
}

