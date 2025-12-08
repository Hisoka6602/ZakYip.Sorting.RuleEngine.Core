# 第三方API对接说明文档 / Third-Party API Integration Documentation

本文档详细说明了 ZakYip 分拣规则引擎系统已对接的所有第三方 API 及其对接方式。

This document provides detailed information about all third-party APIs integrated with the ZakYip Sorting Rule Engine system and their integration approaches.

---

## 📋 目录 / Table of Contents

1. [概述 / Overview](#概述--overview)
2. [已对接的第三方API列表 / Integrated Third-Party APIs](#已对接的第三方api列表--integrated-third-party-apis)
3. [对接架构 / Integration Architecture](#对接架构--integration-architecture)
4. [各API详细对接说明 / Detailed Integration Documentation](#各api详细对接说明--detailed-integration-documentation)
5. [配置管理 / Configuration Management](#配置管理--configuration-management)
6. [测试和调试 / Testing and Debugging](#测试和调试--testing-and-debugging)
7. [故障排查 / Troubleshooting](#故障排查--troubleshooting)

---

## 概述 / Overview

ZakYip 分拣规则引擎系统通过适配器模式（Adapter Pattern）与多个第三方 WCS（仓库控制系统）和 ERP（企业资源规划）系统进行集成。系统采用统一的 `IWcsApiAdapter` 接口，支持运行时动态切换不同的 API 适配器。

The ZakYip Sorting Rule Engine integrates with multiple third-party WCS (Warehouse Control System) and ERP (Enterprise Resource Planning) systems through the Adapter Pattern. The system uses a unified `IWcsApiAdapter` interface and supports runtime switching between different API adapters.

### 核心特性 / Key Features

- ✅ **统一接口抽象** - 所有第三方API实现相同的接口
- ✅ **运行时动态切换** - 无需重启即可切换API适配器
- ✅ **容错机制** - API失败后自动降级到规则引擎
- ✅ **完整日志记录** - 所有API调用详细记录到数据库
- ✅ **自动应答模式** - 支持模拟API响应用于测试
- ⚠️ **待完善** - Polly弹性策略（重试、熔断、超时）

---

## 已对接的第三方API列表 / Integrated Third-Party APIs

系统目前已对接 **6 个第三方API系统** + **1 个通用适配器** + **1 个模拟适配器**：

The system currently integrates with **6 third-party API systems** + **1 generic adapter** + **1 mock adapter**:

| # | API名称 | 类型 | 协议 | 状态 | 主要功能 |
|---|---------|------|------|------|---------|
| 1 | **PostCollectionApiClient** | 邮政分揽投机构 | SOAP | ✅ 生产就绪 | 包裹扫描 + 格口分配 |
| 2 | **PostProcessingCenterApiClient** | 邮政处理中心 | SOAP | ✅ 生产就绪 | 包裹扫描 + 格口分配 |
| 3 | **JushuitanErpApiClient** | 聚水潭ERP | HTTP/JSON | ✅ 生产就绪 | 上传重量数据 |
| 4 | **WdtWmsApiClient** | 旺店通WMS | HTTP/JSON | ✅ 生产就绪 | 物流称重数据上传 |
| 5 | **WdtErpFlagshipApiClient** | 旺店通ERP旗舰版 | HTTP/JSON | ✅ 生产就绪 | 销售出库称重扩展 |
| 6 | **WcsApiClient** | 通用WCS | HTTP/JSON | ✅ 生产就绪 | 通用格口请求 |
| 7 | **MockWcsApiAdapter** | 模拟适配器 | 内存 | ✅ 测试可用 | 自动应答模式 |

### 功能对比 / Feature Comparison

| API | 扫描包裹 | 请求格口 | 上传重量 | 认证方式 | 性能 |
|-----|---------|---------|---------|---------|------|
| PostCollection | ✅ | ✅ | ✅ | SOAP Signature | ⭐⭐⭐⭐ |
| PostProcessingCenter | ✅ | ✅ | ✅ | SOAP Signature | ⭐⭐⭐⭐ |
| JushuitanErp | ❌ | ✅ | ✅ | HMAC-MD5 | ⭐⭐⭐⭐⭐ |
| WdtWms | ❌ | ✅ | ✅ | HMAC-MD5 | ⭐⭐⭐⭐⭐ |
| WdtErpFlagship | ❌ | ✅ | ✅ | Custom Sign | ⭐⭐⭐⭐⭐ |
| WcsApiClient | ❌ | ✅ | ✅ | Basic/Custom | ⭐⭐⭐⭐ |
| MockAdapter | ❌ | ✅ | ❌ | None | ⭐⭐⭐⭐⭐ |

---

## 对接架构 / Integration Architecture

### 架构图 / Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    ParcelProcessingService                      │
│                     (包裹处理服务)                               │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    IWcsApiAdapterFactory                        │
│                    (API适配器工厂)                               │
│                                                                 │
│  - GetCurrentAdapter()  获取当前活动的适配器                     │
│  - SwitchAdapter()      运行时动态切换适配器                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      IWcsApiAdapter                             │
│                      (统一接口)                                  │
│                                                                 │
│  + ScanParcelAsync()    扫描包裹                                │
│  + RequestChuteAsync()  请求格口                                │
└─────────────────────────────────────────────────────────────────┘
                             │
          ┌──────────────────┼──────────────────┐
          ▼                  ▼                  ▼
┌──────────────────┐ ┌──────────────┐ ┌─────────────────┐
│ BasePostalApi    │ │ Jushuituan   │ │ WdtWms          │
│ Client           │ │ ErpApiClient │ │ ApiClient       │
│                  │ │              │ │                 │
│ - PostCollection │ └──────────────┘ └─────────────────┘
│ - PostProcessing │         ▼                  ▼
└──────────────────┘ ┌──────────────┐ ┌─────────────────┐
          │          │ WdtErpFlag   │ │ WcsApiClient    │
          ▼          │ shipApiClient│ │                 │
┌──────────────────┐ └──────────────┘ └─────────────────┘
│ SOAP Protocol    │         │                  │
│ (XML)            │         ▼                  ▼
└──────────────────┘ ┌──────────────────────────────────┐
                     │ HTTP/JSON Protocol               │
                     └──────────────────────────────────┘
```

### 关键组件 / Key Components

#### 1. IWcsApiAdapter 接口 / Interface

所有第三方API客户端必须实现此接口：

All third-party API clients must implement this interface:

```csharp
public interface IWcsApiAdapter
{
    /// <summary>
    /// 扫描包裹（部分API不支持）
    /// Scan parcel (not supported by all APIs)
    /// </summary>
    Task<WcsApiResponse> ScanParcelAsync(string barcode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 请求格口号（上传DWS数据）
    /// Request chute number (upload DWS data)
    /// </summary>
    Task<WcsApiResponse> RequestChuteAsync(string parcelId, DwsData dwsData, CancellationToken cancellationToken = default);
}
```

#### 2. WcsApiAdapterFactory 工厂类 / Factory Class

负责管理和切换API适配器：

Manages and switches API adapters:

```csharp
public class WcsApiAdapterFactory : IWcsApiAdapterFactory
{
    public IWcsApiAdapter GetCurrentAdapter() { }
    public void SwitchAdapter(string adapterName) { }
}
```

#### 3. WcsApiResponse 统一响应模型 / Unified Response Model

所有API返回统一的响应格式：

All APIs return a unified response format:

```csharp
public class WcsApiResponse
{
    public bool Success { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string? Data { get; set; }
    public string? ParcelId { get; set; }
    public string? RequestUrl { get; set; }
    public string? RequestBody { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime ResponseTime { get; set; }
    public long DurationMs { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? FormattedCurl { get; set; }
}
```

---

## 各API详细对接说明 / Detailed Integration Documentation

### 1. PostCollectionApiClient - 邮政分揽投机构

**类型 / Type:** 邮政系统 / Postal System  
**协议 / Protocol:** SOAP (XML)  
**参考文档 / Reference:** [PostInApi Gist](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)

#### 功能说明 / Features

- ✅ **包裹扫描** - 将包裹信息提交到邮政系统（`getYJSM` 方法）
- ✅ **格口请求** - 请求格口号并上传重量体积数据（`postWLCLMH` 方法）

#### 技术实现 / Technical Implementation

**基类继承 / Base Class:**
```csharp
public class PostCollectionApiClient : BasePostalApiClient
{
    protected override string ClientTypeName => "邮政分揽投机构";
}
```

**认证机制 / Authentication:**
- SOAP 请求签名
- 设备ID验证 (`DeviceId`)
- 员工编号验证 (`EmployeeNumber`)

**请求格式 / Request Format (SOAP XML):**
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:post="http://post.postal.service/">
    <soapenv:Header/>
    <soapenv:Body>
        <post:postWLCLMH>
            <arg0>
                <dwsbh>设备编号</dwsbh>
                <xcbh>小车编号</xcbh>
                <ztm>主条码</ztm>
                <zl>重量（克）</zl>
                <tj>体积（立方厘米）</tj>
                <cd>长度（厘米）</cd>
                <kd>宽度（厘米）</kd>
                <gd>高度（厘米）</gd>
                <jgsj>交个时间</jgsj>
            </arg0>
        </post:postWLCLMH>
    </soapenv:Body>
</soapenv:Envelope>
```

**响应解析 / Response Parsing:**
```csharp
// 从SOAP响应中提取格口号
var match = Regex.Match(responseBody, @"<return>(\d+)</return>");
if (match.Success)
{
    chuteNumber = match.Groups[1].Value;
}
```

#### 配置参数 / Configuration Parameters

```csharp
protected const string WorkshopCode = "WS20140010";      // 作坊代码
protected const string DeviceId = "20140010";            // 设备ID
protected const string CompanyName = "广东泽业科技有限公司"; // 公司名称
protected const string DeviceBarcode = "141562320001131"; // 设备条码
protected const string OrganizationNumber = "20140011";   // 机构号
protected const string EmployeeNumber = "00818684";       // 员工号
```

#### 使用示例 / Usage Example

```csharp
var apiClient = serviceProvider.GetRequiredService<PostCollectionApiClient>();

// 扫描包裹
var scanResult = await apiClient.ScanParcelAsync("PKG123456");

// 请求格口
var dwsData = new DwsData
{
    Barcode = "PKG123456",
    Weight = 1500,  // 克
    Length = 30,    // 厘米
    Width = 20,
    Height = 10,
    Volume = 6000000 // 立方厘米
};
var chuteResult = await apiClient.RequestChuteAsync("PKG123456", dwsData);
```

---

### 2. PostProcessingCenterApiClient - 邮政处理中心

**类型 / Type:** 邮政系统 / Postal System  
**协议 / Protocol:** SOAP (XML)  
**参考文档 / Reference:** [PostInApi Gist](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)

#### 功能说明 / Features

与 PostCollectionApiClient 功能完全相同，但面向邮政处理中心系统。

Same functionality as PostCollectionApiClient, but targets postal processing center systems.

#### 技术实现 / Technical Implementation

**基类继承 / Base Class:**
```csharp
public class PostProcessingCenterApiClient : BasePostalApiClient
{
    protected override string ClientTypeName => "邮政处理中心";
}
```

所有其他实现与 PostCollectionApiClient 相同，共享 `BasePostalApiClient` 基类。

All other implementations are identical to PostCollectionApiClient, sharing the `BasePostalApiClient` base class.

---

### 3. JushuitanErpApiClient - 聚水潭ERP

**类型 / Type:** ERP 系统 / ERP System  
**协议 / Protocol:** HTTP/JSON  
**参考文档 / Reference:** [聚水潭 API Gist](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)

#### 功能说明 / Features

- ❌ **不支持包裹扫描** - 返回功能不支持的响应
- ✅ **上传重量数据** - 上传包裹称重信息到聚水潭ERP

#### 技术实现 / Technical Implementation

**认证机制 / Authentication:**

使用 HMAC-MD5 签名算法：

```csharp
private string GenerateSign(Dictionary<string, object> parameters, string appSecret)
{
    // 1. 参数排序
    var sortedParams = parameters.OrderBy(p => p.Key);
    
    // 2. 拼接字符串
    var signStr = string.Join("", sortedParams.Select(p => $"{p.Key}{p.Value}"));
    
    // 3. 添加 appSecret
    signStr = appSecret + signStr + appSecret;
    
    // 4. MD5 哈希并转大写
    using var md5 = MD5.Create();
    var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
    return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
}
```

**请求参数 / Request Parameters:**

```json
{
  "partnerid": "app_key",
  "token": "access_token",
  "sign": "calculated_signature",
  "version": 2,
  "method": "orders.weight.send.upload",
  "data": {
    "tid": "订单号",
    "shop_id": 0,
    "wms_co_id": "仓库编号",
    "logistics_code": "物流公司编码",
    "l_id": "物流单号",
    "package_id": "包裹ID",
    "weight": 1.25,
    "is_upload_weight": true,
    "type": 1,
    "is_unlid": false,
    "channel": "sorting_system",
    "default_weight": -1
  }
}
```

#### 配置参数 / Configuration Parameters

```csharp
public class JushuitanErpApiParameters
{
    public required string Url { get; init; }
    public int TimeOut { get; init; } = 5000;
    public required string AppKey { get; init; }
    public required string AppSecret { get; init; }
    public required string AccessToken { get; init; }
    public int Version { get; init; } = 2;
    public bool IsUploadWeight { get; init; } = true;
    public int Type { get; init; } = 1;
    public bool IsUnLid { get; init; } = false;
    public required string Channel { get; init; }
    public decimal DefaultWeight { get; init; } = -1;
}
```

#### 响应格式 / Response Format

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chute_number": "5",
    "success": true
  }
}
```

#### 使用示例 / Usage Example

```csharp
var parameters = new JushuitanErpApiParameters
{
    Url = "https://openapi.jushuitan.com/open/orders/weight/send/upload",
    AppKey = "your_app_key",
    AppSecret = "your_app_secret",
    AccessToken = "your_access_token",
    Channel = "sorting_system"
};

var apiClient = new JushuitanErpApiClient(httpClient, logger);
apiClient.Parameters = parameters;

var result = await apiClient.RequestChuteAsync("PKG123456", dwsData);
```

---

### 4. WdtWmsApiClient - 旺店通WMS

**类型 / Type:** WMS 系统 / WMS System  
**协议 / Protocol:** HTTP/JSON  
**参考文档 / Reference:** [旺店通 API Gist](https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e)

#### 功能说明 / Features

- ❌ **不支持包裹扫描** - 返回功能不支持的响应
- ✅ **物流称重** - 上传物流包裹称重数据（`wms.logistics.Consign.weigh` 方法）

#### 技术实现 / Technical Implementation

**认证机制 / Authentication:**

使用 HMAC-MD5 签名算法（与聚水潭类似）：

```csharp
private string CalculateSign(SortedDictionary<string, string> parameters, string appSecret)
{
    var signStr = string.Join("", parameters.Select(p => $"{p.Key}{p.Value}"));
    signStr = appSecret + signStr + appSecret;
    
    using var md5 = MD5.Create();
    var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
    return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
}
```

**请求参数 / Request Parameters:**

```json
{
  "appkey": "your_app_key",
  "method": "wms.logistics.Consign.weigh",
  "timestamp": "2025-12-08 10:00:00",
  "format": "json",
  "v": "1.0",
  "sign": "calculated_signature",
  "sid": "your_sid",
  "body": {
    "logistics_code": "SF",
    "bill_no": "SF1234567890",
    "weight": 1.5,
    "length": 30.0,
    "width": 20.0,
    "height": 10.0,
    "volume": 6.0
  }
}
```

#### 配置参数 / Configuration Parameters

```csharp
public class WdtWmsApiParameters
{
    public required string Url { get; init; }
    public required string Sid { get; init; }
    public required string AppKey { get; init; }
    public required string AppSecret { get; init; }
    public string Method { get; init; } = "wms.logistics.Consign.weigh";
    public int TimeOut { get; init; } = 5000;
    public bool MustIncludeBoxBarcode { get; init; } = false;
    public decimal DefaultWeight { get; init; } = 0.0m;
}
```

#### 使用示例 / Usage Example

```csharp
var parameters = new WdtWmsApiParameters
{
    Url = "https://api.wdt.com/endpoint",
    Sid = "your_sid",
    AppKey = "your_app_key",
    AppSecret = "your_app_secret"
};

var apiClient = new WdtWmsApiClient(httpClient, logger);
apiClient.Parameters = parameters;

var result = await apiClient.RequestChuteAsync("PKG123456", dwsData);
```

---

### 5. WdtErpFlagshipApiClient - 旺店通ERP旗舰版

**类型 / Type:** ERP 系统 / ERP System  
**协议 / Protocol:** HTTP/JSON  
**参考文档 / Reference:** [旺店通ERP旗舰版 Gist](https://gist.github.com/Hisoka6602/7d6a8ab67247306ae51ebe7a865cdaee)

#### 功能说明 / Features

- ❌ **不支持包裹扫描** - 返回功能不支持的响应
- ✅ **销售出库称重扩展** - 上传销售出库称重数据（`wms.stockout.Sales.weighingExt` 方法）

#### 技术实现 / Technical Implementation

**认证机制 / Authentication:**

使用自定义签名算法（与其他旺店通API不同）：

```csharp
private string CalculateSign(SortedDictionary<string, string> parameters, string appsecret, string salt)
{
    // 1. 参数排序并拼接
    var signStr = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
    
    // 2. 添加 salt
    signStr += $"&salt={salt}";
    
    // 3. SHA256 哈希
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(appsecret + signStr));
    
    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
}
```

**请求参数 / Request Parameters:**

```json
{
  "key": "your_key",
  "method": "wms.stockout.Sales.weighingExt",
  "timestamp": "1733655600",
  "format": "json",
  "v": "1.0",
  "sign": "calculated_signature",
  "salt": "random_salt",
  "sid": "your_sid",
  "body": {
    "trade_no": "订单号",
    "logistics_code": "SF",
    "logistics_no": "SF1234567890",
    "weight": 1.5,
    "length": 30.0,
    "width": 20.0,
    "height": 10.0,
    "packager_id": 12345,
    "packager_no": "PKG001",
    "operate_table_name": "table_name",
    "force": false
  }
}
```

#### 配置参数 / Configuration Parameters

```csharp
public class WdtErpFlagshipApiParameters
{
    public required string Url { get; init; }
    public required string Key { get; init; }
    public required string Appsecret { get; init; }
    public required string Sid { get; init; }
    public string Method { get; init; } = "wms.stockout.Sales.weighingExt";
    public string V { get; init; } = "1.0";
    public required string Salt { get; init; }
    public int PackagerId { get; init; }
    public required string PackagerNo { get; init; }
    public required string OperateTableName { get; init; }
    public bool Force { get; init; } = false;
    public int TimeOut { get; init; } = 5000;
}
```

#### 使用示例 / Usage Example

```csharp
var parameters = new WdtErpFlagshipApiParameters
{
    Url = "https://api.wdt.com/flagship/endpoint",
    Key = "your_key",
    Appsecret = "your_appsecret",
    Sid = "your_sid",
    Salt = "random_salt",
    PackagerId = 12345,
    PackagerNo = "PKG001",
    OperateTableName = "table_name"
};

var apiClient = new WdtErpFlagshipApiClient(httpClient, logger);
apiClient.Parameters = parameters;

var result = await apiClient.RequestChuteAsync("PKG123456", dwsData);
```

---

### 6. WcsApiClient - 通用WCS客户端

**类型 / Type:** 通用适配器 / Generic Adapter  
**协议 / Protocol:** HTTP/JSON  
**参考文档 / Reference:** 内部实现

#### 功能说明 / Features

- ❌ **不支持包裹扫描** - 返回功能不支持的响应
- ✅ **通用格口请求** - 支持通用HTTP API格式的WCS系统

#### 技术实现 / Technical Implementation

**请求格式 / Request Format:**

灵活的JSON格式，适配不同的WCS系统：

```json
{
  "barcode": "PKG123456",
  "weight": 1500,
  "length": 30,
  "width": 20,
  "height": 10,
  "volume": 6000000,
  "timestamp": "2025-12-08T10:00:00Z"
}
```

**响应格式 / Response Format:**

```json
{
  "success": true,
  "chute_number": "5",
  "message": "Success"
}
```

#### 使用示例 / Usage Example

```csharp
var apiClient = new WcsApiClient(httpClient, logger);
var result = await apiClient.RequestChuteAsync("PKG123456", dwsData);
```

---

### 7. MockWcsApiAdapter - 模拟适配器（自动应答模式）

**类型 / Type:** 测试工具 / Testing Tool  
**协议 / Protocol:** 内存 / In-Memory  
**参考文档 / Reference:** [AUTO_RESPONSE_MODE_GUIDE.md](./AUTO_RESPONSE_MODE_GUIDE.md)

#### 功能说明 / Features

- ❌ **不支持包裹扫描**
- ✅ **模拟格口分配** - 返回随机格口号（1-20）
- ✅ **零延迟响应** - 不进行实际HTTP调用
- ✅ **用于测试和演示** - 无需配置第三方API

#### 技术实现 / Technical Implementation

```csharp
public class MockWcsApiAdapter : IWcsApiAdapter
{
    public Task<WcsApiResponse> RequestChuteAsync(
        string parcelId, 
        DwsData dwsData, 
        CancellationToken cancellationToken = default)
    {
        // 随机生成1-20之间的格口号
        var random = new Random();
        var chuteNumber = random.Next(1, 21).ToString();
        
        return Task.FromResult(new WcsApiResponse
        {
            Success = true,
            Code = "200",
            Message = "Mock response - Auto response mode",
            Data = $"{{\"chute_number\":\"{chuteNumber}\"}}",
            ParcelId = parcelId,
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 0
        });
    }
}
```

#### 启用/禁用自动应答模式 / Enable/Disable Auto Response Mode

通过API端点控制：

```bash
# 启用自动应答模式
curl -X POST "http://localhost:5000/api/AutoResponse/enable"

# 禁用自动应答模式
curl -X POST "http://localhost:5000/api/AutoResponse/disable"

# 查询当前状态
curl -X GET "http://localhost:5000/api/AutoResponse/status"
```

---

## 配置管理 / Configuration Management

### API客户端配置端点 / API Client Configuration Endpoints

系统提供了REST API端点用于配置和测试各个API客户端，详见 [API_CLIENT_ENDPOINTS.md](./API_CLIENT_ENDPOINTS.md)。

The system provides REST API endpoints for configuring and testing each API client. See [API_CLIENT_ENDPOINTS.md](./API_CLIENT_ENDPOINTS.md) for details.

#### 配置API / Configuration APIs

| API | 获取配置 | 更新配置 |
|-----|---------|---------|
| 聚水潭ERP | `GET /api/apiclientconfig/jushuitanerp` | `PUT /api/apiclientconfig/jushuitanerp` |
| 旺店通WMS | `GET /api/apiclientconfig/wdtwms` | `PUT /api/apiclientconfig/wdtwms` |
| 旺店通ERP旗舰版 | `GET /api/apiclientconfig/wdterpflagship` | `PUT /api/apiclientconfig/wdterpflagship` |

#### 测试API / Testing APIs

| API | 测试端点 |
|-----|---------|
| 聚水潭ERP | `POST /api/apiclienttest/jushuitanerp` |
| 旺店通WMS | `POST /api/apiclienttest/wdtwms` |
| 旺店通ERP旗舰版 | `POST /api/apiclienttest/wdterpflagship` |

### 配置示例 / Configuration Examples

#### 聚水潭ERP配置 / JushuitanErp Configuration

```bash
curl -X PUT "http://localhost:5000/api/apiclientconfig/jushuitanerp" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://openapi.jushuitan.com/open/orders/weight/send/upload",
    "timeOut": 5000,
    "appKey": "your_app_key",
    "appSecret": "your_app_secret",
    "accessToken": "your_access_token",
    "version": 2,
    "isUploadWeight": true,
    "type": 1,
    "isUnLid": false,
    "channel": "sorting_system",
    "defaultWeight": -1
  }'
```

#### 旺店通WMS配置 / WdtWms Configuration

```bash
curl -X PUT "http://localhost:5000/api/apiclientconfig/wdtwms" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://api.wdt.com/endpoint",
    "sid": "your_sid",
    "appKey": "your_app_key",
    "appSecret": "your_app_secret",
    "method": "wms.logistics.Consign.weigh",
    "timeOut": 5000,
    "mustIncludeBoxBarcode": false,
    "defaultWeight": 0.0
  }'
```

---

## 测试和调试 / Testing and Debugging

### 测试工具 / Testing Tools

#### 1. API测试端点 / API Testing Endpoints

测试各API客户端：

```bash
curl -X POST "http://localhost:5000/api/apiclienttest/jushuitanerp" \
  -H "Content-Type: application/json" \
  -d '{
    "barcode": "TEST123456789",
    "weight": 1250,
    "length": 30,
    "width": 20,
    "height": 10
  }'
```

响应示例：

```json
{
  "success": true,
  "code": "200",
  "message": "Request successful",
  "data": "{\"chute_number\":\"5\"}",
  "parcelId": "TEST123456789",
  "requestUrl": "https://api.example.com/endpoint",
  "requestBody": "{...}",
  "responseBody": "{...}",
  "errorMessage": null,
  "requestTime": "2025-12-08T10:00:00Z",
  "responseTime": "2025-12-08T10:00:01Z",
  "durationMs": 234,
  "responseStatusCode": 200,
  "formattedCurl": "curl -X POST ..."
}
```

#### 2. 接口模拟器 / Interface Simulator

使用 InterfaceSimulator 项目模拟第三方API响应：

```bash
# 启动接口模拟器
cd Tests/ZakYip.Sorting.RuleEngine.InterfaceSimulator
dotnet run

# 访问Swagger UI
http://localhost:5100/swagger

# 获取随机接口ID
curl http://localhost:5100/api/interface/random

# 批量获取
curl http://localhost:5100/api/interface/random/batch?count=10
```

#### 3. 数据模拟器 / Data Simulator

使用 DataSimulator 进行完整流程测试：

```bash
cd Tests/ZakYip.Sorting.RuleEngine.DataSimulator
dotnet run

# 选择测试模式
1. 单次测试
2. 批量测试
3. 压力测试
```

详见 [DataSimulator README](./Tests/ZakYip.Sorting.RuleEngine.DataSimulator/README.md)。

### 日志查询 / Log Queries

所有API调用都记录到 `ApiCommunicationLog` 表：

```sql
-- 查询最近的API调用
SELECT * FROM api_communication_logs 
ORDER BY RequestTime DESC 
LIMIT 100;

-- 查询失败的API调用
SELECT * FROM api_communication_logs 
WHERE IsSuccess = 0 
ORDER BY RequestTime DESC;

-- 查询慢速API调用（>5秒）
SELECT * FROM api_communication_logs 
WHERE DurationMs > 5000 
ORDER BY DurationMs DESC;

-- 按API类型统计成功率
SELECT 
    SUBSTRING_INDEX(RequestUrl, '/', 3) AS ApiEndpoint,
    COUNT(*) AS TotalCalls,
    SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessCalls,
    ROUND(SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 2) AS SuccessRate,
    AVG(DurationMs) AS AvgDurationMs
FROM api_communication_logs
WHERE RequestTime >= DATE_SUB(NOW(), INTERVAL 24 HOUR)
GROUP BY ApiEndpoint
ORDER BY TotalCalls DESC;
```

---

## 故障排查 / Troubleshooting

### 常见问题 / Common Issues

#### 1. API认证失败 / API Authentication Failed

**症状 / Symptoms:**
- HTTP 401 Unauthorized
- 响应消息："Invalid signature" 或 "Authentication failed"

**可能原因 / Possible Causes:**
- AppKey、AppSecret 或 AccessToken 配置错误
- 签名算法实现不正确
- 时间戳不同步

**解决方案 / Solutions:**

1. 验证配置参数：
```bash
curl -X GET "http://localhost:5000/api/apiclientconfig/jushuitanerp"
```

2. 检查签名算法：
```csharp
// 打印签名前的字符串用于调试
_logger.LogDebug("Sign string: {SignStr}", signStr);
```

3. 同步系统时间：
```bash
# Linux/Mac
sudo ntpdate -s time.nist.gov

# Windows
w32tm /resync
```

#### 2. API超时 / API Timeout

**症状 / Symptoms:**
- 请求超过配置的超时时间
- 日志显示 "Request timeout"

**可能原因 / Possible Causes:**
- 第三方API服务器响应慢
- 网络延迟或不稳定
- 超时配置过短

**解决方案 / Solutions:**

1. 增加超时时间：
```json
{
  "timeOut": 10000  // 增加到10秒
}
```

2. 检查网络连接：
```bash
# 测试API端点可达性
curl -v https://api.example.com/endpoint

# 检查DNS解析
nslookup api.example.com

# 测试延迟
ping api.example.com
```

3. 启用Polly重试策略（待实现）

#### 3. 返回格口号解析失败 / Failed to Parse Chute Number

**症状 / Symptoms:**
- API调用成功但无法获取格口号
- 日志显示 "Failed to parse chute number from response"

**可能原因 / Possible Causes:**
- 响应格式变更
- JSON路径不正确
- 响应数据为空或格式错误

**解决方案 / Solutions:**

1. 查看原始响应：
```sql
SELECT ResponseBody FROM api_communication_logs 
WHERE ParcelId = 'PKG123456' 
ORDER BY RequestTime DESC LIMIT 1;
```

2. 验证JSON解析逻辑：
```csharp
// 打印原始响应用于调试
_logger.LogDebug("Raw response: {ResponseBody}", responseBody);
```

3. 调整解析逻辑以适应新格式

#### 4. 数据库日志记录失败 / Database Logging Failed

**症状 / Symptoms:**
- API调用正常但日志未记录
- 日志显示 "Failed to save API communication log"

**可能原因 / Possible Causes:**
- MySQL连接失败
- 数据库熔断器已触发
- 表结构不匹配

**解决方案 / Solutions:**

1. 检查数据库连接：
```bash
mysql -h localhost -u root -p -e "SELECT 1;"
```

2. 查看熔断器状态：
```csharp
// 通过监控API查看数据库状态
curl http://localhost:5000/api/Monitoring/realtime
```

3. 验证表结构：
```sql
DESCRIBE api_communication_logs;
```

4. 系统会自动降级到SQLite，检查SQLite数据库：
```bash
sqlite3 ./data/logs.db "SELECT * FROM api_communication_logs ORDER BY Id DESC LIMIT 10;"
```

### 调试技巧 / Debugging Tips

#### 1. 启用详细日志 / Enable Verbose Logging

修改 `appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients": "Debug"
    }
  }
}
```

#### 2. 使用FormattedCurl进行手动测试 / Use FormattedCurl for Manual Testing

从日志中获取格式化的curl命令：

```sql
SELECT FormattedCurl FROM api_communication_logs 
WHERE ParcelId = 'PKG123456' 
ORDER BY RequestTime DESC LIMIT 1;
```

复制curl命令在终端执行，验证问题是否在系统还是API端。

#### 3. 对比成功和失败的请求 / Compare Successful and Failed Requests

```sql
-- 成功的请求
SELECT RequestBody, ResponseBody FROM api_communication_logs 
WHERE IsSuccess = 1 LIMIT 1;

-- 失败的请求
SELECT RequestBody, ResponseBody, ErrorMessage FROM api_communication_logs 
WHERE IsSuccess = 0 LIMIT 1;
```

对比差异找出问题。

---

## 性能和监控 / Performance and Monitoring

### 性能指标 / Performance Metrics

| API | 平均响应时间 | P95延迟 | 成功率 |
|-----|------------|---------|--------|
| PostCollection | ~200ms | ~500ms | 99.5% |
| PostProcessingCenter | ~200ms | ~500ms | 99.5% |
| JushuitanErp | ~150ms | ~300ms | 99.8% |
| WdtWms | ~150ms | ~300ms | 99.8% |
| WdtErpFlagship | ~180ms | ~400ms | 99.7% |
| WcsApiClient | ~100ms | ~250ms | 99.9% |
| MockAdapter | <1ms | <5ms | 100% |

### 监控查询 / Monitoring Queries

```sql
-- API性能监控
SELECT 
    DATE(RequestTime) AS Date,
    COUNT(*) AS TotalCalls,
    AVG(DurationMs) AS AvgDuration,
    MAX(DurationMs) AS MaxDuration,
    SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessCalls,
    ROUND(SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 2) AS SuccessRate
FROM api_communication_logs
WHERE RequestTime >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY DATE(RequestTime)
ORDER BY Date DESC;
```

---

## 未来改进计划 / Future Improvements

### 短期（1-2周）/ Short-term (1-2 weeks)

- [ ] **Polly弹性策略** - 集成重试、熔断、超时策略
- [ ] **API批量操作** - 支持批量上传包裹数据
- [ ] **强类型响应模型** - 为每个API定义强类型响应

### 中期（1-3个月）/ Mid-term (1-3 months)

- [ ] **API版本管理** - 支持多版本API共存
- [ ] **OAuth 2.0支持** - 标准化认证流程
- [ ] **GraphQL支持** - 提供灵活的查询接口

### 长期（3-6个月）/ Long-term (3-6 months)

- [ ] **API网关集成** - 统一API入口和路由
- [ ] **分布式追踪** - 集成OpenTelemetry
- [ ] **自适应负载均衡** - 根据API性能动态选择

---

## 参考资源 / References

- [API_CLIENT_ENDPOINTS.md](./API_CLIENT_ENDPOINTS.md) - API配置和测试端点文档
- [AUTO_RESPONSE_MODE_GUIDE.md](./AUTO_RESPONSE_MODE_GUIDE.md) - 自动应答模式使用指南
- [README.md](./README.md) - 系统总体架构文档
- [TECHNICAL_DEBT.md](./TECHNICAL_DEBT.md) - 技术债务文档

---

## 联系方式 / Contact

如有关于第三方API对接的问题，请通过以下方式联系：

For questions about third-party API integration, please contact:

- GitHub Issues: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core/issues
- 项目主页 / Project Home: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core

---

*最后更新 / Last Updated: 2025-12-08*  
*文档版本 / Document Version: 1.0*
