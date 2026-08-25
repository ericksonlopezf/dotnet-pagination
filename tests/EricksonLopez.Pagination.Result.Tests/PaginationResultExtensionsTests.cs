// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Result;
using EricksonLopez.Result;
using Xunit;

namespace EricksonLopez.Pagination.Result.Tests;

public class PaginationResultExtensionsTests
{
    [Fact]
    public async Task ExecuteResultAsync_WhenQueryIsNull_ThrowsArgumentNullException()
    {
        Func<Task> act = async () => await PaginationResultExtensions.ExecuteResultAsync<string>((Func<Task<string>>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteResultAsync_WhenQuerySucceeds_ReturnsSuccessResult()
    {
        var expectedValue = "paged-data-sample";

        var result = await PaginationResultExtensions.ExecuteResultAsync(() => Task.FromResult(expectedValue));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedValue);
    }

    [Fact]
    public async Task ExecuteResultAsync_WhenExpiredPaginationCursorExceptionThrown_ReturnsExpiredCursorFailure()
    {
        var expiredAt = DateTimeOffset.UtcNow;
        
        var result = await PaginationResultExtensions.ExecuteResultAsync<string>(
            () => throw new ExpiredPaginationCursorException("opaque-cursor", expiredAt));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaginationErrors.ExpiredCursor);
        result.Error.Code.Should().Be("Pagination.ExpiredCursor");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ExecuteResultAsync_WhenReplayedPaginationCursorExceptionThrown_ReturnsReplayedCursorFailure()
    {
        var result = await PaginationResultExtensions.ExecuteResultAsync<string>(
            () => throw new ReplayedPaginationCursorException("opaque-cursor"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaginationErrors.ReplayedCursor);
        result.Error.Code.Should().Be("Pagination.ReplayedCursor");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ExecuteResultAsync_WhenInvalidPaginationCursorExceptionThrown_ReturnsInvalidCursorFailure()
    {
        var result = await PaginationResultExtensions.ExecuteResultAsync<string>(
            () => throw new InvalidPaginationCursorException("Invalid cursor format", "malformed-cursor"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaginationErrors.InvalidCursor);
        result.Error.Code.Should().Be("Pagination.InvalidCursor");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ExecuteResultAsync_WhenGenericExceptionThrown_ReturnsFailureResultWithError()
    {
        const string errorMessage = "Unexpected database connection failure";

        var result = await PaginationResultExtensions.ExecuteResultAsync<string>(
            () => throw new InvalidOperationException(errorMessage));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Pagination.Error");
        result.Error.Description.Should().Be(errorMessage);
        result.Error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void PaginationErrors_StandardErrors_HaveExpectedCodesAndDescriptions()
    {
        PaginationErrors.InvalidCursor.Code.Should().Be("Pagination.InvalidCursor");
        PaginationErrors.InvalidCursor.Description.Should().Be("The provided pagination cursor is invalid or malformed.");
        PaginationErrors.InvalidCursor.Type.Should().Be(ErrorType.Validation);

        PaginationErrors.ExpiredCursor.Code.Should().Be("Pagination.ExpiredCursor");
        PaginationErrors.ExpiredCursor.Description.Should().Be("The provided pagination cursor has expired.");
        PaginationErrors.ExpiredCursor.Type.Should().Be(ErrorType.Validation);

        PaginationErrors.ReplayedCursor.Code.Should().Be("Pagination.ReplayedCursor");
        PaginationErrors.ReplayedCursor.Description.Should().Be("The provided pagination cursor has already been used and cannot be replayed.");
        PaginationErrors.ReplayedCursor.Type.Should().Be(ErrorType.Validation);
    }
}
