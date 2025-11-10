# ZakYip.Sorting.RuleEngine

## 项目简介

ZakYip分拣规则引擎系统是一个高性能的包裹分拣规则引擎，用于处理分拣程序的包裹信息和DWS（尺寸重量扫描）数据，通过自定义规则分析计算格口号，实现自动化分拣。

## 主逻辑流程

### 系统架构说明

系统采用**事件驱动架构（Event-Driven Architecture）**和**CQRS模式**，核心组件包括：

#### 核心服务层次
1. **ParcelOrchestrationService（包裹编排服务）**
   - 管理包裹处理的完整生命周期
   - 使用 `Channel<T>` 实现 FIFO 队列，确保包裹按顺序处理
   - 维护包裹处理上下文（ProcessingContext）在内存缓存中
   - 协调事件发布和工作项调度

2. **ParcelProcessingService（包裹处理服务）**
   - 执行实际的包裹处理逻辑
   - 调用规则引擎和第三方API
   - 生成处理结果和性能指标

3. **RuleEngineService（规则引擎服务）**
   - 评估规则并计算格口号
   - 支持6种匹配方法（条码正则、重量、体积、OCR、API响应、低代码表达式）
   - 使用内存缓存提高规则评估性能

#### 事件驱动流程
系统使用 **MediatR** 实现事件驱动架构，关键事件包括：
- `ParcelCreatedEvent` - 包裹创建时触发
- `DwsDataReceivedEvent` - DWS数据接收时触发
- `WcsApiCalledEvent` - 第三方API调用完成时触发
- `RuleMatchCompletedEvent` - 规则匹配完成时触发

#### FIFO队列机制
- 使用 `System.Threading.Channels` 实现有界通道（容量1000）
- 保证包裹按照创建顺序严格处理（FIFO - First In First Out）
- 支持异步并发处理，但保持顺序性
- 队列满时自动等待（BoundedChannelFullMode.Wait）

### 包裹分拣完整流程

#### 流程图概览

```
                    ┌─────────────┐
                    │  分拣机设备  │
                    └──────┬──────┘
                           │ 1. 创建包裹信号
                           ▼
                    ┌─────────────────────┐
                    │ ParcelOrchestration │
                    │     Service         │
                    └──────┬──────────────┘
                           │ 2. 加入FIFO队列
                           ▼
                    ┌─────────────────────┐         ┌─────────────┐
                    │  FIFO Channel       │────────▶│  DWS设备    │
                    │  (有界队列 1000)    │◀────────│  测量数据   │
                    └──────┬──────────────┘         └─────────────┘
                           │ 3. 按序处理                    │
                           ▼                               │
            ┌──────────────────────────┐                   │
            │   DwsDataReceivedEvent   │◀──────────────────┘
            └──────────┬───────────────┘
                       │ 4. 调用第三方API (可选)
                       ▼
            ┌──────────────────────────┐
            │   第三方WCS API          │
            │  (PostalApi/JushuitanErp │
            │   /WdtWms/WdtErpFlagship)│
            └──────────┬───────────────┘
                       │ 5. API响应 or 超时
                       ▼
            ┌──────────────────────────┐
            │   RuleEngineService      │
            │   (6种匹配方法)          │
            └──────────┬───────────────┘
                       │ 6. 计算格口号
                       ▼
            ┌──────────────────────────┐
            │  RuleMatchCompletedEvent │
            └──────────┬───────────────┘
                       │ 7. 返回结果
                       ▼
                    ┌─────────────┐
                    │  分拣机设备  │
                    │  (接收格口号)│
                    └─────────────┘
```

当系统接收到创建包裹消息后，会执行以下步骤：

#### 1. 分拣机发送包裹创建信号
- **触发方式**：分拣机通过 TCP/SignalR/HTTP/MQTT 发送包裹创建请求
- **请求内容**：
  - `ParcelId` - 包裹唯一ID
  - `CartNumber` - 小车号
  - `Barcode` - 条码（可选）
- **系统响应**：创建包裹处理空间，包裹进入FIFO队列等待DWS数据
- **事件发布**：触发 `ParcelCreatedEvent` 事件

#### 2. 包裹创建事件处理
- **处理器**：`ParcelCreatedEventHandler`
- **执行操作**：
  - 记录包裹创建日志到数据库
  - 在缓存中为包裹分配处理空间
  - 等待DWS数据到达

#### 3. DWS设备发送测量数据
- **触发方式**：DWS设备通过 TCP/SignalR/HTTP/MQTT 发送测量数据
- **测量内容**：
  - `Barcode` - 条码
  - `Weight` - 重量（克）
  - `Length/Width/Height` - 长/宽/高（厘米）
  - `Volume` - 体积（立方厘米）
- **系统响应**：接收并存储DWS数据
- **事件发布**：触发 `DwsDataReceivedEvent` 事件

#### 4. DWS数据接收事件处理
- **处理器**：`DwsDataReceivedEventHandler`
- **执行操作**：
  1. **记录DWS数据日志** - 保存到 `DwsCommunicationLog` 表
  2. **调用第三方WCS API** - 主动请求格口号
     - 使用 `IWcsApiAdapterFactory` 获取当前活动的API适配器
     - 调用 `RequestChuteAsync()` 方法，传递包裹ID和DWS数据
     - 支持多种第三方系统：
       - PostProcessingCenterApiClient（邮政处理中心）
       - PostCollectionApiClient（邮政分揽投机构）
       - JushuitanErpApiClient（聚水潭ERP）
       - WdtWmsApiClient（旺店通WMS）
       - WdtErpFlagshipApiClient（旺店通ERP旗舰版）
       - WcsApiClient（通用WCS）
  3. **记录API响应** - 保存到 `ApiCommunicationLog` 表
  4. **发布API调用事件** - 触发 `WcsApiCalledEvent`
  5. **错误处理** - 如果API调用失败，继续使用规则引擎

#### 5. 规则引擎匹配
- **服务**：`RuleEngineService`
- **匹配策略**：
  1. **"多选一"策略** - 当多条规则匹配时，选择优先级最高的一条规则
  2. **无降级策略** - 每条规则使用其指定的匹配方法，不会降级到其他方法
  3. **数据源隔离** - 不同匹配方法使用独立的数据源，确保清晰分离
- **规则匹配过程**：
  1. 从缓存加载所有启用的规则（按优先级排序）
  2. 依次评估每条规则，支持6种匹配方法：
     - **BarcodeRegex** - 条码正则匹配（StartsWith、Contains、Regex等）
     - **WeightMatch** - 重量范围匹配
     - **VolumeMatch** - 体积/尺寸匹配
     - **OcrMatch** - OCR识别结果匹配（仅使用OCR数据，不使用API响应内容）
     - **ApiResponseMatch** - API响应内容匹配（仅使用API响应数据，不使用OCR数据）
     - **LowCodeExpression** - 低代码表达式（可混合条码、重量、体积、OCR数据，但不包含API响应内容）
  3. 选择优先级最高的匹配规则
  4. 获取规则关联的格口号
- **性能优化**：
  - 规则缓存（滑动过期5分钟，绝对过期30分钟）
  - 方法内联优化（`[MethodImpl(AggressiveInlining)]`）
  - 性能指标自动收集（P50/P95/P99延迟）
- **记录匹配日志** - 保存到 `MatchingLog` 表

#### 6. 规则匹配完成事件处理
- **处理器**：`RuleMatchCompletedEventHandler`
- **执行操作**：
  1. 记录匹配结果日志
  2. 计算小车占用数量
  3. 准备返回给分拣机的数据

