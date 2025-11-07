# ZakYip.Sorting.RuleEngine

## 项目简介

ZakYip分拣规则引擎系统是一个高性能的包裹分拣规则引擎，用于处理分拣程序的包裹信息和DWS（尺寸重量扫描）数据，通过自定义规则分析计算格口号，实现自动化分拣。

## 最新更新

### v1.14.4 (2025-11-07)
- ✅ **API客户端重构完成** - 按照参考代码重新实现API适配器
  - **RequestChuteAsync方法签名更新** - 现在接受ParcelId、DwsData、OcrData参数
    - 对应参考代码中的UploadData方法
    - 包含完整的DWS数据（重量、长度、宽度、高度、体积）
    - 可选的OCR数据支持
  - **WcsApiResponse字段完善** - 与ApiCommunicationLog保持一致
    - 添加ParcelId、RequestUrl、RequestBody、RequestHeaders
    - 添加ResponseTime、DurationMs、ResponseStatusCode
    - 添加ErrorMessage、FormattedCurl等字段
    - 支持完整的通信日志持久化
  - **PostCollectionApiClient（邮政分揽投机构）**
    - RequestChuteAsync对应UploadData，包含DWS数据上传
    - ScanParcelAsync对应SubmitScanInfo
    - UploadImageAsync留空实现（根据要求）
  - **PostProcessingCenterApiClient（邮政处理中心）**
    - RequestChuteAsync对应UploadData，包含DWS数据上传
    - ScanParcelAsync对应SubmitScanInfo
    - UploadImageAsync留空实现（根据要求）
  - **JushuitanErpApiClient（聚水潭ERP）**
    - RequestChuteAsync使用DwsData中的实际重量
    - ScanParcelAsync不支持（返回不支持消息）
  - **WdtWmsApiClient（旺店通WMS）**
    - RequestChuteAsync使用DwsData中的实际重量
    - ScanParcelAsync不支持（返回不支持消息）
  - **WcsApiClient（通用WCS客户端）**
    - RequestChuteAsync支持完整DWS和OCR数据
  - **所有调用代码和测试已更新** - 匹配新的方法签名

### v1.14.3 (2025-11-07)
- ✅ **API客户端重构** - 统一命名规范和方法映射
  - 重命名JushuitanErpApiAdapter → JushuitanErpApiClient
  - 重命名WdtWmsApiAdapter → WdtWmsApiClient
  - 重命名PostCollectionApiAdapter → PostCollectionApiClient
  - 重命名PostProcessingCenterApiAdapter → PostProcessingCenterApiClient
  - 所有API客户端统一使用ApiClient后缀
  - RequestChuteAsync方法对应参考代码中的UploadData方法
  - ScanParcelAsync方法对应参考代码中的SubmitScanInfo方法
  - JushuitanErpApiClient和WdtWmsApiClient的ScanParcelAsync返回不支持消息
  - 更新所有相关引用和测试文件

## 当前实现内容

### 核心功能
- ✅ **包裹分拣流程** - 完整的包裹创建、DWS数据接收、规则匹配、格口分配流程
- ✅ **规则引擎** - 支持6种匹配方法（条码正则、重量、体积、OCR、API响应、低代码表达式）
- ✅ **API适配器** - 统一的IWcsApiAdapter接口，支持多种第三方系统集成
  - PostCollectionApiClient - 邮政分揽投机构API客户端
  - PostProcessingCenterApiClient - 邮政处理中心API客户端
  - JushuitanErpApiClient - 聚水潭ERP API客户端
  - WdtWmsApiClient - 旺店通WMS API客户端
  - WcsApiClient - 通用WCS API客户端
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
- ✅ **日志系统** - NLog日志框架，多种专用日志表

### 测试覆盖
- ✅ **单元测试** - 232+ 测试用例，覆盖核心功能
- ✅ **性能测试** - BenchmarkDotNet基准测试
- ✅ **压力测试** - NBomber高并发压力测试（支持100-1000包裹/秒）
- ✅ **测试控制台** - 模拟分拣机信号和DWS数据发送

## 当前存在的问题

### 已知问题
1. **API客户端增强待实现** - 当前基础实现完成，待增强：
   - ⏳ Polly弹性策略（重试、熔断、超时）待添加到各API客户端
   - ⏳ 强类型响应模型解析待实现
   - ⏳ 批量操作支持（批量扫描、批量请求格口）待实现
   - ⏳ 配置文件管理（端点、超时、认证参数）待完善
   - ⏳ 响应缓存机制待实现

2. **代码质量提升中**
   - ⏳ 代码文档覆盖率需从70%提升至90%以上
   - ⏳ 静态代码分析（SonarQube）待集成
   - ⏳ 单元测试覆盖率需从当前提升至85%以上

3. **Web管理界面缺失**
   - ⏳ 规则管理界面待开发
   - ⏳ 格口管理界面待开发
   - ⏳ 日志查询和分析界面待开发
   - ⏳ 性能监控仪表板待开发

## 未来优化方向

### 短期优化（1-2周）
1. **API客户端功能完善**
   - 添加Polly弹性策略到所有API客户端
   - 实现强类型响应模型和自动解析
   - 支持批量操作以提高效率
   - 完善配置管理和认证机制

2. **代码质量改进**
   - 提升代码文档覆盖率至90%
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
   - 数据库查询优化（应用QueryOptimizationExtensions到更多场景）
   - 引入Redis分布式缓存支持多实例部署
   - 批量处理优化和并行处理增强

### 长期优化（3-6个月）
1. **容器化和云原生**（优先级提高）
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

## 快速开始

### 前置要求

- .NET 8.0 SDK
- Visual Studio 2022 或 Visual Studio Code
- （可选）MySQL服务器

### 构建项目

```bash
# 克隆仓库
git clone https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core.git
cd ZakYip.Sorting.RuleEngine

# 还原依赖并构建
dotnet restore
dotnet build
```

### 配置

