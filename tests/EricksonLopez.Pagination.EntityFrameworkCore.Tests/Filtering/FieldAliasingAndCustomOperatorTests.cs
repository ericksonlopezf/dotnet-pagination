// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class FieldAliasingAndCustomOperatorTests
{
    private class Product
    {
        [Filterable]
        public int Id { get; set; }

        [Filterable(Name = "product_name")]
        public string Title { get; set; } = string.Empty;

        [Filterable(Name = "cost")]
        public decimal Price { get; set; }

        public string SecretCode { get; set; } = string.Empty;
    }

    private class ProductCustomOperatorProvider : IFilterOperatorProvider<Product>
    {
        public IReadOnlyDictionary<string, FilterOperatorHandler<Product>> Operators =>
            new Dictionary<string, FilterOperatorHandler<Product>>
            {
                ["%="] = (propExpr, rawValue) =>
                {
                    // Custom modulus operator: property % value == 0
                    if (propExpr.Type == typeof(int) && int.TryParse(rawValue, out int val))
                    {
                        var modulo = Expression.Modulo(propExpr, Expression.Constant(val));
                        return Expression.Equal(modulo, Expression.Constant(0));
                    }
                    return null;
                }
            };
    }

    [Fact]
    public void FilterByAlias_MatchesAliasedProperty()
    {
        var data = new[]
        {
            new Product { Id = 1, Title = "Laptop", Price = 1200 },
            new Product { Id = 2, Title = "Mouse", Price = 25 },
            new Product { Id = 3, Title = "Keyboard", Price = 75 }
        }.AsQueryable();

        var filter = FilterParameters.From("product_name=Mouse");
        var result = data.ApplyFilter(filter);

        result.Count().Should().Be(1);
        result.First().Title.Should().Be("Mouse");
    }

    [Fact]
    public void FilterByAlias_CaseInsensitiveMatching()
    {
        var data = new[]
        {
            new Product { Id = 1, Title = "Laptop", Price = 1200 },
            new Product { Id = 2, Title = "Mouse", Price = 25 }
        }.AsQueryable();

        var filter = FilterParameters.From("PRODUCT_NAME=Laptop");
        var result = data.ApplyFilter(filter);

        result.Count().Should().Be(1);
        result.First().Id.Should().Be(1);
    }

    [Fact]
    public void FilterByAlias_NumericComparisons()
    {
        var data = new[]
        {
            new Product { Id = 1, Title = "A", Price = 100 },
            new Product { Id = 2, Title = "B", Price = 500 },
            new Product { Id = 3, Title = "C", Price = 1500 }
        }.AsQueryable();

        var filter = FilterParameters.From("cost>=500");
        var result = data.ApplyFilter(filter);

        result.Count().Should().Be(2);
        result.Select(p => p.Id).Should().Equal(2, 3);
    }

    [Fact]
    public void FilterByNonFilterableProperty_WhenOthersHaveFilterable_ThrowsArgumentException()
    {
        var data = new[]
        {
            new Product { Id = 1, SecretCode = "XYZ" }
        }.AsQueryable();

        var filter = FilterParameters.From("SecretCode=XYZ");
        var act = () => data.ApplyFilter(filter);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApplyFilter_WithCustomOperatorProvider_ExecutesCustomLogic()
    {
        var data = new[]
        {
            new Product { Id = 2, Title = "Even2" },
            new Product { Id = 3, Title = "Odd3" },
            new Product { Id = 4, Title = "Even4" },
            new Product { Id = 5, Title = "Odd5" }
        }.AsQueryable();

        var provider = new ProductCustomOperatorProvider();
        var filter = FilterParameters.From("Id%=2");

        var result = data.ApplyFilter(filter, provider);

        result.Count().Should().Be(2);
        result.Select(p => p.Id).Should().Equal(2, 4);
    }

    [Fact]
    public void ApplyFilter_WithCustomOperatorProvider_NegationSupported()
    {
        var data = new[]
        {
            new Product { Id = 2, Title = "Even2" },
            new Product { Id = 3, Title = "Odd3" },
            new Product { Id = 4, Title = "Even4" },
            new Product { Id = 5, Title = "Odd5" }
        }.AsQueryable();

        var provider = new ProductCustomOperatorProvider();
        var filter = FilterParameters.From("!Id%=2");

        var result = data.ApplyFilter(filter, provider);

        result.Count().Should().Be(2);
        result.Select(p => p.Id).Should().Equal(3, 5);
    }

    [Fact]
    public void ApplyFilter_WithCustomOperatorProvider_NullGuards()
    {
        IQueryable<Product> query = new Product[0].AsQueryable();
        var provider = new ProductCustomOperatorProvider();

        var act1 = () => QueryableExtensions.ApplyFilter<Product>(null!, FilterParameters.Empty, provider);
        var act2 = () => QueryableExtensions.ApplyFilter<Product>(query, FilterParameters.From("Id%=2"), (IFilterOperatorProvider<Product>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyFilter_WithCustomOperatorProvider_OptionsMaxFilterStringLength_ThrowsArgumentException()
    {
        var data = new Product[0].AsQueryable();
        var provider = new ProductCustomOperatorProvider();
        var filter = FilterParameters.From("Id%=12345");
        var options = new PaginationCoreOptions { MaxFilterStringLength = 5 };

        var act = () => QueryableExtensions.ApplyFilter(data, filter, provider, options: options);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*The filter string exceeds the maximum allowed length of 5 characters.*");
    }
}
