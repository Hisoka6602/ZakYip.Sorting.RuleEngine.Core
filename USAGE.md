# 使用示例

本文档提供ZakYip分拣规则引擎的具体使用示例。

## 1. 启动服务

### 开发模式

```bash
cd ZakYip.Sorting.RuleEngine.Service
dotnet run
```

服务将在 `http://localhost:5000` 启动。

访问 Swagger 文档：`http://localhost:5000/swagger`

### 生产模式

```powershell
# 发布应用
dotnet publish -c Release -o ./publish

# 安装为Windows服务
sc create "ZakYipSortingEngine" binPath="C:\path\to\publish\ZakYip.Sorting.RuleEngine.Service.exe"

# 启动服务
sc start "ZakYipSortingEngine"

# 停止服务
sc stop "ZakYipSortingEngine"

# 删除服务
sc delete "ZakYipSortingEngine"
```

## 2. 添加分拣规则

### 示例1：基于重量的规则

```bash
curl -X POST http://localhost:5000/api/rule \
  -H "Content-Type: application/json" \
  -d '{
    "ruleId": "RULE-WEIGHT-001",
    "ruleName": "重物分拣规则",
    "description": "重量大于1000克的包裹分配到A区",
    "priority": 1,
    "conditionExpression": "Weight > 1000",
    "targetChute": "CHUTE-A01",
    "isEnabled": true
  }'
```

### 示例2：基于体积的规则

```bash
curl -X POST http://localhost:5000/api/rule \
  -H "Content-Type: application/json" \
  -d '{
    "ruleId": "RULE-VOLUME-001",
    "ruleName": "大件分拣规则",
    "description": "体积大于100000立方厘米的包裹分配到B区",
    "priority": 2,
    "conditionExpression": "Volume > 100000",
    "targetChute": "CHUTE-B01",
    "isEnabled": true
  }'
```

### 示例3：基于条码的规则

```bash
curl -X POST http://localhost:5000/api/rule \
  -H "Content-Type: application/json" \
  -d '{
    "ruleId": "RULE-SF-001",
    "ruleName": "顺丰快递规则",
    "description": "条码包含SF的包裹分配到C区",
    "priority": 3,
    "conditionExpression": "Barcode CONTAINS '\''SF'\''",
    "targetChute": "CHUTE-C01",
    "isEnabled": true
  }'
```

### 示例4：默认规则

```bash
curl -X POST http://localhost:5000/api/rule \
  -H "Content-Type: application/json" \
  -d '{
    "ruleId": "RULE-DEFAULT",
    "ruleName": "默认规则",
    "description": "不满足任何规则的包裹默认分配",
    "priority": 999,
    "conditionExpression": "DEFAULT",
    "targetChute": "CHUTE-DEFAULT",
    "isEnabled": true
  }'
```

## 3. 查询规则

### 获取所有规则

```bash
curl http://localhost:5000/api/rule
```

### 获取启用的规则

```bash
curl http://localhost:5000/api/rule/enabled
```

### 获取特定规则

```bash
curl http://localhost:5000/api/rule/RULE-WEIGHT-001
```

## 4. 更新规则

```bash
curl -X PUT http://localhost:5000/api/rule/RULE-WEIGHT-001 \
  -H "Content-Type: application/json" \
  -d '{
    "ruleId": "RULE-WEIGHT-001",
    "ruleName": "重物分拣规则（已更新）",
    "description": "重量大于1500克的包裹分配到A区",
    "priority": 1,
    "conditionExpression": "Weight > 1500",
    "targetChute": "CHUTE-A01",
    "isEnabled": true
  }'
```

## 5. 删除规则

```bash
curl -X DELETE http://localhost:5000/api/rule/RULE-WEIGHT-001
```

## 6. 处理包裹

### 处理单个包裹

```bash
curl -X POST http://localhost:5000/api/parcel/process \
  -H "Content-Type: application/json" \
  -d '{
    "parcelId": "PKG20240101001",
    "cartNumber": "CART001",
    "barcode": "SF1234567890",
    "weight": 1500,
    "length": 300,
    "width": 200,
    "height": 150,
    "volume": 9000
  }'
```

响应示例：

```json
{
  "success": true,
  "parcelId": "PKG20240101001",
  "chuteNumber": "CHUTE-A01",
  "errorMessage": null,
  "processingTimeMs": 45
}
```

### 批量处理包裹

```bash
curl -X POST http://localhost:5000/api/parcel/process/batch \
  -H "Content-Type: application/json" \
  -d '[
    {
      "parcelId": "PKG001",
      "cartNumber": "CART001",
      "barcode": "SF001",
      "weight": 1200,
      "volume": 8000
    },
    {
      "parcelId": "PKG002",
      "cartNumber": "CART002",
      "barcode": "JD002",
      "weight": 800,
      "volume": 5000
    },
    {
      "parcelId": "PKG003",
      "cartNumber": "CART003",
      "barcode": "YTO003",
      "weight": 2500,
      "volume": 120000
    }
  ]'
```

## 7. 规则表达式示例

### 数值比较

