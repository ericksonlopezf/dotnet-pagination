// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.Pagination.Dapper.Tests;

public class Entity
{
    public int Id { get; set; }
    public int Id1 { get; set; }
    public int Id2 { get; set; }
    public string Name { get; set; } = string.Empty;
}

public static class DapperTestHelper
{
    public static async Task<SqliteConnection> GetConnectionAsync(int count = 25)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE Entities (
                Id INTEGER PRIMARY KEY,
                Id1 INT,
                Id2 INT,
                Name TEXT NOT NULL
            )");

        for (int i = 1; i <= count; i++)
        {
            await connection.ExecuteAsync(
                "INSERT INTO Entities (Id, Id1, Id2, Name) VALUES (@Id, @Id1, @Id2, @Name)",
                new { Id = i, Id1 = i, Id2 = i * 10, Name = $"Entity {i}" });
        }

        return connection;
    }

    public static SqliteConnection GetConnection(int count = 25)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE Entities (
                Id INTEGER PRIMARY KEY,
                Id1 INT,
                Id2 INT,
                Name TEXT NOT NULL
            )");

        for (int i = 1; i <= count; i++)
        {
            connection.Execute(
                "INSERT INTO Entities (Id, Id1, Id2, Name) VALUES (@Id, @Id1, @Id2, @Name)",
                new { Id = i, Id1 = i, Id2 = i * 10, Name = $"Entity {i}" });
        }

        return connection;
    }
}



