# DWS 通信配置 API 使用指南
# DWS Communication Configuration API Guide

## 概述 / Overview

本文档介绍如何使用 DWS（尺寸重量扫描）通信配置 API 来配置和管理 DWS 设备通信。

This document describes how to use the DWS (Dimensioning Weighing Scanning) communication configuration API to configure and manage DWS device communication.

## 功能特性 / Features

- ✅ 支持 TCP 客户端和服务端模式 / Support TCP client and server modes
- ✅ 自定义数据解析模板 / Custom data parsing templates
- ✅ 支持 CSV 格式和 JSON 格式数据 / Support CSV and JSON format data
- ✅ 自动重连机制（客户端模式）/ Auto-reconnect mechanism (client mode)
- ✅ 连接池管理（服务端模式）/ Connection pool management (server mode)

## API 端点 / API Endpoints

### 1. DWS 配置管理 / DWS Configuration Management

#### 获取所有配置 / Get All Configurations
```http
GET /api/DwsConfig
```

#### 获取启用的配置 / Get Enabled Configurations
```http
GET /api/DwsConfig/enabled
```

#### 获取指定配置 / Get Configuration by ID
```http
GET /api/DwsConfig/{id}
```

#### 创建配置 / Create Configuration
```http
POST /api/DwsConfig
Content-Type: application/json

{
  "configId": "dws-server-001",
  "name": "DWS服务端配置",
  "mode": "Server",
  "host": "0.0.0.0",
  "port": 8081,
  "dataTemplateId": "template-csv-001",
  "isEnabled": true,
  "maxConnections": 1000,
  "receiveBufferSize": 8192,
  "sendBufferSize": 8192,
  "timeoutSeconds": 30,
  "autoReconnect": false,
  "reconnectIntervalSeconds": 5,
  "description": "DWS设备TCP服务端配置"
}
```

#### 更新配置 / Update Configuration
```http
PUT /api/DwsConfig/{id}
Content-Type: application/json

{
  "configId": "dws-server-001",
  "name": "DWS服务端配置（已更新）",
  "mode": "Server",
  "host": "0.0.0.0",
  "port": 8082,
  "dataTemplateId": "template-csv-001",
  "isEnabled": true,
  ...
}
```

#### 删除配置 / Delete Configuration
```http
DELETE /api/DwsConfig/{id}
```

### 2. DWS 数据模板管理 / DWS Data Template Management

#### 获取所有模板 / Get All Templates
```http
GET /api/DwsDataTemplate
```

#### 创建模板 / Create Template
```http
POST /api/DwsDataTemplate
Content-Type: application/json

{
  "templateId": "template-csv-001",
  "name": "标准CSV模板",
  "template": "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
  "delimiter": ",",
  "isJsonFormat": false,
  "isEnabled": true,
  "description": "标准DWS数据模板：条码,重量,长,宽,高,体积,时间戳"
}
```

## 配置示例 / Configuration Examples

### 示例 1: TCP 服务端模式（CSV 格式）
### Example 1: TCP Server Mode (CSV Format)

**数据模板 / Data Template:**
```json
{
  "templateId": "template-csv-standard",
  "name": "标准CSV模板",
  "template": "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
  "delimiter": ",",
  "isJsonFormat": false,
  "isEnabled": true,
  "description": "标准格式: 条码,重量(g),长(mm),宽(mm),高(mm),体积(cm³),时间戳(ms)"
}
```

**DWS 配置 / DWS Configuration:**
```json
{
  "configId": "dws-server-csv",
  "name": "DWS TCP服务端",
  "mode": "Server",
  "host": "0.0.0.0",
  "port": 8081,
  "dataTemplateId": "template-csv-standard",
  "isEnabled": true,
  "maxConnections": 1000,
  "receiveBufferSize": 8192,
  "sendBufferSize": 8192,
  "timeoutSeconds": 30
}
```

**测试数据 / Test Data:**
```
9811962888027,0.000,0,0,0,0,1765365164205
TEST123,250.5,100.2,50.3,30.1,150.75,1765365164205
```

### 示例 2: TCP 客户端模式（JSON 格式）
### Example 2: TCP Client Mode (JSON Format)

**数据模板 / Data Template:**
```json
{
  "templateId": "template-json",
  "name": "JSON模板",
  "template": "",
  "isJsonFormat": true,
  "isEnabled": true,
  "description": "JSON格式数据解析"
}
```

