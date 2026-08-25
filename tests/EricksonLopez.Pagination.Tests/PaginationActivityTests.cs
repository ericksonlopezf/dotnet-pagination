// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using AwesomeAssertions;
using EricksonLopez.Pagination.Internal;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class PaginationActivityTests
{
    [Fact]
    public void PaginationActivity_Source_IsConfiguredCorrectly()
    {
        PaginationActivity.Source.Should().NotBeNull();
        PaginationActivity.Source.Name.Should().Be("EricksonLopez.Pagination");

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "EricksonLopez.Pagination",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = PaginationActivity.Source.StartActivity("TestPaginationOperation");
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be("TestPaginationOperation");
    }
}
