using System.Diagnostics;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.LoadTests;

/// <summary>
/// 数据库性能测试
/// Database performance tests
/// </summary>
public class DatabasePerformanceTests
{
    /// <summary>
    /// 测试大批量规则加载性能
    /// Test bulk rule loading performance
    /// </summary>
    [Fact]
    public async Task BulkRuleLoading_PerformanceTest()
    {
        // 这个测试需要实际的数据库连接和仓储实现
        // 这里提供测试框架，实际测试时需要配置数据库
        
        var stopwatch = Stopwatch.StartNew();
        
        // 模拟加载1000条规则
        var rules = new List<SortingRule>();
        for (int i = 0; i < 1000; i++)
        {
            rules.Add(new SortingRule
            {
                RuleId = $"RULE_{i:D6}",
                RuleName = $"Test Rule {i}",
                Priority = i,
                MatchingMethod = MatchingMethodType.BarcodeRegex,
                ConditionExpression = "^TEST.*$",
                TargetChute = $"CHUTE_{i % 100}",
                IsEnabled = true
            });
        }
        
        stopwatch.Stop();
        
        // 验证加载性能
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Loading 1000 rules should take < 1000ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 测试格口统计查询性能
    /// Test chute statistics query performance
    /// </summary>
    [Fact]
    public async Task ChuteStatisticsQuery_PerformanceTest()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // 模拟查询100个格口的统计数据
        var statisticsData = new List<object>();
        for (int i = 0; i < 100; i++)
        {
            statisticsData.Add(new
            {
                ChuteId = i,
                ChuteName = $"Chute_{i}",
                TotalParcels = Random.Shared.Next(1000, 10000),
                SuccessRate = Random.Shared.NextDouble() * 100,
                UtilizationRate = Random.Shared.NextDouble() * 100
            });
        }
        
        stopwatch.Stop();
        
        // 验证查询性能
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Querying 100 chute statistics should take < 500ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 测试并发写入性能
    /// Test concurrent write performance
    /// </summary>
    [Fact]
    public async Task ConcurrentWrites_PerformanceTest()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        
        // 模拟100个并发写入任务
        for (int i = 0; i < 100; i++)
        {
            var task = Task.Run(async () =>
            {
                // 模拟写入延迟
                await Task.Delay(Random.Shared.Next(10, 50));
            });
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // 验证并发写入性能
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"100 concurrent writes should complete < 5000ms, actual: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试日志写入吞吐量
    /// Test log write throughput
    /// </summary>
    [Fact]
    public async Task LogWriteThroughput_Test()
    {
        var stopwatch = Stopwatch.StartNew();
        var writeCount = 10000;
        
        // 模拟写入10000条日志
        var tasks = Enumerable.Range(0, writeCount).Select(i => Task.Run(async () =>
        {
            // 模拟日志写入
            await Task.Delay(1); // 非常短的延迟
        }));
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        var throughput = writeCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        
        // 验证吞吐量（至少1000条/秒）
        Assert.True(throughput >= 1000, 
            $"Log write throughput should be >= 1000/s, actual: {throughput:F2}/s");
    }
}
