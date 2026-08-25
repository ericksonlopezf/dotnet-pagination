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

public class FilterParametersModelBinderTests
{
    private readonly IOptionsSnapshot<PaginationCoreOptions> _optionsSnapshot;
    private readonly FilterParametersModelBinder _binder;

    public FilterParametersModelBinderTests()
    {
        _optionsSnapshot = Substitute.For<IOptionsSnapshot<PaginationCoreOptions>>();
        _optionsSnapshot.Value.Returns(new PaginationCoreOptions
        {
            MaxFilterStringLength = 100
        });

        _binder = new FilterParametersModelBinder(_optionsSnapshot);
    }

    [Fact]
    public async Task BindModelAsync_NoValue_ReturnsCompletedTaskWithoutSettingResult()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "filter",
            ValueProvider = new SimpleValueProvider(null),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
    }

    [Fact]
    public async Task BindModelAsync_ExceedsMaxLength_FailsBinding()
    {
        var longFilter = new string('a', 101);
        var context = new DefaultModelBindingContext
        {
            ModelName = "filter",
            ValueProvider = new SimpleValueProvider(longFilter),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.ErrorCount.Should().Be(1);
        context.ModelState["filter"]!.Errors[0].ErrorMessage.Should().Be("The filter expression exceeds the maximum allowed length of 100 characters.");
        context.ModelState["filter"]!.RawValue.Should().Be(longFilter);
    }

    [Fact]
    public async Task BindModelAsync_ExactMaxLength_SucceedsBinding()
    {
        // 100 character valid filter
        var exactFilter = "name=" + new string('a', 95);
        exactFilter.Length.Should().Be(100);
        var context = new DefaultModelBindingContext
        {
            ModelName = "filter",
            ValueProvider = new SimpleValueProvider(exactFilter),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        context.ModelState.ErrorCount.Should().Be(0);
        context.ModelState["filter"]!.RawValue.Should().Be(exactFilter);
    }

    [Fact]
    public async Task BindModelAsync_ValidValue_SucceedsBinding()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "filter",
            ValueProvider = new SimpleValueProvider("name=john"),
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var filter = context.Result.Model.As<FilterParameters>();
        filter.Value.Should().Be("name=john");
    }

    [Fact]
    public async Task BindModelAsync_InvalidSyntax_FailsBinding()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "filter",
            ValueProvider = new SimpleValueProvider("name=john,,bad"), // invalid syntax
            ModelState = new ModelStateDictionary()
        };

        await _binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.ErrorCount.Should().Be(1);
        context.ModelState["filter"]!.Errors[0].ErrorMessage.Should().Be("The value 'name=john,,bad' is not a valid filter expression.");
    }

    [Fact]
    public async Task BindModelAsync_NullValue_FailsBinding()
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = "filter",
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





