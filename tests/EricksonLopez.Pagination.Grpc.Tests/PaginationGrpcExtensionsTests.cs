// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Grpc;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.Grpc.Tests;

public class PaginationGrpcExtensionsTests
{
    [Fact]
    public void ToParameters_FromPaginationParametersMessage_Null_ReturnsDefault()
    {
        PaginationParametersMessage? message = null;
        var result = message!.ToParameters();
        
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void ToParameters_FromPaginationParametersMessage_Valid_ReturnsMapped()
    {
        var message = new PaginationParametersMessage { Page = 2, PageSize = 25 };
        var result = message.ToParameters();
        
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public void ToParameters_FromPaginationParametersMessage_WithOptions_Valid_ReturnsMapped()
    {
        var message = new PaginationParametersMessage { Page = 2, PageSize = 25 };
        var options = Substitute.For<IPaginationOptions>();
        options.MaxPageSize.Returns(50);
        
        var result = message.ToParameters(options);
        
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public void ToParameters_FromPaginationParametersMessage_Negative_ReturnsDefaults()
    {
        var message = new PaginationParametersMessage { Page = -1, PageSize = -5 };
        var result = message.ToParameters();
        
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void ToParameters_FromPaginationParametersMessage_Zero_ReturnsDefaults()
    {
        var message = new PaginationParametersMessage { Page = 0, PageSize = 0 };
        var result = message.ToParameters();
        
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_Null_ReturnsEmpty()
    {
        CursorPaginationParametersMessage? message = null;
        var result = message!.ToParameters();
        
        result.Should().NotBeNull();
        result.First.Should().BeNull();
        result.After.Should().BeNull();
        result.Last.Should().BeNull();
        result.Before.Should().BeNull();
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_Forward_Valid_ReturnsMapped()
    {
        var message = new CursorPaginationParametersMessage
        {
            First = 10,
            After = "afterCursor"
        };
        var result = message.ToParameters();
        
        result.First.Should().Be(10);
        result.After.Should().Be("afterCursor");
        result.Last.Should().BeNull();
        result.Before.Should().BeNull();
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_WithOptions_Valid_ReturnsMapped()
    {
        var message = new CursorPaginationParametersMessage
        {
            First = 10,
            After = "afterCursor"
        };
        var options = Substitute.For<IPaginationOptions>();
        options.MaxPageSize.Returns(50);
        
        var result = message.ToParameters(options);
        
        result.First.Should().Be(10);
        result.After.Should().Be("afterCursor");
        result.Last.Should().BeNull();
        result.Before.Should().BeNull();
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_Backward_Valid_ReturnsMapped()
    {
        var message = new CursorPaginationParametersMessage
        {
            Last = 5,
            Before = "beforeCursor"
        };
        var result = message.ToParameters();
        
        result.Last.Should().Be(5);
        result.Before.Should().Be("beforeCursor");
        result.First.Should().BeNull();
        result.After.Should().BeNull();
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_Negative_ThrowsArgumentOutOfRangeException()
    {
        var message = new CursorPaginationParametersMessage
        {
            First = -1,
            Last = -1
        };
        Action act = () => message.ToParameters();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_Zero_ReturnsEmpty()
    {
        var message = new CursorPaginationParametersMessage
        {
            First = 0,
            After = "",
            Last = 0,
            Before = string.Empty
        };
        var result = message.ToParameters();
        
        result.First.Should().BeNull();
        result.After.Should().BeNull();
        result.Last.Should().BeNull();
        result.Before.Should().BeNull();
    }

    [Fact]
    public void ToParameters_FromFilterParametersMessage_Null_ReturnsEmpty()
    {
        FilterParametersMessage? message = null;
        var result = message!.ToParameters();
        
        result.Should().Be(FilterParameters.Empty);
    }

    [Fact]
    public void ToParameters_FromFilterParametersMessage_EmptyString_ReturnsEmpty()
    {
        var message = new FilterParametersMessage { Value = "   " };
        var result = message.ToParameters();
        
        result.Should().Be(FilterParameters.Empty);
    }

    [Fact]
    public void ToParameters_FromFilterParametersMessage_Valid_ReturnsMapped()
    {
        var message = new FilterParametersMessage { Value = "Name:eq:Test" };
        var result = message.ToParameters();
        
        result.Value.Should().Be("Name:eq:Test");
    }

    [Fact]
    public void ToMessage_FromIPagedList_Null_ThrowsArgumentNullException()
    {
        IPagedList? pagedList = null;
        Action act = () => pagedList!.ToMessage();
        act.Should().Throw<ArgumentNullException>().WithMessage("*pagedList*");
    }

    [Fact]
    public void ToMessage_FromIPagedList_Valid_ReturnsMappedMessage()
    {
        var pagedList = PagedList<string>.WithCount(new List<string> { "A", "B" }, new PaginationParameters { Page = 2, PageSize = 10 }, 30);
        
        var message = pagedList.ToMessage();
        
        message.TotalCount.Should().Be(30);
        message.Page.Should().Be(2);
        message.PageSize.Should().Be(10);
        message.TotalPages.Should().Be(3);
        message.HasPreviousPage.Should().BeTrue();
        message.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ToMessage_FromIPagedList_WithoutCount_ReturnsDefaultTotalCount()
    {
        var pagedList = PagedList<string>.WithoutCount(new List<string> { "A", "B" }, new PaginationParameters { Page = 2, PageSize = 10 }, false);
        
        var message = pagedList.ToMessage();
        
        message.HasTotalCount.Should().BeFalse();
        message.HasTotalPages.Should().BeFalse();
    }

    [Fact]
    public void ToMessage_FromICursorPagedList_Null_ThrowsArgumentNullException()
    {
        ICursorPagedList? pagedList = null;
        Action act = () => pagedList!.ToMessage();
        act.Should().Throw<ArgumentNullException>().WithMessage("*pagedList*");
    }

    [Fact]
    public void ToMessage_FromICursorPagedList_Valid_ReturnsMappedMessage()
    {
        var pagedList = CursorPagedList<string>.Create(new List<string> { "A" }, "start", "end", true, false);
        
        var message = pagedList.ToMessage();
        
        message.StartCursor.Should().Be("start");
        message.EndCursor.Should().Be("end");
        message.HasPreviousPage.Should().BeTrue();
        message.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToMessage_FromICursorPagedList_NullCursors_ReturnsEmptyString()
    {
        var pagedList = CursorPagedList<string>.Create(new List<string>(), null, null, false, false);
        
        var message = pagedList.ToMessage();
        
        message.StartCursor.Should().BeEmpty();
        message.EndCursor.Should().BeEmpty();
    }
    [Fact]
    public void ToParameters_FromPaginationParametersMessage_ExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var message = new PaginationParametersMessage { Page = 1, PageSize = 1001 };
        Action act = () => message.ToParameters();
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*PageSize cannot exceed 1000.*");
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_MutuallyExclusive_ThrowsArgumentException()
    {
        var message = new CursorPaginationParametersMessage
        {
            First = 10,
            Last = 10
        };
        Action act = () => message.ToParameters();
        act.Should().Throw<ArgumentException>().WithMessage("*mutually exclusive*");
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_ExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var message = new CursorPaginationParametersMessage
        {
            First = 1001
        };
        Action act = () => message.ToParameters();
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*cannot exceed 1000.*");
    }

    [Fact]
    public void ToParameters_FromFilterParametersMessage_ExceedsMaxLength_ThrowsArgumentException()
    {
        var message = new FilterParametersMessage { Value = new string('a', FilterParameters.AbsoluteMaxLength + 1) };
        Action act = () => message.ToParameters();
        act.Should().Throw<ArgumentException>().WithMessage($"*{FilterParameters.AbsoluteMaxLength}*");
    }

    [Fact]
    public void ToParameters_FromSortParametersMessage_Null_ReturnsEmpty()
    {
        SortParametersMessage? message = null;
        var result = message!.ToParameters();
        result.Should().Be(SortParameters.Empty);
    }

    [Fact]
    public void ToParameters_FromSortParametersMessage_EmptyString_ReturnsEmpty()
    {
        var message = new SortParametersMessage { Value = "   " };
        var result = message.ToParameters();
        result.Should().Be(SortParameters.Empty);
    }

    [Fact]
    public void ToParameters_FromSortParametersMessage_Valid_ReturnsMapped()
    {
        var message = new SortParametersMessage { Value = "Name desc" };
        var result = message.ToParameters();
        result.Value.Should().Be("Name desc");
    }

    [Fact]
    public void ToMessage_PagedList_WithResponse_NullArgs_ThrowsArgumentNullException()
    {
        IPagedList<string>? nullList = null;
        var list = PagedList<string>.WithoutCount(new List<string>(), new PaginationParameters(), false);
        var response = new PagedListMetadataMessage();
        
        Action act1 = () => nullList!.ToMessage<string, PagedListMetadataMessage>(response, (r, m) => { });
        act1.Should().Throw<ArgumentNullException>().WithMessage("*pagedList*");

        Action act2 = () => list.ToMessage<string, PagedListMetadataMessage>(null!, (r, m) => { });
        act2.Should().Throw<ArgumentNullException>().WithMessage("*response*");

        Action act3 = () => list.ToMessage<string, PagedListMetadataMessage>(response, null!);
        act3.Should().Throw<ArgumentNullException>().WithMessage("*configureMetadata*");
    }

    [Fact]
    public void ToMessage_PagedList_WithResponse_Valid_CallsAction()
    {
        var pagedList = PagedList<string>.WithoutCount(new List<string> { "A" }, new PaginationParameters { Page = 1, PageSize = 10 }, false);
        var response = new FilterParametersMessage(); // Just using as a dummy response message
        
        PagedListMetadataMessage? configuredMetadata = null;
        
        var result = pagedList.ToMessage<string, FilterParametersMessage>(response, (r, m) =>
        {
            configuredMetadata = m;
        });

        result.Should().BeSameAs(response);
        configuredMetadata.Should().NotBeNull();
        configuredMetadata!.Page.Should().Be(1);
    }

    [Fact]
    public void ToMessage_CursorPagedList_WithResponse_NullArgs_ThrowsArgumentNullException()
    {
        ICursorPagedList<string>? nullList = null;
        var list = CursorPagedList<string>.Create(new List<string>(), null, null, false, false);
        var response = new CursorPagedListMetadataMessage();
        
        Action act1 = () => nullList!.ToMessage<string, CursorPagedListMetadataMessage>(response, (r, m) => { });
        act1.Should().Throw<ArgumentNullException>().WithMessage("*pagedList*");

        Action act2 = () => list.ToMessage<string, CursorPagedListMetadataMessage>(null!, (r, m) => { });
        act2.Should().Throw<ArgumentNullException>().WithMessage("*response*");

        Action act3 = () => list.ToMessage<string, CursorPagedListMetadataMessage>(response, null!);
        act3.Should().Throw<ArgumentNullException>().WithMessage("*configureMetadata*");
    }

    [Fact]
    public void ToMessage_CursorPagedList_WithResponse_Valid_CallsAction()
    {
        var pagedList = CursorPagedList<string>.Create(new List<string> { "A" }, "start", "end", true, false);
        var response = new FilterParametersMessage(); // Dummy response message
        
        CursorPagedListMetadataMessage? configuredMetadata = null;
        
        var result = pagedList.ToMessage<string, FilterParametersMessage>(response, (r, m) =>
        {
            configuredMetadata = m;
        });

        result.Should().BeSameAs(response);
        configuredMetadata.Should().NotBeNull();
        configuredMetadata!.StartCursor.Should().Be("start");
    }

    [Fact]
    public void ToParameters_FromPaginationParametersMessage_PageSizeEqualsMax_ReturnsMapped()
    {
        var message = new PaginationParametersMessage { Page = 1, PageSize = 1000 };
        var result = message.ToParameters();
        result.PageSize.Should().Be(1000);
    }

    [Fact]
    public void ToParameters_FromPaginationParametersMessage_WithOptionsMaxPageSize_Enforced()
    {
        var message = new PaginationParametersMessage { PageSize = 100 };
        var options = Substitute.For<IPaginationOptions>();
        options.MaxPageSize.Returns(50);
        
        Action act = () => message.ToParameters(options);
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*PageSize cannot exceed 50.*");
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_FirstNegative_ThrowsArgumentOutOfRangeException()
    {
        var message = new CursorPaginationParametersMessage { First = -1, Last = 0 };
        Action act = () => message.ToParameters();
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Cursor connection parameters 'First' and 'Last' cannot be negative.*");
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_LastNegative_ThrowsArgumentOutOfRangeException()
    {
        var message = new CursorPaginationParametersMessage { First = 0, Last = -1 };
        Action act = () => message.ToParameters();
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Cursor connection parameters 'First' and 'Last' cannot be negative.*");
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_FirstEqualsMax_ReturnsMapped()
    {
        var message = new CursorPaginationParametersMessage { First = 1000 };
        var result = message.ToParameters();
        result.First.Should().Be(1000);
    }
    
    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_LastEqualsMax_ReturnsMapped()
    {
        var message = new CursorPaginationParametersMessage { Last = 1000 };
        var result = message.ToParameters();
        result.Last.Should().Be(1000);
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_WithOptionsMaxPageSize_Enforced()
    {
        var message = new CursorPaginationParametersMessage { First = 100 };
        var options = Substitute.For<IPaginationOptions>();
        options.MaxPageSize.Returns(50);
        
        Action act = () => message.ToParameters(options);
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Cursor connection parameters 'First' and 'Last' cannot exceed 50.*");
    }

    [Fact]
    public void ToParameters_FromFilterParametersMessage_LengthEqualsMax_ReturnsMapped()
    {
        var message = new FilterParametersMessage { Value = new string('a', FilterParameters.AbsoluteMaxLength) };
        var result = message.ToParameters();
        result.Value.Should().Be(message.Value);
    }

    [Fact]
    public void ToParameters_FromCursorPaginationParametersMessage_WithOptionsMaxPageSize_LastEnforced()
    {
        var message = new CursorPaginationParametersMessage { Last = 100 };
        var options = Substitute.For<IPaginationOptions>();
        options.MaxPageSize.Returns(50);
        
        Action act = () => message.ToParameters(options);
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Cursor connection parameters 'First' and 'Last' cannot exceed 50.*");
    }
}



