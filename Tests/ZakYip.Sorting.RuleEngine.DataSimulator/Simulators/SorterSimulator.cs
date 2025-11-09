using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ZakYip.Sorting.RuleEngine.DataSimulator.Configuration;
using ZakYip.Sorting.RuleEngine.DataSimulator.Generators;

namespace ZakYip.Sorting.RuleEngine.DataSimulator.Simulators;

/// <summary>
/// 分拣机模拟器 - 通过HTTP API发送包裹信号
/// Sorter simulator - Send parcel signals via HTTP API
/// </summary>
public class SorterSimulator
{
    private readonly HttpClient _httpClient;
    private readonly DataGenerator _generator;
    private readonly SimulatorConfig _config;

    public SorterSimulator(SimulatorConfig config, DataGenerator generator)
    {
        _config = config;
        _generator = generator;
        _httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(config.HttpApiUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// 发送单个包裹信号
    /// Send single parcel signal
    /// </summary>
    public async Task<SimulatorResult> SendParcelAsync(ParcelData parcel)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var request = new
            {
                parcelId = parcel.ParcelId,
                cartNumber = parcel.CartNumber,
                barcode = parcel.Barcode
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/sortingmachine/create-parcel", content);
            var result = await response.Content.ReadAsStringAsync();

            sw.Stop();

            return new SimulatorResult
            {
                Success = response.IsSuccessStatusCode,
                Message = result,
                ElapsedMs = sw.ElapsedMilliseconds,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new SimulatorResult
            {
                Success = false,
                Message = ex.Message,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// 批量发送包裹信号
    /// Send batch of parcel signals
    /// </summary>
    public async Task<BatchResult> SendBatchAsync(int count, int delayMs = 0)
    {
        var results = new List<SimulatorResult>();
        var totalSw = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            var parcel = _generator.GenerateParcel();
            var result = await SendParcelAsync(parcel);
            results.Add(result);

            if (delayMs > 0 && i < count - 1)
            {
                await Task.Delay(delayMs);
            }
        }

        totalSw.Stop();

        return new BatchResult
        {
            TotalCount = count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            TotalTimeMs = totalSw.ElapsedMilliseconds,
            AverageLatencyMs = results.Average(r => r.ElapsedMs),
            MinLatencyMs = results.Min(r => r.ElapsedMs),
            MaxLatencyMs = results.Max(r => r.ElapsedMs),
            Results = results
        };
    }

    /// <summary>
    /// 压力测试模式 - 持续发送指定速率的包裹信号
    /// Stress test mode - Continuously send parcel signals at specified rate
    /// </summary>
    public async Task<StressTestResult> RunStressTestAsync(
        int durationSeconds,
        int ratePerSecond,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SimulatorResult>();
        var startTime = DateTime.Now;
        var endTime = startTime.AddSeconds(durationSeconds);
        var intervalMs = 1000.0 / ratePerSecond;
        var successCount = 0;
        var failureCount = 0;

        Console.WriteLine($"开始压力测试: {ratePerSecond} 包裹/秒, 持续 {durationSeconds} 秒");
        Console.WriteLine($"预期总数: {ratePerSecond * durationSeconds} 包裹");

        var sw = Stopwatch.StartNew();
        long nextSendTime = 0;

        while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
        {
            var currentTime = sw.ElapsedMilliseconds;
            
            if (currentTime >= nextSendTime)
            {
                var parcel = _generator.GenerateParcel();
                var sendTask = SendParcelAsync(parcel);
                
                // Don't wait for the result to maintain rate
                _ = sendTask.ContinueWith(t =>
                {
                    if (t.Result.Success)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref failureCount);
                    
                    lock (results)
                    {
                        results.Add(t.Result);
                    }
                }, cancellationToken);

                nextSendTime += (long)intervalMs;
            }

            // Small delay to prevent CPU spinning
            await Task.Delay(1, cancellationToken);
        }

        sw.Stop();

        // Wait a bit for remaining requests to complete
        await Task.Delay(2000, cancellationToken);

        var actualDuration = sw.Elapsed.TotalSeconds;

        return new StressTestResult
        {
            DurationSeconds = actualDuration,
            TargetRate = ratePerSecond,
            ActualRate = results.Count / actualDuration,
            TotalSent = results.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            AverageLatencyMs = results.Any() ? results.Average(r => r.ElapsedMs) : 0,
            P50LatencyMs = CalculatePercentile(results, 50),
            P95LatencyMs = CalculatePercentile(results, 95),
            P99LatencyMs = CalculatePercentile(results, 99)
        };
    }

    private double CalculatePercentile(List<SimulatorResult> results, int percentile)
    {
        if (!results.Any()) return 0;

        var sorted = results.OrderBy(r => r.ElapsedMs).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));
        return sorted[index].ElapsedMs;
    }
}

/// <summary>
/// 模拟器结果
/// Simulator result
/// </summary>
public class SimulatorResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public int StatusCode { get; set; }
}

/// <summary>
/// 批量测试结果
/// Batch test result
/// </summary>
public class BatchResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long TotalTimeMs { get; set; }
    public double AverageLatencyMs { get; set; }
    public long MinLatencyMs { get; set; }
    public long MaxLatencyMs { get; set; }
    public List<SimulatorResult> Results { get; set; } = new();
}

/// <summary>
/// 压力测试结果
/// Stress test result
/// </summary>
public class StressTestResult
{
    public double DurationSeconds { get; set; }
    public int TargetRate { get; set; }
    public double ActualRate { get; set; }
    public int TotalSent { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P50LatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
}
