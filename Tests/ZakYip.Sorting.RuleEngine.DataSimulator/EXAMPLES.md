# 数据模拟器使用示例

本文档提供了ZakYip.Sorting.RuleEngine.DataSimulator的详细使用示例和最佳实践。

## 场景1: 功能验证测试

### 目标
验证系统基本功能是否正常工作。

### 步骤

1. **启动分拣规则引擎服务**
```bash
cd Service/ZakYip.Sorting.RuleEngine.Service
dotnet run
```

2. **启动数据模拟器**
```bash
cd Tests/ZakYip.Sorting.RuleEngine.DataSimulator
dotnet run
```

3. **执行单次测试**
   - 选择菜单选项 `1. 发送单个分拣机信号`
   - 查看返回结果和响应时间
   - 选择菜单选项 `4. 发送单个DWS数据`
   - 验证DWS数据是否成功接收

4. **执行完整流程测试**
   - 选择菜单选项 `7. 完整流程模拟（包裹+DWS）`
   - 输入模拟数量: `5`
   - 输入间隔时间: `500` (毫秒)
   - 观察包裹和DWS数据的匹配过程

### 预期结果
- 所有请求成功率 = 100%
- 响应时间 < 100ms
- 无错误日志

---

## 场景2: 批量处理测试

### 目标
测试系统批量处理包裹的能力。

### 步骤

1. **小批量测试 (10个包裹)**
   - 选择选项 `2. 批量发送分拣机信号`
   - 输入数量: `10`
   - 输入间隔: `100` ms
   - 记录结果

2. **中批量测试 (100个包裹)**
   - 选择选项 `2. 批量发送分拣机信号`
   - 输入数量: `100`
   - 输入间隔: `50` ms
   - 观察成功率和延迟

3. **大批量测试 (1000个包裹)**
   - 选择选项 `2. 批量发送分拣机信号`
   - 输入数量: `1000`
   - 输入间隔: `10` ms
   - 分析性能指标

### 预期结果
| 批量大小 | 成功率 | 平均延迟 | 最大延迟 |
|---------|--------|----------|----------|
| 10      | 100%   | < 50ms   | < 100ms  |
| 100     | ≥ 99%  | < 100ms  | < 500ms  |
| 1000    | ≥ 95%  | < 200ms  | < 1000ms |

---

## 场景3: 性能压力测试

### 目标
测试系统在高并发场景下的性能表现。

### 步骤

1. **低压力测试 (50包裹/秒)**
   - 选择选项 `3. 分拣机压力测试`
   - 持续时间: `60` 秒
   - 目标速率: `50` 包裹/秒
   - 记录P50、P95、P99延迟

2. **中压力测试 (100包裹/秒)**
   - 选择选项 `3. 分拣机压力测试`
   - 持续时间: `120` 秒
   - 目标速率: `100` 包裹/秒
   - 监控系统资源使用

3. **高压力测试 (200包裹/秒)**
   - 选择选项 `3. 分拣机压力测试`
   - 持续时间: `180` 秒
   - 目标速率: `200` 包裹/秒
   - 观察系统瓶颈

4. **极限测试 (500包裹/秒)**
   - 选择选项 `3. 分拣机压力测试`
   - 持续时间: `60` 秒
   - 目标速率: `500` 包裹/秒
   - 找出系统容量上限

### 监控指标

在压力测试期间，使用以下命令监控系统：

```bash
# 监控CPU和内存
top

# 监控网络连接
netstat -an | grep 5000 | wc -l

# 监控数据库连接
mysql -u root -p -e "SHOW PROCESSLIST;"
```

### 性能基准

| 速率 (包裹/秒) | 成功率 | P50延迟 | P95延迟 | P99延迟 |
|---------------|--------|---------|---------|---------|
| 50            | ≥ 99%  | < 20ms  | < 50ms  | < 100ms |
| 100           | ≥ 98%  | < 30ms  | < 100ms | < 200ms |
| 200           | ≥ 95%  | < 50ms  | < 200ms | < 500ms |
| 500           | ≥ 90%  | < 100ms | < 500ms | < 1000ms|

---

## 场景4: 稳定性测试

### 目标
验证系统长时间运行的稳定性。

### 步骤

1. **启动持续测试**
   - 选择选项 `3. 分拣机压力测试`
   - 持续时间: `3600` 秒 (1小时)
   - 目标速率: `50` 包裹/秒
   - 预期总数: 180,000个包裹

2. **监控关键指标**
   - 每10分钟记录一次成功率
   - 监控内存使用趋势
   - 检查是否有内存泄漏
   - 观察延迟分布变化

3. **分析结果**
   - 计算1小时内的平均成功率
   - 检查延迟是否随时间增加
   - 验证系统是否出现降级

### 预期结果
- 1小时成功率 ≥ 99%
- 内存使用稳定（无持续增长）
- P99延迟无明显增长趋势
- 无系统崩溃或重启

---

## 场景5: DWS数据模拟测试

### 目标
专门测试DWS数据处理能力。

### 步骤

1. **连接测试**
   - 选择选项 `4. 发送单个DWS数据`
   - 验证TCP连接是否成功
   - 确认数据格式正确

2. **批量DWS测试**
   - 选择选项 `5. 批量发送DWS数据`
   - 输入数量: `100`
   - 输入间隔: `50` ms
   - 检查数据接收情况

3. **DWS压力测试**
   - 选择选项 `6. DWS压力测试`
   - 持续时间: `60` 秒
   - 目标速率: `100` 数据/秒
   - 分析TCP连接稳定性

