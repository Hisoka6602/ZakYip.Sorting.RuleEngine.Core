# 接口模拟器 / Interface Simulator

## 概述 / Overview

接口模拟器是一个简单的Web API服务，用于生成随机的接口ID（1-50）。适用于测试和开发场景。

Interface Simulator is a simple Web API service that generates random interface IDs (1-50). Suitable for testing and development scenarios.

## 功能 / Features

- ✅ 生成单个随机接口ID（1-50）
- ✅ 批量生成随机接口ID（最多100个）
- ✅ Swagger UI 文档
- ✅ 健康检查端点
- ✅ 完整的异常处理

- ✅ Generate single random interface ID (1-50)
- ✅ Generate batch of random interface IDs (up to 100)
- ✅ Swagger UI documentation
- ✅ Health check endpoint
- ✅ Complete exception handling

## 快速开始 / Quick Start

### 运行服务 / Run the Service

```bash
cd Tests/ZakYip.Sorting.RuleEngine.InterfaceSimulator
dotnet run
```

默认端口：5100  
Default port: 5100

访问 Swagger UI：http://localhost:5100  
Access Swagger UI: http://localhost:5100

## API 端点 / API Endpoints

### 1. 获取随机接口ID / Get Random Interface ID

```http
GET /api/interface/random
```

**响应示例 / Response Example:**
```json
{
  "interfaceId": 23,
  "timestamp": "2025-11-09T12:00:00Z",
  "success": true,
  "message": "接口ID: 23"
}
```

### 2. 批量获取随机接口ID / Get Batch Random Interface IDs

```http
GET /api/interface/random/batch?count=10
```

**参数 / Parameters:**
- `count` - 数量（1-100），默认10 / Count (1-100), default 10

**响应示例 / Response Example:**
```json
{
  "interfaceIds": [15, 42, 7, 33, 28, 9, 41, 19, 3, 50],
  "count": 10,
  "timestamp": "2025-11-09T12:00:00Z",
  "success": true,
  "message": "生成了 10 个接口ID"
}
```

### 3. 健康检查 / Health Check

```http
GET /api/health
```

**响应示例 / Response Example:**
```json
{
  "status": "Healthy",
  "service": "Interface Simulator",
  "timestamp": "2025-11-09T12:00:00Z",
  "version": "1.0.0"
}
```

## 使用示例 / Usage Examples

### cURL

```bash
# 获取单个接口ID / Get single interface ID
curl http://localhost:5100/api/interface/random

# 批量获取接口ID / Get batch interface IDs
curl "http://localhost:5100/api/interface/random/batch?count=20"

# 健康检查 / Health check
curl http://localhost:5100/api/health
```

### PowerShell

```powershell
# 获取单个接口ID / Get single interface ID
Invoke-RestMethod -Uri "http://localhost:5100/api/interface/random" -Method Get

# 批量获取接口ID / Get batch interface IDs
Invoke-RestMethod -Uri "http://localhost:5100/api/interface/random/batch?count=20" -Method Get
```

### C#

```csharp
using System.Net.Http;
using System.Net.Http.Json;

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5100");

// 获取单个接口ID / Get single interface ID
var response = await httpClient.GetFromJsonAsync<InterfaceResponse>("/api/interface/random");
Console.WriteLine($"Interface ID: {response.InterfaceId}");

// 批量获取接口ID / Get batch interface IDs
var batchResponse = await httpClient.GetFromJsonAsync<BatchInterfaceResponse>("/api/interface/random/batch?count=20");
Console.WriteLine($"Generated {batchResponse.Count} interface IDs");
```

## 异常处理 / Exception Handling

所有端点都包含完整的异常处理：
- 输入验证错误返回 400 Bad Request
- 内部错误返回 500 Internal Server Error
- 详细的错误消息帮助调试

All endpoints include complete exception handling:
- Input validation errors return 400 Bad Request
- Internal errors return 500 Internal Server Error
- Detailed error messages for debugging

## 配置 / Configuration

在 `appsettings.json` 中配置端口：

Configure port in `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5100"
      }
    }
  }
}
```

## 作为Windows服务运行 / Run as Windows Service

### 安装服务 / Install Service

```powershell
# 发布应用 / Publish app
dotnet publish -c Release -o ./publish

# 创建Windows服务 / Create Windows service
sc create "InterfaceSimulator" binPath="C:\path\to\publish\ZakYip.Sorting.RuleEngine.InterfaceSimulator.exe"

# 启动服务 / Start service
sc start "InterfaceSimulator"
```

### 卸载服务 / Uninstall Service

```powershell
sc stop "InterfaceSimulator"
sc delete "InterfaceSimulator"
```

## Docker支持 / Docker Support

### 创建Dockerfile / Create Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ZakYip.Sorting.RuleEngine.InterfaceSimulator.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZakYip.Sorting.RuleEngine.InterfaceSimulator.dll"]
```

### 构建和运行 / Build and Run

```bash
# 构建镜像 / Build image
docker build -t interface-simulator .

# 运行容器 / Run container
docker run -d -p 5100:5100 --name interface-sim interface-simulator
```

## 许可证 / License

MIT License

## 联系方式 / Contact

- 项目地址 / Project: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core
- 问题反馈 / Issues: https://github.com/Hisoka6602/ZakYip.Sorting.RuleEngine.Core/issues
