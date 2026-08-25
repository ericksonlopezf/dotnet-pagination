// Copyright © Erickson Lopez. MIT License.
#pragma warning disable CA1848, CA2254
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.AspNetCore.Tests;

public class PaginationParametersModelBinderTests
{
    private readonly IOptionsSnapshot<PaginationCoreOptions> _optionsSnapshot;
    private readonly ILogger<PaginationParametersModelBinder> _logger;

    public PaginationParametersModelBinderTests()
    {
        _optionsSnapshot = Substitute.For<IOptionsSnapshot<PaginationCoreOptions>>();
        _optionsSnapshot.Value.Returns(new PaginationCoreOptions
        {
            DefaultPageSize = 10,
            MaxPageSize = 100,
            DeepOffsetWarningThreshold = 1000
        });

        _logger = Substitute.For<ILogger<PaginationParametersModelBinder>>();
    }

    [Fact]
    public async Task BindModelAsync_BindsDefaultValues_WhenNoProvidersSupplied()
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider(null, null),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<PaginationParameters>();
        model.Page.Should().Be(1);
        model.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task BindModelAsync_BindsProvidedValues()
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("2", "50"),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<PaginationParameters>();
        model.Page.Should().Be(2);
        model.PageSize.Should().Be(50);
    }

    [Theory]
    [InlineData("0", "10", "page")]
    [InlineData("-1", "10", "page")]
    [InlineData("1", "0", "pageSize")]
    [InlineData("1", "-5", "pageSize")]
    public async Task BindModelAsync_Fails_WhenValuesAreInvalid(string pageStr, string pageSizeStr, string expectedErrorKey)
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider(pageStr, pageSizeStr),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.ErrorCount.Should().BeGreaterThan(0);
        context.ModelState.ContainsKey(expectedErrorKey).Should().BeTrue();
        context.ModelState[expectedErrorKey]!.Errors.Should().Contain(e => !string.IsNullOrEmpty(e.ErrorMessage));
    }
    
    [Fact]
    public async Task BindModelAsync_LogsWarning_WhenOffsetExceedsThreshold()
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("102", "10"), // Offset = (102-1) * 10 = 1010 > 1000
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);
        
        context.Result.IsModelSet.Should().BeTrue();
        
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("exceeds the configured warning threshold")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task BindModelAsync_BindsValues_AtBoundary()
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("1", "100"), // page=1, pageSize=MaxPageSize
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<PaginationParameters>();
        model.Page.Should().Be(1);
        model.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task BindModelAsync_SilentlyCapsPageSize_WhenValuesExceedMaxPageSize()
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("1", "101"),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<PaginationParameters>();
        model.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task BindModelAsync_DoesNotLogWarning_WhenOffsetIsExactThreshold()
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("101", "10"), // Offset = (101-1) * 10 = 1000 == 1000
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);
        
        context.Result.IsModelSet.Should().BeTrue();
        _logger.DidNotReceiveWithAnyArgs().Log(default, default, default, default, default!);
    }

    [Fact]
    public async Task BindModelAsync_DoesNotLogWarning_WhenThresholdIsNegative()
    {
        _optionsSnapshot.Value.Returns(new PaginationCoreOptions
        {
            DefaultPageSize = 10,
            MaxPageSize = 100,
            DeepOffsetWarningThreshold = -1
        });
        
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("1000", "100"), // Massive offset, but threshold is -1
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);
        
        context.Result.IsModelSet.Should().BeTrue();
    }

    [Fact]
    public async Task BindModelAsync_DoesNotLogWarning_WhenThresholdIsZero()
    {
        _optionsSnapshot.Value.Returns(new PaginationCoreOptions
        {
            DefaultPageSize = 10,
            MaxPageSize = 100,
            DeepOffsetWarningThreshold = 0 // 0 means disabled
        });
        
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("1000", "100"), // Massive offset
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);
        
        context.Result.IsModelSet.Should().BeTrue();
        _logger.DidNotReceiveWithAnyArgs().Log(default, default, default, default, default!);
    }

    [Fact]
    public async Task BindModelAsync_BindsValues_AtLowerBoundary()
    {
        var binder = new PaginationParametersModelBinder(_optionsSnapshot, _logger);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new SimpleValueProvider("1", "1"), // Minimum allowed values
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<PaginationParameters>();
        model.Page.Should().Be(1);
        model.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task Cursor_BindModelAsync_BindsDefaultValues_WhenNoProvidersSupplied()
    {
        var binder = new CursorPaginationParametersModelBinder(_optionsSnapshot);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new CursorValueProvider(null, null, null, null),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<CursorPaginationParameters>();
        model.First.Should().BeNull();
        model.Last.Should().BeNull();
        model.After.Should().BeNull();
        model.Before.Should().BeNull();
    }

    [Fact]
    public async Task Cursor_BindModelAsync_BindsProvidedValues()
    {
        var binder = new CursorPaginationParametersModelBinder(_optionsSnapshot);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new CursorValueProvider("20", "30", "enc_after", "enc_before"),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<CursorPaginationParameters>();
        model.First.Should().Be(20);
        model.Last.Should().Be(30);
        model.After.Should().Be("enc_after");
        model.Before.Should().Be("enc_before");
    }

    [Theory]
    [InlineData("0", null, "first")]
    [InlineData("-1", null, "first")]
    [InlineData(null, "0", "last")]
    [InlineData(null, "-5", "last")]
    public async Task Cursor_BindModelAsync_Fails_WhenValuesAreInvalid(string? firstStr, string? lastStr, string expectedErrorKey)
    {
        var binder = new CursorPaginationParametersModelBinder(_optionsSnapshot);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new CursorValueProvider(firstStr, lastStr, null, null),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.ErrorCount.Should().BeGreaterThan(0);
        context.ModelState.ContainsKey(expectedErrorKey).Should().BeTrue();
        context.ModelState[expectedErrorKey]!.Errors.Should().Contain(e => !string.IsNullOrEmpty(e.ErrorMessage));
    }
    
    [Fact]
    public void Provider_ReturnsBinder_ForPaginationParameters()
    {
        var provider = new PaginationParametersModelBinderProvider();
        var context = new DefaultModelBinderProviderContext(typeof(PaginationParameters));
        
        var binder = provider.GetBinder(context);
        binder.Should().BeOfType<BinderTypeModelBinder>();
    }
    
    [Fact]
    public void Provider_ReturnsBinder_ForCursorPaginationParameters()
    {
        var provider = new PaginationParametersModelBinderProvider();
        var context = new DefaultModelBinderProviderContext(typeof(CursorPaginationParameters));
        
        var binder = provider.GetBinder(context);
        binder.Should().BeOfType<BinderTypeModelBinder>();
    }
    
    [Fact]
    public void Provider_ReturnsNull_ForOtherTypes()
    {
        var provider = new PaginationParametersModelBinderProvider();
        var context = new DefaultModelBinderProviderContext(typeof(string));
        
        var binder = provider.GetBinder(context);
        binder.Should().BeNull();
    }
    
    [Fact]
    public void Provider_ReturnsBinder_ForFilterParameters()
    {
        var provider = new PaginationParametersModelBinderProvider();
        var context = new DefaultModelBinderProviderContext(typeof(FilterParameters));
        
        var binder = provider.GetBinder(context);
        binder.Should().BeOfType<BinderTypeModelBinder>();
    }
    
    [Fact]
    public void Provider_ReturnsBinder_ForSortParameters()
    {
        var provider = new PaginationParametersModelBinderProvider();
        var context = new DefaultModelBinderProviderContext(typeof(SortParameters));
        
        var binder = provider.GetBinder(context);
        binder.Should().BeOfType<BinderTypeModelBinder>();
    }
    
    [Fact]
    public void Provider_Throws_WhenContextIsNull()
    {
        var provider = new PaginationParametersModelBinderProvider();
        Action act = () => provider.GetBinder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class SimpleValueProvider : IValueProvider
    {
        private readonly string? _page;
        private readonly string? _pageSize;

        public SimpleValueProvider(string? page, string? pageSize)
        {
            _page = page;
            _pageSize = pageSize;
        }

        public bool ContainsPrefix(string prefix) => true;

        public ValueProviderResult GetValue(string key)
        {
            if (key == "page" && _page != null) return new ValueProviderResult(_page);
            if (key == "pageSize" && _pageSize != null) return new ValueProviderResult(_pageSize);
            return ValueProviderResult.None;
        }
    }
    
    private sealed class CursorValueProvider : IValueProvider
    {
        private readonly string? _first;
        private readonly string? _last;
        private readonly string? _after;
        private readonly string? _before;

        public CursorValueProvider(string? first, string? last, string? after, string? before)
        {
            _first = first;
            _last = last;
            _after = after;
            _before = before;
        }

        public bool ContainsPrefix(string prefix) => true;

        public ValueProviderResult GetValue(string key)
        {
            if (key == "first" && _first != null) return new ValueProviderResult(_first);
            if (key == "last" && _last != null) return new ValueProviderResult(_last);
            if (key == "after" && _after != null) return new ValueProviderResult(_after);
            if (key == "before" && _before != null) return new ValueProviderResult(_before);
            return ValueProviderResult.None;
        }
    }
    
    private sealed class DefaultModelBinderProviderContext : ModelBinderProviderContext
    {
        private readonly ModelMetadata _metadata;
        
        public DefaultModelBinderProviderContext(Type modelType)
        {
            _metadata = Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForType(modelType));
        }
        
        public override BindingInfo BindingInfo => new BindingInfo();
        public override IModelMetadataProvider MetadataProvider => null!;
        public override ModelMetadata Metadata => _metadata;
        public override IServiceProvider Services => null!;
        public override IModelBinder CreateBinder(ModelMetadata metadata) => null!;
    }

    [Fact]
    public async Task Cursor_BindModelAsync_BindsValues_AtLowerBoundary()
    {
        var binder = new CursorPaginationParametersModelBinder(_optionsSnapshot);
        var context = new DefaultModelBindingContext
        {
            ValueProvider = new CursorValueProvider("1", "1", null, null),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<CursorPaginationParameters>();
        model.First.Should().Be(1);
        model.Last.Should().Be(1);
    }

    [Fact]
    public async Task Cursor_BindModelAsync_BindsValues_AtUpperBoundary()
    {
        var binder = new CursorPaginationParametersModelBinder(_optionsSnapshot);
        var context = new DefaultModelBindingContext
        {
            // MaxPageSize is 100
            ValueProvider = new CursorValueProvider("100", "100", null, null),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<CursorPaginationParameters>();
        model.First.Should().Be(100);
        model.Last.Should().Be(100);
    }
    [Fact]
    public async Task Cursor_BindModelAsync_ClipsValues_WhenExceedingMaxPageSize()
    {
        var binder = new CursorPaginationParametersModelBinder(_optionsSnapshot);
        var context = new DefaultModelBindingContext
        {
            // MaxPageSize is 100
            ValueProvider = new CursorValueProvider("150", "200", null, null),
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        var model = context.Result.Model.As<CursorPaginationParameters>();
        model.First.Should().Be(100);
        model.Last.Should().Be(100);
    }
}





