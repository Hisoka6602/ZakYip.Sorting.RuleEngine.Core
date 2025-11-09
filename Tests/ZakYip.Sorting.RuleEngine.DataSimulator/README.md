# 分拣机和DWS数据模拟程序

## 概述

ZakYip.Sorting.RuleEngine.DataSimulator 是一个综合的数据模拟工具，用于测试和验证分拣规则引擎系统的运行效果。该工具支持模拟分拣机信号（MQTT/TCP）和DWS（尺寸重量扫描）数据（TCP），并提供压力测试功能。

## 主要功能

### 1. 分拣机信号模拟
- **通信协议**: 支持MQTT和TCP两种协议
- **单次发送**: 发送单个包裹信号到系统
- **批量发送**: 批量发送多个包裹信号
- **压力测试**: 持续以指定速率发送包裹信号（支持100-1000包裹/秒）

### 2. DWS数据模拟
- **通信协议**: TCP
- **单次发送**: 发送单条DWS测量数据
- **批量发送**: 批量发送多条DWS数据
- **压力测试**: 持续以指定速率发送DWS数据

### 3. 完整流程模拟
- 模拟真实场景：先发送包裹信号，延迟后发送对应的DWS数据
- 自动生成匹配的包裹和DWS数据对

### 4. 详细的性能统计
- 成功率统计
- 延迟分析（平均、最小、最大、P50、P95、P99）
- 实时进度显示
- 彩色控制台输出

## 快速开始

### 前置条件

1. .NET 8.0 SDK
2. 确保分拣规则引擎服务正在运行
3. MQTT代理（如使用MQTT模式）或TCP服务器

### 运行模拟器

```bash
cd Tests/ZakYip.Sorting.RuleEngine.DataSimulator
dotnet run
```

或者编译后直接运行：

```bash
dotnet build
../bin/Debug/net8.0/ZakYip.Sorting.RuleEngine.DataSimulator
```

### 配置文件

编辑 `appsettings.json` 来配置模拟器：

```json
{
  "Simulator": {
    "SorterCommunicationType": "MQTT",
    "SorterMqtt": {
      "BrokerHost": "127.0.0.1",
      "BrokerPort": 1883,
      "PublishTopic": "sorter/parcel",
      "ClientId": "DataSimulator",
      "Username": null,
      "Password": null
    },
    "SorterTcp": {
      "Host": "127.0.0.1",
      "Port": 8000
    },
    "DwsTcpHost": "127.0.0.1",
    "DwsTcpPort": 8001,
    "StressTest": {
      "Duration": 60,
      "RatePerSecond": 100,
      "WarmupSeconds": 5
    },
    "DataGeneration": {
      "WeightMin": 100,
      "WeightMax": 5000,
      "LengthMin": 100,
      "LengthMax": 500,
      "WidthMin": 100,
      "WidthMax": 500,
      "HeightMin": 50,
      "HeightMax": 300
    }
  }
}
```

### 配置说明

#### 分拣机通信配置

**SorterCommunicationType**: 选择分拣机通信类型
- `MQTT` - 使用MQTT协议（推荐）
- `TCP` - 使用TCP协议

**SorterMqtt**: MQTT配置（当SorterCommunicationType为MQTT时使用）
- `BrokerHost` - MQTT代理地址
- `BrokerPort` - MQTT代理端口（默认1883）
- `PublishTopic` - 发布主题
- `ClientId` - 客户端ID
- `Username` - 用户名（可选，如不需要认证设为null）
- `Password` - 密码（可选，如不需要认证设为null）

**SorterTcp**: TCP配置（当SorterCommunicationType为TCP时使用）
- `Host` - TCP服务器地址
- `Port` - TCP端口

## 使用指南

### 主菜单选项

运行程序后，您将看到交互式菜单：

```
1. 发送单个分拣机信号
2. 批量发送分拣机信号
3. 分拣机压力测试
4. 发送单个DWS数据
5. 批量发送DWS数据
6. DWS压力测试
7. 完整流程模拟（包裹+DWS）
8. 查看当前配置
9. 退出
```

### 使用场景

#### 场景1：验证单个包裹处理
选择选项 1 或 4，逐步验证系统处理单个包裹的能力。

#### 场景2：批量数据测试
选择选项 2 或 5：
- 输入发送数量（例如：100）
- 设置发送间隔（例如：100毫秒）
- 查看批量处理统计结果

#### 场景3：压力测试
选择选项 3 或 6：
- 设置测试持续时间（例如：60秒）
- 设置目标速率（例如：100包裹/秒）
- 实时查看压力测试结果和性能指标

#### 场景4：真实流程模拟
选择选项 7：
- 输入模拟数量
- 设置包裹和DWS之间的间隔时间
- 系统会自动发送匹配的包裹和DWS数据对

