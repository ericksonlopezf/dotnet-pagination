// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.AspNetCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationCoreOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        var validator = new PaginationCoreOptionsValidator();
        var options = new PaginationCoreOptions
        {
            DefaultPageSize = 10,
            MaxPageSize = 100
        };

        var result = validator.Validate(Options.DefaultName, options);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_WithInvalidDefaultPageSize_ReturnsFail()
    {
        var validator = new PaginationCoreOptionsValidator();
        var options = new PaginationCoreOptions
        {
            DefaultPageSize = 0,
            MaxPageSize = 100
        };

        var result = validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultPageSize");
    }

    [Fact]
    public void Validate_WithInvalidMaxPageSize_ReturnsFail()
    {
        var validator = new PaginationCoreOptionsValidator();
        var options = new PaginationCoreOptions
        {
            DefaultPageSize = 10,
            MaxPageSize = 0
        };

        var result = validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxPageSize");
    }
}



