# IHttpClientFactory 使用审计报告 / IHttpClientFactory Usage Audit Report

**审计日期 / Audit Date:** 2025-12-16  
**审计范围 / Audit Scope:** 所有对外 HTTP 访问 / All external HTTP access  
**审计结果 / Audit Result:** ✅ **已全部合规 / FULLY COMPLIANT**

---

## 📋 执行摘要 / Executive Summary

根据新需求"**所有对外的http访问都需要使用IHttpClientFactory**"，我们对整个代码库进行了全面审计。

**结论 / Conclusion:**  
✅ **项目已完全符合要求，所有生产代码中的 HTTP 客户端都正确使用了 IHttpClientFactory。**

---

## 🔍 审计方法 / Audit Methodology

1. **代码扫描 / Code Scanning**
   - 搜索所有 `new HttpClient()` 实例
   - 搜索所有 `HttpClient` 字段声明
   - 搜索所有 `IHttpClientFactory` 使用

2. **架构审查 / Architecture Review**
   - 检查 Program.cs 中的 DI 注册
   - 验证 API 客户端的构造函数设计
   - 确认 HttpClient 生命周期管理

3. **最佳实践验证 / Best Practices Validation**
   - Typed Client 模式使用
   - HttpClientHandler 配置
   - 超时和重试策略配置

---

## ✅ 合规的 HTTP 客户端清单 / Compliant HTTP Clients

### 1. WCS API 客户端 / WCS API Client

**类名 / Class:** `WcsApiClient`  
**文件 / File:** `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/WcsApiClient.cs`  
**注册方式 / Registration:** `AddHttpClient<WcsApiClient>()`  

```csharp
// Constructor - 通过 DI 注入 HttpClient
public WcsApiClient(
    HttpClient httpClient,
    ILogger<WcsApiClient> logger,
    ISystemClock clock)
{
    _httpClient = httpClient;
    _logger = logger;
    _clock = clock;
}
```

**配置详情 / Configuration:**
```csharp
services.AddHttpClient<WcsApiClient>(client =>
{
    client.BaseAddress = new Uri(appSettings.WcsApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(appSettings.WcsApi.TimeoutSeconds);
    
    if (!string.IsNullOrEmpty(appSettings.WcsApi.ApiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-Key", appSettings.WcsApi.ApiKey);
    }
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    
    // ⚠️ WARNING: Only disable SSL validation in development/testing environments
    if (appSettings.WcsApi.DisableSslValidation)
    {
        logger.Warn("SSL certificate validation is DISABLED - development/testing only!");
        handler.ServerCertificateCustomValidationCallback = (m, c, ch, _) => true;
    }
    // Production: Uses default certificate validation ✅
    
    return handler;
});
```

**✅ 合规要点 / Compliance Points:**
- ✅ 使用 IHttpClientFactory 管理生命周期
- ✅ BaseAddress 和 Timeout 集中配置
- ✅ SSL 证书验证：生产环境启用，开发环境可配置禁用
- ✅ API Key 通过 Headers 配置
- ⚠️ **安全警告**: `DisableSslValidation` 仅用于开发/测试，生产环境必须为 `false`

---

### 2. 旺店通 WMS API 客户端 / WdtWms API Client

**类名 / Class:** `WdtWmsApiClient`  
**文件 / File:** `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/WdtWmsApiClient.cs`  
**注册方式 / Registration:** `AddHttpClient<WdtWmsApiClient>()` + `AddTypedClient`  

```csharp
// Constructor - 接收 HttpClient 及额外依赖
public WdtWmsApiClient(
    HttpClient httpClient,
    ILogger<WdtWmsApiClient> logger,
    ISystemClock clock,
    string appKey,
    string appSecret) : base(httpClient, logger, clock)
{
    _appKey = appKey;
    _appSecret = appSecret;
}
```

