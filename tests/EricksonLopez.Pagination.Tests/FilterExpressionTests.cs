// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Tests;

public class FilterExpressionTests
{
    private sealed class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
    }

    private sealed class TestAddress { public string City { get; set; } = ""; }
    private sealed class TestUserWithAddress { public TestAddress Address { get; set; } = new(); }

    [Fact]
    public void Build_WithAllowedProperties_BypassesFilterableCheckForNestedProperties()
    {
        var filter = FilterParameters.From("Address.City=NY");
        var allowedProps = new[] { "Address.City" };
        var expr = FilterExpression.Build<TestUserWithAddress>(filter, allowedProperties: allowedProps);
        
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new TestUserWithAddress { Address = new TestAddress { City = "NY" } }).Should().BeTrue();
        compiled(new TestUserWithAddress { Address = new TestAddress { City = "LA" } }).Should().BeFalse();
    }

    [Fact]
    public void Build_WithNegatedNullEquality_BuildsExpectedExpression()
    {
        var filter = FilterParameters.From("!Name=null");
        var expr = FilterExpression.Build<TestUser>(filter);
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new TestUser { Name = "Alice" }).Should().BeTrue();
        compiled(new TestUser { Name = null! }).Should().BeFalse();

        var filterNeq = FilterParameters.From("!Name!=null");
        var exprNeq = FilterExpression.Build<TestUser>(filterNeq);
        exprNeq.Should().NotBeNull();
        var compiledNeq = exprNeq!.Compile();
        compiledNeq(new TestUser { Name = "Alice" }).Should().BeFalse();
        compiledNeq(new TestUser { Name = null! }).Should().BeTrue();
    }

    [Fact]
    public void Build_WithEmptyFilter_ReturnsNull()
    {
        var filter = FilterParameters.Empty;
        var expr = FilterExpression.Build<TestUser>(filter);
        expr.Should().BeNull();
    }

    [Fact]
    public void Build_WithInvalidMaxComplexity_ThrowsArgumentOutOfRangeException()
    {
        var filter = FilterParameters.From("Id=1");
        var act = () => FilterExpression.Build<TestUser>(filter, maxComplexity: 0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*maxComplexity must be greater than zero*");
    }

    

    [Fact]
    public void Build_WithTooManyClauses_ThrowsArgumentOutOfRangeException()
    {
        var filterStr = string.Join(",", Enumerable.Range(1, 21).Select(i => $"Id={i}"));
        var filter = FilterParameters.From(filterStr);
        var act = () => FilterExpression.Build<TestUser>(filter, maxComplexity: 20);
        // FIX-05: Changed from InvalidOperationException to ArgumentOutOfRangeException.
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*complexity*");
    }
    
    [Fact]
    public void Build_WithTooManyOrClauses_ThrowsArgumentOutOfRangeException()
    {
        var filterStr = string.Join("|", Enumerable.Range(1, 21).Select(i => $"Id={i}"));
        var filter = FilterParameters.From(filterStr);
        var act = () => FilterExpression.Build<TestUser>(filter, maxComplexity: 20);
        // FIX-05: Changed from InvalidOperationException to ArgumentOutOfRangeException.
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*complexity*");
    }

    [Fact]
    public void Build_ValidFilter_ReturnsExpression()
    {
        var filter = FilterParameters.From("Name~=John,Id>=1,IsActive=true");
        var expr = FilterExpression.Build<TestUser>(filter);
        
        expr.Should().NotBeNull();
        
        var compiled = expr!.Compile();
        
        compiled(new TestUser { Name = "John Doe", Id = 5, IsActive = true }).Should().BeTrue();
        compiled(new TestUser { Name = "Jane Doe", Id = 5, IsActive = true }).Should().BeFalse(); // Name fails
        compiled(new TestUser { Name = "John Doe", Id = 0, IsActive = true }).Should().BeFalse(); // Id fails
        compiled(new TestUser { Name = "John Doe", Id = 5, IsActive = false }).Should().BeFalse(); // IsActive fails
    }
    
    [Fact]
    public void Build_OrClauses_ReturnsExpression()
    {
        var filter = FilterParameters.From("Name=John|Name=Jane");
        var expr = FilterExpression.Build<TestUser>(filter);
        
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        
        compiled(new TestUser { Name = "John", Id = 1 }).Should().BeTrue();
        compiled(new TestUser { Name = "Jane", Id = 2 }).Should().BeTrue();
        compiled(new TestUser { Name = "Bob", Id = 3 }).Should().BeFalse();
    }

    [Fact]
    public void Build_NegatedClauses_ReturnsExpression()
    {
        var filter = FilterParameters.From("!Name=John,!IsActive=false");
        var expr = FilterExpression.Build<TestUser>(filter);
        
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        
        compiled(new TestUser { Name = "Jane", Id = 1, IsActive = true }).Should().BeTrue();
        compiled(new TestUser { Name = "John", Id = 1, IsActive = true }).Should().BeFalse();
        compiled(new TestUser { Name = "Jane", Id = 1, IsActive = false }).Should().BeFalse();
    }
    
    [Fact]
    public void Build_InvalidPropertyWithThrow_ThrowsException()
    {
        var filter = FilterParameters.From("UnknownProp=1");
        var act = () => FilterExpression.Build<TestUser>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Build_InvalidPropertyWithIgnore_ReturnsNull()
    {
        var filter = FilterParameters.From("UnknownProp=1");
        var expr = FilterExpression.Build<TestUser>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        expr.Should().BeNull();
    }

    [Fact]
    public void Build_WithNoOperator_ReturnsNull()
    {
        var filter = FilterParameters.From("JustAFieldName");
        var expr = FilterExpression.Build<TestUser>(filter);
        expr.Should().BeNull();
    }

    [Fact]
    public void Build_WithExactly128CharFieldName_DoesNotThrowArgumentException()
    {
        // 128 characters total, split into segments <= 100 characters to avoid the segment length limit
        var longFieldName = new string('A', 50) + "." + new string('B', 50) + "." + new string('C', 26);
        var filter = FilterParameters.From($"{longFieldName}=1");
        // It might throw ArgumentException because the property is not found, but it should NOT throw the 128-char limit exception.
        var act = () => FilterExpression.Build<TestUser>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage($"Property '{longFieldName}' not found*");
    }

    [Fact]
    public void Build_WithExactly100CharFieldSegment_DoesNotThrowLengthException()
    {
        var longFieldName = new string('A', 100);
        var filter = FilterParameters.From($"{longFieldName}=1");
        // Should not throw the 100-char segment exception, but the property not found exception.
        var act = () => FilterExpression.Build<TestUser>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>().WithMessage($"Property '{longFieldName}' not found*");
    }

    [Fact]
    public void Build_WithDisallowedProperty_ThrowsInvalidOperationException()
    {
        var filter = FilterParameters.From("Name=John");
        var allowedProps = new[] { "Id" };
        
        var act = () => FilterExpression.Build<TestUser>(filter, allowedProperties: allowedProps);
        
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Filtering on property 'Name' is not permitted.");
    }

    [Fact]
    public void Build_WithDisallowedPropertyAndIgnoreBehavior_IgnoresProperty()
    {
        var filter = FilterParameters.From("Name=John,Id=5");
        var allowedProps = new[] { "Id" };
        
        var expr = FilterExpression.Build<TestUser>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore, allowedProperties: allowedProps);
        
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        
        // It should only filter by Id=5 since Name is ignored
        compiled(new TestUser { Name = "Jane", Id = 5 }).Should().BeTrue();
        compiled(new TestUser { Name = "John", Id = 6 }).Should().BeFalse();
    }

    [Fact]
    public void Build_WithAllowedPropertiesContainingNull_IgnoresNull()
    {
        var filter = FilterParameters.From("Id=1");
        var allowedProps = new string[] { "Id", null! }; // Null element in allowed properties
        
        var expr = FilterExpression.Build<TestUser>(filter, allowedProperties: allowedProps);
        
        expr.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithValueLengthExceedingMax_ThrowsArgumentOutOfRangeException()
    {
        var filter = FilterParameters.From("Name=John");
        
        var act = () => FilterExpression.Build<TestUser>(filter, maxFilterValueLength: 3); // "John" is 4 chars
        
        // FIX-05: Changed from InvalidOperationException to ArgumentOutOfRangeException.
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*The filter value length exceeds the maximum allowed length of 3 characters*");
    }

    [Fact]
    public void Build_WithValueLengthExactlyMax_DoesNotThrow()
    {
        var filter = FilterParameters.From("Name=Bob");
        var expr = FilterExpression.Build<TestUser>(filter, maxFilterValueLength: 3); // "Bob" is 3 chars
        
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new TestUser { Name = "Bob" }).Should().BeTrue();
    }

    [Fact]
    public void Build_WithLeadingComma_IgnoresEmptyClause()
    {
        var filter = FilterParameters.From(",Name=John");
        var expr = FilterExpression.Build<TestUser>(filter);
        
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        
        compiled(new TestUser { Name = "John", Id = 1 }).Should().BeTrue();
    }
    
    [Fact]
    public void Build_WithLeadingPipe_IgnoresEmptyOrClause()
    {
        var filter = FilterParameters.From("|Name=John");
        var expr = FilterExpression.Build<TestUser>(filter);
        
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        
        compiled(new TestUser { Name = "John", Id = 1 }).Should().BeTrue();
    }

    [Fact]
    public void Build_WithOnlyWhitespace_ReturnsNull()
    {
        var filter = FilterParameters.From("   ,  |   ");
        var expr = FilterExpression.Build<TestUser>(filter);
        
        expr.Should().BeNull();
    }

    [Fact]
    public void Build_WithExactlyMaxComplexityClauses_ReturnsExpression()
    {
        // 20 clauses, max is 20
        var filterStr = string.Join(",", Enumerable.Range(1, 20).Select(i => $"Id={i}"));
        var filter = FilterParameters.From(filterStr);
        var expr = FilterExpression.Build<TestUser>(filter, maxComplexity: 20);
        
        expr.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithExactlyMaxComplexityOrClauses_ReturnsExpression()
    {
        // 20 OR clauses, max is 20
        var filterStr = string.Join("|", Enumerable.Range(1, 20).Select(i => $"Id={i}"));
        var filter = FilterParameters.From(filterStr);
        var expr = FilterExpression.Build<TestUser>(filter, maxComplexity: 20);
        
        expr.Should().NotBeNull();
    }

    private sealed class TestCustomOperatorProvider : IFilterOperatorProvider<TestUser>
    {
        public IReadOnlyDictionary<string, FilterOperatorHandler<TestUser>> Operators { get; } =
            new Dictionary<string, FilterOperatorHandler<TestUser>>
            {
                ["%="] = (prop, val) => Expression.Equal(prop, Expression.Constant(val)),
                ["*="] = (prop, val) => null
            };
    }

    [Fact]
    public void Build_WithCustomOperatorProvider_AppliesCustomOperator()
    {
        var provider = new TestCustomOperatorProvider();
        var filter = new FilterParameters { Value = "Name%=John" };
        var expr = FilterExpression.Build<TestUser>(filter, customOperatorProvider: provider);

        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new TestUser { Name = "John" }).Should().BeTrue();
        compiled(new TestUser { Name = "Jane" }).Should().BeFalse();
    }

    [Fact]
    public void Build_WithNegatedCustomOperator_NegatesExpression()
    {
        var provider = new TestCustomOperatorProvider();
        var filter = new FilterParameters { Value = "!Name%=John" };
        var expr = FilterExpression.Build<TestUser>(filter, customOperatorProvider: provider);

        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new TestUser { Name = "John" }).Should().BeFalse();
        compiled(new TestUser { Name = "Jane" }).Should().BeTrue();
    }

    [Fact]
    public void Build_WithCustomOperatorReturningNull_ReturnsNullExpression()
    {
        var provider = new TestCustomOperatorProvider();
        var filter = new FilterParameters { Value = "Name*=John" };
        var expr = FilterExpression.Build<TestUser>(filter, customOperatorProvider: provider);

        expr.Should().BeNull();
    }

    [Fact]
    public void Build_WithoutCustomOperatorProvider_CachesExpressionInstance()
    {
        var filter = FilterParameters.From("Name=CachedUser");
        var expr1 = FilterExpression.Build<TestUser>(filter);
        var expr2 = FilterExpression.Build<TestUser>(filter);

        expr1.Should().NotBeNull();
        expr2.Should().NotBeNull();
        ReferenceEquals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Build_WithCustomOperatorProvider_StandardOperator_FallsBackToStandardOperator()
    {
        var provider = new TestCustomOperatorProvider();
        var filter = FilterParameters.From("Name=John");
        var expr = FilterExpression.Build<TestUser>(filter, customOperatorProvider: provider);

        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new TestUser { Name = "John" }).Should().BeTrue();
        compiled(new TestUser { Name = "Jane" }).Should().BeFalse();
    }

    [Fact]
    public void FilterPredicateBuilder_WithUnregisteredCustomOperator_ThrowException_ThrowsArgumentException()
    {
        var provider = new TestCustomOperatorProvider();
        var clause = new EricksonLopez.Pagination.Internal.FilterClause
        {
            FieldName = "Name",
            Op = EricksonLopez.Pagination.Internal.FilterOp.Custom,
            CustomOp = "@unknown",
            Value = "John",
            Negate = false
        };
        var param = Expression.Parameter(typeof(TestUser), "x");
        var act = () => EricksonLopez.Pagination.Internal.FilterPredicateBuilder.Build<TestUser>(param, clause, FilterUnknownFieldBehavior.ThrowException, false, 3, provider);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown or unregistered custom operator '@unknown'.*");
    }

    [Fact]
    public void FilterPredicateBuilder_WithCustomOpAndNullProvider_ThrowsArgumentException()
    {
        var clause = new EricksonLopez.Pagination.Internal.FilterClause
        {
            FieldName = "Name",
            Op = EricksonLopez.Pagination.Internal.FilterOp.Custom,
            CustomOp = "@unknown",
            Value = "John",
            Negate = false
        };
        var param = Expression.Parameter(typeof(TestUser), "x");
        var act = () => EricksonLopez.Pagination.Internal.FilterPredicateBuilder.Build<TestUser>(param, clause, FilterUnknownFieldBehavior.ThrowException, false, 3, null);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown or unregistered custom operator '@unknown'.*");
    }

    [Fact]
    public void FilterPredicateBuilder_WithUnregisteredCustomOperator_Ignore_ReturnsNull()
    {
        var provider = new TestCustomOperatorProvider();
        var clause = new EricksonLopez.Pagination.Internal.FilterClause
        {
            FieldName = "Name",
            Op = EricksonLopez.Pagination.Internal.FilterOp.Custom,
            CustomOp = "@unknown",
            Value = "John",
            Negate = false
        };
        var param = Expression.Parameter(typeof(TestUser), "x");
        var res = EricksonLopez.Pagination.Internal.FilterPredicateBuilder.Build<TestUser>(param, clause, FilterUnknownFieldBehavior.Ignore, false, 3, provider);
        res.Should().BeNull();
    }

    [Fact]
    public void FilterPredicateBuilder_WithInvalidSegmentCharacters_ThrowsArgumentException()
    {
        var clause = new EricksonLopez.Pagination.Internal.FilterClause
        {
            FieldName = "user$name",
            Op = EricksonLopez.Pagination.Internal.FilterOp.Equal,
            Value = "123",
            Negate = false
        };
        var param = Expression.Parameter(typeof(TestUser), "x");
        var act = () => EricksonLopez.Pagination.Internal.FilterPredicateBuilder.Build<TestUser>(param, clause, FilterUnknownFieldBehavior.ThrowException, false, 3);
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid characters in filter segment: 'user$name'*");
    }

    [Fact]
    public void FilterPredicateBuilder_WithEmptySegment_ThrowsArgumentException()
    {
        var clause = new EricksonLopez.Pagination.Internal.FilterClause
        {
            FieldName = "Name..Age",
            Op = EricksonLopez.Pagination.Internal.FilterOp.Equal,
            Value = "123",
            Negate = false
        };
        var param = Expression.Parameter(typeof(TestUser), "x");
        var act = () => EricksonLopez.Pagination.Internal.FilterPredicateBuilder.Build<TestUser>(param, clause, FilterUnknownFieldBehavior.ThrowException, false, 3);
        act.Should().Throw<ArgumentException>().WithMessage("*Filter field contains an empty segment.*");
    }

    [Fact]
    public void FilterPredicateBuilder_WithSegmentExceeding100Chars_ThrowsInvalidOperationException()
    {
        var longName = new string('a', 101);
        var clause = new EricksonLopez.Pagination.Internal.FilterClause
        {
            FieldName = longName,
            Op = EricksonLopez.Pagination.Internal.FilterOp.Equal,
            Value = "123",
            Negate = false
        };
        var param = Expression.Parameter(typeof(TestUser), "x");
        var act = () => EricksonLopez.Pagination.Internal.FilterPredicateBuilder.Build<TestUser>(param, clause, FilterUnknownFieldBehavior.ThrowException, false, 3);
        act.Should().Throw<InvalidOperationException>().WithMessage("*exceeds maximum length of 100 characters.*");
    }

    private sealed class TypeWithoutFilterable
    {
        public NestedItem Nested { get; set; } = new();
    }

    private sealed class NestedItem
    {
        public string Val { get; set; } = "";
    }

    [Fact]
    public void Build_WithNestedPropertyOnTypeWithoutFilterable_ThrowsInvalidOperationException()
    {
        var filter = FilterParameters.From("Nested.Val=123");
        var act = () => FilterExpression.Build<TypeWithoutFilterable>(filter);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Nested property filtering on 'Nested.Val' is not permitted because type 'TypeWithoutFilterable' does not define any [Filterable] properties.*");
    }

    private sealed class TestUserWithAlias
    {
        [Filterable(Name = "alias_name")]
        public string RealName { get; set; } = "";

        public string Secret { get; set; } = "";
    }

    [Fact]
    public void Build_WithFilterableAlias_ResolvesProperty()
    {
        var filter = FilterParameters.From("alias_name=John");
        var expr = FilterExpression.Build<TestUserWithAlias>(filter);
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new TestUserWithAlias { RealName = "John" }).Should().BeTrue();
        compiled(new TestUserWithAlias { RealName = "Jane" }).Should().BeFalse();
    }

    [Fact]
    public void Build_WithUnfilterablePropertyOnTypeWithFilterable_ThrowException_ThrowsArgumentException()
    {
        var filter = FilterParameters.From("Secret=Confidential");
        var act = () => FilterExpression.Build<TestUserWithAlias>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.ThrowException);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Property 'Secret' in path 'Secret' is not allowed to be filtered. It must be marked with [Filterable].*");
    }

    [Fact]
    public void Build_WithUnfilterablePropertyOnTypeWithFilterable_Ignore_ReturnsNull()
    {
        var filter = FilterParameters.From("Secret=Confidential");
        var expr = FilterExpression.Build<TestUserWithAlias>(filter, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        expr.Should().BeNull();
    }

    [Theory]
    [InlineData("!Name~=John", "Jane Doe", true)]
    [InlineData("!Name~=John", "John Doe", false)]
    [InlineData("!Name^=John", "Jane John", true)]
    [InlineData("!Name^=John", "John Doe", false)]
    [InlineData("!Name$=John", "Jane Doe", true)]
    [InlineData("!Name$=John", "Mr John", false)]
    [InlineData("!Id=5", "5", false)]
    [InlineData("!Id=5", "6", true)]
    [InlineData("!Id>5", "5", true)]
    [InlineData("!Id>5", "6", false)]
    [InlineData("!Id>=5", "4", true)]
    [InlineData("!Id>=5", "5", false)]
    [InlineData("!Id<5", "5", true)]
    [InlineData("!Id<5", "4", false)]
    [InlineData("!Id<=5", "6", true)]
    [InlineData("!Id<=5", "5", false)]
    [InlineData("!Id!=5", "5", true)]
    [InlineData("!Id!=5", "6", false)]
    public void Build_NegatedOperators_EvaluatesCorrectly(string filterString, string testVal, bool expected)
    {
        var filter = FilterParameters.From(filterString);
        var expr = FilterExpression.Build<TestUser>(filter);
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();

        int.TryParse(testVal, out var id);
        var user = new TestUser { Name = testVal, Id = id };
        compiled(user).Should().Be(expected);
    }

    [Fact]
    public void FilterPredicateBuilder_ClearCache_ExecutesSuccessfully()
    {
        Action act1 = () => EricksonLopez.Pagination.Internal.FilterPredicateBuilder.ClearCache(null);
        act1.Should().NotThrow();

        Action act2 = () => EricksonLopez.Pagination.Internal.FilterPredicateBuilder.ClearCache(new[] { typeof(TestUser) });
        act2.Should().NotThrow();
    }
}

