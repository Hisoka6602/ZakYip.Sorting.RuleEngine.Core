using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;
using ZakYip.Sorting.RuleEngine.Service.Configuration;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS数据接收超时配置管理控制器 / DWS data reception timeout configuration management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("DWS超时配置管理接口 / DWS timeout configuration management API")]
public class DwsTimeoutController : ControllerBase
{
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly ILogger<DwsTimeoutController> _logger;

    public DwsTimeoutController(
        IOptionsMonitor<AppSettings> appSettings,
        ILogger<DwsTimeoutController> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
    }

    /// <summary>
    /// 获取DWS数据接收超时配置 / Get DWS data reception timeout configuration
    /// </summary>
    /// <returns>DWS超时配置 / DWS timeout configuration</returns>
    /// <response code="200">成功返回DWS超时配置 / Successfully returns DWS timeout configuration</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取DWS超时配置 / Get DWS timeout configuration",
        Description = "获取DWS数据接收超时配置，包括最小/最大等待时间、异常格口ID等 / Get DWS data reception timeout configuration including min/max wait times, exception chute ID, etc.",
        OperationId = "GetDwsTimeoutConfig",
        Tags = new[] { "DwsTimeout" }
    )]
    [SwaggerResponse(200, "成功返回DWS超时配置", typeof(ApiResponse<DwsTimeoutSettings>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsTimeoutSettings>))]
    public ActionResult<ApiResponse<DwsTimeoutSettings>> Get()
    {
        try
        {
            var settings = _appSettings.CurrentValue.DwsTimeout;
            return Ok(ApiResponse<DwsTimeoutSettings>.SuccessResult(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS超时配置时发生错误 / Error occurred while getting DWS timeout configuration");
            return StatusCode(500, ApiResponse<DwsTimeoutSettings>.FailureResult(
                "获取DWS超时配置失败 / Failed to get DWS timeout configuration", 
                "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新DWS数据接收超时配置 / Update DWS data reception timeout configuration
    /// </summary>
    /// <param name="request">超时配置更新请求 / Timeout configuration update request</param>
    /// <returns>更新后的配置 / Updated configuration</returns>
    /// <response code="200">配置更新成功 / Configuration updated successfully</response>
    /// <response code="400">请求参数错误 / Invalid request parameters</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    /// <remarks>
    /// 注意：此API仅更新内存中的配置，重启后会恢复到appsettings.json中的值。
    /// 如需持久化配置，请直接修改appsettings.json文件并重启服务。
    /// 
    /// Note: This API only updates in-memory configuration, which will revert to appsettings.json values after restart.
    /// For persistent configuration changes, directly modify the appsettings.json file and restart the service.
    /// </remarks>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新DWS超时配置 / Update DWS timeout configuration",
        Description = "更新DWS数据接收超时配置。注意：此操作仅更新内存中的配置，需要更新appsettings.json文件以持久化 / Update DWS data reception timeout configuration. Note: This only updates in-memory configuration, update appsettings.json file for persistence",
        OperationId = "UpdateDwsTimeoutConfig",
        Tags = new[] { "DwsTimeout" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<DwsTimeoutSettings>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<DwsTimeoutSettings>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsTimeoutSettings>))]
    public ActionResult<ApiResponse<DwsTimeoutSettings>> Update(
        [FromBody, SwaggerRequestBody("DWS超时配置更新请求", Required = true)] DwsTimeoutUpdateRequest request)
    {
        try
        {
            // 验证参数
            if (request.MinDwsWaitSeconds < 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutSettings>.FailureResult(
                    "最小等待时间不能小于0 / MinDwsWaitSeconds cannot be less than 0",
                    "INVALID_MIN_WAIT_TIME"));
            }

            if (request.MaxDwsWaitSeconds <= request.MinDwsWaitSeconds)
            {
                return BadRequest(ApiResponse<DwsTimeoutSettings>.FailureResult(
                    "最大等待时间必须大于最小等待时间 / MaxDwsWaitSeconds must be greater than MinDwsWaitSeconds",
                    "INVALID_MAX_WAIT_TIME"));
            }

            if (request.ExceptionChuteId < 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutSettings>.FailureResult(
                    "异常格口ID不能小于0 / ExceptionChuteId cannot be less than 0",
                    "INVALID_EXCEPTION_CHUTE_ID"));
            }

            if (request.CheckIntervalSeconds <= 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutSettings>.FailureResult(
                    "检查间隔必须大于0 / CheckIntervalSeconds must be greater than 0",
                    "INVALID_CHECK_INTERVAL"));
            }

            // 更新内存中的配置（仅在当前会话中有效）
            var currentSettings = _appSettings.CurrentValue.DwsTimeout;
            currentSettings.Enabled = request.Enabled;
            currentSettings.MinDwsWaitSeconds = request.MinDwsWaitSeconds;
            currentSettings.MaxDwsWaitSeconds = request.MaxDwsWaitSeconds;
            currentSettings.ExceptionChuteId = request.ExceptionChuteId;
            currentSettings.CheckIntervalSeconds = request.CheckIntervalSeconds;

            _logger.LogInformation(
                "DWS超时配置已更新: Enabled={Enabled}, MinWait={MinWait}s, MaxWait={MaxWait}s, ExceptionChute={ExceptionChute}, CheckInterval={CheckInterval}s / " +
                "DWS timeout configuration updated: Enabled={Enabled}, MinWait={MinWait}s, MaxWait={MaxWait}s, ExceptionChute={ExceptionChute}, CheckInterval={CheckInterval}s",
                currentSettings.Enabled, currentSettings.MinDwsWaitSeconds, currentSettings.MaxDwsWaitSeconds,
                currentSettings.ExceptionChuteId, currentSettings.CheckIntervalSeconds);

            return Ok(ApiResponse<DwsTimeoutSettings>.SuccessResult(currentSettings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS超时配置时发生错误 / Error occurred while updating DWS timeout configuration");
            return StatusCode(500, ApiResponse<DwsTimeoutSettings>.FailureResult(
                "更新DWS超时配置失败 / Failed to update DWS timeout configuration",
                "UPDATE_CONFIG_FAILED"));
        }
    }
}

/// <summary>
/// DWS超时配置更新请求 / DWS timeout configuration update request
/// </summary>
public record DwsTimeoutUpdateRequest
{
    /// <summary>
    /// 是否启用超时检查 / Enable timeout check
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// 最小等待时间（秒）- 避免匹配上一个包裹的DWS数据 / Minimum wait time (seconds) - Avoid matching DWS data from previous parcel
    /// </summary>
    /// <example>2</example>
    public int MinDwsWaitSeconds { get; init; } = 2;

    /// <summary>
    /// 最大等待时间（秒）- 超时截止时间 / Maximum wait time (seconds) - Timeout deadline
    /// </summary>
    /// <example>30</example>
    public int MaxDwsWaitSeconds { get; init; } = 30;

    /// <summary>
    /// 异常格口ID - 当DWS数据接收超时时，分配到此格口 / Exception chute ID - Assign to this chute when DWS data reception times out
    /// </summary>
    /// <example>999</example>
    public long ExceptionChuteId { get; init; } = 0;

    /// <summary>
    /// 超时检查间隔（秒）- 后台任务检查超时包裹的频率 / Timeout check interval (seconds) - Frequency of background task checking for timed-out parcels
    /// </summary>
    /// <example>5</example>
    public int CheckIntervalSeconds { get; init; } = 5;
}
