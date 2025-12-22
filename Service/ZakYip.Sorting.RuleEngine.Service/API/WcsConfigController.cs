using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
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
    private readonly ILogger<WcsConfigController> _logger;
    private readonly ISystemClock _clock;

    public WcsConfigController(
        IWcsApiAdapterFactory wcsApiAdapterFactory,
        ILogger<WcsConfigController> logger,
        ISystemClock clock)
    {
        _wcsApiAdapterFactory = wcsApiAdapterFactory;
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
