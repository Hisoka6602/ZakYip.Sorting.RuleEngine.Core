namespace ZakYip.Sorting.RuleEngine.DataSimulator.Simulators;

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
