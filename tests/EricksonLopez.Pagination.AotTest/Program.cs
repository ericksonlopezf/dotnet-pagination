// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

#pragma warning disable S1118, S927, S6966
namespace EricksonLopez.Pagination.AotTest;

[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "EF Core warning, not our library")]
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL2026", Justification = "EF Core warning, not our library")]
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("DataSource=:memory:");
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

class Program
{
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "EF Core warning, not our library")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL2026", Justification = "EF Core warning, not our library")]
    static void Main(string[] args)
    {
        Console.WriteLine("Starting AOT test...");

        // 1. Test Offset Pagination Data Structures
        var offsetParams = new PaginationParameters { Page = 1, PageSize = 10 };
        var users = new[]
        {
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" }
        };
        var pagedList = new PagedList<User>(users, totalCount: users.Length, page: offsetParams.Page, pageSize: offsetParams.PageSize);
        Console.WriteLine($"Offset Count: {pagedList.Count}");

        // 2. Test Keyset / Cursor Pagination Structures
        var cursorPagedList = new CursorPagedList<User>(users, startCursor: "start", endCursor: "end", hasPreviousPage: false, hasNextPage: false);
        Console.WriteLine($"Keyset Count: {cursorPagedList.Count}");

        // 3. Test Encoders in Native AOT
        var hmac = new HmacCursorEncoder("a-very-long-secret-key-for-hmac-32-chars!!");
        var encodedHmac = hmac.Encode("cursor_123");
        var decodedHmac = hmac.Decode(encodedHmac);
        if (decodedHmac != "cursor_123")
        {
            Console.WriteLine("HMAC Failed");
            Environment.Exit(1);
        }

        var b64 = new Base64CursorEncoder();
        var encodedB64 = b64.Encode("cursor_456");
        var decodedB64 = b64.Decode(encodedB64);
        if (decodedB64 != "cursor_456")
        {
            Console.WriteLine("Base64 Failed");
            Environment.Exit(1);
        }

        // 4. Test Sort and Filter Parsing in Native AOT
        var sort = SortParameters.From("Name asc, Id desc");
        if (!sort.HasValue || sort.Value != "Name asc, Id desc")
        {
            Console.WriteLine("SortParameters Failed");
            Environment.Exit(1);
        }

        var filter = FilterParameters.From("Name=Alice,Id>5");
        if (!filter.HasValue || filter.Value != "Name=Alice,Id>5")
        {
            Console.WriteLine("FilterParameters Failed");
            Environment.Exit(1);
        }

        if (pagedList.Count == 2 && cursorPagedList.Count == 2)
        {
            Console.WriteLine("SUCCESS");
        }
        else
        {
            Console.WriteLine("FAILURE");
            Environment.Exit(1);
        }
    }
}