**DWS 配置 / DWS Configuration:**
```json
{
  "configId": "dws-client-json",
  "name": "DWS TCP客户端",
  "mode": "Client",
  "host": "192.168.1.100",
  "port": 8081,
  "dataTemplateId": "template-json",
  "isEnabled": true,
  "timeoutSeconds": 30,
  "autoReconnect": true,
  "reconnectIntervalSeconds": 5
}
```

**测试数据 / Test Data:**
```json
{
  "barcode": "JSON123",
  "weight": 99.9,
  "length": 150,
  "width": 80,
  "height": 60,
  "volume": 720
}
```

### 示例 3: 自定义分隔符（管道符）
### Example 3: Custom Delimiter (Pipe)

**数据模板 / Data Template:**
```json
{
  "templateId": "template-pipe",
  "name": "管道分隔符模板",
  "template": "{Code}|{Weight}|{Length}|{Width}|{Height}|{Volume}|{Timestamp}",
  "delimiter": "|",
  "isJsonFormat": false,
  "isEnabled": true
}
```

**测试数据 / Test Data:**
```
PIPE123|456.78|100|50|25|125.0|1765365164205
```

## 数据字段映射 / Data Field Mapping

| 模板字段 / Template Field | DWS 字段 / DWS Field | 单位 / Unit | 说明 / Description |
|--------------------------|---------------------|-------------|-------------------|
| `{Code}` 或 `{Barcode}` | `Barcode` | - | 条码 / Barcode |
| `{Weight}` | `Weight` | 克 / grams | 重量 / Weight |
| `{Length}` | `Length` | 毫米 / mm | 长度 / Length |
| `{Width}` | `Width` | 毫米 / mm | 宽度 / Width |
| `{Height}` | `Height` | 毫米 / mm | 高度 / Height |
| `{Volume}` | `Volume` | 立方厘米 / cm³ | 体积 / Volume |
| `{Timestamp}` | `ScannedAt` | Unix时间戳(毫秒/秒) | 扫描时间 / Scan time |

## 时间戳格式支持 / Timestamp Format Support

解析器支持以下时间戳格式 / The parser supports the following timestamp formats:

1. **Unix 毫秒时间戳 / Unix milliseconds**: `1765365164205`
2. **Unix 秒时间戳 / Unix seconds**: `1765365164`
3. **ISO 8601 格式 / ISO 8601 format**: `2025-12-10T18:40:36.344Z`

## 通信模式说明 / Communication Mode Description

### Server 模式（服务端）/ Server Mode

- 系统作为 TCP 服务端，监听指定端口
- System acts as TCP server, listening on specified port
- 支持多客户端连接（连接池）
- Supports multiple client connections (connection pool)
- 适用场景：多台 DWS 设备连接到系统
- Use case: Multiple DWS devices connecting to the system

### Client 模式（客户端）/ Client Mode

- 系统作为 TCP 客户端，连接到 DWS 设备
- System acts as TCP client, connecting to DWS device
- 支持自动重连机制
- Supports auto-reconnect mechanism
- 适用场景：系统主动连接 DWS 设备获取数据
- Use case: System actively connects to DWS device to get data

## 最佳实践 / Best Practices

1. **模板设计 / Template Design**
   - 保持模板简洁，仅包含必要字段
   - Keep templates simple, include only necessary fields
   - 使用有意义的模板名称和描述
   - Use meaningful template names and descriptions

2. **配置管理 / Configuration Management**
   - 为不同的 DWS 设备创建不同的配置
   - Create different configurations for different DWS devices
   - 定期检查和更新配置
   - Regularly check and update configurations

3. **性能优化 / Performance Optimization**
   - 合理设置缓冲区大小
   - Set buffer sizes appropriately
   - 根据实际需求调整连接池大小（服务端模式）
   - Adjust connection pool size based on actual needs (server mode)

4. **故障处理 / Error Handling**
   - 启用自动重连（客户端模式）
   - Enable auto-reconnect (client mode)
   - 监控通信日志，及时发现问题
   - Monitor communication logs to identify issues promptly

## 相关文档 / Related Documents

- [API 客户端配置指南](API_CLIENT_ENDPOINTS.md)
- [通信适配器测试报告](COMMUNICATION_ADAPTERS_TEST_REPORT.md)
- [MQTT 使用指南](MQTT_USAGE_GUIDE.md)

## 技术支持 / Technical Support

如有问题，请查看：
For issues, please check:

- 系统日志: `./logs`
- 通信日志: 通过 API 查询 `/api/CommunicationLog`
- Communication logs: Query via API `/api/CommunicationLog`
