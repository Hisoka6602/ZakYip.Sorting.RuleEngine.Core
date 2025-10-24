# 数据库熔断器和降级方案

## 概述

本系统实现了数据库熔断器机制，当MySQL数据库持续访问失败时，自动降级使用SQLite存储。当MySQL恢复连接后，会自动同步SQLite中的数据到MySQL，并清空SQLite数据和瘦身。

## 特性

### 1. 熔断器配置（可配置）

熔断器的所有参数都可以通过 `appsettings.json` 进行配置：

```json
{
  "AppSettings": {
    "MySql": {
      "ConnectionString": "Server=localhost;Database=sorting_logs;User=root;Password=your_password;",
      "Enabled": true,
      "CircuitBreaker": {
        "FailureRatio": 0.5,              // 失败率阈值（50%）
        "MinimumThroughput": 10,          // 最小吞吐量（请求数）
        "SamplingDurationSeconds": 30,    // 采样周期（30秒）
        "BreakDurationSeconds": 1200      // 熔断持续时间（20分钟）
      }
    }
  }
}
```

### 2. 自动降级

当MySQL数据库出现以下情况时，系统会自动降级使用SQLite：

- MySQL连接失败
- MySQL写入失败
- 失败率达到配置的阈值（默认50%）
- 熔断器打开

### 3. 自动恢复和数据同步

当MySQL恢复正常后，熔断器会关闭，系统会自动执行以下操作：

1. **数据同步** - 将SQLite中的所有日志数据同步到MySQL
2. **清空数据** - 清空SQLite中的日志数据
3. **数据库瘦身** - 执行SQLite的VACUUM操作，释放磁盘空间

### 4. HTTP客户端池化

系统使用ASP.NET Core内置的 `HttpClientFactory` 进行HTTP客户端池化，避免端口耗尽问题：

```csharp
builder.Services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>(client =>
{
    client.BaseAddress = new Uri(appSettings.ThirdPartyApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(appSettings.ThirdPartyApi.TimeoutSeconds);
    
    if (!string.IsNullOrEmpty(appSettings.ThirdPartyApi.ApiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-Key", appSettings.ThirdPartyApi.ApiKey);
    }
});
```

**注意：** HTTP请求不使用熔断器，只有数据库访问使用熔断器。

## 缓存配置

系统支持可配置的缓存过期时间：

```json
{
  "AppSettings": {
    "Cache": {
      "AbsoluteExpirationSeconds": 3600,  // 绝对过期时间（1小时）
      "SlidingExpirationSeconds": 600     // 滑动过期时间（10分钟）
    }
  }
}
```

### 缓存使用示例

```csharp
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_cacheSettings.AbsoluteExpirationSeconds),
    SlidingExpiration = TimeSpan.FromSeconds(_cacheSettings.SlidingExpirationSeconds)
};

_cache.Set(cacheKey, value, cacheOptions);
```

## 数据库索引优化

### MySQL索引

系统为MySQL日志表创建了优化的索引：

1. **Level索引** - 用于按日志级别筛选
2. **CreatedAt降序索引** - 用于时间范围查询和排序（最新的日志排在前面）
3. **复合索引(Level + CreatedAt)** - 优化按日志级别和时间的组合查询

```csharp
entity.HasIndex(e => e.Level).HasDatabaseName("IX_log_entries_Level");
entity.HasIndex(e => e.CreatedAt).IsDescending().HasDatabaseName("IX_log_entries_CreatedAt_Desc");
entity.HasIndex(e => new { e.Level, e.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_log_entries_Level_CreatedAt");
```

### 时间字段降序排序

所有时间相关的查询都默认按降序排序，确保最新的数据排在前面：

```sql
SELECT * FROM log_entries ORDER BY CreatedAt DESC;
```

## 监控和日志

系统会记录以下熔断器相关的日志：

- **熔断器打开** - `LogError("MySQL熔断器打开，切换到SQLite降级方案")`
- **熔断器关闭** - `LogInformation("MySQL熔断器关闭，开始同步SQLite数据到MySQL")`
- **熔断器半开** - `LogInformation("MySQL熔断器半开状态，尝试恢复连接")`
- **数据同步完成** - `LogInformation("成功同步 {Count} 条日志到MySQL")`
- **SQLite清理完成** - `LogInformation("SQLite数据库瘦身完成")`

## 最佳实践

### 1. 熔断器参数调优

根据实际业务场景调整熔断器参数：

- **高可用场景** - 降低失败率阈值（如0.3），增加采样周期（如60秒）
- **低延迟场景** - 减少熔断持续时间（如5分钟），快速尝试恢复
- **高并发场景** - 增加最小吞吐量（如50），确保统计准确性

### 2. 监控告警

建议配置以下监控指标：

- MySQL连接失败次数
- 熔断器打开次数
- SQLite数据量
- 数据同步延迟

### 3. 数据备份

虽然系统会自动同步数据，但建议定期备份SQLite和MySQL数据，防止数据丢失。

## 故障排查

### 熔断器频繁打开

如果熔断器频繁打开，可能的原因：

1. MySQL数据库性能问题
2. 网络连接不稳定
3. 失败率阈值设置过低
4. 采样周期过短

**解决方案：**
- 检查MySQL数据库性能和网络连接
- 适当提高失败率阈值
- 增加采样周期时长

### 数据同步失败

如果数据同步失败，检查：

1. MySQL连接是否正常
2. MySQL权限是否足够
3. MySQL磁盘空间是否充足

**解决方案：**
- 查看日志了解具体错误信息
- 手动执行数据同步脚本
- 检查数据库配置

### SQLite文件过大

如果SQLite文件持续增长：

1. 检查MySQL是否长时间不可用
2. 检查数据同步是否正常执行
3. 检查VACUUM操作是否成功

**解决方案：**
- 修复MySQL连接问题
- 手动执行VACUUM操作
- 定期清理历史数据

## 技术实现

### 核心类

- `ResilientLogRepository` - 带熔断器的弹性日志仓储
- `DatabaseCircuitBreakerSettings` - 数据库熔断器配置
- `MySqlLogRepository` - MySQL日志仓储
- `SqliteLogRepository` - SQLite日志仓储

### 依赖库

- **Polly 8.5.0** - 熔断器和弹性策略
- **Entity Framework Core 8.0** - 数据库访问
- **Microsoft.Extensions.Http** - HTTP客户端池化
- **Microsoft.Extensions.Caching.Memory** - 内存缓存

## 总结

本系统实现了完善的数据库熔断器和降级方案，确保在MySQL不可用时系统依然可以正常运行。所有关键参数都可以通过配置文件调整，方便根据实际场景优化。
