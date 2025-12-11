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
/// WCS API配置管理控制器（单例模式）
/// WCS API configuration management controller (Singleton pattern)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("WCS API配置管理接口")]
public class WcsApiConfigController : ControllerBase
{
    private readonly IWcsApiConfigRepository _repository;
    private readonly IConfigReloadService _reloadService;
    private readonly ILogger<WcsApiConfigController> _logger;

    public WcsApiConfigController(
        IWcsApiConfigRepository repository,
        IConfigReloadService reloadService,
        ILogger<WcsApiConfigController> logger)
    {
        _repository = repository;
        _reloadService = reloadService;
        _logger = logger;
    }

    /// <summary>
    /// 获取WCS API配置（单例）
    /// Get WCS API configuration (singleton)
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取WCS API配置",
        Description = "获取系统中唯一的WCS API配置（单例模式，API密钥已脱敏）",
        OperationId = "GetWcsApiConfig",
        Tags = new[] { "WcsApiConfig" }
    )]
    [SwaggerResponse(200, "成功返回WCS API配置", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<WcsApiConfigResponseDto>>> Get()
    {
        try
        {
            var config = await _repository.GetByIdAsync(WcsApiConfig.SINGLETON_ID);
            
            if (config == null)
            {
                // 返回默认配置
                var defaultConfig = GetDefaultConfig();
                return Ok(ApiResponse<WcsApiConfigResponseDto>.SuccessResult(defaultConfig));
            }
            
            var dto = config.ToResponseDto();
            return Ok(ApiResponse<WcsApiConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取WCS API配置时发生错误");
            return StatusCode(500, ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                "获取WCS API配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新WCS API配置（Upsert）
    /// Update WCS API configuration (Upsert)
    /// </summary>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新WCS API配置",
        Description = "更新WCS API配置，如果不存在则创建（单例模式，全量更新）",
        OperationId = "UpdateWcsApiConfig",
        Tags = new[] { "WcsApiConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<WcsApiConfigResponseDto>>> Update(
        [FromBody, SwaggerRequestBody("WCS API配置更新请求", Required = true)] WcsApiConfigUpdateRequest request)
    {
        try
        {
            // 从请求创建实体（自动设置单例ID）
            var config = request.ToEntity();
            
            // 检查现有配置
            var existing = await _repository.GetByIdAsync(WcsApiConfig.SINGLETON_ID);
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
                _logger.LogInformation("WCS API配置已更新");
                
                // 触发配置热更新
                try
                {
                    await _reloadService.ReloadWcsConfigAsync();
                    _logger.LogInformation("WCS API配置热更新已触发");
                }
                catch (Exception reloadEx)
                {
                    _logger.LogWarning(reloadEx, "配置热更新失败，但配置已保存");
                }
                
                var dto = config.ToResponseDto();
                return Ok(ApiResponse<WcsApiConfigResponseDto>.SuccessResult(dto));
            }
            
            return BadRequest(ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                "更新WCS API配置失败", "UPDATE_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新WCS API配置时发生错误");
            return StatusCode(500, ApiResponse<WcsApiConfigResponseDto>.FailureResult(
                "更新WCS API配置失败", "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// 获取默认配置
    /// Get default configuration
    /// </summary>
    private static WcsApiConfigResponseDto GetDefaultConfig()
    {
        return new WcsApiConfigResponseDto
        {
            Name = "默认WCS API",
            BaseUrl = "http://localhost:8080",
            TimeoutSeconds = 30,
            ApiKey = null,
            CustomHeaders = null,
            HttpMethod = "POST",
            IsEnabled = false,
            Priority = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}
