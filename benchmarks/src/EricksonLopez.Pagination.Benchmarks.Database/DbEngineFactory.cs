// Copyright © Erickson Lopez. MIT License.
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Pagination.Benchmarks.Database;

public enum DatabaseEngine
{
    PostgreSQL,
    SQLServer,
    MySQL,
    Oracle,
    SQLite
}

public static class DbEngineFactory
{
    public static string GetConnectionString(DatabaseEngine engine)
    {
        var sqliteDbPath = Path.Combine(Path.GetTempPath(), "pagination_benchmark.db");
        return engine switch
        {
            DatabaseEngine.PostgreSQL => "Host=localhost;Database=pagination_benchmark;Username=benchmark_user;Password=benchmark_password",
            DatabaseEngine.SQLServer => "Server=localhost,14330;Database=pagination_benchmark;User Id=sa;Password=Benchmark_password_123!;TrustServerCertificate=True",
            DatabaseEngine.MySQL => "Server=localhost;Database=pagination_benchmark;Uid=benchmark_user;Pwd=benchmark_password;",
            DatabaseEngine.Oracle => "Data Source=localhost:1521/FREEPDB1;User Id=benchmark_user;Password=benchmark_password;",
            DatabaseEngine.SQLite => $"Data Source={sqliteDbPath}",
            _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
        };
    }

    public static DbContextOptions<AppDbContext> CreateOptions(DatabaseEngine engine)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = GetConnectionString(engine);

        switch (engine)
        {
            case DatabaseEngine.PostgreSQL:
                builder.UseNpgsql(connectionString);
                break;
            case DatabaseEngine.SQLServer:
                builder.UseSqlServer(connectionString);
                break;
            case DatabaseEngine.MySQL:
                builder.UseMySQL(connectionString);
                break;
            case DatabaseEngine.Oracle:
                builder.UseOracle(connectionString);
                break;
            case DatabaseEngine.SQLite:
                builder.UseSqlite(connectionString);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(engine), engine, null);
        }

        // Common settings for benchmarking: disable tracking for performance parity
        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        
        return builder.Options;
    }
}
