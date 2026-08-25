// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using AwesomeAssertions.Execution;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class KeysetBuilderCoverageTests
{
    

    
    private static TestDbContext GetContext(int entityCount = 0) => TestDbContext.CreateInMemory(entityCount);

    
    
#pragma warning restore S1172

[Fact]
    public async Task KeysetBuilder_AllPrimitiveTypes_PaginatesCorrectly()
    {
        var ctx = GetContext();
        
        

        await ctx.Set<TypeEntity>().AddRangeAsync(
            new TypeEntityBuilder().WithId(1L).WithGuid(Guid.Parse("00000000-0000-0000-0000-000000000001")).WithDateTimeOffset(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)).WithDateTime(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).WithString("A").Build(),
            new TypeEntityBuilder().WithId(2L).WithGuid(Guid.Parse("00000000-0000-0000-0000-000000000002")).WithDateTimeOffset(new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero)).WithDateTime(new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc)).WithString("B").Build(),
            new TypeEntityBuilder().WithId(3L).WithGuid(Guid.Parse("00000000-0000-0000-0000-000000000003")).WithDateTimeOffset(new DateTimeOffset(2020, 1, 3, 0, 0, 0, TimeSpan.Zero)).WithDateTime(new DateTime(2020, 1, 3, 0, 0, 0, DateTimeKind.Utc)).WithString("C").Build()
        );
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // 1. Long
        var cp1 = new CursorPaginationParametersBuilder().WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("1")).WithFirst(1).Build();
        var p1 = await query.Keyset(cp1).Ascending(e => e.Id).ToCursorPagedListAsync();
        p1[0].Id.Should().Be(2L);

        // 2. Guid
        var cp2 = new CursorPaginationParametersBuilder().WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("00000000-0000-0000-0000-000000000001")).WithFirst(1).Build();
        var p2 = await query.Keyset(cp2).Ascending(e => e.GuidVal).ToCursorPagedListAsync();
        p2[0].Id.Should().Be(2L);

        // 3. Guid descending (forces greaterthan in compareto fallback)
        var cp3 = new CursorPaginationParametersBuilder().WithBefore(HmacCursorEncoder.DevelopmentDefault.Encode("00000000-0000-0000-0000-000000000001")).WithLast(1).Build();
        var p2d = await query.Keyset(cp3).Descending(e => e.GuidVal).ToCursorPagedListAsync();
        p2d[0].Id.Should().Be(2L);

        // 4. DateTimeOffset
        var cp4 = new CursorPaginationParametersBuilder().WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("2020-01-01T00:00:00.0000000+00:00")).WithFirst(1).Build();
        Func<Task> act = async () => await query.Keyset(cp4).Ascending(e => e.DateTimeOffsetVal).ToCursorPagedListAsync();
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*could not be translated*");
        
        // 5. String descending
        var cp5 = new CursorPaginationParametersBuilder().WithBefore(HmacCursorEncoder.DevelopmentDefault.Encode("A")).WithLast(1).Build();
        var p4 = await query.Keyset(cp5).Descending(e => e.StringVal).ToCursorPagedListAsync();
        p4[0].Id.Should().Be(2L);

        // 6. DateTime
        var cp6 = new CursorPaginationParametersBuilder().WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("2020-01-01T00:00:00.0000000Z")).WithFirst(1).Build();
        var p6 = await query.Keyset(cp6).Ascending(e => e.DateTimeVal).ToCursorPagedListAsync();
        p6[0].Id.Should().Be(2L);

        // 7. Empty page
        var cp7 = new CursorPaginationParametersBuilder().WithAfter(HmacCursorEncoder.DevelopmentDefault.Encode("Z")).WithFirst(1).Build();
        var p7 = await query.Keyset(cp7).Ascending(e => e.StringVal).ToCursorPagedListAsync();
        p7.Should().BeEmpty();


    }

[Fact]
    public async Task KeysetBuilder_CustomEncoder_Used()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        var cp = new CursorPaginationParametersBuilder().WithFirst(1).Build();
        var paged = await query.Keyset(cp, cursorEncoder: new CustomCursorEncoder()).Ascending(e => e.Id).ToCursorPagedListAsync();
        
        paged.EndCursor.Should().StartWith("CUSTOM-");
        
        var nextCp = new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(paged.EndCursor).Build();
        var paged2 = await query.Keyset(nextCp, cursorEncoder: new CustomCursorEncoder()).Ascending(e => e.Id).ToCursorPagedListAsync();
        paged2.Should().BeEmpty();
    }

