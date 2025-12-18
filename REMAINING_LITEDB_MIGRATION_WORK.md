# 完成所有API客户端配置迁移到LiteDB的剩余工作

## 当前状态 / Current Status

### ✅ 已完成 / Completed
1. PostCollectionApiClient - 已使用LiteDB
2. PostProcessingCenterApiClient - 已使用LiteDB
3. 创建了3个新的Config实体（JushuitanErpConfig, WdtWmsConfig, WdtErpFlagshipConfig）
4. 创建了3个新的Repository接口和LiteDB实现
5. 在Program.cs中注册了3个新仓储

### ⏳ 待完成 / Remaining Work

需要修改3个API客户端：
1. **JushuitanErpApiClient.cs** - 约30-45分钟
2. **WdtWmsApiClient.cs** - 约30-45分钟
3. **WdtErpFlagshipApiClient.cs** - 约30-45分钟
4. **ApiClientConfigController.cs** - 更新3个端点 - 约20-30分钟

**总预估工作量**: 约2-3小时

## 详细修改步骤 / Detailed Steps

### 1. JushuitanErpApiClient 修改

**文件**: `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/JushuitanErp/JushuitanErpApiClient.cs`

**当前代码结构**:
```csharp
public class JushuitanErpApiClient : IWcsApiAdapter
{
    public JushuitanErpApiParameters Parameters { get; set; }
    
    public JushuitanErpApiClient(HttpClient httpClient, ILogger logger, ISystemClock clock)
    {
        Parameters = new JushuitanErpApiParameters { ... };
    }
}
```

**需要修改为**:
```csharp
public class JushuitanErpApiClient : IWcsApiAdapter
{
    private readonly IJushuitanErpConfigRepository _configRepository;
    private JushuitanErpConfig? _cachedConfig;
    private DateTime _configCacheTime = DateTime.MinValue;
    private readonly TimeSpan _configCacheExpiry = TimeSpan.FromMinutes(5);
    
    public JushuitanErpApiClient(
        HttpClient httpClient, 
        ILogger<JushuitanErpApiClient> logger, 
        ISystemClock clock,
        IJushuitanErpConfigRepository configRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clock = clock;
        _configRepository = configRepository;
    }
    
    private async Task<JushuitanErpConfig> GetConfigAsync()
    {
        if (_cachedConfig != null && _clock.LocalNow - _configCacheTime < _configCacheExpiry)
        {
            return _cachedConfig;
        }
        
        var config = await _configRepository.GetByIdAsync(JushuitanErpConfig.SingletonId).ConfigureAwait(false);
        
        if (config == null)
        {
            _logger.LogWarning("聚水潭ERP配置不存在，使用默认配置");
            config = new JushuitanErpConfig
            {
                ConfigId = JushuitanErpConfig.SingletonId,
                Name = "聚水潭ERP默认配置",
                Url = "https://openapi.jushuitan.com/open/orders/weight/send/upload",
                AppKey = "",
                AppSecret = "",
                AccessToken = "",
                Version = 2,
                TimeoutMs = 5000,
                IsUploadWeight = true,
                Type = 1,
                IsUnLid = false,
                Channel = "",
                DefaultWeight = -1,
                IsEnabled = true,
                Description = "默认配置 - 请通过API更新",
                CreatedAt = _clock.LocalNow,
                UpdatedAt = _clock.LocalNow
            };
            
            await _configRepository.AddAsync(config).ConfigureAwait(false);
        }
        
        _cachedConfig = config;
        _configCacheTime = _clock.LocalNow;
        
        return config;
    }
}
```

**在 RequestChuteAsync 方法中**:
将所有 `Parameters.xxx` 替换为 `config.xxx`，在方法开始处调用：
```csharp
var config = await GetConfigAsync().ConfigureAwait(false);
```

然后：
- `Parameters.Type` → `config.Type`
- `Parameters.IsUnLid` → `config.IsUnLid`
- `Parameters.Channel` → `config.Channel`
- `Parameters.IsUploadWeight` → `config.IsUploadWeight`
- `Parameters.AppKey` → `config.AppKey`
- `Parameters.AccessToken` → `config.AccessToken`
- `Parameters.Version` → `config.Version`
- `Parameters.AppSecret` → `config.AppSecret`
- `Parameters.TimeOut` → `config.TimeoutMs`
- `Parameters.Url` → `config.Url`

