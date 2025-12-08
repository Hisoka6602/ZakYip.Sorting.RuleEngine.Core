# 自动应答模式使用指南 / Auto-Response Mode Usage Guide

## 概述 / Overview

自动应答模式是一个用于测试和演示的功能，允许系统在不调用实际第三方API的情况下模拟API响应。启用此模式后，系统将返回随机的格口ID（1-20），而不是请求外部WCS、ERP或其他第三方系统。

Auto-response mode is a feature for testing and demonstration that allows the system to simulate API responses without calling actual third-party APIs. When enabled, the system returns random chute IDs (1-20) instead of requesting external WCS, ERP, or other third-party systems.

## 使用场景 / Use Cases

1. **测试环境 / Testing Environment**
   - 在没有第三方系统可用时进行功能测试
   - 验证系统的核心逻辑，而不依赖外部服务
   - Test functionality when third-party systems are unavailable
   - Verify core system logic without external dependencies

2. **演示和培训 / Demo and Training**
   - 演示系统功能，无需配置真实的第三方系统
   - 培训新用户，避免对生产系统的影响
   - Demonstrate system features without real third-party system setup
   - Train new users without affecting production systems

3. **开发调试 / Development and Debugging**
   - 快速迭代开发，无需等待外部API响应
   - 模拟各种场景进行边界测试
   - Rapid development iteration without waiting for external API responses
   - Simulate various scenarios for boundary testing

## API 端点 / API Endpoints

### 1. 启用自动应答模式 / Enable Auto-Response Mode

```http
POST /api/autoresponsemode/enable
```

**响应示例 / Response Example:**
```json
{
  "enabled": true,
  "message": "自动应答模式已启用 / Auto-response mode enabled",
  "timestamp": "2024-12-08T10:30:00Z"
}
```

**cURL 示例 / cURL Example:**
```bash
curl -X POST http://localhost:5009/api/autoresponsemode/enable
```

### 2. 禁用自动应答模式 / Disable Auto-Response Mode

```http
POST /api/autoresponsemode/disable
```

**响应示例 / Response Example:**
```json
{
  "enabled": false,
  "message": "自动应答模式已禁用 / Auto-response mode disabled",
  "timestamp": "2024-12-08T10:35:00Z"
}
```

**cURL 示例 / cURL Example:**
```bash
curl -X POST http://localhost:5009/api/autoresponsemode/disable
```

### 3. 查询自动应答模式状态 / Get Auto-Response Mode Status

```http
GET /api/autoresponsemode/status
```

**响应示例 / Response Example:**
```json
{
  "enabled": true,
  "message": "自动应答模式已启用 / Auto-response mode enabled",
  "timestamp": "2024-12-08T10:32:00Z"
}
```

**cURL 示例 / cURL Example:**
```bash
curl http://localhost:5009/api/autoresponsemode/status
```

## 工作原理 / How It Works

### 架构组件 / Architecture Components

1. **IAutoResponseModeService** - 管理自动应答模式的状态
   - 线程安全的启用/禁用功能
   - 默认状态：禁用
   - Thread-safe enable/disable functionality
   - Default state: disabled

2. **MockWcsApiAdapter** - 模拟WCS API适配器
   - 实现 `IWcsApiAdapter` 接口
   - 返回随机格口ID（1-20）
   - 响应结构与真实API一致
   - Implements `IWcsApiAdapter` interface
   - Returns random chute IDs (1-20)
   - Response structure matches real API

3. **WcsApiAdapterFactory** - 动态切换适配器
   - 根据自动应答模式状态选择适配器
   - 启用时使用 `MockWcsApiAdapter`
   - 禁用时使用配置的真实适配器
   - Selects adapter based on auto-response mode state
   - Uses `MockWcsApiAdapter` when enabled
   - Uses configured real adapter when disabled

### 流程图 / Flow Diagram

```
包裹处理请求 / Parcel Processing Request
        ↓
WcsApiAdapterFactory
        ↓
    [检查自动应答模式状态 / Check auto-response mode status]
        ↓
   已启用? / Enabled?
    ↙     ↘
  是/Yes  否/No
    ↓       ↓
MockWcsApiAdapter  ConfiguredAdapter (WcsApiClient, etc.)
    ↓       ↓
返回随机格口ID  请求真实API
1-20        Real API Request
    ↓       ↓
   [包裹处理完成 / Parcel Processing Complete]
```

## 模拟响应示例 / Mock Response Examples

### 请求格口 / Request Chute

当调用 `RequestChuteAsync` 时，MockWcsApiAdapter 返回：

