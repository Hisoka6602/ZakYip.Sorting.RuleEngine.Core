# DWS 配置更新指南 / DWS Configuration Update Guide

## ⚠️ 重要变更 / Important Changes

**ParcelId 和 Barcode 现在是两个独立的字段！**
**ParcelId and Barcode are now two separate fields!**

---

## 问题背景 / Background

### 之前的错误实现 / Previous Wrong Implementation

```csharp
// ❌ 错误：用 Barcode 替代 ParcelId
ParcelId = dwsData.Barcode ?? "UNKNOWN"
```

**问题 / Problem:**
- ParcelId（包裹ID）和 Barcode（条码）是**完全不同的概念**
- ParcelId = 包裹的唯一标识（通常是时间戳或序列号）
- Barcode = 条码/快递单号（业务信息）
- **不能用 Barcode 替代 ParcelId**

### 现在的正确实现 / Current Correct Implementation

```csharp
// ✅ 正确：使用真正的 ParcelId
ParcelId = dwsData.ParcelId
```

---

## 必须的配置更新 / Required Configuration Update

### 步骤 1: 更新 DWS 数据模板 / Step 1: Update DWS Data Template

#### 旧模板格式 / Old Template Format
```
{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
```

**示例数据 / Example Data:**
```
9443000712227,0.000,0,0,0,0,1766474478322
```

#### 新模板格式 / New Template Format
```
{ParcelId},{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
```

**示例数据 / Example Data:**
```
1766474478500,9443000712227,0.000,0,0,0,0,1766474478322
```

**字段说明 / Field Description:**
- `ParcelId` (新增 / NEW) - 包裹唯一标识 / Parcel unique identifier
- `Code` - 条码/快递单号 / Barcode/Tracking number
- `Weight` - 重量（克）/ Weight (grams)
- `Length` - 长度（毫米）/ Length (mm)
- `Width` - 宽度（毫米）/ Width (mm)
- `Height` - 高度（毫米）/ Height (mm)
- `Volume` - 体积（立方厘米）/ Volume (cm³)
- `Timestamp` - 时间戳 / Timestamp

---

## 如何更新配置 / How to Update Configuration

### 方法 1: 通过 API 更新 / Method 1: Update via API

#### 1.1 获取当前配置 / Get Current Configuration

```bash
curl http://localhost:5009/api/DwsDataTemplate
```

**响应示例 / Response Example:**
```json
{
  "templateId": 1,
  "name": "默认DWS模板",
  "template": "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
  "delimiter": ",",
  "isJsonFormat": false,
  "isEnabled": true
}
```

#### 1.2 更新模板 / Update Template

```bash
curl -X PUT http://localhost:5009/api/DwsDataTemplate \
  -H "Content-Type: application/json" \
  -d '{
    "name": "默认DWS模板（含ParcelId）",
    "template": "{ParcelId},{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
    "delimiter": ",",
    "isJsonFormat": false,
    "isEnabled": true,
    "description": "新增ParcelId字段，用于包裹唯一标识"
  }'
```

**✅ 成功响应 / Success Response:**
```json
{
  "success": true,
  "message": "DWS数据模板更新成功",
  "data": {
    "templateId": 1,
    "template": "{ParcelId},{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}"
  }
}
```

### 方法 2: 通过 Swagger UI 更新 / Method 2: Update via Swagger UI

1. 访问 Swagger UI: `http://localhost:5009/swagger`
2. 找到 `/api/DwsDataTemplate` 端点
3. 点击 **PUT** 方法
4. 点击 **Try it out**
5. 填入以下JSON:
   ```json
   {
     "name": "默认DWS模板（含ParcelId）",
     "template": "{ParcelId},{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
     "delimiter": ",",
     "isJsonFormat": false,
     "isEnabled": true,
     "description": "新增ParcelId字段"
   }
   ```
6. 点击 **Execute**

### 方法 3: 直接修改 LiteDB 数据库 / Method 3: Modify LiteDB Database Directly

⚠️ **不推荐 / Not Recommended** - 可能导致数据不一致

如果必须直接修改:
1. 停止应用程序
2. 使用 LiteDB Studio 打开 `data/config.db`
3. 找到 `dws_data_templates` 集合
4. 编辑模板字段
5. 重启应用程序

---

## DWS 设备数据格式要求 / DWS Device Data Format Requirements

### 新的数据格式 / New Data Format

**您的 DWS 设备必须发送以下格式的数据：**
**Your DWS device must send data in the following format:**

```
<ParcelId>,<Barcode>,<Weight>,<Length>,<Width>,<Height>,<Volume>,<Timestamp>
```

**示例 / Example:**
```
1766474478500,9443000712227,0.000,0,0,0,0,1766474478322
```

### 字段解释 / Field Explanation