编辑 `ZakYip.Sorting.RuleEngine.Service/appsettings.json` 配置文件：

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
cd ZakYip.Sorting.RuleEngine.Service
dotnet run
```

访问 Swagger UI: http://localhost:5000/swagger

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
- ✅ **日志安全性增强** - 防止敏感信息泄露
  - 生产环境完全禁止SQL语句和表名记录
  - EF Core配置条件编译：DEBUG模式启用详细日志，Release禁用敏感数据
  - NLog配置更新：仅Error级别记录EF Core异常，包含完整堆栈用于调试
  - appsettings.json日志级别优化：EntityFrameworkCore.Database.Command设为None
  - SignalR详细错误仅在DEBUG模式启用
- ✅ **数据精度提升** - double替换为decimal
  - GanttChartDto的Weight和Volume改用decimal类型
  - ParcelProcessRequest的Range验证使用实际最大值（999999999）
  - TestConsole输入解析改用decimal.TryParse
  - 保留必要的double转换（Polly库接口要求）
  - 所有物理测量字段统一使用decimal，提高数值计算精度
- ✅ **布尔字段命名规范验证** - 已符合最佳实践
  - 所有布尔字段使用Is/Has前缀（IsEnabled、IsSuccess、IsResolved等）
  - Success字段在响应DTO中使用，符合常见约定
  - 无需修改，现有命名已规范

### v1.13.0 (2025-11-04)
- ✅ **API响应DTO完全规范化** - 所有查询API统一使用标准响应格式
  - 所有GET查询接口使用ApiResponse<T>或PagedResponse<T>包装
  - ChuteController: 格口查询接口标准化
  - RuleController: 规则查询接口标准化
  - LogController: 日志查询接口标准化（匹配日志、DWS通信、API通信、分拣机通信）
  - ThirdPartyApiConfigController: 第三方API配置查询接口标准化（自动脱敏）
  - VersionController: 版本信息查询接口标准化
  - 统一错误码和错误消息格式
  - 完整的Swagger文档注释（中文）
- ✅ **健康检查已完整实现** - 详细的组件健康状态监控（v1.12.0已完成）
  - MySQL数据库健康检查
  - SQLite数据库健康检查
  - 内存缓存健康检查
  - 第三方API健康检查
  - /health - 简单健康检查
  - /health/detail - 详细健康状态（包含各组件耗时、错误信息）
  - /health/ready - 就绪检查（数据库）
  - /health/live - 存活检查

### v1.12.1 (2025-11-04)
- ✅ **代码质量修复** - 修复编译和测试问题
  - 修复HighConcurrencyStressTests.cs编译错误（缺少类结束大括号）
  - 修复taskId变量未定义问题（添加循环变量捕获）
  - 修复NBomber场景名称必须以字母开头的问题
  - 所有196个单元测试通过
  - 编译成功，无警告无错误

### v1.12.0 (2025-11-04)
- ✅ **查询API响应DTO规范化** - 所有查询API使用标准响应格式
  - 新增ApiResponse<T>通用响应包装器
  - 新增PagedResponse<T>分页响应包装器
  - LogController查询接口已更新使用标准响应
  - 完整的Swagger文档注释（中文）
- ✅ **异常隔离器（Polly策略）** - 全面的弹性和容错机制
  - 数据库操作重试策略（指数退避：2s, 4s, 8s）
  - 第三方API熔断策略（可配置失败率、熔断时长）
  - 超时策略（数据库30秒、API30秒）
  - 组合策略支持（重试+熔断+超时）
- ✅ **健康检查增强** - 详细的组件健康状态监控
  - MySQL数据库健康检查
  - SQLite数据库健康检查
  - 内存缓存健康检查
  - 第三方API健康检查
  - /health/detail - 详细健康状态（包含各组件耗时、错误信息）
  - /health/ready - 就绪检查（数据库）
  - /health/live - 存活检查
- ✅ **日志归档优化** - 批量处理和并行优化
  - 批量处理大数据集（每批1000条）
  - 并行统计查询
  - 进度报告和错误恢复
  - 自动熔断失败过多的归档操作
  - 批次间延迟避免数据库压力过大

### v1.11.0 (2025-11-04)
- ✅ **完整数据库降级还原逻辑** - 增强数据库熔断器的自动恢复功能
  - MySQL恢复后自动同步SQLite所有数据到MySQL（之前仅同步LogEntry表）
  - 新增支持表：CommunicationLog、SorterCommunicationLog、DwsCommunicationLog、ApiCommunicationLog、MatchingLog、ApiRequestLog
  - 同步完成后自动清空SQLite所有表数据
  - 执行VACUUM命令压缩SQLite磁盘空间，释放已删除数据占用的空间
  - 防止SQLite数据库文件持续增长
  - 确保数据完整性，无数据丢失

### v1.10.0 (2025-10-25)
- ✅ **数据库迁移重置** - 清空并重新生成所有数据库迁移
  - 删除所有历史迁移文件，简化迁移历史
  - 重新生成MySQL初始迁移 (20251025042050_InitialCreate)
  - 重新生成SQLite初始迁移 (20251025042107_InitialCreate)
  - 新迁移包含所有现有数据库架构（格口、专用日志表、性能指标等）
  - 构建成功，所有测试通过
  - 简化部署流程，统一初始数据库结构

## 核心特性

- ✅ **Windows服务** - 作为Windows服务运行，稳定可靠
- ✅ **MiniAPI集成** - 内置Web API用于前端配置和交互
- ✅ **事件驱动架构** - 使用MediatR实现事件驱动，支持分拣程序信号接收和FIFO队列处理
- ✅ **数据分片** - 使用EFCore.Sharding实现时间维度分表，支持热冷数据分离
- ✅ **高性能设计** - 使用HTTP客户端池化、可配置缓存（绝对/滑动过期）、异步处理等技术，适合高频率场景（50次/秒）
- ✅ **多数据库支持** - LiteDB存储配置，MySQL记录日志，SQLite作为降级方案，支持EF Core自动迁移，优化索引和降序排序
- ✅ **弹性架构** - 数据库熔断器（可配置失败率、熔断时长），自动降级和完整数据同步，防止系统雪崩
  - MySQL失败时自动降级到SQLite存储所有日志数据
  - MySQL恢复后自动将SQLite所有表数据回填到MySQL
  - 自动清空SQLite数据并执行VACUUM压缩磁盘空间
  - 支持7种日志表完整同步：LogEntry、CommunicationLog、SorterCommunicationLog、DwsCommunicationLog、ApiCommunicationLog、MatchingLog、ApiRequestLog
  - Polly策略：数据库重试、API熔断、超时控制（v1.12.0新增）
- ✅ **自动数据管理** - 可配置的数据清理（默认90天）和批量归档服务，自动维护数据生命周期
  - 批量处理大数据集（每批1000条）
  - 并行统计查询优化
  - 进度报告和错误恢复（v1.12.0新增）
- ✅ **MySQL自动调谐** - 性能监控、索引分析、连接池优化和慢查询识别
- ✅ **多协议支持** - 支持TCP/SignalR等多种协议，通过适配器模式扩展多厂商对接
- ✅ **SignalR Hub** - 提供SortingHub和DwsHub实现实时双向通信，生产环境推荐使用
- ✅ **热切换支持** - 适配器管理器支持运行时热切换，无需重启服务即可切换DWS/分拣机适配器
- ✅ **TouchSocket优化** - 使用TouchSocket库实现高性能TCP通信，支持连接池和自动重连
- ✅ **通信日志** - 全量记录TCP/SignalR/HTTP通信日志，支持问题追踪和调试
- ✅ **测试工具** - 提供分拣机信号模拟测试控制台，便于开发和测试
- ✅ **自动迁移** - EF Core自动应用数据库迁移，部署时自动创建表和索引
- ✅ **清晰架构** - 采用DDD分层架构，所有类、枚举、事件独立文件存放
- ✅ **中央包管理** - 使用Directory.Packages.props统一管理NuGet包版本
- ✅ **完整测试** - 单元测试覆盖核心功能
- ✅ **中文注释** - 核心代码使用中文注释
- ✅ **性能监控** - 全面的性能指标收集和监控（v1.5.0新增）
- ✅ **多种匹配方法** - 支持6种匹配方法：条码正则、重量、体积、OCR、API响应、低代码表达式（v1.5.0新增）
- ✅ **多规则匹配** - 一个格口可匹配多条规则，按优先级选择（v1.5.0新增）
- ✅ **标准化API响应** - 所有查询API使用统一响应格式（ApiResponse、PagedResponse）（v1.13.0全面完成）
- ✅ **增强健康检查** - 详细的组件健康状态监控（数据库、缓存、第三方API）（v1.12.0完成）

## 已实现功能总览 (截至 v1.13.0)

### 核心业务功能
- ✅ **包裹分拣流程** - 完整的包裹创建、DWS数据接收、规则匹配、格口分配流程
- ✅ **规则引擎** - 支持6种匹配方法（条码正则、重量、体积、OCR、API响应、低代码表达式）
- ✅ **多规则匹配** - 一个格口支持多条规则，按优先级自动选择最佳规则
- ✅ **规则安全验证** - 防止代码注入，验证表达式安全性
- ✅ **格口管理** - 完整的格口CRUD操作，支持启用/禁用状态管理
- ✅ **第三方API集成** - 支持多个第三方API配置，可配置优先级和超时

### 通信与协议
- ✅ **SignalR实时通信** - 提供SortingHub和DwsHub，支持双向通信和自动重连
- ✅ **TCP通信** - 基于TouchSocket的高性能TCP通信，支持连接池和自动重连
- ✅ **HTTP API** - 完整的REST API，支持包裹处理、规则管理、日志查询等
- ✅ **适配器模式** - 支持多厂商设备对接，运行时热切换无需重启
- ✅ **通信日志** - 全量记录所有TCP/SignalR/HTTP通信，支持问题追踪

### 数据持久化
- ✅ **多数据库支持** - LiteDB（配置）、MySQL（日志）、SQLite（降级）
- ✅ **EF Core自动迁移** - 部署时自动创建/更新数据库结构
- ✅ **数据分片** - 基于时间维度的表分区（日/周/月），支持热冷数据分离
- ✅ **专用日志表** - 分拣机通信日志、DWS通信日志、API通信日志、匹配日志、API请求日志
- ✅ **自动数据清理** - 基于空闲策略的数据清理（默认90天保留期）
- ✅ **数据归档** - 自动归档冷数据（默认30天阈值）
- ✅ **完整数据同步** - MySQL恢复后自动回填SQLite所有表数据并压缩磁盘

### 性能与弹性
- ✅ **高性能设计** - 对象池、内存缓存、异步处理、批量处理
- ✅ **缓存策略** - 可配置的绝对/滑动过期时间
- ✅ **数据库熔断器** - MySQL失败自动降级到SQLite，恢复后自动同步7种日志表
- ✅ **HTTP客户端池化** - 避免端口耗尽
- ✅ **性能监控** - P50/P95/P99延迟统计，规则评估、API调用、数据库操作时长追踪
- ✅ **性能基准测试** - 基于BenchmarkDotNet的完整性能测试项目
- ✅ **MySQL自动调谐** - 表统计分析、索引使用分析、连接池监控、慢查询识别

### 监控与日志
- ✅ **NLog日志框架** - 多目标日志（控制台、文件、错误、性能），自动轮转归档
- ✅ **通信日志记录** - 记录所有通信细节，包括原始内容、格式化内容、时间戳
- ✅ **API请求日志** - 记录所有HTTP请求/响应，包括耗时、状态码、IP地址
- ✅ **性能指标收集** - 自动收集和统计各类操作的性能指标
- ✅ **日志查询API** - 支持按时间、包裹ID、类型等条件查询和导出日志
- ✅ **甘特图数据API** - 查询指定包裹前后N条数据，支持时间线可视化

### 开发与测试
- ✅ **单元测试** - 182个单元测试，覆盖核心功能
- ✅ **性能基准测试** - 规则匹配性能测试（条码、重量、体积）
- ✅ **测试控制台** - 模拟分拣机信号和DWS数据发送
- ✅ **Swagger文档** - 自动生成的API文档
- ✅ **全局模型验证** - 自动验证所有API请求参数

### 架构与代码质量
- ✅ **DDD分层架构** - 清晰的领域层、应用层、基础设施层、服务层
- ✅ **事件驱动架构** - 基于MediatR的事件驱动设计
- ✅ **中央包管理** - Directory.Packages.props统一管理依赖
- ✅ **中文注释** - 核心代码全中文注释
- ✅ **独立文件** - 所有类、枚举、事件独立文件存放
- ✅ **条件编译** - Debug模式下控制台应用，Release模式下Windows服务

### 运维与部署
- ✅ **Windows服务** - 作为Windows服务运行，稳定可靠
- ✅ **自动迁移** - EF Core自动应用数据库迁移
- ✅ **健康检查** - /health端点监控服务状态
- ✅ **Kestrel优化** - 连接数、缓冲区、超时等配置优化
- ✅ **日志文件清理** - 自动清理过期日志文件（默认7天）
- ✅ **版本信息API** - 查询系统版本和构建信息

## 项目完成度

### 核心功能完成度：100%
- ✅ 包裹分拣核心流程 - 100%
- ✅ 规则引擎与匹配 - 100%
- ✅ 多协议通信支持 - 100%
- ✅ 数据库降级与恢复 - 100%
- ✅ 性能监控与优化 - 100%
- ✅ API响应DTO规范化 - 100%（v1.13.0完成）

### 生产就绪度：95%
- ✅ 弹性与容错机制 - 100%
- ✅ 监控与日志系统 - 100%
- ✅ 性能优化 - 95%
- ✅ 测试覆盖 - 85%
- ✅ API文档完善度 - 95%（v1.13.0提升）
- ✅ 健康检查增强 - 100%（v1.12.0完成）

### 推荐生产环境配置
- ✅ MySQL + SQLite双数据库架构
- ✅ SignalR或TouchSocket TCP通信
- ✅ 数据库熔断器（50%失败率，20分钟熔断）
- ✅ 自动数据清理（90天保留，30天归档）
- ✅ NLog日志轮转（7天保留）
- ⏳ 建议配合负载均衡（规划中）

### 当前版本：v1.14.0
- **稳定性**：生产级（Production-Ready）
- **性能**：50次/秒包裹处理能力
- **可靠性**：自动降级恢复，零数据丢失
- **可维护性**：清晰分层架构，完整中文注释
- **可扩展性**：适配器模式，热切换支持
- **代码质量**：196个单元测试全部通过，8个nullability警告
- **API标准化**：所有查询接口统一响应格式（v1.13.0）
- **健康监控**：完整的组件健康检查（v1.12.0）
- **安全性**：生产环境禁止SQL日志，防止敏感信息泄露（v1.14.0）
- **数据精度**：所有物理测量使用decimal类型（v1.14.0）

## 架构设计

项目采用清晰的分层架构（Clean Architecture / DDD）：

```
ZakYip.Sorting.RuleEngine/
├── ZakYip.Sorting.RuleEngine.Domain/          # 领域层
│   ├── Entities/                              # 实体
│   │   ├── ParcelInfo.cs                      # 包裹信息实体
│   │   ├── DwsData.cs                         # DWS数据实体
│   │   ├── SortingRule.cs                     # 分拣规则实体
│   │   └── ThirdPartyResponse.cs              # 第三方API响应实体
│   └── Interfaces/                            # 领域接口
│       ├── IRuleEngineService.cs              # 规则引擎服务接口
│       ├── IRuleRepository.cs                 # 规则仓储接口
│       ├── IThirdPartyApiClient.cs            # 第三方API客户端接口
│       └── ILogRepository.cs                  # 日志仓储接口
│
├── ZakYip.Sorting.RuleEngine.Application/     # 应用层
│   ├── Services/                              # 应用服务
│   │   ├── ParcelProcessingService.cs         # 包裹处理服务（高性能）
│   │   └── RuleEngineService.cs               # 规则引擎服务（带缓存）
│   ├── DTOs/                                  # 数据传输对象
│   │   ├── ParcelProcessRequest.cs            # 包裹处理请求
│   │   └── ParcelProcessResponse.cs           # 包裹处理响应
│   └── Interfaces/                            # 应用接口
│       └── IParcelProcessingService.cs        # 包裹处理服务接口
│
├── ZakYip.Sorting.RuleEngine.Infrastructure/  # 基础设施层
│   ├── Persistence/                           # 持久化实现
│   │   ├── LiteDb/                           # LiteDB实现（配置存储）
│   │   │   └── LiteDbRuleRepository.cs       # 规则仓储实现
│   │   ├── MySql/                            # MySQL实现（日志存储）
│   │   │   ├── MySqlLogDbContext.cs          # MySQL数据库上下文
│   │   │   └── MySqlLogRepository.cs         # MySQL日志仓储
│   │   └── Sqlite/                           # SQLite实现（降级方案）
│   │       ├── SqliteLogDbContext.cs         # SQLite数据库上下文
│   │       └── SqliteLogRepository.cs        # SQLite日志仓储
│   └── ApiClients/                           # API客户端
│       └── ThirdPartyApiClient.cs            # 第三方API客户端实现
│
└── ZakYip.Sorting.RuleEngine.Service/         # 服务层（Windows Service + MiniAPI）
    ├── API/                                   # API控制器
    │   ├── ParcelController.cs               # 包裹处理API
    │   └── RuleController.cs                 # 规则管理API
    ├── Configuration/                         # 配置
    │   └── AppSettings.cs                    # 应用配置类
    ├── appsettings.json                      # 配置文件
    └── Program.cs                            # 主程序入口
```

## 技术栈

- **.NET 8.0** - 最新的.NET框架
- **ASP.NET Core Minimal API** - 轻量级Web API
- **Kestrel** - 高性能Web服务器
- **MediatR** - 事件驱动架构实现
- **EFCore.Sharding** - 时间维度数据分片
- **LiteDB** - 嵌入式NoSQL数据库（配置存储）
- **Entity Framework Core** - ORM框架，支持自动迁移
- **MySQL / SQLite** - 关系型数据库（日志存储）
- **Polly** - 弹性和瞬态故障处理（重试、熔断器）
- **IMemoryCache** - 滑动过期内存缓存
- **System.Threading.Channels** - FIFO队列实现
- **TouchSocket** - 高性能TCP通信库
- **SignalR** - 实时双向通信
- **NLog** - 高性能日志框架
- **Newtonsoft.Json** - JSON序列化库
- **Swagger/OpenAPI** - API文档
- **Object Pool** - 对象池优化性能
- **xUnit / Moq** - 单元测试框架
- **BenchmarkDotNet** - 性能基准测试框架
- **Windows Services** - Windows服务托管

## 快速开始

### 前置要求

- .NET 8.0 SDK
- Visual Studio 2022 或 Visual Studio Code
- （可选）MySQL服务器

### 构建项目

```bash
# 克隆仓库
git clone https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core.git
cd ZakYip.Sorting.RuleEngine

