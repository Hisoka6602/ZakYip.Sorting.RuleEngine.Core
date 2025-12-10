using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 响应式包裹处理服务 - 使用Rx.NET实现包裹处理的响应式流
/// Reactive Parcel Processing Service - Reactive streams for parcel processing using Rx.NET
/// </summary>
public class ReactiveParcelProcessingService : IDisposable
{
    private readonly ILogger<ReactiveParcelProcessingService> _logger;
    private readonly Subject<ParcelCreatedEvent> _parcelCreatedSubject;
    private readonly Subject<DwsDataReceivedEvent> _dwsDataReceivedSubject;
    private readonly Subject<ParcelProcessedEvent> _parcelProcessedSubject;
    private readonly List<IDisposable> _subscriptions;

    /// <summary>
    /// 包裹创建事件流
    /// Parcel created event stream
    /// </summary>
    public IObservable<ParcelCreatedEvent> ParcelCreated => _parcelCreatedSubject.AsObservable();

    /// <summary>
    /// DWS数据接收事件流
    /// DWS data received event stream
    /// </summary>
    public IObservable<DwsDataReceivedEvent> DwsDataReceived => _dwsDataReceivedSubject.AsObservable();

    /// <summary>
    /// 包裹处理完成事件流
    /// Parcel processed event stream
    /// </summary>
    public IObservable<ParcelProcessedEvent> ParcelProcessed => _parcelProcessedSubject.AsObservable();

    public ReactiveParcelProcessingService(ILogger<ReactiveParcelProcessingService> logger)
    {
        _logger = logger;
        _parcelCreatedSubject = new Subject<ParcelCreatedEvent>();
        _dwsDataReceivedSubject = new Subject<DwsDataReceivedEvent>();
        _parcelProcessedSubject = new Subject<ParcelProcessedEvent>();
        _subscriptions = new List<IDisposable>();

        InitializeStreams();
    }

    private void InitializeStreams()
    {
        // 包裹吞吐量监控流 - 每10秒统计包裹创建数量
        // Parcel throughput monitoring stream - Count parcels created every 10 seconds
        var throughputSubscription = ParcelCreated
            .Buffer(TimeSpan.FromSeconds(10))
            .Where(batch => batch.Count > 0)
            .Subscribe(
                batch => _logger.LogInformation("包裹吞吐量: {Count} 个包裹/10秒", batch.Count),
                ex => _logger.LogError(ex, "处理包裹吞吐量流时发生错误")
            );
        _subscriptions.Add(throughputSubscription);

        // 包裹处理延迟监控流 - 计算从创建到处理完成的时间
        // Parcel processing latency stream - Calculate time from creation to completion
        var latencySubscription = Observable
            .CombineLatest(
                ParcelCreated,
                ParcelProcessed.Where(p => p.Success),
                (created, processed) => new { Created = created, Processed = processed }
            )
            .Where(pair => pair.Created.ParcelId == pair.Processed.ParcelId)
            .Select(pair => new
            {
                pair.Created.ParcelId,
                Latency = (pair.Processed.ProcessedAt - pair.Created.CreatedAt).TotalMilliseconds
            })
            .Buffer(TimeSpan.FromMinutes(1))
            .Where(batch => batch.Count > 0)
            .Select(batch => new
            {
                Count = batch.Count,
                AvgLatency = batch.Average(x => x.Latency),
                MaxLatency = batch.Max(x => x.Latency),
                MinLatency = batch.Min(x => x.Latency),
                P95Latency = batch.OrderBy(x => x.Latency).ElementAt((int)(batch.Count * 0.95))?.Latency ?? 0
            })
            .Subscribe(
                stats => _logger.LogInformation(
                    "包裹处理延迟统计 (1分钟): 平均={Avg:F2}ms, P95={P95:F2}ms, 最大={Max:F2}ms, 最小={Min:F2}ms",
                    stats.AvgLatency, stats.P95Latency, stats.MaxLatency, stats.MinLatency),
                ex => _logger.LogError(ex, "处理包裹延迟统计流时发生错误")
            );
        _subscriptions.Add(latencySubscription);

        // DWS数据质量监控流 - 检测异常的DWS数据
        // DWS data quality monitoring stream - Detect anomalous DWS data
        var dwsQualitySubscription = DwsDataReceived
            .Where(dws =>
                dws.Weight <= 0 || dws.Weight > 50000 || // 异常重量: <=0 或 >50kg
                dws.Volume <= 0 || dws.Volume > 1000000 || // 异常体积: <=0 或 >1立方米
                string.IsNullOrEmpty(dws.Barcode)) // 条码为空
            .Throttle(TimeSpan.FromSeconds(5))
            .Subscribe(
                dws => _logger.LogWarning(
                    "检测到异常DWS数据: Barcode={Barcode}, Weight={Weight}g, Volume={Volume}cm³",
                    dws.Barcode ?? "NULL", dws.Weight, dws.Volume),
                ex => _logger.LogError(ex, "处理DWS数据质量监控流时发生错误")
            );
        _subscriptions.Add(dwsQualitySubscription);

        // 失败包裹监控流 - 识别处理失败的包裹
        // Failed parcel monitoring stream - Identify failed parcels
        var failedParcelsSubscription = ParcelProcessed
            .Where(p => !p.Success)
            .Buffer(TimeSpan.FromMinutes(5))
            .Where(batch => batch.Count > 0)
            .Subscribe(
                batch =>
                {
                    _logger.LogWarning("处理失败的包裹数量 (5分钟): {Count}", batch.Count);
                    foreach (var failed in batch.Take(5)) // 记录前5个失败包裹
                    {
                        _logger.LogDebug("失败包裹详情: ParcelId={ParcelId}, Reason={Reason}",
                            failed.ParcelId, failed.ErrorMessage ?? "未知原因");
                    }
                },
                ex => _logger.LogError(ex, "处理失败包裹监控流时发生错误")
            );
        _subscriptions.Add(failedParcelsSubscription);

        // 包裹流完整性监控 - 确保每个创建的包裹都有对应的DWS数据
        // Parcel flow integrity monitoring - Ensure every created parcel has corresponding DWS data
        var integritySubscription = ParcelCreated
            .SelectMany(created => DwsDataReceived
                .Where(dws => dws.Barcode == created.Barcode || dws.ParcelId == created.ParcelId)
                .Take(1)
                .Timeout(TimeSpan.FromMinutes(5))
                .Catch<DwsDataReceivedEvent, TimeoutException>(ex =>
                {
                    _logger.LogWarning("包裹 {ParcelId} 在5分钟内未收到DWS数据", created.ParcelId);
                    return Observable.Empty<DwsDataReceivedEvent>();
                }))
            .Subscribe(
                _ => { /* DWS数据正常接收 */ },
                ex => _logger.LogError(ex, "处理包裹流完整性监控时发生错误")
            );
        _subscriptions.Add(integritySubscription);
    }

