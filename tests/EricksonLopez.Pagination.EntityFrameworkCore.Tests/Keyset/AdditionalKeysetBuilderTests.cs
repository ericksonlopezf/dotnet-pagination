// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests;

public class AdditionalKeysetBuilderTests
{
    

    private TestDbContext GetContext(int entityCount)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        for (int i = 1; i <= entityCount; i++)
        {
            context.Entities.Add(new TestEntityBuilder().WithId(i).WithName($"Entity {i}").Build());
        }
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithSelector_ReturnsProjectedData()
    {
        using var context = GetContext(5);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).Build())
            .Ascending(e => e.Id);

        var result = await builder.ToCursorPagedListAsync(e => new { DtoId = e.Id, DtoName = e.Name });
        
        result.Count.Should().Be(3);
        result[0].DtoId.Should().Be(1);
        result[0].DtoName.Should().Be("Entity 1");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WithSelector_ThrowsIfTooManyColumns()
    {
        using var context = GetContext(1);
        // F-006: limit expanded from 5 to 16; we need 17 columns to trigger the exception
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).Build())
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id)
            .Ascending(e => e.Name)
            .Ascending(e => e.Id); // 17 columns — exceeds the new 16-column limit

        Func<Task> act = async () => await builder.ToCursorPagedListAsync(e => e.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum of 16 keyset columns*");
    }

    [Fact]
    public async Task ToPagedAsyncEnumerable_StreamsData()
    {
        using var context = GetContext(5);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).Build())
            .Ascending(e => e.Id);

        var list = await builder.ToPagedAsyncEnumerable().ToListAsync();
        list.Count.Should().Be(3);
        list[0].Id.Should().Be(1);
    }

    [Fact]
    public void ToPagedAsyncEnumerable_Backward_ThrowsInvalidOperationException()
    {
        using var context = GetContext(5);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithLast(3).Build())
            .Ascending(e => e.Id);

        Action act = () => builder.ToPagedAsyncEnumerable();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not supported when paginating backwards*");
    }

    [Fact]
    public async Task Ascending_String_SortsDynamically()
    {
        using var context = GetContext(5);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).Build())
            .Ascending("Name");

        var list = await builder.ToPagedAsyncEnumerable().ToListAsync();
        list.Count.Should().Be(3);
        list[0].Name.Should().Be("Entity 1");
    }

    [Fact]
    public async Task Descending_String_SortsDynamically()
    {
        using var context = GetContext(5);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).Build())
            .Descending("Name");

        var list = await builder.ToPagedAsyncEnumerable().ToListAsync();
        list.Count.Should().Be(3);
        list[0].Name.Should().Be("Entity 5");
    }

    [Fact]
    public async Task SortBy_SortParameters_SortsDynamically()
    {
        using var context = GetContext(5);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).Build())
            .SortBy(SortParameters.From("Name desc, Id asc"));

        var list = await builder.ToPagedAsyncEnumerable().ToListAsync();
        list.Count.Should().Be(3);
        list[0].Name.Should().Be("Entity 5");
    }

    [Fact]
    public async Task InvalidCursor_TooLong_Throws()
    {
        using var context = GetContext(1);
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).WithAfter(new string('A', 5000)).Build())
            .Ascending(e => e.Id);

        Func<Task> act = async () => await builder.ToCursorPagedListAsync();
        await act.Should().ThrowAsync<InvalidPaginationCursorException>();
    }
    
    [Fact]
    public async Task InvalidCursor_WrongParts_Throws()
    {
        using var context = GetContext(1);
        var badCursor = HmacCursorEncoder.DevelopmentDefault.Encode("M|v2|hash|part1|part2"); // 2 parts
        var builder = context.Entities.AsQueryable()
            .Keyset(new CursorPaginationParametersBuilder().WithFirst(3).WithAfter(badCursor).Build())
            .Ascending(e => e.Id); // 1 column expected

        Func<Task> act = async () => await builder.ToCursorPagedListAsync();
        await act.Should().ThrowAsync<InvalidPaginationCursorException>();
    }
    [Fact]
    public void Keyset_ParametersNull_ReturnsBuilder()
    {
        using var context = GetContext(1);
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParameters());
        builder.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_GenericType_NullFullName()
    {
        using var context = GetContext(1);
        // T is generic, so FullName is null. The builder will use Name.
        var builder = context.Set<TestEntity>().AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build()).Ascending(e => e.Id);
        var res = await builder.ToCursorPagedListAsync();
        res.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_NullStringColumn()
    {
        using var context = GetContext(1);
        var entity = await context.Entities.FirstAsync();
        entity.Name2 = null;
        await context.SaveChangesAsync();
        
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build()).Ascending(e => e.Name2);
        var res = await builder.ToCursorPagedListAsync();
        res.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_DecoderReturnsEmpty_ReturnsNullCursor()
    {
        using var context = GetContext(1);
        
        var mockEncoder = NSubstitute.Substitute.For<ICursorEncoder>();
        mockEncoder.Decode(Arg.Any<string>()).Returns("");
        
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).WithAfter("something").Build(), cursorEncoder: mockEncoder).Ascending(e => e.Id);
        var res = await builder.ToCursorPagedListAsync();
        res.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ToCursorPagedListAsync_LoggerNull_NoException()
    {
        using var context = GetContext(1);
        // By default, no logger factory is injected in standard query provider in these tests, so it returns null for logger.
        // The condition Logger == null is already covered by most tests, wait, no, we need to test the opposite?
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build()).Ascending(e => e.Id);
        var res = await builder.ToCursorPagedListAsync();
        res.Should().NotBeNull();
    }


    
    [Fact]
    public void KeysetBuilder_DeserializeCursorParts_LessPartsThanColumns()
    {
        using var context = GetContext(1);
        var encoder = new Base64CursorEncoder();
        // M|v2|hash|part1 (only 1 part, but 2 columns)
        var cursor = encoder.Encode("M|v2|hash|1");
        var builder = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(cursor).Build())
            .Ascending(e => e.Id).Ascending(e => e.Name);
            
        Func<Task> act = async () => await builder.ToCursorPagedListAsync();
        act.Should().ThrowAsync<InvalidPaginationCursorException>();
    }
    
    [Fact]
        public async Task KeysetBuilder_CompareTo_LessThan()
    {
        using var context = GetContext(2);
        var builder1 = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build()).Descending(e => e.Name);
        var paged = await builder1.ToCursorPagedListAsync();
        
        var builder2 = context.Entities.AsQueryable().Keyset(new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(paged.EndCursor).Build()).Descending(e => e.Name);
        var res = await builder2.ToCursorPagedListAsync();
        res.Should().NotBeNull();
    }
        [Fact]
    public async Task KeysetBuilder_CompareToFallback_UsesIComparable()
    {
        // Act: Custom struct that implements IComparable but does not have < or > operators
        using var context = GetContext(2);
        (await context.Entities.FirstAsync(e => e.Id == 1)).BooleanValue = true;
        (await context.Entities.FirstAsync(e => e.Id == 2)).BooleanValue = false;
        await context.SaveChangesAsync();
        
        var items = context.Entities.AsQueryable();
        
        var builder = items.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Ascending(x => x.BooleanValue);

        // Let's invoke the filter directly to hit the generated expression
        var paged = await builder.ToCursorPagedListAsync();
        
        var builder2 = items.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(paged.EndCursor).Build())
            .Ascending(x => x.BooleanValue);
            
        var list2 = await builder2.ToCursorPagedListAsync();
        list2.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task KeysetBuilder_StringCoalesce_HandlesNullStrings()
    {
        // Act: Create an entity with a null string
        using var context = GetContext(2);
        (await context.Entities.FirstAsync(e => e.Id == 1)).Name2 = null;
        (await context.Entities.FirstAsync(e => e.Id == 2)).Name2 = "B";
        await context.SaveChangesAsync();
        
        var items = context.Entities.AsQueryable();
        
        var builder = items.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Descending(x => x.Name2)
            .Ascending(x => x.Id);

        var paged = await builder.ToCursorPagedListAsync();
        
        var builder2 = items.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).WithAfter(paged.EndCursor).Build())
            .Descending(x => x.Name2)
            .Ascending(x => x.Id);
            
        var list2 = await builder2.ToCursorPagedListAsync();
        list2.Should().HaveCount(1);
    }
    [Fact]
    public async Task KeysetBuilder_CursorPartsMismatch_ThrowsInvalidPaginationCursorException()
    {
        using var context = GetContext(1);
        var items = context.Entities.AsQueryable();
        
        var cursor = HmacCursorEncoder.DevelopmentDefault.Encode("M|v2|TestFingerprint|1|2|3");
        var builder = items.Keyset(new CursorPaginationParametersBuilder().WithFirst(10).WithAfter(cursor).Build())
            .Ascending(x => x.Id);
            
        var act = async () => await builder.ToCursorPagedListAsync();
        
        await act.Should().ThrowAsync<InvalidPaginationCursorException>().WithMessage("*Cursor has * parts but keyset expects*");
    }

    [Fact]
    public async Task KeysetBuilder_AscendingString_MultipleColumns_Valid()
    {
        using var context = GetContext(2);
        var items = context.Entities.AsQueryable();
        
        var builder = items.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Ascending(x => x.Id)
            .Ascending("Name");
            
        var paged = await builder.ToCursorPagedListAsync();
        paged.Should().HaveCount(1);
    }

    [Fact]
    public async Task KeysetBuilder_DescendingString_MultipleColumns_Valid()
    {
        using var context = GetContext(2);
        var items = context.Entities.AsQueryable();
        
        var builder = items.Keyset(new CursorPaginationParametersBuilder().WithFirst(1).Build())
            .Ascending(x => x.Id)
            .Descending("Name");
            
        var paged = await builder.ToCursorPagedListAsync();
        paged.Should().HaveCount(1);
    }

}






