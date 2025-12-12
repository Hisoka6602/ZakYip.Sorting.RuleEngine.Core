using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using ZakYip.Sorting.RuleEngine.DataSimulator.Configuration;
using ZakYip.Sorting.RuleEngine.DataSimulator.Generators;

namespace ZakYip.Sorting.RuleEngine.DataSimulator.Simulators;

/// <summary>
/// MQTT分拣机模拟器 - 通过MQTT发送包裹信号
/// MQTT Sorter simulator - Send parcel signals via MQTT
/// </summary>
public class MqttSorterSimulator : ISorterSimulator
{
    private readonly MqttConfig _config;
    private readonly DataGenerator _generator;
    private IMqttClient? _mqttClient;
    private bool _isConnected;

    public MqttSorterSimulator(MqttConfig config, DataGenerator generator)
    {
        _config = config;
        _generator = generator;
    }

    /// <summary>
    /// 连接到MQTT代理
    /// Connect to MQTT broker
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.BrokerHost, _config.BrokerPort)
                .WithClientId(_config.ClientId)
                .WithCleanSession();

            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                optionsBuilder = optionsBuilder.WithCredentials(_config.Username, _config.Password);
            }

            var options = optionsBuilder.Build();

            var result = await _mqttClient.ConnectAsync(options, CancellationToken.None).ConfigureAwait(false);
            _isConnected = result.ResultCode == MqttClientConnectResultCode.Success;

            if (_isConnected)
            {
                Console.WriteLine($"✓ 已连接到MQTT代理: {_config.BrokerHost}:{_config.BrokerPort}");
            }
            else
            {
                Console.WriteLine($"✗ 连接MQTT代理失败: {result.ReasonString}");
            }

            return _isConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 连接MQTT代理异常: {ex.Message}");
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// 断开连接
    /// Disconnect
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            if (_mqttClient != null && _isConnected)
            {
                await _mqttClient.DisconnectAsync().ConfigureAwait(false);
                _isConnected = false;
                Console.WriteLine("✓ 已断开MQTT代理连接");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 断开MQTT连接异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送单个包裹信号
    /// Send single parcel signal
    /// </summary>
    public async Task<SimulatorResult> SendParcelAsync(ParcelData parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (!_isConnected || _mqttClient == null)
        {
            return new SimulatorResult
            {
                Success = false,
                Message = "未连接到MQTT代理",
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

            var json = JsonSerializer.Serialize(data);
            var payload = Encoding.UTF8.GetBytes(json);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_config.PublishTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None).ConfigureAwait(false);
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
            throw new InvalidOperationException("未连接到MQTT代理");
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
        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _mqttClient?.Dispose();
        }
        catch
        {
            // Ignore disposal exceptions
        }
    }
}