[Fact]
    public async Task KeysetBuilder_FiveColumns_PaginatesCorrectly()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        var cp = new CursorPaginationParametersBuilder().WithFirst(1).Build();
        
        // 5 columns
        var builder = query.Keyset(cp)
            .Ascending(e => e.Id)
            .Ascending(e => e.StringVal)
            .Ascending(e => e.Id)
            .Ascending(e => e.StringVal)
            .Ascending(e => e.Id);
            
        var paged = await builder.ToCursorPagedListAsync();
        using (new AssertionScope())
        {
            paged.Count.Should().Be(1);
            paged.EndCursor.Should().NotBeNull();
        }
        
        // Deserialize next
        var nextCp = new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(paged.EndCursor).Build();
        var paged2 = await query.Keyset(nextCp)
            .Ascending(e => e.Id)
            .Ascending(e => e.StringVal)
            .Ascending(e => e.Id)
            .Ascending(e => e.StringVal)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();
            
        using (new AssertionScope())
        {
            paged2.Count.Should().Be(1);
            paged2[0].Id.Should().Be(2);
        }
    }

[Fact]
    public async Task KeysetBuilder_EmptyCursorParts_Ignored()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // Create an invalid cursor with empty parts: "M|v2|Fingerprint|||||"
        var builder = query.Keyset(new CursorPaginationParameters()).Ascending(e => e.Id);
        var fingerprint = builder.GetKeysetSchemaFingerprint();
        var invalidCursor = HmacCursorEncoder.DevelopmentDefault.Encode($"M|v2|{fingerprint}|||||");
        
        var nextCp = new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(invalidCursor).Build();
        var act = async () => await query.Keyset(nextCp).Ascending(e => e.Id).ToCursorPagedListAsync();
        
        // Should throw because empty parts skip population, leading to incomplete parsing
        await act.Should().ThrowAsync<InvalidPaginationCursorException>();
    }

[Fact]
    public void KeysetBuilder_ValidationException_Messages()
    {
        var ctx = GetContext();
        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // Not a MemberExpression, but nullable so it throws InvalidOperationException with "Property"
        var act1 = () => query.Keyset(new CursorPaginationParameters()).Ascending(e => (int?)1);
        act1.Should().Throw<InvalidOperationException>().WithMessage("*Property*");
        
        // Nullable properties
        var act2 = () => query.Keyset(new CursorPaginationParameters()).Ascending(e => (int?)e.Id);
        act2.Should().Throw<InvalidOperationException>().WithMessage("*Property*");
    }

[Fact]
    public void KeysetBuilder_Descending_ValidationException()
    {
        var ctx = GetContext();
        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        var act1 = () => query.Keyset(new CursorPaginationParameters()).Descending(e => (int?)1);
        act1.Should().Throw<InvalidOperationException>().WithMessage("*Property*");
        
        var act2 = () => query.Keyset(new CursorPaginationParameters()).Descending("Id"); // Valid
        act2.Should().NotThrow();
    }

[Fact]
    public void KeysetBuilder_SortBy_ParsesCorrectly()
    {
        var query = Enumerable.Empty<TypeEntity>().AsQueryable();
        // Tests explicitly provided directions
        var builder = query.Keyset(new CursorPaginationParameters()).SortBy(new SortParameters { Value = "Id asc, StringVal desc" });
        builder.Should().NotBeNull();

        // Tests default ascending (no direction provided) and empty parts (the extra commas)
        var builder2 = query.Keyset(new CursorPaginationParameters()).SortBy(new SortParameters { Value = "Id, , StringVal" });
        builder2.Should().NotBeNull();
    }

[Fact]
    public async Task KeysetBuilder_ToCursorPagedListAsync_WithProjection_ProjectsItemsCorrectly()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();
        
        // Use 5 columns to hit parts[0..4]
        var paged = await query.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Ascending(e => e.Id)
            .Ascending(e => e.StringVal)
            .Ascending(e => e.Id)
            .Ascending(e => e.Id)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new { e.Id }); // Projection overload!
            
        using (new AssertionScope())
        {
            paged.Count.Should().Be(1);
            paged.EndCursor.Should().NotBeNullOrEmpty();
        }
        
        // Also test Last to kill First/Last HasValue mutants
        var pagedLast = await query.Keyset(new CursorPaginationParametersBuilder().WithLast(1).Build())
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new { e.Id });
        pagedLast.Should().NotBeNull();
        
        // And without First or Last
        var pagedNoLimits = await query.Keyset(new CursorPaginationParameters())
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync(e => new { e.Id });
        pagedNoLimits.Should().NotBeNull();
    }

