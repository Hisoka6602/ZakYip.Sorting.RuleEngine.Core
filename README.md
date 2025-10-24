# ZakYip.Sorting.RuleEngine

## 项目简介

ZakYip分拣规则引擎系统是一个高性能的包裹分拣规则引擎，用于处理分拣程序的包裹信息和DWS（尺寸重量扫描）数据，通过自定义规则分析计算格口号，实现自动化分拣。

## 核心特性

- ✅ **Windows服务** - 作为Windows服务运行，稳定可靠
- ✅ **MiniAPI集成** - 内置Web API用于前端配置和交互
- ✅ **事件驱动架构** - 使用MediatR实现事件驱动，支持分拣程序信号接收和FIFO队列处理
- ✅ **数据分片** - 使用EFCore.Sharding实现时间维度分表，支持热冷数据分离
- ✅ **高性能设计** - 使用HTTP客户端池化、可配置缓存（绝对/滑动过期）、异步处理等技术，适合高频率场景（50次/秒）
- ✅ **多数据库支持** - LiteDB存储配置，MySQL记录日志，SQLite作为降级方案，支持EF Core自动迁移，优化索引和降序排序
- ✅ **弹性架构** - 数据库熔断器（可配置失败率、熔断时长），自动降级和数据同步，防止系统雪崩
- ✅ **自动数据管理** - 可配置的数据清理（默认90天）和归档服务，自动维护数据生命周期
- ✅ **MySQL自动调谐** - 性能监控、索引分析、连接池优化和慢查询识别
- ✅ **多协议支持** - 支持TCP/HTTP/SignalR等多种协议，通过适配器模式扩展多厂商对接
- ✅ **热切换支持** - 适配器管理器支持运行时热切换，无需重启服务即可切换DWS/分拣机适配器
- ✅ **TouchSocket集成** - 使用TouchSocket库实现高性能TCP通信
- ✅ **通信日志** - 全量记录TCP/SignalR/HTTP通信日志，支持问题追踪和调试
- ✅ **测试工具** - 提供分拣机信号模拟测试控制台，便于开发和测试
- ✅ **自动迁移** - EF Core自动应用数据库迁移，部署时自动创建表和索引
- ✅ **清晰架构** - 采用DDD分层架构，所有类、枚举、事件独立文件存放
- ✅ **中央包管理** - 使用Directory.Packages.props统一管理NuGet包版本
- ✅ **完整测试** - 单元测试覆盖核心功能
- ✅ **中文注释** - 核心代码使用中文注释

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
- **Swagger/OpenAPI** - API文档
- **Object Pool** - 对象池优化性能
- **xUnit / Moq** - 单元测试框架
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

## API端点

### 分拣机信号API（推荐）

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

规则引擎支持以下条件表达式：

### 数值比较

```
Weight > 1000          # 重量大于1000克
Weight >= 500          # 重量大于等于500克
Weight < 2000          # 重量小于2000克
Weight == 1500         # 重量等于1500克
Volume > 50000         # 体积大于50000立方厘米
```

### 字符串匹配

```
Barcode CONTAINS 'SF'          # 条码包含SF
Barcode STARTSWITH '123'       # 条码以123开头
Barcode ENDSWITH '890'         # 条码以890结尾
CartNumber == 'CART001'        # 小车号等于CART001
```

### 默认规则

```
DEFAULT                # 默认规则（匹配所有）
```

## 性能优化

系统采用多种性能优化策略：

1. **对象池** - 使用`ObjectPool`避免频繁对象创建
2. **滑动过期缓存** - 使用`IMemoryCache`，滑动过期5分钟，绝对过期30分钟
3. **异步处理** - 全面使用async/await模式
4. **批量处理** - 支持并行批量处理包裹
5. **连接复用** - HttpClient复用，减少连接开销
6. **索引优化** - 数据库表建立适当索引
7. **自动迁移** - EF Core自动应用数据库迁移，简化部署

## 弹性和降级策略

系统具备完善的弹性和降级机制：

1. **数据库熔断器** - 为MySQL数据库配置熔断器，防止级联故障
   - 可配置的失败率阈值（默认50%）
   - 可配置的熔断持续时间（默认20分钟）
   - 可配置的采样周期和最小吞吐量
   - MySQL失败时自动切换到SQLite
   - MySQL恢复后自动同步SQLite数据并清理
2. **HTTP客户端池化** - 使用HttpClientFactory池化管理HTTP连接
   - 避免端口耗尽问题
   - **注意：HTTP请求不使用熔断器**
3. **API容错** - 第三方API失败时仍可使用规则引擎
4. **规则缓存** - 数据库不可用时使用缓存规则
5. **可配置缓存** - 支持配置绝对过期和滑动过期时间

