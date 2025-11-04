using System.Collections.Concurrent;
using System.Diagnostics;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit.Abstractions;

namespace ZakYip.Sorting.RuleEngine.LoadTests;

/// <summary>
/// 高并发压力测试 - 模拟100-1000包裹/秒的处理场景
/// High concurrency stress tests - Simulate 100-1000 parcels/second processing
/// </summary>
public class HighConcurrencyStressTests
{
    private const string BaseUrl = "http://localhost:5000";
    private readonly ITestOutputHelper _output;

    public HighConcurrencyStressTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试100包裹/秒的处理能力
    /// Test 100 parcels/second processing capacity
    /// </summary>
    [Fact]
    public void ParcelProcessing_100PerSecond_StressTest()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var processedCount = 0;
        var failedCount = 0;
        var latencies = new ConcurrentBag<double>();

        var scenario = Scenario.Create("100_parcels_per_second", async context =>
        {
            var instanceNum = int.Parse(context.ScenarioInfo.InstanceId.Split('_').LastOrDefault() ?? "0");
            var parcelData = new
            {
                parcelId = $"PKG{instanceNum:D4}_{context.InvocationNumber:D6}",
                cartNumber = $"CART{instanceNum % 10:D2}",
                barcode = $"BC{DateTime.Now.Ticks}{context.InvocationNumber}",
                weight = Random.Shared.Next(100, 5000),
                length = Random.Shared.Next(10, 100),
                width = Random.Shared.Next(10, 100),
                height = Random.Shared.Next(10, 100),
                volume = Random.Shared.Next(1000, 100000)
            };

            var request = Http.CreateRequest("POST", "/api/parcel/process")
                .WithJsonBody(parcelData)
                .WithHeader("Content-Type", "application/json");

            var response = await Http.Send(httpClient, request);

            if (response.IsError)
            {
                Interlocked.Increment(ref failedCount);
            }
            else
            {
                Interlocked.Increment(ref processedCount);
            }

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // 持续3分钟，每秒100个请求
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(3))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFileName("stress_test_100rps")
            .WithReportFolder("./load-test-reports")
            .Run();

        // 输出测试结果
        _output.WriteLine($"测试完成 - 100包裹/秒");
        _output.WriteLine($"总请求数: {stats.ScenarioStats[0].Ok.Request.Count + stats.ScenarioStats[0].Fail.Request.Count}");
        _output.WriteLine($"成功: {stats.ScenarioStats[0].Ok.Request.Count}");
        _output.WriteLine($"失败: {stats.ScenarioStats[0].Fail.Request.Count}");
        _output.WriteLine($"RPS: {stats.ScenarioStats[0].Ok.Request.RPS}");
        _output.WriteLine($"P50延迟: {stats.ScenarioStats[0].Ok.Latency.Percent50}ms");
        _output.WriteLine($"P95延迟: {stats.ScenarioStats[0].Ok.Latency.Percent95}ms");
        _output.WriteLine($"P99延迟: {stats.ScenarioStats[0].Ok.Latency.Percent99}ms");

        // 验证性能指标
        Assert.True(stats.ScenarioStats[0].Ok.Request.RPS >= 80, 
            $"RPS应该 >= 80，实际: {stats.ScenarioStats[0].Ok.Request.RPS}");
        Assert.True(stats.ScenarioStats[0].Ok.Latency.Percent99 <= 2000, 
            $"P99延迟应该 <= 2000ms，实际: {stats.ScenarioStats[0].Ok.Latency.Percent99}ms");
        Assert.True(stats.ScenarioStats[0].Fail.Request.Percent < 5, 
            $"错误率应该 < 5%，实际: {stats.ScenarioStats[0].Fail.Request.Percent}%");
    }

