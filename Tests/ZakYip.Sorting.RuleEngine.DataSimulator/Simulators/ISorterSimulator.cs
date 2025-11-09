using ZakYip.Sorting.RuleEngine.DataSimulator.Generators;

namespace ZakYip.Sorting.RuleEngine.DataSimulator.Simulators;

/// <summary>
/// 分拣机模拟器接口
/// Sorter simulator interface
/// </summary>
public interface ISorterSimulator : IDisposable
{
    /// <summary>
    /// 连接到分拣机
    /// Connect to sorter
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 发送单个包裹信号
    /// Send single parcel signal
    /// </summary>
    Task<SimulatorResult> SendParcelAsync(ParcelData parcel);

    /// <summary>
    /// 批量发送包裹信号
    /// Send batch of parcel signals
    /// </summary>
    Task<BatchResult> SendBatchAsync(int count, int delayMs = 0);

    /// <summary>
    /// 压力测试模式
    /// Stress test mode
    /// </summary>
    Task<StressTestResult> RunStressTestAsync(
        int durationSeconds,
        int ratePerSecond,
        CancellationToken cancellationToken = default);
}
