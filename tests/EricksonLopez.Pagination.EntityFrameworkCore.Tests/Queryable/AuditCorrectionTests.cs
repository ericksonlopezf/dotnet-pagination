// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

/// <summary>
/// DbContext for nullable key tests.
/// </summary>
public class NullableKeyDbContext : DbContext
{
    public DbSet<NullableKeyEntity> Items { get; set; } = null!;
    public NullableKeyDbContext(DbContextOptions<NullableKeyDbContext> options) : base(options) { }
}

/// <summary>
/// Tests for corrections applied based on the destructive audit findings.
/// Each test method is named after the specific defect it validates.
/// </summary>
public class AuditCorrectionTests
{
    // ─── Test infrastructure ──────────────────────────────────────────────────









    // ─── IMPROVEMENT: GetPageSize replaces EffectivePageSize ─────────────────

    /// <summary>
    /// Validates GetPageSize replaces EffectivePageSize with configurable defaults.
    /// </summary>
    [Fact]
    public void GetPageSize_ReplacesEffectivePageSize_WithConfigurableDefault()
    {
        var withFirst = new CursorPaginationParametersBuilder().WithFirst(20).Build();
        withFirst.GetPageSize(10).Should().Be(20);

        var withLast = new CursorPaginationParametersBuilder().WithLast(15).Build();
        withLast.GetPageSize(10).Should().Be(15);

        var withNeither = new CursorPaginationParameters();
        withNeither.GetPageSize(10).Should().Be(10);
        withNeither.GetPageSize(25).Should().Be(25); // configurable default
    }

    // ─── IMPROVEMENT: GetPageSize replaces EffectivePageSize ─────────────────

    /// <summary>
    /// Validates GetPageSize properly uses the configurable default instead of hardcoded 10.
    /// </summary>
    [Theory]
    [InlineData(20, null, null, 20)] // neither set → uses default
    [InlineData(20, 5, null, 5)]    // first set → uses first
    [InlineData(20, null, 7, 7)]    // last set → uses last
    public void GetPageSize_WithVariousConfigurations_ReturnsCorrectValue(
        int configuredDefault, int? first, int? last, int expected)
    {
        var p = new CursorPaginationParametersBuilder().WithFirst(first).WithLast(last).Build();
        p.GetPageSize(configuredDefault).Should().Be(expected);
    }

    [Fact]
    public async Task ToPagedListWithoutCountAsync_ReturnsPagedListWithoutTotalCount()
    {
        using var context = TestDbContext.CreateInMemory(15);

        var parameters = new PaginationParameters { Page = 1, PageSize = 10 };
        var page = await context.Entities.OrderBy(e => e.Id).ToPagedListWithoutCountAsync(parameters);

        page.Should().NotBeNull();
        page.Should().HaveCount(10);
        page.HasNextPage.Should().BeTrue();
        page.TotalCount.Should().BeNull();

        var projectedPage = await context.Entities.OrderBy(e => e.Id).ToPagedListWithoutCountAsync(e => e.Name, parameters);
        projectedPage.Should().NotBeNull();
        projectedPage.Should().HaveCount(10);
        projectedPage.HasNextPage.Should().BeTrue();
        projectedPage.TotalCount.Should().BeNull();
    }
}


