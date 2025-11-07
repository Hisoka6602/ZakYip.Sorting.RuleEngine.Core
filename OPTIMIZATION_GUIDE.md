# 系统优化指南 / System Optimization Guide

本文档说明了系统中实施的各种优化措施和性能改进。
This document describes the various optimizations and performance improvements implemented in the system.

## 1. 日志优化 / Logging Optimization

### 1.1 SQL语句日志过滤 / SQL Statement Log Filtering
- **配置位置**: `ZakYip.Sorting.RuleEngine.Service/nlog.config`
- **优化内容**: Entity Framework Core的SQL语句日志已被过滤，不再记录到日志文件
- **实现方式**: 
  ```xml
  <logger name="Microsoft.EntityFrameworkCore.Database.Command" maxlevel="Info" final="true" />
  ```
- **效果**: 减少日志文件大小，提高日志系统性能

### 1.2 慢查询检测 / Slow Query Detection
- **实现位置**: `ZakYip.Sorting.RuleEngine.Infrastructure/Persistence/Optimizations/QueryOptimizationExtensions.cs`
- **慢查询阈值**: 1000ms (1秒)
- **功能**:
  - 自动检测执行时间超过阈值的查询
  - 记录慢查询的SQL语句和执行时间
  - 仅记录到日志文件，不输出到控制台
- **使用方式**:
  ```csharp
  var results = await query.ExecuteWithSlowQueryDetectionAsync(logger, "QueryName", cancellationToken);
  ```

### 1.3 清理和统计消息优化 / Cleanup and Statistics Message Optimization
- **实现位置**: 
  - `LogFileCleanupService.cs` - 日志清理服务
  - `ChuteStatisticsService.cs` - 格口统计服务
- **优化内容**: 清理和统计消息仅输出到控制台，不记录到日志文件
- **效果**: 减少日志文件中的噪音，保持日志文件专注于错误和重要事件

## 2. 代码质量提升 / Code Quality Improvements

### 2.1 精度提升 / Precision Improvement
- **优化内容**: 将 `double` 类型替换为 `decimal` 类型
- **影响范围**:
  - `FailureRatio` (失败率阈值): `double` → `decimal`
  - `CalculateUtilizationRate` (利用率计算): `double` → `decimal`
  - `SuccessRate` (成功率): `double` → `decimal`
  - `AverageDurationMs` (平均耗时): `double` → `decimal`
- **效果**: 提高数值计算精度，避免浮点数精度损失

### 2.2 布尔字段命名规范 / Boolean Field Naming Convention
- **当前状态**: 大部分布尔字段已使用 `Is`, `Has`, `Can` 等前缀
- **特例**: 
  - `Success` - API响应和性能指标中的标准命名，保持不变
  - `Enabled` - 配置字段的标准命名，保持不变

## 3. 数据库性能优化 / Database Performance Optimization

### 3.1 查询优化扩展 / Query Optimization Extensions
位置: `QueryOptimizationExtensions.cs`

#### 3.1.1 慢查询检测 / Slow Query Detection
```csharp
ExecuteWithSlowQueryDetectionAsync<T>(query, logger, queryName, cancellationToken)
```
- 自动检测并记录慢查询
- 提供SQL语句和执行时间信息

#### 3.1.2 索引使用监控 / Index Usage Monitoring
```csharp
ExecuteWithIndexMonitoringAsync<T>(query, logger, cancellationToken)
```
- 监控查询性能
- 返回查询计划用于索引分析

#### 3.1.3 优化建议 / Optimization Suggestions
```csharp
GetOptimizationSuggestions(executionTimeMs, recordCount, queryString)
```
提供自动优化建议：
- 查询执行时间过长警告
- 返回记录数过多提示
- SELECT * 使用警告
- 缺少WHERE条件提示
- OR条件优化建议

### 3.2 连接池优化 / Connection Pool Tuning
- **配置位置**: `appsettings.json` - MySql.ConnectionString
- **优化参数**:
  - `Pooling=true` - 启用连接池
  - `MinimumPoolSize=5` - 最小连接数: 5
  - `MaximumPoolSize=100` - 最大连接数: 100
  - `ConnectionLifeTime=300` - 连接生命周期: 300秒
  - `ConnectionIdleTimeout=180` - 空闲超时: 180秒
- **效果**: 
  - 减少连接创建开销
  - 提高并发处理能力
  - 避免连接泄漏

### 3.3 分片策略 / Sharding Strategy
- **配置位置**: `appsettings.json` - Sharding
- **当前策略**: Monthly (月度分片)
- **可选策略**: Daily (日度), Weekly (周度), Monthly (月度)
- **优化参数**:
  - `RetentionDays=90` - 数据保留90天
  - `ColdDataThresholdDays=30` - 冷数据阈值30天
  - `IdleMinutesBeforeCleanup=30` - 空闲30分钟后清理
  - `CleanupSchedule` - 每天凌晨2点清理
  - `ArchiveSchedule` - 每天凌晨3点归档

