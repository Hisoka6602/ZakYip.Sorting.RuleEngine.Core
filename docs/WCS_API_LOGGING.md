# WCS API通信日志配置说明

## 📋 概述 / Overview

本系统现已支持将所有与WCS系统的API交互内容记录到专用日志文件中。

This system now supports logging all API interactions with the WCS system to a dedicated log file.

## 📁 日志文件位置 / Log File Location

WCS API通信日志文件位于：

```
./logs/comm-wcs-api-{日期}.log
```

例如 / Example:
- `./logs/comm-wcs-api-2025-12-23.log`

归档日志位于 / Archived logs in:
```
./logs/archives/comm-wcs-api-{日期}.log
```

## 📝 日志内容 / Log Content

日志文件包含完整的WCS API通信详情：

### 1. 请求详情 / Request Details

记录每个WCS API请求的完整信息：

```
2025-12-23 13:20:15.123|INFO|WcsApiClient|WCS API请求 [RequestChute] - URL: http://wcs-server/api/chute/request, ParcelId: 1001, Barcode: TEST12345, RequestBody: {"parcelId":"1001","barcode":"TEST12345",...}, Headers: {"Content-Type":"application/json","X-API-Key":"***"}
```

包含字段：
- **操作类型** - ScanParcel, RequestChute, UploadImage, NotifyChuteLanding
- **URL** - 完整的API请求地址
- **ParcelId** - 包裹ID
- **Barcode** - 条码
- **RequestBody** - 完整的请求体（JSON格式）
- **Headers** - 请求头信息

### 2. 响应详情 / Response Details

记录每个WCS API响应的完整信息：

```
2025-12-23 13:20:15.456|INFO|WcsApiClient|WCS API响应 [RequestChute] - ParcelId: 1001, Barcode: TEST12345, StatusCode: 200, Duration: 333ms, ResponseBody: {"success":true,"chuteId":"A01",...}, ResponseHeaders: {"Content-Type":"application/json"}
```

包含字段：
- **StatusCode** - HTTP状态码
- **Duration** - 请求耗时（毫秒）
- **ResponseBody** - 完整的响应体
- **ResponseHeaders** - 响应头信息

### 3. 异常详情 / Exception Details

记录API调用异常的完整堆栈信息：

```
2025-12-23 13:20:15.789|ERROR|WcsApiClient|WCS API异常 [RequestChute] - ParcelId: 1001, Barcode: TEST12345, Duration: 5000ms, RequestBody: {...}, Exception: System.Net.Http.HttpRequestException: Connection timeout...
```

## 🔧 配置 / Configuration

### NLog配置文件 / NLog Configuration

WCS API日志配置在 `Service/nlog.config` 中：

```xml
<!-- 文件输出 - WCS API通信日志（单独文件） -->
<target xsi:type="File" name="wcsApiCommFile"
        fileName="${logDirectory}/comm-wcs-api-${shortdate}.log"
        layout="${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
        archiveEvery="Day"
        archiveNumbering="Date"
        maxArchiveFiles="30"
        concurrentWrites="true"
        encoding="utf-8" />

<!-- 路由规则 -->
<logger name="*.ApiClients.WcsApiClient" minlevel="Debug" writeTo="wcsApiCommFile" final="true" />
<logger name="*.WcsApiCalledEventHandler" minlevel="Debug" writeTo="wcsApiCommFile" final="true" />
```

### 日志级别 / Log Levels

- **Debug** - 开始请求的调试信息
- **Information** - 请求/响应的完整详情
- **Warning** - 请求失败（HTTP 4xx/5xx）
- **Error** - 异常和错误

### 保留策略 / Retention Policy

- **每日归档** - 每天凌晨自动归档前一天的日志
- **保留30天** - 自动删除30天前的归档日志
- **并发写入** - 支持多线程并发写入日志

## 📊 支持的API操作 / Supported API Operations

系统记录以下4类WCS API操作：

### 1. 扫描包裹 / Scan Parcel
- **操作** - `ScanParcel`
- **端点** - `POST /api/parcel/scan`
- **用途** - 在WCS系统中注册包裹

### 2. 请求格口 / Request Chute
- **操作** - `RequestChute`
- **端点** - `POST /api/chute/request`
- **用途** - 根据包裹DWS数据和OCR结果请求分配格口