    /// <summary>
    /// 发布包裹创建事件
    /// Publish parcel created event
    /// </summary>
    public void PublishParcelCreated(string parcelId, string? barcode, DateTime createdAt)
    {
        _parcelCreatedSubject.OnNext(new ParcelCreatedEvent
        {
            ParcelId = parcelId,
            Barcode = barcode,
            CreatedAt = createdAt
        });
    }

    /// <summary>
    /// 发布DWS数据接收事件
    /// Publish DWS data received event
    /// </summary>
    public void PublishDwsDataReceived(string? parcelId, string barcode, decimal weight, decimal volume, DateTime receivedAt)
    {
        _dwsDataReceivedSubject.OnNext(new DwsDataReceivedEvent
        {
            ParcelId = parcelId,
            Barcode = barcode,
            Weight = weight,
            Volume = volume,
            ReceivedAt = receivedAt
        });
    }

    /// <summary>
    /// 发布包裹处理完成事件
    /// Publish parcel processed event
    /// </summary>
    public void PublishParcelProcessed(string parcelId, bool success, DateTime processedAt, string? errorMessage = null)
    {
        _parcelProcessedSubject.OnNext(new ParcelProcessedEvent
        {
            ParcelId = parcelId,
            Success = success,
            ProcessedAt = processedAt,
            ErrorMessage = errorMessage
        });
    }

    /// <summary>
    /// 获取实时包裹处理指标流
    /// Get real-time parcel processing metrics stream
    /// </summary>
    public IObservable<ParcelProcessingMetrics> GetProcessingMetricsStream(TimeSpan windowDuration)
    {
        return Observable.Interval(windowDuration)
            .SelectMany(_ => Observable.CombineLatest(
                ParcelCreated.Buffer(windowDuration),
                DwsDataReceived.Buffer(windowDuration),
                ParcelProcessed.Buffer(windowDuration),
                (created, dwsReceived, processed) => new ParcelProcessingMetrics
                {
                    Timestamp = DateTime.Now,
                    ParcelsCreated = created.Count,
                    DwsDataReceived = dwsReceived.Count,
                    ParcelsProcessed = processed.Count,
                    SuccessfulProcessed = processed.Count(p => p.Success),
                    FailedProcessed = processed.Count(p => !p.Success),
                    SuccessRate = processed.Count > 0
                        ? (processed.Count(p => p.Success) * 100.0 / processed.Count)
                        : 100.0
                }
            ));
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }
        _subscriptions.Clear();

        _parcelCreatedSubject.OnCompleted();
        _parcelCreatedSubject.Dispose();

        _dwsDataReceivedSubject.OnCompleted();
        _dwsDataReceivedSubject.Dispose();

        _parcelProcessedSubject.OnCompleted();
        _parcelProcessedSubject.Dispose();
    }
}

#region Event Models

/// <summary>
/// 包裹创建事件
/// Parcel created event
/// </summary>
public class ParcelCreatedEvent
{
    public string ParcelId { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DWS数据接收事件
/// DWS data received event
/// </summary>
public class DwsDataReceivedEvent
{
    public string? ParcelId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal Volume { get; set; }
    public DateTime ReceivedAt { get; set; }
}

/// <summary>
/// 包裹处理完成事件
/// Parcel processed event
/// </summary>
public class ParcelProcessedEvent
{
    public string ParcelId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 包裹处理指标
/// Parcel processing metrics
/// </summary>
public class ParcelProcessingMetrics
{
    public DateTime Timestamp { get; set; }
    public int ParcelsCreated { get; set; }
    public int DwsDataReceived { get; set; }
    public int ParcelsProcessed { get; set; }
    public int SuccessfulProcessed { get; set; }
    public int FailedProcessed { get; set; }
    public double SuccessRate { get; set; }
}

#endregion
