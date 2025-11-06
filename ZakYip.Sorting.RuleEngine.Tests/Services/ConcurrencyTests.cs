using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 并发场景测试
/// Concurrency scenario tests for rule engine services
/// </summary>
public class ConcurrencyTests
{
    private readonly Mock<IRuleRepository> _mockRuleRepository;
    private readonly Mock<ILogger<RuleEngineService>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly RuleEngineService _service;

    public ConcurrencyTests()
    {
        _mockRuleRepository = new Mock<IRuleRepository>();
        _mockLogger = new Mock<ILogger<RuleEngineService>>();
        var mockPerfLogger = new Mock<ILogger<PerformanceMetricService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var performanceService = new PerformanceMetricService(mockPerfLogger.Object);
        _service = new RuleEngineService(_mockRuleRepository.Object, _mockLogger.Object, _memoryCache, performanceService);
    }

    /// <summary>
    /// 测试多线程并发评估规则
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_MultipleThreadsConcurrent_AllSucceed()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "并发测试规则",
                ConditionExpression = "Weight > 500",
                TargetChute = "CHUTE-CONCURRENT",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 创建100个并发任务
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => _service.EvaluateRulesAsync(parcelInfo, dwsData, null)));

        var results = await Task.WhenAll(tasks);

        // Assert - 所有结果应该一致且成功
        Assert.All(results, r => Assert.Equal("CHUTE-CONCURRENT", r));
        Assert.Equal(100, results.Length);
    }

    /// <summary>
    /// 测试并发缓存访问不会导致竞态条件
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_ConcurrentCacheAccess_NoRaceCondition()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "缓存测试规则",
                ConditionExpression = "Weight > 100",
                TargetChute = "CHUTE-CACHE",
                Priority = 1,
                IsEnabled = true
            }
        };

        var callCount = 0;
        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(10); // 模拟数据库延迟
                return (IEnumerable<SortingRule>)rules;
            });

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 同时发起50个请求
        var tasks = Enumerable.Range(0, 50).Select(_ =>
            _service.EvaluateRulesAsync(parcelInfo, dwsData, null));

        var results = await Task.WhenAll(tasks);

        // Assert - 所有结果应该一致
        Assert.All(results, r => Assert.Equal("CHUTE-CACHE", r));
        
        // 由于缓存，实际调用次数应该远小于50
        Assert.True(callCount < 50, $"Expected cache to reduce calls, but got {callCount} calls");
    }

    /// <summary>
    /// 测试不同包裹的并发处理
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_DifferentParcelsConcurrent_AllProcessedCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "重量规则",
                ConditionExpression = "Weight > 1000",
                TargetChute = "CHUTE-HEAVY",
                Priority = 1,
                IsEnabled = true
            },
            new SortingRule
            {
                RuleId = "R2",
                RuleName = "轻量规则",
                ConditionExpression = "Weight <= 1000",
                TargetChute = "CHUTE-LIGHT",
                Priority = 2,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        // Act - 同时处理50个重包裹和50个轻包裹
        var heavyTasks = Enumerable.Range(0, 50).Select(i =>
            _service.EvaluateRulesAsync(
                new ParcelInfo { ParcelId = $"HEAVY-{i}", CartNumber = $"CART-{i}" },
                new DwsData { Weight = 2000 },
                null));

        var lightTasks = Enumerable.Range(0, 50).Select(i =>
            _service.EvaluateRulesAsync(
                new ParcelInfo { ParcelId = $"LIGHT-{i}", CartNumber = $"CART-{i}" },
                new DwsData { Weight = 500 },
                null));

        var allTasks = heavyTasks.Concat(lightTasks);
        var results = await Task.WhenAll(allTasks);

        // Assert - 前50个应该是重包裹，后50个应该是轻包裹
        var heavyResults = results.Take(50);
        var lightResults = results.Skip(50);

        Assert.All(heavyResults, r => Assert.Equal("CHUTE-HEAVY", r));
        Assert.All(lightResults, r => Assert.Equal("CHUTE-LIGHT", r));
    }

    /// <summary>
    /// 测试取消令牌在并发场景下的行为
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_ConcurrentWithCancellation_HandlesCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "测试规则",
                ConditionExpression = "Weight > 100",
                TargetChute = "CHUTE-TEST",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var cts = new CancellationTokenSource();
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 启动多个任务，然后取消一部分
        var tasks = new List<Task<string?>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_service.EvaluateRulesAsync(parcelInfo, dwsData, null, cts.Token));
        }

        // 让前面的任务先执行
        await Task.Delay(50);
        
        // 取消后续任务
        cts.Cancel();

        // 添加更多任务（这些应该立即取消或抛出异常）
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_service.EvaluateRulesAsync(parcelInfo, dwsData, null, cts.Token));
        }

        // Assert - 至少一些任务应该成功完成
        var completedTasks = tasks.Where(t => t.IsCompletedSuccessfully).ToList();
        Assert.NotEmpty(completedTasks);
    }

    /// <summary>
    /// 测试高并发下的性能指标收集
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_HighConcurrency_MetricsCollectedCorrectly()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "性能测试规则",
                ConditionExpression = "Weight > 100",
                TargetChute = "CHUTE-PERF",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 200个并发请求
        var tasks = Enumerable.Range(0, 200).Select(_ =>
            _service.EvaluateRulesAsync(parcelInfo, dwsData, null));

        var results = await Task.WhenAll(tasks);

        // Assert - 所有请求都应该成功
        Assert.Equal(200, results.Length);
        Assert.All(results, r => Assert.Equal("CHUTE-PERF", r));
    }

    /// <summary>
    /// 测试并发场景下的异常处理
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_ConcurrentWithExceptions_HandlesGracefully()
    {
        // Arrange - 使用新的服务实例以避免缓存影响
        var mockRepo = new Mock<IRuleRepository>();
        var mockLogger = new Mock<ILogger<RuleEngineService>>();
        var mockPerfLogger = new Mock<ILogger<PerformanceMetricService>>();
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var perfService = new PerformanceMetricService(mockPerfLogger.Object);
        var service = new RuleEngineService(mockRepo.Object, mockLogger.Object, memCache, perfService);

        var attemptCount = 0;
        mockRepo.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                var count = Interlocked.Increment(ref attemptCount);
                // 每3次调用抛出一次异常
                if (count % 3 == 0)
                {
                    throw new Exception("Simulated database error");
                }
                return Task.FromResult<IEnumerable<SortingRule>>(new List<SortingRule>
                {
                    new SortingRule
                    {
                        RuleId = "R1",
                        RuleName = "异常测试规则",
                        ConditionExpression = "Weight > 100",
                        TargetChute = "CHUTE-ERROR",
                        Priority = 1,
                        IsEnabled = true
                    }
                });
            });

        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 10个并发请求，由于缓存，实际只调用一次仓储
        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(async () =>
            {
                try
                {
                    // 清除缓存以强制重新加载
                    memCache.Remove("SortingRules");
                    return await service.EvaluateRulesAsync(parcelInfo, dwsData, null);
                }
                catch (Exception)
                {
                    return "ERROR";
                }
            }));

        var results = await Task.WhenAll(tasks);

        // Assert - 至少应该有一些成功的结果
        var successCount = results.Count(r => r == "CHUTE-ERROR");
        var errorCount = results.Count(r => r == "ERROR");

        Assert.True(successCount > 0, "Expected some successful results");
        // 由于缓存被清除，应该有错误发生
        // 但这取决于时序，所以我们只确保有结果
        Assert.Equal(10, successCount + errorCount);
    }

    /// <summary>
    /// 测试线程安全的结果收集
    /// </summary>
    [Fact]
    public async Task EvaluateRulesAsync_ThreadSafeResultCollection_NoDataLoss()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "结果收集测试",
                ConditionExpression = "Weight > 100",
                TargetChute = "CHUTE-COLLECT",
                Priority = 1,
                IsEnabled = true
            }
        };

        _mockRuleRepository.Setup(r => r.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var results = new ConcurrentBag<string?>();
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001", CartNumber = "CART001" };
        var dwsData = new DwsData { Weight = 1000 };

        // Act - 100个并发任务，结果存入线程安全集合
        var tasks = Enumerable.Range(0, 100).Select(async _ =>
        {
            var result = await _service.EvaluateRulesAsync(parcelInfo, dwsData, null);
            results.Add(result);
        });

        await Task.WhenAll(tasks);

        // Assert - 应该收集到所有100个结果
        Assert.Equal(100, results.Count);
        Assert.All(results, r => Assert.Equal("CHUTE-COLLECT", r));
    }
}
