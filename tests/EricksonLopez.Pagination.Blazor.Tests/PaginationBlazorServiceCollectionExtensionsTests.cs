// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.Pagination.Blazor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EricksonLopez.Pagination.Blazor.Tests;

public class PaginationBlazorServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPaginationBlazor_RegistersOptions()
    {
        var services = new ServiceCollection();
        services.AddPaginationBlazor(options => 
        {
            options.ContainerClass = "test-container";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PaginationUIOptions>>().Value;

        options.ContainerClass.Should().Be("test-container");
    }
}
