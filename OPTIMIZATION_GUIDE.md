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

**最后更新**: 2025-11-04
**版本**: 1.0.0