# 还原依赖并构建
dotnet restore
dotnet build
```

### 配置

编辑 `ZakYip.Sorting.RuleEngine.Service/appsettings.json` 配置文件：

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
    "ThirdPartyApi": {
      "BaseUrl": "https://api.example.com",
      "TimeoutSeconds": 30,
      "ApiKey": ""
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
cd ZakYip.Sorting.RuleEngine.Service
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

## 标准化API响应（v1.13.0全面完成）

系统所有查询API统一使用标准响应格式，提供一致的接口体验。

### 响应格式

#### 单个对象响应 - ApiResponse<T>

```json
{
  "success": true,
  "data": { /* 实际数据对象 */ },
  "errorMessage": null,
  "errorCode": null,
  "timestamp": "2025-11-04T07:00:00Z"
}
```

#### 分页响应 - PagedResponse<T>

```json
{
  "success": true,
  "data": [ /* 数据列表 */ ],
  "total": 100,
  "page": 1,
  "pageSize": 50,
  "totalPages": 2,
  "hasPreviousPage": false,
  "hasNextPage": true,
  "errorMessage": null,
  "errorCode": null,
  "timestamp": "2025-11-04T07:00:00Z"
}
```

### 已更新的API端点

**格口管理（ChuteController）**
- `GET /api/chute` - 获取所有格口（使用ApiResponse<IEnumerable<ChuteResponseDto>>）
- `GET /api/chute/{id}` - 根据ID获取格口（使用ApiResponse<ChuteResponseDto>）
- `GET /api/chute/code/{code}` - 根据编号获取格口（使用ApiResponse<ChuteResponseDto>）
- `GET /api/chute/enabled` - 获取启用的格口（使用ApiResponse<IEnumerable<ChuteResponseDto>>）

**规则管理（RuleController）**
- `GET /api/rule` - 获取所有规则（使用ApiResponse<IEnumerable<SortingRuleResponseDto>>）
- `GET /api/rule/enabled` - 获取启用的规则（使用ApiResponse<IEnumerable<SortingRuleResponseDto>>）
- `GET /api/rule/{ruleId}` - 根据ID获取规则（使用ApiResponse<SortingRuleResponseDto>）

**日志查询（LogController）**
- `GET /api/log/matching` - 匹配日志查询（使用PagedResponse<MatchingLogResponseDto>）
- `GET /api/log/dws-communication` - DWS通信日志查询（使用PagedResponse<DwsCommunicationLog>）
- `GET /api/log/api-communication` - API通信日志查询（使用PagedResponse<ApiCommunicationLog>）
- `GET /api/log/sorter-communication` - 分拣机通信日志查询（使用PagedResponse<SorterCommunicationLog>）

**第三方API配置（ThirdPartyApiConfigController）**
- `GET /api/thirdpartyapiconfig` - 获取所有API配置（使用ApiResponse<IEnumerable<ThirdPartyApiConfigResponseDto>>，API密钥已脱敏）
- `GET /api/thirdpartyapiconfig/enabled` - 获取启用的API配置（使用ApiResponse<IEnumerable<ThirdPartyApiConfigResponseDto>>）
- `GET /api/thirdpartyapiconfig/{id}` - 根据ID获取API配置（使用ApiResponse<ThirdPartyApiConfigResponseDto>）

**版本信息（VersionController）**
- `GET /api/version` - 获取系统版本信息（使用ApiResponse<VersionResponseDto>）

**甘特图数据（GanttChartController）**
- `GET /api/ganttchart/{target}` - 查询甘特图数据（使用专用GanttChartQueryResponse）
- `POST /api/ganttchart/query` - POST方式查询甘特图数据（使用专用GanttChartQueryResponse）

## 健康检查（v1.12.0增强，v1.13.0验证完成）

系统提供多层次的健康检查端点，支持监控各个组件的运行状态。

### 健康检查端点

#### 1. 简单健康检查
```http
GET /health
```
快速检查服务是否运行，返回简单状态。

#### 2. 详细健康检查
```http
GET /health/detail
```
返回所有组件的详细健康状态，包括：
- MySQL数据库连接状态
- SQLite数据库连接状态
- 内存缓存工作状态
- 第三方API可用性
- 每个组件的响应时间
- 错误信息（如果有）

响应示例：
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-04T07:00:00Z",
  "duration": 123.45,
  "checks": [
    {
      "name": "mysql",
      "status": "Healthy",
      "description": "MySQL数据库连接正常",
      "duration": 45.2,
      "exception": null,
      "tags": ["database", "mysql"]
    },
    {
      "name": "sqlite",
      "status": "Healthy",
      "description": "SQLite数据库连接正常",
      "duration": 12.3,
      "exception": null,
      "tags": ["database", "sqlite"]
    },
    {
      "name": "memory_cache",
      "status": "Healthy",
      "description": "内存缓存工作正常",
      "duration": 5.1,
      "exception": null,
      "tags": ["cache"]
    },
    {
      "name": "third_party_api",
      "status": "Healthy",
      "description": "第三方API可访问 (状态码: 200)",
      "duration": 234.5,
      "exception": null,
      "tags": ["external", "api"]
    }
  ]
}
```

#### 3. 就绪检查（Readiness）
```http
GET /health/ready
```
检查服务是否准备好接受流量（主要检查数据库）。

#### 4. 存活检查（Liveness）
```http
GET /health/live
```
检查服务进程是否存活。

## Polly弹性策略（v1.12.0新增）

系统使用Polly库实现全面的弹性和容错机制。

### 可用策略

#### 1. 数据库重试策略
- **重试次数**: 3次
- **重试间隔**: 指数退避（2秒、4秒、8秒）
- **适用场景**: 数据库超时、死锁、连接失败

#### 2. 第三方API熔断策略
- **失败阈值**: 50%（可配置）
- **采样时长**: 60秒（可配置）
- **最小吞吐量**: 10次请求（可配置）
- **熔断时长**: 60秒（可配置）
- **状态**: 开启、半开、关闭

#### 3. 超时策略
- **数据库超时**: 30秒（可配置）
- **API超时**: 30秒（可配置）
- **策略**: 悲观超时（立即取消）

#### 4. 组合策略
重试 → 熔断 → 超时，多层保护确保系统稳定性。

### 使用示例

```csharp
// 使用数据库重试策略
var retryPolicy = ResiliencePolicyFactory.CreateDatabaseRetryPolicy(logger);
await retryPolicy.ExecuteAsync(async () => {
    // 数据库操作
});

// 使用API组合策略（重试+熔断+超时）
var combinedPolicy = ResiliencePolicyFactory.CreateCombinedPolicy(logger);
await combinedPolicy.ExecuteAsync(async () => {
    // API调用
});
```

## 通信方式

### SignalR Hub（生产环境推荐）

系统提供SignalR Hub用于实时双向通信，这是**生产环境推荐的通信方式**。

#### 1. 分拣机Hub（SortingHub）

连接地址：`/hubs/sorting`

**方法：创建包裹处理空间**
```javascript
// 客户端调用
const result = await connection.invoke("CreateParcel", "PKG20241024001", "CART001", "1234567890123");
// 返回: { success: true, parcelId: "PKG20241024001", message: "包裹处理空间已创建，等待DWS数据" }
```

**接收格口号结果**
```javascript
// 客户端监听
connection.on("ReceiveChuteNumber", (parcelId, chuteNumber, cartNumber, cartCount) => {
    console.log(`包裹 ${parcelId} 分配到格口 ${chuteNumber}`);
});
```

#### 2. DWS Hub（DwsHub）

连接地址：`/hubs/dws`

**方法：接收DWS数据**
```javascript
// 客户端调用
const result = await connection.invoke("ReceiveDwsData", 
    "PKG20241024001", 
    "1234567890123", 
    1500,  // weight
    300,   // length
    200,   // width
    150,   // height
    9000   // volume
);
// 返回: { success: true, parcelId: "PKG20241024001", message: "DWS数据已接收，开始处理" }
```

#### 3. SignalR连接示例（C#）

```csharp
using Microsoft.AspNetCore.SignalR.Client;

// 分拣机连接
var sortingConnection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/sorting")
    .WithAutomaticReconnect()
    .Build();

// 监听格口号
sortingConnection.On<string, string, string, int>("ReceiveChuteNumber", 
    (parcelId, chuteNumber, cartNumber, cartCount) =>
{
    Console.WriteLine($"包裹 {parcelId} 分配到格口 {chuteNumber}");
});

await sortingConnection.StartAsync();

// 创建包裹
var result = await sortingConnection.InvokeAsync<object>(
    "CreateParcel", "PKG001", "CART001", "123456");

// DWS连接
var dwsConnection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/dws")
    .WithAutomaticReconnect()
    .Build();

await dwsConnection.StartAsync();

// 发送DWS数据
var dwsResult = await dwsConnection.InvokeAsync<object>(
    "ReceiveDwsData", "PKG001", "123456", 1500, 300, 200, 150, 9000);
```

#### 4. TouchSocket TCP通信（生产环境推荐）

系统使用TouchSocket库实现高性能TCP通信，支持连接池和自动重连。

**DWS TCP适配器配置**：
- 默认最大连接数：1000
- 可配置接收缓冲区大小：8192字节
- 可配置发送缓冲区大小：8192字节
- 支持连接池管理

**分拣机TCP适配器配置**：
- 自动重连间隔：5000毫秒
- 可配置缓冲区大小：8192字节
- 支持自动断线重连

**使用示例**：
```csharp
// DWS适配器
var dwsAdapter = new TouchSocketDwsAdapter(
    host: "192.168.1.100",
    port: 8001,
    logger: logger,
    communicationLogRepository: commLogRepo,
    maxConnections: 1000,        // 可选，默认1000
    receiveBufferSize: 8192,     // 可选，默认8192
    sendBufferSize: 8192         // 可选，默认8192
);

// 分拣机适配器
var sorterAdapter = new TouchSocketSorterAdapter(
    host: "192.168.1.200",
    port: 9000,
    logger: logger,
    communicationLogRepository: commLogRepo,
    reconnectIntervalMs: 5000,   // 可选，默认5000
    receiveBufferSize: 8192,     // 可选，默认8192
    sendBufferSize: 8192         // 可选，默认8192
);
```

### HTTP API（仅用于测试）

**注意**：HTTP API仅用于测试和调试，生产环境中分拣程序和DWS应使用TCP或SignalR通信。

#### 分拣机信号API

#### 1. 创建包裹处理空间

分拣程序推送包裹ID和小车号，系统创建处理空间等待DWS数据。

```http
POST /api/sortingmachine/create-parcel
Content-Type: application/json

{
  "parcelId": "PKG20241024001",
  "cartNumber": "CART001",
  "barcode": "1234567890123"
}
```

响应：

```json
{
  "success": true,
  "parcelId": "PKG20241024001",
  "message": "包裹处理空间已创建，等待DWS数据"
}
```

#### 2. 接收DWS数据

上传DWS内容，触发第三方API调用和规则匹配。

```http
POST /api/sortingmachine/receive-dws
Content-Type: application/json

{
  "parcelId": "PKG20241024001",
  "barcode": "1234567890123",
  "weight": 1500,
  "length": 300,
  "width": 200,
  "height": 150,
  "volume": 9000
}
```

响应：

```json
{
  "success": true,
  "parcelId": "PKG20241024001",
  "message": "DWS数据已接收，开始处理"
}
```