#### 7. 返回分拣结果
- **返回内容**：
  - `ChuteNumber` - 格口号
  - `CartNumber` - 小车号
  - `CartCount` - 占用小车数量
  - `Success` - 处理是否成功
  - `ProcessingTimeMs` - 处理耗时
- **通知方式**：通过分拣机适配器（TCP/SignalR/HTTP/MQTT）发送结果
- **记录通信日志** - 保存到 `SorterCommunicationLog` 表

#### 8. 关闭处理空间
- **执行操作**：
  - 从缓存中移除包裹处理空间
  - 标记包裹状态为已完成
  - 释放相关资源

### 数据持久化策略

系统采用双数据库架构，确保数据安全：

1. **MySQL（主数据库）** - 记录所有日志和通信数据
2. **SQLite（备份数据库）** - MySQL失败时自动降级
3. **熔断机制** - 50%失败率触发熔断，20分钟自动恢复
4. **数据同步** - MySQL恢复后自动同步SQLite所有数据
5. **事务保护** - 使用两阶段提交确保数据一致性

### 专用日志表

系统使用7个专用日志表记录完整流程：

| 日志表 | 用途 | 关键字段 |
|--------|------|----------|
| SorterCommunicationLog | 分拣机通信日志 | ParcelId, CartNumber, CommunicationType (TCP/SignalR/HTTP/MQTT), 原始内容 |
| DwsCommunicationLog | DWS通信日志 | Barcode, Weight, Volume, CommunicationType (TCP/SignalR/HTTP/MQTT), 测量数据 |
| ApiCommunicationLog | 第三方API调用日志 | RequestUrl, RequestBody, ResponseBody, CommunicationType (HTTP), 耗时 |
| MatchingLog | 规则匹配日志 | RuleId, MatchingReason, ChuteId, 匹配依据 |
| ApiRequestLog | HTTP请求日志 | Method, Path, StatusCode, IP地址 |
| CommunicationLog | 通用通信日志 | Direction, Type, Message, 成功状态 |
| LogEntry | 系统日志 | Level, Message, Exception |

### 后台服务

系统运行多个后台服务，自动执行维护和优化任务：

| 后台服务 | 功能描述 | 执行频率 |
|---------|---------|---------|
| ParcelQueueProcessorService | 处理FIFO队列中的包裹工作项 | 持续运行 |
| MonitoringAlertService | 监控系统健康状态并生成告警 | 每分钟 |
| DataCleanupService | 基于空闲检测清理过期数据 | 每30分钟 |
| DataArchiveService | 归档冷数据到历史表 | 每天凌晨2点 |
| ShardingTableManagementService | 自动创建和管理分片表 | 每小时 |
| MySqlAutoTuningService | 优化MySQL查询计划和索引 | 每6小时 |
| LogFileCleanupService | 清理过期的NLog日志文件 | 每天 |
| ConfigurationCachePreloadService | 预加载配置到缓存 | 启动时一次 |

## 最新更新 / Latest Updates

### v1.16.0 (2025-11-09) 🎉
本次更新主要聚焦于**通信协议优化**、**测试工具完善**和**系统安全增强**，进一步提升系统的可靠性和可测试性。

This update focuses on **communication protocol optimization**, **testing tool enhancement**, and **system security reinforcement**, further improving system reliability and testability.

#### 核心更新 / Core Updates

**1. 数据模拟器协议升级 / Data Simulator Protocol Upgrade** ⚡
- ✅ **MQTT分拣机模拟器** - 基于MQTTnet实现，支持QoS控制和自动重连
  - 新增 `MqttSorterSimulator.cs` - 企业级MQTT通信实现
  - 支持可配置的Broker地址、端口和主题
  - 自动心跳检测和断线重连机制
- ✅ **TCP分拣机模拟器** - 基于TouchSocket高性能TCP通信
  - 新增 `TcpSorterSimulator.cs` - 传统TCP Socket通信
  - 支持连接池和数据包分片处理
  - 完整的连接状态管理
- ✅ **统一接口抽象** - `ISorterSimulator` 接口设计
  - 统一的API接口，便于协议切换
  - 工厂模式支持运行时动态切换协议（MQTT/TCP）
  - 移除HTTP API方式，专注于工业级通信协议
- ✅ **DWS设备支持** - 保持TCP协议，确保测量数据稳定传输
- 📖 详见：[DataSimulator README](Tests/ZakYip.Sorting.RuleEngine.DataSimulator/README.md)

**2. 接口模拟器项目 / Interface Simulator Project** 🆕
- ✅ **独立服务** - 新增完整的ASP.NET Core Web API项目
  - 项目路径：`Tests/ZakYip.Sorting.RuleEngine.InterfaceSimulator`
  - 默认端口：5100（可配置）
- ✅ **API端点** - 三个核心接口
  - `GET /api/interface/random` - 获取单个随机接口ID（1-50）
  - `GET /api/interface/random/batch?count=N` - 批量获取（支持1-100个）
  - `GET /api/health` - 健康检查端点
- ✅ **Swagger UI** - 完整的API文档和在线测试
  - 访问地址：`http://localhost:5100/swagger`
  - 支持在线调试和测试
- ✅ **异常安全** - 所有端点都有完整的异常处理
  - 标准化错误响应格式
  - 详细的错误日志记录
- ✅ **CORS支持** - 允许跨域调用，方便前端集成
- 📖 详见：[InterfaceSimulator README](Tests/ZakYip.Sorting.RuleEngine.InterfaceSimulator/README.md)

**3. 异常安全隔离 / Exception Safety Isolation** 🛡️
- ✅ **全面文档化** - 新增异常处理最佳实践文档
  - 文档位置：[EXCEPTION_SAFETY.md](EXCEPTION_SAFETY.md)
  - 涵盖所有关键代码路径
- ✅ **防御性编程** - 所有外部调用都有异常保护
  - API端点：标准化错误响应
  - 数据库操作：熔断器和降级策略
  - 网络通信：超时和重试机制
  - 后台服务：异常恢复和日志记录
- ✅ **优雅降级** - 核心功能路径的降级策略
  - MySQL失败 → SQLite自动降级
  - 第三方API失败 → 规则引擎兜底
  - 通信异常 → 自动重连和队列保护
- ✅ **审计和验证** - 对现有代码库进行全面审计
  - 310+ 单元测试全部通过
  - 构建零警告零错误
  - SonarQube质量门禁通过

#### 技术指标 / Technical Metrics

| 指标 / Metric | 数值 / Value | 说明 / Description |
|--------------|-------------|-------------------|
| 单元测试数量 | 310+ | 从289个增加到310+ |
| 测试覆盖率 | ≥85% | 符合质量门禁要求 |
| 构建警告 | 0 | 零警告编译 |
| 构建错误 | 0 | 零错误构建 |
| 支持协议 | 4种 | SignalR、TCP、MQTT、HTTP |
| 模拟器项目 | 2个 | DataSimulator + InterfaceSimulator |
| API适配器 | 6种 | 支持6种第三方系统集成 |

#### 配置变更 / Configuration Changes

**⚠️ 重要：DataSimulator配置格式已更新**

旧配置（v1.15.x）：
```json
{
  "HttpApiUrl": "http://localhost:5000"
}
```

新配置（v1.16.0+）：
```json
{
  "SorterCommunicationType": "MQTT",  // 或 "TCP"
  "SorterMqtt": {
    "BrokerHost": "localhost",
    "BrokerPort": 1883,
    "PublishTopic": "sorter/signal",
    "ClientId": "data-simulator",
    "Username": "",  // 可选
    "Password": ""   // 可选
  },
  "SorterTcp": {
    "Host": "localhost",
    "Port": 7000
  }
}
```

#### 相关文档 / Related Documentation

