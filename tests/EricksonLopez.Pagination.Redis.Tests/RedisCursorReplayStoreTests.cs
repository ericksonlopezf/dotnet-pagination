// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace EricksonLopez.Pagination.Redis.Tests;

public class RedisCursorReplayStoreTests
{
    private static (IConnectionMultiplexer Connection, IDatabase Database) CreateMocks()
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();

        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        connection.GetDatabase(Arg.Any<int>()).Returns(database);
        connection.GetDatabase().Returns(database);

        return (connection, database);
    }

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        var act = () => new RedisCursorReplayStore(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public void Options_DefaultsAndProperties_WorkAsExpected()
    {
        var options = new RedisCursorReplayStoreOptions();
        options.KeyPrefix.Should().Be("pagination:replay:");
        options.DatabaseIndex.Should().BeNull();

        options.KeyPrefix = "custom:";
        options.DatabaseIndex = 3;

        options.KeyPrefix.Should().Be("custom:");
        options.DatabaseIndex.Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryAcquireNonce_WithNullOrEmptyNonce_ReturnsFalse(string? nonce)
    {
        var (connection, _) = CreateMocks();
        var store = new RedisCursorReplayStore(connection);

        bool result = store.TryAcquireNonce(nonce!, TimeSpan.FromMinutes(5));
        result.Should().BeFalse();
    }

    [Fact]
    public void TryAcquireNonce_WhenUnacquired_ReturnsTrue()
    {
        var (connection, database) = CreateMocks();

        database.StringSet(
            "pagination:replay:nonce123",
            "1",
            TimeSpan.FromMinutes(5),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(true);

        var store = new RedisCursorReplayStore(connection);
        bool result = store.TryAcquireNonce("nonce123", TimeSpan.FromMinutes(5));

        result.Should().BeTrue();
    }

    [Fact]
    public void TryAcquireNonce_WhenAlreadyAcquired_ReturnsFalse()
    {
        var (connection, database) = CreateMocks();

        database.StringSet(
            "pagination:replay:nonce123",
            "1",
            TimeSpan.FromMinutes(5),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(false);

        var store = new RedisCursorReplayStore(connection);
        bool result = store.TryAcquireNonce("nonce123", TimeSpan.FromMinutes(5));

        result.Should().BeFalse();
    }

    [Fact]
    public void TryAcquireNonce_WithCustomDatabaseIndexAndPrefix_UsesConfiguredSettings()
    {
        var (connection, database) = CreateMocks();

        database.StringSet(
            "custom:prefix:nonce999",
            "1",
            TimeSpan.FromSeconds(45),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(true);

        var options = Options.Create(new RedisCursorReplayStoreOptions
        {
            KeyPrefix = "custom:prefix:",
            DatabaseIndex = 4
        });

        var store = new RedisCursorReplayStore(connection, options);
        bool result = store.TryAcquireNonce("nonce999", TimeSpan.FromSeconds(45));

        result.Should().BeTrue();
        connection.Received(1).GetDatabase(4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task TryAcquireNonceAsync_WithNullOrEmptyNonce_ReturnsFalse(string? nonce)
    {
        var (connection, _) = CreateMocks();
        var store = new RedisCursorReplayStore(connection);

        bool result = await store.TryAcquireNonceAsync(nonce!, TimeSpan.FromMinutes(5));
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireNonceAsync_WhenUnacquired_ReturnsTrue()
    {
        var (connection, database) = CreateMocks();

        database.StringSetAsync(
            "pagination:replay:nonce123",
            "1",
            TimeSpan.FromMinutes(5),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(Task.FromResult(true));

        var store = new RedisCursorReplayStore(connection);
        bool result = await store.TryAcquireNonceAsync("nonce123", TimeSpan.FromMinutes(5));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireNonceAsync_WhenAlreadyAcquired_ReturnsFalse()
    {
        var (connection, database) = CreateMocks();

        database.StringSetAsync(
            "pagination:replay:nonce123",
            "1",
            TimeSpan.FromMinutes(5),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(Task.FromResult(false));

        var store = new RedisCursorReplayStore(connection);
        bool result = await store.TryAcquireNonceAsync("nonce123", TimeSpan.FromMinutes(5));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireNonceAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        var (connection, _) = CreateMocks();
        var store = new RedisCursorReplayStore(connection);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.TryAcquireNonceAsync("nonce123", TimeSpan.FromMinutes(5), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TryAcquireNonceAsync_WithCustomPrefixAndDatabaseIndex_UsesSpecifiedSettings()
    {
        var (connection, database) = CreateMocks();

        database.StringSetAsync(
            "custom:prefix:nonce999",
            "1",
            TimeSpan.FromSeconds(30),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(Task.FromResult(true));

        var options = Options.Create(new RedisCursorReplayStoreOptions
        {
            KeyPrefix = "custom:prefix:",
            DatabaseIndex = 2
        });

        var store = new RedisCursorReplayStore(connection, options);
        bool result = await store.TryAcquireNonceAsync("nonce999", TimeSpan.FromSeconds(30));

        result.Should().BeTrue();
        connection.Received(1).GetDatabase(2);
    }

    [Fact]
    public void AddPaginationRedisReplayStore_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? nullServices = null;
        var (connection, _) = CreateMocks();

        var act1 = () => nullServices!.AddPaginationRedisReplayStore(connection);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("services");

        var act2 = () => nullServices!.AddPaginationRedisReplayStore();
        act2.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddPaginationRedisReplayStore_NullConnection_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        IConnectionMultiplexer? nullConnection = null;

        var act = () => services.AddPaginationRedisReplayStore(nullConnection!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public void AddPaginationRedisReplayStore_WithConnectionInstance_RegistersStoreInDI()
    {
        var services = new ServiceCollection();
        var (connection, database) = CreateMocks();

        database.StringSet(
            "test:nonce_di",
            "1",
            TimeSpan.FromMinutes(1),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(true);

        var returned = services.AddPaginationRedisReplayStore(connection, opt => opt.KeyPrefix = "test:");
        returned.Should().BeSameAs(services);

        using var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICursorReplayStore>();

        store.Should().NotBeNull();
        store.Should().BeOfType<RedisCursorReplayStore>();

        var options = provider.GetRequiredService<IOptions<RedisCursorReplayStoreOptions>>();
        options.Value.KeyPrefix.Should().Be("test:");

        store!.TryAcquireNonce("nonce_di", TimeSpan.FromMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void AddPaginationRedisReplayStore_WithConnectionInstanceWithoutOptions_RegistersStoreInDI()
    {
        var services = new ServiceCollection();
        var (connection, _) = CreateMocks();

        services.AddPaginationRedisReplayStore(connection);

        using var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICursorReplayStore>();

        store.Should().NotBeNull();
        store.Should().BeOfType<RedisCursorReplayStore>();
    }

    [Fact]
    public void AddPaginationRedisReplayStore_FromDIConnection_ResolvesCorrectly()
    {
        var services = new ServiceCollection();
        var (connection, database) = CreateMocks();
        services.AddSingleton(connection);

        database.StringSet(
            "test:nonce_di2",
            "1",
            TimeSpan.FromMinutes(1),
            false,
            When.NotExists,
            CommandFlags.None
        ).Returns(true);

        var returned = services.AddPaginationRedisReplayStore(opt => opt.KeyPrefix = "test:");
        returned.Should().BeSameAs(services);

        using var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICursorReplayStore>();

        store.Should().NotBeNull();
        store.Should().BeOfType<RedisCursorReplayStore>();

        var options = provider.GetRequiredService<IOptions<RedisCursorReplayStoreOptions>>();
        options.Value.KeyPrefix.Should().Be("test:");

        store!.TryAcquireNonce("nonce_di2", TimeSpan.FromMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void AddPaginationRedisReplayStore_FromDIConnectionWithoutOptions_ResolvesCorrectly()
    {
        var services = new ServiceCollection();
        var (connection, _) = CreateMocks();
        services.AddSingleton(connection);

        services.AddPaginationRedisReplayStore();

        using var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICursorReplayStore>();

        store.Should().NotBeNull();
        store.Should().BeOfType<RedisCursorReplayStore>();
    }
}