**完整流程说明**：
1. 分拣程序调用 `create-parcel` 创建包裹处理空间
2. 包裹进入FIFO队列，保证先进先出处理
3. 分拣程序调用 `receive-dws` 上传DWS数据
4. 系统自动上传数据到第三方API
5. 执行规则匹配，确定格口号
6. 将结果（格口号、包裹ID、小车号、占用小车数）发送给分拣程序
7. 关闭处理空间，从缓存删除

### 包裹处理API（兼容旧版）

#### 1. 处理单个包裹

```http
POST /api/parcel/process
Content-Type: application/json

{
  "parcelId": "PKG001",
  "cartNumber": "CART001",
  "barcode": "1234567890",
  "weight": 1500,
  "length": 300,
  "width": 200,
  "height": 150,
  "volume": 9000
}
```

响应：

```json
{
  "success": true,
  "parcelId": "PKG001",
  "chuteNumber": "CHUTE-A01",
  "errorMessage": null,
  "processingTimeMs": 45
}
```

#### 2. 批量处理包裹

```http
POST /api/parcel/process/batch
Content-Type: application/json

[
  { "parcelId": "PKG001", "cartNumber": "CART001", ... },
  { "parcelId": "PKG002", "cartNumber": "CART002", ... }
]
```

### 规则管理API

#### 1. 获取所有规则

```http
GET /api/rule
```

#### 2. 添加规则

```http
POST /api/rule
Content-Type: application/json

{
  "ruleId": "RULE001",
  "ruleName": "重量规则",
  "description": "重量大于1000克分配到A区",
  "priority": 1,
  "conditionExpression": "Weight > 1000",
  "targetChute": "CHUTE-A01",
  "isEnabled": true
}
```

#### 3. 更新规则

```http
PUT /api/rule/RULE001
Content-Type: application/json

{
  "ruleId": "RULE001",
  "ruleName": "重量规则（已更新）",
  ...
}
```

#### 4. 删除规则

```http
DELETE /api/rule/RULE001
```

## 规则表达式语法

规则引擎支持多种匹配方法，每种方法针对不同的使用场景进行了优化。

### 匹配方法类型

#### 1. 条码正则匹配 (BarcodeRegex)

支持预设选项和自定义正则表达式：

```
STARTSWITH:SF          # 条码以SF开头
CONTAINS:ABC           # 条码包含ABC
NOTCONTAINS:XYZ        # 条码不包含XYZ
ALLDIGITS              # 条码全为数字
ALPHANUMERIC           # 条码为字母+数字
LENGTH:5-10            # 条码长度在5-10之间
REGEX:^SF\d{6}$        # 自定义正则表达式
```

#### 2. 重量匹配 (WeightMatch)

支持复杂的逻辑表达式：

```
Weight > 50                           # 重量大于50克
Weight < 100 and Weight > 10          # 重量在10-100克之间
Weight > 1000 or Weight < 50          # 重量大于1000克或小于50克
```

#### 3. 体积匹配 (VolumeMatch)

支持长宽高和体积的复杂表达式：

```
Length > 20 and Width > 10            # 长度大于20且宽度大于10
Height = 20.5 or Volume > 200         # 高度等于20.5或体积大于200
Length > 500 or Width > 400           # 超大件判断
```

#### 4. OCR匹配 (OcrMatch)

支持地址段码和电话后缀匹配：

```
firstSegmentCode=^64\d*$              # 第一段码以64开头（使用正则）
recipientPhoneSuffix=1234             # 收件人电话后缀为1234
firstSegmentCode=^64\d*$ and recipientPhoneSuffix=1234  # 组合条件
```

#### 5. API响应内容匹配 (ApiResponseMatch)

支持字符串查找、正则查找和JSON匹配：

```
STRING:keyword                        # 在响应中查找关键字
REGEX:\d{3}                          # 使用正则表达式匹配
JSON:status=success                   # 匹配JSON字段
JSON:data.user.name=John              # 匹配嵌套JSON字段
```

#### 6. 低代码表达式匹配 (LowCodeExpression)

支持混合使用多种条件：

```
if(Weight>10) and firstSegmentCode=^64\d*$
Weight > 50 and Length > 300
Barcode=STARTSWITH:SF and Volume > 1000
```

#### 7. 传统表达式 (LegacyExpression - 默认)

兼容旧版本的表达式语法：

```
Weight > 1000          # 重量大于1000克
Barcode CONTAINS 'SF'  # 条码包含SF
Volume < 50000         # 体积小于50000立方厘米
DEFAULT                # 默认规则（匹配所有）
```

### 示例规则配置

```json
{
  "ruleId": "R001",
  "ruleName": "顺丰快递",
  "matchingMethod": "BarcodeRegex",
  "conditionExpression": "STARTSWITH:SF",
  "targetChute": "CHUTE-SF-01",
  "priority": 1,
  "isEnabled": true
}
```

详细文档请查看 [MATCHING_METHODS.md](./MATCHING_METHODS.md)

## 性能优化

系统采用多种性能优化策略：

1. **对象池** - 使用`ObjectPool`避免频繁对象创建
2. **滑动过期缓存** - 使用`IMemoryCache`，滑动过期5分钟，绝对过期30分钟
3. **异步处理** - 全面使用async/await模式
4. **批量处理** - 支持并行批量处理包裹
5. **连接复用** - HttpClient复用，减少连接开销
6. **TouchSocket优化** - TCP连接池（默认1000连接），自动重连（5秒间隔），缓冲区优化（8KB）
7. **索引优化** - 数据库表建立适当索引
8. **自动迁移** - EF Core自动应用数据库迁移，简化部署
9. **性能监控** - 全面的性能指标收集和监控（v1.5.0新增）

### 性能指标监控

系统自动收集以下性能指标：

- **规则评估时长** - 跟踪每次规则评估的执行时间
- **API调用时长** - 监控第三方API调用性能
- **数据库操作时长** - 跟踪数据库操作性能
- **成功率统计** - 统计操作成功率
- **P50/P95/P99延迟** - 提供详细的延迟分布统计

性能指标自动记录到日志系统，支持查询和分析。详细文档请查看 [PERFORMANCE_METRICS.md](./PERFORMANCE_METRICS.md)

## 弹性和降级策略

系统具备完善的弹性和降级机制：

1. **数据库熔断器** - 为MySQL数据库配置熔断器，防止级联故障
   - 可配置的失败率阈值（默认50%）
   - 可配置的熔断持续时间（默认20分钟）
   - 可配置的采样周期和最小吞吐量
   - MySQL失败时自动切换到SQLite存储所有日志
   - MySQL恢复后自动执行完整数据同步：
     - 同步所有7种日志表数据（LogEntry、CommunicationLog、SorterCommunicationLog、DwsCommunicationLog、ApiCommunicationLog、MatchingLog、ApiRequestLog）
     - 按时间顺序批量插入到MySQL
     - 同步成功后清空SQLite所有表
     - 执行VACUUM命令压缩SQLite数据库文件
     - 自动记录同步进度和结果
2. **HTTP客户端池化** - 使用HttpClientFactory池化管理HTTP连接
   - 避免端口耗尽问题
   - **注意：HTTP请求不使用熔断器**
3. **API容错** - 第三方API失败时仍可使用规则引擎
4. **规则缓存** - 数据库不可用时使用缓存规则
5. **可配置缓存** - 支持配置绝对过期和滑动过期时间

## 事件驱动架构

系统采用MediatR实现完整的事件驱动架构：

### 工作流程

```
1. 分拣程序发送信号 → 创建包裹处理空间（开辟缓存）
2. 包裹进入FIFO队列 → 保证先进先出处理
3. 接收DWS数据 → 触发数据处理事件
4. 上传第三方API → 获取额外信息
5. 执行规则匹配 → 确定格口号
6. 发送结果给分拣程序 → 完成分拣
7. 清理缓存空间 → 释放资源
```

### 领域事件

- **ParcelCreatedEvent** - 包裹创建事件
- **DwsDataReceivedEvent** - DWS数据接收事件
- **ThirdPartyResponseReceivedEvent** - 第三方API响应事件
- **RuleMatchCompletedEvent** - 规则匹配完成事件

详细说明请查看 [EVENT_DRIVEN_AND_SHARDING.md](./EVENT_DRIVEN_AND_SHARDING.md)

## 数据分片和自动管理

### 时间维度分表

使用EFCore.Sharding实现按时间维度的表分区：

- **月度分片** - 默认策略，适合中等数据量
- **日度分片** - 适合高频数据场景
- **周度分片** - 适合低频数据场景

### 热冷数据分离

- **热数据**: 最近30天，存储在主表，频繁访问
- **冷数据**: 30天以前，可归档到历史表，查询较少

### 自动数据清理

- 默认保留90天数据
- **基于空闲策略清理**（默认30分钟无包裹创建后触发）
- 清理可被新包裹创建打断
- 可配置清理策略和保留期

### 分片表自动管理

- **自动创建分片表**（根据策略提前创建）
- 支持日度、周度、月度分片
- 自动创建索引优化性能
- 每小时检查确保表存在

### MySQL自动调谐

后台服务自动监控和优化MySQL性能：

- **表统计分析** - 监控表大小和行数
- **索引使用分析** - 识别未使用的索引
- **连接池监控** - 优化连接池配置
- **慢查询识别** - 发现并优化慢查询

## 监控和日志

- **结构化日志** - 使用Microsoft.Extensions.Logging
- **数据库日志** - 持久化到MySQL或SQLite
- **通信日志** - 全量记录TCP/SignalR/HTTP通信，存储到communication_logs表
- **健康检查** - `/health` 端点监控服务状态
- **熔断器监控** - 记录熔断器状态变化和异常

## 多协议支持和热切换

系统通过适配器模式支持多种通信协议（TCP/SignalR），并支持运行时热切换。

**重要说明**：生产环境中，分拣程序和DWS设备应使用TCP或SignalR通信，不应使用HTTP API。HTTP API仅用于测试和调试。

### 邮政/WCS API客户端（IWcsApiAdapter）

系统提供了四种独立的API客户端实现（v1.14.3统一重命名），每个都根据其对应的参考代码实现：

#### 1. 邮政处理中心API客户端（PostProcessingCenterApiClient）
- **实现接口**：IWcsApiAdapter
- **基础URL**：`/api/post/processing`
- **参考实现**：[PostApi.cs](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)
- **特点**：独立实现，按照PostApi.cs的具体逻辑和数据结构
- **方法映射**：
  - `ScanParcelAsync` - 对应参考代码的SubmitScanInfo方法
  - `RequestChuteAsync` - 对应参考代码的UploadData方法
  - `UploadImageAsync` - 上传图片功能

#### 2. 邮政分揽投机构API客户端（PostCollectionApiClient）
- **实现接口**：IWcsApiAdapter
- **基础URL**：`/api/post/collection`
- **参考实现**：[PostInApi.cs](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)
- **特点**：独立实现，按照PostInApi.cs的具体逻辑和数据结构
- **方法映射**：
  - `ScanParcelAsync` - 对应参考代码的SubmitScanInfo方法
  - `RequestChuteAsync` - 对应参考代码的UploadData方法
  - `UploadImageAsync` - 上传图片功能

#### 3. 聚水潭ERP API客户端（JushuitanErpApiClient）
- **实现接口**：IWcsApiAdapter
- **参考实现**：[参考代码](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)
- **特点**：ERP系统集成，主要用于订单和重量数据上传
- **方法映射**：
  - `RequestChuteAsync` - 对应参考代码的UploadData方法（上传称重数据）
  - `ScanParcelAsync` - 不支持（返回不支持消息）
  - `UploadImageAsync` - 留空实现

#### 4. 旺店通WMS API客户端（WdtWmsApiClient）
- **实现接口**：IWcsApiAdapter
- **参考实现**：[参考代码](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)
- **特点**：WMS系统集成，用于仓储管理系统对接
- **方法映射**：
  - `RequestChuteAsync` - 对应参考代码的UploadData方法（上传称重数据）
  - `ScanParcelAsync` - 不支持（返回不支持消息）
  - `UploadImageAsync` - 实现图片上传功能

#### 标准接口方法
所有WCS API客户端都实现相同的IWcsApiAdapter接口方法：
- `ScanParcelAsync(string barcode)` - 扫描包裹
- `RequestChuteAsync(string barcode)` - 请求格口号
- `UploadImageAsync(string barcode, byte[] imageData, string contentType)` - 上传图片

