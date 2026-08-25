// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using DemoApp.Api.Levels;
using DemoApp.Application;
using DemoApp.Infrastructure;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.AspNetCore;
using EricksonLopez.Pagination.EntityFrameworkCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOutputCache();
builder.Services.AddHealthChecks();

// ─── Configure Pagination ───────────────────────────────────────────────────
// AddPagination registers in DI:
//   · PaginationCoreOptions with DataAnnotations startup validation
//   · ICursorEncoder (HmacCursorEncoder with development key — replace in production)
//   · IPagedListFactory / ICursorPagedListFactory
//   · PaginationParametersModelBinderProvider for MVC/Controllers
builder.Services.AddPagination(options =>
{
    options.DefaultPageSize = 20;
    options.MaxPageSize = 200;
    options.DeepOffsetWarningThreshold = 500; // logs a warning when exceeding this page depth
});

// ─── PaginationExceptionHandler (.NET 8+ IExceptionHandler) ──────────────────
// Globally intercepts InvalidPaginationCursorException and ExpiredPaginationCursorException,
// returning standard HTTP 400 ProblemDetails with error diagnostics.
builder.Services.AddExceptionHandler<PaginationExceptionHandler>();
builder.Services.AddProblemDetails();

// ─── MediatR (CQRS — Level 4) ────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetProductsQuery).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ApplicationDbContext).Assembly);
});

// ─── EF Core with InMemory Database ──────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("DemoDatabase"));

// ─── Benchmark service (Level 7 - Dapper) ────────────────────────────────────
builder.Services.AddScoped<PaginationBenchmarkService>();

var app = builder.Build();

// Seed Database with 1,000 products
await SeedDatabaseAsync(app);

app.UseSwagger();
app.UseSwaggerUI();

app.UseOutputCache();
app.UseExceptionHandler();
app.UseHttpsRedirection();

// ─── Map Progressive Showcase Endpoints ─────────────────────────────────────
app.MapLevel0Endpoints();  // Conceptual: what it is, offset vs keyset, comparison, tradeoffs
app.MapLevel1Endpoints();
app.MapLevel2Endpoints();
app.MapLevel3Endpoints();
app.MapLevel4Endpoints();
app.MapLevel5Endpoints();
app.MapLevel6Endpoints();
app.MapLevel7Endpoints();
app.MapLevel8Endpoints();
app.MapLevel9Endpoints();
app.MapLevel10Endpoints();

app.MapHealthChecks("/health");

await app.RunAsync();

// ── Seed helper ─────────────────────────────────────────────────────────────
static async Task SeedDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

    if (!await db.Products.AnyAsync().ConfigureAwait(false))
    {
        var categories = new[] { "Electronics", "Clothing", "Books", "Home", "Sports" };
        var rng = new Random(42); // Fixed seed for reproducible data

        for (int i = 1; i <= 1_000; i++)
        {
            await db.Products.AddAsync(new DemoApp.Domain.Product
            {
                Id = i,
                Name = $"Product {i:D4} — {categories[i % categories.Length]}",
                Price = Math.Round((decimal)(rng.NextDouble() * 500 + 1), 2),
                CreatedAt = DateTime.UtcNow.AddSeconds(-i * 300)
            }).ConfigureAwait(false);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
