using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 响应式监控服务 - 使用Rx.NET实现实时数据流监控
/// Reactive Monitoring Service - Real-time data stream monitoring using Rx.NET
/// </summary>
public class ReactiveMonitoringService : IDisposable
{
    private readonly ILogger<ReactiveMonitoringService> _logger;
    private readonly Subject<CommunicationLog> _communicationLogSubject;
    private readonly Subject<ApiCommunicationLog> _apiCommunicationLogSubject;
    private readonly Subject<MatchingLog> _matchingLogSubject;
    private readonly Subject<MonitoringAlert> _alertSubject;
    private readonly List<IDisposable> _subscriptions;

    /// <summary>
    /// 通信日志流 - 可观察的通信日志序列
    /// Communication log stream - Observable sequence of communication logs
    /// </summary>
    public IObservable<CommunicationLog> CommunicationLogs => _communicationLogSubject.AsObservable();

    /// <summary>
    /// API通信日志流 - 可观察的API通信日志序列
    /// API communication log stream - Observable sequence of API communication logs
    /// </summary>
    public IObservable<ApiCommunicationLog> ApiCommunicationLogs => _apiCommunicationLogSubject.AsObservable();

    /// <summary>
    /// 匹配日志流 - 可观察的匹配日志序列
    /// Matching log stream - Observable sequence of matching logs
    /// </summary>
    public IObservable<MatchingLog> MatchingLogs => _matchingLogSubject.AsObservable();

    /// <summary>
    /// 告警流 - 可观察的告警序列
    /// Alert stream - Observable sequence of monitoring alerts
    /// </summary>
    public IObservable<MonitoringAlert> Alerts => _alertSubject.AsObservable();

    public ReactiveMonitoringService(ILogger<ReactiveMonitoringService> logger)
    {
        _logger = logger;
        _communicationLogSubject = new Subject<CommunicationLog>();
        _apiCommunicationLogSubject = new Subject<ApiCommunicationLog>();
        _matchingLogSubject = new Subject<MatchingLog>();
        _alertSubject = new Subject<MonitoringAlert>();
        _subscriptions = new List<IDisposable>();

        InitializeStreams();
    }