📚 **详细文档**: 
- [CIRCUIT_BREAKER.md](./CIRCUIT_BREAKER.md) - 数据库熔断器的完整配置和使用说明
- [EVENT_DRIVEN_AND_SHARDING.md](./EVENT_DRIVEN_AND_SHARDING.md) - 事件驱动架构和数据分片实现指南

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

系统通过适配器模式支持多种通信协议，并支持运行时热切换：

### DWS适配器（IDwsAdapter）
- **TouchSocket TCP** - `TouchSocketDwsAdapter` 高性能TCP通信
- **支持热切换** - 可在运行时切换不同厂商的DWS设备
- **自动重连** - 连接断开时自动重连
- **完整日志** - 记录所有接收的DWS数据到数据库

使用示例：
```csharp
// 注册DWS适配器
services.AddSingleton<IDwsAdapter>(sp => 
    new TouchSocketDwsAdapter("192.168.1.100", 8001, logger, commLogRepo));

// 使用适配器管理器支持热切换
services.AddSingleton<IAdapterManager<IDwsAdapter>>(sp => 
    new AdapterManager<IDwsAdapter>(
        sp.GetServices<IDwsAdapter>(), 
        "TouchSocket-DWS", 
        logger));
```

### 分拣机适配器（ISorterAdapter）
- **TouchSocket TCP** - `TouchSocketSorterAdapter` 高性能TCP通信
- **标准TCP** - `TcpSorterAdapter` 支持标准TCP通信
- **支持热切换** - 可在运行时切换不同厂商的分拣机
- **完整日志** - 记录所有发送的分拣指令到数据库

使用示例：
```csharp
// 注册分拣机适配器
services.AddSingleton<ISorterAdapter>(sp => 
    new TouchSocketSorterAdapter("192.168.1.200", 9000, logger, commLogRepo));

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

## 最新实现功能 (v1.3.0)

### 已实现的新功能

#### 1. 自动数据库迁移
- ✅ EF Core自动迁移，部署时自动创建数据库表
- ✅ 自动应用迁移更新，支持MySQL和SQLite
- ✅ 在Program.cs中实现，启动时自动执行

#### 2. 适配器热切换
- ✅ 实现IAdapterManager<T>接口，支持运行时切换适配器
- ✅ 支持切换DWS适配器（不同厂商、不同协议）
- ✅ 支持切换分拣机适配器（不同厂商、不同协议）
- ✅ 支持切换第三方API适配器
- ✅ 无需重启服务即可热切换

#### 3. TouchSocket TCP通信
- ✅ 集成TouchSocket高性能TCP库
- ✅ 实现TouchSocketDwsAdapter用于DWS数据接收
- ✅ 实现TouchSocketSorterAdapter用于分拣机通信
- ✅ 支持自动重连和异常处理

#### 4. SignalR支持
- ✅ 添加Microsoft.AspNetCore.SignalR.Client包
- ✅ 支持实时双向通信
- ✅ 可用于分拣机信号和DWS数据传输

#### 5. 通信日志记录
- ✅ 新增CommunicationLog实体存储所有通信日志
- ✅ 记录TCP、SignalR、HTTP通信的全量日志
- ✅ 支持按类型、时间、包裹ID查询日志
- ✅ 自动记录发送/接收方向、成功/失败状态

#### 6. 代码结构优化
- ✅ 所有枚举独立文件存放（ParcelStatus, WorkItemType, CommunicationType等）
- ✅ 所有枚举添加Description特性，使用中文描述
- ✅ 将嵌套类移到独立文件（ParcelProcessingContext, ParcelWorkItem）
- ✅ 代码注释改为中文

#### 7. 测试工具
- ✅ 创建ZakYip.Sorting.RuleEngine.TestConsole测试控制台
- ✅ 支持模拟分拣机信号发送（HTTP API）
- ✅ 支持模拟DWS数据发送（TCP）
- ✅ 提供交互式命令行界面

### 优化方向

#### 短期优化（1-2周）
1. **SignalR Hub实现** - 创建SignalR Hub用于实时通信
2. **适配器配置界面** - 添加API端点支持运行时切换适配器
3. **通信日志查询API** - 提供API查询和导出通信日志
4. **性能优化** - 优化TouchSocket连接池和消息处理
5. **英文注释清理** - 完成所有英文注释的移除

#### 中期优化（1-3个月）
1. **监控面板** - 开发实时监控面板显示系统状态
2. **更多适配器** - 支持更多厂商的DWS和分拣机设备
3. **负载均衡** - 支持多实例部署和负载均衡
4. **性能测试** - 压力测试和性能基准测试
5. **文档完善** - 完善开发文档和部署文档

#### 长期优化（3-6个月）
1. **微服务架构** - 拆分为微服务架构，提高可扩展性
2. **容器化部署** - 支持Docker和Kubernetes部署
3. **AI规则引擎** - 引入机器学习优化规则匹配
4. **国际化** - 支持多语言界面
5. **云原生** - 支持云平台部署（Azure, AWS, 阿里云）

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