**注**：虽然接口相同，但每个客户端的具体实现都是独立的，根据各自的参考代码实现，可能在请求格式、数据结构、处理流程等方面有所不同。

#### 使用示例
```csharp
// 邮政处理中心客户端注册
services.AddHttpClient<IWcsApiAdapter, PostProcessingCenterApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.post-processing.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 邮政分揽投机构客户端注册
services.AddHttpClient<IWcsApiAdapter, PostCollectionApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.post-collection.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 聚水潭ERP客户端注册
services.AddHttpClient<IWcsApiAdapter, JushuitanErpApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.jushuitan.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 旺店通WMS客户端注册
services.AddHttpClient<IWcsApiAdapter, WdtWmsApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.wdt.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 使用客户端
var adapter = serviceProvider.GetService<IWcsApiAdapter>();
var scanResult = await adapter.ScanParcelAsync("POST123456");
var chuteResult = await adapter.RequestChuteAsync("POST123456");
var imageResult = await adapter.UploadImageAsync("POST123456", imageBytes, "image/jpeg");
```

### SignalR Hub（推荐）
- **SortingHub** - `/hubs/sorting` 分拣机实时通信
- **DwsHub** - `/hubs/dws` DWS实时通信
- **自动重连** - 支持自动重连机制
- **双向通信** - 支持服务端主动推送消息
- **完整日志** - 记录所有SignalR通信到数据库

### DWS适配器（IDwsAdapter）
- **TouchSocket TCP** - `TouchSocketDwsAdapter` 高性能TCP通信
  - 连接池大小：默认1000，可配置
  - 缓冲区大小：默认8192字节，可配置
  - 支持并发连接管理
- **支持热切换** - 可在运行时切换不同厂商的DWS设备
- **完整日志** - 记录所有接收的DWS数据到数据库

使用示例：
```csharp
// 注册DWS适配器（带优化配置）
services.AddSingleton<IDwsAdapter>(sp => 
    new TouchSocketDwsAdapter(
        host: "192.168.1.100", 
        port: 8001, 
        logger: logger, 
        commLogRepo: commLogRepo,
        maxConnections: 1000,      // 可选
        receiveBufferSize: 8192,   // 可选
        sendBufferSize: 8192       // 可选
    ));

// 使用适配器管理器支持热切换
services.AddSingleton<IAdapterManager<IDwsAdapter>>(sp => 
    new AdapterManager<IDwsAdapter>(
        sp.GetServices<IDwsAdapter>(), 
        "TouchSocket-DWS", 
        logger));
```

### 分拣机适配器（ISorterAdapter）
- **TouchSocket TCP** - `TouchSocketSorterAdapter` 高性能TCP通信
  - 自动重连：默认5秒间隔，可配置
  - 缓冲区大小：默认8192字节，可配置
  - 断线自动恢复
- **标准TCP** - `TcpSorterAdapter` 支持标准TCP通信
- **支持热切换** - 可在运行时切换不同厂商的分拣机
- **完整日志** - 记录所有发送的分拣指令到数据库

使用示例：
```csharp
// 注册分拣机适配器（带优化配置）
services.AddSingleton<ISorterAdapter>(sp => 
    new TouchSocketSorterAdapter(
        host: "192.168.1.200", 
        port: 9000, 
        logger: logger, 
        commLogRepo: commLogRepo,
        reconnectIntervalMs: 5000,  // 可选
        receiveBufferSize: 8192,    // 可选
        sendBufferSize: 8192        // 可选
    ));

// 使用适配器管理器支持热切换
services.AddSingleton<IAdapterManager<ISorterAdapter>>(sp => 
    new AdapterManager<ISorterAdapter>(
        sp.GetServices<ISorterAdapter>(), 
        "TouchSocket-Sorter", 
        logger));
```

### 第三方API适配器（IThirdPartyAdapter）
- **HTTP协议** - `HttpThirdPartyAdapter` 带熔断器的HTTP通信
- **支持热切换** - 可在运行时切换不同的API提供商
- **可扩展** - 通过实现`IThirdPartyAdapter`接口支持其他协议

### 热切换使用

```csharp
// 获取适配器管理器
var adapterManager = serviceProvider.GetService<IAdapterManager<IDwsAdapter>>();

// 运行时切换到其他适配器（无需重启服务）
await adapterManager.SwitchAdapterAsync("OtherVendor-DWS");

// 获取当前活动的适配器
var currentAdapter = adapterManager.GetActiveAdapter();

// 获取所有可用适配器
var allAdapters = adapterManager.GetAllAdapters();
```

## 测试

系统包含完整的单元测试：

```bash
# 运行所有测试
dotnet test

# 运行特定测试
dotnet test --filter "FullyQualifiedName~RuleEngineServiceTests"
```

**测试覆盖**：
- ✅ 规则引擎服务测试（重量条件、条码匹配、缓存、优先级）
- ✅ Mock对象支持（使用Moq框架）
- 🔜 更多服务和适配器测试正在开发中

## 开发指南

### 添加新规则类型

1. 在`RuleEngineService.cs`中添加新的评估方法
2. 更新文档说明新的表达式语法

### 扩展持久化层

1. 实现相应的Repository接口
2. 在`Program.cs`中注册新的实现
3. 使用EF Core创建新迁移：
   ```bash
   dotnet ef migrations add YourMigrationName --project ZakYip.Sorting.RuleEngine.Infrastructure --context MySqlLogDbContext
   ```

### 添加新的分拣机适配器

1. 实现`ISorterAdapter`接口：
   ```csharp
   public class CustomSorterAdapter : ISorterAdapter
   {
       public string AdapterName => "CustomVendor";
       public string ProtocolType => "CustomProtocol";
       
       public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken)
       {
           // 实现自定义协议
       }
   }
   ```

2. 在`Program.cs`中注册适配器

### 添加新的DWS适配器

1. 实现`IDwsAdapter`接口：
   ```csharp
   public class CustomDwsAdapter : IDwsAdapter
   {
       public string AdapterName => "CustomVendor-DWS";
       public string ProtocolType => "TCP";
       
       public event Func<DwsData, Task>? OnDwsDataReceived;
       
       public async Task StartAsync(CancellationToken cancellationToken)
       {
           // 实现启动逻辑
       }
       
       public async Task StopAsync(CancellationToken cancellationToken)
       {
           // 实现停止逻辑
       }
   }
   ```

2. 在Program.cs中注册适配器到AdapterManager

### 添加新的第三方API适配器

1. 实现`IThirdPartyAdapter`接口：
   ```csharp
   public class CustomApiAdapter : IThirdPartyAdapter
   {
       public string AdapterName => "CustomAPI";
       public string ProtocolType => "HTTP";
       
       public async Task<ThirdPartyResponse> CallApiAsync(ParcelInfo parcelInfo, DwsData dwsData, CancellationToken cancellationToken)
       {
           // 实现自定义API调用
       }
   }
   ```

2. 在依赖注入中注册适配器

### 查询通信日志

通信日志存储在`communication_logs`表中，可以通过ICommunicationLogRepository查询：

