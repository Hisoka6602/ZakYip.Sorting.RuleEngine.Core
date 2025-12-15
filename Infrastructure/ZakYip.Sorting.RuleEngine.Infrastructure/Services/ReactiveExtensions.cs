using System.Reactive.Linq;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 响应式扩展工具类 - 提供常用的Rx.NET操作符扩展
/// Reactive Extensions Utility - Common Rx.NET operator extensions
/// </summary>
public static class ReactiveExtensions
{
    private static readonly ISystemClock Clock = new SystemClock();

    /// <summary>
    /// 创建一个可观察序列，当超过指定时间未收到元素时发出超时通知
    /// Create an observable sequence that emits a timeout notification when no elements are received within the specified time
    /// </summary>
    public static IObservable<T> TimeoutWithNotification<T>(
        this IObservable<T> source,
        TimeSpan timeout,
        Action onTimeout)
    {
        return source
            .Timeout(timeout)
            .Catch<T, TimeoutException>(ex =>
            {
                onTimeout();
                return Observable.Empty<T>();
            });
    }

    /// <summary>
    /// 创建滑动窗口统计 - 计算指定时间窗口内的统计信息
    /// Create sliding window statistics - Calculate statistics within the specified time window
    /// </summary>
    public static IObservable<WindowStatistics<T>> SlidingWindowStats<T>(
        this IObservable<T> source,
        TimeSpan windowDuration,
        Func<T, double> selector)
    {
        return source
            .Buffer(windowDuration)
            .Where(batch => batch.Count > 0)
            .Select(batch =>
            {
                var values = batch.Select(selector).ToList();
                var now = Clock.LocalNow;
                return new WindowStatistics<T>
                {
                    WindowStart = now - windowDuration,
                    WindowEnd = now,
                    Count = values.Count,
                    Average = values.Average(),
                    Min = values.Min(),
                    Max = values.Max(),
                    Sum = values.Sum(),
                    Items = batch
                };
            });
    }

    /// <summary>
    /// 重试操作符 - 当发生错误时，按指数退避策略重试
    /// Retry operator - Retry on error with exponential backoff strategy
    /// </summary>
    public static IObservable<T> RetryWithBackoff<T>(
        this IObservable<T> source,
        int retryCount = 3,
        TimeSpan? initialDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromSeconds(1);
        
        IObservable<T> retrySequence = source;
        for (int i = 0; i < retryCount; i++)
        {
            var waitTime = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * Math.Pow(2, i));
            retrySequence = retrySequence.Catch((Exception _) => 
                Observable.Timer(waitTime).SelectMany(_ => source)
            );
        }
        
