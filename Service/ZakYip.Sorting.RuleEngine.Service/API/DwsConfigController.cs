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
/// DWS配置管理控制器（单例模式）
/// DWS configuration management controller (Singleton pattern)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("DWS配置管理接口")]
public class DwsConfigController : ControllerBase
{
    private readonly IDwsConfigRepository _repository;
    private readonly IConfigReloadService _reloadService;
    private readonly ILogger<DwsConfigController> _logger;

    public DwsConfigController(
        IDwsConfigRepository repository,
        IConfigReloadService reloadService,
        ILogger<DwsConfigController> logger)
    {
        _repository = repository;
        _reloadService = reloadService;
        _logger = logger;
    }

    /// <summary>
    /// 获取DWS配置（单例）
    /// Get DWS configuration (singleton)
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取DWS配置",
        Description = "获取系统中唯一的DWS配置（单例模式）",
        OperationId = "GetDwsConfig",
        Tags = new[] { "DwsConfig" }
    )]
    [SwaggerResponse(200, "成功返回DWS配置", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsConfigResponseDto>>> Get()
    {
        try
        {
            var config = await _repository.GetByIdAsync(DwsConfig.SINGLETON_ID);
            
            if (config == null)
            {
                // 返回默认配置
                var defaultConfig = GetDefaultConfig();
                return Ok(ApiResponse<DwsConfigResponseDto>.SuccessResult(defaultConfig));
            }
            
            var dto = config.ToResponseDto();
            return Ok(ApiResponse<DwsConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS配置时发生错误");
            return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult(
                "获取DWS配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新DWS配置（Upsert）
    /// Update DWS configuration (Upsert)
    /// </summary>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新DWS配置",
        Description = "更新DWS配置，如果不存在则创建（单例模式，全量更新）",
        OperationId = "UpdateDwsConfig",
        Tags = new[] { "DwsConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsConfigResponseDto>>> Update(
        [FromBody, SwaggerRequestBody("DWS配置更新请求", Required = true)] DwsConfigUpdateRequest request)
    {
        try
        {
            // 从请求创建实体（自动设置单例ID）
            var config = request.ToEntity();
            
            // 检查现有配置
            var existing = await _repository.GetByIdAsync(DwsConfig.SINGLETON_ID);
            bool success;
            
            if (existing == null)
            {
                success = await _repository.AddAsync(config);
            }
            else
            {
                // 保留原创建时间
                config = config with { CreatedAt = existing.CreatedAt };
                success = await _repository.UpdateAsync(config);
            }
            
            if (success)
            {
                _logger.LogInformation("DWS配置已更新");
                
                // 触发配置热更新
                try
                {
                    await _reloadService.ReloadDwsConfigAsync();
                    _logger.LogInformation("DWS配置热更新已触发");
                }
                catch (Exception reloadEx)
                {
                    _logger.LogWarning(reloadEx, "配置热更新失败，但配置已保存");
                }
                
                var dto = config.ToResponseDto();
                return Ok(ApiResponse<DwsConfigResponseDto>.SuccessResult(dto, "配置已更新并重新加载"));
            }
            
            return BadRequest(ApiResponse<DwsConfigResponseDto>.FailureResult(
                "更新DWS配置失败", "UPDATE_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS配置时发生错误");
            return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult(
                "更新DWS配置失败", "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// 获取默认配置
    /// Get default configuration
    /// </summary>
    private static DwsConfigResponseDto GetDefaultConfig()
    {
        return new DwsConfigResponseDto
        {
            Name = "默认DWS配置",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8081,
            DataTemplateId = 1L,
            IsEnabled = false,
            MaxConnections = 1000,
            ReceiveBufferSize = 8192,
            SendBufferSize = 8192,
            TimeoutSeconds = 30,
            AutoReconnect = true,
            ReconnectIntervalSeconds = 5,
            Description = "默认的DWS通信配置",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}
