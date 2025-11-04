# Data Analysis and Monitoring System

## 概述 (Overview)

本系统新增了数据分析API端点和监控告警系统，为分拣规则引擎提供了全面的数据分析和实时监控能力。

This system adds data analysis API endpoints and a monitoring/alerting system, providing comprehensive data analysis and real-time monitoring capabilities for the sorting rule engine.

---

## 1. 数据分析 API (Data Analysis API)

### 1.1 格口使用热力图 (Chute Usage Heatmap)

**端点 (Endpoint):** `GET /api/DataAnalysis/heatmap`

**功能 (Features):**
- 显示各格口在24小时内不同时段的使用率
- 支持时间范围查询
- 支持按格口筛选
- 返回每小时的包裹数量、成功率、失败率

**查询参数 (Query Parameters):**
```
- startDate: 开始日期 (默认: 7天前)
- endDate: 结束日期 (默认: 当前日期)
- chuteId: 格口ID（可选，为空则查询所有格口）
- onlyEnabled: 仅查询启用的格口 (默认: true)
```

**响应示例 (Response Example):**
```json
[
  {
    "chuteId": 1,
    "chuteName": "深圳格口1号",
    "chuteCode": "SZ001",
    "hourlyData": [
      {
        "hour": 0,
        "usageRate": 45.5,
        "parcelCount": 273,
        "successCount": 268,
        "failureCount": 5
      },
      ...
    ],
    "averageUsageRate": 62.3,
    "peakUsageRate": 85.7,
    "peakHour": 14
  }
]
```

### 1.2 分拣效率分析报表 (Sorting Efficiency Report)

**端点 (Endpoint):** `GET /api/DataAnalysis/efficiency-report`

**功能 (Features):**
- 系统级别的分拣效率统计
- 格口利用率分析
- 吞吐量统计
- 识别最高效和最繁忙的格口

**查询参数 (Query Parameters):**
```
- startTime: 开始时间 (可选，默认: 7天前)
- endTime: 结束时间 (可选，默认: 当前时间)
```

**响应示例 (Response Example):**
```json
{
  "totalChutes": 50,
  "enabledChutes": 48,
  "activeChutes": 42,
  "totalParcelsProcessed": 125430,
  "averageUtilizationRate": 68.5,
  "averageSuccessRate": 98.2,
  "systemThroughputPerHour": 745.3,
  "mostEfficientChute": "深圳格口1号",
  "busiestChute": "北京格口3号",
  "startTime": "2023-11-01T00:00:00",
  "endTime": "2023-11-08T00:00:00"
}
```

---

## 2. 监控告警系统 (Monitoring and Alerting System)

### 2.1 实时监控数据 (Real-time Monitoring)

**端点 (Endpoint):** `GET /api/Monitoring/realtime`

**功能 (Features):**
- 当前包裹处理速率
- 活跃格口数量
- 平均格口使用率
- 当前错误率
- 数据库状态
- 系统健康状态

**响应示例 (Response Example):**
```json
{
  "currentProcessingRate": 12.5,
  "activeChutes": 35,
  "averageChuteUsageRate": 65.8,
  "currentErrorRate": 2.3,
  "databaseStatus": "Healthy",
  "lastMinuteParcels": 12,
  "last5MinutesParcels": 58,
  "lastHourParcels": 745,
  "activeAlerts": 2,
  "healthStatus": "Healthy",
  "updateTime": "2023-11-04T15:30:00"
}
```

### 2.2 告警管理 (Alert Management)

#### 2.2.1 获取活跃告警 (Get Active Alerts)

**端点 (Endpoint):** `GET /api/Monitoring/alerts/active`

**响应示例 (Response Example):**
```json
[
  {
    "alertId": "550e8400-e29b-41d4-a716-446655440000",
    "type": "ChuteUsage",
    "severity": "Warning",
    "title": "格口使用率警告",
    "message": "格口 深圳格口1号 使用率为 85.2%，超过警告阈值 80%",
    "resourceId": "1",
    "currentValue": 85.2,
    "thresholdValue": 80.0,
    "alertTime": "2023-11-04T15:25:00",
    "isResolved": false,
    "resolvedTime": null
  }
]
```

#### 2.2.2 获取告警历史 (Get Alert History)

**端点 (Endpoint):** `GET /api/Monitoring/alerts/history?startTime=2023-11-01T00:00:00&endTime=2023-11-08T00:00:00`

#### 2.2.3 解决告警 (Resolve Alert)

**端点 (Endpoint):** `POST /api/Monitoring/alerts/{alertId}/resolve`

#### 2.2.4 手动触发告警检查 (Manual Alert Check)

**端点 (Endpoint):** `POST /api/Monitoring/alerts/check`

---

## 3. SignalR 实时通信 (SignalR Real-time Communication)

### 3.1 监控 Hub (Monitoring Hub)

**连接端点 (Connection Endpoint):** `/hubs/monitoring`

**客户端方法 (Client Methods):**

#### 订阅监控更新
```javascript
connection.invoke("SubscribeToMonitoring");
```

#### 获取实时监控数据
```javascript
const data = await connection.invoke("GetRealtimeMonitoringData");
```

#### 获取活跃告警
```javascript
const alerts = await connection.invoke("GetActiveAlerts");
```

#### 解决告警
```javascript
await connection.invoke("ResolveAlert", alertId);
```

**服务器推送事件 (Server Push Events):**

```javascript
// 新告警通知
connection.on("NewAlert", (alert) => {
  console.log("New alert:", alert);
});

// 监控数据更新
connection.on("MonitoringDataUpdate", (data) => {
  console.log("Monitoring data updated:", data);
});

// 告警已解决
connection.on("AlertResolved", (alertId) => {
  console.log("Alert resolved:", alertId);
});
```