| 字段 Field | 说明 Description | 示例 Example |
|-----------|-----------------|--------------|
| **ParcelId** | 包裹唯一标识（时间戳/序列号）<br/>Parcel unique ID (timestamp/sequence) | `1766474478500` |
| **Barcode** | 条码/快递单号<br/>Barcode/Tracking number | `9443000712227` |
| **Weight** | 重量（克）<br/>Weight (grams) | `0.000` |
| **Length** | 长度（毫米）<br/>Length (mm) | `0` |
| **Width** | 宽度（毫米）<br/>Width (mm) | `0` |
| **Height** | 高度（毫米）<br/>Height (mm) | `0` |
| **Volume** | 体积（立方厘米）<br/>Volume (cm³) | `0` |
| **Timestamp** | 时间戳（毫秒）<br/>Timestamp (ms) | `1766474478322` |

### ParcelId 的生成规则 / ParcelId Generation Rules

**推荐方式 / Recommended Approach:**
使用毫秒级时间戳作为 ParcelId，确保唯一性：
Use millisecond timestamp as ParcelId to ensure uniqueness:

```python
# Python 示例 / Python Example
import time
parcel_id = int(time.time() * 1000)  # 毫秒时间戳 / Millisecond timestamp
print(parcel_id)  # 输出: 1766474478500
```

```csharp
// C# 示例 / C# Example
long parcelId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
Console.WriteLine(parcelId);  // 输出: 1766474478500
```

**替代方式 / Alternative Approach:**
使用递增序列号（需要确保唯一性）:
Use incremental sequence number (must ensure uniqueness):
```
序列号: 1, 2, 3, 4, ...
Sequence: 1, 2, 3, 4, ...
```

---

## 验证配置更新 / Verify Configuration Update

### 步骤 1: 检查模板配置 / Step 1: Check Template Configuration

```bash
curl http://localhost:5009/api/DwsDataTemplate
```

**预期输出 / Expected Output:**
```json
{
  "template": "{ParcelId},{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}"
}
```

### 步骤 2: 发送测试数据 / Step 2: Send Test Data

**测试数据 / Test Data:**
```
1766474478500,9443000712227,500.5,300,200,100,6000,1766474478322
```

**预期日志 / Expected Logs:**
```
[INFO] ✅ DWS数据解析成功 | ParcelId=1766474478500, Barcode=9443000712227, Weight=500.5g
[INFO] 📢 已发布DwsDataReceivedEvent事件 | ParcelId=1766474478500, Barcode=9443000712227
```

### 步骤 3: 检查数据库记录 / Step 3: Check Database Records

```sql
-- MySQL
SELECT * FROM parcel_infos ORDER BY CreatedAt DESC LIMIT 1;

-- 预期结果 / Expected Result:
-- ParcelId: 1766474478500
-- Barcode: 9443000712227
-- Weight: 500.5
```

---

## 常见问题 / FAQ

### Q1: 如果我的 DWS 设备不支持发送 ParcelId 怎么办？

**A:** 有两种解决方案 / Two solutions:

**方案 1: 使用中间件生成 ParcelId**
在 DWS 设备和规则引擎之间添加一个中间件，自动为每条数据生成 ParcelId：
```
DWS设备 → 中间件(添加ParcelId) → 规则引擎
DWS Device → Middleware(Add ParcelId) → Rule Engine
```

**方案 2: 修改 DWS 固件**
更新 DWS 设备固件，使其在发送数据时包含 ParcelId 字段。

### Q2: ParcelId 和 Barcode 可以相同吗？

**A:** 技术上可以，但**强烈不推荐** / Technically yes, but **strongly discouraged**

- ParcelId 应该是系统内部的唯一标识
- Barcode 是业务层面的标识（如快递单号）
- 保持两者独立可以提供更好的灵活性和可追溯性

### Q3: 旧数据会受影响吗？

**A:** 不会 / No

- 旧数据已经存储在数据库中
- 新配置只影响新接收的 DWS 数据
- 建议在低峰期更新配置

### Q4: 如何回滚到旧配置？

**A:** 通过 API 更新模板 / Update template via API

```bash
curl -X PUT http://localhost:5009/api/DwsDataTemplate \
  -H "Content-Type: application/json" \
  -d '{
    "template": "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
    ...
  }'
```

⚠️ **注意**：回滚后，系统会继续使用 Barcode 作为 ParcelId（错误的逻辑）

---

## 迁移检查清单 / Migration Checklist

- [ ] 已了解 ParcelId 和 Barcode 的区别
- [ ] 已更新 DWS 数据模板配置
- [ ] DWS 设备已配置为发送 ParcelId 字段
- [ ] 已发送测试数据验证配置
- [ ] 已检查应用程序日志
- [ ] 已验证数据库中的记录
- [ ] 已通知团队成员配置变更

---

## 技术支持 / Technical Support

如果遇到问题，请提供以下信息:
If you encounter issues, please provide:

1. DWS 设备型号和固件版本 / DWS device model and firmware version
2. 实际发送的数据格式 / Actual data format being sent
3. 应用程序日志（最近100行）/ Application logs (last 100 lines)
4. 当前的 DWS 模板配置 / Current DWS template configuration

---

**最后更新 / Last Updated**: 2025-12-23  
**变更原因 / Change Reason**: 修复严重的业务逻辑错误 - ParcelId 不能用 Barcode 替代  
**影响范围 / Impact Scope**: 所有使用 DWS 设备的部署环境  
**紧急程度 / Urgency**: 🔴 高 High - 影响数据准确性和业务逻辑
