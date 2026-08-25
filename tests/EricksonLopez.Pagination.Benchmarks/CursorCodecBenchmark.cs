// Copyright © Erickson Lopez. MIT License.
using System;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Benchmarks;

/// <summary>
/// Benchmark measuring cursor encoding and decoding latency, comparing Base64
/// vs HMAC-SHA256 authenticated cursors (with and without TTL).
/// </summary>
[MemoryDiagnoser]
public class CursorCodecBenchmark
{
    private const string SingleColumnPayload = "123456";
    private const string MultiColumnPayload = "2026-08-14T10:00:00.0000000Z|49900|Active";

    private readonly ICursorEncoder _base64 = HmacCursorEncoder.DevelopmentDefault;
    private readonly ICursorEncoder _hmac = new HmacCursorEncoder("this-is-a-32-byte-secret-key-for-hmac-benchmarks");
    private readonly ICursorEncoder _hmacWithTtl = new HmacCursorEncoder(
        "this-is-a-32-byte-secret-key-for-hmac-benchmarks",
        timeToLive: TimeSpan.FromMinutes(30));

    private string _encodedBase64Single = string.Empty;
    private string _encodedHmacSingle = string.Empty;
    private string _encodedBase64Multi = string.Empty;
    private string _encodedHmacMulti = string.Empty;
    private string _encodedHmacTtl = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _encodedBase64Single = _base64.Encode(SingleColumnPayload) ?? string.Empty;
        _encodedHmacSingle = _hmac.Encode(SingleColumnPayload) ?? string.Empty;
        _encodedBase64Multi = _base64.Encode(MultiColumnPayload) ?? string.Empty;
        _encodedHmacMulti = _hmac.Encode(MultiColumnPayload) ?? string.Empty;
        _encodedHmacTtl = _hmacWithTtl.Encode(SingleColumnPayload) ?? string.Empty;
    }

    [Benchmark(Baseline = true)]
    public string Base64_Encode_Single() => _base64.Encode(SingleColumnPayload) ?? string.Empty;

    [Benchmark]
    public string Hmac_Encode_Single() => _hmac.Encode(SingleColumnPayload) ?? string.Empty;

    [Benchmark]
    public string Base64_Decode_Single() => _base64.Decode(_encodedBase64Single) ?? string.Empty;

    [Benchmark]
    public string Hmac_Decode_Single() => _hmac.Decode(_encodedHmacSingle) ?? string.Empty;

    [Benchmark]
    public string Base64_Encode_Multi() => _base64.Encode(MultiColumnPayload) ?? string.Empty;

    [Benchmark]
    public string Hmac_Encode_Multi() => _hmac.Encode(MultiColumnPayload) ?? string.Empty;

    [Benchmark]
    public string Base64_Decode_Multi() => _base64.Decode(_encodedBase64Multi) ?? string.Empty;

    [Benchmark]
    public string Hmac_Decode_Multi() => _hmac.Decode(_encodedHmacMulti) ?? string.Empty;

    [Benchmark]
    public string Hmac_Encode_WithTTL() => _hmacWithTtl.Encode(SingleColumnPayload) ?? string.Empty;

    [Benchmark]
    public string Hmac_Decode_WithTTL() => _hmacWithTtl.Decode(_encodedHmacTtl) ?? string.Empty;
}
