using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 分拣机配置管理控制器（单例模式）
/// Sorter configuration management controller (Singleton pattern)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("分拣机配置管理接口")]
public class SorterConfigController : ControllerBase
{
    private readonly ISorterConfigRepository _repository;
    private readonly IConfigReloadService _reloadService;
    private readonly ILogger<SorterConfigController> _logger;

    public SorterConfigController(
        ISorterConfigRepository repository,
        IConfigReloadService reloadService,
        ILogger<SorterConfigController> logger)
    {
        _repository = repository;
        _reloadService = reloadService;
        _logger = logger;
    }

    /// <summary>
    /// 获取分拣机配置（单例）
    /// Get sorter configuration (singleton)
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取分拣机配置",
        Description = "获取系统中唯一的分拣机配置（单例模式）",
        OperationId = "GetSorterConfig",
        Tags = new[] { "SorterConfig" }
    )]
    [SwaggerResponse(200, "成功返回分拣机配置", typeof(ApiResponse<SorterConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<SorterConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<SorterConfigResponseDto>>> Get()
    {
        try
        {
            var config = await _repository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                // 返回默认配置
                var defaultConfig = GetDefaultConfig();
                return Ok(ApiResponse<SorterConfigResponseDto>.SuccessResult(defaultConfig));
            }
            
            var dto = config.ToResponseDto();
            return Ok(ApiResponse<SorterConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分拣机配置时发生错误");
            return StatusCode(500, ApiResponse<SorterConfigResponseDto>.FailureResult(
                "获取分拣机配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新分拣机配置（Upsert）
    /// Update sorter configuration (Upsert)
    /// </summary>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新分拣机配置",
        Description = "更新分拣机配置，如果不存在则创建（单例模式，全量更新）",
        OperationId = "UpdateSorterConfig",
        Tags = new[] { "SorterConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<SorterConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<SorterConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<SorterConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<SorterConfigResponseDto>>> Update(
        [FromBody, SwaggerRequestBody("分拣机配置更新请求", Required = true)] SorterConfigUpdateRequest request)
    {
        try
        {
            // 从请求创建实体（自动设置单例ID）
            var config = request.ToEntity();
            
            // Upsert操作：如果存在则更新，否则插入
            var success = await _repository.UpsertAsync(config).ConfigureAwait(false);
            
            if (success)
            {
                _logger.LogInformation("分拣机配置已更新");
                
                // 触发配置热更新
                try
                {
                    await _reloadService.ReloadSorterConfigAsync().ConfigureAwait(false);
                    _logger.LogInformation("分拣机配置热更新已触发");
                }
                catch (Exception reloadEx)
                {
                    _logger.LogWarning(reloadEx, "配置热更新失败，但配置已保存");
                }
                
                var dto = config.ToResponseDto();
                return Ok(ApiResponse<SorterConfigResponseDto>.SuccessResult(dto));
            }
            
            return BadRequest(ApiResponse<SorterConfigResponseDto>.FailureResult(
                "更新分拣机配置失败", "UPDATE_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新分拣机配置时发生错误");
            return StatusCode(500, ApiResponse<SorterConfigResponseDto>.FailureResult(
                "更新分拣机配置失败", "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// 获取默认配置
    /// Get default configuration
    /// </summary>
    private static SorterConfigResponseDto GetDefaultConfig()
    {
        return new SorterConfigResponseDto
        {
            Name = "默认分拣机配置",
            Protocol = "TCP",
            Host = "127.0.0.1",
            Port = 8888,
            IsEnabled = false,
            TimeoutSeconds = 30,
            AutoReconnect = true,
            ReconnectIntervalSeconds = 5,
            HeartbeatIntervalSeconds = 10,
            Description = "默认的分拣机通信配置",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}
