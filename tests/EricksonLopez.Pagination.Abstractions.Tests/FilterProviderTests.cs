// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class FilterProviderTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    private class TestItemFilterProvider : IFilterProvider<TestItem>
    {
        public Expression<Func<TestItem, bool>>? Build(FilterParameters filter)
        {
            if (!filter.HasValue) return null;

            Expression<Func<TestItem, bool>>? result = null;
            var parts = filter.Value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                if (part.StartsWith("minAge=", StringComparison.OrdinalIgnoreCase))
                {
                    var age = int.Parse(part["minAge=".Length..]);
                    Expression<Func<TestItem, bool>> clause = x => x.Age >= age;
                    result = result == null ? clause : result.And(clause);
                }
                else if (part.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                {
                    var name = part["name=".Length..];
                    Expression<Func<TestItem, bool>> clause = x => x.Name == name;
                    result = result == null ? clause : result.And(clause);
                }
            }

            return result;
        }
    }

    [Fact]
    public void FilterProvider_EmptyFilter_ReturnsNull()
    {
        var provider = new TestItemFilterProvider();
        var expr = provider.Build(FilterParameters.Empty);

        expr.Should().BeNull();
    }

    [Fact]
    public void FilterProvider_WithFilter_FiltersCorrectly()
    {
        var items = new[]
        {
            new TestItem { Id = 1, Name = "Alice", Age = 25 },
            new TestItem { Id = 2, Name = "Bob", Age = 17 },
            new TestItem { Id = 3, Name = "Alice", Age = 15 }
        }.AsQueryable();

        var provider = new TestItemFilterProvider();
        var filter = FilterParameters.From("name=Alice,minAge=18");
        var expr = provider.Build(filter);

        expr.Should().NotBeNull();
        var filtered = items.Where(expr!).ToList();

        filtered.Should().HaveCount(1);
        filtered[0].Id.Should().Be(1);
    }

    [Fact]
    public void ExpressionExtensions_And_CombinesPredicates()
    {
        Expression<Func<TestItem, bool>> expr1 = x => x.Age >= 18;
        Expression<Func<TestItem, bool>> expr2 = x => x.Name == "Alice";

        var combined = expr1.And(expr2);
        var compiled = combined.Compile();

        compiled(new TestItem { Age = 20, Name = "Alice" }).Should().BeTrue();
        compiled(new TestItem { Age = 15, Name = "Alice" }).Should().BeFalse();
        compiled(new TestItem { Age = 20, Name = "Bob" }).Should().BeFalse();
    }

    [Fact]
    public void ExpressionExtensions_Or_CombinesPredicates()
    {
        Expression<Func<TestItem, bool>> expr1 = x => x.Age >= 18;
        Expression<Func<TestItem, bool>> expr2 = x => x.Name == "Alice";

        var combined = expr1.Or(expr2);
        var compiled = combined.Compile();

        compiled(new TestItem { Age = 20, Name = "Bob" }).Should().BeTrue();
        compiled(new TestItem { Age = 15, Name = "Alice" }).Should().BeTrue();
        compiled(new TestItem { Age = 15, Name = "Bob" }).Should().BeFalse();
    }

    [Fact]
    public void ExpressionExtensions_Not_NegatesPredicate()
    {
        Expression<Func<TestItem, bool>> expr = x => x.IsActive;

        var negated = expr.Not();
        var compiled = negated.Compile();

        compiled(new TestItem { IsActive = true }).Should().BeFalse();
        compiled(new TestItem { IsActive = false }).Should().BeTrue();
    }

    [Fact]
    public void ExpressionExtensions_NullArguments_ThrowArgumentNullException()
    {
        Expression<Func<TestItem, bool>> expr = x => x.IsActive;

        var act1 = () => FilterExpressionExtensions.And<TestItem>(null!, expr);
        var act2 = () => FilterExpressionExtensions.And(expr, null!);
        var act3 = () => FilterExpressionExtensions.Or<TestItem>(null!, expr);
        var act4 = () => FilterExpressionExtensions.Or(expr, null!);
        var act5 = () => FilterExpressionExtensions.Not<TestItem>(null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
        act4.Should().Throw<ArgumentNullException>();
        act5.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExpressionExtensions_WithNestedLambdas_PreservesNestedParameters()
    {
        Expression<Func<TestItem, bool>> expr1 = x => x.Tags.Any(t => t == "urgent");
        Expression<Func<TestItem, bool>> expr2 = y => y.Age > 30;

        var combined = expr1.And(expr2);
        var compiled = combined.Compile();

        var matching = new TestItem { Age = 35, Tags = new[] { "urgent", "review" } };
        var nonMatching1 = new TestItem { Age = 25, Tags = new[] { "urgent" } };
        var nonMatching2 = new TestItem { Age = 40, Tags = new[] { "normal" } };

        compiled(matching).Should().BeTrue();
        compiled(nonMatching1).Should().BeFalse();
        compiled(nonMatching2).Should().BeFalse();
    }
}


