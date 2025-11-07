# 压力测试指南 (Load Testing Guide)

## 概述 (Overview)

本项目包含全面的压力测试套件，用于验证系统在高并发场景下的性能和稳定性。

This project includes a comprehensive load testing suite to verify system performance and stability under high concurrency scenarios.

## 测试场景 (Test Scenarios)

### 1. 高并发压力测试 (High Concurrency Stress Tests)

#### ParcelProcessing_100PerSecond_StressTest
- **目标**: 测试100包裹/秒的处理能力
- **持续时间**: 3分钟
- **性能指标**:
  - RPS >= 80
  - P99延迟 <= 2000ms
  - 错误率 < 5%

#### ParcelProcessing_500PerSecond_StressTest
- **目标**: 测试500包裹/秒的高负载处理能力
- **持续时间**: 2分钟
- **性能指标**:
  - RPS >= 300
  - 错误率 < 10%

#### ParcelProcessing_1000PerSecond_StressTest
- **目标**: 测试1000包裹/秒的极限处理能力
- **持续时间**: 1分钟
- **性能指标**:
  - RPS >= 200 (在极限负载下)
  - 识别性能瓶颈

#### LongDuration_StabilityTest
- **目标**: 长时间稳定性测试
- **持续时间**: 10分钟
- **负载**: 50并发请求
- **性能指标**:
  - 成功率 >= 98%
  - P99延迟 <= 1500ms

#### DatabaseSyncTransaction_StressTest
- **目标**: 数据库同步事务压力测试
- **并发数**: 100个并发操作
- **性能指标**:
  - 事务成功率 >= 95%

## 运行测试 (Running Tests)

### 前置条件 (Prerequisites)

1. 确保系统服务正在运行 (Ensure the system service is running):
```bash
cd ZakYip.Sorting.RuleEngine.Service
dotnet run
```

2. 服务应该在 http://localhost:5000 上运行

### 运行所有压力测试 (Run All Load Tests)

```bash
cd ZakYip.Sorting.RuleEngine.LoadTests
dotnet test --filter "FullyQualifiedName~HighConcurrencyStressTests"
```

### 运行特定测试 (Run Specific Test)

```bash
# 100 RPS 测试
dotnet test --filter "FullyQualifiedName~ParcelProcessing_100PerSecond_StressTest"

# 500 RPS 测试
dotnet test --filter "FullyQualifiedName~ParcelProcessing_500PerSecond_StressTest"

# 1000 RPS 极限测试
dotnet test --filter "FullyQualifiedName~ParcelProcessing_1000PerSecond_StressTest"

# 长时间稳定性测试
dotnet test --filter "FullyQualifiedName~LongDuration_StabilityTest"

# 数据库事务测试
dotnet test --filter "FullyQualifiedName~DatabaseSyncTransaction_StressTest"
```

## 测试报告 (Test Reports)

测试完成后，报告将生成在 `./load-test-reports/` 目录下：

- `stress_test_100rps_*.html` - 100 RPS 测试报告
- `stress_test_500rps_*.html` - 500 RPS 测试报告
- `stress_test_1000rps_*.html` - 1000 RPS 极限测试报告
- `stability_test_*.html` - 稳定性测试报告
- `performance_summary.txt` - 性能摘要报告

## 性能瓶颈识别 (Performance Bottleneck Identification)

### 自动识别的瓶颈类型:

1. **吞吐量瓶颈**
   - 如果 RPS < 500，可能是数据库或网络限制

2. **延迟瓶颈**
   - 如果 P99延迟 > 3000ms，检查:
     - 数据库查询优化
     - 锁竞争问题
     - 缓存策略

3. **错误率瓶颈**
   - 如果错误率 > 15%，检查:
     - 资源限制（CPU、内存、连接数）
     - 超时配置
     - 数据库连接池大小

## 优化建议 (Optimization Recommendations)

### 数据库优化:
- 使用事务确保数据一致性
- 批量处理减少数据库往返
- 合理的索引配置
- 连接池优化

### 应用优化:
- 异步处理
- 缓存策略
- 对象池
- 负载均衡

### 基础设施优化:
- 增加服务器资源
- 数据库读写分离
- 分布式缓存
- CDN加速

## 数据库事务安全性 (Database Transaction Safety)

本版本增强了数据库同步的事务安全性:

### 特性:
1. **原子性事务**: MySQL插入和SQLite删除在同一事务中
2. **防止数据丢失**: 断电或崩溃时自动回滚
3. **防止重复数据**: 成功同步后立即删除
4. **批量处理**: 1000条记录/批次，优化性能

### 测试验证:
运行 `DatabaseSyncTransaction_StressTest` 来验证事务安全性。

## 持续监控 (Continuous Monitoring)

建议在生产环境中:

1. 定期运行压力测试（每月或每次重大更新后）
2. 监控关键指标:
   - 响应时间 (P50, P95, P99)
   - 吞吐量 (RPS)
   - 错误率
   - 资源使用率

3. 建立性能基准线
4. 设置性能回归告警

## 常见问题 (FAQ)

### Q: 测试失败怎么办?
A: 检查:
1. 服务是否正在运行
2. 数据库连接是否正常
3. 系统资源是否充足
4. 网络连接是否稳定

### Q: 如何调整测试参数?
A: 编辑测试文件中的参数:
- `rate`: 每秒请求数
- `during`: 测试持续时间
- `copies`: 并发数

### Q: 如何解读测试结果?
A: 重点关注:
- **RPS**: 实际处理能力
- **P99延迟**: 99%请求的响应时间
- **错误率**: 失败请求的百分比
- **稳定性**: 长时间运行的表现

## 技术栈 (Tech Stack)

- **NBomber**: 负载测试框架
- **xUnit**: 测试运行器
- **.NET 8.0**: 运行时环境

## 贡献 (Contributing)

欢迎添加新的测试场景或改进现有测试！

## 许可证 (License)

MIT License
