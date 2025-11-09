# ZakYip.Sorting.RuleEngine

## 项目简介

ZakYip分拣规则引擎系统是一个高性能的包裹分拣规则引擎，用于处理分拣程序的包裹信息和DWS（尺寸重量扫描）数据，通过自定义规则分析计算格口号，实现自动化分拣。

## 主逻辑流程

### 包裹分拣完整流程

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

## 最新更新

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

## 最新更新

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

## 当前实现内容

### 核心功能
- ✅ **包裹分拣流程** - 完整的包裹创建、DWS数据接收、规则匹配、格口分配流程
- ✅ **规则引擎** - 支持6种匹配方法（条码正则、重量、体积、OCR、API响应、低代码表达式）
- ✅ **多规则匹配** - 一个格口可匹配多条规则，按优先级选择最佳规则
- ✅ **API适配器** - 统一的IWcsApiAdapter接口，支持多种第三方系统集成
  - PostCollectionApiClient - 邮政分揽投机构
  - PostProcessingCenterApiClient - 邮政处理中心
  - JushuitanErpApiClient - 聚水潭ERP
  - WdtWmsApiClient - 旺店通WMS
  - WcsApiClient - 通用WCS客户端
- ✅ **数据持久化** - LiteDB（配置）、MySQL（日志）、SQLite（降级）
- ✅ **数据库熔断** - MySQL失败自动降级到SQLite，恢复后完整同步
- ✅ **性能监控** - P50/P95/P99延迟统计，完整的性能指标收集

### 通信支持
- ✅ **SignalR Hub** - SortingHub和DwsHub，实时双向通信（生产环境推荐）
- ✅ **TouchSocket TCP** - 高性能TCP通信，支持连接池和自动重连
- ✅ **HTTP API** - 完整的REST API（仅用于测试和调试）
- ✅ **MQTT** - 基于MQTTnet的MQTT通信，支持QoS控制和自动重连（v1.14.8新增）
- ✅ **适配器热切换** - 运行时切换不同厂商设备，无需重启

### 数据管理
- ✅ **数据分片** - 按时间维度分表（日/周/月），支持热冷数据分离
- ✅ **自动清理** - 基于空闲策略的数据清理（默认90天保留）
- ✅ **数据归档** - 批量处理大数据集，自动归档冷数据
- ✅ **ArrayPool优化** - 批量操作使用ArrayPool减少内存分配40-60%
- ✅ **并行处理** - 归档服务支持并行批处理，吞吐量提升50-100%
- ✅ **查询优化** - 查询计划缓存、慢查询检测、智能优化建议

### 测试覆盖
- ✅ **单元测试** - 289个测试用例，覆盖核心功能
- ✅ **性能测试** - BenchmarkDotNet基准测试
- ✅ **压力测试** - NBomber高并发压力测试（支持100-1000包裹/秒）
- ✅ **测试控制台** - 模拟分拣机信号和DWS数据发送

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

## 代码质量指标

### 单元测试覆盖率
- **目标覆盖率**：≥ 85%
- **当前测试用例**：196+ 单元测试
- **CI/CD集成**：每次提交自动运行测试并生成覆盖率报告
- **质量门禁**：PR合并前必须达到85%覆盖率阈值

### SonarQube静态分析
- **平台**：SonarCloud (https://sonarcloud.io)
- **项目ID**：Hisoka6602_ZakYip.Sorting.RuleEngine.Core
- **分析频率**：每次push和PR时自动触发
- **质量门禁配置**：
  - 代码覆盖率 ≥ 85%
  - 代码重复率 ≤ 3%
  - 代码异味（Code Smells）：持续改进
  - 安全漏洞（Vulnerabilities）：0容忍
  - Bug：0容忍

### 代码文档覆盖率
- **目标文档覆盖率**：≥ 90%
- **文档要求**：
  - 所有公共类、接口、方法必须有XML文档注释
  - 复杂的私有方法需要添加说明注释
  - 关键业务逻辑需要详细的代码注释
  - 支持中英文双语文档
- **文档生成**：通过编译时XML文档生成，集成到NuGet包

### CI/CD工作流
系统配置了两个主要的CI/CD工作流：

#### 1. CI Build and Test (.github/workflows/ci.yml)
- 自动构建所有项目
- 运行所有单元测试
- 生成代码覆盖率报告（Cobertura格式）
- 生成HTML覆盖率报告
- 强制执行85%覆盖率阈值
- PR评论自动显示覆盖率变化

#### 2. SonarQube Analysis (.github/workflows/sonarqube.yml)
- 静态代码分析
- 代码质量评分
- 安全漏洞检测
- 代码异味识别
- 覆盖率集成
- 质量门禁自动检查

## 未来优化方向

### 短期优化（1-2周内）
1. **API客户端功能完善**
   - 添加Polly弹性策略到所有API客户端（重试、熔断、超时）
   - 实现强类型响应模型和自动解析
   - 支持批量操作以提高效率
   - 完善配置管理和认证机制

2. **监控告警增强** ✅ 已完成
   - ✅ 实时包裹处理量监控
   - ✅ 格口使用率监控和告警
   - ✅ 系统性能指标监控
   - ✅ 错误率和异常监控告警
   - 邮件/短信/企业微信通知（计划中）

3. **代码质量改进** ✅ 已完成
   - ✅ 提升代码文档覆盖率至90%
   - ✅ 集成SonarQube静态分析
   - ✅ 增加单元测试覆盖率至85%

### 中期优化（1-3个月）
1. **Web管理界面开发**（高优先级）
   - 规则管理界面（创建、编辑、测试规则）
   - 格口管理界面（格口配置和使用统计）
   - 日志查询和分析界面（支持多维度过滤和导出）
   - 系统配置界面（实时配置更新）
   - 性能监控仪表板（P50/P95/P99延迟图表）

2. **智能分析功能**
   - 基于历史数据的规则优化建议
   - 异常模式识别和自动规则生成
   - 格口利用率分析和优化建议

3. **性能优化**
   - 引入Redis分布式缓存支持多实例部署
   - 数据库查询优化（应用QueryOptimizationExtensions到更多场景）
   - 批量处理优化和并行处理增强

### 长期优化（3-6个月）
1. **容器化和云原生**
   - Docker镜像构建和优化
   - Kubernetes部署配置
   - Helm Charts包管理
   - CI/CD流水线（GitHub Actions）

2. **微服务架构演进**
   - 规则引擎服务独立
   - 包裹处理服务独立
   - 通信网关服务
   - 日志服务独立
   - 配置中心和API网关

3. **AI和大数据**
   - 智能规则推荐系统
   - 异常包裹自动识别
   - 格口分配优化算法
   - 负载预测和资源调度

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
