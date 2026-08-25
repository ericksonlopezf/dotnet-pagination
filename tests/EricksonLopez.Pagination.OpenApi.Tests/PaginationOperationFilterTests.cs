// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace EricksonLopez.Pagination.OpenApi.Tests;

public class PaginationOperationFilterTests
{
    private static class DummyController
    {
        public static void NoPagination() { }
        public static void WithPagination(PaginationParameters parameters) { }
        public static void WithCursorPagination(CursorPaginationParameters parameters) { }
        public static void WithFilter(FilterParameters parameters) { }
        public static void WithSortByAndPagination(string sortBy, PaginationParameters parameters) { }
        public static void WithSortAndPagination(string sort, PaginationParameters parameters) { }
        public static void WithAll(PaginationParameters pagination, CursorPaginationParameters cursor, FilterParameters filter, string sortBy) { }
        public static void WithPrimitiveTypes(int page, int first, string filter, string sortBy) { }
        public static void WithSortParameters(SortParameters mySortParam) { }
    }

    private static OperationFilterContext CreateContext(string methodName)
    {
        var methodInfo = typeof(DummyController).GetMethod(methodName);
        var apiDescription = new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription();
        if (methodInfo != null)
        {
            foreach (var p in methodInfo.GetParameters())
            {
                apiDescription.ParameterDescriptions.Add(new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription
                {
                    Name = p.Name!,
                    Type = p.ParameterType
                });
            }
        }
        return new OperationFilterContext(apiDescription, null, null, methodInfo);
    }

    [Fact]
    public void Apply_WithoutPaginationOrFilter_DoesNotModifyOperation()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext(nameof(DummyController.NoPagination));

        filter.Apply(operation, context);

