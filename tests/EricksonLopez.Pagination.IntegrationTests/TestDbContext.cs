// Copyright © Erickson Lopez. MIT License.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.IntegrationTests;

public class User
{
    [Key]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Age { get; set; }
    
    public bool IsActive { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}

