// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Pagination.Elasticsearch;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Pagination.Elasticsearch.Tests;

public class ElasticsearchCursorPaginationTests
{
    public class TestDoc
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    [Fact]
    public void EncodeSort_WithNullOrEmpty_ReturnsNull()
    {
        ElasticsearchCursorHelper.EncodeSort(null).Should().BeNull();
        ElasticsearchCursorHelper.EncodeSort(Array.Empty<FieldValue>()).Should().BeNull();
    }

    [Fact]
    public void EncodeSort_WithCustomEncoder_UsesCustomEncoder()
    {
        var mockEncoder = Substitute.For<ICursorEncoder>();
        mockEncoder.Encode(Arg.Any<string>()).Returns("CUSTOM_ENCODED");

        var sortFields = new List<FieldValue> { FieldValue.String("custom_field") };
        var result = ElasticsearchCursorHelper.EncodeSort(sortFields, mockEncoder);

        result.Should().Be("CUSTOM_ENCODED");
        mockEncoder.Received(1).Encode(Arg.Is<string>(s => s == "custom_field"));
    }

    [Fact]
    public void DecodeSort_WithNullOrWhitespace_ReturnsNull_AndDoesNotCallEncoder()
    {
        var mockEncoder = Substitute.For<ICursorEncoder>();
        mockEncoder.Decode(Arg.Any<string>()).Returns("should_not_be_called");

        ElasticsearchCursorHelper.DecodeSort(null, mockEncoder).Should().BeNull();
        ElasticsearchCursorHelper.DecodeSort("", mockEncoder).Should().BeNull();
        ElasticsearchCursorHelper.DecodeSort("   ", mockEncoder).Should().BeNull();

        mockEncoder.DidNotReceive().Decode(Arg.Any<string>());
    }

    [Fact]
    public void DecodeSort_WhenEncoderReturnsNullOrEmpty_ReturnsNull()
    {
        var mockEncoder = Substitute.For<ICursorEncoder>();
        mockEncoder.Decode(Arg.Any<string>()).Returns((string?)null);

        ElasticsearchCursorHelper.DecodeSort("some_cursor", mockEncoder).Should().BeNull();

        mockEncoder.Decode(Arg.Any<string>()).Returns("");
        ElasticsearchCursorHelper.DecodeSort("some_cursor", mockEncoder).Should().BeNull();
    }

    [Fact]
    public void DecodeSort_ParsesLong_Double_Bool_And_String_Fields()
    {
        var sortFields = new List<FieldValue>
        {
            FieldValue.Long(1234567890123L),
            FieldValue.Double(42.58),
            FieldValue.Boolean(true),
            FieldValue.Boolean(false),
            FieldValue.String("alphanumeric_token")
        };

        string? cursor = ElasticsearchCursorHelper.EncodeSort(sortFields);
        cursor.Should().NotBeNullOrWhiteSpace();

        var decoded = ElasticsearchCursorHelper.DecodeSort(cursor);
        decoded.Should().NotBeNull();
        decoded!.Length.Should().Be(5);
        decoded[0].ToString().Should().Be("1234567890123");
        decoded[1].ToString().Should().Be("42.58");
        decoded[2].ToString().Should().Be(true.ToString());
        decoded[3].ToString().Should().Be(false.ToString());
        decoded[4].ToString().Should().Be("alphanumeric_token");
    }

    [Fact]
    public void DecodeSort_WithCustomEncoder_UsesCustomEncoder()
    {
        var mockEncoder = Substitute.For<ICursorEncoder>();
        mockEncoder.Decode("MY_RAW_CURSOR").Returns("9999\u001FTrue\u001Fcustom");

        var decoded = ElasticsearchCursorHelper.DecodeSort("MY_RAW_CURSOR", mockEncoder);
        decoded.Should().NotBeNull();
        decoded!.Length.Should().Be(3);
        decoded[0].ToString().Should().Be("9999");
        decoded[1].ToString().Should().Be("True");
        decoded[2].ToString().Should().Be("custom");
    }

    [Fact]
    public void ApplyCursorPagination_NullDescriptor_ThrowsArgumentNullException()
    {
        SearchRequestDescriptor<TestDoc>? nullDescriptor = null;
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => nullDescriptor!.ApplyCursorPagination(parameters);
        act.Should().Throw<ArgumentNullException>().WithParameterName("descriptor");
    }

    [Fact]
    public void ApplyCursorPagination_WithValidParametersAndAfter_ConfiguresSizeAndSearchAfter()
    {
        var descriptor = new SearchRequestDescriptor<TestDoc>();
        var sortFields = new[] { FieldValue.Long(100), FieldValue.String("tag") };
        string? cursor = ElasticsearchCursorHelper.EncodeSort(sortFields);

        var parameters = new CursorPaginationParameters
        {
            First = 15,
            After = cursor
        };

        var result = descriptor.ApplyCursorPagination(parameters);
        result.Should().BeSameAs(descriptor);

        int? size = GetDescriptorSize(descriptor);
        size.Should().Be(16);

        var searchAfter = GetDescriptorSearchAfter(descriptor);
        searchAfter.Should().NotBeNull();
        searchAfter!.Count.Should().Be(2);
    }

