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
}



