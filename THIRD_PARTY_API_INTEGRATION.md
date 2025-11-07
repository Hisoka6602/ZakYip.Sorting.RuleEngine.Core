# 第三方API集成文档 / Third-Party API Integration Documentation

## 概述 / Overview

本项目集成了两个第三方API客户端，用于与外部系统进行数据交互：
- **旺店通WMS API** (WDT WMS API) - 仓库管理系统
- **聚水潭ERP API** (Jushuituan ERP API) - 企业资源规划系统

This project integrates two third-party API clients for data exchange with external systems:
- **WDT WMS API** - Warehouse Management System
- **Jushuituan ERP API** - Enterprise Resource Planning System

## 配置 / Configuration

### appsettings.json

在 `appsettings.json` 中添加以下配置：

```json
{
  "AppSettings": {
    "WdtWmsApi": {
      "BaseUrl": "https://api.wdt.com",
      "AppKey": "your_app_key",
      "AppSecret": "your_app_secret",
      "TimeoutSeconds": 30,
      "Enabled": true
    },
    "JushuitanErpApi": {
      "BaseUrl": "https://api.jushuitan.com",
      "PartnerKey": "your_partner_key",
      "PartnerSecret": "your_partner_secret",
      "Token": "your_access_token",
      "TimeoutSeconds": 30,
      "Enabled": true
    }
  }
}
```

### 环境变量 / Environment Variables

也可以通过环境变量配置（推荐用于生产环境）：

```bash
# 旺店通WMS API
export AppSettings__WdtWmsApi__BaseUrl="https://api.wdt.com"
export AppSettings__WdtWmsApi__AppKey="your_app_key"
export AppSettings__WdtWmsApi__AppSecret="your_app_secret"
export AppSettings__WdtWmsApi__Enabled="true"

# 聚水潭ERP API
export AppSettings__JushuitanErpApi__BaseUrl="https://api.jushuitan.com"
export AppSettings__JushuitanErpApi__PartnerKey="your_partner_key"
export AppSettings__JushuitanErpApi__PartnerSecret="your_partner_secret"
export AppSettings__JushuitanErpApi__Token="your_access_token"
export AppSettings__JushuitanErpApi__Enabled="true"
```

## 旺店通WMS API / WDT WMS API

### API端点 / API Endpoint

所有API请求统一使用路由端点：
```
POST /openapi/router
```

All API requests use the unified router endpoint.

### 功能 / Features

#### 1. 称重扫描 / Weight Scanning
```csharp
var result = await wdtWmsApiClient.UploadDataAsync(
    parcelInfo: parcelInfo,
    dwsData: dwsData
);
```

**请求参数 / Request Parameters:**
- method: `wms.weigh.upload`
- app_key: 应用密钥
- timestamp: Unix时间戳
- format: `json`
- v: `1.0`
- body: JSON格式的业务数据
- sign: MD5签名

#### 2. 查询包裹信息 / Query Parcel Information
```csharp
var result = await wdtWmsApiClient.RequestChuteAsync("SF123456789");
```

**请求参数 / Request Parameters:**
- method: `wms.parcel.query`

#### 3. 扫描包裹 / Scan Parcel
```csharp
var result = await wdtWmsApiClient.ScanParcelAsync("SF123456789");
```

**请求参数 / Request Parameters:**
- method: `wms.parcel.scan`

#### 4. 上传包裹图片 / Upload Parcel Image
```csharp
byte[] imageData = File.ReadAllBytes("parcel_image.jpg");
var result = await wdtWmsApiClient.UploadImageAsync(
    barcode: "SF123456789",
    imageData: imageData
);
```

**请求参数 / Request Parameters:**
- method: `wms.parcel.image.upload`

### 认证方式 / Authentication

旺店通API使用MD5签名认证。签名生成规则：
1. 按字典序排序请求参数（排除sign字段）
2. 拼接格式：AppSecret + key1value1key2value2... + AppSecret
3. 对拼接后的字符串进行MD5哈希
4. 将哈希值转换为小写十六进制字符串

WDT API uses MD5 signature authentication. Signature generation:
1. Sort request parameters by key (excluding sign field)
2. Concatenate: AppSecret + key1value1key2value2... + AppSecret
3. Compute MD5 hash of the concatenated string
4. Convert hash to lowercase hexadecimal string

### 请求格式 / Request Format