    [Fact]
    public void ApplyCursorPagination_WithoutAfter_ConfiguresSizeOnly()
    {
        var descriptor = new SearchRequestDescriptor<TestDoc>();
        var parameters = new CursorPaginationParameters
        {
            First = 20,
            After = null
        };

        var result = descriptor.ApplyCursorPagination(parameters);
        result.Should().BeSameAs(descriptor);

        int? size = GetDescriptorSize(descriptor);
        size.Should().Be(21);

        var searchAfter = GetDescriptorSearchAfter(descriptor);
        searchAfter.Should().BeNull();
    }

    [Fact]
    public void ApplyCursorPagination_WithInvalidOrEmptyAfter_DoesNotConfigureSearchAfter()
    {
        var descriptor1 = new SearchRequestDescriptor<TestDoc>();
        var parameters1 = new CursorPaginationParameters { First = 10, After = "   " };

        descriptor1.ApplyCursorPagination(parameters1);
        GetDescriptorSearchAfter(descriptor1).Should().BeNull();

        var mockEncoder = Substitute.For<ICursorEncoder>();
        mockEncoder.Decode(Arg.Any<string>()).Returns((string?)null);

        var descriptor2 = new SearchRequestDescriptor<TestDoc>();
        var parameters2 = new CursorPaginationParameters { First = 10, After = "invalid_token" };

        descriptor2.ApplyCursorPagination(parameters2, mockEncoder);
        GetDescriptorSearchAfter(descriptor2).Should().BeNull();
    }

    [Fact]
    public void ToCursorPagedList_NullResponse_ThrowsArgumentNullException()
    {
        SearchResponse<TestDoc>? nullResponse = null;
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => nullResponse!.ToCursorPagedList(parameters);
        act.Should().Throw<ArgumentNullException>().WithParameterName("response");
    }