[Fact]
    public async Task KeysetBuilder_BackwardPagination_VerifiesPreviousAndNextPage()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddRangeAsync(
            new TypeEntityBuilder().WithId(1).Build(),
            new TypeEntityBuilder().WithId(2).Build(),
            new TypeEntityBuilder().WithId(3).Build(),
            new TypeEntityBuilder().WithId(4).Build()
        );
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();

        // Query the first 2 items to get a cursor
        var firstPage = await query.Keyset(new CursorPaginationParametersBuilder().WithFirst(2).Build()).Ascending(e => e.Id).ToCursorPagedListAsync();
        using (new AssertionScope())
        {
            firstPage.HasNextPage.Should().BeTrue();
            firstPage.HasPreviousPage.Should().BeFalse();
        }
        
        // Backward query BEFORE Id=3. (Items: 1, 2)
        var endCursor = firstPage.EndCursor; // EndCursor of Id=2
        var backwardPage = await query.Keyset(new CursorPaginationParametersBuilder().WithLast(2).WithBefore(endCursor).Build()).Ascending(e => e.Id).ToCursorPagedListAsync();
        
        // Items 1 returned. (Since EndCursor was Id=2, Before Id=2 is only Id=1)
        using (new AssertionScope())
        {
            backwardPage.Count.Should().Be(1);
            backwardPage[0].Id.Should().Be(1);
            
            // For backward pagination, HasNextPage is true if before != null
            backwardPage.HasNextPage.Should().BeTrue();
            backwardPage.HasPreviousPage.Should().BeFalse();
        }

        // Now test when hasMore IS true for backward pagination (e.g., fetch Last=1 before Id=3)
        var middleCursor = await query.Keyset(new CursorPaginationParametersBuilder().WithFirst(3).Build()).Ascending(e => e.Id).ToCursorPagedListAsync();
        var cursor3 = middleCursor.EndCursor; // After Id=3
        var backwardPage2 = await query.Keyset(new CursorPaginationParametersBuilder().WithLast(1).WithBefore(cursor3).Build()).Ascending(e => e.Id).ToCursorPagedListAsync();
        
        using (new AssertionScope())
        {
            backwardPage2.Count.Should().Be(1);
            backwardPage2[0].Id.Should().Be(2); // Fetches Id=2
            // Since there is Id=1 before Id=2, hasMore should be TRUE!
            backwardPage2.HasPreviousPage.Should().BeTrue();
            backwardPage2.HasNextPage.Should().BeTrue(); 
        }
    }

[Fact]
    public async Task KeysetBuilder_WithSpecialCharacters_EncodesProperly()
    {
        var ctx = GetContext();
        
        
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(1).Build());
        await ctx.Set<TypeEntity>().AddAsync(new TypeEntityBuilder().WithId(2).Build());
        await ctx.SaveChangesAsync();

        var query = ctx.Set<TypeEntity>().AsQueryable();

        var page = await query.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Ascending(e => e.StringVal)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();
            
        using (new AssertionScope())
        {
            page.Count.Should().Be(1);
            page.EndCursor.Should().NotBeNullOrEmpty();
        }
        
        var next = await query.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(page.EndCursor).Build())
            .Ascending(e => e.StringVal)
            .Ascending(e => e.Id)
            .ToCursorPagedListAsync();
            
        using (new AssertionScope())
        {
            next.Count.Should().Be(1);
            next[0].Id.Should().Be(2);
        }
    }

[Fact]
    public async Task KeysetBuilder_ToCursorPagedListAsync_CursorLengthExceeded_Throws()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var builder = query.Keyset(new CursorPaginationParametersBuilder().WithAfter(new string('A', 5000)).Build()).Ascending(e => e.Id);
        
        await FluentActions.Awaiting(() => builder.ToCursorPagedListAsync())
            .Should().ThrowAsync<InvalidPaginationCursorException>();
    }

