using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// WCS适配器选择控制器 / WCS Adapter Selection Controller
/// </summary>
/// <remarks>
/// 本控制器仅负责选择使用哪个WCS适配器，不包含具体API配置。
/// 具体的API配置（URL、ApiKey等）请使用 ApiClientConfigController。
/// This controller only handles WCS adapter selection, not specific API configurations.
/// For specific API configurations (URL, ApiKey, etc.), use ApiClientConfigController.
/// </remarks>
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
    /// 更新WCS适配器配置（支持热更新）/ Update WCS Adapter Configuration (with hot reload)
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    /// <response code="200">更新成功，适配器已切换</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 更新配置后，系统会立即切换到指定的WCS适配器，无需重启应用程序。
    /// 
    /// 示例请求:
    /// 
    ///     PUT /api/WcsConfig
    ///     {
    ///       "activeAdapter": "JushuitanErpApiClient",
    ///       "isEnabled": true,
    ///       "description": "切换到聚水潭ERP"
    ///     }
    /// </remarks>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新WCS适配器配置",
        Description = "更新当前激活的WCS适配器并触发热更新。支持在运行时动态切换不同的WCS API客户端（如聚水潭ERP、旺店通WMS等），无需重启应用。",
        OperationId = "UpdateWcsConfig",
        Tags = new[] { "WCS配置 / WCS Configuration" }
    )]
    [SwaggerResponse(200, "更新成功", typeof(ApiResponse<WcsConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<WcsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WcsConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<WcsConfigResponseDto>>> UpdateConfig(
        [FromBody, SwaggerRequestBody("WCS适配器配置更新请求", Required = true)] WcsConfigUpdateRequest request)
    {
        try
        {
            // 验证适配器名称
            var availableAdapters = GetAvailableAdapters();
            var isValidAdapter = availableAdapters.Any(a => 
                a.Name == request.ActiveAdapter || 
                a.Name.Contains(request.ActiveAdapter, StringComparison.OrdinalIgnoreCase));
            
            if (!isValidAdapter)
            {
                return BadRequest(ApiResponse<WcsConfigResponseDto>.FailureResult(
                    $"无效的适配器名称: {request.ActiveAdapter}。可用适配器: {string.Join(", ", availableAdapters.Select(a => a.Name))} / Invalid adapter name", 
                    "INVALID_ADAPTER"));
            }

            var now = _clock.LocalNow;
            
            // 检查配置是否存在
            var existingConfig = await _wcsApiConfigRepository.GetByIdAsync(WcsApiConfig.SingletonId).ConfigureAwait(false);
            
            // 保留原有的 URL、ApiKey 等配置，只更新 ActiveAdapterType
            var config = new WcsApiConfig
            {
                ConfigId = WcsApiConfig.SingletonId,
                ActiveAdapterType = request.ActiveAdapter,
                Url = existingConfig?.Url ?? "http://localhost",
                ApiKey = existingConfig?.ApiKey,
                TimeoutMs = existingConfig?.TimeoutMs ?? 30000,
                DisableSslValidation = existingConfig?.DisableSslValidation ?? false,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _wcsApiConfigRepository.AddAsync(config).ConfigureAwait(false);
                _logger.LogInformation("创建WCS适配器配置: {Adapter} / Created WCS adapter config", request.ActiveAdapter);
            }
            else
            {
                success = await _wcsApiConfigRepository.UpdateAsync(config).ConfigureAwait(false);
                _logger.LogInformation("更新WCS适配器配置: {OldAdapter} -> {NewAdapter} / Updated WCS adapter config", 
                    existingConfig.ActiveAdapterType, request.ActiveAdapter);
            }

            if (!success)
            {
                return StatusCode(500, ApiResponse<WcsConfigResponseDto>.FailureResult(
                    "保存配置失败 / Failed to save config", 
                    "SAVE_FAILED"));
            }

            // 触发适配器工厂重新加载（热更新）
            // Trigger adapter factory to reload (hot update)
            _wcsApiAdapterFactory.InvalidateCache();
            _logger.LogInformation("WCS适配器配置已更新并触发热更新 / WCS adapter config updated and hot reload triggered");

            var dto = new WcsConfigResponseDto
            {
                ActiveAdapter = request.ActiveAdapter,
                IsEnabled = request.IsEnabled,
                AvailableAdapters = availableAdapters,
                Description = GetAdapterDescription(request.ActiveAdapter),
                Timestamp = now
            };

            return Ok(ApiResponse<WcsConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新WCS适配器配置失败 / Failed to update WCS adapter config");
            return StatusCode(500, ApiResponse<WcsConfigResponseDto>.FailureResult(
                $"更新配置失败: {ex.Message} / Failed to update config: {ex.Message}", 
                "UPDATE_FAILED"));
        }
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
