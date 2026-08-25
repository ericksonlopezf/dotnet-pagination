// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance);
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [MemoryDiagnoser]
    public class FilterExpressionBenchmarks
    {
        private FilterParameters _filterParameters = new();

        [GlobalSetup]
        public void Setup()
        {
            _filterParameters = new FilterParameters
            {
                Value = "Name~=John,Age>=18"
            };
            
            // JIT warm-up
            _ = FilterExpression.Build<TestEntity>(_filterParameters, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore);
        }

        [Benchmark]
        public object BuildExpression_Warm()
        {
            return FilterExpression.Build<TestEntity>(_filterParameters, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore)!;
        }

        private int _counter = 0;

        [Benchmark]
        public object BuildExpression_Cold()
        {
            var coldParams = new FilterParameters { Value = $"Name~=John{_counter++},Age>={_counter}" };
            return FilterExpression.Build<TestEntity>(coldParams, unknownFieldBehavior: FilterUnknownFieldBehavior.Ignore)!;
        }

    }

    [MemoryDiagnoser]
    public class CursorEncoderBenchmarks
    {
        private const string CursorValue = "1234567890|abcdefghijklmnopqrstuvwxyz";
        private string _encodedBase64 = string.Empty;
        private string _encodedHmac = string.Empty;
        private readonly ICursorEncoder _base64 = HmacCursorEncoder.DevelopmentDefault;
        private readonly ICursorEncoder _hmac = new HmacCursorEncoder("this-is-a-very-secret-key-that-is-long-enough");

        [GlobalSetup]
        public void Setup()
        {
            _encodedBase64 = _base64.Encode(CursorValue)!;
            _encodedHmac = _hmac.Encode(CursorValue)!;
        }

        [Benchmark]
        public string EncodeBase64() => _base64.Encode(CursorValue)!;

        [Benchmark]
        public string DecodeBase64() => _base64.Decode(_encodedBase64)!;

        [Benchmark]
        public string EncodeHmac() => _hmac.Encode(CursorValue)!;

        [Benchmark]
        public string DecodeHmac() => _hmac.Decode(_encodedHmac)!;
    }
}

