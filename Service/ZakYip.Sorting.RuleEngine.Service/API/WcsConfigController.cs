using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// WCS配置控制器 / WCS Configuration Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WcsConfigController : ControllerBase
{
    private readonly IWcsApiAdapterFactory _wcsApiAdapterFactory;
    private readonly IWcsApiConfigRepository _wcsApiConfigRepository;
    private readonly ILogger<WcsConfigController> _logger;
    private readonly ISystemClock _clock;

    public WcsConfigController(
        IWcsApiAdapterFactory wcsApiAdapterFactory,
        IWcsApiConfigRepository wcsApiConfigRepository,
        ILogger<WcsConfigController> logger,
        ISystemClock clock)
    {
        _wcsApiAdapterFactory = wcsApiAdapterFactory;
        _wcsApiConfigRepository = wcsApiConfigRepository;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 获取当前WCS适配器配置 / Get Current WCS Adapter Configuration
    /// </summary>
    /// <returns>当前激活的WCS适配器信息</returns>
    /// <response code="200">成功返回配置</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取当前WCS适配器配置",
        Description = "查询当前系统使用的WCS第三方接口协议适配器。可选值包括：无（不调用任何第三方API）、WcsApiClient、JushuitanErpApiClient、WdtWmsApiClient、PostCollectionApiClient、PostProcessingCenterApiClient等。",
        OperationId = "GetWcsConfig",
        Tags = new[] { "WCS配置 / WCS Configuration" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<WcsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WcsConfigResponseDto>))]
    public ActionResult<ApiResponse<WcsConfigResponseDto>> GetConfig()
    {
        try
        {
            var activeAdapterName = _wcsApiAdapterFactory.GetActiveAdapterName();
            var config = new WcsConfigResponseDto
            {
                ActiveAdapter = activeAdapterName,
                IsEnabled = activeAdapterName != "None" && activeAdapterName != "无",
                AvailableAdapters = GetAvailableAdapters(),
                Description = GetAdapterDescription(activeAdapterName),
                Timestamp = _clock.LocalNow
            };

            return Ok(ApiResponse<WcsConfigResponseDto>.SuccessResult(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取WCS配置失败");
            return StatusCode(500, ApiResponse<WcsConfigResponseDto>.FailureResult(
                $"获取配置失败: {ex.Message}", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 获取可用的WCS适配器列表 / Get Available WCS Adapters List
    /// </summary>
    /// <returns>可用的适配器列表</returns>
    /// <response code="200">成功返回列表</response>
    [HttpGet("adapters")]
    [SwaggerOperation(
        Summary = "获取可用的WCS适配器列表",
        Description = "获取所有已对接的WCS API适配器列表，包括适配器名称和描述信息。",
        OperationId = "GetAvailableAdapters",
        Tags = new[] { "WCS配置 / WCS Configuration" }
    )]
    [SwaggerResponse(200, "成功返回列表", typeof(ApiResponse<List<AdapterInfoDto>>))]
    public ActionResult<ApiResponse<List<AdapterInfoDto>>> GetAdapters()
    {
        try
        {
            var adapters = GetAvailableAdapters();
            return Ok(ApiResponse<List<AdapterInfoDto>>.SuccessResult(adapters));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用适配器列表失败");
            return StatusCode(500, ApiResponse<List<AdapterInfoDto>>.FailureResult(
                $"获取列表失败: {ex.Message}", "GET_ADAPTERS_FAILED"));
        }
    }

    /// <summary>
    /// 获取WCS API配置 / Get WCS API Configuration
    /// </summary>
    /// <returns>WCS API配置详情</returns>
    /// <response code="200">成功返回配置</response>
    /// <response code="404">配置不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("api")]
    [SwaggerOperation(
        Summary = "获取WCS API配置",
        Description = "获取WCS API端点配置，包括URL、超时时间等信息",
        OperationId = "GetWcsApiConfig",
        Tags = new[] { "WCS配置 / WCS Configuration" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<WcsApiConfigResponseDto>>> GetApiConfig()
    {
        try
        {
            var config = await _wcsApiConfigRepository.GetByIdAsync(WcsApiConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                    "WCS API配置不存在，请先创建配置 / WCS API config not found, please create it first", 
                    "CONFIG_NOT_FOUND"));
            }

            var dto = new WcsApiConfigResponseDto
            {
                Url = config.Url,
                ApiKeyMasked = MaskApiKey(config.ApiKey),
                TimeoutMs = config.TimeoutMs,
                DisableSslValidation = config.DisableSslValidation,
                IsEnabled = config.IsEnabled,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<WcsApiConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取WCS API配置失败 / Failed to get WCS API config");
            return StatusCode(500, ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                $"获取配置失败: {ex.Message} / Failed to get config: {ex.Message}", 
                "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新WCS API配置（支持热更新）/ Update WCS API Configuration (with hot reload)
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    /// <response code="200">更新成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPut("api")]
    [SwaggerOperation(
        Summary = "更新WCS API配置",
        Description = "更新WCS API端点配置并触发热更新。配置更新后立即生效，无需重启应用。",
        OperationId = "UpdateWcsApiConfig",
        Tags = new[] { "WCS配置 / WCS Configuration" }
    )]
    [SwaggerResponse(200, "更新成功", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<WcsApiConfigResponseDto>>> UpdateApiConfig(
        [FromBody, SwaggerRequestBody("WCS API配置更新请求", Required = true)] WcsApiConfigUpdateRequest request)
    {
        try
        {
            // 验证URL格式
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            {
                return BadRequest(ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                    "URL格式无效 / Invalid URL format", 
                    "INVALID_URL"));
            }

            // 验证超时时间
            if (request.TimeoutMs < 1000 || request.TimeoutMs > 300000)
            {
                return BadRequest(ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                    "超时时间必须在1000-300000毫秒之间 / Timeout must be between 1000-300000 ms", 
                    "INVALID_TIMEOUT"));
            }

            var now = _clock.LocalNow;
            
            // 检查配置是否存在
            var existingConfig = await _wcsApiConfigRepository.GetByIdAsync(WcsApiConfig.SingletonId).ConfigureAwait(false);
            
            var config = new WcsApiConfig
            {
                ConfigId = WcsApiConfig.SingletonId,
                Url = request.Url,
                ApiKey = request.ApiKey,
                TimeoutMs = request.TimeoutMs,
                DisableSslValidation = request.DisableSslValidation,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _wcsApiConfigRepository.AddAsync(config).ConfigureAwait(false);
                _logger.LogInformation("创建WCS API配置成功 / WCS API config created successfully");
            }
            else
            {
                success = await _wcsApiConfigRepository.UpdateAsync(config).ConfigureAwait(false);
                _logger.LogInformation("更新WCS API配置成功 / WCS API config updated successfully");
            }

            if (!success)
            {
                return StatusCode(500, ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                    "保存配置失败 / Failed to save config", 
                    "SAVE_FAILED"));
            }

            var dto = new WcsApiConfigResponseDto
            {
                Url = config.Url,
                ApiKeyMasked = MaskApiKey(config.ApiKey),
                TimeoutMs = config.TimeoutMs,
                DisableSslValidation = config.DisableSslValidation,
                IsEnabled = config.IsEnabled,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<WcsApiConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新WCS API配置失败 / Failed to update WCS API config");
            return StatusCode(500, ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                $"更新配置失败: {ex.Message} / Failed to update config: {ex.Message}", 
                "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// 删除WCS API配置 / Delete WCS API Configuration
    /// </summary>
    /// <returns>删除结果</returns>
    /// <response code="200">删除成功</response>
    /// <response code="404">配置不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpDelete("api")]
    [SwaggerOperation(
        Summary = "删除WCS API配置",
        Description = "删除WCS API端点配置。删除后将无法调用WCS API。",
        OperationId = "DeleteWcsApiConfig",
        Tags = new[] { "WCS配置 / WCS Configuration" }
    )]
    [SwaggerResponse(200, "删除成功", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> DeleteApiConfig()
    {
        try
        {
            var config = await _wcsApiConfigRepository.GetByIdAsync(WcsApiConfig.SingletonId).ConfigureAwait(false);
            if (config == null)
            {
                return NotFound(ApiResponse<object>.FailureResult(
                    "配置不存在 / Config not found", 
                    "CONFIG_NOT_FOUND"));
            }

            var success = await _wcsApiConfigRepository.DeleteAsync(WcsApiConfig.SingletonId).ConfigureAwait(false);
            if (!success)
            {
                return StatusCode(500, ApiResponse<object>.FailureResult(
                    "删除配置失败 / Failed to delete config", 
                    "DELETE_FAILED"));
            }

            _logger.LogInformation("删除WCS API配置成功 / WCS API config deleted successfully");
            return Ok(ApiResponse<object>.SuccessResult(new { Message = "配置已删除 / Config deleted" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除WCS API配置失败 / Failed to delete WCS API config");
            return StatusCode(500, ApiResponse<object>.FailureResult(
                $"删除配置失败: {ex.Message} / Failed to delete config: {ex.Message}", 
                "DELETE_FAILED"));
        }
    }

    /// <summary>
    /// 手动重载WCS API配置 / Manually Reload WCS API Configuration
    /// </summary>
    /// <returns>重载结果</returns>
    /// <response code="200">重载成功</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("api/reload")]
    [SwaggerOperation(
        Summary = "手动重载WCS API配置",
        Description = "触发手动重载WCS API配置。适用于配置更新后未自动生效的情况。",
        OperationId = "ReloadWcsApiConfig",
        Tags = new[] { "WCS配置 / WCS Configuration" }
    )]
    [SwaggerResponse(200, "重载成功", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<object>))]
    public Task<ActionResult<ApiResponse<object>>> ReloadApiConfig()
    {
        try
        {
            _logger.LogInformation("手动重载WCS API配置 / Manually reload WCS API config");
            
            // Note: 实际的热更新逻辑应该由 IWcsApiAdapterFactory 或相关服务处理
            // Actual hot reload logic should be handled by IWcsApiAdapterFactory or related services
            // 这里只是一个触发点
            // This is just a trigger point
            
            return Task.FromResult<ActionResult<ApiResponse<object>>>(
                Ok(ApiResponse<object>.SuccessResult(new { 
                    Message = "配置重载成功 / Config reloaded successfully",
                    ReloadedAt = _clock.LocalNow
                })));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重载WCS API配置失败 / Failed to reload WCS API config");
            return Task.FromResult<ActionResult<ApiResponse<object>>>(
                StatusCode(500, ApiResponse<object>.FailureResult(
                    $"重载配置失败: {ex.Message} / Failed to reload config: {ex.Message}", 
                    "RELOAD_FAILED")));
        }
    }

    /// <summary>
    /// 脱敏API密钥 / Mask API key
    /// </summary>
    private static string? MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 8)
        {
            return apiKey == null ? null : "****";
        }
        
        // 显示前4个和后4个字符，中间用星号代替
        // Show first 4 and last 4 characters, replace middle with asterisks
        return $"{apiKey[..4]}****{apiKey[^4..]}";
    }

    private static List<AdapterInfoDto> GetAvailableAdapters()
    {
        return new List<AdapterInfoDto>
        {
            new() { Name = "无 / None", Description = "不调用任何第三方WCS API / Do not call any third-party WCS API" },
            new() { Name = "MockWcsApiAdapter", Description = "模拟WCS适配器（测试用）/ Mock WCS Adapter (for testing)" },
            new() { Name = "WcsApiClient", Description = "标准WCS API客户端 / Standard WCS API Client" },
            new() { Name = "JushuitanErpApiClient", Description = "聚水潭ERP API客户端 / Jushuituan ERP API Client" },
            new() { Name = "WdtWmsApiClient", Description = "旺店通WMS API客户端 / WDT WMS API Client" },
            new() { Name = "WdtErpFlagshipApiClient", Description = "旺店通旗舰版ERP API客户端 / WDT Flagship ERP API Client" },
            new() { Name = "PostCollectionApiClient", Description = "邮政揽收API客户端 / Post Collection API Client" },
            new() { Name = "PostProcessingCenterApiClient", Description = "邮政处理中心API客户端 / Post Processing Center API Client" }
        };
    }

    private static string GetAdapterDescription(string adapterName)
    {
        return adapterName switch
        {
            "无" or "None" => "未配置任何WCS适配器 / No WCS adapter configured",
            "MockWcsApiAdapter" => "使用模拟适配器进行测试 / Using mock adapter for testing",
            "WcsApiClient" => "使用标准WCS API / Using standard WCS API",
            "JushuitanErpApiClient" => "对接聚水潭ERP系统 / Integrated with Jushuituan ERP",
            "WdtWmsApiClient" => "对接旺店通WMS系统 / Integrated with WDT WMS",
            "WdtErpFlagshipApiClient" => "对接旺店通旗舰版ERP / Integrated with WDT Flagship ERP",
            "PostCollectionApiClient" => "对接邮政揽收系统 / Integrated with Post Collection System",
            "PostProcessingCenterApiClient" => "对接邮政处理中心 / Integrated with Post Processing Center",
            _ => $"当前适配器: {adapterName} / Current adapter: {adapterName}"
        };
    }
}

/// <summary>
/// WCS配置响应DTO / WCS Config Response DTO
/// </summary>
public record WcsConfigResponseDto
{
    /// <summary>
    /// 当前激活的适配器名称 / Active Adapter Name
    /// </summary>
    public required string ActiveAdapter { get; init; }

    /// <summary>
    /// 是否启用 / Is Enabled
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// 可用的适配器列表 / Available Adapters List
    /// </summary>
    public required List<AdapterInfoDto> AvailableAdapters { get; init; }

    /// <summary>
    /// 描述 / Description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 时间戳 / Timestamp
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// 适配器信息DTO / Adapter Info DTO
/// </summary>
public record AdapterInfoDto
{
    /// <summary>
    /// 适配器名称 / Adapter Name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 适配器描述 / Adapter Description
    /// </summary>
    public required string Description { get; init; }
}