### 3. 上传图片 / Upload Image
- **操作** - `UploadImage`
- **端点** - `POST /api/image/upload`
- **用途** - 上传包裹图片到WCS系统

### 4. 落格回调 / Notify Chute Landing
- **操作** - `NotifyChuteLanding`
- **端点** - `POST /api/chute/landing`
- **用途** - 通知WCS包裹已成功落格

## 🔍 日志查询示例 / Log Query Examples

### 查询特定包裹的所有API请求

```bash
grep "ParcelId: 1001" ./logs/comm-wcs-api-2025-12-23.log
```

### 查询所有失败的API请求

```bash
grep "ERROR\|WARNING" ./logs/comm-wcs-api-2025-12-23.log
```

### 查询特定操作类型

```bash
grep "\[RequestChute\]" ./logs/comm-wcs-api-2025-12-23.log
```

### 查询特定条码的请求

```bash
grep "Barcode: TEST12345" ./logs/comm-wcs-api-2025-12-23.log
```

### 统计API调用次数

```bash
grep "WCS API请求" ./logs/comm-wcs-api-2025-12-23.log | wc -l
```

### 统计失败率

```bash
# 总请求数
total=$(grep "WCS API请求" ./logs/comm-wcs-api-2025-12-23.log | wc -l)
# 失败请求数
failed=$(grep "WCS API异常\|WCS API响应.*StatusCode: [45]" ./logs/comm-wcs-api-2025-12-23.log | wc -l)
# 失败率
echo "失败率: $(awk "BEGIN {printf \"%.2f%%\", ($failed/$total)*100}")"
```

## 🎯 最佳实践 / Best Practices

### 1. 定期检查日志

建议每天检查WCS API通信日志，关注：
- 异常和错误率
- 响应时间超过1秒的请求
- 4xx/5xx错误

### 2. 监控关键指标

使用日志分析工具监控：
- API调用总数
- 平均响应时间
- 错误率
- 超时请求数

### 3. 问题排查

当WCS系统出现问题时：
1. 查看 `comm-wcs-api-{日期}.log` 确认请求是否发送
2. 检查请求体（RequestBody）是否正确
3. 查看响应体（ResponseBody）了解WCS返回的错误信息
4. 检查异常堆栈（Exception）定位问题根源

### 4. 性能优化

如果发现性能问题：
1. 查看Duration字段，识别慢请求
2. 分析ResponseBody，确认是否返回了过多数据
3. 检查是否存在重复的API调用

## 🔒 安全注意事项 / Security Notes

### 敏感信息保护

日志中的敏感HTTP Header已自动脱敏：
- **API密钥（X-API-Key）** - 只显示前4个字符，其余显示为 `***` (例如: `abcd***`)
- **认证Token（Authorization, X-Auth-Token）** - 只显示前4个字符，其余显示为 `***`
- **Cookie和Set-Cookie** - 自动脱敏处理
- **其他认证相关Header** - 包括 `Proxy-Authorization`, `WWW-Authenticate`, `X-Access-Token`, `X-Refresh-Token`

**脱敏示例**:
```json
{
  "X-API-Key": "abcd***",
  "Content-Type": "application/json",
  "Authorization": "Bear***"
}
```

**注意**: 请求和响应的**消息体（Body）不会自动脱敏**。如果Body中包含敏感个人信息（如姓名、身份证号、手机号等），这些信息会被完整记录。建议：
- 在系统设计中避免在请求/响应Body中传递不必要的敏感个人信息
- 结合日志保留策略，定期清理历史日志
- 在生产环境中限制日志文件的访问权限

### 访问权限

建议设置适当的文件权限：
```bash
chmod 640 ./logs/comm-wcs-api-*.log
```

### 日志备份

重要的API通信日志应定期备份到安全位置。

---

## 📞 技术支持 / Technical Support

如有问题，请联系开发团队或查看相关文档：
- `nlog.config` - NLog配置文件
- `WcsApiClient.cs` - WCS API客户端实现
- `WcsApiCalledEventHandler.cs` - API调用事件处理器

---

**创建日期 / Created Date**: 2025-12-23  
**版本 / Version**: 1.0  
**状态 / Status**: ✅ 已启用 / Enabled
