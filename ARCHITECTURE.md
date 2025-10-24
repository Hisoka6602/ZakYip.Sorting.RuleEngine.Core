# 架构设计文档

## 概述

ZakYip分拣规则引擎核心系统采用清晰的分层架构（Clean Architecture），遵循领域驱动设计（DDD）原则，实现了零边界入侵和高性能的包裹分拣处理系统。

## 架构层次

### 1. Domain Layer（领域层）

**职责**: 定义核心业务实体和领域接口

**特点**:
- 不依赖任何外部框架
- 包含核心业务逻辑
- 定义接口但不实现

**主要组件**:
- `Entities/`: 业务实体
  - `ParcelInfo`: 包裹信息
  - `DwsData`: DWS扫描数据
  - `SortingRule`: 分拣规则
  - `ThirdPartyResponse`: 第三方API响应
  
- `Interfaces/`: 领域服务接口
  - `IRuleEngineService`: 规则引擎服务
  - `IRuleRepository`: 规则仓储
  - `IThirdPartyApiClient`: 第三方API客户端
  - `ILogRepository`: 日志仓储

### 2. Application Layer（应用层）

**职责**: 实现业务用例和应用服务

**特点**:
- 依赖Domain层
- 实现业务流程编排
- 包含性能优化逻辑

**主要组件**:
- `Services/`:
  - `ParcelProcessingService`: 包裹处理服务（使用对象池优化）
  - `RuleEngineService`: 规则引擎服务（带缓存机制）
  
- `DTOs/`: 数据传输对象
  - `ParcelProcessRequest`: 包裹处理请求
  - `ParcelProcessResponse`: 包裹处理响应

**性能优化**:
- 使用`ObjectPool<Stopwatch>`避免频繁对象创建
- 规则缓存机制（5分钟过期时间）
- 支持并行批量处理

### 3. Infrastructure Layer（基础设施层）

**职责**: 实现技术细节和外部集成

**特点**:
- 实现Domain层定义的接口
- 处理数据库、API等外部依赖
- 包含具体的技术实现

**主要组件**:

#### 3.1 持久化 (Persistence)

**LiteDB（配置存储）**:
- 轻量级嵌入式数据库
- 用于存储分拣规则配置
- 无需额外安装
- 高性能读写

**MySQL（日志存储）**:
- 主要日志存储方案
- 支持大规模日志查询
- 企业级可靠性

**SQLite（降级方案）**:
- 当MySQL不可用时自动切换
- 保证系统持续运行
- 与MySQL相同的接口

#### 3.2 API客户端

**ThirdPartyApiClient**:
- HTTP客户端实现
- 自动重试机制
- 超时控制
- 错误处理

### 4. Service Layer（服务层）

**职责**: Windows服务和Web API入口

**特点**:
- Windows Service托管
- ASP.NET Core Minimal API
- Swagger文档支持
- CORS配置

**主要组件**:
- `Program.cs`: 主程序入口，依赖注入配置
- `API/ParcelController`: 包裹处理API
- `API/RuleController`: 规则管理API
- `Configuration/AppSettings`: 应用配置

## 数据流

```
1. 前端/分拣程序
   ↓ (HTTP Request)
2. Service Layer (ParcelController)
   ↓
3. Application Layer (ParcelProcessingService)
   ↓
4. Domain Layer (IRuleEngineService)
   ↓
5. Infrastructure Layer
   ├─→ ThirdPartyApiClient (外部API)
   ├─→ RuleRepository (LiteDB)
   └─→ LogRepository (MySQL/SQLite)
   ↓
6. Response返回给调用方
```

## 依赖关系

```
┌─────────────────────┐
│   Service Layer     │
└──────────┬──────────┘
           │ depends on
           ↓
┌─────────────────────┐
│  Application Layer  │
└──────────┬──────────┘
           │ depends on
           ↓
┌─────────────────────┐
│   Domain Layer      │
└─────────────────────┘
           ↑
           │ implements
┌──────────┴──────────┐
│ Infrastructure Layer│
└─────────────────────┘
```

## 设计原则

### 1. 依赖倒置原则 (DIP)
- 高层模块不依赖低层模块
- 都依赖于抽象（接口）

### 2. 单一职责原则 (SRP)
- 每个类只有一个职责
- Domain层只包含业务逻辑
- Infrastructure层只处理技术实现

### 3. 开闭原则 (OCP)
- 对扩展开放
- 对修改关闭
- 通过接口实现可替换性

### 4. 接口隔离原则 (ISP)
- 接口细粒度设计
- 客户端不依赖不需要的接口

## 性能优化策略

### 1. 对象池
```csharp
private readonly ObjectPool<Stopwatch> _stopwatchPool;
```
- 避免频繁创建和销毁对象
- 提高内存使用效率

### 2. 缓存机制
```csharp
private IEnumerable<SortingRule>? _cachedRules;
private DateTime _lastCacheUpdate;
```
- 减少数据库访问
- 5分钟自动刷新

### 3. 异步处理
```csharp
public async Task<ParcelProcessResponse> ProcessParcelAsync(...)
```
- 全面使用async/await
- 提高吞吐量

### 4. 并行处理
```csharp
var tasks = requests.Select(request => ProcessParcelAsync(...));
await Task.WhenAll(tasks);
```
- 批量处理并行执行
- 最大化CPU利用率

### 5. 连接复用
```csharp
builder.Services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>
```
- HttpClient复用
- 减少连接建立开销

## 容错和降级

### 1. 数据库降级
- MySQL → SQLite自动切换
- 保证服务持续运行

### 2. API容错
- 第三方API失败不影响主流程
- 仍可使用规则引擎

### 3. 日志容错
- 日志写入失败不中断业务
- 防止日志系统成为单点故障

## 配置管理

### 中央包管理
使用`Directory.Packages.props`统一管理所有NuGet包版本：

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
</PropertyGroup>
```

**优点**:
- 避免版本冲突
- 统一升级管理
- 减少配置冗余

### 通用属性
使用`Directory.Build.props`设置项目通用属性：

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

## 部署架构

### 开发环境
```
dotnet run
```
- 快速启动
- Swagger UI调试
- 热重载支持

### Windows服务
```
sc create "ZakYipSortingEngine" binPath="..."
```
- 开机自启动
- 后台运行
- 系统托管

### 容器化（可选）
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY publish/ /app
ENTRYPOINT ["dotnet", "ZakYip.Sorting.RuleEngine.Service.dll"]
```

## 监控和维护

### 健康检查
```
GET /health
```

### 日志查询
```sql
SELECT * FROM log_entries 
WHERE Level = 'ERROR' 
ORDER BY CreatedAt DESC
```

### 性能指标
- 包裹处理延迟
- 规则匹配成功率
- API响应时间

## 扩展性

### 添加新的规则类型
1. 在`RuleEngineService`中添加评估逻辑
2. 更新文档

### 更换数据库
1. 实现对应的Repository接口
2. 修改依赖注入配置

### 集成新的第三方API
1. 实现`IThirdPartyApiClient`
2. 注册新实现

## 安全考虑

- API密钥配置
- CORS策略
- 输入验证
- SQL注入防护（使用EF Core参数化查询）

## 总结

本系统通过清晰的分层架构、合理的性能优化和完善的容错机制，实现了一个高性能、可维护、可扩展的分拣规则引擎系统。
