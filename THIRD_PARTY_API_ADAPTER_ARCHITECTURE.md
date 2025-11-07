# 第三方API适配器架构 / Third-Party API Adapter Architecture

## 概述 / Overview

系统采用**适配器模式 + 工厂模式**实现第三方API的灵活切换，支持同时配置多个API适配器，但只有一个在运行时激活。

The system uses **Adapter Pattern + Factory Pattern** to enable flexible switching between third-party APIs. Multiple API adapters can be configured, but only one is active at runtime.

## 架构设计 / Architecture Design

### 1. 接口层 / Interface Layer

#### IThirdPartyApiAdapter
```csharp
public interface IThirdPartyApiAdapter
{
    Task<ThirdPartyResponse> UploadDataAsync(ParcelInfo parcelInfo, DwsData dwsData, CancellationToken cancellationToken = default);
    Task<ThirdPartyResponse> ScanParcelAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ThirdPartyResponse> RequestChuteAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ThirdPartyResponse> UploadImageAsync(string barcode, byte[] imageData, string contentType = "image/jpeg", CancellationToken cancellationToken = default);
}
```

#### IThirdPartyApiAdapterFactory
```csharp
public interface IThirdPartyApiAdapterFactory
{
    IThirdPartyApiAdapter GetActiveAdapter();
    string GetActiveAdapterName();
}
```

### 2. 实现层 / Implementation Layer

#### 可用的适配器 / Available Adapters

1. **ThirdPartyApiClient** - 默认通用API适配器
2. **WdtWmsApiClient** - 旺店通WMS API适配器
3. **JushuitanErpApiClient** - 聚水潭ERP API适配器

#### 适配器工厂 / Adapter Factory

**ThirdPartyApiAdapterFactory** 负责：
- 根据配置选择唯一激活的适配器
- 提供适配器实例给业务层
- 记录当前激活的适配器名称

## 配置说明 / Configuration

### appsettings.json

```json
{
  "AppSettings": {
    "ActiveApiAdapter": "WdtWmsApiClient",  // 激活的适配器类型
    
    "ThirdPartyApi": {
      "BaseUrl": "https://api.example.com",
      "TimeoutSeconds": 30,
      "ApiKey": "your_api_key"
    },
    
    "WdtWmsApi": {
      "BaseUrl": "https://api.wdt.com",
      "AppKey": "your_app_key",
      "AppSecret": "your_app_secret",
      "TimeoutSeconds": 30
    },
    
    "JushuitanErpApi": {
      "BaseUrl": "https://api.jushuitan.com",
      "PartnerKey": "your_partner_key",
      "PartnerSecret": "your_partner_secret",
      "Token": "your_token",
      "TimeoutSeconds": 30
    }
  }
}
```

### 切换适配器 / Switching Adapters

修改 `ActiveApiAdapter` 的值即可切换适配器：
- `ThirdPartyApiClient` - 使用默认API
- `WdtWmsApiClient` - 使用旺店通WMS API
- `JushuitanErpApiClient` - 使用聚水潭ERP API

## 使用方法 / Usage

### 业务层注入 / Service Layer Injection

```csharp
public class ParcelProcessingService
{
    private readonly IThirdPartyApiAdapterFactory _apiAdapterFactory;
    
    public ParcelProcessingService(IThirdPartyApiAdapterFactory apiAdapterFactory)
    {
        _apiAdapterFactory = apiAdapterFactory;
    }
    
    public async Task ProcessParcelAsync(ParcelInfo parcel, DwsData dws)
    {
        // 获取当前激活的适配器
        var adapter = _apiAdapterFactory.GetActiveAdapter();
        
        // 使用适配器调用API
        var response = await adapter.UploadDataAsync(parcel, dws);
        
        if (response.Success)
        {
            // 处理成功响应
        }
    }
}
```

### 查看当前激活的适配器 / Check Active Adapter

```csharp
var adapterName = _apiAdapterFactory.GetActiveAdapterName();
_logger.LogInformation("当前使用的API适配器: {AdapterName}", adapterName);
```

## 依赖注入配置 / Dependency Injection

在 `Program.cs` 中的配置：