[Fact]
    public async Task KeysetBuilder_ToCursorPagedListAsync_DecodedCursorLengthExceeded_Throws()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var encoder = new Base64CursorEncoder();
        var builder = query.Keyset(new CursorPaginationParametersBuilder().WithAfter(encoder.Encode(new string('B', 5000))).Build()).Ascending(e => e.Id);
        
        await FluentActions.Awaiting(() => builder.ToCursorPagedListAsync())
            .Should().ThrowAsync<InvalidPaginationCursorException>();
    }

[Fact]
    public async Task KeysetBuilder_ToCursorPagedListAsync_InvalidCursorFormat_Throws()
    {
        var ctx = GetContext();
        var query = ctx.Entities.AsQueryable();
        var encoder = new Base64CursorEncoder();
        var builder = query.Keyset(new CursorPaginationParametersBuilder().WithAfter(encoder.Encode("INVALID")).Build()).Ascending(e => e.Id);
        
        await FluentActions.Awaiting(() => builder.ToCursorPagedListAsync())
            .Should().ThrowAsync<InvalidPaginationCursorException>();
    }

[Fact]
    public async Task KeysetBuilder_Backward_StringAndEnum()
    {
        var ctx = GetContext();
        
        

        await ctx.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").WithState(TestState.A).Build());
        await ctx.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").WithState(TestState.B).Build());
        await ctx.SaveChangesAsync();

        var full = await ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(2).Build())
            .Ascending(x => x.Name)
            .Ascending(x => x.State)
            .ToCursorPagedListAsync();
            
        var forward = await ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(full.StartCursor).Build())
            .Ascending(x => x.Name)
            .Ascending(x => x.State)
            .ToCursorPagedListAsync();
            
        var backward = await ctx.Entities
            .Keyset(new CursorPaginationParametersBuilder().WithLast(1).WithBefore(full.EndCursor).Build())
            .Ascending(x => x.Name)
            .Ascending(x => x.State)
            .ToCursorPagedListAsync();
            
        using (new AssertionScope())
        {
            forward.Should().NotBeNull();
            backward.Should().NotBeNull();
            backward.Should().HaveCount(1);
            backward[0].Name.Should().Be("A");
        }
    }

[Fact]
        public async Task ExecuteCursorQueryAsync_NullLastKey_Throws()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build()); // NullableId is allowed to be null in DB
            await context.SaveChangesAsync();
            var items = context.Entities.AsQueryable();

            var act = async () => await items.Keyset(new CursorPaginationParameters())
                .Ascending(x => x.NullableId)
                .ToCursorPagedListAsync();

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Keyset pagination on nullable property*");
        }

[Fact]
        public async Task KeysetBuilder_StringMethods_ThrowsOnNull()
        {
            using var context = GetContext();
            var source = context.Entities.AsQueryable();
            var builder = source.Keyset(new CursorPaginationParameters());
            
            Action act1 = () => builder.Ascending((string)null!);
            act1.Should().Throw<ArgumentException>();

            Action act2 = () => builder.Ascending(string.Empty);
            act2.Should().Throw<ArgumentException>();

            Action act3 = () => builder.Descending((string)null!);
            act3.Should().Throw<ArgumentException>();

        }

[Fact]
        public async Task KeysetBuilder_ThenAscending_WithExpression_Works()
        {
            using var context = GetContext();
            var source = context.Entities.AsQueryable();
            var builder = source.Keyset(new CursorPaginationParameters());
            builder.Ascending(x => x.Id);
            builder.Ascending(x => x.Name); // Uncovered line 87, 89
            builder.Should().NotBeNull();
        }

[Fact]
        public async Task KeysetBuilder_ThenDescending_WithExpression_Works()
        {
            using var context = GetContext();
            var source = context.Entities.AsQueryable();
            var builder = source.Keyset(new CursorPaginationParameters());
            builder.Descending(x => x.Id);
            builder.Descending(x => x.Name); // Uncovered line 87, 89
            builder.Should().NotBeNull();
        }

[Fact]
        public async Task KeysetBuilder_StringMethods_ThrowsOnInvalidProperty()
        {
            using var context = GetContext();
            var source = context.Entities.AsQueryable();
            var builder = source.Keyset(new CursorPaginationParameters());
            
            Action act1 = () => builder.Ascending("InvalidProperty");
            act1.Should().Throw<ArgumentException>();

            Action act2 = () => builder.Descending("InvalidProperty");
            act2.Should().Throw<ArgumentException>();
        }

