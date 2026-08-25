// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using EricksonLopez.Pagination.Benchmarks.Database;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.Benchmarks.Seed;

public static class DatabaseSeeder
{
    public static async Task EnsureSeededAsync(DatabaseEngine engine, int count)
    {
        var options = DbEngineFactory.CreateOptions(engine);
        await using var context = new AppDbContext(options);
        
        Console.WriteLine($"[{engine}] Ensuring database is created...");
        await context.Database.EnsureCreatedAsync();

        var currentCount = await context.Products.CountAsync();
        if (currentCount >= count)
        {
            Console.WriteLine($"[{engine}] Database already contains {currentCount} products. Skipping seeding.");
            return;
        }

        Console.WriteLine($"[{engine}] Database has {currentCount} products, targeting {count}. Seeding...");
        
        var remaining = count - currentCount;
        await SeedBatchAsync(engine, remaining);
    }

    private static async Task SeedBatchAsync(DatabaseEngine engine, int targetCount)
    {
        var faker = new Faker<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Category, f => f.Commerce.Department())
            .RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000))
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(2).ToUniversalTime())
            .RuleFor(p => p.UpdatedAt, f => f.Date.Recent().ToUniversalTime())
            .RuleFor(p => p.Stock, f => f.Random.Int(0, 1000))
            .RuleFor(p => p.IsActive, f => f.Random.Bool())
            .RuleFor(p => p.Sku, f => f.Commerce.Ean13())
            .RuleFor(p => p.Manufacturer, f => f.Company.CompanyName())
            .RuleFor(p => p.Country, f => f.Address.Country())
            .RuleFor(p => p.Weight, f => f.Random.Double(0.1, 100))
            .RuleFor(p => p.Rating, f => f.Random.Double(1, 5))
            .RuleFor(p => p.TotalSales, f => f.Random.Int(0, 10000));

        var batchSize = 10_000; // Optimal for EF Core bulk inserts across providers
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < targetCount; i += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, targetCount - i);
            var products = faker.Generate(currentBatchSize);

            // Re-create context for each batch to avoid ChangeTracker bloat
            var options = DbEngineFactory.CreateOptions(engine);
            await using var context = new AppDbContext(options);
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            Console.WriteLine($"[{engine}] Seeded {i + currentBatchSize} / {targetCount} products... ({stopwatch.ElapsedMilliseconds} ms)");
        }
        
        stopwatch.Stop();
        Console.WriteLine($"[{engine}] Seeding complete in {stopwatch.ElapsedMilliseconds} ms.");
    }
}