```csharp
// 获取最近的通信日志
var logs = await communicationLogRepository.GetLogsAsync(
    startTime: DateTime.UtcNow.AddHours(-1),
    type: CommunicationType.Tcp,
    parcelId: "PKG001",
    maxRecords: 100);

// 遍历日志
foreach (var log in logs)
{
    Console.WriteLine($"{log.CreatedAt}: {log.Direction} {log.Message}");
    if (!log.IsSuccess)
    {
        Console.WriteLine($"  错误: {log.ErrorMessage}");
    }
}

### 自定义第三方API

1. 实现`IThirdPartyApiClient`接口
2. 在依赖注入中替换默认实现

### 配置缓存清除

当规则配置更新时，手动清除缓存：

```csharp
// 在RuleController中
public async Task<IActionResult> UpdateRule([FromBody] SortingRule rule)
{
    await _ruleRepository.UpdateAsync(rule);
    
    // 清除缓存以重新加载规则
    if (_ruleEngineService is RuleEngineService service)
    {
        service.ClearCache();
    }
    
    return Ok();
}
```

## 最新实现功能 (v1.9.0)

### 新增功能

#### 1. 甘特图数据查询API（新增）
- ✅ **GanttChartController** - 甘特图数据查询接口
  - `GET /api/ganttchart/{target}` - 查询指定包裹前后N条数据
  - `POST /api/ganttchart/query` - POST方式查询（支持复杂参数）
  - 支持查询前后各100条数据范围
  - 自动关联匹配日志、DWS通信、API通信、格口信息
  - 支持MySQL和SQLite双数据库查询
- ✅ **GanttChartDataItem** - 甘特图数据项DTO
  - 包含包裹信息、匹配信息、通信时间、格口信息等
  - 支持时间线可视化展示
  - 标记目标包裹和序列号
- ✅ **IGanttChartService** - 甘特图服务接口和实现
  - 智能查询目标包裹前后数据
  - 自动降级处理（MySQL → SQLite）

#### 2. 规则安全验证（新增）
- ✅ **RuleValidationService** - 规则验证服务
  - 防止代码注入攻击
  - 检查危险关键字（eval、exec、system、script等）
  - 验证表达式格式和长度限制
  - 支持按匹配方法类型验证表达式
  - 优先级范围验证（0-9999）
- ✅ **规则控制器集成** - RuleController自动验证
  - 新增规则时自动验证安全性
  - 更新规则时自动验证安全性
  - 验证失败返回详细错误信息

#### 3. API请求日志记录（新增）
- ✅ **ApiRequestLog实体** - API请求日志表
  - 记录请求时间、IP、方法、路径、查询字符串
  - 记录请求头、请求体（最大10KB）
  - 记录响应时间、状态码、响应头、响应体
  - 记录耗时、用户标识、成功状态
- ✅ **ApiRequestLoggingMiddleware** - 请求日志中间件
  - 自动记录所有API请求
  - 跳过健康检查、Swagger、SignalR端点
  - 支持MySQL和SQLite双数据库
  - 捕获请求和响应完整信息
  - 计算精确耗时
- ✅ **数据库集成** - MySQL和SQLite都支持
  - 自动创建api_request_logs表
  - 优化的索引设计（时间、路径、IP、方法）

#### 4. 全局模型验证（新增）
- ✅ **ModelValidationFilter** - 全局验证过滤器
  - 自动验证所有API请求参数
  - ModelState自动检查
  - 统一的错误响应格式
- ✅ **SortingRule验证特性** - 实体级验证
  - Required、StringLength、Range等特性
  - 规则ID、名称、表达式必填验证
  - 长度限制验证
  - 优先级范围验证

#### 5. 架构优化（改进）
- ✅ **移除ThirdPartyResponseReceivedEvent**
  - 删除不必要的事件定义和处理器
  - 简化第三方API调用流程
  - 直接在DwsDataReceivedEventHandler中记录响应
  - 减少事件传播开销
- ✅ **中央包管理更新**
  - 新增Microsoft.AspNetCore.Http.Abstractions
  - 新增Microsoft.AspNetCore.Http
  - 新增Microsoft.AspNetCore.Mvc.Core
  - 统一管理ASP.NET Core相关包

## 最新实现功能 (v1.8.0)

### 新增功能

#### 1. NLog日志框架集成（新增）
- ✅ **NLog 5.3.4** - 高性能日志框架
  - 完整的nlog.config配置文件
  - 支持多种日志目标：控制台、文件、错误日志、性能日志
  - 自动日志轮转和归档（7-30天保留期）
  - 日志级别过滤和分类
  - UTF-8编码支持

#### 2. Newtonsoft.Json集成（新增）
- ✅ **Newtonsoft.Json 13.0.3** - JSON序列化库
  - Microsoft.AspNetCore.Mvc.NewtonsoftJson集成
  - 配置循环引用处理
  - 空值忽略选项
  - 标准化日期格式（yyyy-MM-dd HH:mm:ss）

#### 3. 条件Windows服务支持（改进）
- ✅ **条件编译** - 仅Release模式使用Windows服务
  - DEBUG模式下作为控制台应用运行
  - 使用#if !DEBUG条件编译指令
  - 便于开发调试

#### 4. Kestrel服务器配置（新增）
- ✅ **Kestrel优化配置** - ASP.NET Core Web服务器
  - 最大并发连接数：1000
  - 请求体大小限制：10MB
  - Keep-Alive超时：2分钟
  - 请求头超时：30秒
  - 关闭Server头以提高安全性

#### 5. 第三方API数据库配置（新增）
- ✅ **ThirdPartyApiConfig实体** - 使用record class
  - 支持多个第三方API配置
  - 存储在LiteDB中
  - 可配置优先级、超时、自定义头等
  - 支持HTTP方法和请求体模板
- ✅ **ThirdPartyApiConfigController** - 完整CRUD API
  - `GET /api/thirdpartyapiconfig` - 获取所有配置
  - `GET /api/thirdpartyapiconfig/enabled` - 获取启用的配置
  - `POST /api/thirdpartyapiconfig` - 创建配置
  - `PUT /api/thirdpartyapiconfig/{id}` - 更新配置
  - `DELETE /api/thirdpartyapiconfig/{id}` - 删除配置

#### 6. SignalR客户端服务（新增）
- ✅ **SignalRClientService** - 支持自动重连
  - 快速重连策略（0ms, 100ms, 500ms, 1s, 2s）
  - 连接状态变化事件
  - 线程安全的连接管理
  - 方法调用和事件订阅
  - 实现IAsyncDisposable

#### 7. TCP客户端服务（新增）
- ✅ **TcpClientService** - 支持自动重连
  - 自动重连循环（最长2秒间隔）
  - 数据接收事件
  - 连接状态变化事件
  - 发送文本和字节数据
  - 实现IAsyncDisposable

#### 8. 事件载荷优化（改进）
- ✅ **所有事件改为record class** - 值语义
  - ParcelCreatedEvent
  - DwsDataReceivedEvent
  - ThirdPartyResponseReceivedEvent
  - RuleMatchCompletedEvent
  - 使用required关键字而非构造函数

#### 9. 性能基准测试（新增）
- ✅ **ZakYip.Sorting.RuleEngine.Benchmarks项目** - BenchmarkDotNet框架
  - RuleMatchingBenchmarks - 7个基准测试
    - 条码匹配（StartsWith, Contains, Regex）
    - 重量匹配（简单、复杂表达式）
    - 体积匹配（简单、复杂表达式）
  - 内存诊断和性能排名
  - 可扩展的基准测试架构

#### 10. 性能优化特性（改进）
- ✅ **MethodImpl特性** - 极致性能优化
  - SignalRClientService使用AggressiveInlining
  - TcpClientService使用AggressiveInlining
  - 关键路径方法内联优化

## 历史版本功能 (v1.7.0)

### 新增功能

#### 1. 版本信息查询（新增）
- ✅ **版本信息API** - 通过HTTP查询系统版本
  - `/api/version` - 返回系统版本、构建日期等信息
  - 支持SignalR Hub查询版本（SortingHub、DwsHub的GetVersion方法）
  - 版本号：1.7.0

#### 2. 日志文件自动清理（新增）
- ✅ **LogFileCleanupService** - 后台服务定期清理.log文件
  - 可配置保留天数（默认7天）
  - 可配置日志目录（默认./logs）
  - 每小时检查一次，自动删除过期文件
  - 在appsettings.json中配置LogFileCleanup节

#### 3. 配置文件中文注释（新增）
- ✅ **appsettings.json完整中文注释** - 所有配置字段都有详细中文注释
  - LiteDB配置注释
  - MySQL配置和熔断器参数注释
  - 分片策略配置注释
  - API和缓存配置注释
  - 日志清理配置注释

#### 4. 枚举使用重构（新增）
- ✅ **BarcodeRegexMatcher重构** - 支持枚举类型匹配
  - 新增枚举参数方法：`Evaluate(BarcodeMatchPreset preset, string parameter, string barcode)`
  - 保持向后兼容的字符串表达式方法
  - 使用BarcodeMatchPreset枚举
- ✅ **ApiResponseMatcher重构** - 支持枚举类型匹配
  - 新增枚举参数方法：`Evaluate(ApiResponseMatchType matchType, string parameter, string responseData)`
  - 保持向后兼容的字符串表达式方法
  - 使用ApiResponseMatchType枚举

#### 5. 格口管理API（新增）
- ✅ **ChuteController** - 完整的格口CRUD API
  - `GET /api/chute` - 获取所有格口
  - `GET /api/chute/{id}` - 根据ID获取格口
  - `GET /api/chute/code/{code}` - 根据编号获取格口
  - `GET /api/chute/enabled` - 获取所有启用的格口
  - `POST /api/chute` - 创建格口
  - `PUT /api/chute/{id}` - 更新格口
  - `DELETE /api/chute/{id}` - 删除格口
- ✅ **IChuteRepository接口和实现** - LiteDB格口仓储

#### 6. 日志查询和导出API（新增）
- ✅ **LogController** - 专用日志表查询API
  - `GET /api/log/matching` - 查询匹配日志，支持分页和时间过滤
  - `GET /api/log/dws-communication` - 查询DWS通信日志
  - `GET /api/log/api-communication` - 查询API通信日志
  - `GET /api/log/sorter-communication` - 查询分拣机通信日志
  - `GET /api/log/matching/export` - 导出匹配日志为CSV（最多10000条）
  - 支持按时间范围、包裹ID、条码等条件查询

#### 7. 日志归档策略（已有）
- ✅ **DataArchiveService** - 已实现数据归档服务
  - 在appsettings.json中配置ArchiveSchedule（默认每天凌晨3点）
  - 冷数据阈值配置（默认30天）
  - 自动统计和归档冷数据

## 历史版本功能 (v1.6.0)

### 已实现功能

#### 1. 枚举类型定义
- ✅ **BarcodeMatchPreset枚举** - 定义条码匹配预设类型
  - StartsWith - 以指定字符串开头
  - Contains - 包含指定字符串
  - NotContains - 不包含指定字符串
  - AllDigits - 全数字
  - Alphanumeric - 字母和数字组合
  - Length - 指定长度范围
  - Regex - 自定义正则表达式
- ✅ **ApiResponseMatchType枚举** - 定义API响应匹配类型
  - String - 字符串查找（正向）
  - StringReverse - 字符串查找（反向）
  - Regex - 正则表达式匹配
  - Json - JSON字段匹配

#### 2. 格口管理
- ✅ **Chute实体** - 格口信息管理
  - ChuteId (long) - 格口ID，自增主键
  - ChuteName - 格口名称
  - ChuteCode - 格口编号（可选）
  - Description - 格口描述
  - IsEnabled - 是否启用
  - CreatedAt/UpdatedAt - 创建/更新时间
- ✅ 数据库表和索引配置
- ✅ 支持MySQL和SQLite双数据库

#### 3. 专用通信日志表
- ✅ **SorterCommunicationLog** - 分拣机通信日志
  - 分拣机地址、通信类型（接收/发送）
  - 原始内容、格式化内容
  - 提取的包裹ID、小车号（接收时）
  - 通信时间、成功状态、错误信息
- ✅ **DwsCommunicationLog** - DWS通信日志
  - DWS地址、原始内容、格式化内容
  - 条码、重量、体积
  - 通信时间、成功状态、错误信息
- ✅ **ApiCommunicationLog** - API通信日志
  - 包裹ID、请求地址、请求/响应内容
  - 请求/响应头、状态码
  - 请求时间、耗时、响应时间
  - 格式化的Curl内容
- ✅ **MatchingLog** - 匹配日志
  - 包裹ID、关联的DWS/API内容
  - 匹配的规则ID、匹配依据
  - 格口ID、小车占位数量
  - 匹配时间、成功状态、错误信息

#### 4. 数据库优化
- ✅ 所有表的ID字段统一使用long类型（自增主键）
- ✅ 为所有日志表创建了合适的索引
  - 按时间降序索引，优化查询性能
  - 按关键字段索引（包裹ID、条码、格口ID等）
- ✅ 支持MySQL和SQLite双数据库
- ✅ 自动创建EF Core数据库迁移

## 历史版本功能 (v1.5.0)

### 已实现功能

#### 1. 性能指标收集和监控
- ✅ 创建性能指标实体（PerformanceMetric）
- ✅ 创建性能指标仓储接口（IPerformanceMetricRepository）
- ✅ 创建性能指标收集服务（PerformanceMetricService）
- ✅ 集成到规则引擎服务，自动收集规则评估性能
- ✅ 支持P50/P95/P99延迟统计
- ✅ 详细文档：[PERFORMANCE_METRICS.md](./PERFORMANCE_METRICS.md)

#### 2. 多种匹配方法
- ✅ **条码正则匹配** (BarcodeRegexMatcher)
  - 支持预设选项：STARTSWITH、CONTAINS、NOTCONTAINS、ALLDIGITS、ALPHANUMERIC、LENGTH
  - 支持自定义正则表达式
- ✅ **重量匹配** (WeightMatcher)
  - 支持表达式：>、=、<、&/and、|/or
- ✅ **体积匹配** (VolumeMatcher)
  - 支持Length、Width、Height、Volume的复杂表达式
- ✅ **OCR匹配** (OcrMatcher)
  - 支持地址段码匹配（三段码、第一/二/三段码）
  - 支持收件人/寄件人地址和电话后缀匹配
- ✅ **API响应内容匹配** (ApiResponseMatcher)
  - 字符串查找（正向/反向）
  - 正则查找
  - JSON匹配（支持嵌套字段）
- ✅ **低代码表达式匹配** (LowCodeExpressionMatcher)
  - 支持混合使用多种条件
  - 灵活的自定义表达式
- ✅ 详细文档：[MATCHING_METHODS.md](./MATCHING_METHODS.md)

#### 3. 多规则匹配支持
- ✅ 一个格口ID可以匹配多条规则
- ✅ 系统收集所有匹配的规则
- ✅ 按优先级返回最高优先级规则的格口号
- ✅ 记录匹配到的规则数量

#### 4. 完善的单元测试
- ✅ BarcodeRegexMatcherTests - 条码正则匹配测试
- ✅ WeightMatcherTests - 重量匹配测试
- ✅ VolumeMatcherTests - 体积匹配测试
- ✅ OcrMatcherTests - OCR匹配测试
- ✅ ApiResponseMatcherTests - API响应匹配测试
- ✅ 总计70个单元测试，全部通过

## 历史版本功能 (v1.4.0)

### 已实现的功能

#### 1. SignalR Hub实现（新增）
- ✅ 创建SortingHub用于分拣机实时通信（/hubs/sorting）
- ✅ 创建DwsHub用于DWS实时通信（/hubs/dws）
- ✅ 在Program.cs中配置SignalR服务
- ✅ 支持自动重连和双向通信
- ✅ 生产环境推荐使用SignalR或TCP，HTTP仅用于测试

#### 2. 通信方式优化（新增）
- ✅ 明确分拣程序和DWS只使用TCP/SignalR通信
- ✅ HTTP API标记为仅用于测试和调试
- ✅ 添加通信方式选择指南

#### 3. TouchSocket连接池优化（新增）
- ✅ TouchSocketDwsAdapter添加连接池配置（最大连接数：默认1000）
- ✅ TouchSocketSorterAdapter添加自动重连配置（默认5秒间隔）
- ✅ 可配置缓冲区大小（默认8192字节）
- ✅ 优化消息处理性能

#### 4. 代码质量改进（新增）
- ✅ 移除所有英文注释，统一使用中文注释
- ✅ 优化代码可读性和维护性

#### 5. 自动数据库迁移
- ✅ EF Core自动迁移，部署时自动创建数据库表
- ✅ 自动应用迁移更新，支持MySQL和SQLite
- ✅ 在Program.cs中实现，启动时自动执行

#### 6. 适配器热切换
- ✅ 实现IAdapterManager<T>接口，支持运行时切换适配器
- ✅ 支持切换DWS适配器（不同厂商、不同协议）
- ✅ 支持切换分拣机适配器（不同厂商、不同协议）
- ✅ 支持切换第三方API适配器
- ✅ 无需重启服务即可热切换

#### 7. TouchSocket TCP通信
- ✅ 集成TouchSocket高性能TCP库
- ✅ 实现TouchSocketDwsAdapter用于DWS数据接收
- ✅ 实现TouchSocketSorterAdapter用于分拣机通信
- ✅ 支持自动重连和异常处理

#### 8. 通信日志记录
- ✅ 新增CommunicationLog实体存储所有通信日志
- ✅ 记录TCP、SignalR、HTTP通信的全量日志
- ✅ 支持按类型、时间、包裹ID查询日志
- ✅ 自动记录发送/接收方向、成功/失败状态

#### 9. 代码结构优化
- ✅ 所有枚举独立文件存放（ParcelStatus, WorkItemType, CommunicationType等）
- ✅ 所有枚举添加Description特性，使用中文描述
- ✅ 将嵌套类移到独立文件（ParcelProcessingContext, ParcelWorkItem）
- ✅ 代码注释统一使用中文

#### 10. 测试工具
- ✅ 创建ZakYip.Sorting.RuleEngine.TestConsole测试控制台
- ✅ 支持模拟分拣机信号发送（HTTP API）
- ✅ 支持模拟DWS数据发送（TCP）
- ✅ 提供交互式命令行界面

### 优化方向

#### 短期优化（1-2周）
1. ~~**格口管理API**~~ - ✅ 已完成（v1.7.0）
2. ~~**日志查询API**~~ - ✅ 已完成（v1.7.0）
3. ~~**枚举使用重构**~~ - ✅ 已完成（v1.7.0）
4. ~~**NLog日志框架**~~ - ✅ 已完成（v1.8.0）
5. ~~**第三方API数据库配置**~~ - ✅ 已完成（v1.8.0）
6. ~~**SignalR/TCP客户端服务**~~ - ✅ 已完成（v1.8.0）
7. ~~**性能基准测试**~~ - ✅ 已完成（v1.8.0）
8. ~~**甘特图数据查询API**~~ - ✅ 已完成（v1.9.0）
9. ~~**规则安全验证**~~ - ✅ 已完成（v1.9.0）
10. ~~**API请求日志记录**~~ - ✅ 已完成（v1.9.0）
11. ~~**全局模型验证**~~ - ✅ 已完成（v1.9.0）
12. ~~**完整数据库降级还原**~~ - ✅ 已完成（v1.11.0）
13. ~~**查询API响应DTO规范化**~~ - ✅ 已完成（v1.13.0）- 所有查询API使用ApiResponse<T>和PagedResponse<T>标准格式
14. ~~**异常隔离器**~~ - ✅ 已完成（v1.12.0）- Polly重试、熔断、超时策略
15. ~~**健康检查增强**~~ - ✅ 已完成（v1.12.0）- 详细组件健康检查（/health/detail）
16. ~~**日志归档优化**~~ - ✅ 已完成（v1.12.0）- 批量处理、并行查询、进度报告
17. ~~**数据库事务安全**~~ - ✅ 已完成 - MySQL/SQLite同步使用事务保护，防止断电数据丢失
18. ~~**查询性能优化工具**~~ - ✅ 已完成 - QueryOptimizationExtensions提供分页、时间范围、批量操作优化
19. ~~**高并发压力测试**~~ - ✅ 已完成 - 支持100-1000包裹/秒压力测试，自动瓶颈识别
20. ~~**日志安全性增强**~~ - ✅ 已完成（v1.14.0）- 生产环境禁止SQL日志，防止敏感信息泄露
21. ~~**数据精度提升**~~ - ✅ 已完成（v1.14.0）- double替换为decimal，提高物理测量精度
22. ~~**布尔字段命名规范**~~ - ✅ 已验证（v1.14.0）- 所有字段符合Is/Has前缀规范
23. ~~**API客户端统一命名**~~ - ✅ 已完成（v1.14.3）- 所有API适配器重命名为ApiClient后缀，统一方法映射
24. **API客户端增强** - API客户端功能完善（v1.14.3新增优化方向）
   - 添加Polly弹性策略（重试、熔断、超时）
   - 实现强类型响应模型解析
   - 支持批量操作（批量扫描、批量请求格口）
   - 添加请求/响应日志记录
   - 支持配置文件管理（端点、超时、认证参数）
   - 实现响应缓存机制
25. **格口分配优化** - 基于格口使用情况的智能分配算法
26. **版本升级通知** - 当有新版本时自动通知管理员
27. **规则测试工具** - 提供规则表达式在线测试和验证工具（Web界面）
28. **性能监控仪表板** - 实时显示系统性能指标和瓶颈分析
29. **代码覆盖率提升** - 从当前约70%提升至85%以上（v1.14.0部分完成）
30. **静态代码分析** - 集成SonarQube进行代码质量分析（待实现）

#### 中期优化（1-3个月）
1. **Web管理界面** - 开发完整的Web管理控制台（高优先级）
   - 规则管理界面（创建、编辑、测试规则）
   - 格口管理界面（格口配置和使用统计）
   - 日志查询和分析界面（支持多维度过滤和导出）
   - 系统配置界面（实时配置更新）
   - 性能监控仪表板（P50/P95/P99延迟图表）
   - 甘特图可视化（包裹处理时间线）
2. **监控告警系统** - 生产环境监控和告警
   - 实时包裹处理量监控
   - 格口使用率监控和告警
   - 性能指标监控（基于现有PerformanceMetric）
   - 错误率和异常监控告警
   - 数据库熔断状态监控
   - 邮件/短信/企业微信通知
3. **格口利用率分析** - 深度统计分析格口使用情况
   - 格口使用热力图
   - 分拣效率分析报表
   - 空闲格口识别
   - 优化建议生成
4. **负载均衡支持** - 支持多实例部署（高并发场景）
   - Redis分布式缓存替代IMemoryCache
   - 分布式锁实现（防止重复处理）
   - 会话共享和粘性会话
   - 健康检查集成负载均衡器
5. **更多设备适配器** - 扩展设备兼容性
   - 更多品牌的DWS设备适配
   - 更多品牌的分拣机适配
   - 通用协议适配器（Modbus、OPC UA等）
   - 适配器配置热加载
6. **代码质量提升（v1.14.0部分完成）** 
   - ✅ double替换为decimal提高精度（已完成）
   - ✅ 布尔字段前缀规范化（Is/Has/Can等）（已验证符合规范）
   - ⏳ 代码覆盖率提升至85%以上（当前约70%，需继续提升）
   - ⏳ 静态代码分析集成（SonarQube）（待集成）
7. **数据库性能优化（v1.14.0部分完成）** 
   - ✅ QueryOptimizationExtensions工具类已实现（需在更多查询中应用）
   - ✅ 慢查询识别机制已存在（SlowQueryThresholdMs = 1000ms）
   - ✅ 索引优化已完成（所有日志表配置降序时间索引）
   - ✅ 连接池已调优（MinimumPoolSize=5, MaximumPoolSize=100）
   - ⏳ 索引使用情况监控（待实现）
   - 分表策略优化（基于负载测试结果）
   - 连接池调优

#### 长期优化（3-6个月）
1. **容器化部署** - Docker和Kubernetes支持（优先级提高）
   - Docker镜像构建和优化
   - Kubernetes部署配置（Deployment、Service、Ingress）
   - Helm Charts包管理
   - CI/CD流水线（GitHub Actions/Azure DevOps）
   - 环境隔离和配置管理
2. **微服务架构演进** - 模块化拆分提高可扩展性
   - 规则引擎服务（独立规则计算）
   - 包裹处理服务（FIFO队列处理）
   - 通信网关服务（设备协议转换）
   - 日志服务（集中式日志管理）
   - 配置中心（动态配置管理）
   - API网关（统一入口和认证）
3. **大数据分析平台** - 基于历史数据的深度分析
   - 分拣效率趋势分析
   - 包裹流向和路径优化
   - 异常模式识别和预警
   - 格口分配效率分析
   - 自定义报表生成器
   - BI仪表板集成（Power BI/Tableau）
4. **AI智能优化** - 机器学习增强决策能力
   - 基于历史数据的智能规则推荐
   - 异常包裹自动识别和处理建议
   - 格口分配智能优化算法
   - 负载预测和资源调度
   - 设备故障预测性维护
5. **实时数据流处理** - 事件流和消息队列
   - Kafka消息队列集成
   - 事件溯源（Event Sourcing）
   - CQRS模式实现
   - 实时数据管道
   - 流式数据处理和分析
6. **云原生部署** - 多云平台支持
   - Azure云服务集成
   - AWS云服务支持
   - 阿里云适配
   - 云数据库支持（Azure SQL、RDS）
   - 云存储集成（Blob Storage、S3）
   - 弹性伸缩和自动扩容
7. **国际化和多租户** - 全球化部署支持
   - 多语言界面（中文/英文/其他）
   - 本地化资源管理
   - 多租户架构（SaaS模式）
   - 租户隔离和数据安全
   - 全球化部署和CDN加速
8. **高可用架构** - 企业级可靠性保障
   - 主从/主主数据库架构
   - 多区域容灾部署
   - 自动故障转移
   - 零停机升级方案
   - 99.99%可用性保障

### 优化实施进展

#### 已完成的性能和安全优化

基于生产环境需求和压力测试结果，系统已实施以下关键优化：

##### 1. 数据库事务安全机制 ✅
- **事务保护**: 所有7个数据同步方法（LogEntry、CommunicationLog、SorterCommunicationLog、DwsCommunicationLog、ApiCommunicationLog、MatchingLog、ApiRequestLog）均采用协调事务
- **断电保护**: MySQL插入和SQLite删除采用两阶段提交，确保原子性
- **防重复**: 仅在MySQL成功后删除SQLite数据，避免重复同步
- **批量安全**: 每批1000条记录独立事务，失败批次不影响已成功批次
- **自动VACUUM**: 同步完成后自动压缩SQLite数据库文件，释放磁盘空间

##### 2. 查询性能优化工具 ✅
- **QueryOptimizationExtensions**: 提供完整的查询优化工具类
  - `OptimizedPaging()` - 优化分页查询，使用AsNoTracking提高只读性能
  - `OptimizedTimeRange()` - 优化时间范围查询，确保索引正确使用
  - `BulkInsertAsync()` - 批量插入优化，禁用自动变更检测
  - `BulkDeleteAsync()` - 批量删除优化，使用原始SQL提高效率
  - `CompileTimeRangeQuery()` - 编译频繁查询以重用，提高性能
- **索引优化**: 所有日志表配置降序时间索引，优化时间范围查询和排序

##### 3. 高并发压力测试框架 ✅
- **NBomber框架**: 专业的负载测试工具集成
- **测试场景**:
  - 100包裹/秒压力测试（3分钟持续）
  - 500包裹/秒高负载测试（2分钟逐步增加）
  - 1000包裹/秒极限测试（1分钟逐步增加）
  - 长时间稳定性测试（10分钟，50并发）
  - 数据库同步事务压力测试（100并发）
- **自动瓶颈识别**: 智能分析吞吐量、延迟、错误率瓶颈
- **详细报告**: 生成HTML、文本、CSV多格式测试报告
- **性能基准**: 建立生产环境性能基线

##### 4. 生产环境监控建议
定期监控以下关键指标以确保系统稳定运行：
- **同步延迟**: MySQL恢复后的同步时间应在合理范围内
- **事务成功率**: 应保持在95%以上
- **SQLite大小**: 定期检查并VACUUM压缩
- **系统响应时间**: P99延迟应保持在2000ms以下
- **熔断器状态**: 监控MySQL熔断器开启/关闭状态
- **包裹处理吞吐量**: 确保满足业务需求（50-1000包裹/秒）

详细实施文档请参考 [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) 和 [LOAD_TESTING_GUIDE.md](./ZakYip.Sorting.RuleEngine.LoadTests/LOAD_TESTING_GUIDE.md)

## 性能基准测试

系统包含完整的性能基准测试项目（BenchmarkDotNet框架）：

### 运行基准测试

```bash
cd ZakYip.Sorting.RuleEngine.Benchmarks
dotnet run -c Release
```

### 可用基准测试

1. **RuleMatchingBenchmarks** - 规则匹配性能测试
   - 条码正则匹配（StartsWith, Contains, Regex）
   - 重量匹配（简单、复杂表达式）
   - 体积匹配（简单、复杂表达式）

### 基准测试特性

- ✅ 内存诊断 - 跟踪内存分配
- ✅ 性能排名 - 自动排序最快到最慢
- ✅ 统计数据 - P50/P95/P99延迟
- ✅ 可扩展 - 轻松添加新的基准测试

## 测试工具使用

### 运行测试控制台

```bash
cd ZakYip.Sorting.RuleEngine.TestConsole
dotnet run
```

测试控制台提供两种模式：

**模式1：模拟分拣机信号**
- 通过HTTP API发送包裹创建信号到主系统
- 测试分拣机信号接收流程

**模式2：模拟DWS数据**
- 通过TCP发送DWS测量数据到主系统
- 测试DWS数据接收和处理流程

## 未来优化方向

基于当前项目状态和生产环境需求，以下为系统未来的优化方向和改进计划：

### 一、代码质量提升（高优先级）

#### 1. 注释覆盖率提升
- **目标**：将代码注释覆盖率从当前约70%提升至90%以上
- **重点区域**：
  - 所有公共API方法添加完整的XML文档注释
  - 复杂业务逻辑添加详细的内联注释
  - 所有领域实体和DTO添加属性说明
- **预期效果**：提高代码可维护性，降低新开发人员上手难度

#### 2. 静态代码分析集成
- **工具**：集成SonarQube进行代码质量分析
- **指标**：
  - 代码重复率控制在3%以内
  - 代码复杂度（圈复杂度）控制在合理范围
  - 消除所有代码异味（Code Smells）
- **持续改进**：建立代码质量门禁，不达标不允许合并

#### 3. 单元测试覆盖率提升
- **当前状态**：196个单元测试通过，覆盖率约70%
- **目标**：提升至85%以上
- **重点**：
  - 增加边界条件测试用例
  - 补充异常处理测试
  - 添加并发场景测试
  - 完善集成测试

### 二、性能优化（中优先级）

#### 1. 数据库查询优化
- **已完成**：
  - QueryOptimizationExtensions工具类已实现
  - 所有日志表配置降序时间索引
  - 连接池优化（MinimumPoolSize=5, MaximumPoolSize=100）
- **待实现**：
  - 在更多查询场景中应用QueryOptimizationExtensions
  - 索引使用情况监控和优化
  - 慢查询自动告警机制
  - 查询计划分析和优化

#### 2. 缓存策略优化
- **当前**：IMemoryCache本地缓存
- **计划**：
  - 引入Redis分布式缓存支持多实例部署
  - 实现缓存预热机制
  - 优化缓存失效策略
  - 添加缓存命中率监控

#### 3. 批量处理优化
- **目标**：优化大批量数据处理性能
- **方案**：
  - 增加批处理大小的动态调整
  - 实现并行批处理
  - 优化内存使用避免OOM

### 三、功能增强（中优先级）

#### 1. Web管理界面开发
- **规则管理**：
  - 可视化规则创建和编辑
  - 规则测试和验证工具
  - 规则导入导出功能
- **格口管理**：
  - 格口配置和状态监控
  - 格口使用统计和热力图
  - 格口分配策略配置
- **日志查询**：
  - 多维度日志查询和过滤
  - 日志导出和下载
  - 甘特图时间线可视化
- **系统监控**：
  - 实时性能指标仪表板
  - 系统健康状态监控
  - 告警配置和通知

#### 2. 智能规则推荐
- **基于历史数据的规则优化建议**
- **异常模式识别和自动规则生成**
- **规则冲突检测和解决建议**

#### 3. 格口智能分配
- **基于格口使用率的动态分配**
- **负载均衡算法优化**
- **格口优先级和权重配置**

### 四、监控与运维（高优先级）

#### 1. 监控告警系统
- **实时监控**：
  - 包裹处理量监控
  - 格口使用率监控
  - 系统性能指标监控（CPU、内存、数据库）
  - 第三方API调用监控
- **告警机制**：
  - 错误率超阈值告警
  - 性能下降告警
  - 数据库熔断状态告警
  - 通知方式：邮件、短信、企业微信

#### 2. 日志聚合和分析
- **集成ELK Stack**（Elasticsearch, Logstash, Kibana）
- **日志统一收集和存储**
- **日志分析和可视化**
- **异常日志自动检测**

#### 3. APM集成
- **Application Performance Monitoring**
- **端到端性能追踪**
- **分布式追踪（Distributed Tracing）**
- **性能瓶颈自动识别**

### 五、架构演进（长期规划）

#### 1. 容器化和云原生
- **Docker容器化**：
  - 构建优化的Docker镜像
  - 多阶段构建减小镜像大小
- **Kubernetes部署**：
  - 自动扩缩容（HPA）
  - 滚动更新和回滚
  - 健康检查和自愈
  - Helm Charts包管理
- **CI/CD流水线**：
  - GitHub Actions自动化构建
  - 自动化测试和部署
  - 环境隔离管理

#### 2. 微服务架构拆分
- **规则引擎服务**：独立的规则计算和匹配服务
- **包裹处理服务**：FIFO队列和包裹流转管理
- **通信网关服务**：设备协议转换和适配
- **日志服务**：集中式日志管理和查询
- **配置中心**：动态配置管理（如Consul、Nacos）
- **API网关**：统一入口、认证鉴权、限流熔断

#### 3. 服务网格（Service Mesh）
- **Istio/Linkerd集成**
- **服务间通信加密**
- **流量管理和灰度发布**
- **可观测性增强**

### 六、数据分析与AI（长期规划）

#### 1. 大数据分析平台
- **分拣效率趋势分析**
- **包裹流向和路径优化**
- **格口分配效率分析**
- **自定义报表生成器**
- **BI仪表板集成**（Power BI/Tableau）

#### 2. 机器学习应用
- **智能规则推荐系统**
- **异常包裹自动识别**
- **格口分配优化算法**
- **负载预测和资源调度**
- **设备故障预测性维护**

#### 3. 实时数据流处理
- **Kafka消息队列集成**
- **事件溯源（Event Sourcing）**
- **CQRS模式实现**
- **流式数据处理和分析**

### 七、安全性增强（持续优先级）

#### 1. 身份认证和授权
- **JWT Token认证**
- **OAuth 2.0集成**
- **RBAC角色权限管理**
- **API密钥管理**

#### 2. 数据安全
- **敏感数据加密存储**
- **传输层加密（TLS/SSL）**
- **审计日志记录**
- **数据脱敏和匿名化**

#### 3. 安全扫描
- **依赖漏洞扫描（已有gh-advisory-database）**
- **代码安全扫描（已有codeql_checker）**
- **容器镜像安全扫描**
- **定期安全审计**

### 八、国际化和多租户（长期规划）

#### 1. 国际化支持
- **多语言界面**（中文、英文、其他语言）
- **本地化资源管理**
- **时区和日期格式适配**
- **多币种支持**

#### 2. 多租户架构
- **SaaS模式支持**
- **租户隔离和数据安全**
- **租户级别配置和定制**
- **租户计量和计费**

### 九、高可用性（长期规划）

#### 1. 数据库高可用
- **主从/主主架构**
- **自动故障转移**
- **读写分离**
- **数据库备份和恢复策略**

#### 2. 应用高可用
- **多区域容灾部署**
- **零停机升级方案**
- **自动扩缩容**
- **99.99%可用性保障**

#### 3. 灾难恢复
- **数据备份策略**
- **异地容灾方案**
- **灾难恢复演练**
- **RTO/RPO目标定义**

### 十、生态系统集成（持续）

#### 1. 更多设备适配器
- **更多品牌DWS设备适配**
- **更多品牌分拣机适配**
- **通用协议支持**（Modbus、OPC UA）
- **适配器热加载和动态配置**

#### 2. 第三方系统集成
- **WMS系统集成** - WdtWmsApiClient（旺店通WMS）
- **ERP系统集成** - JushuitanErpApiClient（聚水潭ERP）
- **邮政系统集成** - PostProcessingCenterApiClient和PostCollectionApiClient
- **标准API接口提供** - 统一的IWcsApiAdapter接口
- **方法映射规范** - 参考代码方法一致性映射

#### 3. 邮政系统深度集成（v1.14.3更新）
- ✅ 邮政处理中心API客户端（已重命名为PostProcessingCenterApiClient）
  - 实现IWcsApiAdapter接口
  - ScanParcelAsync - 扫描包裹到系统（对应SubmitScanInfo）
  - RequestChuteAsync - 请求格口号（对应UploadData）
  - UploadImageAsync - 上传图片
- ✅ 邮政分揽投机构API客户端（已重命名为PostCollectionApiClient）
  - 实现IWcsApiAdapter接口
  - ScanParcelAsync - 扫描包裹到系统（对应SubmitScanInfo）
  - RequestChuteAsync - 请求格口号（对应UploadData）
  - UploadImageAsync - 上传图片
- ✅ 聚水潭ERP API客户端（已重命名为JushuitanErpApiClient）
  - 实现IWcsApiAdapter接口
  - RequestChuteAsync - 上传称重数据（对应UploadData）
  - ScanParcelAsync - 不支持（返回不支持消息）
  - UploadImageAsync - 留空实现
- ✅ 旺店通WMS API客户端（已重命名为WdtWmsApiClient）
  - 实现IWcsApiAdapter接口
  - RequestChuteAsync - 上传称重数据（对应UploadData）
  - ScanParcelAsync - 不支持（返回不支持消息）
  - UploadImageAsync - 实现图片上传
- ✅ 统一命名规范（所有API适配器统一使用ApiClient后缀）
- ⏳ 邮政API弹性策略（Polly重试和熔断）（待实现）
- ⏳ 邮政API配置化（配置文件管理端点和参数）（待实现）
- ⏳ 邮政API强类型响应模型（待实现）
- ⏳ 邮政API批量操作支持（待实现）

---

**实施建议**：
- **短期（1-3个月）**：专注代码质量提升、Web管理界面开发、监控告警系统
- **中期（3-6个月）**：性能优化、功能增强、容器化部署
- **长期（6-12个月）**：微服务架构拆分、AI应用、多租户支持

**资源需求评估**：
- **开发团队**：2-3名全职开发工程师
- **测试团队**：1名专职测试工程师
- **运维团队**：1名DevOps工程师
- **基础设施**：云服务器、数据库、缓存、消息队列等

## 仓库维护

### 分支管理

仓库提供了分支清理脚本，用于删除除master以外的所有分支。详细说明请参见 [BRANCH_CLEANUP.md](./BRANCH_CLEANUP.md)。

**Linux/macOS:**
```bash
# 预览要删除的分支
./cleanup-branches.sh --dry-run

