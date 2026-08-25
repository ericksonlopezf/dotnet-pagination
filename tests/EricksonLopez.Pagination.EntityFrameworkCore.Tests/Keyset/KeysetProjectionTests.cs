// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class KeysetProjectionTests
{
    private record EntityDto(int Id, string DisplayName);

    [Fact]
    public void Properties_SetAndGet()
    {
        var type = typeof(KeysetBuilder<>).Assembly.GetType("EricksonLopez.Pagination.EntityFrameworkCore.KeysetProjection`1")!.MakeGenericType(typeof(string));
        var instance = Activator.CreateInstance(type)!;

        var itemProp = type.GetProperty("Item")!;
        var c1Prop = type.GetProperty("C1")!;
        var c2Prop = type.GetProperty("C2")!;
        var c3Prop = type.GetProperty("C3")!;
        var c4Prop = type.GetProperty("C4")!;
        var c5Prop = type.GetProperty("C5")!;
        var c6Prop = type.GetProperty("C6")!;
        var c7Prop = type.GetProperty("C7")!;
        var c8Prop = type.GetProperty("C8")!;
        var c9Prop = type.GetProperty("C9")!;
        var c10Prop = type.GetProperty("C10")!;
        var c11Prop = type.GetProperty("C11")!;
        var c12Prop = type.GetProperty("C12")!;
        var c13Prop = type.GetProperty("C13")!;
        var c14Prop = type.GetProperty("C14")!;
        var c15Prop = type.GetProperty("C15")!;
        var c16Prop = type.GetProperty("C16")!;

        itemProp.SetValue(instance, "testItem");
        c1Prop.SetValue(instance, "c1");
        c2Prop.SetValue(instance, "c2");
        c3Prop.SetValue(instance, "c3");
        c4Prop.SetValue(instance, "c4");
        c5Prop.SetValue(instance, "c5");
        c6Prop.SetValue(instance, "c6");
        c7Prop.SetValue(instance, "c7");
        c8Prop.SetValue(instance, "c8");
        c9Prop.SetValue(instance, "c9");
        c10Prop.SetValue(instance, "c10");
        c11Prop.SetValue(instance, "c11");
        c12Prop.SetValue(instance, "c12");
        c13Prop.SetValue(instance, "c13");
        c14Prop.SetValue(instance, "c14");
        c15Prop.SetValue(instance, "c15");
        c16Prop.SetValue(instance, "c16");

        itemProp.GetValue(instance).Should().Be("testItem");
        c1Prop.GetValue(instance).Should().Be("c1");
        c2Prop.GetValue(instance).Should().Be("c2");
        c3Prop.GetValue(instance).Should().Be("c3");
        c4Prop.GetValue(instance).Should().Be("c4");
        c5Prop.GetValue(instance).Should().Be("c5");
        c6Prop.GetValue(instance).Should().Be("c6");
        c7Prop.GetValue(instance).Should().Be("c7");
        c8Prop.GetValue(instance).Should().Be("c8");
        c9Prop.GetValue(instance).Should().Be("c9");
        c10Prop.GetValue(instance).Should().Be("c10");
        c11Prop.GetValue(instance).Should().Be("c11");
        c12Prop.GetValue(instance).Should().Be("c12");
        c13Prop.GetValue(instance).Should().Be("c13");
        c14Prop.GetValue(instance).Should().Be("c14");
        c15Prop.GetValue(instance).Should().Be("c15");
        c16Prop.GetValue(instance).Should().Be("c16");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_ForwardPagination_ProjectsAndPagesCorrectly()
    {
        using var context = await TestDbContext.CreateInMemoryAsync(20);
        var parameters1 = new CursorPaginationParametersBuilder().WithFirst(5).Build();

        var paged1 = await context.Entities.AsQueryable()
            .Keyset(parameters1)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new EntityDto(e.Id, e.Name.ToUpperInvariant()));

        paged1.Should().NotBeNull();
        paged1.Count.Should().Be(5);
        paged1.HasNextPage.Should().BeTrue();
        paged1.HasPreviousPage.Should().BeFalse();
        paged1[0].Id.Should().Be(1);
        paged1[0].DisplayName.Should().Be("ENTITY 1");
        paged1[4].Id.Should().Be(5);
        paged1[4].DisplayName.Should().Be("ENTITY 5");
        paged1.StartCursor.Should().NotBeNull();
        paged1.EndCursor.Should().NotBeNull();

        // Page 2
        var parameters2 = new CursorPaginationParametersBuilder().WithFirst(5).WithAfter(paged1.EndCursor).Build();
        var paged2 = await context.Entities.AsQueryable()
            .Keyset(parameters2)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new EntityDto(e.Id, e.Name.ToUpperInvariant()));

        paged2.Should().NotBeNull();
        paged2.Count.Should().Be(5);
        paged2.HasNextPage.Should().BeTrue();
        paged2.HasPreviousPage.Should().BeTrue();
        paged2[0].Id.Should().Be(6);
        paged2[0].DisplayName.Should().Be("ENTITY 6");
        paged2[4].Id.Should().Be(10);
        paged2[4].DisplayName.Should().Be("ENTITY 10");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_BackwardPagination_ProjectsAndPagesCorrectly()
    {
        using var context = await TestDbContext.CreateInMemoryAsync(20);
        var parameters = new CursorPaginationParametersBuilder().WithLast(5).Build();

        var paged = await context.Entities.AsQueryable()
            .Keyset(parameters)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new EntityDto(e.Id, e.Name.ToUpperInvariant()));

        paged.Should().NotBeNull();
        paged.Count.Should().Be(5);
        paged.HasNextPage.Should().BeFalse();
        paged.HasPreviousPage.Should().BeTrue();
        paged[0].Id.Should().Be(16);
        paged[0].DisplayName.Should().Be("ENTITY 16");
        paged[4].Id.Should().Be(20);
        paged[4].DisplayName.Should().Be("ENTITY 20");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_BackwardPaginationWithBeforeCursor_PaginatesCorrectly()
    {
        using var context = await TestDbContext.CreateInMemoryAsync(20);

        // Fetch last page first
        var lastPage = await context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithLast(5).Build())
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new EntityDto(e.Id, e.Name.ToUpperInvariant()));

        // Fetch page before last page
        var prevParameters = new CursorPaginationParametersBuilder().WithLast(5).WithBefore(lastPage.StartCursor).Build();
        var prevPage = await context.Entities.AsQueryable()
            .Keyset(prevParameters)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new EntityDto(e.Id, e.Name.ToUpperInvariant()));

        prevPage.Should().NotBeNull();
        prevPage.Count.Should().Be(5);
        prevPage.HasNextPage.Should().BeTrue();
        prevPage.HasPreviousPage.Should().BeTrue();
        prevPage[0].Id.Should().Be(11);
        prevPage[0].DisplayName.Should().Be("ENTITY 11");
        prevPage[4].Id.Should().Be(15);
        prevPage[4].DisplayName.Should().Be("ENTITY 15");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_NullSelector_ThrowsArgumentNullException()
    {
        using var context = await TestDbContext.CreateInMemoryAsync(1);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(5).Build())
            .Ascending(e => e.Id);

        Func<Task> act = async () => await builder.ToCursorPagedListAsync<EntityDto>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithProjection_NoColumns_ThrowsInvalidOperationException()
    {
        using var context = await TestDbContext.CreateInMemoryAsync(1);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(5).Build());

        Func<Task> act = async () => await builder.ToCursorPagedListAsync(e => new EntityDto(e.Id, e.Name));
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*At least one column*");
    }
}