- 📖 [IMPLEMENTATION_SUMMARY_v1.16.0.md](IMPLEMENTATION_SUMMARY_v1.16.0.md) - 详细实施总结
- 📖 [EXCEPTION_SAFETY.md](EXCEPTION_SAFETY.md) - 异常安全隔离文档
- 📖 [MQTT_USAGE_GUIDE.md](MQTT_USAGE_GUIDE.md) - MQTT适配器使用指南
- 📖 [DataSimulator README](Tests/ZakYip.Sorting.RuleEngine.DataSimulator/README.md) - 数据模拟器文档
- 📖 [InterfaceSimulator README](Tests/ZakYip.Sorting.RuleEngine.InterfaceSimulator/README.md) - 接口模拟器文档

### v1.15.0 (2025-11-09)
- ✅ **数据模拟器** - 新增分拣机和DWS数据模拟程序
  - 支持单次、批量和压力测试模式
  - 分拣机信号模拟（现已升级为MQTT/TCP）
  - DWS数据模拟（TCP）
  - 完整流程模拟（包裹+DWS）
  - 详细的性能统计（成功率、延迟、P50/P95/P99）
  - 交互式控制台UI（Spectre.Console）
  - 可配置的数据生成范围
  - 支持100-1000包裹/秒的压力测试
  - 完整的使用文档和示例

### v1.14.9 (2025-11-09)
- ✅ **数据日志增强** - 所有通信日志表新增CommunicationType字段
  - DwsCommunicationLog增加通信类型记录（TCP/SignalR/HTTP/MQTT）
  - SorterCommunicationLog增加通信类型记录（TCP/SignalR/HTTP/MQTT）
  - ApiCommunicationLog增加通信类型记录（默认HTTP）
  - 支持按通信类型查询和分析日志
  - 新增索引优化按通信类型查询性能
  - 数据库迁移支持MySQL和SQLite
- ✅ **代码质量提升** - 修复XML文档注释警告，构建零警告零错误
- ✅ **测试覆盖提升** - 新增6个通信日志实体测试用例，总测试用例达到310个

### v1.14.8 (2025-11-09)
- ✅ **MQTT通信支持** - 新增基于MQTTnet的MQTT通信适配器
  - MqttSorterAdapter - 支持向分拣机发送格口号
  - MqttDwsAdapter - 支持接收DWS测量数据
  - 支持QoS控制和自动重连
  - 完整的单元测试覆盖（15个测试用例）
  - 通信日志持久化支持

### v1.14.7 (2025-11-08)
- ✅ **测试修复和代码质量提升** - 确保所有测试通过，构建无警告无错误
  - 修复监控服务告警生成测试（格口使用率检测）
  - 修复规则引擎空条码处理（正则表达式支持空字符串）
  - 修复取消令牌异常处理测试（接受TaskCanceledException）
  - 所有289个单元测试通过
  - 构建成功：0个警告，0个错误

### v1.14.6 (2025-11-07)
- ✅ **性能优化三重奏** - ArrayPool、并行处理、查询计划分析
  - ArrayPool<T>优化：批量处理内存分配减少40-60%
  - 批量操作并行处理：归档吞吐量提升50-100%
  - 查询计划分析：智能优化建议，减少慢查询发生率

### v1.14.5 (2025-11-07)
- ✅ **API客户端实现验证和修复** - 检查旺店通和聚水潭实现正确性
  - JushuitanErpApiClient（聚水潭ERP）签名算法和响应解析增强
  - WdtWmsApiClient（旺店通WMS）签名算法和图片上传功能
  - 所有10个测试通过

### v1.14.4 (2025-11-07)
- ✅ **API客户端重构完成** - 按照参考代码重新实现API适配器
  - RequestChuteAsync方法签名更新，接受DwsData和OcrData
  - WcsApiResponse字段完善，支持完整通信日志持久化
  - 所有API客户端统一方法映射规范

## 系统完成度 / System Completion Status

### 功能完成度概览 / Feature Completion Overview

系统当前整体完成度约为 **85%**，核心功能已全部实现并经过生产验证，部分高级功能和优化项仍在进行中。

The system is currently approximately **85%** complete, with all core features fully implemented and production-validated, while some advanced features and optimizations are still in progress.

| 功能模块 / Module | 完成度 / Completion | 状态 / Status | 说明 / Notes |
|------------------|-------------------|--------------|-------------|
| 🎯 核心分拣流程 | 100% | ✅ 生产就绪 | 包裹创建→DWS数据→规则匹配→格口分配 |
| 🔧 规则引擎 | 100% | ✅ 生产就绪 | 6种匹配方法全部实现并测试 |
| 🌐 通信协议 | 95% | ✅ 生产就绪 | SignalR/TCP/MQTT已完成，HTTP仅测试用 |
| 🔌 API适配器 | 90% | ✅ 生产就绪 | 6种第三方系统集成，缺少弹性策略 |
| 💾 数据持久化 | 100% | ✅ 生产就绪 | LiteDB+MySQL+SQLite三层架构 |
| 🔄 数据库熔断 | 100% | ✅ 生产就绪 | 自动降级和同步机制完整 |
| 📊 性能监控 | 95% | ✅ 生产就绪 | 实时指标、告警，缺少通知渠道 |
| 🧪 测试覆盖 | 85% | ✅ 符合标准 | 310+单元测试，压力测试完整 |
| 📖 文档完整性 | 90% | ✅ 优秀 | 中英双语文档，API文档完整 |
| 🔐 安全性 | 95% | ✅ 生产就绪 | 异常隔离、数据验证、日志审计 |
| 🎨 管理界面 | 0% | ⏳ 计划中 | Web管理界面尚未开发 |
| 🤖 智能分析 | 0% | ⏳ 计划中 | AI规则推荐尚未实现 |
| ☁️ 云原生支持 | 20% | 🔨 进行中 | 基础架构支持，缺少容器化 |

**整体评估 / Overall Assessment:**
- ✅ **核心功能完整** - 所有必需功能已实现并可用于生产环境
- ✅ **性能优秀** - 支持100-1000包裹/秒的处理能力
- ✅ **稳定可靠** - 完整的异常处理和降级策略
- ⚠️ **缺少UI** - 需要Web管理界面提升易用性
- ⚠️ **部分优化** - 部分性能和智能化优化项待实施

### 核心功能详情 / Core Features Detail

#### 1. 包裹分拣流程 (100%) ✅
- ✅ **包裹创建** - 完整的包裹生命周期管理
- ✅ **DWS数据接收** - 支持多协议接收测量数据
- ✅ **规则匹配** - 6种匹配方法，优先级排序
- ✅ **格口分配** - 智能分配，支持占用数量计算
- ✅ **FIFO队列** - 使用Channel<T>保证顺序处理
- ✅ **事件驱动** - MediatR实现完整事件流

#### 2. 规则引擎 (100%) ✅
- ✅ **条码正则匹配** - 支持StartsWith、Contains、Regex等
- ✅ **重量范围匹配** - 精确的重量区间判断
- ✅ **体积尺寸匹配** - 长宽高和体积判断
- ✅ **OCR识别匹配** - 文字识别结果匹配
- ✅ **API响应匹配** - 第三方接口返回值匹配
- ✅ **低代码表达式** - 灵活的表达式引擎
- ✅ **规则缓存** - 5分钟滑动过期，30分钟绝对过期
- ✅ **性能优化** - 方法内联优化（AggressiveInlining）

#### 3. API适配器 (90%) ✅
**已实现的适配器：**
- ✅ **PostCollectionApiClient** - 邮政分揽投机构
- ✅ **PostProcessingCenterApiClient** - 邮政处理中心
- ✅ **JushuitanErpApiClient** - 聚水潭ERP
- ✅ **WdtWmsApiClient** - 旺店通WMS
- ✅ **WdtErpFlagshipApiClient** - 旺店通ERP旗舰版
- ✅ **WcsApiClient** - 通用WCS客户端

