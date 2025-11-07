# API集成修复说明 / API Integration Fixes

本文档说明了对旺店通WMS API和聚水潭ERP API集成的修复内容。

This document describes the fixes made to the WDT WMS API and Jushuituan ERP API integrations.

## 修复日期 / Fix Date
2025-11-07

## 问题描述 / Problem Description

原有的API实现未能正确对接旺店通和聚水潭的标准API接口规范。

The original API implementations did not correctly integrate with the standard API specifications of WDT and Jushuituan.

## 修复内容 / Fixes Applied

### 1. 旺店通WMS API (WdtWmsApiClient)

#### 修复前的问题 / Issues Before Fix:
- 使用JSON格式的请求体
- 端点分散（如 `/openapi/data/upload`, `/openapi/parcel/scan` 等）
- 参数名称不符合WDT标准（使用 `appkey` 而非 `app_key`）
- 签名算法不正确

#### 修复后 / After Fix:
- ✅ 改用表单URL编码格式 (form-urlencoded)
- ✅ 统一使用路由端点 `/openapi/router`
- ✅ 添加标准WDT参数：
  - `method`: API方法名（如 `wms.weigh.upload`）
  - `app_key`: 应用密钥
  - `timestamp`: 时间戳
  - `format`: 返回格式（json）
  - `v`: API版本（1.0）
  - `body`: 业务参数的JSON字符串
  - `sign`: MD5签名
- ✅ 修正签名算法为: `md5(appsecret + key1value1key2value2... + appsecret)`

#### 更新的方法 / Updated Methods:
1. **UploadDataAsync** - 称重数据上传
   - 旧端点: `/openapi/data/upload`
   - 新端点: `/openapi/router`
   - 新方法名: `wms.weigh.upload`

2. **ScanParcelAsync** - 包裹扫描
   - 旧端点: `/openapi/parcel/scan`
   - 新端点: `/openapi/router`
   - 新方法名: `wms.parcel.scan`

3. **RequestChuteAsync** - 查询包裹/请求格口
   - 旧端点: `/openapi/parcel/query`
   - 新端点: `/openapi/router`
   - 新方法名: `wms.parcel.query`

4. **UploadImageAsync** - 图片上传
   - 旧端点: `/openapi/parcel/image`
   - 新端点: `/openapi/router`
   - 新方法名: `wms.parcel.image.upload`

### 2. 聚水潭ERP API (JushuitanErpApiClient)

#### 修复前的问题 / Issues Before Fix:
- 使用分散的端点（如 `/open/api/weigh/upload`, `/open/api/orders/query` 等）
- 未使用统一路由

#### 修复后 / After Fix:
- ✅ 统一使用路由端点 `/open/api/open/router`
- ✅ 保持正确的表单URL编码格式
- ✅ 保持正确的签名算法: `md5(partnersecret + key1value1key2value2... + partnersecret)`

#### 更新的方法 / Updated Methods:
1. **UploadDataAsync** - 称重回传
   - 旧端点: `/open/api/weigh/upload`
   - 新端点: `/open/api/open/router`
   - 方法名: `weighing.upload`

2. **ScanParcelAsync** - 订单查询
   - 旧端点: `/open/api/orders/query`
   - 新端点: `/open/api/open/router`
   - 方法名: `orders.single.query`

3. **RequestChuteAsync** - 物流更新
   - 旧端点: `/open/api/logistic/update`
   - 新端点: `/open/api/open/router`
   - 方法名: `logistic.upload`

## 技术细节 / Technical Details

### WDT签名生成 / WDT Signature Generation
```csharp
// 1. 按字典序排序参数（排除sign）
// 2. 拼接: appsecret + key1value1key2value2... + appsecret
// 3. MD5哈希并转小写
string signString = $"{_appSecret}{string.Join("", sortedParams)}{_appSecret}";
string sign = MD5(signString).ToLower();
```

### JST签名生成 / JST Signature Generation
```csharp
// 1. 按字典序排序参数（排除sign）
// 2. 拼接: partnersecret + key1value1key2value2... + partnersecret
// 3. MD5哈希并转小写
string signString = $"{_partnerSecret}{string.Join("", sortedParams)}{_partnerSecret}";
string sign = MD5(signString).ToLower();
```

## 兼容性 / Compatibility

- ✅ 向后兼容原有接口签名
- ✅ 保持IWcsApiAdapter接口不变
- ✅ 现有调用代码无需修改

## 测试建议 / Testing Recommendations

1. 测试WDT API各个方法：
   - 称重数据上传
   - 包裹扫描
   - 格口查询
   - 图片上传

2. 测试JST API各个方法：
   - 称重回传
   - 订单查询
   - 物流更新

3. 验证签名生成正确性
4. 验证请求参数格式
5. 验证响应处理逻辑

## 配置要求 / Configuration Requirements

### WDT WMS API
```json
{
  "WdtWmsApi": {
    "BaseUrl": "https://api.wdt.com",
    "AppKey": "your_app_key",
    "AppSecret": "your_app_secret"
  }
}
```

### Jushuituan ERP API
```json
{
  "JushuitanErpApi": {
    "BaseUrl": "https://api.jushuitan.com",
    "PartnerKey": "your_partner_key",
    "PartnerSecret": "your_partner_secret",
    "Token": "your_token"
  }
}
```

## 参考文档 / References

- 旺店通开放平台文档
- 聚水潭开放平台文档
- THIRD_PARTY_API_INTEGRATION.md
- THIRD_PARTY_API_ADAPTER_ARCHITECTURE.md

## 注意事项 / Notes

1. 确保AppKey/AppSecret配置正确
2. 确保PartnerKey/PartnerSecret/Token配置正确
3. 注意API调用频率限制
4. 生产环境使用前请先在测试环境验证
5. 关注第三方API文档更新

## 版本历史 / Version History

### v1.15.1 (2025-11-07)
- ✅ 修复WDT WMS API集成
- ✅ 修复聚水潭ERP API集成
- ✅ 统一API路由端点
- ✅ 修正签名算法
- ✅ 更新请求参数格式
