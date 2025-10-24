# API快速参考 / API Quick Reference

## ZakYip Sorting Rule Engine - Event-Driven API

---

## 分拣机信号API / Sorting Machine Signal API

### 1. 创建包裹处理空间 / Create Parcel Space

```http
POST /api/sortingmachine/create-parcel
Content-Type: application/json

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

**说明 / Description:**
- 分拣程序推送包裹ID和小车号
- 系统创建处理空间并等待DWS数据
- 包裹进入FIFO队列，保证顺序处理

---

### 2. 接收DWS数据 / Receive DWS Data

```http
POST /api/sortingmachine/receive-dws
Content-Type: application/json

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

**说明 / Description:**
- 分拣程序上传DWS测量数据
- 触发自动处理流程：
  1. 上传数据到第三方API
  2. 执行规则匹配
  3. 确定格口号
  4. 发送结果给分拣程序
  5. 清理缓存空间

---

## 规则管理API / Rule Management API

### 3. 获取所有规则 / Get All Rules

```http
GET /api/rule
```

**响应 / Response:**
```json
[
  {
    "ruleId": "RULE001",
    "ruleName": "重量规则",
    "priority": 1,
    "conditionExpression": "Weight > 1000",
    "targetChute": "CHUTE-A01",
    "isEnabled": true
  }
]
```

---

### 4. 添加规则 / Add Rule

```http
POST /api/rule
Content-Type: application/json

{
  "ruleId": "RULE001",
  "ruleName": "重量规则",
  "description": "重量大于1000克分配到A区",
  "priority": 1,
  "conditionExpression": "Weight > 1000",
  "targetChute": "CHUTE-A01",
  "isEnabled": true
}
```

---

### 5. 更新规则 / Update Rule

```http
PUT /api/rule/RULE001
Content-Type: application/json

{
  "ruleId": "RULE001",
  "ruleName": "重量规则（已更新）",
  ...
}
```

---

### 6. 删除规则 / Delete Rule

```http
DELETE /api/rule/RULE001
```

---

## 健康检查API / Health Check API

### 7. 健康检查 / Health Check

```http
GET /health
```

**响应 / Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-10-24T07:06:42.407Z"
}
```

---

### 8. 版本信息 / Version Info

```http
GET /version
```

**响应 / Response:**
```json
{
  "version": "1.0.0",
  "name": "ZakYip.Sorting.RuleEngine.Core",
  "description": "分拣规则引擎核心系统"
}
```

---

## 完整工作流程示例 / Complete Workflow Example

### Bash脚本 / Bash Script

```bash
#!/bin/bash

BASE_URL="http://localhost:5000"

# 1. 创建包裹处理空间
echo "Step 1: Creating parcel space..."
curl -X POST "${BASE_URL}/api/sortingmachine/create-parcel" \
  -H "Content-Type: application/json" \
  -d '{
    "parcelId": "PKG001",
    "cartNumber": "CART001",
    "barcode": "1234567890"
  }'

echo -e "\n\n"

# 2. 等待DWS测量（模拟延迟）
echo "Step 2: Waiting for DWS measurement..."
sleep 2

# 3. 上传DWS数据
echo "Step 3: Uploading DWS data..."
curl -X POST "${BASE_URL}/api/sortingmachine/receive-dws" \
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

echo -e "\n\n"
echo "Workflow completed!"
```

### PowerShell脚本 / PowerShell Script

```powershell
$baseUrl = "http://localhost:5000"

# 1. 创建包裹处理空间
Write-Host "Step 1: Creating parcel space..." -ForegroundColor Green
$createParcel = @{
    parcelId = "PKG001"
    cartNumber = "CART001"
    barcode = "1234567890"
} | ConvertTo-Json

Invoke-RestMethod -Uri "$baseUrl/api/sortingmachine/create-parcel" `
    -Method Post `
    -ContentType "application/json" `
    -Body $createParcel

# 2. 等待DWS测量
Write-Host "`nStep 2: Waiting for DWS measurement..." -ForegroundColor Green
Start-Sleep -Seconds 2

