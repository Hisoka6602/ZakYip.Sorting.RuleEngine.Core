# 压力测试项目 (Load Tests)

本项目包含针对ZakYip分拣规则引擎的压力测试和性能测试。

## 测试类别

### 1. RuleEngineLoadTests - 规则引擎负载测试

- **ParcelProcessing_LoadTest**: 包裹处理API的吞吐量和响应时间测试
  - 模拟每秒100个请求注入
  - 持续时间: 1分钟注入 + 2分钟保持50个并发
  - 性能目标: RPS >= 50, P99延迟 <= 1000ms

- **RuleEvaluation_StressTest**: 规则评估性能测试
  - 逐步增加负载到200个并发连接
  - 持续时间: 1分钟爬坡 + 2分钟保持
  - 性能目标: 处理 > 1000个请求, 错误率 < 5%

- **ConcurrentRequests_StabilityTest**: 并发请求稳定性测试
  - 100个并发用户持续5分钟
  - 测试多个端点的稳定性
  - 性能目标: 成功率 >= 99%

### 2. DatabasePerformanceTests - 数据库性能测试

- **BulkRuleLoading_PerformanceTest**: 大批量规则加载性能
  - 测试加载1000条规则的性能
  - 性能目标: < 1000ms

- **ChuteStatisticsQuery_PerformanceTest**: 格口统计查询性能
  - 查询100个格口的统计数据
  - 性能目标: < 500ms

- **ConcurrentWrites_PerformanceTest**: 并发写入性能
  - 100个并发写入任务
  - 性能目标: < 5000ms完成所有写入

- **LogWriteThroughput_Test**: 日志写入吞吐量测试
  - 写入10000条日志
  - 性能目标: >= 1000条/秒

## 运行测试

### 前置条件

1. 确保服务正在运行: `http://localhost:5000`
2. 安装依赖: `dotnet restore`

### 运行所有测试

```bash
dotnet test ZakYip.Sorting.RuleEngine.LoadTests
```

### 运行特定测试

```bash
# 运行规则引擎负载测试
dotnet test --filter "FullyQualifiedName~RuleEngineLoadTests"

# 运行数据库性能测试
dotnet test --filter "FullyQualifiedName~DatabasePerformanceTests"
```

### 生成测试报告

NBomber会自动生成HTML报告，保存在 `./reports/` 目录下。

## 使用的工具

- **NBomber**: 现代化的.NET负载测试框架
- **NBomber.Http**: HTTP客户端插件
- **xUnit**: 测试框架

## 配置压力测试参数

可以在测试代码中调整以下参数:

- `rate`: 请求注入速率
- `interval`: 注入间隔
- `during`: 持续时间
- `copies`: 并发连接数

## 性能基准

当前系统的性能目标:

| 指标 | 目标值 |
|------|--------|
| RPS (每秒请求数) | >= 50 |
| P99延迟 | <= 1000ms |
| 成功率 | >= 99% |
| 错误率 | < 5% |
| 规则加载 (1000条) | < 1000ms |
| 统计查询 (100个格口) | < 500ms |
| 日志吞吐量 | >= 1000条/秒 |

## 注意事项

1. 压力测试会对系统产生真实的负载，建议在测试环境中运行
2. 确保有足够的系统资源(CPU、内存、网络)
3. 某些测试需要实际的数据库连接才能运行
4. 可以根据实际硬件调整性能目标

## 扩展测试

可以基于现有测试创建更多场景:

- 混合负载测试 (多个端点同时测试)
- 长时间稳定性测试 (运行数小时)
- 峰值流量测试 (模拟突发流量)
- 资源限制测试 (在资源受限环境下测试)