| 表达式 | 说明 |
|-------|------|
| `Weight > 1000` | 重量大于1000克 |
| `Weight >= 500` | 重量大于等于500克 |
| `Weight < 2000` | 重量小于2000克 |
| `Weight <= 1500` | 重量小于等于1500克 |
| `Weight == 1000` | 重量等于1000克 |
| `Volume > 50000` | 体积大于50000立方厘米 |

### 字符串匹配

| 表达式 | 说明 |
|-------|------|
| `Barcode CONTAINS 'SF'` | 条码包含"SF" |
| `Barcode STARTSWITH '123'` | 条码以"123"开头 |
| `Barcode ENDSWITH '890'` | 条码以"890"结尾 |
| `CartNumber == 'CART001'` | 小车号等于"CART001" |

### 默认匹配

| 表达式 | 说明 |
|-------|------|
| `DEFAULT` | 匹配所有（用于默认规则） |

## 8. C# 客户端示例

```csharp
using System.Net.Http.Json;

public class SortingEngineClient
{
    private readonly HttpClient _httpClient;

    public SortingEngineClient(string baseUrl = "http://localhost:5000")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    // 处理包裹
    public async Task<ParcelProcessResponse> ProcessParcelAsync(
        ParcelProcessRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/parcel/process", request);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content
            .ReadFromJsonAsync<ParcelProcessResponse>();
    }

    // 添加规则
    public async Task<SortingRule> AddRuleAsync(SortingRule rule)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/rule", rule);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content
            .ReadFromJsonAsync<SortingRule>();
    }

    // 获取所有规则
    public async Task<List<SortingRule>> GetRulesAsync()
    {
        return await _httpClient
            .GetFromJsonAsync<List<SortingRule>>("/api/rule");
    }
}

// 使用示例
var client = new SortingEngineClient();

// 处理包裹
var request = new ParcelProcessRequest
{
    ParcelId = "PKG001",
    CartNumber = "CART001",
    Barcode = "SF1234567890",
    Weight = 1500,
    Volume = 9000
};

var result = await client.ProcessParcelAsync(request);
Console.WriteLine($"格口号: {result.ChuteNumber}");
```

## 9. Python 客户端示例

```python
import requests

class SortingEngineClient:
    def __init__(self, base_url="http://localhost:5000"):
        self.base_url = base_url
    
    def process_parcel(self, parcel_data):
        """处理包裹"""
        response = requests.post(
            f"{self.base_url}/api/parcel/process",
            json=parcel_data
        )
        response.raise_for_status()
        return response.json()
    
    def add_rule(self, rule_data):
        """添加规则"""
        response = requests.post(
            f"{self.base_url}/api/rule",
            json=rule_data
        )
        response.raise_for_status()
        return response.json()
    
    def get_rules(self):
        """获取所有规则"""
        response = requests.get(f"{self.base_url}/api/rule")
        response.raise_for_status()
        return response.json()

# 使用示例
client = SortingEngineClient()

# 处理包裹
parcel = {
    "parcelId": "PKG001",
    "cartNumber": "CART001",
    "barcode": "SF1234567890",
    "weight": 1500,
    "volume": 9000
}

result = client.process_parcel(parcel)
print(f"格口号: {result['chuteNumber']}")
```

## 10. 健康检查

```bash
# 检查服务状态
curl http://localhost:5000/health

# 响应
{
  "status": "healthy",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## 11. 版本信息

```bash
curl http://localhost:5000/version

# 响应
{
  "version": "1.0.0",
  "name": "ZakYip.Sorting.RuleEngine.Core",
  "description": "分拣规则引擎核心系统"
}
```

## 12. 配置文件说明

### appsettings.json

```json
{
  "AppSettings": {
    "LiteDb": {
      "ConnectionString": "Filename=./data/config.db;Connection=shared"
    },
    "MySql": {
      "ConnectionString": "Server=localhost;Database=sorting_logs;User=root;Password=your_password;",
      "Enabled": true
    },
    "Sqlite": {
      "ConnectionString": "Data Source=./data/logs.db"
    },
    "ThirdPartyApi": {
      "BaseUrl": "https://api.example.com",
      "TimeoutSeconds": 30,
      "ApiKey": "your-api-key"
    },
    "MiniApi": {
      "Urls": [ "http://localhost:5000" ],
      "EnableSwagger": true
    }
  }
}
```

## 13. 故障排查

### 服务无法启动

1. 检查端口是否被占用
2. 查看日志文件
3. 确认配置文件正确

### 数据库连接失败

1. MySQL连接失败会自动降级到SQLite
2. 检查连接字符串配置
3. 确保数据库服务运行

### API调用失败

1. 检查服务是否正常运行
2. 验证请求格式
3. 查看返回的错误信息

## 14. 性能测试

使用Apache Bench进行简单性能测试：

```bash
# 单个请求测试
ab -n 1000 -c 10 -p parcel.json -T "application/json" \
   http://localhost:5000/api/parcel/process
```

其中 `parcel.json` 文件内容：

```json
{
  "parcelId": "PKG001",
  "cartNumber": "CART001",
  "weight": 1500,
  "volume": 9000
}
```

## 15. 监控建议

- 定期检查 `/health` 端点
- 监控数据库日志表大小
- 关注API响应时间
- 设置告警规则

---

更多信息请参考：
- [README.md](README.md) - 项目概述
- [ARCHITECTURE.md](ARCHITECTURE.md) - 架构设计