### 2. WdtWmsApiClient 修改

**文件**: `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/WdtWms/WdtWmsApiClient.cs`

按照相同模式修改，替换参数：
- `Parameters.Url` → `config.Url`
- `Parameters.Sid` → `config.Sid`
- `Parameters.AppKey` → `config.AppKey`
- `Parameters.AppSecret` → `config.AppSecret`
- `Parameters.Method` → `config.Method`
- `Parameters.TimeOut` → `config.TimeoutMs`
- `Parameters.MustIncludeBoxBarcode` → `config.MustIncludeBoxBarcode`
- `Parameters.DefaultWeight` → `config.DefaultWeight`

### 3. WdtErpFlagshipApiClient 修改

**文件**: `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/ApiClients/WdtErpFlagship/WdtErpFlagshipApiClient.cs`

按照相同模式修改，替换参数：
- `Parameters.Method` → `config.Method`
- `Parameters.PackagerId` → `config.PackagerId`
- `Parameters.PackagerNo` → `config.PackagerNo`
- `Parameters.OperateTableName` → `config.OperateTableName`
- `Parameters.Force` → `config.Force`
- `Parameters.Url` → `config.Url`
- `Parameters.Key` → `config.Key`
- `Parameters.Sid` → `config.Sid`
- `Parameters.V` → `config.V`
- `Parameters.Salt` → `config.Salt`
- `Parameters.Appsecret` → `config.Appsecret`
- `Parameters.TimeOut` → `config.TimeoutMs`

### 4. ApiClientConfigController 修改

**文件**: `Service/ZakYip.Sorting.RuleEngine.Service/API/ApiClientConfigController.cs`

#### 4.1 添加依赖注入

在构造函数中添加：
```csharp
private readonly IJushuitanErpConfigRepository _jushuitanErpConfigRepository;
private readonly IWdtWmsConfigRepository _wdtWmsConfigRepository;
private readonly IWdtErpFlagshipConfigRepository _wdtErpFlagshipConfigRepository;

public ApiClientConfigController(
    // ... existing parameters
    IJushuitanErpConfigRepository jushuitanErpConfigRepository,
    IWdtWmsConfigRepository wdtWmsConfigRepository,
    IWdtErpFlagshipConfigRepository wdtErpFlagshipConfigRepository)
{
    // ... existing code
    _jushuitanErpConfigRepository = jushuitanErpConfigRepository;
    _wdtWmsConfigRepository = wdtWmsConfigRepository;
    _wdtErpFlagshipConfigRepository = wdtErpFlagshipConfigRepository;
}
```

#### 4.2 修改 GetJushuitanErpConfig 方法

```csharp
[HttpGet("jushuitanerp")]
public async Task<ActionResult<ApiResponse<JushuitanErpConfigRequest>>> GetJushuitanErpConfig()
{
    try
    {
        var config = await _jushuitanErpConfigRepository.GetByIdAsync(JushuitanErpConfig.SingletonId).ConfigureAwait(false);
        
        if (config == null)
        {
            return NotFound(ApiResponse<JushuitanErpConfigRequest>.FailureResult(
                "聚水潭ERP配置不存在", "CONFIG_NOT_FOUND"));
        }

        var dto = new JushuitanErpConfigRequest
        {
            Name = config.Name,
            Url = config.Url,
            AppKey = MaskSecret(config.AppKey),
            AppSecret = MaskSecret(config.AppSecret),
            AccessToken = MaskSecret(config.AccessToken),
            Version = config.Version,
            TimeoutMs = config.TimeoutMs,
            IsUploadWeight = config.IsUploadWeight,
            Type = config.Type,
            IsUnLid = config.IsUnLid,
            Channel = config.Channel,
            DefaultWeight = config.DefaultWeight,
            IsEnabled = config.IsEnabled,
            Description = config.Description
        };

        return Ok(ApiResponse<JushuitanErpConfigRequest>.SuccessResult(dto));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取聚水潭ERP配置时发生错误");
        return StatusCode(500, ApiResponse<JushuitanErpConfigRequest>.FailureResult(
            "获取配置失败", "GET_CONFIG_FAILED"));
    }
}
```

