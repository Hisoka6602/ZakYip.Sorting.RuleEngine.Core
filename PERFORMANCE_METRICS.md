# 性能指标监控文档

## 概述

ZakYip分拣规则引擎集成了全面的性能指标收集和监控系统，用于跟踪和分析系统性能。

## 性能指标实体

### PerformanceMetric

性能指标包含以下信息：

```csharp
public class PerformanceMetric
{
    public string MetricId { get; set; }              // 指标ID
    public string? ParcelId { get; set; }             // 包裹ID（可选）
    public string OperationName { get; set; }         // 操作名称
    public long DurationMs { get; set; }              // 执行时长（毫秒）
    public bool Success { get; set; }                 // 是否成功
    public string? ErrorMessage { get; set; }         // 错误消息
    public string? Metadata { get; set; }             // 额外元数据（JSON格式）
    public DateTime RecordedAt { get; set; }          // 记录时间
}
```

## 操作类型

系统自动收集以下操作的性能指标：

### 1. 规则评估 (RuleEvaluation)

- 包裹ID：关联的包裹ID
- 时长：规则评估所需时间
- 成功：规则是否成功匹配
- 元数据：匹配的规则数量等

### 2. 第三方API调用 (ThirdPartyApiCall)

- 包裹ID：关联的包裹ID
- 时长：API调用时间
- 成功：API调用是否成功
- 元数据：API响应状态码等

### 3. 数据库操作 (DatabaseOperation)

- 时长：数据库操作时间
- 成功：操作是否成功
- 元数据：操作类型、影响行数等

## 使用方式

### 自动收集

规则引擎服务自动收集规则评估的性能指标：

```csharp
public async Task<string?> EvaluateRulesAsync(
    ParcelInfo parcelInfo,
    DwsData? dwsData,
    ThirdPartyResponse? thirdPartyResponse,
    CancellationToken cancellationToken = default)
{
    return await _performanceService.ExecuteWithMetricsAsync(
        "RuleEvaluation",
        async () =>
        {
            // 规则评估逻辑
            // ...
        },
        parcelInfo.ParcelId,
        null,
        cancellationToken);
}
```

### 手动收集

您也可以手动收集性能指标：

```csharp
await _performanceService.ExecuteWithMetricsAsync(
    "CustomOperation",
    async () =>
    {
        // 您的操作
        await DoSomethingAsync();
        return result;
    },
    parcelId: "PKG001",
    metadata: "{\"customField\": \"value\"}",
    cancellationToken);
```

## 性能指标查询

### 查询指定时间范围的指标

```csharp
var metrics = await performanceMetricRepository.GetMetricsAsync(
    startTime: DateTime.UtcNow.AddHours(-1),
    endTime: DateTime.UtcNow,
    operationName: "RuleEvaluation",
    cancellationToken);
```

### 获取性能统计摘要

```csharp
var summary = await performanceMetricRepository.GetMetricsSummaryAsync(
    startTime: DateTime.UtcNow.AddHours(-24),
    endTime: DateTime.UtcNow,
    operationName: "RuleEvaluation",
    cancellationToken);

Console.WriteLine($"总操作数: {summary.TotalOperations}");
Console.WriteLine($"成功率: {(double)summary.SuccessfulOperations / summary.TotalOperations * 100:F2}%");
Console.WriteLine($"平均时长: {summary.AverageDurationMs:F2}ms");
Console.WriteLine($"P95时长: {summary.P95DurationMs:F2}ms");
Console.WriteLine($"P99时长: {summary.P99DurationMs:F2}ms");
```

## 性能指标摘要

性能摘要包含以下统计信息：

```csharp
public class PerformanceMetricSummary
{
    public long TotalOperations { get; set; }         // 总操作数
    public long SuccessfulOperations { get; set; }    // 成功操作数
    public long FailedOperations { get; set; }        // 失败操作数
    public double AverageDurationMs { get; set; }     // 平均时长
    public long MinDurationMs { get; set; }           // 最小时长
    public long MaxDurationMs { get; set; }           // 最大时长
    public double P50DurationMs { get; set; }         // P50时长（中位数）
    public double P95DurationMs { get; set; }         // P95时长
    public double P99DurationMs { get; set; }         // P99时长
}
```

## 实现性能指标仓储

您需要实现 `IPerformanceMetricRepository` 接口来存储性能指标：

### MySQL实现示例

