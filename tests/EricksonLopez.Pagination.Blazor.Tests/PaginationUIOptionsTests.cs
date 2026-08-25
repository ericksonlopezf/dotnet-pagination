// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Pagination.Blazor.Tests;

public class PaginationUIOptionsTests
{
    [Fact]
    public void Default_Properties_AreEmptyStrings()
    {
        var options = new PaginationUIOptions();
        options.ContainerClass.Should().BeEmpty();
        options.PaginationClass.Should().BeEmpty();
        options.ListClass.Should().BeEmpty();
        options.ItemClass.Should().BeEmpty();
        options.LinkClass.Should().BeEmpty();
        options.ActiveClass.Should().BeEmpty();
        options.DisabledClass.Should().BeEmpty();
    }

    [Fact]
    public void Setters_WorkCorrectly()
    {
        var options = new PaginationUIOptions
        {
            ContainerClass = "c",
            PaginationClass = "p",
            ListClass = "l",
            ItemClass = "i",
            LinkClass = "li",
            ActiveClass = "a",
            DisabledClass = "d"
        };

        options.ContainerClass.Should().Be("c");
        options.PaginationClass.Should().Be("p");
        options.ListClass.Should().Be("l");
        options.ItemClass.Should().Be("i");
        options.LinkClass.Should().Be("li");
        options.ActiveClass.Should().Be("a");
        options.DisabledClass.Should().Be("d");
    }

    [Fact]
    public void Bootstrap_ReturnsExpectedClasses()
    {
        var options = PaginationUIOptions.Bootstrap;
        options.PaginationClass.Should().Be("justify-content-center");
        options.ListClass.Should().Be("pagination");
        options.ItemClass.Should().Be("page-item");
        options.LinkClass.Should().Be("page-link");
        options.ActiveClass.Should().Be("active");
        options.DisabledClass.Should().Be("disabled");
    }

    [Fact]
    public void Tailwind_ReturnsExpectedClasses()
    {
        var options = PaginationUIOptions.Tailwind;
        options.PaginationClass.Should().Be("flex justify-center");
        options.ListClass.Should().Be("flex list-none rounded");
        options.ItemClass.Should().Be("mx-1");
        options.LinkClass.Should().Be("block px-3 py-2 border rounded hover:bg-gray-200");
        options.ActiveClass.Should().Be("bg-blue-500 text-white");
        options.DisabledClass.Should().Be("opacity-50 cursor-not-allowed");
    }

    [Fact]
    public void Bootstrap5_ReturnsEmptyOptions()
    {
        var options = PaginationUIOptions.Bootstrap5;
        options.PaginationClass.Should().BeEmpty();
    }
}