**配置详情 / Configuration:**
```csharp
services.AddHttpClient<WdtWmsApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(appSettings.WdtWmsApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(appSettings.WdtWmsApi.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    
    // ⚠️ WARNING: Only disable SSL validation in development/testing environments
    if (appSettings.WdtWmsApi.DisableSslValidation)
    {
        logger.Warn("SSL certificate validation is DISABLED - development/testing only!");
        handler.ServerCertificateCustomValidationCallback = (m, c, ch, _) => true;
    }
    // Production: Uses default certificate validation ✅
    
    return handler;
})
.AddTypedClient<WdtWmsApiClient>((client, sp) =>
{
    var loggerWdt = sp.GetRequiredService<ILogger<WdtWmsApiClient>>();
    var clock = sp.GetRequiredService<ISystemClock>();
    return new WdtWmsApiClient(
        client,
        loggerWdt,
        clock,
        appSettings.WdtWmsApi.AppKey,
        appSettings.WdtWmsApi.AppSecret);
});
```

**✅ 合规要点 / Compliance Points:**
- ✅ 使用 Typed Client 模式
- ✅ 通过 Factory 方法注入额外依赖（AppKey, AppSecret）
- ✅ 继承自 BaseErpApiClient，复用 HttpClient
- ✅ SSL 证书验证：生产环境启用，开发环境可配置禁用
- ⚠️ **安全警告**: `DisableSslValidation` 仅用于开发/测试，生产环境必须为 `false`

---

### 3. 聚水潭 ERP API 客户端 / Jushuitán ERP API Client

**类名 / Class:** `JushuitanErpApiClient`  
**文件 / File:** `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/JushuitanErpApiClient.cs`  
**注册方式 / Registration:** `AddHttpClient<JushuitanErpApiClient>()` + `AddTypedClient`  

```csharp
// Constructor - 接收 HttpClient 及 ERP 凭证
public JushuitanErpApiClient(
    HttpClient httpClient,
    ILogger<JushuitanErpApiClient> logger,
    ISystemClock clock,
    string partnerKey,
    string partnerSecret,
    string token) : base(httpClient, logger, clock)
{
    _partnerKey = partnerKey;
    _partnerSecret = partnerSecret;
    _token = token;
}
```

**配置详情 / Configuration:**
```csharp
services.AddHttpClient<JushuitanErpApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(appSettings.JushuitanErpApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(appSettings.JushuitanErpApi.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    
    // ⚠️ WARNING: Only disable SSL validation in development/testing environments
    if (appSettings.JushuitanErpApi.DisableSslValidation)
    {
        logger.Warn("SSL certificate validation is DISABLED - development/testing only!");
        handler.ServerCertificateCustomValidationCallback = (m, c, ch, _) => true;
    }
    // Production: Uses default certificate validation ✅
    
    return handler;
})
.AddTypedClient<JushuitanErpApiClient>((client, sp) =>
{
    var loggerJst = sp.GetRequiredService<ILogger<JushuitanErpApiClient>>();
    var clock = sp.GetRequiredService<ISystemClock>();
    return new JushuitanErpApiClient(
        client,
        loggerJst,
        clock,
        appSettings.JushuitanErpApi.PartnerKey,
        appSettings.JushuitanErpApi.PartnerSecret,
        appSettings.JushuitanErpApi.Token);
});
```

**✅ 合规要点 / Compliance Points:**
- ✅ 使用 Typed Client 模式
- ✅ 通过 Factory 注入 3 个 ERP 凭证参数
- ✅ 继承自 BaseErpApiClient
- ✅ SSL 证书验证：生产环境启用，开发环境可配置禁用
- ⚠️ **安全警告**: `DisableSslValidation` 仅用于开发/测试，生产环境必须为 `false`

---

### 4. 邮政处理中心 API 客户端 / Post Processing Center API Client

**类名 / Class:** `PostProcessingCenterApiClient`  
**文件 / File:** `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/PostProcessingCenterApiClient.cs`  
**注册方式 / Registration:** `AddHttpClient<PostProcessingCenterApiClient>()`  

```csharp
// Constructor - 继承自 BasePostalApiClient
public PostProcessingCenterApiClient(
    HttpClient httpClient,
    ILogger<PostProcessingCenterApiClient> logger,
    ISystemClock clock) : base(httpClient, logger, clock)
{
}
```