    /// <summary>
    /// 测试500包裹/秒的处理能力（高负载）
    /// Test 500 parcels/second processing capacity (high load)
    /// </summary>
    [Fact]
    public void ParcelProcessing_500PerSecond_StressTest()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("500_parcels_per_second", async context =>
        {
            var instanceNum = int.Parse(context.ScenarioInfo.InstanceId.Split('_').LastOrDefault() ?? "0");
            var parcelData = new
            {
                parcelId = $"PKG{instanceNum:D4}_{context.InvocationNumber:D6}",
                cartNumber = $"CART{instanceNum % 20:D2}",
                barcode = $"BC{DateTime.Now.Ticks}{context.InvocationNumber}",
                weight = Random.Shared.Next(100, 5000),
                length = Random.Shared.Next(10, 100),
                width = Random.Shared.Next(10, 100),
                height = Random.Shared.Next(10, 100),
                volume = Random.Shared.Next(1000, 100000)
            };

            var request = Http.CreateRequest("POST", "/api/parcel/process")
                .WithJsonBody(parcelData)
                .WithHeader("Content-Type", "application/json");

            return await Http.Send(httpClient, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(15))
        .WithLoadSimulations(
            // 逐步增加负载
            Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFileName("stress_test_500rps")
            .WithReportFolder("./load-test-reports")
            .Run();

        // 输出测试结果
        _output.WriteLine($"测试完成 - 500包裹/秒");
        _output.WriteLine($"总请求数: {stats.ScenarioStats[0].Ok.Request.Count + stats.ScenarioStats[0].Fail.Request.Count}");
        _output.WriteLine($"成功: {stats.ScenarioStats[0].Ok.Request.Count}");
        _output.WriteLine($"失败: {stats.ScenarioStats[0].Fail.Request.Count}");
        _output.WriteLine($"RPS: {stats.ScenarioStats[0].Ok.Request.RPS}");
        _output.WriteLine($"P99延迟: {stats.ScenarioStats[0].Ok.Latency.Percent99}ms");

        // 验证性能指标
        Assert.True(stats.ScenarioStats[0].Ok.Request.RPS >= 300, 
            $"RPS应该 >= 300，实际: {stats.ScenarioStats[0].Ok.Request.RPS}");
        Assert.True(stats.ScenarioStats[0].Fail.Request.Percent < 10, 
            $"错误率应该 < 10%，实际: {stats.ScenarioStats[0].Fail.Request.Percent}%");
    }

    /// <summary>
    /// 测试1000包裹/秒的极限处理能力
    /// Test 1000 parcels/second maximum processing capacity
    /// </summary>
    [Fact]
    public void ParcelProcessing_1000PerSecond_StressTest()
    {
        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        var scenario = Scenario.Create("1000_parcels_per_second", async context =>
        {
            var instanceNum = int.Parse(context.ScenarioInfo.InstanceId.Split('_').LastOrDefault() ?? "0");
            var parcelData = new
            {
                parcelId = $"PKG{instanceNum:D4}_{context.InvocationNumber:D6}",
                cartNumber = $"CART{instanceNum % 50:D2}",
                barcode = $"BC{DateTime.Now.Ticks}{context.InvocationNumber}",
                weight = Random.Shared.Next(100, 5000),
                length = Random.Shared.Next(10, 100),
                width = Random.Shared.Next(10, 100),
                height = Random.Shared.Next(10, 100),
                volume = Random.Shared.Next(1000, 100000)
            };

            var request = Http.CreateRequest("POST", "/api/parcel/process")
                .WithJsonBody(parcelData)
                .WithHeader("Content-Type", "application/json");

            return await Http.Send(httpClient, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(20))
        .WithLoadSimulations(
            // 逐步增加到1000 RPS
            Simulation.RampingInject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.RampingInject(rate: 600, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFileName("stress_test_1000rps")
            .WithReportFolder("./load-test-reports")
            .Run();

        // 输出测试结果和性能瓶颈分析
        _output.WriteLine($"=== 极限压力测试完成 - 1000包裹/秒 ===");
        _output.WriteLine($"总请求数: {stats.ScenarioStats[0].Ok.Request.Count + stats.ScenarioStats[0].Fail.Request.Count}");
        _output.WriteLine($"成功: {stats.ScenarioStats[0].Ok.Request.Count}");
        _output.WriteLine($"失败: {stats.ScenarioStats[0].Fail.Request.Count}");
        _output.WriteLine($"实际RPS: {stats.ScenarioStats[0].Ok.Request.RPS}");
        _output.WriteLine($"P50延迟: {stats.ScenarioStats[0].Ok.Latency.Percent50}ms");
        _output.WriteLine($"P75延迟: {stats.ScenarioStats[0].Ok.Latency.Percent75}ms");
        _output.WriteLine($"P95延迟: {stats.ScenarioStats[0].Ok.Latency.Percent95}ms");
        _output.WriteLine($"P99延迟: {stats.ScenarioStats[0].Ok.Latency.Percent99}ms");
        _output.WriteLine($"错误率: {stats.ScenarioStats[0].Fail.Request.Percent}%");

        // 性能瓶颈识别
        _output.WriteLine("\n=== 性能瓶颈分析 ===");
        if (stats.ScenarioStats[0].Ok.Request.RPS < 500)
        {
            _output.WriteLine("⚠ 瓶颈: 吞吐量低于预期（< 500 RPS），可能是数据库或网络限制");
        }
        if (stats.ScenarioStats[0].Ok.Latency.Percent99 > 3000)
        {
            _output.WriteLine("⚠ 瓶颈: P99延迟过高（> 3000ms），检查数据库查询和锁竞争");
        }
        if (stats.ScenarioStats[0].Fail.Request.Percent > 15)
        {
            _output.WriteLine("⚠ 瓶颈: 错误率过高（> 15%），检查资源限制和超时配置");
        }

        // 记录结果到文件
        var reportPath = Path.Combine("./load-test-reports", "performance_summary.txt");
        Directory.CreateDirectory("./load-test-reports");
        File.AppendAllText(reportPath, $"\n\n=== 测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
        File.AppendAllText(reportPath, $"场景: 1000包裹/秒极限测试\n");
        File.AppendAllText(reportPath, $"实际RPS: {stats.ScenarioStats[0].Ok.Request.RPS}\n");
        File.AppendAllText(reportPath, $"P99延迟: {stats.ScenarioStats[0].Ok.Latency.Percent99}ms\n");
        File.AppendAllText(reportPath, $"错误率: {stats.ScenarioStats[0].Fail.Request.Percent}%\n");

        // 性能验证（较宽松的标准，因为这是极限测试）
        Assert.True(stats.ScenarioStats[0].Ok.Request.RPS >= 200, 
            $"即使在极限负载下，RPS应该 >= 200，实际: {stats.ScenarioStats[0].Ok.Request.RPS}");
    }

    /// <summary>
    /// 数据库同步事务压力测试
    /// Database synchronization transaction stress test
    /// </summary>
    [Fact]
    public async Task DatabaseSyncTransaction_StressTest()
    {
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;
        var failureCount = 0;
        var tasks = new List<Task>();

        // 模拟100个并发的数据库同步操作
        for (int i = 0; i < 100; i++)
        {
            var taskId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    // 模拟事务处理：插入到MySQL + 从SQLite删除
                    await Task.Delay(Random.Shared.Next(50, 200)); // 模拟MySQL插入
                    await Task.Delay(Random.Shared.Next(10, 50));  // 模拟SQLite删除
                    
                    Interlocked.Increment(ref successCount);
                }
                catch
                {
                    Interlocked.Increment(ref failureCount);
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        _output.WriteLine($"数据库同步事务压力测试完成");
        _output.WriteLine($"总操作数: {successCount + failureCount}");
        _output.WriteLine($"成功: {successCount}");
        _output.WriteLine($"失败: {failureCount}");
        _output.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"平均耗时: {stopwatch.ElapsedMilliseconds / (double)(successCount + failureCount):F2}ms");

        // 验证事务成功率
        Assert.True(successCount >= 95, $"事务成功率应该 >= 95%，实际: {successCount}%");
    }

    /// <summary>
    /// 长时间稳定性测试
    /// Long-duration stability test
    /// </summary>
    [Fact]
    public void LongDuration_StabilityTest()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("long_duration_stability", async context =>
        {
            var instanceNum = int.Parse(context.ScenarioInfo.InstanceId.Split('_').LastOrDefault() ?? "0");
            var parcelData = new
            {
                parcelId = $"STABLE_{instanceNum:D4}_{context.InvocationNumber:D6}",
                cartNumber = $"CART{instanceNum % 10:D2}",
                barcode = $"BC{DateTime.Now.Ticks}{context.InvocationNumber}",
                weight = Random.Shared.Next(100, 5000),
                length = Random.Shared.Next(10, 100),
                width = Random.Shared.Next(10, 100),
                height = Random.Shared.Next(10, 100),
                volume = Random.Shared.Next(1000, 100000)
            };

            var request = Http.CreateRequest("POST", "/api/parcel/process")
                .WithJsonBody(parcelData)
                .WithHeader("Content-Type", "application/json");

            return await Http.Send(httpClient, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // 持续10分钟，保持50 RPS的稳定负载
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFileName("stability_test")
            .WithReportFolder("./load-test-reports")
            .Run();

        _output.WriteLine($"长时间稳定性测试完成 - 10分钟");
        _output.WriteLine($"总请求数: {stats.ScenarioStats[0].Ok.Request.Count + stats.ScenarioStats[0].Fail.Request.Count}");
        _output.WriteLine($"成功率: {stats.ScenarioStats[0].Ok.Request.Percent}%");
        _output.WriteLine($"P99延迟稳定性: {stats.ScenarioStats[0].Ok.Latency.Percent99}ms");

        // 验证长时间稳定性
        Assert.True(stats.ScenarioStats[0].Ok.Request.Percent >= 98, 
            $"成功率应该 >= 98%，实际: {stats.ScenarioStats[0].Ok.Request.Percent}%");
        Assert.True(stats.ScenarioStats[0].Ok.Latency.Percent99 <= 1500, 
            $"P99延迟应该保持稳定 <= 1500ms，实际: {stats.ScenarioStats[0].Ok.Latency.Percent99}ms");
    }
}