    /// <summary>
    /// 初始化响应式数据流
    /// Initialize reactive data streams
    /// </summary>
    private void InitializeStreams()
    {
        // 失败通信日志流 - 过滤失败的通信日志
        // Failed communication log stream - Filter failed communication logs
        var failedCommSubscription = CommunicationLogs
            .Where(log => !log.IsSuccess)
            .Buffer(TimeSpan.FromMinutes(1)) // 按1分钟批次缓冲
            .Where(batch => batch.Count > 0)
            .Subscribe(
                logs => _logger.LogWarning("检测到 {Count} 个失败的通信日志在过去1分钟内", logs.Count),
                ex => _logger.LogError(ex, "处理失败通信日志流时发生错误")
            );
        _subscriptions.Add(failedCommSubscription);

        // 慢速API调用流 - 检测超过5秒的API调用
        // Slow API call stream - Detect API calls taking more than 5 seconds
        var slowApiSubscription = ApiCommunicationLogs
            .Where(log => log.DurationMs > 5000)
            .Throttle(TimeSpan.FromSeconds(5)) // 防抖：5秒内只报告一次
            .Subscribe(
                log => _logger.LogWarning("检测到慢速API调用: ParcelId={ParcelId}, Duration={Duration}ms, Url={Url}",
                    log.ParcelId, log.DurationMs, log.RequestUrl),
                ex => _logger.LogError(ex, "处理慢速API调用流时发生错误")
            );
        _subscriptions.Add(slowApiSubscription);

        // 失败率统计流 - 每分钟计算失败率
        // Failure rate stream - Calculate failure rate per minute
        var failureRateSubscription = MatchingLogs
            .Buffer(TimeSpan.FromMinutes(1))
            .Where(batch => batch.Count > 0)
            .Select(batch => new
            {
                Total = batch.Count,
                Failed = batch.Count(log => !log.IsSuccess),
                FailureRate = batch.Count > 0 ? (batch.Count(log => !log.IsSuccess) * 100.0 / batch.Count) : 0
            })
            .Where(stats => stats.FailureRate > 5) // 失败率超过5%时报告
            .Subscribe(
                stats => _logger.LogWarning("匹配失败率过高: {FailureRate:F2}% ({Failed}/{Total})",
                    stats.FailureRate, stats.Failed, stats.Total),
                ex => _logger.LogError(ex, "处理失败率统计流时发生错误")
            );
        _subscriptions.Add(failureRateSubscription);

        // 告警聚合流 - 按类型和严重程度聚合告警
        // Alert aggregation stream - Aggregate alerts by type and severity
        var alertAggregationSubscription = Alerts
            .Buffer(TimeSpan.FromMinutes(5))
            .Where(batch => batch.Count > 0)
            .Select(batch => batch
                .GroupBy(alert => new { alert.Type, alert.Severity })
                .Select(g => new { g.Key.Type, g.Key.Severity, Count = g.Count() })
                .OrderByDescending(x => x.Count))
            .Subscribe(
                groups =>
                {
                    foreach (var group in groups)
                    {
                        _logger.LogInformation("告警统计 (过去5分钟): 类型={Type}, 严重程度={Severity}, 数量={Count}",
                            group.Type, group.Severity, group.Count);
                    }
                },
                ex => _logger.LogError(ex, "处理告警聚合流时发生错误")
            );
        _subscriptions.Add(alertAggregationSubscription);

        // API性能趋势流 - 计算滑动窗口平均响应时间
        // API performance trend stream - Calculate moving average response time
        var apiPerfSubscription = ApiCommunicationLogs
            .Where(log => log.IsSuccess)
            .Buffer(10) // 每10个请求计算一次
            .Select(batch => new
            {
                Count = batch.Count,
                AvgDuration = batch.Average(log => log.DurationMs),
                MaxDuration = batch.Max(log => log.DurationMs),
                MinDuration = batch.Min(log => log.DurationMs)
            })
            .DistinctUntilChanged(stats => stats.AvgDuration) // 仅在平均值变化时输出
            .Subscribe(
                stats => _logger.LogDebug("API性能指标 (最近10个请求): 平均={Avg:F2}ms, 最大={Max}ms, 最小={Min}ms",
                    stats.AvgDuration, stats.MaxDuration, stats.MinDuration),
                ex => _logger.LogError(ex, "处理API性能趋势流时发生错误")
            );
        _subscriptions.Add(apiPerfSubscription);
    }

    /// <summary>
    /// 发布通信日志事件
    /// Publish communication log event
    /// </summary>
    public void PublishCommunicationLog(CommunicationLog log)
    {
        if (log == null) throw new ArgumentNullException(nameof(log));
        _communicationLogSubject.OnNext(log);
    }

    /// <summary>
    /// 发布API通信日志事件
    /// Publish API communication log event
    /// </summary>
    public void PublishApiCommunicationLog(ApiCommunicationLog log)
    {
        if (log == null) throw new ArgumentNullException(nameof(log));
        _apiCommunicationLogSubject.OnNext(log);
    }

    /// <summary>
    /// 发布匹配日志事件
    /// Publish matching log event
    /// </summary>
    public void PublishMatchingLog(MatchingLog log)
    {
        if (log == null) throw new ArgumentNullException(nameof(log));
        _matchingLogSubject.OnNext(log);
    }

    /// <summary>
    /// 发布告警事件
    /// Publish monitoring alert event
    /// </summary>
    public void PublishAlert(MonitoringAlert alert)
    {
        if (alert == null) throw new ArgumentNullException(nameof(alert));
        _alertSubject.OnNext(alert);
    }

    /// <summary>
    /// 订阅通信日志流（自定义处理）
    /// Subscribe to communication log stream (custom processing)
    /// </summary>
    public IDisposable SubscribeToCommunicationLogs(Action<CommunicationLog> onNext, Action<Exception>? onError = null)
    {
        return onError != null
            ? CommunicationLogs.Subscribe(onNext, onError)
            : CommunicationLogs.Subscribe(onNext);
    }

