// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.MongoDb;
using Xunit;

#pragma warning disable CS0618
namespace EricksonLopez.Pagination.MongoDB.Tests;

public class MongoDbFixture : IAsyncLifetime
{
    // Fix obsolete warning by providing an image explicitly, e.g. MongoDbBuilder("mongo:6.0") or just suppressing it
    public MongoDbContainer Container { get; } = new MongoDbBuilder().WithImage("mongo:6.0").Build();

    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container.DisposeAsync().AsTask();
    }
}