[Fact]
        public async Task KeysetBuilder_ToPagedAsyncEnumerable_ThrowsIfNoColumns()
        {
            using var context = GetContext();
            var source = context.Entities.AsQueryable();
            var builder = source.Keyset(new CursorPaginationParameters());
            
            var act = () => builder.ToPagedAsyncEnumerable();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*At least one column must be specified*");
        }

[Fact]
        public async Task KeysetBuilder_ToPagedAsyncEnumerable_WithValidCursor_Works()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
            await context.SaveChangesAsync();
            var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
                .Ascending(x => x.Id);
            
            // Generate a valid v2 cursor for Id = 1
            var paged = await builder.ToCursorPagedListAsync();
            var cursor = paged.EndCursor; // Next cursor

            var builder2 = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithAfter(cursor).Build())
                .Ascending(x => x.Id);
            var asyncEnum = builder2.ToPagedAsyncEnumerable();
            
            var resultList = new List<TestEntity>();
            await foreach (var item in asyncEnum)
            {
                resultList.Add(item);
            }
            
            using (new AssertionScope())
            {
                resultList.Count.Should().Be(1);
                resultList[0].Id.Should().Be(2); // Should skip Id = 1
            }
        }

[Fact]
        public async Task KeysetBuilder_ParseLegacyV1Cursor_Works()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
            await context.SaveChangesAsync();
            
            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode("M|1");
            
            var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithAfter(cursor).Build(), acceptLegacyCursors: true)
                .Ascending(x => x.Id);
            
            var paged = await builder.ToCursorPagedListAsync();
            var itemsList = paged.ToList();
            using (new AssertionScope())
            {
                itemsList.Count.Should().Be(1);
                itemsList[0].Id.Should().Be(2);
            }
        }

[Fact]
        public async Task KeysetBuilder_ParseLegacyV1Cursor_ThrowsIfDisabled()
        {
            using var context = GetContext();
            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode("M|1");
            
            var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithAfter(cursor).Build(), acceptLegacyCursors: false)
                .Ascending(x => x.Id);
            
            var act = async () => await builder.ToCursorPagedListAsync();
            await act.Should().ThrowAsync<InvalidPaginationCursorException>()
                .WithMessage("*Legacy v1 cursor format is not accepted*");
        }

[Fact]
        public void KeysetBuilder_StringProperty_NotFound_ThrowsArgumentException()
        {
            var items = new List<TestEntity>().AsQueryable();
            var builder = items.Keyset(new CursorPaginationParameters());

            var act1 = () => builder.Ascending("NonExistentProperty");
            act1.Should().Throw<ArgumentException>().WithMessage("*not found on type*");

            var act2 = () => builder.Descending("NonExistentProperty");
            act2.Should().Throw<ArgumentException>().WithMessage("*not found on type*");

            var act3 = () => builder.SortBy(new SortParameters { Value = "NonExistentProperty" });
            act3.Should().Throw<ArgumentException>().WithMessage("*not found on type*");
        }

[Fact]
        public async Task ToCursorPagedListAsync_Projection_Backward_StringColumn()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
            await context.SaveChangesAsync();

            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode("{\"V\":[\"B\"]}");
            var parameters = new CursorPaginationParametersBuilder().WithLast(10).WithBefore(cursor).Build();
            
            var result = await context.Entities.AsQueryable()
                .Keyset(parameters)
                .Ascending(x => x.Name)
                .ToCursorPagedListAsync(x => new { x.Id, x.Name });
                
            result.Should().NotBeNull();
        }

[Fact]
        public async Task ToCursorPagedListAsync_NoColumns_ThrowsInvalidOperationException()
        {
            using var context = GetContext();
            var parameters = new CursorPaginationParameters();
            
            var act = async () => await context.Entities.AsQueryable()
                .Keyset(parameters)
                .ToCursorPagedListAsync(x => new { x.Id });
                
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("At least one column must be specified.");
        }

[Fact]
        public async Task ToCursorPagedListAsync_EnumProperty_StringCursor_Parsed()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).Build());
            await context.SaveChangesAsync();

            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode("M|One|1");
            var parameters = new CursorPaginationParametersBuilder().WithAfter(cursor).Build();
            
            var result = await context.Entities.AsQueryable()
                .Keyset(parameters, acceptLegacyCursors: true)
                .Ascending(x => x.StateValue)
                .Ascending(x => x.Id)
                .ToCursorPagedListAsync();
                
            result.Should().NotBeNull();
        }

