# 异常隔离和弹性策略 (Exception Isolation and Resilience Policies)

本文档描述系统中实施的异常隔离和弹性策略，使用Polly库实现重试、熔断器和降级处理。

## 已实施的弹性策略

### 1. 数据库熔断器 (DatabaseCircuitBreaker)

**位置**: `ResilientLogRepository`
**目的**: 当MySQL不可用时自动降级到SQLite

**配置**:
```csharp
FailureRatio: 0.5 (50%失败率触发熔断)
MinimumThroughput: 10 (最小吞吐量)
SamplingDuration: 30秒 (采样周期)
BreakDuration: 1200秒 (20分钟熔断时间)
```

**行为**:
- 当50%的请求失败且达到最小吞吐量时，熔断器打开
- 熔断器打开后，所有请求直接降级到SQLite
- 20分钟后熔断器进入半开状态，尝试恢复
- 如果恢复成功，开始同步SQLite数据到MySQL并清空SQLite

### 2. 格口统计服务重试策略 (ChuteStatisticsService Retry)

**位置**: `ChuteStatisticsService`
**目的**: 提高统计查询的可靠性

**配置**:
```csharp
MaxRetryAttempts: 3
Delay: 100ms
BackoffType: Exponential (指数退避)
```

**应用范围**:
- `GetChuteUtilizationStatisticsAsync`
- `GetChuteStatisticsByIdAsync`
- `GetSortingEfficiencyOverviewAsync`
- `GetChuteHourlyStatisticsAsync`

## 需要添加弹性策略的关键点

### 1. 第三方API调用

**服务**: `ThirdPartyApiClient`
**建议策略**:
- 重试策略: 3次重试，指数退避
- 超时策略: 30秒超时
- 熔断器: 连续失败5次后熔断5分钟

**实施优先级**: 高

### 2. 规则引擎服务

**服务**: `RuleEngineService`
**建议策略**:
- 重试策略: 对临时性失败重试2次
- 超时策略: 单个规则评估5秒超时
- 降级策略: 规则评估失败时返回默认格口

**实施优先级**: 中

### 3. 数据库查询

**服务**: 所有仓储实现
**建议策略**:
- 重试策略: 瞬态错误重试3次
- 超时策略: 查询30秒超时
- 熔断器: 已在ResilientLogRepository实施

**实施优先级**: 中（部分已实施）

### 4. SignalR Hub通信

**服务**: `SortingHub`, `DwsHub`
**建议策略**:
- 重连策略: 自动重连，最多5次
- 超时策略: 消息发送10秒超时
- 降级策略: Hub不可用时记录日志

**实施优先级**: 低

## 弹性策略最佳实践

### 重试策略 (Retry Policy)

适用场景:
- 网络瞬态故障
- 数据库连接暂时不可用
- 远程服务临时过载

不适用场景:
- 验证错误
- 权限错误
- 资源不存在错误

示例实现:
```csharp
var retryPipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(100),
        BackoffType = DelayBackoffType.Exponential,
        OnRetry = args =>
        {
            _logger.LogWarning("重试第 {Attempt} 次", args.AttemptNumber);
            return ValueTask.CompletedTask;
        }
    })
    .Build();
```

### 熔断器策略 (Circuit Breaker)

适用场景:
- 外部服务持续不可用
- 数据库连接问题
- 第三方API故障

配置建议:
- FailureRatio: 0.5-0.7 (50%-70%失败率)
- MinimumThroughput: 10-20
- BreakDuration: 30秒-5分钟（根据服务恢复时间）

示例实现:
```csharp
var circuitBreaker = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 10,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromMinutes(1),
        OnOpened = args =>
        {
            _logger.LogError("熔断器打开");
            return ValueTask.CompletedTask;
        }
    })
    .Build();
```

### 超时策略 (Timeout Policy)

适用场景:
- 所有外部调用
- 长时间运行的操作
- 数据库查询

配置建议:
- 短操作: 1-5秒
- 中等操作: 10-30秒
- 长操作: 1-5分钟

示例实现:
```csharp
var timeoutPipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromSeconds(30))
    .Build();
```

### 降级策略 (Fallback Policy)

适用场景:
- 主服务不可用时提供备用方案
- 返回缓存数据
- 返回默认值

示例实现:
```csharp
var fallbackPipeline = new ResiliencePipelineBuilder<MyResult>()
    .AddFallback(new FallbackStrategyOptions<MyResult>
    {
        FallbackAction = args => Outcome.FromResultAsValueTask(GetDefaultResult()),
        OnFallback = args =>
        {
            _logger.LogWarning("执行降级策略");
            return ValueTask.CompletedTask;
        }
    })
    .Build();
```

## 组合策略

建议将多个策略组合使用以获得最佳效果:

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromSeconds(30))              // 外层超时
    .AddCircuitBreaker(circuitBreakerOptions)          // 熔断器
    .AddRetry(retryOptions)                            // 重试
    .Build();
```

策略执行顺序（从外到内）:
1. 超时 - 防止操作无限期挂起
2. 熔断器 - 快速失败，避免级联故障
3. 重试 - 处理瞬态故障

## 监控和日志

所有弹性策略都应该记录日志:

- 重试事件: Warning级别
- 熔断器打开/关闭: Error/Info级别
- 超时事件: Error级别
- 降级执行: Warning级别

建议使用结构化日志记录关键指标:
- 重试次数
- 熔断器状态变化
- 操作耗时
- 失败原因

## 配置管理

所有弹性策略配置应该:
1. 在`appsettings.json`中可配置
2. 有合理的默认值
3. 支持运行时热更新（对于关键参数）
4. 根据环境调整（开发/测试/生产）

## 测试

弹性策略应该有专门的测试:
- 模拟瞬态故障验证重试
- 模拟持续故障验证熔断器
- 验证降级策略正确执行
- 性能测试验证策略开销可接受

## 性能考虑

- 重试会增加延迟，应谨慎使用
- 熔断器可以显著提高系统响应性
- 超时应该设置合理值，避免过早或过晚超时
- 监控策略执行开销

## 参考资源

- [Polly Documentation](https://github.com/App-vNext/Polly)
- [The Polly Project](https://www.thepollyproject.org/)
- [Resilience Patterns](https://docs.microsoft.com/en-us/azure/architecture/patterns/category/resiliency)
- [Circuit Breaker Pattern](https://martinfowler.com/bliki/CircuitBreaker.html)
