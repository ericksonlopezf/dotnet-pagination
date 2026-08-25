// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
#pragma warning disable S6966
#pragma warning disable S3881
#pragma warning disable S3881
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class QueryableExtensionsApproximateCountTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used by EF Core")]
    private class ViewDto
    {
        public int Id { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestEntity> Entities { get; set; } = null!;
        public DbSet<ViewDto> Views { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ViewDto>().HasNoKey().ToView("DummyView");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
            }
        }
    }

    private class FakeNpgsqlDatabaseProvider : Microsoft.EntityFrameworkCore.Storage.IDatabaseProvider
    {
        public string Name => "Npgsql.EntityFrameworkCore.PostgreSQL";
        public bool IsConfigured(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions options) => true;
    }

    private class PostgresFakeDbContext : DbContext
    {
        public PostgresFakeDbContext(DbContextOptions<PostgresFakeDbContext> options) : base(options) { }
        public DbSet<TestEntity> Entities { get; set; } = null!;
        public DbSet<ViewDto> Views { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ViewDto>().HasNoKey().ToView("DummyView");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
            }
            optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Storage.IDatabaseProvider, FakeNpgsqlDatabaseProvider>();
        }
    }

    [Fact]
    public async Task ToPagedListAsync_UseApproximateCount_NotPostgres_ThrowsArgumentException()
    {
        // FIX-14: useApproximateCount on a non-PostgreSQL provider now throws ArgumentException
        // instead of silently falling back to exact count. This prevents callers from being
        // misled into thinking approximate counting is active when it isn't.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>();
        
        using var provider = services.BuildServiceProvider();
        using var ctx = provider.GetRequiredService<TestDbContext>();
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        ctx.Entities.Add(new TestEntity { Id = 1, Name = "A" });
        ctx.SaveChanges();

        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        var act = async () => await ctx.Entities.ToPagedListAsync(parameters, useApproximateCount: true);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*useApproximateCount*");
    }
    
    [Fact]
    public async Task ToPagedListAsync_UseApproximateCount_UnmappedType_FallsBackToStandardCount()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PostgresFakeDbContext>(opts => opts.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared"));
        
        using var provider = services.BuildServiceProvider();
        using var ctx = provider.GetRequiredService<PostgresFakeDbContext>();
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        ctx.Database.ExecuteSqlRaw("CREATE VIEW DummyView AS SELECT 1 AS Id;");
        // Use an entity mapped to a view, which doesn't have a table name
        var unmappedQuery = ctx.Views.AsQueryable();
        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        var result = await unmappedQuery.ToPagedListAsync(parameters, useApproximateCount: true);
        
        result.Count.Should().Be(1);
        result.TotalCount.Should().Be(1);
    }
    
    [Fact]
    public async Task ToPagedListAsync_UseApproximateCount_Postgres_DbException_FallsBackToStandardCount()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PostgresFakeDbContext>(opts => opts.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared"));
        
        using var provider = services.BuildServiceProvider();
        using var ctx = provider.GetRequiredService<PostgresFakeDbContext>();
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        ctx.Entities.Add(new TestEntity { Id = 1, Name = "A" });
        ctx.SaveChanges();

        var query = ctx.Entities.AsQueryable();
        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(10).Build();
        // This will trigger GetApproximateCountAsync, which will execute postgres-specific SQL on SQLite
        // This will throw a SqliteException (which is DbException), catch it, and fallback to exact count
        var result = await query.ToPagedListAsync(parameters, useApproximateCount: true);
        
        result.Count.Should().Be(1);
        result.TotalCount.Should().Be(1);
    }
    [Fact]
    public async Task ToPagedListAsync_PageSizeExceedsMax_LogsWarning()
    {
        var services = new ServiceCollection();
        var loggerProvider = new TestLoggerProvider();
        services.AddLogging(b => b.AddProvider(loggerProvider));
        services.AddDbContext<TestDbContext>(opts => opts.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared"));
        
        using var provider = services.BuildServiceProvider();
        using var ctx = provider.GetRequiredService<TestDbContext>();
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        ctx.Entities.Add(new TestEntity { Id = 1, Name = "A" });
        ctx.SaveChanges();

        var parameters = new PaginationParametersBuilder().WithPage(1).WithPageSize(100).Build();
        // This will be capped to 50 by options, so effective is less than original
        var options = new PaginationCoreOptions { MaxPageSize = 50 };
        await ctx.Entities.ToPagedListAsync(parameters, options: options);
        
        // Assert log contains warning
        loggerProvider.Logs.Should().Contain(msg => msg.Contains("exceeds the maximum allowed"));
    }

    [Fact]
    public async Task ToPagedListAsync_DeepOffset_LogsWarning()
    {
        var services = new ServiceCollection();
        var loggerProvider = new TestLoggerProvider();
        services.AddLogging(b => b.AddProvider(loggerProvider));
        services.AddDbContext<TestDbContext>(opts => opts.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared"));
        
        using var provider = services.BuildServiceProvider();
        using var ctx = provider.GetRequiredService<TestDbContext>();
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();

        var parameters = new PaginationParametersBuilder().WithPage(150).WithPageSize(100).Build();
        var options = new PaginationCoreOptions { DeepOffsetWarningThreshold = 10_000 };
        
        await ctx.Entities.ToPagedListAsync(parameters, options: options);
        
        // Assert log contains warning
        loggerProvider.Logs.Should().Contain(msg => msg.Contains("deep offset"));
    }
}

public sealed class TestLoggerProvider : ILoggerProvider, ILogger
{
    public List<string> Logs { get; } = new();

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
    
    public bool IsEnabled(LogLevel logLevel) => true;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Logs.Add(formatter(state, exception));
    }
    
    public void Dispose()
    {
    }
}







