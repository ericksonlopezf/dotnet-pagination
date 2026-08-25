// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class SortParametersModelBinderTests
{
    private readonly IOptionsSnapshot<PaginationCoreOptions> _optionsSnapshot;
    private readonly SortParametersModelBinder _binder;

    public SortParametersModelBinderTests()
    {
        _optionsSnapshot = Substitute.For<IOptionsSnapshot<PaginationCoreOptions>>();
        _optionsSnapshot.Value.Returns(new PaginationCoreOptions
        {
            MaxSortStringLength = 100
        });

        _binder = new SortParametersModelBinder(_optionsSnapshot);
    }

    [Fact]
    public async Task BindModelAsync_NoValue_ReturnsCompletedTaskWithoutSettingResult()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "sort",
            ValueProvider = new SimpleValueProvider(null),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
    }

    [Fact]
    public async Task BindModelAsync_ExceedsMaxLength_FailsBinding()
    {
        var longSort = new string('a', 101);
        var context = new DefaultModelBindingContext
        {
            ModelName = "sort",
            ValueProvider = new SimpleValueProvider(longSort),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.ErrorCount.Should().Be(1);
        context.ModelState["sort"]!.Errors[0].ErrorMessage.Should().Be("The sort expression exceeds the maximum allowed length of 100 characters.");
        context.ModelState["sort"]!.RawValue.Should().Be(longSort);
    }

    [Fact]
    public async Task BindModelAsync_ExactMaxLength_SucceedsBinding()
    {
        // 100 character valid sort: "name asc," (9) + 86 + " desc" (5) = 100
        var exactSort = "name asc," + new string('a', 86) + " desc";
        exactSort.Length.Should().Be(100);
        var context = new DefaultModelBindingContext
        {
            ModelName = "sort",
            ValueProvider = new SimpleValueProvider(exactSort),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        context.ModelState.ErrorCount.Should().Be(0);
        context.ModelState["sort"]!.RawValue.Should().Be(exactSort);
    }

    [Fact]
    public async Task BindModelAsync_ValidValue_SucceedsBinding()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "sort",
            ValueProvider = new SimpleValueProvider("name asc"),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var sort = context.Result.Model.As<SortParameters>();
        sort.Value.Should().Be("name asc");
    }

    [Fact]
    public async Task BindModelAsync_InvalidSyntax_FailsBinding()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "sort",
            ValueProvider = new SimpleValueProvider("name asc bad"), // invalid syntax
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.ErrorCount.Should().Be(1);
        context.ModelState["sort"]!.Errors[0].ErrorMessage.Should().Be("The value 'name asc bad' is not a valid sort expression.");
    }

    [Fact]
    public async Task BindModelAsync_NullValue_FailsBinding()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "sort",
            ValueProvider = new SimpleValueProvider(null, false), 
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
    }

    private sealed class SimpleValueProvider : IValueProvider
    {
        private readonly string? _value;
        private readonly bool _returnNoneForNull;

        public SimpleValueProvider(string? value, bool returnNoneForNull = true)
        {
            _value = value;
            _returnNoneForNull = returnNoneForNull;
        }

        public bool ContainsPrefix(string prefix) => true;

        public ValueProviderResult GetValue(string key)
        {
            if (_value != null || !_returnNoneForNull) return new ValueProviderResult(_value!);
            return ValueProviderResult.None;
        }
    }
}