```csharp
public class MySqlPerformanceMetricRepository : IPerformanceMetricRepository
{
    private readonly MySqlDbContext _context;

    public async Task RecordMetricAsync(PerformanceMetric metric, CancellationToken cancellationToken = default)
    {
        await _context.PerformanceMetrics.AddAsync(metric, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<PerformanceMetric>> GetMetricsAsync(
        DateTime startTime,
        DateTime endTime,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PerformanceMetrics
            .Where(m => m.RecordedAt >= startTime && m.RecordedAt <= endTime);

        if (!string.IsNullOrEmpty(operationName))
        {
            query = query.Where(m => m.OperationName == operationName);
        }

        return await query
            .OrderByDescending(m => m.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PerformanceMetricSummary> GetMetricsSummaryAsync(
        DateTime startTime,
        DateTime endTime,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PerformanceMetrics
            .Where(m => m.RecordedAt >= startTime && m.RecordedAt <= endTime);

        if (!string.IsNullOrEmpty(operationName))
        {
            query = query.Where(m => m.OperationName == operationName);
        }

        var metrics = await query.ToListAsync(cancellationToken);
        var durations = metrics.Select(m => m.DurationMs).OrderBy(d => d).ToList();

        return new PerformanceMetricSummary
        {
            TotalOperations = metrics.Count,
            SuccessfulOperations = metrics.Count(m => m.Success),
            FailedOperations = metrics.Count(m => !m.Success),
            AverageDurationMs = metrics.Any() ? metrics.Average(m => m.DurationMs) : 0,
            MinDurationMs = durations.Any() ? durations.First() : 0,
            MaxDurationMs = durations.Any() ? durations.Last() : 0,
            P50DurationMs = GetPercentile(durations, 0.50),
            P95DurationMs = GetPercentile(durations, 0.95),
            P99DurationMs = GetPercentile(durations, 0.99)
        };
    }

    private double GetPercentile(List<long> sortedValues, double percentile)
    {
        if (!sortedValues.Any()) return 0;

        int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));
        return sortedValues[index];
    }
}
```

## 配置

在 `Program.cs` 中注册性能指标服务：

```csharp
// 注册性能指标仓储
services.AddSingleton<IPerformanceMetricRepository, MySqlPerformanceMetricRepository>();

// 注册性能指标服务
services.AddSingleton<PerformanceMetricService>();
```

## 监控和告警

### 性能阈值

建议设置以下性能阈值：

- **规则评估**：P95 < 50ms，P99 < 100ms
- **第三方API调用**：P95 < 200ms，P99 < 500ms
- **数据库操作**：P95 < 30ms，P99 < 100ms

### 告警示例

```csharp
var summary = await repository.GetMetricsSummaryAsync(
    DateTime.UtcNow.AddMinutes(-5),
    DateTime.UtcNow,
    "RuleEvaluation");

if (summary.P95DurationMs > 50)
{
    logger.LogWarning(
        "规则评估性能告警：P95={P95}ms 超过阈值50ms",
        summary.P95DurationMs);
    
    // 发送告警通知
    await SendAlertAsync("规则评估性能下降", summary);
}

if (summary.FailedOperations > summary.TotalOperations * 0.01)
{
    logger.LogError(
        "规则评估失败率告警：{FailureRate}% 超过阈值1%",
        (double)summary.FailedOperations / summary.TotalOperations * 100);
    
    // 发送告警通知
    await SendAlertAsync("规则评估失败率过高", summary);
}
```

## 性能优化建议

1. **启用缓存** - 规则缓存默认启用，滑动过期5分钟
2. **数据库索引** - 在 `RecordedAt` 和 `OperationName` 字段上创建索引
3. **批量插入** - 对于高频场景，考虑批量插入性能指标
4. **数据归档** - 定期归档旧的性能指标数据
5. **采样** - 在极高频场景下，可以采样记录部分指标

## 数据库表结构

### MySQL

```sql
CREATE TABLE performance_metrics (
    metric_id VARCHAR(36) PRIMARY KEY,
    parcel_id VARCHAR(50),
    operation_name VARCHAR(100) NOT NULL,
    duration_ms BIGINT NOT NULL,
    success BOOLEAN NOT NULL,
    error_message TEXT,
    metadata TEXT,
    recorded_at DATETIME NOT NULL,
    INDEX idx_recorded_at (recorded_at),
    INDEX idx_operation_name (operation_name),
    INDEX idx_parcel_id (parcel_id)
);
```

### SQLite

```sql
CREATE TABLE performance_metrics (
    metric_id TEXT PRIMARY KEY,
    parcel_id TEXT,
    operation_name TEXT NOT NULL,
    duration_ms INTEGER NOT NULL,
    success INTEGER NOT NULL,
    error_message TEXT,
    metadata TEXT,
    recorded_at TEXT NOT NULL
);

CREATE INDEX idx_recorded_at ON performance_metrics(recorded_at);
CREATE INDEX idx_operation_name ON performance_metrics(operation_name);
CREATE INDEX idx_parcel_id ON performance_metrics(parcel_id);
```

## 日志集成

性能指标也会自动记录到日志系统：

```
2025-10-24 12:00:00 [INF] 性能指标 - 操作: RuleEvaluation, 包裹: PKG001, 时长: 25ms, 成功: True
```

## 最佳实践

1. **定期审查** - 每周审查性能指标，识别性能瓶颈
2. **趋势分析** - 跟踪性能趋势，及早发现问题
3. **容量规划** - 基于性能指标进行容量规划
4. **持续优化** - 根据性能数据持续优化系统
5. **告警设置** - 设置合理的性能告警阈值