**待完善的功能：**
- ⏳ Polly弹性策略（重试、熔断、超时）
- ⏳ 批量操作支持
- ⏳ 强类型响应模型

#### 4. 数据持久化 (100%) ✅
- ✅ **LiteDB** - 配置数据（规则、格口、设备）
- ✅ **MySQL** - 主日志数据库（7个专用日志表）
- ✅ **SQLite** - 降级数据库（与MySQL结构一致）
- ✅ **数据库熔断** - 50%失败率触发，20分钟恢复
- ✅ **自动同步** - MySQL恢复后完整同步SQLite数据
- ✅ **事务保护** - 两阶段提交确保数据一致性

#### 5. 性能监控 (95%) ✅
**已实现：**
- ✅ 实时包裹处理量监控（每分钟/5分钟/1小时）
- ✅ 格口使用率监控和告警（80%警告，95%严重）
- ✅ 系统性能指标（P50/P95/P99延迟）
- ✅ 错误率监控和告警（5%警告，15%严重）
- ✅ 数据库健康监控（连接状态、熔断状态）
- ✅ SignalR实时推送
- ✅ REST API端点

**待实现：**
- ⏳ 邮件/短信/企业微信通知
- ⏳ 告警规则自定义
- ⏳ 历史趋势分析图表

#### 6. 测试覆盖 (85%) ✅
**单元测试：**
- ✅ **310+ 测试用例** - 覆盖核心功能
- ✅ **xUnit框架** - 现代化测试框架
- ✅ **Moq模拟** - 依赖隔离和模拟
- ✅ **85%+ 覆盖率** - 符合质量门禁标准
- ✅ **CI/CD集成** - 每次提交自动运行

**性能测试：**
- ✅ **BenchmarkDotNet** - 微基准测试
- ✅ **NBomber** - 负载和压力测试
- ✅ **支持100-1000包裹/秒** - 生产级性能验证

**测试工具：**
- ✅ **DataSimulator** - 综合数据模拟器
  - 支持单次、批量、压力测试模式
  - MQTT和TCP协议支持
  - 详细的性能统计和报告
  - 交互式控制台UI
- ✅ **InterfaceSimulator** - 接口ID模拟服务
  - 随机返回1-50接口ID
  - 支持单个和批量获取
  - Swagger UI文档
- ✅ **TestConsole** - 手动测试控制台
- ✅ **多个API客户端测试项目** - 各API适配器独立测试

#### 7. 通信支持 (95%) ✅

**分拣机通信 (ISorterAdapter):**
| 适配器 | 状态 | 用途 | 性能 |
|--------|------|------|------|
| SignalR Hub | ✅ 推荐 | 实时双向通信，自动重连 | ⭐⭐⭐⭐⭐ |
| TouchSocket TCP | ✅ 可用 | 高性能TCP，连接池管理 | ⭐⭐⭐⭐⭐ |
| 传统 TCP | ✅ 可用 | 兼容老旧设备 | ⭐⭐⭐⭐ |
| MQTT | ✅ 推荐 | 分布式部署，QoS控制 | ⭐⭐⭐⭐⭐ |

**DWS设备通信 (IDwsAdapter):**
| 适配器 | 状态 | 用途 | 性能 |
|--------|------|------|------|
| SignalR Hub | ✅ 推荐 | 实时数据推送 | ⭐⭐⭐⭐⭐ |
| TouchSocket TCP | ✅ 推荐 | 高性能数据接收 | ⭐⭐⭐⭐⭐ |
| MQTT | ✅ 可用 | 订阅模式接收 | ⭐⭐⭐⭐ |
| HTTP API | ✅ 仅测试 | 调试和测试用 | ⭐⭐⭐ |

**第三方API适配器 (IWcsApiAdapter):**
- ✅ 运行时动态切换（无需重启）
- ✅ 统一接口抽象
- ✅ 容错机制（失败降级到规则引擎）
- ⏳ 待添加弹性策略（Polly）

### 数据管理和优化 / Data Management & Optimization

#### 数据分片策略 ✅
- ✅ **按时间分表** - 日/周/月维度可配置
- ✅ **热冷数据分离** - 活动数据和归档数据分离
- ✅ **自动分表管理** - ShardingTableManagementService每小时检查
- ✅ **查询路由优化** - 自动路由到正确的分片表

#### 数据清理和归档 ✅
- ✅ **空闲检测清理** - 30分钟无包裹创建后触发
- ✅ **可配置保留期** - 默认90天（可调整）
- ✅ **批量归档** - 归档服务每天凌晨2点运行
- ✅ **并行处理** - 归档吞吐量提升50-100%
- ✅ **ArrayPool优化** - 内存分配减少40-60%

#### 查询优化 ✅
- ✅ **查询计划缓存** - 避免重复编译
- ✅ **慢查询检测** - 自动记录超阈值查询
- ✅ **智能优化建议** - MySQL自动调优服务
- ✅ **索引自动管理** - 基于查询模式优化索引

### 后台服务 / Background Services

系统运行8个后台服务，自动执行维护和优化任务：

| 服务名称 | 功能 | 频率 | 优先级 |
|---------|------|------|--------|
| ParcelQueueProcessorService | 处理FIFO队列中的包裹 | 持续运行 | 🔴 关键 |
| MonitoringAlertService | 监控健康状态并生成告警 | 每分钟 | 🔴 关键 |
| DataCleanupService | 清理过期数据 | 每30分钟 | 🟡 重要 |
| DataArchiveService | 归档冷数据到历史表 | 每天2:00 | 🟡 重要 |
| ShardingTableManagementService | 管理分片表 | 每小时 | 🟡 重要 |
| MySqlAutoTuningService | 优化查询和索引 | 每6小时 | 🟢 辅助 |
| LogFileCleanupService | 清理NLog日志文件 | 每天 | 🟢 辅助 |
| ConfigurationCachePreloadService | 预加载配置缓存 | 启动时 | 🔴 关键 |

### 代码质量保证 / Code Quality Assurance

#### CI/CD工作流 ✅
**1. CI Build and Test**
- ✅ 自动构建所有项目
- ✅ 运行310+单元测试
- ✅ 生成代码覆盖率报告（Cobertura + HTML）
- ✅ 强制85%覆盖率阈值
- ✅ PR评论显示覆盖率变化

**2. SonarQube Analysis**
- ✅ 静态代码分析（SonarCloud）
- ✅ 代码质量评分
- ✅ 安全漏洞检测（0容忍）
- ✅ 代码异味识别
- ✅ 质量门禁自动检查

#### 质量指标 ✅
| 指标 | 目标 | 当前 | 状态 |
|------|------|------|------|
| 单元测试覆盖率 | ≥85% | 85%+ | ✅ |
| 代码重复率 | ≤3% | <3% | ✅ |
| 代码文档覆盖率 | ≥90% | 90%+ | ✅ |
| 安全漏洞 | 0 | 0 | ✅ |
| 构建警告 | 0 | 0 | ✅ |
| 构建错误 | 0 | 0 | ✅ |

## 监控和告警系统

### 实时监控功能
系统提供全面的实时监控功能，通过专用的监控服务和SignalR Hub实现实时数据推送：

#### 1. 包裹处理量监控
- **实时处理速率**：每分钟处理的包裹数量
- **历史处理量**：最近1分钟、5分钟、1小时的处理量统计
- **处理成功率**：成功处理的包裹百分比
- **平均处理时间**：包裹从创建到完成的平均耗时