## 数据生成说明

### 包裹数据格式
```json
{
  "parcelId": "PKG20231109150000000001",
  "cartNumber": "CART001",
  "barcode": "BC1699517200000001",
  "timestamp": "2023-11-09T15:00:00Z"
}
```

### DWS数据格式
```json
{
  "barcode": "BC1699517200000001",
  "weight": 1500,
  "length": 350,
  "width": 250,
  "height": 180,
  "volume": 15750,
  "scannedAt": "2023-11-09T15:00:00Z"
}
```

## 性能指标说明

### 批量测试结果
- **总数**: 发送的总请求数
- **成功**: 成功处理的请求数
- **失败**: 失败的请求数
- **成功率**: 成功请求占总请求的百分比
- **平均延迟**: 所有请求的平均响应时间
- **最小/最大延迟**: 最快和最慢的响应时间

### 压力测试结果
- **目标速率**: 设定的每秒请求数
- **实际速率**: 实际达到的每秒请求数
- **P50延迟**: 50%的请求响应时间低于此值
- **P95延迟**: 95%的请求响应时间低于此值
- **P99延迟**: 99%的请求响应时间低于此值

## 切换通信协议

### 切换到MQTT
在 `appsettings.json` 中设置：
```json
{
  "Simulator": {
    "SorterCommunicationType": "MQTT"
  }
}
```

### 切换到TCP
在 `appsettings.json` 中设置：
```json
{
  "Simulator": {
    "SorterCommunicationType": "TCP"
  }
}
```

## 故障排查

### 问题1: 无法连接到MQTT代理
**原因**: MQTT代理未运行或配置错误
**解决方案**: 
- 确认MQTT代理（如Mosquitto）正在运行
- 验证 `BrokerHost` 和 `BrokerPort` 配置正确
- 检查防火墙规则

### 问题2: 无法连接到TCP服务器
**原因**: TCP服务器未监听或端口配置错误
**解决方案**:
- 检查服务配置中的TCP端口设置
- 验证防火墙规则允许TCP连接
- 确认端口号与服务配置一致

### 问题3: 压力测试成功率低
**原因**: 系统资源不足或配置不当
**解决方案**:
- 降低测试速率
- 增加系统资源（CPU、内存）
- 检查网络连接稳定性
- 查看服务日志了解具体错误

### 问题4: 实际速率远低于目标速率
**原因**: 系统瓶颈或网络延迟
**解决方案**:
- 检查网络延迟
- 优化系统配置
- 使用更快的通信协议（MQTT vs TCP）
- 检查服务器性能

## 技术架构

### 核心组件

1. **DataGenerator**: 随机生成测试数据
2. **MqttSorterSimulator**: MQTT分拣机信号模拟器
3. **TcpSorterSimulator**: TCP分拣机信号模拟器
4. **DwsSimulator**: DWS数据模拟器（TCP）
5. **SimulatorConfig**: 配置管理

### 使用的技术栈

- **.NET 8.0**: 运行时框架
- **Spectre.Console**: 交互式控制台UI
- **TouchSocket**: 高性能TCP通信
- **MQTTnet**: MQTT协议支持
- **System.Text.Json**: JSON序列化

## 最佳实践

### 1. 渐进式测试
从小规模开始，逐步增加负载：
1. 先发送10个包裹测试基本功能
2. 再发送100个包裹测试批量处理
3. 最后进行压力测试验证系统极限

### 2. 监控系统资源
在压力测试期间，监控以下指标：
- 服务器CPU使用率
- 内存占用
- 网络连接数
- 响应时间分布

### 3. 调整配置
根据测试结果调整配置：
- 如果P99延迟过高（>1秒），降低测试速率
- 如果成功率低于95%，检查系统资源和配置
- 如果实际速率远低于目标速率，优化系统性能

### 4. 选择合适的协议
- **MQTT**: 适合分布式部署，支持QoS控制
- **TCP**: 直连简单，低延迟

## 版本历史

### v1.1.0 (2025-11-09)
- ✅ 添加MQTT分拣机模拟器支持
- ✅ 添加TCP分拣机模拟器支持
- ✅ 移除HTTP API分拣机模拟器
- ✅ 支持运行时切换通信协议
- ✅ 更新配置结构
- ✅ 完整的异常处理和日志记录

### v1.0.0 (2025-11-09)
- ✅ 初始版本发布
- ✅ 支持分拣机信号模拟（HTTP API）
- ✅ 支持DWS数据模拟（TCP）
- ✅ 批量和压力测试功能
- ✅ 完整流程模拟
- ✅ 详细的性能统计
- ✅ 交互式控制台UI

## 许可证

MIT License

## 反馈与支持

如有问题或建议，请提交issue到主项目仓库。
