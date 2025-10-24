# 事件驱动架构和数据分片实现指南

## Event-Driven Architecture and Data Sharding Implementation Guide

本文档详细说明了ZakYip分拣规则引擎的事件驱动架构和数据分片功能的实现。

This document details the implementation of event-driven architecture and data sharding in the ZakYip Sorting Rule Engine.

---

## 目录 / Table of Contents

1. [事件驱动架构 / Event-Driven Architecture](#事件驱动架构)
2. [数据分片 / Data Sharding](#数据分片)
3. [FIFO队列处理 / FIFO Queue Processing](#fifo队列处理)
4. [自动数据清理 / Automatic Data Cleanup](#自动数据清理)
5. [MySQL自动调谐 / MySQL Auto-Tuning](#mysql自动调谐)
6. [API使用指南 / API Usage Guide](#api使用指南)

---

## 事件驱动架构

### Event-Driven Architecture

项目采用MediatR实现事件驱动架构，确保包裹处理流程的解耦和可扩展性。

The project uses MediatR to implement event-driven architecture, ensuring decoupling and scalability of the parcel processing workflow.

### 领域事件 / Domain Events

#### 1. ParcelCreatedEvent（包裹创建事件）

当分拣程序发送包裹ID和小车号时触发。

Triggered when the sorting machine sends parcel ID and cart number.

```csharp
public class ParcelCreatedEvent : INotification
{
    public string ParcelId { get; init; }
    public string CartNumber { get; init; }
    public string? Barcode { get; init; }
    public long SequenceNumber { get; init; }
}
```

**处理器 / Handler:** `ParcelCreatedEventHandler`
- 记录包裹创建日志
- 在缓存中开辟处理空间

#### 2. DwsDataReceivedEvent（DWS数据接收事件）

当接收到DWS（尺寸重量扫描）数据时触发。

Triggered when DWS (Dimension Weight Scan) data is received.

```csharp
public class DwsDataReceivedEvent : INotification
{
    public string ParcelId { get; init; }
    public DwsData DwsData { get; init; }
}
```

**处理器 / Handler:** `DwsDataReceivedEventHandler`
- 上传数据到第三方API
- 发布第三方响应接收事件

#### 3. ThirdPartyResponseReceivedEvent（第三方API响应事件）

当接收到第三方API响应时触发。

Triggered when third-party API response is received.

```csharp
public class ThirdPartyResponseReceivedEvent : INotification
{
    public string ParcelId { get; init; }
    public ThirdPartyResponse Response { get; init; }
}
```

**处理器 / Handler:** `ThirdPartyResponseReceivedEventHandler`
- 记录第三方响应
- 准备规则匹配

#### 4. RuleMatchCompletedEvent（规则匹配完成事件）

当规则匹配完成并确定格口号时触发。

Triggered when rule matching is completed and chute number is determined.

```csharp
public class RuleMatchCompletedEvent : INotification
{
    public string ParcelId { get; init; }
    public string ChuteNumber { get; init; }
    public string CartNumber { get; init; }
    public int CartCount { get; init; }
}
```

**处理器 / Handler:** `RuleMatchCompletedEventHandler`
- 发送结果给分拣程序
- 清理缓存中的处理空间

### 事件流程 / Event Flow

```
1. 分拣程序 → CreateParcel API
   ↓
2. ParcelCreatedEvent → ParcelCreatedEventHandler
   ↓ (开辟缓存空间)
3. 分拣程序 → ReceiveDWS API
   ↓
4. DwsDataReceivedEvent → DwsDataReceivedEventHandler
   ↓ (上传到第三方API)
5. ThirdPartyResponseReceivedEvent → ThirdPartyResponseReceivedEventHandler
   ↓ (执行规则匹配)
6. RuleMatchCompletedEvent → RuleMatchCompletedEventHandler
   ↓ (发送结果，清理缓存)
7. 完成
```

---

## 数据分片

### Data Sharding

使用EFCore.Sharding实现基于时间维度的表分区，支持热数据和冷数据分离。

Uses EFCore.Sharding to implement time-based table partitioning, supporting hot and cold data separation.

### 配置 / Configuration

在 `appsettings.json` 中配置分片参数：

Configure sharding parameters in `appsettings.json`:

```json
{
  "AppSettings": {
    "Sharding": {
      "Enabled": true,
      "Strategy": "Monthly",
      "RetentionDays": 90,
      "ColdDataThresholdDays": 30,
      "CleanupSchedule": "0 0 2 * * ?",
      "ArchiveSchedule": "0 0 3 * * ?"
    }
  }
}
```

### 配置参数说明 / Configuration Parameters

| 参数 / Parameter | 说明 / Description | 默认值 / Default |
|------------------|-------------------|-----------------|
| Enabled | 是否启用分片 / Enable sharding | true |
| Strategy | 分片策略 (Monthly/Daily/Weekly) / Sharding strategy | Monthly |
| RetentionDays | 数据保留天数 / Data retention days | 90 |
| ColdDataThresholdDays | 冷数据阈值天数 / Cold data threshold days | 30 |
| CleanupSchedule | 清理计划(Cron) / Cleanup schedule (Cron) | 0 0 2 * * ? |
| ArchiveSchedule | 归档计划(Cron) / Archive schedule (Cron) | 0 0 3 * * ? |

### 分片策略 / Sharding Strategies

- **Monthly（月度分片）**: 按月创建表分区，适合数据量中等的场景
- **Daily（日度分片）**: 按天创建表分区，适合高频数据场景
- **Weekly（周度分片）**: 按周创建表分区，适合数据量较小的场景

### 热冷数据分离 / Hot-Cold Data Separation

- **热数据 (Hot Data)**: 最近30天内的数据，存储在主表中，频繁访问
- **冷数据 (Cold Data)**: 30天以前的数据，可归档到历史表，查询较少

---

## FIFO队列处理

### FIFO Queue Processing

使用 `System.Threading.Channels` 实现先进先出的包裹处理队列。

Uses `System.Threading.Channels` to implement First-In-First-Out parcel processing queue.

### 特性 / Features

1. **顺序保证 / Order Guarantee**: 严格按照包裹创建顺序处理
2. **并发处理 / Concurrent Processing**: 多个包裹可以同时在不同阶段处理
3. **缓存管理 / Cache Management**: 使用ConcurrentDictionary管理包裹上下文
4. **自动清理 / Auto Cleanup**: 处理完成后自动清理缓存空间

### 工作流程 / Workflow

```
包裹创建 → 加入FIFO队列 → 按序处理 → DWS数据接收 
→ 第三方API调用 → 规则匹配 → 发送结果 → 清理缓存
```

### 编排服务 / Orchestration Service

`ParcelOrchestrationService` 负责整个流程的编排：

```csharp
// 创建包裹处理空间
await orchestrationService.CreateParcelAsync(parcelId, cartNumber);

// 接收DWS数据
await orchestrationService.ReceiveDwsDataAsync(parcelId, dwsData);

// 后台自动处理队列
await orchestrationService.ProcessQueueAsync(cancellationToken);
```

---

## 自动数据清理

### Automatic Data Cleanup

`DataCleanupService` 后台服务自动清理过期数据。

`DataCleanupService` background service automatically cleans up expired data.

### 功能特性 / Features

- **定时清理 / Scheduled Cleanup**: 每天凌晨2点自动执行
- **可配置保留期 / Configurable Retention**: 默认保留90天
- **日志记录 / Logging**: 记录清理操作和删除数量
- **安全执行 / Safe Execution**: 错误不影响系统运行

### 清理范围 / Cleanup Scope

1. 日志表 (LogEntries)
2. 包裹日志表 (ParcelLogEntries)
3. 其他历史数据表

### 手动触发清理 / Manual Cleanup

如需手动清理，可以调用数据库存储过程或直接执行SQL：

```sql
DELETE FROM log_entries WHERE created_at < DATE_SUB(NOW(), INTERVAL 90 DAY);
```

---

## MySQL自动调谐

### MySQL Auto-Tuning

`MySqlAutoTuningService` 后台服务提供MySQL性能监控和优化建议。

`MySqlAutoTuningService` background service provides MySQL performance monitoring and optimization recommendations.

### 监控项目 / Monitoring Items

#### 1. 表统计信息 / Table Statistics
- 表行数 / Row count
- 表大小 / Table size
- 建议优化或分区 / Optimization recommendations

#### 2. 索引使用情况 / Index Usage
- 识别未使用的索引 / Identify unused indexes
- 索引使用频率分析 / Index usage frequency analysis

#### 3. 连接池状态 / Connection Pool Status
- 当前连接数 / Current connections
- 最大使用连接数 / Max used connections
- 中断连接数 / Aborted connections

#### 4. 慢查询监控 / Slow Query Monitoring
- 识别执行时间>5秒的查询 / Identify queries taking >5 seconds
- 查询优化建议 / Query optimization recommendations

### 自动调谐频率 / Auto-Tuning Frequency

- 检查间隔: 每6小时 / Check interval: Every 6 hours
- 可通过配置修改 / Configurable via settings

### 查看调谐日志 / View Tuning Logs

调谐结果记录在应用日志中，可以通过日志查看：

```bash
# 查看MySQL调谐日志
grep "MySQL自动调谐" /path/to/logs/app.log
```

---

## API使用指南

### API Usage Guide

### 分拣机信号API / Sorting Machine Signal API

#### 1. 创建包裹处理空间 / Create Parcel Processing Space

**端点 / Endpoint:** `POST /api/sortingmachine/create-parcel`

**请求体 / Request Body:**
```json
{
  "parcelId": "PKG20241024001",
  "cartNumber": "CART001",
  "barcode": "1234567890123"
}
```

**响应 / Response:**
```json
{
  "success": true,
  "parcelId": "PKG20241024001",
  "message": "包裹处理空间已创建，等待DWS数据"
}
```

#### 2. 接收DWS数据 / Receive DWS Data

**端点 / Endpoint:** `POST /api/sortingmachine/receive-dws`

**请求体 / Request Body:**
```json
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

**响应 / Response:**
```json
{
  "success": true,
  "parcelId": "PKG20241024001",
  "message": "DWS数据已接收，开始处理"
}
```

### 完整流程示例 / Complete Workflow Example

```bash
# 1. 创建包裹
curl -X POST http://localhost:5000/api/sortingmachine/create-parcel \
  -H "Content-Type: application/json" \
  -d '{
    "parcelId": "PKG001",
    "cartNumber": "CART001",
    "barcode": "1234567890"
  }'

# 2. 发送DWS数据
curl -X POST http://localhost:5000/api/sortingmachine/receive-dws \
  -H "Content-Type: application/json" \
  -d '{
    "parcelId": "PKG001",
    "barcode": "1234567890",
    "weight": 1500,
    "length": 300,
    "width": 200,
    "height": 150,
    "volume": 9000
  }'

# 3. 系统自动处理：
#    - 上传到第三方API
#    - 执行规则匹配
#    - 确定格口号
#    - 发送结果给分拣程序
#    - 清理缓存空间
```

---

## 性能优化

### Performance Optimization

### 系统优化措施 / System Optimizations

1. **事件驱动解耦 / Event-Driven Decoupling**
   - 异步事件处理，不阻塞主流程
   - 各模块独立扩展

2. **FIFO队列 / FIFO Queue**
   - 有界通道防止内存溢出
   - 高效的并发处理

3. **数据分片 / Data Sharding**
   - 减少单表数据量
   - 提高查询性能
   - 支持数据归档

4. **自动调谐 / Auto-Tuning**
   - 持续监控数据库性能
   - 自动识别性能瓶颈
   - 提供优化建议

5. **缓存管理 / Cache Management**
   - 使用ConcurrentDictionary高效管理包裹上下文
   - 处理完成自动清理

### 性能指标 / Performance Metrics

- **吞吐量 / Throughput**: 支持50次/秒的包裹处理
- **延迟 / Latency**: 单个包裹处理<100ms
- **并发度 / Concurrency**: 支持1000个并发包裹处理
- **数据保留 / Data Retention**: 90天自动清理
- **查询性能 / Query Performance**: 热数据查询<50ms

---

## 故障排查

### Troubleshooting

### 常见问题 / Common Issues

#### 1. 包裹处理失败

**问题 / Issue**: 包裹创建后没有响应

**解决方案 / Solution**:
- 检查ParcelQueueProcessorService是否运行
- 查看日志中的错误信息
- 确认DWS数据格式正确

#### 2. 数据清理失败

**问题 / Issue**: 旧数据未被清理

**解决方案 / Solution**:
- 检查DataCleanupService是否启动
- 验证RetentionDays配置
- 查看清理日志

#### 3. MySQL性能问题

**问题 / Issue**: 数据库查询慢

**解决方案 / Solution**:
- 查看MySqlAutoTuningService日志
- 检查索引使用情况
- 考虑增加分片粒度

---

## 最佳实践

### Best Practices

1. **合理配置保留期 / Configure Retention Period Properly**
   - 根据业务需求设置RetentionDays
   - 平衡存储成本和数据可用性

2. **监控系统性能 / Monitor System Performance**
   - 定期查看自动调谐日志
   - 关注慢查询和索引使用

3. **优化分片策略 / Optimize Sharding Strategy**
   - 高频场景使用日度分片
   - 低频场景使用月度分片

4. **错误处理 / Error Handling**
   - 事件处理失败不影响其他流程
   - 记录详细错误日志

5. **测试和验证 / Testing and Validation**
   - 在生产环境前充分测试
   - 验证数据完整性

---

## 总结

### Summary

本实现提供了完整的事件驱动架构和数据分片解决方案，满足高并发、大数据量的分拣场景需求。通过自动化的数据管理和性能优化，确保系统长期稳定运行。

This implementation provides a complete event-driven architecture and data sharding solution, meeting the requirements of high concurrency and large data volume in sorting scenarios. Through automated data management and performance optimization, it ensures long-term stable system operation.
