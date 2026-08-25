// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.Cosmos.Tests;

public class CosmosPaginationExtensionsTests
{
    [Fact]
    public async Task ToCursorPagedListAsync_WhenBackwardPaginationRequestedWithLast_ThrowsNotSupportedException()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var query = new QueryDefinition("SELECT * FROM c");
        var parameters = new CursorPaginationParameters { Last = 10 };

        // Act
        var act = () => container.ToCursorPagedListAsync<object>(query, parameters);

        // Assert
        var ex = await act.Should().ThrowAsync<NotSupportedException>();
        ex.WithMessage("Backward pagination (Last/Before) is not supported by the Cosmos DB provider. Cosmos DB continuation tokens are forward-only. Use forward pagination (First/After) only.");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenBackwardPaginationRequestedWithBefore_ThrowsNotSupportedException()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var query = new QueryDefinition("SELECT * FROM c");
        var parameters = new CursorPaginationParameters { Before = "some_cursor" };

        // Act
        var act = () => container.ToCursorPagedListAsync<object>(query, parameters);

        // Assert
        var ex = await act.Should().ThrowAsync<NotSupportedException>();
        ex.WithMessage("Backward pagination (Last/Before) is not supported by the Cosmos DB provider. Cosmos DB continuation tokens are forward-only. Use forward pagination (First/After) only.");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenFirstExceedsMaxPageSize_CapsAtMaxPageSize()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var query = new QueryDefinition("SELECT * FROM c");
        var parameters = new CursorPaginationParameters { First = 100 };
        var maxPageSize = 50; // maxPageSize < First

        var feedIterator = Substitute.For<FeedIterator<string>>();
        var response = Substitute.For<FeedResponse<string>>();

        response.GetEnumerator().Returns(new List<string>().GetEnumerator());
        response.ContinuationToken.Returns((string?)null);