```csharp
// 注册所有适配器实现
builder.Services.AddHttpClient<ThirdPartyApiClient>(...);
builder.Services.AddHttpClient<WdtWmsApiClient>(...);
builder.Services.AddHttpClient<JushuitanErpApiClient>(...);

// 将所有适配器注册为IThirdPartyApiAdapter
builder.Services.AddSingleton<IThirdPartyApiAdapter>(sp => sp.GetRequiredService<ThirdPartyApiClient>());
builder.Services.AddSingleton<IThirdPartyApiAdapter>(sp => sp.GetRequiredService<WdtWmsApiClient>());
builder.Services.AddSingleton<IThirdPartyApiAdapter>(sp => sp.GetRequiredService<JushuitanErpApiClient>());

// 注册工厂 - 根据配置选择激活的适配器
builder.Services.AddSingleton<IThirdPartyApiAdapterFactory>(sp =>
{
    var adapters = sp.GetServices<IThirdPartyApiAdapter>();
    var logger = sp.GetRequiredService<ILogger<ThirdPartyApiAdapterFactory>>();
    return new ThirdPartyApiAdapterFactory(adapters, appSettings.ActiveApiAdapter, logger);
});
```

## 适配器方法映射 / Adapter Method Mapping

不同适配器对相同接口方法的实现：

| 接口方法 | ThirdPartyApiClient | WdtWmsApiClient | JushuitanErpApiClient |
|---------|---------------------|-----------------|----------------------|
| UploadDataAsync | 上传包裹数据 | 称重扫描 | 称重回传 |
| ScanParcelAsync | 扫描包裹 | 扫描包裹 | 查询订单 |
| RequestChuteAsync | 请求格口 | 查询包裹 | 更新物流 |
| UploadImageAsync | 上传图片 | 上传图片 | 不支持（返回成功） |

## 优势 / Advantages

1. **单一激活** - 同时只有一个适配器激活，避免冲突
2. **灵活切换** - 通过配置即可切换，无需修改代码
3. **统一接口** - 所有适配器实现相同接口，业务层无感知
4. **易于扩展** - 新增适配器只需实现接口并注册
5. **类型安全** - 编译时检查，避免运行时错误

## 扩展新适配器 / Adding New Adapter

1. 创建新类实现 `IThirdPartyApiAdapter` 接口
2. 在 `Program.cs` 中注册HttpClient和适配器
3. 在 `appsettings.json` 中添加配置节
4. 更新 `ActiveApiAdapter` 配置项文档

```csharp
public class MyNewApiClient : IThirdPartyApiAdapter
{
    // 实现接口方法
}

// Program.cs
builder.Services.AddHttpClient<MyNewApiClient>(...);
builder.Services.AddSingleton<IThirdPartyApiAdapter>(sp => sp.GetRequiredService<MyNewApiClient>());

// appsettings.json
"ActiveApiAdapter": "MyNewApiClient"
```

## 注意事项 / Notes

1. **配置验证** - 确保ActiveApiAdapter的值与实际类名匹配
2. **完整配置** - 激活的适配器必须有完整的配置信息
3. **日志记录** - 工厂初始化时会记录激活的适配器名称
4. **异常处理** - 如果没有找到匹配的适配器会抛出异常
5. **单例模式** - 工厂和适配器都注册为单例，确保全局唯一

## 测试 / Testing

### 单元测试示例

```csharp
[Fact]
public async Task Test_WithWdtAdapter()
{
    // Arrange
    var mockAdapter = new Mock<IThirdPartyApiAdapter>();
    var mockFactory = new Mock<IThirdPartyApiAdapterFactory>();
    mockFactory.Setup(f => f.GetActiveAdapter()).Returns(mockAdapter.Object);
    mockFactory.Setup(f => f.GetActiveAdapterName()).Returns("WdtWmsApiClient");
    
    // Act
    var service = new MyService(mockFactory.Object);
    await service.ProcessAsync();
    
    // Assert
    mockAdapter.Verify(a => a.UploadDataAsync(
        It.IsAny<ParcelInfo>(),
        It.IsAny<DwsData>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

## 版本历史 / Version History

### v1.15.0 (2025-11-07)
- ✅ 重构为适配器模式
- ✅ 实现工厂模式支持切换
- ✅ 重命名IThirdPartyApiClient为IThirdPartyApiAdapter
- ✅ 移除单独的接口，统一使用IThirdPartyApiAdapter
- ✅ 支持配置化切换适配器
- ✅ 确保只有一个适配器激活
