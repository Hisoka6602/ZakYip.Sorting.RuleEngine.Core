# ApiClientTest 方法选择功能指南 / ApiClientTest Method Selection Guide

## 概述 / Overview

ApiClientTest 端点现在支持动态选择要测试的 WCS API 方法。可以测试 `IWcsApiAdapter` 接口中定义的以下方法（不包括 `UploadImageAsync`）：

The ApiClientTest endpoint now supports dynamically selecting which WCS API method to test. You can test the following methods defined in the `IWcsApiAdapter` interface (excluding `UploadImageAsync`):

## 可用的测试方法 / Available Test Methods

### 1. ScanParcel (扫描包裹)
- **枚举值 / Enum Value**: `WcsApiMethod.ScanParcel` (1)
- **对应接口方法 / Interface Method**: `ScanParcelAsync(string barcode, CancellationToken)`
- **用途 / Purpose**: 扫描包裹并在 WCS 系统中注册
- **必需参数 / Required Parameters**:
  - `Barcode`: 包裹条码

### 2. RequestChute (请求格口) - 默认 / Default
- **枚举值 / Enum Value**: `WcsApiMethod.RequestChute` (2)
- **对应接口方法 / Interface Method**: `RequestChuteAsync(string parcelId, DwsData dwsData, OcrData?, CancellationToken)`
- **用途 / Purpose**: 请求分配格口号码
- **必需参数 / Required Parameters**:
  - `Barcode`: 包裹条码
  - `Weight`: 重量（克）
  - `Length`, `Width`, `Height`: 尺寸（厘米，可选）

### 3. NotifyChuteLanding (落格回调)
- **枚举值 / Enum Value**: `WcsApiMethod.NotifyChuteLanding` (3)
- **对应接口方法 / Interface Method**: `NotifyChuteLandingAsync(string parcelId, string chuteId, string barcode, CancellationToken)`
- **用途 / Purpose**: 通知 WCS 系统包裹已经落入指定格口
- **必需参数 / Required Parameters**:
  - `Barcode`: 包裹条码
  - `ParcelId`: 包裹ID（可选，默认使用 Barcode）
  - `ChuteId`: 格口ID（可选，默认使用 "DEFAULT_CHUTE"）

## API 请求示例 / API Request Examples

### 示例 1: 测试 ScanParcel 方法

```json
POST /api/ApiClientTest/jushuitanerp
Content-Type: application/json

{
  "barcode": "TEST123456789",
  "weight": 500,
  "methodName": 1
}
```

### 示例 2: 测试 RequestChute 方法（默认）

```json
POST /api/ApiClientTest/wdtwms
Content-Type: application/json

{
  "barcode": "TEST123456789",
  "weight": 500,
  "length": 30,
  "width": 20,
  "height": 15
}
```

或明确指定方法：

```json
{
  "barcode": "TEST123456789",
  "weight": 500,
  "length": 30,
  "width": 20,
  "height": 15,
  "methodName": 2
}
```

### 示例 3: 测试 NotifyChuteLanding 方法

```json
POST /api/ApiClientTest/wdterpflagship
Content-Type: application/json

{
  "barcode": "TEST123456789",
  "weight": 500,
  "parcelId": "PARCEL001",
  "chuteId": "CHUTE_A01",
  "methodName": 3
}
```

## 向后兼容性 / Backward Compatibility

如果未指定 `methodName` 字段，系统将默认使用 `RequestChute` 方法（值为 2），以确保与现有客户端的兼容性。

If the `methodName` field is not specified, the system will default to the `RequestChute` method (value 2) to ensure compatibility with existing clients.

## 注意事项 / Notes

1. **UploadImageAsync 方法不可测试** - 此方法需要图片数据，不适合通过简单的 JSON 请求进行测试
2. **不同方法需要不同参数** - 请根据要测试的方法提供相应的参数
3. **所有方法都会返回 WcsApiResponse** - 响应格式统一为 `ApiClientTestResponse`

## 技术实现 / Technical Implementation

- **枚举定义**: `ZakYip.Sorting.RuleEngine.Domain.Enums.WcsApiMethod`
- **DTO 更新**: `ZakYip.Sorting.RuleEngine.Application.DTOs.Requests.ApiClientTestRequest`
- **控制器逻辑**: `ZakYip.Sorting.RuleEngine.Service.API.ApiClientTestController`

## 支持的客户端 / Supported Clients

所有实现了 `IWcsApiAdapter` 接口的 API 客户端都支持方法选择功能：
- JushuitanErp API Client
- WdtWms API Client
- WdtErpFlagship API Client
- PostCollection API Client
- PostProcessingCenter API Client

---

**最后更新 / Last Updated**: 2025-12-20