        feedIterator.HasMoreResults.Returns(true);
        feedIterator.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));

        container.GetItemQueryIterator<string>(
            queryDefinition: Arg.Any<QueryDefinition>(),
            continuationToken: Arg.Any<string>(),
            requestOptions: Arg.Is<QueryRequestOptions>(r => r.MaxItemCount == 50))
            .Returns(feedIterator);

        // Act
        await container.ToCursorPagedListAsync<string>(query, parameters, maxPageSize);
        
        // Assert is handled by NSubstitute Arg.Is matching the requestOptions.MaxItemCount
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenCursorIsInvalid_ThrowsInvalidPaginationCursorException()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var feedIterator = Substitute.For<FeedIterator<object>>();
        container.GetItemQueryIterator<object>(Arg.Any<QueryDefinition>(), Arg.Any<string>(), Arg.Any<QueryRequestOptions>()).Returns(feedIterator);

        var query = new QueryDefinition("SELECT * FROM c");
        var parameters = new CursorPaginationParameters { After = "invalid!!base64==" };

        // Act
        var act = () => container.ToCursorPagedListAsync<object>(query, parameters);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidPaginationCursorException>();
        ex.Which.OpaqueCursor.Should().Be("invalid!!base64==");
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenForwardCursorProvided_ReturnsPagedListWithForwardToken()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var query = new QueryDefinition("SELECT * FROM c");
        
        // Let's create an original token, e.g., "token123"
        var rawToken = "token123";
        var encodedToken = HmacCursorEncoder.DevelopmentDefault.Encode(rawToken);
        var parameters = new CursorPaginationParameters { After = encodedToken, First = 5 };
        var maxPageSize = 10;

        var feedIterator = Substitute.For<FeedIterator<string>>();
        var response = Substitute.For<FeedResponse<string>>();
        var nextRawToken = "token456";
        var expectedNextEncodedToken = HmacCursorEncoder.DevelopmentDefault.Encode(nextRawToken);

        // Setup the response
        response.GetEnumerator().Returns(new List<string> { "item1", "item2" }.GetEnumerator());
        response.ContinuationToken.Returns(nextRawToken);

        // Setup the iterator
        feedIterator.HasMoreResults.Returns(true);
        feedIterator.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));

        container.GetItemQueryIterator<string>(
            queryDefinition: Arg.Is<QueryDefinition>(q => q.QueryText == query.QueryText),
            continuationToken: rawToken,
            requestOptions: Arg.Is<QueryRequestOptions>(r => r.MaxItemCount == 5))
            .Returns(feedIterator);

        // Act
        var result = await container.ToCursorPagedListAsync<string>(query, parameters, maxPageSize);

        // Assert
        result.Should().NotBeNull();
        var expectedItems = new[] { "item1", "item2" };
        result.Should().BeEquivalentTo(expectedItems);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.EndCursor.Should().Be(expectedNextEncodedToken);
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenNoMoreResults_ReturnsPagedListWithNoNextToken()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var query = new QueryDefinition("SELECT * FROM c");
        var parameters = new CursorPaginationParameters { First = 10 };

        var feedIterator = Substitute.For<FeedIterator<string>>();
        var response = Substitute.For<FeedResponse<string>>();

        // Setup the response
        response.GetEnumerator().Returns(new List<string> { "item1" }.GetEnumerator());
        response.ContinuationToken.Returns((string?)null); // No more pages

        // Setup the iterator
        feedIterator.HasMoreResults.Returns(true);
        feedIterator.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));

        container.GetItemQueryIterator<string>(
            queryDefinition: Arg.Any<QueryDefinition>(),
            continuationToken: Arg.Any<string>(),
            requestOptions: Arg.Any<QueryRequestOptions>())
            .Returns(feedIterator);

        // Act
        var result = await container.ToCursorPagedListAsync<string>(query, parameters);

        // Assert
        result.Should().NotBeNull();
        var expectedItems = new[] { "item1" };
        result.Should().BeEquivalentTo(expectedItems);
        result.HasNextPage.Should().BeFalse();
        result.EndCursor.Should().BeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenIteratorHasNoResults_ReturnsEmptyList()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var query = new QueryDefinition("SELECT * FROM c");
        var parameters = new CursorPaginationParameters { First = 10 };

        var feedIterator = Substitute.For<FeedIterator<string>>();

        // Setup the iterator to immediately say it has no results
        feedIterator.HasMoreResults.Returns(false);

        container.GetItemQueryIterator<string>(
            queryDefinition: Arg.Any<QueryDefinition>(),
            continuationToken: Arg.Any<string>(),
            requestOptions: Arg.Any<QueryRequestOptions>())
            .Returns(feedIterator);

        // Act
        var result = await container.ToCursorPagedListAsync<string>(query, parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        result.HasNextPage.Should().BeFalse();
        result.EndCursor.Should().BeNull();
    }

    [Fact]
    public async Task ToCursorPagedListAsync_WhenFirstIsMissing_UsesDefaultPageSize()
    {
        // Arrange
        var container = Substitute.For<Container>();
        var query = new QueryDefinition("SELECT * FROM c");
        // No First or Last provided
        var parameters = new CursorPaginationParameters(); 

        var feedIterator = Substitute.For<FeedIterator<string>>();
        var response = Substitute.For<FeedResponse<string>>();

        response.GetEnumerator().Returns(new List<string>().GetEnumerator());
        response.ContinuationToken.Returns((string?)null);

        feedIterator.HasMoreResults.Returns(true);
        feedIterator.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));

        container.GetItemQueryIterator<string>(
            queryDefinition: Arg.Any<QueryDefinition>(),
            continuationToken: Arg.Any<string>(),
            // The default fallback page size should be used (10)
            requestOptions: Arg.Is<QueryRequestOptions>(r => r.MaxItemCount == 10))
            .Returns(feedIterator);

        // Act
        await container.ToCursorPagedListAsync<string>(query, parameters);
        
        // Assert is handled by NSubstitute Arg.Is matching the requestOptions.MaxItemCount
    }
}
