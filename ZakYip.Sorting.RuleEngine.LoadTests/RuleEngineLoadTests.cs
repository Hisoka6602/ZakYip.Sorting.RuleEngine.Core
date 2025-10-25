using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace ZakYip.Sorting.RuleEngine.LoadTests;

/// <summary>
/// 规则引擎压力测试
/// Load tests for rule engine processing
/// </summary>
public class RuleEngineLoadTests
{
    private const string BaseUrl = "http://localhost:5000";

    /// <summary>
    /// 测试包裹处理API的吞吐量和响应时间
    /// Test parcel processing API throughput and response time
    /// </summary>
    [Fact]
    public void ParcelProcessing_LoadTest()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("parcel_processing", async context =>
        {
            var parcelData = new
            {
                barcode = $"TEST{context.ScenarioInfo.InstanceId:D10}",
                weight = 1.5m,
                volume = 100.0m,
                destination = "Beijing"
            };

            var request = Http.CreateRequest("POST", "/api/parcel/process")
                .WithJsonBody(parcelData)
                .WithHeader("Content-Type", "application/json");

            var response = await Http.Send(httpClient, request);
            
            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(2))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // 验证性能指标
        Assert.True(stats.ScenarioStats[0].Ok.Request.RPS >= 50, "RPS should be at least 50");
        Assert.True(stats.ScenarioStats[0].Ok.Latency.Percent99 <= 1000, "P99 latency should be <= 1000ms");
    }

    /// <summary>
    /// 规则评估性能测试
    /// Rule evaluation performance test
    /// </summary>
    [Fact]
    public void RuleEvaluation_StressTest()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("rule_evaluation", async context =>
        {
            var parcelData = new
            {
                barcode = $"STRESS{DateTime.UtcNow.Ticks}",
                weight = Random.Shared.NextDouble() * 10,
                volume = Random.Shared.NextDouble() * 1000,
                destination = $"City{Random.Shared.Next(1, 100)}"
            };

            var request = Http.CreateRequest("POST", "/api/parcel/evaluate")
                .WithJsonBody(parcelData)
                .WithHeader("Content-Type", "application/json");

            var response = await Http.Send(httpClient, request);
            
            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 200, during: TimeSpan.FromMinutes(2))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // 验证压力测试结果
        Assert.True(stats.ScenarioStats[0].Ok.Request.Count > 1000, "Should process > 1000 requests");
        Assert.True(stats.ScenarioStats[0].Fail.Request.Percent < 5, "Error rate should be < 5%");
    }

    /// <summary>
    /// 并发请求稳定性测试
    /// Concurrent request stability test
    /// </summary>
    [Fact]
    public void ConcurrentRequests_StabilityTest()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("concurrent_stability", async context =>
        {
            var endpoints = new[] 
            { 
                "/api/chute/list", 
                "/api/rules/list",
                "/api/chutestatistics/overview"
            };
            
            var instanceNum = int.Parse(context.ScenarioInfo.InstanceId.Split('_')[1]);
            var endpoint = endpoints[instanceNum % endpoints.Length];

            var request = Http.CreateRequest("GET", endpoint);
            var response = await Http.Send(httpClient, request);
            
            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(5))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // 验证稳定性
        Assert.True(stats.ScenarioStats[0].Ok.Request.Percent >= 99, "Success rate should be >= 99%");
    }
}