使用 `application/x-www-form-urlencoded` 格式
Uses form-urlencoded format

### 标准参数 / Standard Parameters

所有请求必须包含以下参数：
All requests must include:
- `method`: API方法名 / API method name
- `app_key`: 应用密钥 / Application key
- `timestamp`: Unix时间戳 / Unix timestamp
- `format`: 返回格式，固定为 `json`
- `v`: API版本，固定为 `1.0`
- `body`: 业务参数JSON / Business parameters in JSON
- `sign`: MD5签名 / MD5 signature

## 聚水潭ERP API / Jushuituan ERP API

### API端点 / API Endpoint

所有API请求统一使用路由端点：
```
POST /open/api/open/router
```

All API requests use the unified router endpoint.

### 功能 / Features

#### 1. 称重回传 / Weight Data Callback
```csharp
var result = await jushuitanErpApiClient.UploadDataAsync(
    parcelInfo: parcelInfo,
    dwsData: dwsData
);
```

**请求参数 / Request Parameters:**
- method: `weighing.upload`
- partnerkey: 合作伙伴密钥
- token: 访问令牌
- ts: Unix时间戳
- charset: `utf-8`
- biz_content: JSON格式的业务数据
- sign: MD5签名

#### 2. 查询订单信息 / Query Order Information
```csharp
var result = await jushuitanErpApiClient.ScanParcelAsync("JD123456789");
```

**请求参数 / Request Parameters:**
- method: `orders.single.query`

#### 3. 更新物流信息 / Update Logistics Information
```csharp
var result = await jushuitanErpApiClient.RequestChuteAsync("JD123456789");
```

**请求参数 / Request Parameters:**
- method: `logistic.upload`

### 认证方式 / Authentication

聚水潭API使用MD5签名认证。签名生成规则：
1. 按字典序排序请求参数（排除sign字段）
2. 拼接格式：PartnerSecret + key1value1key2value2... + PartnerSecret
3. 对拼接后的字符串进行MD5哈希
4. 将哈希值转换为小写十六进制字符串

Jushuituan API uses MD5 signature authentication. Signature generation:
1. Sort request parameters by key (excluding sign field)
2. Concatenate: PartnerSecret + key1value1key2value2... + PartnerSecret
3. Compute MD5 hash of the concatenated string
4. Convert hash to lowercase hexadecimal string

### 请求格式 / Request Format

使用 `application/x-www-form-urlencoded` 格式
Uses form-urlencoded format

### 标准参数 / Standard Parameters

所有请求必须包含以下参数：
All requests must include:
- `method`: API方法名 / API method name
- `partnerkey`: 合作伙伴密钥 / Partner key
- `token`: 访问令牌 / Access token
- `ts`: Unix时间戳 / Unix timestamp
- `charset`: 字符集，固定为 `utf-8`
- `biz_content`: 业务参数JSON / Business parameters in JSON
- `sign`: MD5签名 / MD5 signature

## 依赖注入 / Dependency Injection

API客户端已在 `Program.cs` 中自动注册。使用时通过构造函数注入：

```csharp
public class MyService
{
    private readonly IWdtWmsApiClient _wdtWmsApiClient;
    private readonly IJushuitanErpApiClient _jushuitanErpApiClient;

    public MyService(
        IWdtWmsApiClient wdtWmsApiClient,
        IJushuitanErpApiClient jushuitanErpApiClient)
    {
        _wdtWmsApiClient = wdtWmsApiClient;
        _jushuitanErpApiClient = jushuitanErpApiClient;
    }

    public async Task ProcessParcelAsync(string barcode, decimal weight)
    {
        // 使用旺店通API
        var wdtResult = await _wdtWmsApiClient.WeighScanAsync(
            barcode, weight, 30m, 20m, 10m);
            
        // 使用聚水潭API
        var jstResult = await _jushuitanErpApiClient.WeightCallbackAsync(
            barcode, weight, 30m, 20m, 10m);
    }
}
```

## 响应格式 / Response Format

所有API方法返回 `ThirdPartyResponse` 对象：

```csharp
public class ThirdPartyResponse
{
    public bool Success { get; set; }           // 是否成功
    public string Code { get; set; }            // 响应代码
    public string Message { get; set; }         // 响应消息
    public string? Data { get; set; }           // 响应数据(JSON)
    public OcrData? OcrData { get; set; }       // OCR数据(可选)
    public DateTime ResponseTime { get; set; }  // 响应时间
}
```