# 3. 上传DWS数据
Write-Host "`nStep 3: Uploading DWS data..." -ForegroundColor Green
$dwsData = @{
    parcelId = "PKG001"
    barcode = "1234567890"
    weight = 1500
    length = 300
    width = 200
    height = 150
    volume = 9000
} | ConvertTo-Json

Invoke-RestMethod -Uri "$baseUrl/api/sortingmachine/receive-dws" `
    -Method Post `
    -ContentType "application/json" `
    -Body $dwsData

Write-Host "`nWorkflow completed!" -ForegroundColor Green
```

---

## C# 客户端示例 / C# Client Example

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class SortingMachineClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000";

    public SortingMachineClient()
    {
        _httpClient = new HttpClient();
    }

    public async Task<bool> ProcessParcelAsync(string parcelId, string cartNumber, DwsData dwsData)
    {
        // Step 1: Create parcel space
        var createRequest = new
        {
            parcelId,
            cartNumber,
            barcode = dwsData.Barcode
        };

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        
        var createResponse = await _httpClient.PostAsync(
            $"{BaseUrl}/api/sortingmachine/create-parcel",
            createContent);

        if (!createResponse.IsSuccessStatusCode)
            return false;

        // Step 2: Send DWS data
        await Task.Delay(100); // Simulate DWS measurement time

        var dwsRequest = new
        {
            parcelId,
            barcode = dwsData.Barcode,
            weight = dwsData.Weight,
            length = dwsData.Length,
            width = dwsData.Width,
            height = dwsData.Height,
            volume = dwsData.Volume
        };

        var dwsJson = JsonSerializer.Serialize(dwsRequest);
        var dwsContent = new StringContent(dwsJson, Encoding.UTF8, "application/json");
        
        var dwsResponse = await _httpClient.PostAsync(
            $"{BaseUrl}/api/sortingmachine/receive-dws",
            dwsContent);

        return dwsResponse.IsSuccessStatusCode;
    }
}

public class DwsData
{
    public string Barcode { get; set; }
    public decimal Weight { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Volume { get; set; }
}
```

---

## 错误处理 / Error Handling

### 常见错误码 / Common Error Codes

| 状态码 / Status Code | 说明 / Description | 解决方案 / Solution |
|---------------------|-------------------|-------------------|
| 200 OK | 成功 / Success | - |
| 400 Bad Request | 请求参数错误 / Invalid request | 检查请求体格式 |
| 404 Not Found | 包裹不存在 / Parcel not found | 确认包裹已创建 |
| 500 Internal Server Error | 服务器错误 / Server error | 查看日志 |

### 错误响应示例 / Error Response Example

```json
{
  "success": false,
  "parcelId": "PKG001",
  "message": "包裹ID已存在或创建失败"
}
```

---

## 性能建议 / Performance Recommendations

1. **批量处理 / Batch Processing**
   - 避免频繁的单个请求
   - 使用异步并发处理

2. **重试策略 / Retry Strategy**
   - 网络错误时实施指数退避重试
   - 最多重试3次

3. **超时设置 / Timeout Settings**
   - 连接超时：5秒
   - 请求超时：30秒

4. **连接池 / Connection Pooling**
   - 复用HttpClient实例
   - 避免为每个请求创建新连接

---

## 监控指标 / Monitoring Metrics

建议监控的关键指标：

1. **API响应时间 / API Response Time**
   - create-parcel: < 50ms
   - receive-dws: < 100ms

2. **队列长度 / Queue Length**
   - 正常: < 100
   - 告警: > 500

3. **处理成功率 / Success Rate**
   - 目标: > 99.5%

4. **数据库性能 / Database Performance**
   - 查询延迟: < 50ms
   - 连接数: < 100

---

**文档版本 / Document Version**: 1.0.0
**最后更新 / Last Updated**: 2024年10月24日 / October 24, 2024
