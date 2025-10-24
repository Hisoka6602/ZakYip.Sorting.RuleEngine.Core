# ZakYip.Sorting.RuleEngine.Core

## 项目简介

ZakYip分拣规则引擎核心系统是一个高性能的包裹分拣规则引擎，用于处理分拣程序的包裹信息和DWS（尺寸重量扫描）数据，通过自定义规则分析计算格口号，实现自动化分拣。

## 核心特性

- ✅ **Windows服务** - 作为Windows服务运行，稳定可靠
- ✅ **MiniAPI集成** - 内置Web API用于前端配置和交互
- ✅ **高性能设计** - 使用对象池、滑动过期缓存、异步处理等技术，适合高频率场景（50次/秒）
- ✅ **多数据库支持** - LiteDB存储配置，MySQL记录日志，SQLite作为降级方案，支持EF Core自动迁移
- ✅ **弹性架构** - 集成Polly熔断器和重试策略，防止系统雪崩
- ✅ **多协议支持** - 支持TCP/HTTP等多种协议，通过适配器模式扩展多厂商对接
- ✅ **清晰架构** - 采用DDD分层架构，零边界入侵
- ✅ **中央包管理** - 使用Directory.Packages.props统一管理NuGet包版本
- ✅ **完整测试** - 单元测试覆盖核心功能
- ✅ **完整中文注释** - 所有代码都包含详细的中文和英文注释

## 架构设计

项目采用清晰的分层架构（Clean Architecture / DDD）：

```
ZakYip.Sorting.RuleEngine.Core/
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
- **LiteDB** - 嵌入式NoSQL数据库（配置存储）
- **Entity Framework Core** - ORM框架，支持自动迁移
- **MySQL / SQLite** - 关系型数据库（日志存储）
- **Polly** - 弹性和瞬态故障处理（重试、熔断器）
- **IMemoryCache** - 滑动过期内存缓存
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
cd ZakYip.Sorting.RuleEngine.Core

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

### 包裹处理API

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

1. **日志降级** - MySQL失败时自动切换到SQLite
2. **熔断器** - 使用Polly熔断器，防止级联故障
   - 失败率达到50%时自动熔断
   - 熔断30秒后尝试恢复
3. **重试策略** - 自动重试失败的HTTP请求
   - 最多3次重试
   - 指数退避策略
4. **API容错** - 第三方API失败时仍可使用规则引擎
5. **规则缓存** - 数据库不可用时使用缓存规则

## 监控和日志

- **结构化日志** - 使用Microsoft.Extensions.Logging
- **数据库日志** - 持久化到MySQL或SQLite
- **健康检查** - `/health` 端点监控服务状态
- **熔断器监控** - 记录熔断器状态变化和异常

## 多协议支持

系统通过适配器模式支持多种通信协议：

### 分拣机适配器（ISorterAdapter）
- **TCP协议** - `TcpSorterAdapter` 支持标准TCP通信
- **可扩展** - 通过实现`ISorterAdapter`接口支持其他厂商协议

### 第三方API适配器（IThirdPartyAdapter）
- **HTTP协议** - `HttpThirdPartyAdapter` 带熔断器的HTTP通信
- **TCP协议** - 可扩展支持TCP协议API
- **可扩展** - 通过实现`IThirdPartyAdapter`接口支持其他协议

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

## 贡献

欢迎提交Issue和Pull Request！

## 许可证

MIT License

## 联系方式

- 项目地址: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core
- 问题反馈: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core/issues

---

**注意**: 本系统设计用于高频率场景，确保硬件资源充足以获得最佳性能。