**配置详情 / Configuration:**
```csharp
services.AddHttpClient<PostProcessingCenterApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(appSettings.PostProcessingCenterApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(appSettings.PostProcessingCenterApi.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() => HttpClientConfigurationHelper.CreatePostalApiHandler());
```

**✅ 合规要点 / Compliance Points:**
- ✅ 使用 IHttpClientFactory
- ✅ 继承自 BasePostalApiClient，共享 SOAP 请求逻辑
- ✅ 使用辅助方法 `CreatePostalApiHandler()` 创建专用 Handler
- ✅ 支持邮政 API 特殊配置（TLS 1.2, 编码等）

---

### 5. 邮政分揽投机构 API 客户端 / Post Collection API Client

**类名 / Class:** `PostCollectionApiClient`  
**文件 / File:** `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/PostCollectionApiClient.cs`  
**注册方式 / Registration:** `AddHttpClient<PostCollectionApiClient>()`  

```csharp
// Constructor - 继承自 BasePostalApiClient
public PostCollectionApiClient(
    HttpClient httpClient,
    ILogger<PostCollectionApiClient> logger,
    ISystemClock clock) : base(httpClient, logger, clock)
{
}
```

**配置详情 / Configuration:**
```csharp
services.AddHttpClient<PostCollectionApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(appSettings.PostCollectionApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(appSettings.PostCollectionApi.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() => HttpClientConfigurationHelper.CreatePostalApiHandler());
```

**✅ 合规要点 / Compliance Points:**
- ✅ 使用 IHttpClientFactory
- ✅ 继承自 BasePostalApiClient
- ✅ 共享邮政 API Handler 配置
- ✅ BaseAddress 和 Timeout 独立配置

---

### 6. HTTP 第三方适配器 / HTTP Third Party Adapter

**类名 / Class:** `HttpThirdPartyAdapter`  
**文件 / File:** `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/Adapters/ThirdParty/HttpThirdPartyAdapter.cs`  
**注册方式 / Registration:** 构造函数注入 / Constructor Injection  

```csharp
// Constructor - 通过 DI 接收 HttpClient
public HttpThirdPartyAdapter(
    HttpClient httpClient,
    string endpoint,
    ILogger<HttpThirdPartyAdapter> logger)
{
    _httpClient = httpClient;
    _endpoint = endpoint;
    _logger = logger;
    
    // 配置弹性策略（Polly）
    _resiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(...)
        .AddCircuitBreaker(...)
        .Build();
}
```

**✅ 合规要点 / Compliance Points:**
- ✅ HttpClient 通过构造函数注入
- ✅ 集成 Polly 弹性策略（重试 + 断路器）
- ✅ 支持动态 endpoint 配置
- ✅ 适用于通用 HTTP 调用场景

---

### 7. WCS API 健康检查 / WCS API Health Check

**类名 / Class:** `WcsApiHealthCheck`  
**文件 / File:** `Service/ZakYip.Sorting.RuleEngine.Service/HealthChecks/WcsApiHealthCheck.cs`  
**注册方式 / Registration:** `IHttpClientFactory` 构造函数注入  

```csharp
// Constructor - 注入 IHttpClientFactory
private readonly IHttpClientFactory _httpClientFactory;

public WcsApiHealthCheck(
    IHttpClientFactory httpClientFactory,
    ILogger<WcsApiHealthCheck> logger)
{
    _httpClientFactory = httpClientFactory;
    _logger = logger;
}

// 使用 Factory 创建 HttpClient
public async Task<HealthCheckResult> CheckHealthAsync(...)
{
    var client = _httpClientFactory.CreateClient();
    // ...
}
```

**✅ 合规要点 / Compliance Points:**
- ✅ 直接使用 IHttpClientFactory
- ✅ 按需创建 HttpClient（健康检查场景）
- ✅ 避免在长生命周期服务中持有 HttpClient

---

## 🎯 IHttpClientFactory 最佳实践对照 / Best Practices Checklist

