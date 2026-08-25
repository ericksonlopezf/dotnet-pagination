// Copyright © Erickson Lopez. MIT License.
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.Benchmarks.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Required indices for pagination benchmarks
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Price);
            
            // Composite index for keyset pagination
            entity.HasIndex(e => new { e.CreatedAt, e.Id });
        });
    }
}
