using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS数据接收超时配置管理控制器（单例模式）/ DWS data reception timeout configuration management controller (Singleton pattern)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("DWS超时配置管理接口 / DWS timeout configuration management API")]
public class DwsTimeoutController : ControllerBase
{
    private readonly ISystemClock _clock;
    private readonly IDwsTimeoutConfigRepository _repository;
    private readonly IConfigReloadService _reloadService;
    private readonly ILogger<DwsTimeoutController> _logger;

    public DwsTimeoutController(
        IDwsTimeoutConfigRepository repository,
        IConfigReloadService reloadService,
        ILogger<DwsTimeoutController> logger,
        ISystemClock clock)
    {
        _repository = repository;
        _reloadService = reloadService;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 获取DWS超时配置（单例）/ Get DWS timeout configuration (singleton)
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取DWS超时配置 / Get DWS timeout configuration",
        Description = "获取系统中唯一的DWS超时配置（单例模式）/ Get the unique DWS timeout configuration in the system (singleton pattern)",
        OperationId = "GetDwsTimeoutConfig",
        Tags = new[] { "DwsTimeout" }
    )]
    [SwaggerResponse(200, "成功返回DWS超时配置", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsTimeoutConfigResponseDto>>> Get()
    {
        try
        {
            var config = await _repository.GetByIdAsync(DwsTimeoutConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                // 返回默认配置 / Return default configuration
                var defaultConfig = GetDefaultConfig();
                return Ok(ApiResponse<DwsTimeoutConfigResponseDto>.SuccessResult(defaultConfig));
            }
            
            var dto = ToResponseDto(config);
            return Ok(ApiResponse<DwsTimeoutConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS超时配置时发生错误 / Error occurred while getting DWS timeout configuration");
            return StatusCode(500, ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                "获取DWS超时配置失败 / Failed to get DWS timeout configuration", 
                "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新DWS超时配置（Upsert）/ Update DWS timeout configuration (Upsert)
    /// </summary>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新DWS超时配置 / Update DWS timeout configuration",
        Description = "更新DWS数据接收超时配置，如果不存在则创建（单例模式，全量更新）/ Update DWS timeout configuration, create if not exists (singleton pattern, full update)",
        OperationId = "UpdateDwsTimeoutConfig",
        Tags = new[] { "DwsTimeout" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsTimeoutConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsTimeoutConfigResponseDto>>> Update(
        [FromBody, SwaggerRequestBody("DWS超时配置更新请求", Required = true)] DwsTimeoutUpdateRequest request)
    {
        try
        {
            // 验证参数 / Validate parameters
            if (request.MinDwsWaitSeconds < 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "最小等待时间不能小于0 / MinDwsWaitSeconds cannot be less than 0",
                    "INVALID_MIN_WAIT_TIME"));
            }

            if (request.MaxDwsWaitSeconds <= request.MinDwsWaitSeconds)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "最大等待时间必须大于最小等待时间 / MaxDwsWaitSeconds must be greater than MinDwsWaitSeconds",
                    "INVALID_MAX_WAIT_TIME"));
            }

            if (request.ExceptionChuteId < 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "异常格口ID不能小于0 / ExceptionChuteId cannot be less than 0",
                    "INVALID_EXCEPTION_CHUTE_ID"));
            }

            if (request.CheckIntervalSeconds <= 0)
            {
                return BadRequest(ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "检查间隔必须大于0 / CheckIntervalSeconds must be greater than 0",
                    "INVALID_CHECK_INTERVAL"));
            }

            // 从请求创建实体（自动设置单例ID）/ Create entity from request (auto-set singleton ID)
            var config = ToEntity(request);
            
            // 检查现有配置 / Check existing configuration
            var existing = await _repository.GetByIdAsync(DwsTimeoutConfig.SingletonId).ConfigureAwait(false);
            bool success;

            if (existing == null)
            {
                // 创建新配置 / Create new configuration
                success = await _repository.UpsertAsync(config).ConfigureAwait(false);
                _logger.LogInformation("创建DWS超时配置成功 / DWS timeout configuration created successfully");
            }
            else
            {
                // 更新现有配置，保留原始创建时间 / Update existing configuration, preserve original CreatedAt
                config = config with { CreatedAt = existing.CreatedAt };
                success = await _repository.UpsertAsync(config).ConfigureAwait(false);
                _logger.LogInformation("更新DWS超时配置成功 / DWS timeout configuration updated successfully");
            }

            if (!success)
            {
                return StatusCode(500, ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                    "保存DWS超时配置失败 / Failed to save DWS timeout configuration",
                    "SAVE_FAILED"));
            }

            // 触发配置重新加载 / Trigger configuration reload
            await _reloadService.ReloadDwsTimeoutConfigAsync().ConfigureAwait(false);

            var dto = ToResponseDto(config);
            return Ok(ApiResponse<DwsTimeoutConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS超时配置时发生错误 / Error occurred while updating DWS timeout configuration");
            return StatusCode(500, ApiResponse<DwsTimeoutConfigResponseDto>.FailureResult(
                "更新DWS超时配置失败 / Failed to update DWS timeout configuration",
                "UPDATE_CONFIG_FAILED"));
        }
    }

    private DwsTimeoutConfigResponseDto GetDefaultConfig()
    {
        return new DwsTimeoutConfigResponseDto
        {
            ConfigId = DwsTimeoutConfig.SingletonId,
            Enabled = true,
            MinDwsWaitSeconds = 2,
            MaxDwsWaitSeconds = 30,
            ExceptionChuteId = 999, // 999 表示未分配异常格口 / 999 means "unassigned exception chute"
            CheckIntervalSeconds = 5,
            Description = "默认DWS超时配置（异常格口未分配）/ Default DWS timeout configuration (exception chute unassigned)",
            CreatedAt = _clock.LocalNow,
            UpdatedAt = _clock.LocalNow
        };
    }

    private DwsTimeoutConfig ToEntity(DwsTimeoutUpdateRequest request)
    {
        return new DwsTimeoutConfig
        {
            ConfigId = DwsTimeoutConfig.SingletonId,
            Enabled = request.Enabled,
            MinDwsWaitSeconds = request.MinDwsWaitSeconds,
            MaxDwsWaitSeconds = request.MaxDwsWaitSeconds,
            ExceptionChuteId = request.ExceptionChuteId,
            CheckIntervalSeconds = request.CheckIntervalSeconds,
            Description = request.Description,
            CreatedAt = _clock.LocalNow,
            UpdatedAt = _clock.LocalNow
        };
    }

    private static DwsTimeoutConfigResponseDto ToResponseDto(DwsTimeoutConfig config)
    {
        return new DwsTimeoutConfigResponseDto
        {
            ConfigId = config.ConfigId,
            Enabled = config.Enabled,
            MinDwsWaitSeconds = config.MinDwsWaitSeconds,
            MaxDwsWaitSeconds = config.MaxDwsWaitSeconds,
            ExceptionChuteId = config.ExceptionChuteId,
            CheckIntervalSeconds = config.CheckIntervalSeconds,
            Description = config.Description,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }
}