# 执行删除
./cleanup-branches.sh
```

**Windows PowerShell:**
```powershell
# 预览要删除的分支
.\cleanup-branches.ps1 -DryRun

# 执行删除
.\cleanup-branches.ps1
```

## 代码质量文档

项目包含以下代码质量相关文档：

- **[CODE_QUALITY_GUIDE.md](./CODE_QUALITY_GUIDE.md)** - 代码质量和测试改进完整指南
  - 代码文档覆盖率详情
  - SonarQube配置和使用说明
  - 单元测试策略和工具
  - 持续改进流程
  - 质量门限建议

- **[QUALITY_IMPROVEMENT_SUMMARY.md](./QUALITY_IMPROVEMENT_SUMMARY.md)** - v1.14.1质量改进实施总结
  - 实施成果和指标对比
  - 新增测试详情
  - 技术债务和改进建议
  - 运行和验证指南

## 贡献

欢迎提交Issue和Pull Request！

## 许可证

MIT License

## 联系方式

- 项目地址: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core
- 问题反馈: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core/issues

---

**注意**: 
- 本系统设计用于高频率场景，确保硬件资源充足以获得最佳性能。
- 项目已重命名为 `ZakYip.Sorting.RuleEngine`（移除了 `.Core` 后缀）。
- 数据清理策略已从定时改为基于空闲检测（默认30分钟无包裹创建后触发清理）。
- 新增分片表自动创建和管理功能。