```json
{
  "success": true,
  "code": "200",
  "message": "自动应答模式: 已分配模拟格口 15",
  "data": "{\"chuteNumber\":\"15\"}",
  "responseBody": "{\"chuteNumber\":\"15\",\"weight\":2500.5,\"volume\":9000}",
  "parcelId": "PKG20231101001",
  "requestUrl": "/api/mock/chute-request",
  "requestTime": "2024-12-08T10:30:00Z",
  "responseTime": "2024-12-08T10:30:00Z",
  "responseStatusCode": 200,
  "durationMs": 10
}
```

### 扫描包裹 / Scan Parcel

```json
{
  "success": true,
  "code": "200",
  "message": "模拟扫描成功 / Mock scan successful",
  "data": "{\"chuteNumber\":\"8\"}",
  "parcelId": "1234567890123",
  "requestUrl": "/api/mock/scan",
  "requestTime": "2024-12-08T10:30:00Z",
  "responseTime": "2024-12-08T10:30:00Z",
  "durationMs": 10
}
```

## 最佳实践 / Best Practices

### ✅ 推荐做法 / Recommended Practices

1. **仅在测试和开发环境使用 / Use only in test and development environments**
   - 生产环境应禁用自动应答模式
   - Disable auto-response mode in production environments

2. **测试完成后禁用 / Disable after testing**
   - 避免意外启用影响生产数据
   - Prevent accidental enablement affecting production data

3. **记录模式状态 / Log mode status**
   - 系统日志会记录模式的启用和禁用
   - System logs record mode enablement and disablement

4. **验证响应结构 / Verify response structure**
   - 确保模拟响应与真实API结构一致
   - Ensure mock responses match real API structure

### ❌ 避免做法 / Practices to Avoid

1. **在生产环境启用 / Enabling in production**
   - 会导致不正确的格口分配
   - Results in incorrect chute assignment

2. **长期启用 / Long-term enablement**
   - 仅用于临时测试
   - Use only for temporary testing

3. **依赖特定格口号 / Relying on specific chute numbers**
   - 格口ID是随机生成的（1-20）
   - Chute IDs are randomly generated (1-20)

## 配置说明 / Configuration

自动应答模式的配置通过 API 动态管理，无需修改配置文件。

Auto-response mode configuration is dynamically managed via API, no configuration file changes required.

### 默认状态 / Default State
- **状态 / State**: 禁用 / Disabled
- **格口范围 / Chute Range**: 1-20
- **响应延迟 / Response Delay**: 10ms (模拟 / simulated)

## 监控和日志 / Monitoring and Logging

### 日志记录 / Log Entries

启用自动应答模式时：
```
[Information] 自动应答模式已启用 / Auto-response mode enabled
```

处理包裹时：
```
[Information] 自动应答模式: 包裹 PKG20231101001 分配模拟格口号 15
```

禁用自动应答模式时：
```
[Information] 自动应答模式已禁用 / Auto-response mode disabled
```

### 监控指标 / Metrics

- 查看日志以确认模式状态
- 通过 `/api/autoresponsemode/status` 端点实时查询
- Check logs to confirm mode status
- Real-time query via `/api/autoresponsemode/status` endpoint

## 故障排除 / Troubleshooting

### 问题：启用后仍调用真实API / Issue: Real API still called after enabling

**可能原因 / Possible Causes:**
- 服务未正确重启
- 请求在模式切换前已发起

**解决方案 / Solution:**
- 检查日志确认模式已启用
- 重新发起新的包裹处理请求
- Check logs to confirm mode is enabled
- Initiate new parcel processing requests

### 问题：无法启用模式 / Issue: Cannot enable mode

**可能原因 / Possible Causes:**
- API端点不可访问
- 权限问题

**解决方案 / Solution:**
- 检查服务是否运行
- 验证API端点URL
- Check if service is running
- Verify API endpoint URL

## 安全考虑 / Security Considerations

1. **访问控制 / Access Control**
   - 考虑添加身份验证和授权
   - Consider adding authentication and authorization

2. **环境隔离 / Environment Isolation**
   - 确保测试环境与生产环境隔离
   - Ensure test environments are isolated from production

3. **审计日志 / Audit Logs**
   - 所有模式切换操作都会被记录
   - All mode switching operations are logged

## 版本历史 / Version History

| 版本 Version | 日期 Date | 变更 Changes |
|-------------|-----------|-------------|
| 1.0.0 | 2024-12-08 | 初始版本 / Initial release |

## 相关文档 / Related Documentation

- [API_CLIENT_ENDPOINTS.md](./API_CLIENT_ENDPOINTS.md) - API客户端端点文档
- [TECHNICAL_DEBT.md](./TECHNICAL_DEBT.md) - 技术债务文档
- [README.md](./README.md) - 项目主文档

## 技术支持 / Technical Support

如有问题或建议，请创建 GitHub Issue。

For questions or suggestions, please create a GitHub Issue.

---

**注意 / Note:** 自动应答模式仅用于测试和演示目的，不应在生产环境中使用。

**Note:** Auto-response mode is for testing and demonstration purposes only and should not be used in production environments.
