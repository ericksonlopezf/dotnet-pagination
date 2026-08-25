// Copyright © Erickson Lopez. MIT License.
using DemoApp.Domain;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS1591
namespace DemoApp.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}