        return retrySequence;
    }

    /// <summary>
    /// 批量处理 - 按时间或数量触发批处理
    /// Batch processing - Trigger batch processing by time or count
    /// </summary>
    public static IObservable<IList<T>> SmartBatch<T>(
        this IObservable<T> source,
        int maxBatchSize,
        TimeSpan maxBatchDuration)
    {
        return source
            .Window(maxBatchDuration)
            .SelectMany(window => window.Buffer(maxBatchSize))
            .Where(batch => batch.Count > 0);
    }

    /// <summary>
    /// 变化检测 - 仅在值发生显著变化时发出元素
    /// Change detection - Emit elements only when the value changes significantly
    /// </summary>
    public static IObservable<T> DistinctUntilChangedBy<T, TKey>(
        this IObservable<T> source,
        Func<T, TKey> keySelector,
        Func<TKey, TKey, bool> comparer)
    {
        return source
            .DistinctUntilChanged(item => keySelector(item), new FuncEqualityComparer<TKey>(comparer));
    }

    /// <summary>
    /// 采样 - 按指定间隔采样最新值
    /// Sampling - Sample the latest value at specified intervals
    /// </summary>
    public static IObservable<T> SampleLatest<T>(
        this IObservable<T> source,
        TimeSpan interval)
    {
        return source.Sample(interval);
    }

    /// <summary>
    /// 限流 - 限制每秒最多发出指定数量的元素
    /// Rate limiting - Limit the maximum number of elements emitted per second
    /// </summary>
    public static IObservable<T> RateLimit<T>(
        this IObservable<T> source,
        int maxItemsPerSecond)
    {
        var interval = TimeSpan.FromMilliseconds(1000.0 / maxItemsPerSecond);
        return source
            .Select((item, index) => Observable.Timer(TimeSpan.FromMilliseconds(interval.TotalMilliseconds * index))
                .Select(_ => item))
            .Concat();
    }

    /// <summary>
    /// 异常处理包装器 - 捕获并记录异常，然后继续处理
    /// Exception handling wrapper - Catch and log exceptions, then continue processing
    /// </summary>
    public static IObservable<T> CatchAndContinue<T>(
        this IObservable<T> source,
        Action<Exception> onError)
    {
        return source.Catch((Exception ex) =>
        {
            onError(ex);
            return Observable.Empty<T>();
        });
    }

    /// <summary>
    /// 去重 - 在指定时间窗口内去除重复元素
    /// Deduplication - Remove duplicate elements within the specified time window
    /// </summary>
    public static IObservable<T> DistinctWithinWindow<T, TKey>(
        this IObservable<T> source,
        Func<T, TKey> keySelector,
        TimeSpan windowDuration)
    {
        return source
            .GroupByUntil(keySelector, g => Observable.Timer(windowDuration))
            .SelectMany(g => g.Take(1));
    }

    /// <summary>
    /// 心跳检测 - 确保流在指定时间内有活动，否则发出心跳信号
    /// Heartbeat detection - Ensure the stream is active within the specified time, otherwise emit a heartbeat signal
    /// </summary>
    public static IObservable<Either<T, HeartbeatSignal>> WithHeartbeat<T>(
        this IObservable<T> source,
        TimeSpan heartbeatInterval)
    {
        var heartbeat = Observable
            .Interval(heartbeatInterval)
            .Select(_ => Either<T, HeartbeatSignal>.CreateRight(new HeartbeatSignal { Timestamp = Clock.LocalNow }));

        var sourceWithLeft = source.Select(item => Either<T, HeartbeatSignal>.CreateLeft(item));

        return sourceWithLeft.Merge(heartbeat);
    }
}

/// <summary>
/// 窗口统计信息
/// Window statistics
/// </summary>
public class WindowStatistics<T>
{
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public int Count { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Sum { get; set; }
    public IList<T> Items { get; set; } = new List<T>();
}

/// <summary>
/// 心跳信号
/// Heartbeat signal
/// </summary>
public class HeartbeatSignal
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Either类型 - 表示两种可能值之一
/// Either type - Represents one of two possible values
/// </summary>
public class Either<TLeft, TRight>
{
    private readonly TLeft? _left;
    private readonly TRight? _right;
    private readonly bool _isLeft;

    private Either(TLeft left)
    {
        _left = left;
        _isLeft = true;
    }

    private Either(TRight right)
    {
        _right = right;
        _isLeft = false;
    }

    public static Either<TLeft, TRight> CreateLeft(TLeft left) => new(left);
    public static Either<TLeft, TRight> CreateRight(TRight right) => new(right);

    public bool IsLeft => _isLeft;
    public bool IsRight => !_isLeft;

    public TLeft Left => _isLeft ? _left! : throw new InvalidOperationException("Cannot get Left value from Right");
    public TRight Right => !_isLeft ? _right! : throw new InvalidOperationException("Cannot get Right value from Left");
}

/// <summary>
/// 函数相等比较器
/// Function equality comparer
/// </summary>
internal class FuncEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, T, bool> _comparer;

    public FuncEqualityComparer(Func<T, T, bool> comparer)
    {
        _comparer = comparer;
    }

    public bool Equals(T? x, T? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return _comparer(x, y);
    }

    public int GetHashCode(T obj)
    {
        return obj?.GetHashCode() ?? 0;
    }
}