## 错误处理 / Error Handling

API客户端实现了完整的错误处理机制：

```csharp
try
{
    var result = await wdtWmsApiClient.WeighScanAsync(barcode, weight, l, w, h);
    
    if (result.Success)
    {
        // 处理成功响应
        _logger.LogInformation("API调用成功: {Message}", result.Message);
    }
    else
    {
        // 处理业务错误
        _logger.LogWarning("API调用失败: {Code} - {Message}", 
            result.Code, result.Message);
    }
}
catch (Exception ex)
{
    // 处理异常
    _logger.LogError(ex, "API调用异常");
}
```

## 日志记录 / Logging

API客户端使用ILogger进行日志记录，包含：
- Debug级别：请求详情（条码、参数等）
- Information级别：成功响应
- Warning级别：业务错误
- Error级别：异常情况

API clients use ILogger for logging, including:
- Debug: Request details (barcode, parameters, etc.)
- Information: Successful responses
- Warning: Business errors
- Error: Exceptions

## 测试 / Testing

运行单元测试：

```bash
# 运行所有API客户端测试
dotnet test --filter "FullyQualifiedName~WdtWmsApiClientTests|FullyQualifiedName~JushuitanErpApiClientTests"

# 只运行旺店通WMS API测试
dotnet test --filter "FullyQualifiedName~WdtWmsApiClientTests"

# 只运行聚水潭ERP API测试
dotnet test --filter "FullyQualifiedName~JushuitanErpApiClientTests"
```

## 注意事项 / Notes

1. **安全性**: 请勿在代码中硬编码API密钥，使用配置文件或环境变量
2. **超时设置**: 根据实际网络情况调整TimeoutSeconds参数
3. **启用控制**: 通过Enabled参数控制API是否启用，便于测试和生产环境切换
4. **并发限制**: 注意第三方API的并发调用限制，避免触发限流
5. **数据格式**: 重量单位为kg，尺寸单位为cm

1. **Security**: Do not hardcode API keys in code, use configuration files or environment variables
2. **Timeout**: Adjust TimeoutSeconds parameter based on actual network conditions
3. **Enable Control**: Use Enabled parameter to control API activation for testing/production switching
4. **Concurrency**: Be aware of third-party API concurrency limits to avoid rate limiting
5. **Data Format**: Weight in kg, dimensions in cm

## 更新日志 / Changelog

### v1.15.1 (2025-11-07)
- ✅ 修复旺店通WMS API集成
  - 改用统一路由端点 `/openapi/router`
  - 改用表单URL编码格式
  - 修正参数名称（`app_key` 代替 `appkey`）
  - 添加标准参数 `method`, `format`, `v`, `body`
  - 修正签名算法
- ✅ 修复聚水潭ERP API集成
  - 改用统一路由端点 `/open/api/open/router`
  - 保持表单URL编码格式
  - 统一所有API调用使用路由模式
- ✅ 添加API集成修复文档 `API_INTEGRATION_FIXES.md`
- ✅ 更新第三方API集成文档

### v1.15.0 (2025-11-07)
- ✅ 新增旺店通WMS API集成
- ✅ 新增聚水潭ERP API集成
- ✅ 实现MD5签名认证机制
- ✅ 添加18个单元测试用例
- ✅ 支持配置文件和环境变量配置
- ✅ 完整的日志记录和错误处理

### v1.15.1 (2025-11-07)
- ✅ Fixed WDT WMS API integration
  - Changed to unified router endpoint `/openapi/router`
  - Changed to form-urlencoded format
  - Fixed parameter names (`app_key` instead of `appkey`)
  - Added standard parameters `method`, `format`, `v`, `body`
  - Fixed signature algorithm
- ✅ Fixed Jushuituan ERP API integration
  - Changed to unified router endpoint `/open/api/open/router`
  - Maintained form-urlencoded format
  - Unified all API calls to use router pattern
- ✅ Added API integration fixes documentation `API_INTEGRATION_FIXES.md`
- ✅ Updated third-party API integration documentation

### v1.15.0 (2025-11-07)
- ✅ Added WDT WMS API integration
- ✅ Added Jushuituan ERP API integration
- ✅ Implemented MD5 signature authentication
- ✅ Added 18 unit test cases
- ✅ Support for configuration files and environment variables
- ✅ Complete logging and error handling