    /// <summary>
    /// 订阅API通信日志流（自定义处理）
    /// Subscribe to API communication log stream (custom processing)
    /// </summary>
    public IDisposable SubscribeToApiCommunicationLogs(Action<ApiCommunicationLog> onNext, Action<Exception>? onError = null)
    {
        return onError != null
            ? ApiCommunicationLogs.Subscribe(onNext, onError)
            : ApiCommunicationLogs.Subscribe(onNext);
    }

    /// <summary>
    /// 订阅匹配日志流（自定义处理）
    /// Subscribe to matching log stream (custom processing)
    /// </summary>
    public IDisposable SubscribeToMatchingLogs(Action<MatchingLog> onNext, Action<Exception>? onError = null)
    {
        return onError != null
            ? MatchingLogs.Subscribe(onNext, onError)
            : MatchingLogs.Subscribe(onNext);
    }

    /// <summary>
    /// 订阅告警流（自定义处理）
    /// Subscribe to alert stream (custom processing)
    /// </summary>
    public IDisposable SubscribeToAlerts(Action<MonitoringAlert> onNext, Action<Exception>? onError = null)
    {
        return onError != null
            ? Alerts.Subscribe(onNext, onError)
            : Alerts.Subscribe(onNext);
    }

    /// <summary>
    /// 获取实时性能指标流
    /// Get real-time performance metrics stream
    /// </summary>
    /// <param name="windowDuration">时间窗口大小</param>
    /// <returns>性能指标的可观察序列</returns>
    public IObservable<PerformanceMetrics> GetPerformanceMetricsStream(TimeSpan windowDuration)
    {
        return Observable.Interval(windowDuration)
            .SelectMany(_ => Observable.CombineLatest(
                ApiCommunicationLogs
                    .Buffer(windowDuration)
                    .Select(logs => new
                    {
                        TotalApiCalls = logs.Count,
                        SuccessfulApiCalls = logs.Count(l => l.IsSuccess),
                        AvgApiDuration = logs.Count > 0 ? logs.Average(l => l.DurationMs) : 0
                    }),
                MatchingLogs
                    .Buffer(windowDuration)
                    .Select(logs => new
                    {
                        TotalMatches = logs.Count,
                        SuccessfulMatches = logs.Count(l => l.IsSuccess),
                        FailureRate = logs.Count > 0 ? (logs.Count(l => !l.IsSuccess) * 100.0 / logs.Count) : 0
                    }),
                (apiStats, matchStats) => new PerformanceMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    TotalApiCalls = apiStats.TotalApiCalls,
                    SuccessfulApiCalls = apiStats.SuccessfulApiCalls,
                    AverageApiDuration = apiStats.AvgApiDuration,
                    TotalMatches = matchStats.TotalMatches,
                    SuccessfulMatches = matchStats.SuccessfulMatches,
                    MatchFailureRate = matchStats.FailureRate
                }
            ));
    }

    public void Dispose()
    {
        // 取消所有订阅
        // Cancel all subscriptions
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }
        _subscriptions.Clear();

        // 完成所有主题
        // Complete all subjects
        _communicationLogSubject.OnCompleted();
        _communicationLogSubject.Dispose();

        _apiCommunicationLogSubject.OnCompleted();
        _apiCommunicationLogSubject.Dispose();

        _matchingLogSubject.OnCompleted();
        _matchingLogSubject.Dispose();

        _alertSubject.OnCompleted();
        _alertSubject.Dispose();
    }
}

/// <summary>
/// 性能指标数据模型
/// Performance metrics data model
/// </summary>
public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    public int TotalApiCalls { get; set; }
    public int SuccessfulApiCalls { get; set; }
    public double AverageApiDuration { get; set; }
    public int TotalMatches { get; set; }
    public int SuccessfulMatches { get; set; }
    public double MatchFailureRate { get; set; }
}