        operation.Parameters?.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithPagination_AnnotatesExistingParameters()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "page" },
                new OpenApiParameter { Name = "pageSize" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithPagination));

        filter.Apply(operation, context);

        operation.Parameters.First(p => p.Name == "page").Description.Should().Contain("1-indexed page number");
        operation.Parameters.First(p => p.Name == "pageSize").Description.Should().Contain("items per page");
    }

    [Fact]
    public void Apply_WithCursorPagination_AnnotatesExistingParameters()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "first" },
                new OpenApiParameter { Name = "last" },
                new OpenApiParameter { Name = "after" },
                new OpenApiParameter { Name = "before" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithCursorPagination));

        filter.Apply(operation, context);

        operation.Parameters.First(p => p.Name == "first").Description.Should().Contain("Forward cursor pagination");
        operation.Parameters.First(p => p.Name == "last").Description.Should().Contain("Backward cursor pagination");
        operation.Parameters.First(p => p.Name == "after").Description.Should().Contain("opaque cursor returned from the previous page");
        operation.Parameters.First(p => p.Name == "before").Description.Should().Contain("opaque cursor returned from the next page");
    }

    [Fact]
    public void Apply_WithFilter_InjectsFilterParameterIfNotExists()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation(); // null parameters
        var context = CreateContext(nameof(DummyController.WithFilter));

        filter.Apply(operation, context);

        operation.Parameters.Should().NotBeNull();
        var filterParam = operation.Parameters.FirstOrDefault(p => p.Name == "filter");
        filterParam.Should().NotBeNull();
        filterParam!.Required.Should().BeFalse();
        filterParam.Schema.Should().NotBeNull();
        filterParam.Schema.Type.Should().Be("string");
        filterParam.Description.Should().Contain("comma-separated filter expression").And.Contain("Supported operators:").And.Contain("starts with").And.Contain("Example:");
        filterParam.Example.Should().BeOfType<Microsoft.OpenApi.Any.OpenApiString>().Which.Value.Should().Be("name~=John,age>=18,isActive=true");
    }

    [Fact]
    public void Apply_WithFilter_AnnotatesExistingFilterParameter()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "filter" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithFilter));

        filter.Apply(operation, context);

        var filterParam = operation.Parameters.First(p => p.Name == "filter");
        filterParam.Description.Should().Contain("comma-separated filter expression").And.Contain("Example");
    }

    [Fact]
    public void Apply_WithoutPaginationOrFilter_DoesNothing()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext(nameof(DummyController.NoPagination));

        filter.Apply(operation, context);

        operation.Parameters.Should().BeEmpty(); // Should return early and not inject any parameters
    }

    [Fact]
    public void Apply_WithUnrelatedParameters_IgnoresThem()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "otherParam" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithPagination));

        filter.Apply(operation, context);

        var other = operation.Parameters.First(p => p.Name == "otherParam");
        other.Description.Should().BeNull(); // Unchanged
    }

    [Theory]
    [InlineData(nameof(DummyController.WithSortByAndPagination))]
    [InlineData(nameof(DummyController.WithSortAndPagination))]
    public void Apply_WithSortBy_InjectsSortByParameterIfNotExists(string methodName)
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext(methodName);
        
        filter.Apply(operation, context);

        var sortByParam = operation.Parameters.FirstOrDefault(p => p.Name == "sortBy");
        sortByParam.Should().NotBeNull();
        sortByParam!.Required.Should().BeFalse();
        sortByParam.Schema.Should().NotBeNull();
        sortByParam.Schema.Type.Should().Be("string");
        sortByParam.Description.Should().Contain("Comma-separated sort expression").And.Contain("Each clause is a property name").And.Contain("Example:");
        sortByParam.Example.Should().BeOfType<Microsoft.OpenApi.Any.OpenApiString>().Which.Value.Should().Be("name asc,createdAt desc");
    }

    [Fact]
    public void Apply_WithSortBy_DoesNotInjectIfAlreadyExists()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "sortBy", Description = "Custom Description" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithSortByAndPagination));
        
        filter.Apply(operation, context);

        var sortByParam = operation.Parameters.First(p => p.Name == "sortBy");
        sortByParam.Description.Should().Be("Custom Description"); // Unchanged
        operation.Parameters.Should().HaveCount(1);
    }

    [Fact]
    public void OpenApiParameterDetector_DetectParameters_Null_ReturnsAllFalse()
    {
        var (hasPagination, hasCursorPagination, hasFilter, hasSort) = OpenApiParameterDetector.DetectParameters(null);
        
        hasPagination.Should().BeFalse();
        hasCursorPagination.Should().BeFalse();
        hasFilter.Should().BeFalse();
        hasSort.Should().BeFalse();
    }

    [Fact]
    public void Apply_WithExistingDescriptions_DoesNotOverwriteThem()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "page", Description = "Epage" },
                new OpenApiParameter { Name = "pageSize", Description = "EpageSize" },
                new OpenApiParameter { Name = "first", Description = "Efirst" },
                new OpenApiParameter { Name = "last", Description = "Elast" },
                new OpenApiParameter { Name = "after", Description = "Eafter" },
                new OpenApiParameter { Name = "before", Description = "Ebefore" },
                new OpenApiParameter { Name = "filter", Description = "Efilter", Example = new Microsoft.OpenApi.Any.OpenApiString("ex") },
                new OpenApiParameter { Name = "sortBy", Description = "Esort", Example = new Microsoft.OpenApi.Any.OpenApiString("ex") },
                new OpenApiParameter { Name = "sort", Description = "Esort2" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithAll));
        
        filter.Apply(operation, context);

        operation.Parameters.First(p => p.Name == "page").Description.Should().Be("Epage");
        operation.Parameters.First(p => p.Name == "pageSize").Description.Should().Be("EpageSize");
        operation.Parameters.First(p => p.Name == "first").Description.Should().Be("Efirst");
        operation.Parameters.First(p => p.Name == "last").Description.Should().Be("Elast");
        operation.Parameters.First(p => p.Name == "after").Description.Should().Be("Eafter");
        operation.Parameters.First(p => p.Name == "before").Description.Should().Be("Ebefore");
        operation.Parameters.First(p => p.Name == "filter").Description.Should().Be("Efilter");
        operation.Parameters.First(p => p.Name == "filter").Example.Should().BeOfType<Microsoft.OpenApi.Any.OpenApiString>().Which.Value.Should().Be("ex");
        operation.Parameters.First(p => p.Name == "sortBy").Description.Should().Be("Esort");
        operation.Parameters.First(p => p.Name == "sortBy").Example.Should().BeOfType<Microsoft.OpenApi.Any.OpenApiString>().Which.Value.Should().Be("ex");
        operation.Parameters.First(p => p.Name == "sort").Description.Should().Be("Esort2");
    }

    [Fact]
    public void Apply_WithFilter_DoesNotInjectIfAlreadyExists()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "filter", Description = "Custom" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithFilter));
        
        filter.Apply(operation, context);

        operation.Parameters.Count(p => p.Name == "filter").Should().Be(1);
    }

    [Theory]
    [InlineData(nameof(DummyController.WithSortByAndPagination), "sortBy")]
    [InlineData(nameof(DummyController.WithSortAndPagination), "sort")]
    public void Apply_WithSort_AnnotatesExistingSortParameter(string methodName, string paramName)
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = paramName }
            }
        };
        var context = CreateContext(methodName);
        
        filter.Apply(operation, context);

        var param = operation.Parameters.First(p => p.Name == paramName);
        param.Description.Should().Contain("Comma-separated sort expression").And.Contain("Example");
        param.Example.Should().NotBeNull();
    }

    [Fact]
    public void Apply_WithPrimitiveTypes_DetectsParameters()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "page" },
                new OpenApiParameter { Name = "first" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithPrimitiveTypes));
        
        filter.Apply(operation, context);

        operation.Parameters.First(p => p.Name == "page").Description.Should().Contain("1-indexed page number");
        operation.Parameters.First(p => p.Name == "first").Description.Should().Contain("Forward cursor pagination");
        operation.Parameters.Count(p => p.Name == "filter").Should().Be(1);
        operation.Parameters.Count(p => p.Name == "sortBy").Should().Be(1);
    }

    [Fact]
    public void Apply_WithSortParameters_DetectsParameters()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation { Parameters = new List<OpenApiParameter>() };
        var context = CreateContext(nameof(DummyController.WithSortParameters));
        
        filter.Apply(operation, context);

        operation.Parameters.Count(p => p.Name == "sortBy").Should().Be(1);
    }

    [Fact]
    public void Apply_WithAll_AnnotatesExistingParametersWithoutDescriptions()
    {
        var filter = new PaginationOperationFilter();
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter { Name = "page" },
                new OpenApiParameter { Name = "pageSize" },
                new OpenApiParameter { Name = "first" },
                new OpenApiParameter { Name = "last" },
                new OpenApiParameter { Name = "after" },
                new OpenApiParameter { Name = "before" },
                new OpenApiParameter { Name = "filter" },
                new OpenApiParameter { Name = "sortBy" },
                new OpenApiParameter { Name = "sort" },
                new OpenApiParameter { Name = "unrelatedParam" }
            }
        };
        var context = CreateContext(nameof(DummyController.WithAll));
        
        filter.Apply(operation, context);

        operation.Parameters.First(p => p.Name == "page").Description.Should().Contain("1-indexed page number");
        operation.Parameters.First(p => p.Name == "pageSize").Description.Should().Contain("number of items per page");
        operation.Parameters.First(p => p.Name == "first").Description.Should().Contain("Forward cursor pagination");
        operation.Parameters.First(p => p.Name == "last").Description.Should().Contain("Backward cursor pagination");
        operation.Parameters.First(p => p.Name == "after").Description.Should().Contain("opaque cursor returned from the previous page");
        operation.Parameters.First(p => p.Name == "before").Description.Should().Contain("opaque cursor returned from the next page");
        operation.Parameters.First(p => p.Name == "unrelatedParam").Description.Should().BeNull();
        
        var filterParam = operation.Parameters.First(p => p.Name == "filter");
        filterParam.Description.Should().Contain("comma-separated filter expression").And.Contain("Supported operators:").And.Contain("starts with").And.Contain("Example:");
        filterParam.Example.Should().BeOfType<Microsoft.OpenApi.Any.OpenApiString>().Which.Value.Should().Be("name~=John,age>=18,isActive=true");

        var sortByParam = operation.Parameters.First(p => p.Name == "sortBy");
        sortByParam.Description.Should().Contain("Comma-separated sort expression").And.Contain("Each clause is a property name").And.Contain("Example:");
        sortByParam.Example.Should().BeOfType<Microsoft.OpenApi.Any.OpenApiString>().Which.Value.Should().Be("name asc,createdAt desc");

        var sortParam = operation.Parameters.First(p => p.Name == "sort");
        sortParam.Description.Should().Contain("Comma-separated sort expression").And.Contain("Each clause is a property name").And.Contain("Example:");
        sortParam.Example.Should().BeOfType<Microsoft.OpenApi.Any.OpenApiString>().Which.Value.Should().Be("name asc,createdAt desc");
    }
}