---

## 4. 告警类型和阈值 (Alert Types and Thresholds)

### 4.1 告警类型 (Alert Types)

- **ParcelProcessing** (包裹处理量)
- **ChuteUsage** (格口使用率)
- **PerformanceMetric** (性能指标)
- **ErrorRate** (错误率)
- **DatabaseCircuitBreaker** (数据库熔断)
- **SystemException** (系统异常)

### 4.2 告警级别 (Alert Severity)

- **Info** (信息)
- **Warning** (警告)
- **Error** (错误)
- **Critical** (严重)

### 4.3 默认阈值 (Default Thresholds)

| 指标 | 警告阈值 | 严重阈值 |
|------|---------|---------|
| 格口使用率 | 80% | 95% |
| 错误率 | 5% | 15% |
| 处理速率 | 10 包裹/分钟 | - |

---

## 5. 后台服务 (Background Service)

### MonitoringAlertService

- **执行频率:** 每分钟
- **功能:**
  - 检查包裹处理速率
  - 检查格口使用率
  - 检查错误率
  - 检查数据库状态
  - 自动生成告警

---

## 6. 使用示例 (Usage Examples)

### 6.1 C# API 调用示例

```csharp
using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

// 获取格口使用热力图
var heatmapResponse = await httpClient.GetAsync(
    "/api/DataAnalysis/heatmap?startDate=2023-11-01&endDate=2023-11-08");
var heatmapData = await heatmapResponse.Content.ReadFromJsonAsync<List<ChuteHeatmapDto>>();

// 获取实时监控数据
var monitoringResponse = await httpClient.GetAsync("/api/Monitoring/realtime");
var monitoringData = await monitoringResponse.Content.ReadFromJsonAsync<RealtimeMonitoringDto>();

// 获取活跃告警
var alertsResponse = await httpClient.GetAsync("/api/Monitoring/alerts/active");
var alerts = await alertsResponse.Content.ReadFromJsonAsync<List<MonitoringAlertDto>>();
```

### 6.2 JavaScript SignalR 客户端示例

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/monitoring")
  .withAutomaticReconnect()
  .build();

// 连接到 Hub
await connection.start();

// 订阅监控更新
await connection.invoke("SubscribeToMonitoring");

// 监听新告警
connection.on("NewAlert", (alert) => {
  console.log(`New alert: ${alert.title}`);
  // 更新 UI 显示新告警
});

// 监听监控数据更新
connection.on("MonitoringDataUpdate", (data) => {
  console.log(`Processing rate: ${data.currentProcessingRate}/min`);
  // 更新仪表板
});

// 获取实时数据
const realtimeData = await connection.invoke("GetRealtimeMonitoringData");
console.log(realtimeData);
```

---

## 7. 数据库设计 (Database Design)

### MonitoringAlert 表结构

| 字段 | 类型 | 说明 |
|------|------|------|
| AlertId | string | 告警ID (主键) |
| Type | AlertType | 告警类型 |
| Severity | AlertSeverity | 告警级别 |
| Title | string | 告警标题 |
| Message | string | 告警消息 |
| ResourceId | string? | 相关资源ID |
| CurrentValue | decimal? | 当前值 |
| ThresholdValue | decimal? | 阈值 |
| AlertTime | DateTime | 告警时间 |
| IsResolved | bool | 是否已解决 |
| ResolvedTime | DateTime? | 解决时间 |
| AdditionalData | string? | 额外数据(JSON) |

---

## 8. 系统健康状态评估 (System Health Status Evaluation)

系统健康状态根据以下因素自动评估：

- **Healthy (健康):** 无活跃告警或告警数量 ≤ 2
- **Warning (警告):** 活跃告警数量 > 2
- **Unhealthy (不健康):** 数据库降级、错误率 ≥ 5% 或活跃告警 > 5
- **Critical (严重):** 数据库熔断、错误率 ≥ 15% 或活跃告警 > 10

---

## 9. 性能考虑 (Performance Considerations)

- 监控数据基于 PerformanceMetric 表，建议定期清理历史数据
- 告警检查每分钟执行一次，避免频繁生成重复告警
- SignalR 连接使用分组功能，仅向订阅者推送更新
- 热力图数据按小时聚合，减少数据量

---

## 10. 配置建议 (Configuration Recommendations)

可以通过配置文件调整告警阈值：

```json
{
  "Monitoring": {
    "ChuteUsageWarningThreshold": 80.0,
    "ChuteUsageCriticalThreshold": 95.0,
    "ErrorRateWarningThreshold": 5.0,
    "ErrorRateCriticalThreshold": 15.0,
    "ProcessingRateLowThreshold": 10,
    "CheckIntervalMinutes": 1
  }
}
```

---

## 11. 故障排查 (Troubleshooting)

### 问题：告警未生成

**解决方案:**
1. 检查 MonitoringAlertService 后台服务是否运行
2. 检查 PerformanceMetric 数据是否正常记录
3. 手动触发告警检查: `POST /api/Monitoring/alerts/check`

### 问题：SignalR 连接失败

**解决方案:**
1. 确认 CORS 配置允许 SignalR 连接
2. 检查 `/hubs/monitoring` 端点是否正确映射
3. 查看服务器日志了解连接错误详情

---

## 12. 未来扩展 (Future Enhancements)

- [ ] 支持自定义告警规则
- [ ] 告警通知集成（邮件、短信、钉钉等）
- [ ] 告警趋势分析
- [ ] 机器学习预测性告警
- [ ] 导出报表功能
