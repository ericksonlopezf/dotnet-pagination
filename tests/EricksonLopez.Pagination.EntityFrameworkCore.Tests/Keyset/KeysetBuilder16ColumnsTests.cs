// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
 // Unused private types or members should be removed
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S125 // Sections of code should not be commented out
#pragma warning disable S6966 // Await EnsureCreatedAsync instead

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class KeysetBuilder16ColumnsTests
{
    private class BigEntity
    {
        public int C1 { get; set; }
        public int C2 { get; set; }
        public int C3 { get; set; }
        public int C4 { get; set; }
        public int C5 { get; set; }
        public int C6 { get; set; }
        public int C7 { get; set; }
        public int C8 { get; set; }
        public int C9 { get; set; }
        public int C10 { get; set; }
        public int C11 { get; set; }
        public int C12 { get; set; }
        public int C13 { get; set; }
        public int C14 { get; set; }
        public int C15 { get; set; }
        public int C16 { get; set; }
    }

    private class BigDbContext : DbContext
    {
        public BigDbContext(DbContextOptions<BigDbContext> options) : base(options) { }
        public DbSet<BigEntity> Entities => Set<BigEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BigEntity>().HasKey(x => new { x.C1, x.C2, x.C3, x.C4, x.C5, x.C6, x.C7, x.C8, x.C9, x.C10, x.C11, x.C12, x.C13, x.C14, x.C15, x.C16 });
        }
    }

    [Fact]
    public async Task KeysetBuilder_16Columns_AllHit()
    {
        using var connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BigDbContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new BigDbContext(options);
        await db.Database.EnsureCreatedAsync();

        await db.Entities.AddAsync(new BigEntity { C1 = 1, C2 = 1, C3 = 1, C4 = 1, C5 = 1, C6 = 1, C7 = 1, C8 = 1, C9 = 1, C10 = 1, C11 = 1, C12 = 1, C13 = 1, C14 = 1, C15 = 1, C16 = 1 });
        await db.Entities.AddAsync(new BigEntity { C1 = 2, C2 = 2, C3 = 2, C4 = 2, C5 = 2, C6 = 2, C7 = 2, C8 = 2, C9 = 2, C10 = 2, C11 = 2, C12 = 2, C13 = 2, C14 = 2, C15 = 2, C16 = 2 });
        await db.SaveChangesAsync();

        var data = db.Entities.AsQueryable();
        var builder = data.Keyset(new CursorPaginationParametersBuilder().WithFirst(10).Build())
            .Ascending(x => x.C1)
            .Ascending(x => x.C2)
            .Ascending(x => x.C3)
            .Ascending(x => x.C4)
            .Ascending(x => x.C5)
            .Ascending(x => x.C6)
            .Ascending(x => x.C7)
            .Ascending(x => x.C8)
            .Ascending(x => x.C9)
            .Ascending(x => x.C10)
            .Ascending(x => x.C11)
            .Ascending(x => x.C12)
            .Ascending(x => x.C13)
            .Ascending(x => x.C14)
            .Ascending(x => x.C15)
            .Ascending(x => x.C16);

        // This will invoke the projection and hit all 16 `if (_columns.Count > X)` branches.
        var pagedList = await builder.ToCursorPagedListAsync(x => new { x.C1, x.C16 });

        pagedList.Should().NotBeNull();
        pagedList.Count.Should().Be(2);
        
        // Ensure cursors can be parsed (hitting decode path for 16 columns)
        var nextCursor = pagedList.EndCursor;
        var builderNext = data.Keyset(new CursorPaginationParametersBuilder().WithFirst(10).WithAfter(nextCursor).Build())
            .Ascending(x => x.C1)
            .Ascending(x => x.C2)
            .Ascending(x => x.C3)
            .Ascending(x => x.C4)
            .Ascending(x => x.C5)
            .Ascending(x => x.C6)
            .Ascending(x => x.C7)
            .Ascending(x => x.C8)
            .Ascending(x => x.C9)
            .Ascending(x => x.C10)
            .Ascending(x => x.C11)
            .Ascending(x => x.C12)
            .Ascending(x => x.C13)
            .Ascending(x => x.C14)
            .Ascending(x => x.C15)
            .Ascending(x => x.C16);
            
        var pagedListNext = await builderNext.ToCursorPagedListAsync(x => new { x.C1, x.C16 });
        pagedListNext.Should().BeEmpty();
    }
}