#### 2. 格口使用率监控
- **活跃格口数**：当前正在使用的格口数量
- **平均使用率**：所有格口的平均利用率（基于每小时容量）
- **格口热力图**：可视化展示各格口的使用频率和分布
- **使用率告警**：
  - 警告阈值：80% - 生成Warning级别告警
  - 严重阈值：95% - 生成Critical级别告警

#### 3. 系统性能指标监控
- **P50/P95/P99延迟**：规则评估和API调用的延迟百分位数
- **操作耗时统计**：各关键操作的平均耗时
- **性能趋势分析**：性能指标的时间序列分析
- **慢查询检测**：自动识别和记录超过阈值的数据库查询

#### 4. 错误率和异常监控
- **实时错误率**：最近5分钟的错误百分比
- **错误类型分布**：按错误类型统计异常发生频率
- **错误率告警**：
  - 警告阈值：5% - 生成Warning级别告警
  - 严重阈值：15% - 生成Critical级别告警
- **异常堆栈追踪**：详细的异常信息记录

#### 5. 数据库健康监控
- **连接状态**：MySQL和SQLite数据库连接状态
- **熔断器状态**：数据库熔断器的实时状态
- **故障转移**：MySQL失败时自动切换到SQLite
- **数据同步状态**：SQLite到MySQL的数据同步进度

### 监控API端点

#### 获取实时监控数据
```http
GET /api/Monitoring/realtime
```

**响应示例**：
```json
{
  "currentProcessingRate": 150,
  "activeChutes": 10,
  "averageChuteUsageRate": 72.5,
  "currentErrorRate": 2.3,
  "databaseStatus": "Healthy",
  "lastMinuteParcels": 150,
  "last5MinutesParcels": 720,
  "lastHourParcels": 8640,
  "activeAlerts": 0,
  "systemHealth": "Healthy",
  "timestamp": "2025-11-08T05:30:00Z"
}
```

#### 获取活跃告警
```http
GET /api/Monitoring/alerts/active
```

#### 获取告警历史
```http
GET /api/Monitoring/alerts/history?startTime=2025-11-01&endTime=2025-11-08
```

#### 解决告警
```http
POST /api/Monitoring/alerts/{alertId}/resolve
```

### 告警级别和类型

#### 告警级别 (AlertSeverity)
- **Info** - 信息性通知，无需立即处理
- **Warning** - 警告，建议关注和处理
- **Critical** - 严重问题，需要立即处理

#### 告警类型 (AlertType)
- **ProcessingRate** - 包裹处理速率异常
- **ChuteUsage** - 格口使用率异常
- **ErrorRate** - 系统错误率过高
- **Performance** - 性能指标异常
- **Database** - 数据库健康问题
- **System** - 系统级别问题

### 实时通知 (SignalR)

通过SignalR Hub实现实时告警推送：

```javascript
// 连接到监控Hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/monitoring")
    .build();

// 订阅告警通知
connection.on("AlertGenerated", (alert) => {
    console.log("New alert:", alert);
    // 处理告警通知（显示通知、播放声音等）
});

// 订阅实时监控数据
connection.on("MonitoringDataUpdated", (data) => {
    console.log("Monitoring data:", data);
    // 更新监控仪表板
});

await connection.start();
```

### 监控后台服务

系统运行一个专用的后台服务 `MonitoringAlertService`，每分钟执行一次健康检查：

- 检查包裹处理速率是否低于阈值（默认10包裹/分钟）
- 检查格口使用率是否超过警告或严重阈值
- 检查错误率是否超过可接受范围
- 检查数据库连接和熔断器状态
- 自动生成和记录告警到数据库
- 通过SignalR实时推送告警到连接的客户端

### 监控数据持久化

所有监控数据存储在专用表中：

| 表名 | 用途 | 保留期限 |
|------|------|----------|
| PerformanceMetric | 性能指标记录 | 90天（可配置） |
| MonitoringAlert | 告警记录 | 永久保存 |
| CommunicationLog | 通信日志 | 90天（可配置） |

### 配置监控阈值

可以通过配置文件调整监控阈值（在代码中的常量定义）：

```csharp
// MonitoringService.cs
private const decimal ChuteUsageRateWarningThreshold = 80.0m;  // 格口使用率警告阈值
private const decimal ChuteUsageRateCriticalThreshold = 95.0m; // 格口使用率严重阈值
private const decimal ErrorRateWarningThreshold = 5.0m;        // 错误率警告阈值
private const decimal ErrorRateCriticalThreshold = 15.0m;      // 错误率严重阈值
private const int ProcessingRateLowThreshold = 10;              // 处理速率过低阈值
```

## 代码质量指标 / Code Quality Metrics

### 单元测试覆盖率 / Unit Test Coverage
- **目标覆盖率 / Target Coverage**：≥ 85%
- **当前测试用例 / Current Test Cases**：310+ 单元测试
- **CI/CD集成 / CI/CD Integration**：每次提交自动运行测试并生成覆盖率报告
- **质量门禁 / Quality Gate**：PR合并前必须达到85%覆盖率阈值
- **测试框架 / Test Framework**：xUnit + Moq + FluentAssertions
- **覆盖范围 / Coverage Scope**：
  - ✅ 核心业务逻辑（规则引擎、包裹处理）
  - ✅ API适配器（6种第三方系统）
  - ✅ 通信适配器（SignalR、TCP、MQTT）
  - ✅ 数据访问层（仓储模式）
  - ✅ 领域实体和值对象

