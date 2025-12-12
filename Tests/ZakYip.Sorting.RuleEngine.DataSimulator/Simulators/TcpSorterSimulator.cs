using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.DataSimulator.Configuration;
using ZakYip.Sorting.RuleEngine.DataSimulator.Generators;

namespace ZakYip.Sorting.RuleEngine.DataSimulator.Simulators;

/// <summary>
/// TCP分拣机模拟器 - 通过TCP发送包裹信号
/// TCP Sorter simulator - Send parcel signals via TCP
/// </summary>
public class TcpSorterSimulator : ISorterSimulator
{
    private readonly TcpConfig _config;
    private readonly DataGenerator _generator;
    private TcpClient? _tcpClient;
    private bool _isConnected;

    public TcpSorterSimulator(TcpConfig config, DataGenerator generator)
    {
        _config = config;
        _generator = generator;
    }

    /// <summary>
    /// 连接到TCP服务器
    /// Connect to TCP server
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            
            await _tcpClient.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost(new IPHost($"{_config.Host}:{_config.Port}"))
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"))).ConfigureAwait(false);

            await _tcpClient.ConnectAsync().ConfigureAwait(false);
            _isConnected = true;
            
            Console.WriteLine($"✓ 已连接到分拣机TCP服务器: {_config.Host}:{_config.Port}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 连接分拣机TCP服务器失败: {ex.Message}");
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// 断开连接
    /// Disconnect
    /// </summary>
    public void Disconnect()
    {
        if (_tcpClient != null)
        {
            _tcpClient.Close();
            _isConnected = false;
            Console.WriteLine("✓ 已断开分拣机TCP服务器连接");
        }
    }

    /// <summary>
    /// 发送单个包裹信号
    /// Send single parcel signal
    /// </summary>
    public async Task<SimulatorResult> SendParcelAsync(ParcelData parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (!_isConnected || _tcpClient == null)
        {
            return new SimulatorResult
            {
                Success = false,
                Message = "未连接到分拣机TCP服务器",
                ElapsedMs = 0
            };
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var data = new
            {
                parcelId = parcel.ParcelId,
                cartNumber = parcel.CartNumber,
                barcode = parcel.Barcode,
                timestamp = DateTime.Now
            };

            var json = JsonSerializer.Serialize(data) + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);

            await _tcpClient.SendAsync(bytes).ConfigureAwait(false);
            sw.Stop();

            return new SimulatorResult
            {
                Success = true,
                Message = "包裹信号发送成功",
                ElapsedMs = sw.ElapsedMilliseconds
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
            var result = await SendParcelAsync(parcel).ConfigureAwait(false);
            results.Add(result);

            if (delayMs > 0 && i < count - 1)
            {
                await Task.Delay(delayMs).ConfigureAwait(false);
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
        if (!_isConnected)
        {
            throw new InvalidOperationException("未连接到分拣机TCP服务器");
        }

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
            AverageLatencyMs = results.Count > 0 ? results.Average(r => r.ElapsedMs) : 0,
            P50LatencyMs = CalculatePercentile(results, 50),
            P95LatencyMs = CalculatePercentile(results, 95),
            P99LatencyMs = CalculatePercentile(results, 99)
        };
    }

    private static double CalculatePercentile(List<SimulatorResult> results, int percentile)
    {
        if (results.Count == 0) return 0;

        var sorted = results.OrderBy(r => r.ElapsedMs).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));
        return sorted[index].ElapsedMs;
    }

    public void Dispose()
    {
        Disconnect();
    }
}
