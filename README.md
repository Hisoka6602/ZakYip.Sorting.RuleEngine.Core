# ZakYip.Sorting.RuleEngine

## 项目简介

ZakYip分拣规则引擎系统是一个高性能的包裹分拣规则引擎，用于处理分拣程序的包裹信息和DWS（尺寸重量扫描）数据，通过自定义规则分析计算格口号，实现自动化分拣。

## 主逻辑流程

### 包裹分拣完整流程

当系统接收到创建包裹消息后，会执行以下步骤：

#### 1. 分拣机发送包裹创建信号
- **触发方式**：分拣机通过 TCP/SignalR/HTTP 发送包裹创建请求
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
- **触发方式**：DWS设备通过 TCP/SignalR/HTTP 发送测量数据
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
  1. **优先使用API响应** - 如果第三方API返回成功且包含格口号
  2. **使用规则引擎** - 如果API失败或无返回，执行规则匹配
- **规则匹配过程**：
  1. 从缓存加载所有启用的规则（按优先级排序）
  2. 依次评估每条规则，支持6种匹配方法：
     - **BarcodeRegex** - 条码正则匹配（StartsWith、Contains、Regex等）
     - **WeightMatch** - 重量范围匹配
     - **VolumeMatch** - 体积/尺寸匹配
     - **OcrMatch** - OCR识别结果匹配（地址段码、电话后缀）
     - **ApiResponseMatch** - API响应内容匹配
     - **LowCodeExpression** - 低代码表达式（混合多种条件）
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
- **通知方式**：通过分拣机适配器（TCP/SignalR/HTTP）发送结果
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
| SorterCommunicationLog | 分拣机通信日志 | ParcelId, CartNumber, 原始内容 |
| DwsCommunicationLog | DWS通信日志 | Barcode, Weight, Volume, 测量数据 |
| ApiCommunicationLog | 第三方API调用日志 | RequestUrl, RequestBody, ResponseBody, 耗时 |
| MatchingLog | 规则匹配日志 | RuleId, MatchingReason, ChuteId, 匹配依据 |
| ApiRequestLog | HTTP请求日志 | Method, Path, StatusCode, IP地址 |
| CommunicationLog | 通用通信日志 | Direction, Type, Message, 成功状态 |
| LogEntry | 系统日志 | Level, Message, Exception |

## 最新更新

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
- ✅ **适配器热切换** - 运行时切换不同厂商设备，无需重启

### 数据管理
- ✅ **数据分片** - 按时间维度分表（日/周/月），支持热冷数据分离
- ✅ **自动清理** - 基于空闲策略的数据清理（默认90天保留）
- ✅ **数据归档** - 批量处理大数据集，自动归档冷数据
- ✅ **ArrayPool优化** - 批量操作使用ArrayPool减少内存分配40-60%
- ✅ **并行处理** - 归档服务支持并行批处理，吞吐量提升50-100%
- ✅ **查询优化** - 查询计划缓存、慢查询检测、智能优化建议

### 测试覆盖
- ✅ **单元测试** - 196+ 测试用例，覆盖核心功能
- ✅ **性能测试** - BenchmarkDotNet基准测试
- ✅ **压力测试** - NBomber高并发压力测试（支持100-1000包裹/秒）
- ✅ **测试控制台** - 模拟分拣机信号和DWS数据发送

## 未来优化方向

### 短期优化（1-2周内）
1. **API客户端功能完善**
   - 添加Polly弹性策略到所有API客户端（重试、熔断、超时）
   - 实现强类型响应模型和自动解析
   - 支持批量操作以提高效率
   - 完善配置管理和认证机制

2. **代码质量改进**
   - 提升代码文档覆盖率从70%至90%
   - 集成SonarQube静态分析
   - 增加单元测试覆盖率至85%

3. **监控告警系统**
   - 实时包裹处理量监控
   - 格口使用率监控和告警
   - 系统性能指标监控
   - 错误率和异常监控告警
   - 邮件/短信/企业微信通知

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