### 数据验证

在服务端检查接收到的DWS数据：

```sql
-- 查询最近的DWS通信日志
SELECT * FROM DwsCommunicationLog 
ORDER BY CreatedAt DESC 
LIMIT 10;

-- 统计成功率
SELECT 
    COUNT(*) as Total,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) as Success,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) as SuccessRate
FROM DwsCommunicationLog
WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 1 HOUR);
```

---

## 配置优化建议

### 1. 开发环境配置

```json
{
  "Simulator": {
    "HttpApiUrl": "http://localhost:5000",
    "DwsTcpHost": "127.0.0.1",
    "DwsTcpPort": 8001,
    "StressTest": {
      "Duration": 30,
      "RatePerSecond": 10,
      "WarmupSeconds": 2
    }
  }
}
```

### 2. 测试环境配置

```json
{
  "Simulator": {
    "HttpApiUrl": "http://test-server:5000",
    "DwsTcpHost": "test-server",
    "DwsTcpPort": 8001,
    "StressTest": {
      "Duration": 120,
      "RatePerSecond": 100,
      "WarmupSeconds": 5
    }
  }
}
```

### 3. 生产压测配置

```json
{
  "Simulator": {
    "HttpApiUrl": "http://prod-server:5000",
    "DwsTcpHost": "prod-server",
    "DwsTcpPort": 8001,
    "StressTest": {
      "Duration": 300,
      "RatePerSecond": 200,
      "WarmupSeconds": 10
    },
    "DataGeneration": {
      "WeightMin": 50,
      "WeightMax": 10000,
      "LengthMin": 50,
      "LengthMax": 1000,
      "WidthMin": 50,
      "WidthMax": 1000,
      "HeightMin": 20,
      "HeightMax": 500
    }
  }
}
```

---

## 常见问题和解决方案

### Q1: 压力测试时成功率突然下降
**可能原因:**
- 数据库连接池耗尽
- 系统CPU/内存资源不足
- 网络带宽瓶颈

**解决方案:**
1. 检查数据库连接池配置
2. 增加系统资源
3. 降低测试速率
4. 启用数据库缓存

### Q2: TCP连接频繁断开
**可能原因:**
- 防火墙配置问题
- TCP超时设置过短
- 服务端连接数限制

**解决方案:**
1. 检查防火墙规则
2. 增加TCP超时时间
3. 调整服务端最大连接数
4. 使用连接池

### Q3: 延迟P99指标过高
**可能原因:**
- 数据库慢查询
- GC暂停时间过长
- 磁盘I/O瓶颈

**解决方案:**
1. 优化数据库索引
2. 调整GC策略
3. 使用SSD存储
4. 启用查询缓存

---

## 测试报告模板

### 基本信息
- **测试日期**: 2025-11-09
- **测试人员**: [姓名]
- **测试环境**: [开发/测试/生产]
- **系统版本**: v1.14.9

### 测试配置
- **HTTP API**: http://localhost:5000
- **DWS TCP**: 127.0.0.1:8001
- **测试持续时间**: 60秒
- **目标速率**: 100包裹/秒

### 测试结果
| 指标 | 数值 | 是否达标 |
|------|------|----------|
| 总发送数 | 6000 | - |
| 成功数 | 5970 | ✓ |
| 失败数 | 30 | ✓ |
| 成功率 | 99.5% | ✓ (≥95%) |
| 实际速率 | 99.5/秒 | ✓ |
| 平均延迟 | 45ms | ✓ (<100ms) |
| P50延迟 | 42ms | ✓ |
| P95延迟 | 89ms | ✓ |
| P99延迟 | 156ms | ✓ (<500ms) |

### 系统资源
- **CPU使用率**: 45%
- **内存使用**: 2.3GB / 8GB
- **磁盘I/O**: 正常
- **网络吞吐**: 5MB/s

### 结论
系统在100包裹/秒的压力下表现良好，所有指标均达标。建议进行更高速率的测试以找出系统容量上限。

---

## 自动化测试脚本

以下是一个使用PowerShell/Bash的自动化测试脚本示例：

### PowerShell版本
```powershell
# run-stress-test.ps1
param(
    [int]$Duration = 60,
    [int]$Rate = 100
)

Write-Host "Starting stress test..."
Write-Host "Duration: $Duration seconds"
Write-Host "Rate: $Rate parcels/second"

# 启动模拟器（需要修改为自动化输入）
cd Tests/ZakYip.Sorting.RuleEngine.DataSimulator
dotnet run

# 分析结果（需要解析输出）
Write-Host "Test completed. Check the results above."
```

### Bash版本
```bash
#!/bin/bash
# run-stress-test.sh

DURATION=${1:-60}
RATE=${2:-100}

echo "Starting stress test..."
echo "Duration: $DURATION seconds"
echo "Rate: $RATE parcels/second"

cd Tests/ZakYip.Sorting.RuleEngine.DataSimulator
dotnet run

echo "Test completed. Check the results above."
```

---

## 总结

本数据模拟器提供了全面的测试能力，从单次验证到大规模压力测试。通过遵循本文档的示例和最佳实践，您可以：

1. ✅ 验证系统功能正确性
2. ✅ 评估系统性能表现
3. ✅ 发现系统瓶颈和问题
4. ✅ 优化系统配置
5. ✅ 确保生产环境稳定性

建议在每次重大更新后运行完整的测试套件，确保系统质量。