### 3.4 已有的查询优化方法 / Existing Query Optimization Methods

#### OptimizedPaging
```csharp
query.OptimizedPaging(page, pageSize)
```
- 使用 `AsNoTracking()` 提高只读查询性能
- 自动处理分页逻辑

#### OptimizedTimeRange
```csharp
query.OptimizedTimeRange(timeSelector, startTime, endTime)
```
- 优化时间范围查询
- 确保使用索引

#### BulkInsertAsync
```csharp
await context.BulkInsertAsync(entities, cancellationToken)
```
- 批量插入优化
- 禁用自动变更检测以提高性能

#### BulkDeleteAsync
```csharp
await context.BulkDeleteAsync<T>(tableName, createdBefore, cancellationToken)
```
- 使用原始SQL进行批量删除
- 提高大批量删除性能

## 4. 性能监控建议 / Performance Monitoring Recommendations

### 4.1 日志审查 / Log Review
- 定期审查慢查询日志
- 关注警告级别的性能日志
- 监控熔断器状态

### 4.2 指标监控 / Metrics Monitoring
- 监控数据库连接池使用率
- 跟踪慢查询频率和执行时间
- 监控分片表的大小和增长速度

### 4.3 优化建议 / Optimization Recommendations
1. 根据慢查询日志添加或优化索引
2. 对于频繁查询的时间范围，考虑使用编译查询
3. 定期清理和归档旧数据
4. 根据负载测试结果调整连接池大小

## 5. 下一步优化计划 / Future Optimization Plans

### 5.1 代码覆盖率 / Code Coverage
- 目标: 提升至 85% 以上
- 重点: 添加优化功能的单元测试

### 5.2 静态代码分析 / Static Code Analysis
- 工具: SonarQube
- 目标: 集成到CI/CD流程

### 5.3 负载测试优化 / Load Test Optimization
- 基于负载测试结果优化分片策略
- 调整连接池参数
- 优化缓存策略

## 6. 配置示例 / Configuration Examples

### 6.1 开发环境 / Development Environment
```json
{
  "MySql": {
    "ConnectionString": "Server=localhost;Port=3306;Database=dev_db;User=dev;Password=***;Pooling=true;MinimumPoolSize=2;MaximumPoolSize=20;",
    "Enabled": true
  },
  "Sharding": {
    "Enabled": false,
    "RetentionDays": 30
  }
}
```

### 6.2 生产环境 / Production Environment
```json
{
  "MySql": {
    "ConnectionString": "Server=prod-db;Port=3306;Database=prod_db;User=prod;Password=***;Pooling=true;MinimumPoolSize=10;MaximumPoolSize=200;ConnectionLifeTime=600;",
    "Enabled": true
  },
  "Sharding": {
    "Enabled": true,
    "Strategy": "Monthly",
    "RetentionDays": 180
  }
}
```

## 7. 故障排查 / Troubleshooting

### 7.1 慢查询问题 / Slow Query Issues
1. 检查日志文件中的慢查询警告
2. 使用 `GetExecutionPlanAsync` 获取查询计划
3. 使用 `GetOptimizationSuggestions` 获取优化建议
4. 检查相关表的索引

### 7.2 连接池耗尽 / Connection Pool Exhaustion
1. 检查 `MaximumPoolSize` 设置
2. 确保正确释放数据库连接
3. 检查是否有长时间运行的查询
4. 考虑增加连接池大小或优化查询

### 7.3 分片表问题 / Sharding Table Issues
1. 检查清理和归档日志
2. 验证分片策略配置
3. 检查表命名和时间范围
4. 确保有足够的磁盘空间

---

**最后更新**: 2025-11-07
**版本**: 1.1.0

## 8. 最新优化项 (2025-11-07) / Latest Optimizations

### 8.1 代码质量改进 / Code Quality Improvements

#### 8.1.1 消除UTC时间使用 / Eliminate UTC Time Usage
**问题**: 项目中存在多处使用 `DateTime.UtcNow` 和 `DateTimeOffset.UtcNow`，与业务需求不符
**解决方案**: 
- 将所有 `DateTime.UtcNow` 替换为 `DateTime.Now` (共17处)
- 将 `DateTimeOffset.UtcNow` 替换为 `DateTimeOffset.Now` (共1处)
**影响范围**:
- `DataArchiveService.cs` - 数据归档服务
- `DataCleanupService.cs` - 数据清理服务
- `DwsDataReceivedEventHandler.cs` - DWS数据接收事件处理
- `ApiResponse.cs` - API响应DTO
- `PagedResponse.cs` - 分页响应DTO
- `MemoryCacheHealthCheck.cs` - 缓存健康检查
- `JushuitanErpApiClient.cs` - 聚水潭ERP API客户端
- `Program.cs` - 主程序健康检查端点