    [Fact]
    public void ToCursorPagedList_EmptyHits_ReturnsEmptyPagedListWithNullCursors()
    {
        var response = CreateSearchResponse<TestDoc>(new List<Hit<TestDoc>>());
        var parameters = new CursorPaginationParameters { First = 10, After = null };

        var pagedList = response.ToCursorPagedList(parameters);

        pagedList.Should().BeEmpty();
        pagedList.StartCursor.Should().BeNull();
        pagedList.EndCursor.Should().BeNull();
        pagedList.HasPreviousPage.Should().BeFalse();
        pagedList.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToCursorPagedList_EmptyHits_WithAfter_SetsHasPreviousPageTrue()
    {
        var response = CreateSearchResponse<TestDoc>(new List<Hit<TestDoc>>());
        var parameters = new CursorPaginationParameters { First = 10, After = "previous_cursor" };

        var pagedList = response.ToCursorPagedList(parameters);

        pagedList.Should().BeEmpty();
        pagedList.HasPreviousPage.Should().BeTrue();
        pagedList.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToCursorPagedList_FewerHitsThanPageSize_ReturnsAllHitsWithoutNextPage()
    {
        var hits = new List<Hit<TestDoc>>
        {
            CreateHit(new TestDoc { Id = 1, Title = "Doc 1" }, new FieldValue[] { FieldValue.Long(1) }),
            CreateHit(new TestDoc { Id = 2, Title = "Doc 2" }, new FieldValue[] { FieldValue.Long(2) })
        };

        var response = CreateSearchResponse(hits);
        var parameters = new CursorPaginationParameters { First = 5, After = null };

        var pagedList = response.ToCursorPagedList(parameters);

        pagedList.Count.Should().Be(2);
        pagedList[0].Id.Should().Be(1);
        pagedList[1].Id.Should().Be(2);
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeFalse();
        pagedList.StartCursor.Should().NotBeNull();
        pagedList.EndCursor.Should().NotBeNull();
    }

    [Fact]
    public void ToCursorPagedList_ExactHitsEqualToPageSize_HasNextPageIsFalse()
    {
        var hits = new List<Hit<TestDoc>>
        {
            CreateHit(new TestDoc { Id = 1, Title = "Doc 1" }, new FieldValue[] { FieldValue.Long(1) }),
            CreateHit(new TestDoc { Id = 2, Title = "Doc 2" }, new FieldValue[] { FieldValue.Long(2) })
        };

        var response = CreateSearchResponse(hits);
        var parameters = new CursorPaginationParameters { First = 2, After = null };

        var pagedList = response.ToCursorPagedList(parameters);

        pagedList.Count.Should().Be(2);
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToCursorPagedList_ExactPageSizePlusOne_TrimsExtraItemAndSetsHasNextPageTrue()
    {
        var hits = new List<Hit<TestDoc>>
        {
            CreateHit(new TestDoc { Id = 1, Title = "Doc 1" }, new FieldValue[] { FieldValue.Long(1) }),
            CreateHit(new TestDoc { Id = 2, Title = "Doc 2" }, new FieldValue[] { FieldValue.Long(2) }),
            CreateHit(new TestDoc { Id = 3, Title = "Doc 3" }, new FieldValue[] { FieldValue.Long(3) })
        };

        var response = CreateSearchResponse(hits);
        var parameters = new CursorPaginationParameters { First = 2, After = "cur_start" };

        var pagedList = response.ToCursorPagedList(parameters);

        pagedList.Count.Should().Be(2);
        pagedList[0].Id.Should().Be(1);
        pagedList[1].Id.Should().Be(2);
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeTrue();

        var startDecoded = ElasticsearchCursorHelper.DecodeSort(pagedList.StartCursor);
        startDecoded.Should().NotBeNull();
        startDecoded![0].ToString().Should().Be("1");

        var endDecoded = ElasticsearchCursorHelper.DecodeSort(pagedList.EndCursor);
        endDecoded.Should().NotBeNull();
        endDecoded![0].ToString().Should().Be("2");
    }

    [Fact]
    public void ToCursorPagedList_WithCustomEncoder_EncodesCursorsWithCustomEncoder()
    {
        var mockEncoder = Substitute.For<ICursorEncoder>();
        mockEncoder.Encode(Arg.Any<string>()).Returns("CUSTOM_START", "CUSTOM_END");

        var hits = new List<Hit<TestDoc>>
        {
            CreateHit(new TestDoc { Id = 10, Title = "Doc 10" }, new FieldValue[] { FieldValue.Long(10) }),
            CreateHit(new TestDoc { Id = 20, Title = "Doc 20" }, new FieldValue[] { FieldValue.Long(20) })
        };

        var response = CreateSearchResponse(hits);
        var parameters = new CursorPaginationParameters { First = 5 };

        var pagedList = response.ToCursorPagedList(parameters, mockEncoder);

        pagedList.StartCursor.Should().Be("CUSTOM_START");
        pagedList.EndCursor.Should().Be("CUSTOM_END");
    }

private static int? GetDescriptorSize<T>(SearchRequestDescriptor<T> descriptor)
    {
        var client = new ElasticsearchClient();
        var stream = new System.IO.MemoryStream();
        client.RequestResponseSerializer.Serialize(descriptor, stream);
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var jdoc = System.Text.Json.JsonDocument.Parse(json);
        if (jdoc.RootElement.TryGetProperty("size", out var sizeProp))
            return sizeProp.GetInt32();
        return null;
    }

    private static IReadOnlyCollection<FieldValue>? GetDescriptorSearchAfter<T>(SearchRequestDescriptor<T> descriptor)
    {
        var client = new ElasticsearchClient();
        var stream = new System.IO.MemoryStream();
        client.RequestResponseSerializer.Serialize(descriptor, stream);
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var jdoc = System.Text.Json.JsonDocument.Parse(json);
        if (jdoc.RootElement.TryGetProperty("search_after", out var searchAfterProp))
        {
            var arr = new List<FieldValue>();
            foreach (var el in searchAfterProp.EnumerateArray())
            {
                if (el.ValueKind == System.Text.Json.JsonValueKind.Number)
                    arr.Add(FieldValue.Long(el.GetInt64()));
                else if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                    arr.Add(FieldValue.String(el.GetString()!));
            }
            return arr;
        }
        return null;
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    private static SearchResponse<T> CreateSearchResponse<T>(IReadOnlyCollection<Hit<T>> hits)
    {
        var hitsJson = System.Text.Json.JsonSerializer.Serialize(hits.Select(h => new { _id = "1", _source = h.Source, sort = h.Sort }), JsonOptions);
        var json = "{\"hits\":{\"hits\":" + hitsJson + "}}";
        var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var client = new ElasticsearchClient();
        return client.RequestResponseSerializer.Deserialize<SearchResponse<T>>(stream)!;
    }

    private static Hit<T> CreateHit<T>(T source, IReadOnlyCollection<FieldValue> sort)
    {
        var sourceJson = System.Text.Json.JsonSerializer.Serialize(source, JsonOptions);
        var sortJson = System.Text.Json.JsonSerializer.Serialize(sort, JsonOptions);
        var json = "{\"_id\":\"1\",\"_source\":" + sourceJson + ",\"sort\":" + sortJson + "}";
        var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var client = new ElasticsearchClient();
        return client.RequestResponseSerializer.Deserialize<Hit<T>>(stream)!;
    }
}