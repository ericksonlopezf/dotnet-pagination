// Copyright © Erickson Lopez. MIT License.
using System;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class PaginationParametersExtensionsTests
{
    [Fact]
    public void GetSkip_CalculatesCorrectly()
    {
        var parameters = PaginationParameters.Create(3, 15);
        var result = parameters.GetSkip();

        result.Should().Be(30); // (3 - 1) * 15 = 30
    }

    [Fact]
    public void GetSkip_WhenMultiplicationOverflowsInt32_ClampsToInt32MaxValue()
    {
        // Finding: CRITICAL-01 regression test
        // 1,000,000 * 10,000 = 10,000,000,000 (which overflows int.MaxValue and would wrap to negative)
        var parameters = PaginationParameters.Create(1_000_001, 10_000);
        var result = parameters.GetSkip();

        result.Should().Be(int.MaxValue);
    }
}



