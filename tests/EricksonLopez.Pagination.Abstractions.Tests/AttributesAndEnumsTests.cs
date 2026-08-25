// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.Abstractions.Tests;

public class AttributesAndEnumsTests
{
    [Fact]
    public void FilterableAttribute_DefaultConstructor_HasNullName()
    {
        var attr = new FilterableAttribute();
        attr.Name.Should().BeNull();
    }

    [Fact]
    public void FilterableAttribute_ConstructorWithName_SetsName()
    {
        var attr = new FilterableAttribute("customName");
        attr.Name.Should().Be("customName");
    }

    [Fact]
    public void FilterableAttribute_PropertySetter_UpdatesName()
    {
        var attr = new FilterableAttribute { Name = "updatedName" };
        attr.Name.Should().Be("updatedName");
    }

    [Fact]
    public void GenerateFilterProviderAttribute_Instantiation_Succeeds()
    {
        var attr = new GenerateFilterProviderAttribute();
        attr.Should().NotBeNull();
    }

    [Fact]
    public void SortDirection_EnumValues_AreDistinct()
    {
        SortDirection.Ascending.Should().NotBe(SortDirection.Descending);
        ((int)SortDirection.Ascending).Should().Be(0);
        ((int)SortDirection.Descending).Should().Be(1);
    }
}