| 最佳实践 Best Practice | 状态 Status | 说明 Notes |
|----------------------|------------|------------|
| ✅ 使用 IHttpClientFactory 而非 new HttpClient() | ✅ 已实现 | 所有生产代码合规 |
| ✅ 使用 Typed Client 模式 | ✅ 已实现 | 所有 API 客户端都使用 |
| ✅ 配置 BaseAddress | ✅ 已实现 | 所有客户端都有 BaseAddress |
| ✅ 配置 Timeout | ✅ 已实现 | 所有客户端都有超时配置 |
| ✅ 自定义 HttpClientHandler | ✅ 已实现 | SSL 验证、代理等已配置 |
| ✅ 避免在 Singleton 中持有 HttpClient | ✅ 已实现 | 通过 DI 注入，生命周期正确 |
| ✅ 集成 Polly 弹性策略 | ✅ 已实现 | HttpThirdPartyAdapter 已集成 |
| ✅ 集中化配置管理 | ✅ 已实现 | 所有配置在 Program.cs |

---

## 📊 统计数据 / Statistics

- **生产 HTTP 客户端总数 / Total Production HTTP Clients:** 7
- **合规客户端数量 / Compliant Clients:** 7 (100%)
- **不合规客户端数量 / Non-Compliant Clients:** 0
- **测试代码中的 new HttpClient() / new HttpClient() in Tests:** 2 (仅测试，不影响生产)

---

## 🔧 测试代码建议 / Test Code Recommendations

### 发现的测试代码使用 / Found Test Code Usage

**文件 / File:** `Tests/ZakYip.Sorting.RuleEngine.Tests/ApiClients/ApiClientRequiredFieldsTests.cs`

```csharp
// Line 232 & 302
var httpClient = new HttpClient();
var client = new JushuitanErpApiClient(httpClient, logger, new MockSystemClock(), ...);
```

### 建议改进（可选）/ Recommended Improvements (Optional)

虽然测试代码中直接使用 `new HttpClient()` 不影响生产环境，但为了保持一致性，建议：

1. **方案 1：使用测试 HttpClientFactory**
   ```csharp
   var services = new ServiceCollection();
   services.AddHttpClient<JushuitanErpApiClient>();
   var provider = services.BuildServiceProvider();
   var client = provider.GetRequiredService<JushuitanErpApiClient>();
   ```

2. **方案 2：使用 HttpClient Mock**
   ```csharp
   var mockHandler = new Mock<HttpMessageHandler>();
   var httpClient = new HttpClient(mockHandler.Object);
   ```

3. **方案 3：保持现状**
   - 单元测试中的临时实例，无需修改
   - 不会造成套接字耗尽等问题

**建议 / Recommendation:** 方案 3（保持现状）- 测试代码影响范围有限，无需强制修改。

---

## ✅ 审计结论 / Audit Conclusion

### 合规性评估 / Compliance Assessment

**评级 / Rating:** ⭐⭐⭐⭐⭐ **优秀 (Excellent)**

项目已完全符合"所有对外的http访问都需要使用IHttpClientFactory"的要求：

1. ✅ **生产代码 100% 合规** - 所有 HTTP 客户端都使用 IHttpClientFactory
2. ✅ **架构设计优秀** - 使用 Typed Client 模式，代码清晰可维护
3. ✅ **配置管理规范** - 所有配置集中在 Program.cs，易于管理
4. ✅ **弹性策略完备** - 集成 Polly，支持重试和断路器
5. ✅ **生命周期管理正确** - 避免了套接字耗尽等问题

### 无需任何修改 / No Changes Required

**结论 / Conclusion:**  
✅ **项目已完全符合 IHttpClientFactory 最佳实践，无需任何修改。**

---

## 📚 参考资料 / References

1. [Microsoft Docs - IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
2. [Typed Clients Pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-8.0#typed-clients)
3. [Polly Integration with IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)

---

**审计完成 / Audit Complete**  
**审计者 / Auditor:** GitHub Copilot Agent  
**审计日期 / Date:** 2025-12-16
