// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class PaginationCoreOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PaginationCoreOptions();

        options.MaxPageSize.Should().Be(1000);
        options.DefaultPageSize.Should().Be(10);
        options.Cursor.Should().NotBeNull();
        options.DeepOffsetWarningThreshold.Should().Be(1000);
        options.MaxFilterStringLength.Should().Be(1000);
        options.MaxFilterValueLength.Should().Be(200);
        options.MaxSortStringLength.Should().Be(500);
        options.MaxFilterComplexity.Should().Be(20);
        options.AcceptLegacyCursors.Should().BeTrue();
    }

    [Fact]
    public void Setters_WorkCorrectly()
    {
        var registry = new InMemoryCursorDecoderRegistry();
        var options = new PaginationCoreOptions
        {
            MaxPageSize = 500,
            DefaultPageSize = 25,
            DeepOffsetWarningThreshold = 500,
            MaxFilterStringLength = 500,
            MaxFilterValueLength = 100,
            MaxSortStringLength = 200,
            MaxFilterComplexity = 10,
            MaxPropertyDepth = 5,
            AcceptLegacyCursors = false,
            CursorDecoderRegistry = registry,
            Cursor = new PaginationCursorOptions()
        };

        options.MaxPageSize.Should().Be(500);
        options.DefaultPageSize.Should().Be(25);
        options.DeepOffsetWarningThreshold.Should().Be(500);
        options.MaxFilterStringLength.Should().Be(500);
        options.MaxFilterValueLength.Should().Be(100);
        options.MaxSortStringLength.Should().Be(200);
        options.MaxFilterComplexity.Should().Be(10);
        options.MaxPropertyDepth.Should().Be(5);
        options.AcceptLegacyCursors.Should().BeFalse();
        options.CursorDecoderRegistry.Should().BeSameAs(registry);
        options.Cursor.Should().NotBeNull();
        options.Cursor.Encoder.Should().BeNull();
        
        // Test encoder setter
        var newEncoder = new Base64CursorEncoder();
        options.Cursor.Encoder = newEncoder;
        options.Cursor.Encoder.Should().BeSameAs(newEncoder);
    }

    [Fact]
    public void Validate_WhenDefaultPageSizeIsGreaterThanMaxPageSize_ReturnsValidationResult()
    {
        var options = new PaginationCoreOptions
        {
            DefaultPageSize = 2000,
            MaxPageSize = 1000
        };

        var results = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToList();
        
        results.Should().ContainSingle();
        results[0].ErrorMessage.Should().Be("DefaultPageSize cannot be greater than MaxPageSize.");
        results[0].MemberNames.Should().BeEquivalentTo("DefaultPageSize", "MaxPageSize");
    }

    [Fact]
    public void Validate_WhenDefaultPageSizeIsEqualToMaxPageSize_ReturnsEmpty()
    {
        var options = new PaginationCoreOptions
        {
            DefaultPageSize = 1000,
            MaxPageSize = 1000
        };

        var results = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToList();
        
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenDefaultPageSizeIsLessThanMaxPageSize_ReturnsEmpty()
    {
        var options = new PaginationCoreOptions
        {
            DefaultPageSize = 500,
            MaxPageSize = 1000
        };

        var results = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToList();
        
        results.Should().BeEmpty();
    }
}


