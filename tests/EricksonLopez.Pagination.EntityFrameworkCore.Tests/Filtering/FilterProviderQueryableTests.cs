// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class FilterProviderQueryableTests
{
    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private class CustomerFilterProvider : IFilterProvider<Customer>
    {
        public Expression<Func<Customer, bool>>? Build(FilterParameters filter)
        {
            if (!filter.HasValue) return null;
            if (filter.Value == "top")
            {
                return c => c.Score >= 90;
            }
            return null;
        }
    }

    [Fact]
    public void ApplyFilter_WithProvider_NullGuards()
    {
        IQueryable<Customer> query = new Customer[0].AsQueryable();
        var provider = new CustomerFilterProvider();

        var act1 = () => QueryableExtensions.ApplyFilter<Customer>(null!, provider, FilterParameters.Empty);
        var act2 = () => QueryableExtensions.ApplyFilter<Customer>(query, null!, FilterParameters.From("top"));

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyFilter_WithProvider_EmptyFilter_ReturnsOriginalQuery()
    {
        var data = new[]
        {
            new Customer { Id = 1, Score = 50 },
            new Customer { Id = 2, Score = 95 }
        }.AsQueryable();

        var provider = new CustomerFilterProvider();
        var result = data.ApplyFilter(provider, FilterParameters.Empty);

        result.Count().Should().Be(2);
    }

    [Fact]
    public void ApplyFilter_WithProvider_AppliesPredicate()
    {
        var data = new[]
        {
            new Customer { Id = 1, Score = 50 },
            new Customer { Id = 2, Score = 95 }
        }.AsQueryable();

        var provider = new CustomerFilterProvider();
        var result = data.ApplyFilter(provider, FilterParameters.From("top"));

        result.Count().Should().Be(1);
        result.First().Id.Should().Be(2);
    }

    [Fact]
    public void ApplyFilter_WithProvider_ProviderReturnsNullPredicate_ReturnsOriginalQuery()
    {
        var data = new[]
        {
            new Customer { Id = 1, Score = 50 },
            new Customer { Id = 2, Score = 95 }
        }.AsQueryable();

        var provider = new CustomerFilterProvider();
        var result = data.ApplyFilter(provider, FilterParameters.From("other"));

        result.Count().Should().Be(2);
    }
}




