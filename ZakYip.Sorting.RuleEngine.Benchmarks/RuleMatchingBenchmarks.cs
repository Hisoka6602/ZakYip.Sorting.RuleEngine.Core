using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Benchmarks;

/// <summary>
/// 规则匹配性能基准测试
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RuleMatchingBenchmarks
{
    private BarcodeRegexMatcher _barcodeMatch = null!;
    private WeightMatcher _weightMatcher = null!;
    private VolumeMatcher _volumeMatcher = null!;
    private DwsData _dwsData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _barcodeMatch = new BarcodeRegexMatcher();
        _weightMatcher = new WeightMatcher();
        _volumeMatcher = new VolumeMatcher();
        
        _dwsData = new DwsData
        {
            Barcode = "SF123456",
            Weight = 250.0m,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };
    }

    [Benchmark]
    public bool BarcodeStartsWith()
    {
        return _barcodeMatch.Evaluate("STARTSWITH:SF", "SF123456789");
    }

    [Benchmark]
    public bool BarcodeContains()
    {
        return _barcodeMatch.Evaluate("CONTAINS:ABC", "TEST-ABC-123");
    }

    [Benchmark]
    public bool BarcodeRegex()
    {
        return _barcodeMatch.Evaluate("REGEX:^SF\\d{6}$", "SF123456");
    }

    [Benchmark]
    public bool WeightSimple()
    {
        return _weightMatcher.Evaluate("Weight > 100", 150.0m);
    }

    [Benchmark]
    public bool WeightComplex()
    {
        return _weightMatcher.Evaluate("Weight > 100 and Weight < 500", 250.0m);
    }

    [Benchmark]
    public bool VolumeSimple()
    {
        return _volumeMatcher.Evaluate("Volume > 1000", _dwsData);
    }

    [Benchmark]
    public bool VolumeComplex()
    {
        return _volumeMatcher.Evaluate("Length > 200 and Width > 100 or Height > 100", _dwsData);
    }
}