#### 8.1.2 消除魔法数字 / Eliminate Magic Numbers
**问题**: 代码中存在硬编码的魔法数字，降低可维护性
**解决方案**: 创建 `PerformanceConstants` 常量类统一管理

```csharp
namespace ZakYip.Sorting.RuleEngine.Domain.Constants;

public static class PerformanceConstants
{
    public const int MaxChuteCapacityPerHour = 600;        // 格口每小时最大处理能力
    public const int MaxRetryAttempts = 3;                  // 最大重试次数
    public const int RetryInitialDelayMs = 100;             // 重试初始延迟
    public const int MaxQuerySurroundingRecords = 100;      // 最大查询前后记录数
    public const int MaxPercentage = 100;                   // 百分比计算常量
    public const int CacheAbsoluteExpirationSeconds = 3600; // 缓存绝对过期时间
    public const int CacheSlidingExpirationSeconds = 600;   // 缓存滑动过期时间
}
```

**替换位置**:
- `DataAnalysisService.cs` - 使用率计算、查询验证
- `MonitoringService.cs` - 格口使用率监控
- `CacheSettings.cs` - 缓存配置

#### 8.1.3 工具类性能优化 / Utility Class Performance Optimization
**优化内容**: 为工具类方法添加 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 特性
**影响范围**:
- `ApiRequestHelper.cs` 中的所有公共方法
  - `GenerateFormattedCurl()`
  - `FormatHeaders()`
  - `GetFormattedHeadersFromRequest()`
  - `GetFormattedHeadersFromResponse()`

**效果**: 
- 提高频繁调用方法的执行效率
- 减少方法调用开销
- 编译器会尝试内联这些方法以提升性能

### 8.2 架构验证 / Architecture Verification

#### 8.2.1 厂商工具类组织 / Vendor-Specific Utilities Organization
**验证结果**: ✅ 所有厂商专属工具类已正确组织
- `JushuitanErp/` - 聚水潭ERP相关类
- `WdtWms/` - 旺店通WMS相关类  
- `PostCollection/` - 邮政分揽投相关类
- `PostProcessingCenter/` - 邮政处理中心相关类
- `Shared/` - 共享工具类（ApiRequestHelper, PostalSoapRequestBuilder）

#### 8.2.2 数据库索引配置 / Database Index Configuration
**验证结果**: ✅ 数据库索引已完善配置

主要索引:
- `LogEntries` - Level, CreatedAt（降序）, Level+CreatedAt（复合）
- `CommunicationLogs` - ParcelId, CreatedAt（降序）, Type+CreatedAt（复合）
- `Chutes` - ChuteName, ChuteCode, CreatedAt（降序）
- `SorterCommunicationLogs` - ParcelId, CommunicationTime（降序）
- `DwsCommunicationLogs` - Barcode, CommunicationTime（降序）
- `ApiCommunicationLogs` - ParcelId, RequestTime（降序）
- `MatchingLogs` - ParcelId, ChuteId, MatchingTime（降序）
- `ApiRequestLogs` - RequestTime（降序）, RequestPath, RequestIp, Method+Time（复合）

所有时间字段都使用降序索引，优化时间范围查询性能

#### 8.2.3 异步操作验证 / Async Operations Verification
**验证结果**: ✅ 数据库操作不阻塞主流程

关键点:
- 所有数据库操作使用异步方法（`async/await`）
- 后台同步使用 `Task.Run()` 启动独立任务
- 熔断器切换使用 fire-and-forget 模式
- TCP客户端重连使用独立任务循环
- 没有发现 `.Wait()` 或 `.Result` 阻塞调用（TouchSocketDwsAdapter.Dispose除外）

### 8.3 性能提升总结 / Performance Improvement Summary

1. **代码可维护性提升**: 通过常量类统一管理魔法数字，提高代码可读性和维护性
2. **时间处理统一**: 全面使用本地时间，符合业务场景需求
3. **工具类性能优化**: 通过内联优化减少方法调用开销
4. **数据库查询优化**: 完善的索引配置确保查询性能
5. **异步非阻塞**: 所有IO操作使用异步模式，确保系统响应性

### 8.4 后续优化建议 / Future Optimization Recommendations

1. **考虑添加更多性能特性**: 
   - 为关键业务方法添加 `[MethodImpl]` 特性
   - 考虑使用 `ArrayPool<T>` 减少大数组分配

2. **监控和度量**:
   - 定期审查慢查询日志
   - 监控缓存命中率
   - 跟踪API响应时间

3. **编译优化**:
   - 生产环境使用 Release 配置
   - 考虑启用 ReadyToRun (R2R) 编译
   - 使用 Ahead-of-Time (AOT) 编译（.NET 8+）

