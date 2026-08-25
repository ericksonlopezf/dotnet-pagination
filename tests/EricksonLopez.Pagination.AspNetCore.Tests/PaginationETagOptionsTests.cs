// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationETagOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PaginationETagOptions();
        options.CustomETagFactory.Should().BeNull();
        options.CustomCursorETagFactory.Should().BeNull();
        
        PaginationETagOptions.Default.Should().NotBeNull();
    }

    [Fact]
    public void Setters_WorkCorrectly()
    {
        Func<object, string> factory = obj => "test";
        var options = new PaginationETagOptions
        {
            CustomETagFactory = factory,
            CustomCursorETagFactory = factory
        };

        options.CustomETagFactory.Should().BeSameAs(factory);
        options.CustomCursorETagFactory.Should().BeSameAs(factory);
    }
}