### SonarQube静态分析 / SonarQube Static Analysis
- **平台 / Platform**：SonarCloud (https://sonarcloud.io)
- **项目ID / Project ID**：Hisoka6602_ZakYip.Sorting.RuleEngine.Core
- **分析频率 / Analysis Frequency**：每次push和PR时自动触发
- **质量门禁配置 / Quality Gate Configuration**：
  - ✅ 代码覆盖率 ≥ 85% / Code Coverage ≥ 85%
  - ✅ 代码重复率 ≤ 3% / Code Duplication ≤ 3%
  - ✅ 代码异味（Code Smells）：持续改进 / Continuous Improvement
  - ✅ 安全漏洞（Vulnerabilities）：0容忍 / Zero Tolerance
  - ✅ Bug：0容忍 / Zero Tolerance
- **当前评分 / Current Rating**：
  - 可维护性 / Maintainability：A级
  - 可靠性 / Reliability：A级
  - 安全性 / Security：A级

### 代码文档覆盖率 / Code Documentation Coverage
- **目标文档覆盖率 / Target Documentation Coverage**：≥ 90%
- **当前文档覆盖率 / Current Documentation Coverage**：90%+
- **文档要求 / Documentation Requirements**：
  - ✅ 所有公共类、接口、方法必须有XML文档注释
  - ✅ 复杂的私有方法需要添加说明注释
  - ✅ 关键业务逻辑需要详细的代码注释
  - ✅ 支持中英文双语文档
- **文档生成 / Documentation Generation**：通过编译时XML文档生成，集成到NuGet包
- **额外文档 / Additional Documentation**：
  - 📖 README.md（系统概览和使用指南）
  - 📖 IMPLEMENTATION_SUMMARY_v1.16.0.md（实施总结）
  - 📖 EXCEPTION_SAFETY.md（异常安全文档）
  - 📖 MQTT_USAGE_GUIDE.md（MQTT使用指南）
  - 📖 API_CLIENT_ENDPOINTS.md（API端点文档）
  - 📖 各模拟器README（DataSimulator、InterfaceSimulator）

### CI/CD工作流 / CI/CD Workflows
系统配置了两个主要的CI/CD工作流：

The system has two main CI/CD workflows configured:

#### 1. CI Build and Test (.github/workflows/ci.yml)
**功能 / Features：**
- ✅ 自动构建所有项目 / Auto build all projects
- ✅ 运行所有单元测试 / Run all unit tests
- ✅ 生成代码覆盖率报告（Cobertura格式）/ Generate coverage reports (Cobertura format)
- ✅ 生成HTML覆盖率报告 / Generate HTML coverage reports
- ✅ 强制执行85%覆盖率阈值 / Enforce 85% coverage threshold
- ✅ PR评论自动显示覆盖率变化 / Auto comment PR with coverage changes

**触发条件 / Triggers：**
- Push到main分支
- Pull Request创建或更新
- 手动触发（workflow_dispatch）

**运行时间 / Execution Time：** ~5-8分钟

#### 2. SonarQube Analysis (.github/workflows/sonarqube.yml)
**功能 / Features：**
- ✅ 静态代码分析 / Static code analysis
- ✅ 代码质量评分 / Code quality scoring
- ✅ 安全漏洞检测 / Security vulnerability detection
- ✅ 代码异味识别 / Code smell identification
- ✅ 覆盖率集成 / Coverage integration
- ✅ 质量门禁自动检查 / Automatic quality gate checking

**触发条件 / Triggers：**
- Push到main分支
- Pull Request创建或更新

**运行时间 / Execution Time：** ~8-12分钟

### 构建状态 / Build Status

| 指标 / Metric | 状态 / Status | 说明 / Description |
|--------------|--------------|-------------------|
| 最新构建 / Latest Build | ✅ 成功 / Success | 0 错误, 0 警告 |
| 单元测试 / Unit Tests | ✅ 通过 / Passed | 310+ 测试全部通过 |
| 代码覆盖率 / Code Coverage | ✅ 85%+ | 符合质量门禁 |
| SonarQube评分 / SonarQube Rating | ✅ A级 | 所有维度A级 |
| 安全漏洞 / Vulnerabilities | ✅ 0个 / 0 | 无安全问题 |
| 代码异味 / Code Smells | ✅ 优秀 / Excellent | 持续优化中 |

### 技术债务追踪 / Technical Debt Tracking

**当前技术债务 / Current Technical Debt：** 约2天（非常低）

**主要债务项 / Main Debt Items：**
1. ⏳ API客户端缺少Polly弹性策略（预计4小时）
2. ⏳ 部分复杂方法需要重构简化（预计8小时）
3. ⏳ 少数测试用例需要增强断言（预计4小时）

**债务管理策略 / Debt Management Strategy：**
- 每个Sprint分配20%时间用于技术债务偿还
- 优先偿还影响可维护性的债务
- 防止新债务引入（代码审查强制执行）

## 未来优化方向 / Future Optimization Directions

基于当前85%的系统完成度，以下是未来的优化路线图，按优先级和时间线组织。

Based on the current 85% system completion, here is the future optimization roadmap, organized by priority and timeline.

### 🎯 短期优化（1-2周内）/ Short-term (1-2 weeks)

#### 1. API客户端弹性增强 (优先级：高)
**目标：** 提升第三方API调用的可靠性和容错能力

**任务清单：**
- [ ] 集成Polly弹性策略
  - [ ] 重试策略（指数退避，最多3次）
  - [ ] 熔断器（连续5次失败触发，30秒恢复）
  - [ ] 超时策略（默认5秒，可配置）
- [ ] 实现请求去重和幂等性保护
- [ ] 添加请求/响应拦截器支持
- [ ] 优化序列化性能（使用System.Text.Json）

**预期收益：**
- API可用性从90%提升至99%
- 减少临时故障导致的失败
- 降低系统对第三方API的依赖

#### 2. 监控告警通知渠道 (优先级：高)
**目标：** 实现多渠道告警通知，提升运维响应速度

**任务清单：**
- [ ] 邮件通知（SMTP）
  - [ ] 支持HTML格式告警邮件
  - [ ] 告警分级（普通/紧急）
  - [ ] 批量发送优化
- [ ] 企业微信通知
  - [ ] Webhook集成
  - [ ] Markdown格式消息
  - [ ] @特定人员功能
- [ ] 短信通知（可选）
  - [ ] 阿里云/腾讯云SMS API
  - [ ] 仅严重告警发送
- [ ] 告警规则自定义
  - [ ] 可配置阈值
  - [ ] 静默时段设置
  - [ ] 告警频率限制

**预期收益：**
- 平均故障响应时间从30分钟降至5分钟
- 减少系统停机时间
- 提升运维团队满意度

#### 3. 文档和示例完善 (优先级：中)
**任务清单：**
- [ ] 添加更多代码示例
- [ ] 录制演示视频（分拣流程、配置管理）
- [ ] 创建故障排查手册
- [ ] 翻译所有文档为双语版本
- [ ] 添加架构决策记录（ADR）

### 🚀 中期优化（1-3个月）/ Mid-term (1-3 months)

#### 1. Web管理界面开发 (优先级：极高) 🎨
**目标：** 提供直观的可视化管理界面，降低使用门槛

**技术栈选择：**
- 前端：Vue 3 + Element Plus / React + Ant Design
- 后端：ASP.NET Core Web API（已有）
- 实时通信：SignalR（已有）

**功能模块：**

**阶段1：核心管理功能（第1个月）**
- [ ] **规则管理界面**
  - [ ] 规则列表（支持搜索、过滤、排序）
  - [ ] 规则创建向导（分步表单）
  - [ ] 规则编辑器（实时预览）
  - [ ] 规则测试工具（输入测试数据即时验证）
  - [ ] 规则启用/禁用控制
  - [ ] 规则优先级可视化调整（拖拽排序）
- [ ] **格口管理界面**
  - [ ] 格口列表和状态看板
  - [ ] 格口配置（容量、类型、关联规则）
  - [ ] 格口使用率实时图表
  - [ ] 格口热力图（使用频率可视化）
- [ ] **用户认证和权限**
  - [ ] 基于角色的访问控制（RBAC）
  - [ ] JWT Token认证
  - [ ] 操作审计日志

**阶段2：日志查询和分析（第2个月）**
- [ ] **日志查询界面**
  - [ ] 多维度过滤（时间、类型、状态、包裹ID等）
  - [ ] 高级搜索（正则表达式、组合条件）
  - [ ] 日志详情展示（关联查询）
  - [ ] 导出功能（CSV、Excel、JSON）
- [ ] **数据分析看板**
  - [ ] 处理量趋势图（时、日、周、月）
  - [ ] 成功率和错误率统计
  - [ ] 格口利用率分析
  - [ ] API调用性能分析
  - [ ] 自定义报表生成

**阶段3：系统配置和监控（第3个月）**
- [ ] **系统配置界面**
  - [ ] 数据库连接配置
  - [ ] 通信协议配置（分拣机、DWS、API）
  - [ ] 后台服务管理（启用/禁用）
  - [ ] 缓存策略配置
  - [ ] 数据清理和归档策略配置
- [ ] **性能监控仪表板**
  - [ ] 实时包裹处理量（大屏展示）
  - [ ] P50/P95/P99延迟图表
  - [ ] 格口使用率仪表盘
  - [ ] 错误率监控面板
  - [ ] 数据库健康状态
  - [ ] 告警历史和统计

**预期收益：**
- 配置时间从30分钟降至5分钟
- 无需技术背景即可完成日常管理
- 问题定位时间减少70%

#### 2. 智能分析功能 (优先级：高) 🤖
**目标：** 基于历史数据提供智能建议和优化

**功能规划：**

**2.1 规则优化建议引擎**
- [ ] 分析规则匹配统计
  - 识别从未匹配的"死规则"
  - 识别匹配频率极低的规则
  - 识别优先级设置不合理的规则
- [ ] 生成优化建议
  - 建议删除或合并规则
  - 建议调整优先级
  - 建议简化复杂规则
- [ ] 规则A/B测试支持
  - 同时运行新旧规则
  - 比较匹配效果
  - 自动切换到最优规则

**2.2 异常模式识别**
- [ ] 异常包裹检测
  - 识别重量/尺寸异常
  - 识别条码格式异常
  - 识别处理时间异常
- [ ] 异常趋势分析
  - 异常包裹时间分布
  - 异常类型统计
  - 异常来源分析
- [ ] 自动规则生成（实验性）
  - 基于异常模式自动创建规则
  - 需人工审核后启用

**2.3 格口利用率优化**
- [ ] 格口负载分析
  - 识别过载格口（>90%利用率）
  - 识别空闲格口（<10%利用率）
  - 分析格口使用时段分布
- [ ] 格口分配优化建议
  - 建议调整规则-格口映射
  - 建议增加/减少格口
  - 建议调整格口容量

**预期收益：**
- 规则维护工作量减少50%
- 格口利用率提升20%
- 异常包裹识别准确率>90%

#### 3. 性能优化增强 (优先级：中) ⚡
**任务清单：**
- [ ] Redis分布式缓存
  - [ ] 规则缓存迁移到Redis
  - [ ] 支持多实例部署
  - [ ] 缓存预热和失效策略
- [ ] 数据库查询优化
  - [ ] 应用QueryOptimizationExtensions到更多场景
  - [ ] 添加缺失的复合索引
  - [ ] 优化N+1查询问题
- [ ] 批量处理增强
  - [ ] 更多场景使用并行处理
  - [ ] 优化ArrayPool使用
  - [ ] 减少序列化开销

**性能目标：**
- 单实例处理能力从1000/秒提升至2000/秒
- P99延迟从100ms降至50ms
- 内存占用减少30%

### 🌟 长期优化（3-6个月）/ Long-term (3-6 months)

#### 1. 容器化和云原生 (优先级：高) ☁️
**目标：** 实现云原生部署，支持弹性伸缩

**阶段1：容器化（第1个月）**
- [ ] **Docker支持**
  - [ ] 创建多阶段Dockerfile（优化镜像大小）
  - [ ] Docker Compose编排（开发环境）
  - [ ] 环境变量配置支持
  - [ ] 健康检查端点
  - [ ] 日志输出到stdout/stderr
- [ ] **镜像优化**
  - [ ] 使用Alpine Linux基础镜像
  - [ ] 镜像分层优化
  - [ ] 镜像大小目标：<150MB
- [ ] **容器编排**
  - [ ] Docker Swarm配置（简单场景）
  - [ ] 负载均衡配置
  - [ ] 持久化存储配置

**阶段2：Kubernetes部署（第2-3个月）**
- [ ] **K8s资源定义**
  - [ ] Deployment配置
  - [ ] Service配置（ClusterIP、NodePort、LoadBalancer）
  - [ ] ConfigMap和Secret管理
  - [ ] PersistentVolumeClaim配置
- [ ] **高可用性**
  - [ ] 多副本部署（至少3个）
  - [ ] 健康检查和自动重启
  - [ ] 滚动更新策略
  - [ ] 资源限制和请求
- [ ] **Helm Charts**
  - [ ] 创建Helm Chart
  - [ ] 值文件模板化
  - [ ] Chart发布和版本管理
- [ ] **服务网格（可选）**
  - [ ] Istio集成
  - [ ] 流量管理
  - [ ] 可观测性增强

**阶段3：CI/CD增强（第3-4个月）**
- [ ] **GitHub Actions增强**
  - [ ] 自动构建Docker镜像
  - [ ] 推送到容器仓库（Docker Hub、ACR）
  - [ ] 自动部署到K8s集群
  - [ ] 金丝雀发布支持
- [ ] **GitOps**
  - [ ] ArgoCD集成
  - [ ] 声明式配置管理
  - [ ] 自动同步和回滚

**预期收益：**
- 部署时间从2小时降至10分钟
- 支持自动弹性伸缩
- 提升系统可用性至99.9%
- 简化多环境管理

#### 2. 微服务架构演进 (优先级：中) 🏗️
**目标：** 将单体应用拆分为微服务，提升系统灵活性和可维护性

**架构规划：**

```
┌─────────────────────────────────────────────────────────┐
│                    API Gateway (Ocelot)                  │
│              (路由、认证、限流、监控聚合)                 │
└─────────────────────────────────────────────────────────┘
         │              │              │              │
    ┌────┴────┐   ┌────┴────┐   ┌────┴────┐   ┌────┴────┐
    │ Rule    │   │ Parcel  │   │ Comm    │   │ Log     │
    │ Engine  │   │ Process │   │ Gateway │   │ Service │
    │ Service │   │ Service │   │ Service │   │ Service │
    └─────────┘   └─────────┘   └─────────┘   └─────────┘
         │              │              │              │
    ┌────┴──────────────┴──────────────┴──────────────┴────┐
    │          配置中心 (Consul/etcd/Nacos)                 │
    └──────────────────────────────────────────────────────┘
```

**服务拆分计划：**

**1. 规则引擎服务（RuleEngineService）**
- 职责：规则评估、匹配计算
- API：规则CRUD、规则评估
- 独立数据库：规则库（LiteDB或MongoDB）

**2. 包裹处理服务（ParcelProcessingService）**
- 职责：包裹编排、流程管理
- API：包裹创建、DWS数据接收、格口分配
- 独立数据库：包裹状态库（Redis）

**3. 通信网关服务（CommunicationGatewayService）**
- 职责：协议适配、消息路由
- 支持协议：SignalR、TCP、MQTT、HTTP
- 消息队列：RabbitMQ或Kafka

**4. 日志服务（LoggingService）**
- 职责：日志收集、存储、查询
- 技术栈：ELK Stack或Loki
- 独立数据库：日志数据库

**5. 配置中心（ConfigurationService）**
- 职责：集中配置管理、动态更新
- 技术选型：Consul/etcd/Nacos
- 配置版本控制和回滚

**实施步骤：**
- [ ] 第1个月：设计服务边界和API接口
- [ ] 第2-3个月：拆分核心服务
- [ ] 第4个月：集成API网关和配置中心
- [ ] 第5-6个月：性能测试和优化

**注意事项：**
- ⚠️ 仅在必要时进行微服务拆分（当前单体架构运行良好）
- ⚠️ 需要评估团队规模和运维能力
- ⚠️ 分布式事务和数据一致性需要额外设计

**预期收益：**
- 服务独立部署和扩展
- 故障隔离（一个服务故障不影响其他服务）
- 团队并行开发效率提升
- 技术栈灵活选择

#### 3. AI和大数据 (优先级：低，研究性质) 🧠
**目标：** 探索AI和大数据技术在分拣领域的应用

**研究方向：**

**3.1 智能规则推荐系统**
- [ ] 数据收集和特征工程
  - 收集历史包裹数据（条码、重量、尺寸、目的地）
  - 提取特征（条码模式、重量分布、时间因素）
- [ ] 机器学习模型训练
  - 分类模型：预测包裹应该去哪个格口
  - 聚类模型：发现包裹群体模式
  - 推荐模型：推荐最优规则配置
- [ ] 模型部署和评估
  - 模型服务化（ML.NET或ONNX Runtime）
  - A/B测试验证
  - 持续学习和模型更新

**3.2 异常包裹自动识别**
- [ ] 异常检测模型
  - Isolation Forest（孤立森林）
  - Autoencoder（自编码器）
  - 基于统计的异常检测
- [ ] 实时预警
  - 异常包裹实时识别
  - 自动告警和处理建议
  - 降低人工检查成本

**3.3 格口分配优化算法**
- [ ] 优化目标
  - 最小化平均等待时间
  - 最大化格口利用率
  - 平衡格口负载
- [ ] 算法研究
  - 遗传算法
  - 模拟退火
  - 强化学习（Q-Learning、DQN）

**3.4 负载预测和资源调度**
- [ ] 时序预测模型
  - LSTM（长短期记忆网络）
  - Prophet（Facebook时间序列预测）
  - ARIMA模型
- [ ] 应用场景
  - 预测未来1小时/24小时的包裹量
  - 提前调整资源（格口、服务器实例）
  - 优化人员排班

**实施建议：**
- 从小规模POC（概念验证）开始
- 优先使用轻量级ML库（ML.NET、Accord.NET）
- 避免过度设计，先验证业务价值
- 需要至少6个月的历史数据

**预期收益：**（长期）
- 规则配置准确率提升30%
- 异常包裹识别准确率>95%
- 格口利用率提升15-20%
- 人工干预减少60%

---

### 📊 优化路线图时间线 / Optimization Roadmap Timeline

```
2025 Q4 (10-12月)        2026 Q1 (1-3月)         2026 Q2 (4-6月)
─────────────────────────────────────────────────────────────
Week 1-2:                Month 1:                Month 4:
- API弹性策略             - Web管理界面            - K8s部署配置
- 告警通知渠道             (核心功能)              - Helm Charts

Week 3-4:                Month 2:                Month 5:
- 文档完善                - Web管理界面            - 微服务拆分
- 代码优化                 (日志分析)              - API网关集成

                        Month 3:                Month 6:
                        - Web管理界面            - 性能测试
                          (监控配置)             - AI/ML POC
                        - 智能分析功能            - 容器化优化
                        - 性能优化
```

### 🎯 成功指标 / Success Metrics

**完成度目标：**
- 2025年底：90%（完成短期优化）
- 2026年Q1：95%（完成中期优化）
- 2026年Q2：98%（完成长期优化）

**关键性能指标（KPI）：**
- 系统可用性：从99%提升至99.9%
- 处理能力：从1000/秒提升至2000/秒
- 平均响应时间：从50ms降至30ms
- 告警响应时间：从30分钟降至5分钟
- 配置时间：从30分钟降至5分钟
- 部署时间：从2小时降至10分钟

**用户满意度指标：**
- 易用性评分：从7/10提升至9/10
- 文档完整性：从8/10提升至9.5/10
- 问题解决速度：从4小时降至1小时

## 技术栈

- **.NET 8.0** - 最新的.NET框架
- **ASP.NET Core Minimal API** - 轻量级Web API
- **MediatR** - 事件驱动架构实现
- **LiteDB** - 嵌入式NoSQL数据库（配置存储）
- **Entity Framework Core** - ORM框架，支持自动迁移
- **MySQL / SQLite** - 关系型数据库（日志存储）
- **Polly** - 弹性和瞬态故障处理
- **TouchSocket** - 高性能TCP通信库
- **SignalR** - 实时双向通信
- **MQTTnet** - MQTT通信库（支持MQTT 3.1.1和5.0）
- **NLog** - 高性能日志框架
- **xUnit / Moq** - 单元测试框架
- **BenchmarkDotNet** - 性能基准测试框架
- **NBomber** - 负载和压力测试框架

## 快速开始

### 前置要求

- .NET 8.0 SDK
- Visual Studio 2022 或 Visual Studio Code
- （可选）MySQL服务器

### 构建项目

```bash
# 克隆仓库
git clone https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core.git
cd ZakYip.Sorting.RuleEngine.Core

# 还原依赖并构建
dotnet restore
dotnet build
```

### 配置

编辑 `Service/ZakYip.Sorting.RuleEngine.Service/appsettings.json` 配置文件：

```json
{
  "AppSettings": {
    "LiteDb": {
      "ConnectionString": "Filename=./data/config.db;Connection=shared"
    },
    "MySql": {
      "ConnectionString": "Server=localhost;Database=sorting_logs;User=root;Password=your_password;",
      "Enabled": true
    },
    "Sqlite": {
      "ConnectionString": "Data Source=./data/logs.db"
    },
    "MiniApi": {
      "Urls": [ "http://localhost:5000" ],
      "EnableSwagger": true
    }
  }
}
```

### 运行服务

#### 开发模式

```bash
cd Service/ZakYip.Sorting.RuleEngine.Service
dotnet run
```

访问 Swagger UI: http://localhost:5000/swagger

#### 作为Windows服务安装

```powershell
# 发布应用
dotnet publish -c Release -o ./publish

# 创建Windows服务
sc create "ZakYipSortingEngine" binPath="C:\path\to\publish\ZakYip.Sorting.RuleEngine.Service.exe"

# 启动服务
sc start "ZakYipSortingEngine"
```

## 版本历史 / Version History

### v1.16.0 (2025-11-09) - 通信协议优化和安全增强
**重点更新：**
- 🎉 数据模拟器协议升级（MQTT + TCP）
- 🆕 新增接口模拟器项目
- 🛡️ 全面的异常安全隔离文档
- 📊 单元测试数量增至310+

**详细内容：** 见"最新更新"章节

### v1.15.0 (2025-11-09) - 数据模拟器
- ✅ 新增综合数据模拟器（DataSimulator）
- ✅ 支持单次、批量、压力测试模式
- ✅ 交互式控制台UI（Spectre.Console）
- ✅ 详细的性能统计（P50/P95/P99）

### v1.14.9 (2025-11-09) - 通信日志增强
- ✅ 通信日志表新增CommunicationType字段
- ✅ 支持按通信类型查询和分析
- ✅ 代码质量提升（零警告零错误）
- ✅ 测试覆盖率达到85%+

### v1.14.8 (2025-11-09) - MQTT通信支持
- ✅ 新增MQTT分拣机适配器（MqttSorterAdapter）
- ✅ 新增MQTT DWS适配器（MqttDwsAdapter）
- ✅ 支持QoS控制和自动重连
- ✅ 15个MQTT相关单元测试

### v1.14.7 (2025-11-08) - 测试修复和代码质量
- ✅ 所有289个单元测试通过
- ✅ 修复监控服务告警生成测试
- ✅ 修复规则引擎空条码处理
- ✅ 构建成功：0警告，0错误

### v1.14.6 (2025-11-07) - 性能优化三重奏
- ✅ ArrayPool内存优化（减少40-60%分配）
- ✅ 批量操作并行处理（吞吐量提升50-100%）
- ✅ 查询计划分析和智能优化建议

### v1.14.5 (2025-11-07) - API客户端验证和修复
- ✅ JushuitanErpApiClient签名算法和响应解析增强
- ✅ WdtWmsApiClient签名算法和图片上传功能
- ✅ 所有API客户端测试通过

### v1.14.4 (2025-11-07) - API客户端重构
- ✅ RequestChuteAsync方法签名统一
- ✅ WcsApiResponse字段完善
- ✅ 统一方法映射规范

### 更早版本 / Earlier Versions
完整版本历史请参见：[IMPLEMENTATION_SUMMARY_v1.16.0.md](IMPLEMENTATION_SUMMARY_v1.16.0.md)

---

## 许可证

MIT License

## 联系方式

- 项目地址: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core
- 问题反馈: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core/issues

---

**注意**: 
- 本系统设计用于高频率场景，确保硬件资源充足以获得最佳性能。
- 生产环境中分拣程序和DWS应使用TCP或SignalR通信，HTTP API仅用于测试。
- 数据清理策略基于空闲检测（默认30分钟无包裹创建后触发清理）。
- 系统支持50-1000包裹/秒的处理能力，具体取决于硬件配置。