#### 4.3 修改 UpdateJushuitanErpConfig 方法

```csharp
[HttpPut("jushuitanerp")]
public async Task<ActionResult<ApiResponse<string>>> UpdateJushuitanErpConfig([FromBody] JushuitanErpConfigRequest request)
{
    try
    {
        var existingConfig = await _jushuitanErpConfigRepository.GetByIdAsync(JushuitanErpConfig.SingletonId).ConfigureAwait(false);
        
        if (existingConfig == null)
        {
            return NotFound(ApiResponse<string>.FailureResult(
                "聚水潭ERP配置不存在", "CONFIG_NOT_FOUND"));
        }

        var updatedConfig = existingConfig with
        {
            Name = request.Name,
            Url = request.Url,
            AppKey = request.AppKey,
            AppSecret = request.AppSecret,
            AccessToken = request.AccessToken,
            Version = request.Version,
            TimeoutMs = request.TimeoutMs,
            IsUploadWeight = request.IsUploadWeight,
            Type = request.Type,
            IsUnLid = request.IsUnLid,
            Channel = request.Channel,
            DefaultWeight = request.DefaultWeight,
            IsEnabled = request.IsEnabled,
            Description = request.Description,
            UpdatedAt = _clock.LocalNow
        };

        var success = await _jushuitanErpConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
        
        if (!success)
        {
            return StatusCode(500, ApiResponse<string>.FailureResult(
                "更新配置失败", "UPDATE_FAILED"));
        }

        _logger.LogInformation("成功更新聚水潭ERP API配置");
        return Ok(ApiResponse<string>.SuccessResult("配置更新成功"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新聚水潭ERP配置时发生错误");
        return StatusCode(500, ApiResponse<string>.FailureResult(
            "更新配置失败", "UPDATE_CONFIG_FAILED"));
    }
}
```

#### 4.4 对 WdtWms 和 WdtErpFlagship 执行相同修改

按照相同模式修改：
- GetWdtWmsConfig 和 UpdateWdtWmsConfig
- GetWdtErpFlagshipConfig 和 UpdateWdtErpFlagshipConfig

### 5. 创建/更新 DTO 请求类

需要创建或更新以下DTO类以匹配新的Config实体结构：

- **JushuitanErpConfigRequest.cs** (Application/DTOs/Requests/)
- **WdtWmsConfigRequest.cs** (Application/DTOs/Requests/) 
- **WdtErpFlagshipConfigRequest.cs** (Application/DTOs/Requests/)

确保它们包含所有必需字段。

## 测试清单 / Testing Checklist

完成修改后，需要测试：

- [ ] 编译成功（0个错误）
- [ ] GET /api/ApiClientConfig/jushuitanerp 返回配置
- [ ] PUT /api/ApiClientConfig/jushuitanerp 更新配置成功
- [ ] JushuitanErpApiClient.RequestChuteAsync 能从LiteDB读取配置
- [ ] GET /api/ApiClientConfig/wdtwms 返回配置
- [ ] PUT /api/ApiClientConfig/wdtwms 更新配置成功
- [ ] WdtWmsApiClient.RequestChuteAsync 能从LiteDB读取配置
- [ ] GET /api/ApiClientConfig/wdterpflagship 返回配置
- [ ] PUT /api/ApiClientConfig/wdterpflagship 更新配置成功
- [ ] WdtErpFlagshipApiClient.RequestChuteAsync 能从LiteDB读取配置

## 注意事项 / Notes

1. **最小化修改**: 保持现有业务逻辑不变，只更改配置来源
2. **缓存机制**: 使用5分钟缓存避免频繁数据库查询
3. **默认配置**: 如果配置不存在，自动创建默认配置
4. **线程安全**: 缓存更新使用时间戳，无需锁
5. **向后兼容**: Parameters类暂时保留，以便逐步迁移

## 预期效果 / Expected Result

完成后，所有6个API客户端配置都将：
1. 存储在LiteDB中
2. 通过API端点管理
3. 支持热更新（无需重启）
4. 统一的配置管理模式
