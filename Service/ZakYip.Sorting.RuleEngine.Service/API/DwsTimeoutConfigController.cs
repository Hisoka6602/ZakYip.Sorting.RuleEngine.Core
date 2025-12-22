using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS包裹绑定超时配置控制器
/// DWS Parcel Binding Timeout Configuration Controller
/// </summary>
[ApiController]
[Route("api/Dws/TimeoutConfig")]
[Produces("application/json")]
public class DwsTimeoutConfigController : ControllerBase
{
    private readonly IDwsTimeoutConfigRepository _repository;
    private readonly ILogger<DwsTimeoutConfigController> _logger;
    private readonly ISystemClock _clock;

    public DwsTimeoutConfigController(
        IDwsTimeoutConfigRepository repository,
        ILogger<DwsTimeoutConfigController> logger,
        ISystemClock clock)
    {
        _repository = repository;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 获取DWS包裹绑定超时配置
    /// Get DWS parcel binding timeout configuration
    /// </summary>
    /// <returns>当前超时配置 / Current timeout configuration</returns>
    /// <response code="200">成功返回配置 / Successfully returns configuration</response>
    /// <response code="404">配置不存在 / Configuration not found</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    /// <remarks>
    /// **配置说明 / Configuration Description**:
    /// 
    /// DWS包裹绑定有效时间窗口配置。例如：创建包裹后60ms~200ms内可以绑定DWS数据。
    /// 
    /// - **MinDwsWaitMilliseconds** (最小等待时间): 包裹创建后需要等待的最小时间才能绑定DWS数据，避免绑定到上一个包裹的DWS数据
    /// - **MaxDwsWaitMilliseconds** (最大等待时间): 包裹创建后的超时时间，超过此时间未绑定DWS数据则视为超时
    /// - **ExceptionChuteId** (异常格口ID): 超时包裹分配的目标格口（默认999）
    /// - **Enabled** (是否启用): 是否启用超时检查
    /// - **CheckIntervalMilliseconds** (检查间隔): 后台任务检查超时包裹的频率
    /// 
    /// **业务规则 / Business Rules**:
    /// 1. 创建包裹后，在 [MinDwsWaitMilliseconds, MaxDwsWaitMilliseconds] 时间窗口内可以绑定DWS数据
    /// 2. 早于MinDwsWaitMilliseconds的DWS数据会被拒绝（可能是上一个包裹的数据）
    /// 3. 晚于MaxDwsWaitMilliseconds的DWS数据会被拒绝（已超时）
    /// 4. 超过MaxDwsWaitMilliseconds仍未绑定DWS数据的包裹，会被分配默认占位值并发送到异常格口
    /// 5. 已绑定过DWS数据的包裹不能再次绑定
    /// </remarks>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取DWS包裹绑定超时配置",
        Description = "获取当前DWS包裹绑定的有效时间窗口配置（例如：60ms~200ms）",
        OperationId = "GetDwsTimeoutConfig",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsTimeoutConfigResponseDto>>> GetConfig()
    {
        try
        {
            var config = await _repository.GetByIdAsync(DwsTimeoutConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "DWS超时配置不存在，将使用默认值 / DWS timeout configuration not found, will use default values", 
                    "CONFIG_NOT_FOUND"));
            }

            var dto = new DwsTimeoutConfigResponseDto
            {
                Enabled = config.Enabled,
                MinDwsWaitMilliseconds = config.MinDwsWaitMilliseconds,
                MaxDwsWaitMilliseconds = config.MaxDwsWaitMilliseconds,
                ExceptionChuteId = config.ExceptionChuteId,
                CheckIntervalMilliseconds = config.CheckIntervalMilliseconds,
                Description = config.Description,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<DwsTimeoutConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS超时配置失败 / Failed to get DWS timeout configuration");
            return StatusCode(500, ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                "获取配置失败 / Failed to get configuration", 
                "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 更新DWS包裹绑定超时配置
    /// Update DWS parcel binding timeout configuration
    /// </summary>
    /// <param name="request">更新请求 / Update request</param>
    /// <returns>更新后的配置 / Updated configuration</returns>
    /// <response code="200">更新成功 / Successfully updated</response>
    /// <response code="400">请求参数无效 / Invalid request parameters</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    /// <remarks>
    /// **示例 / Example**:
    /// ```json
    /// {
    ///   "enabled": true,
    ///   "minDwsWaitMilliseconds": 60,
    ///   "maxDwsWaitMilliseconds": 200,
    ///   "exceptionChuteId": 999,
    ///   "checkIntervalMilliseconds": 100,
    ///   "description": "包裹创建后60-200ms内可以绑定DWS数据"
    /// }
    /// ```
    /// 
    /// **验证规则 / Validation Rules**:
    /// - MinDwsWaitMilliseconds 必须 >= 0
    /// - MaxDwsWaitMilliseconds 必须 > MinDwsWaitMilliseconds
    /// - ExceptionChuteId 必须 > 0
    /// - CheckIntervalMilliseconds 必须 > 0
    /// </remarks>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新DWS包裹绑定超时配置",
        Description = "更新DWS包裹绑定的有效时间窗口配置和异常格口",
        OperationId = "UpdateDwsTimeoutConfig",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "更新成功", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数无效", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsTimeoutConfigResponseDto>>> UpdateConfig(
        [FromBody] DwsTimeoutConfigUpdateRequest request)
    {
        try
        {
            // 参数验证
            if (request.MinDwsWaitMilliseconds < 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "最小等待时间不能为负数 / Minimum wait time cannot be negative", 
                    "INVALID_MIN_WAIT"));
            }

            if (request.MaxDwsWaitMilliseconds <= request.MinDwsWaitMilliseconds)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "最大等待时间必须大于最小等待时间 / Maximum wait time must be greater than minimum wait time", 
                    "INVALID_MAX_WAIT"));
            }

            if (request.ExceptionChuteId <= 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "异常格口ID必须大于0 / Exception chute ID must be greater than 0", 
                    "INVALID_CHUTE_ID"));
            }

            if (request.CheckIntervalMilliseconds <= 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "检查间隔必须大于0 / Check interval must be greater than 0", 
                    "INVALID_CHECK_INTERVAL"));
            }

            // 创建或更新配置
            var config = new DwsTimeoutConfig
            {
                ConfigId = DwsTimeoutConfig.SingletonId,
                Enabled = request.Enabled,
                MinDwsWaitMilliseconds = request.MinDwsWaitMilliseconds,
                MaxDwsWaitMilliseconds = request.MaxDwsWaitMilliseconds,
                ExceptionChuteId = request.ExceptionChuteId,
                CheckIntervalMilliseconds = request.CheckIntervalMilliseconds,
                Description = request.Description,
                CreatedAt = _clock.LocalNow,
                UpdatedAt = _clock.LocalNow
            };

            await _repository.UpsertAsync(config).ConfigureAwait(false);

            _logger.LogInformation(
                "DWS超时配置已更新: Min={MinWait}ms, Max={MaxWait}ms, ExceptionChute={ChuteId} / DWS timeout configuration updated",
                config.MinDwsWaitMilliseconds,
                config.MaxDwsWaitMilliseconds,
                config.ExceptionChuteId);

            var dto = new DwsTimeoutConfigResponseDto
            {
                Enabled = config.Enabled,
                MinDwsWaitMilliseconds = config.MinDwsWaitMilliseconds,
                MaxDwsWaitMilliseconds = config.MaxDwsWaitMilliseconds,
                ExceptionChuteId = config.ExceptionChuteId,
                CheckIntervalMilliseconds = config.CheckIntervalMilliseconds,
                Description = config.Description,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<DwsTimeoutConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS超时配置失败 / Failed to update DWS timeout configuration");
            return StatusCode(500, ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                "更新配置失败 / Failed to update configuration", 
                "INTERNAL_ERROR"));
        }
    }
}