/// <summary>
/// DWS超时配置响应DTO / DWS timeout configuration response DTO
/// </summary>
public record DwsTimeoutConfigResponseDto
{
    /// <summary>
    /// 配置ID / Configuration ID
    /// </summary>
    public long ConfigId { get; init; }
    
    /// <summary>
    /// 是否启用超时检查 / Enable timeout check
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// 最小等待时间（秒）/ Minimum wait time (seconds)
    /// </summary>
    public int MinDwsWaitSeconds { get; init; }

    /// <summary>
    /// 最大等待时间（秒）/ Maximum wait time (seconds)
    /// </summary>
    public int MaxDwsWaitSeconds { get; init; }

    /// <summary>
    /// 异常格口ID / Exception chute ID
    /// </summary>
    public long ExceptionChuteId { get; init; }

    /// <summary>
    /// 超时检查间隔（秒）/ Timeout check interval (seconds)
    /// </summary>
    public int CheckIntervalSeconds { get; init; }
    
    /// <summary>
    /// 备注说明 / Description
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 创建时间 / Created time
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// 最后更新时间 / Last updated time
    /// </summary>
    public DateTime UpdatedAt { get; init; }
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
    
    /// <summary>
    /// 备注说明 / Description
    /// </summary>
    /// <example>DWS超时配置</example>
    public string? Description { get; init; }
}