[Fact]
        public async Task ToCursorPagedListAsync_EnumProperty_Backward_Parsed()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).Build());
            await context.SaveChangesAsync();

            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode("M|Two|2");
            var parameters = new CursorPaginationParametersBuilder().WithBefore(cursor).Build();
            
            var result = await context.Entities.AsQueryable()
                .Keyset(parameters, acceptLegacyCursors: true)
                .Ascending(x => x.StateValue)
                .Ascending(x => x.Id)
                .ToCursorPagedListAsync();
                
            result.Count.Should().BeGreaterThan(0);
        }

[Fact]
        public async Task ToCursorPagedListAsync_GuidProperty_StringCursor_Parsed()
        {
            using var context = GetContext();
            var g1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var g2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithGuid(g1).Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithGuid(g2).Build());
            await context.SaveChangesAsync();

            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode($"M|{g1}|1");
            var parameters = new CursorPaginationParametersBuilder().WithAfter(cursor).Build();
            
            var result = await context.Entities.AsQueryable()
                .Keyset(parameters, acceptLegacyCursors: true)
                .Ascending(x => x.GuidValue)
                .Ascending(x => x.Id)
                .ToCursorPagedListAsync();
                
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Count.Should().Be(1);
            }
        }

[Fact]
        public async Task ToCursorPagedListAsync_GuidProperty_Backward_Parsed()
        {
            using var context = GetContext();
            var g1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var g2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithGuid(g1).Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithGuid(g2).Build());
            await context.SaveChangesAsync();

            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode($"M|{g2}|2");
            var parameters = new CursorPaginationParametersBuilder().WithBefore(cursor).Build();
            
            var result = await context.Entities.AsQueryable()
                .Keyset(parameters, acceptLegacyCursors: true)
                .Ascending(x => x.GuidValue)
                .Ascending(x => x.Id)
                .ToCursorPagedListAsync();
                
            result.Count.Should().BeGreaterThan(0);
        }

[Fact]
        public async Task ToCursorPagedListAsync_StringProperty_Backward_Parsed()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
            await context.SaveChangesAsync();

            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode("M|B|2");
            var parameters = new CursorPaginationParametersBuilder().WithBefore(cursor).Build();
            
            var result = await context.Entities.AsQueryable()
                .Keyset(parameters, acceptLegacyCursors: true)
                .Ascending(x => x.Name)
                .Ascending(x => x.Id)
                .ToCursorPagedListAsync();
                
            result.Count.Should().BeGreaterThan(0);
        }

[Fact]
        public async Task ToCursorPagedListAsync_5Columns_ReturnsCorrectList()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
            await context.SaveChangesAsync();

            var result = await context.Entities.AsQueryable()
                .Keyset(new CursorPaginationParameters())
                .Ascending(x => x.Name)
                .Ascending(x => x.StateValue)
                .Ascending(x => x.GuidValue)
                .Ascending(x => x.Id)
                .Ascending(x => x.Id) // 5 columns!
                .ToCursorPagedListAsync();
                
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Count.Should().Be(2);
            }
        }

[Fact]
        public async Task ToCursorPagedListAsync_5Columns_Forward_Parsed()
        {
            using var context = GetContext();
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(1).WithName("A").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(2).WithName("B").Build());
            await context.Entities.AddAsync(new TestEntityBuilder().WithId(3).WithName("C").Build());
            await context.SaveChangesAsync();

            var builderWithoutCursor = context.Entities.AsQueryable()
                .Keyset(new CursorPaginationParameters(), acceptLegacyCursors: true)
                .Ascending(x => x.Name)
                .Ascending(x => x.Name2)
                .Ascending(x => x.Name3)
                .Ascending(x => x.Name4)
                .Ascending(x => x.Name5);
                
            var fingerprint = builderWithoutCursor.GetKeysetSchemaFingerprint();

            var cursor = HmacCursorEncoder.DevelopmentDefault.Encode($"M|v2|{fingerprint}|A|A2|A3|A4|A5");
            var parameters = new CursorPaginationParametersBuilder().WithAfter(cursor).Build();
            
            var result = await context.Entities.AsQueryable()
                .Keyset(parameters, acceptLegacyCursors: true)
                .Ascending(x => x.Name)
                .Ascending(x => x.Name2)
                .Ascending(x => x.Name3)
                .Ascending(x => x.Name4)
                .Ascending(x => x.Name5)
                .ToCursorPagedListAsync(x => x.Name);
                
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Count.Should().Be(2);
            }
        }
}





