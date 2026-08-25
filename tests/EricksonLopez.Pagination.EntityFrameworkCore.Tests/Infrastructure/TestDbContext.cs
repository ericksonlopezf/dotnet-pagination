// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> Entities { get; set; } = null!;
    public DbSet<TestStringEntity> StringEntities { get; set; } = null!;
    public DbSet<TypeEntity> TypeEntities { get; set; } = null!;
    public DbSet<NullableKeyEntity> NullableKeyEntities { get; set; } = null!;

    public TestDbContext() { }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    /// <summary>
    /// Creates an initialized in-memory SQLite database context with schema and optional seeded entities.
    /// </summary>
    public static TestDbContext CreateInMemory(int entityCount = 0)
    {
        var context = new TestDbContext();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        if (entityCount > 0)
        {
            for (int i = 1; i <= entityCount; i++)
            {
                context.Entities.Add(new TestEntityBuilder().WithId(i).WithName($"Entity {i}").WithNullableId(i % 2 == 0 ? i : null).Build());
            }
            context.SaveChanges();
        }
        return context;
    }

    /// <summary>
    /// Asynchronously creates an initialized in-memory SQLite database context with schema and optional seeded entities.
    /// </summary>
    public static async Task<TestDbContext> CreateInMemoryAsync(int entityCount = 0)
    {
        var context = new TestDbContext();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        if (entityCount > 0)
        {
            for (int i = 1; i <= entityCount; i++)
            {
                await context.Entities.AddAsync(new TestEntityBuilder().WithId(i).WithName($"Entity {i}").WithNullableId(i % 2 == 0 ? i : null).Build());
            }
            await context.SaveChangesAsync();
        }
        return context;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>()
            .Property(e => e.CustomStructValue)
            .HasConversion(v => v.Value, v => new TestStruct { Value = v });
    }
}

public enum TestState { One = 1, Two = 2, A = 0, B = 1 }

public class TestEntity
{
    public int Id { get; set; }
    public int? NullableId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NullableString { get; set; }
    public TestState StateValue { get; set; }
    public TestState State { get; set; }
    public Guid GuidValue { get; set; }
    public int AdditionalValue { get; set; }
    public string? Name2 { get; set; }
    public string? Name3 { get; set; }
    public bool BooleanValue { get; set; }
    public string? Name4 { get; set; }
    public string? Name5 { get; set; }
    public TestStruct CustomStructValue { get; set; }
}

public class TestStringEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TypeEntity
{
    public long Id { get; set; }
    public Guid GuidVal { get; set; }
    public DateTimeOffset DateTimeOffsetVal { get; set; }
    public DateTime DateTimeVal { get; set; }
    public string StringVal { get; set; } = string.Empty;
}

public class NullableKeyEntity
{
    public int Id { get; set; }
    public string? ExternalCode { get; set; }
}

public struct TestStruct : IComparable<TestStruct>
{
    public int Value { get; set; }
    public int CompareTo(TestStruct other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
}




