using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS配置管理控制器
/// DWS configuration management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("DWS配置管理接口")]
public class DwsConfigController : ControllerBase
{
    private readonly IDwsConfigRepository _repository;
    private readonly ILogger<DwsConfigController> _logger;

    public DwsConfigController(
        IDwsConfigRepository repository,
        ILogger<DwsConfigController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有DWS配置
    /// Get all DWS configurations
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取所有DWS配置",
        Description = "获取系统中所有DWS配置",
        OperationId = "GetAllDwsConfigs",
        Tags = new[] { "DwsConfig" }
    )]
    [SwaggerResponse(200, "成功返回DWS配置列表", typeof(ApiResponse<IEnumerable<DwsConfigResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<DwsConfigResponseDto>>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<DwsConfigResponseDto>>>> GetAll()
    {
        try
        {
            var configs = await _repository.GetAllAsync();
            var dtos = configs.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<DwsConfigResponseDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有DWS配置时发生错误");
            return StatusCode(500, ApiResponse<IEnumerable<DwsConfigResponseDto>>.FailureResult("获取DWS配置失败", "GET_CONFIGS_FAILED"));
        }
    }

    /// <summary>
    /// 获取所有启用的DWS配置
    /// Get all enabled DWS configurations
    /// </summary>
    [HttpGet("enabled")]
    [SwaggerOperation(
        Summary = "获取启用的DWS配置",
        Description = "获取系统中所有已启用的DWS配置",
        OperationId = "GetEnabledDwsConfigs",
        Tags = new[] { "DwsConfig" }
    )]
    [SwaggerResponse(200, "成功返回启用的DWS配置列表", typeof(ApiResponse<IEnumerable<DwsConfigResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<DwsConfigResponseDto>>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<DwsConfigResponseDto>>>> GetEnabled()
    {
        try
        {
            var configs = await _repository.GetEnabledConfigsAsync();
            var dtos = configs.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<DwsConfigResponseDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用的DWS配置时发生错误");
            return StatusCode(500, ApiResponse<IEnumerable<DwsConfigResponseDto>>.FailureResult("获取启用的DWS配置失败", "GET_ENABLED_CONFIGS_FAILED"));
        }
    }

    /// <summary>
    /// 根据ID获取DWS配置
    /// Get DWS configuration by ID
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "根据ID获取DWS配置",
        Description = "根据配置ID获取特定DWS配置的详细信息",
        OperationId = "GetDwsConfigById",
        Tags = new[] { "DwsConfig" }
    )]
    [SwaggerResponse(200, "成功返回DWS配置", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(404, "DWS配置未找到", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsConfigResponseDto>>> GetById(
        [SwaggerParameter("配置ID", Required = true)] string id)
    {
        try
        {
            var config = await _repository.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(ApiResponse<DwsConfigResponseDto>.FailureResult("DWS配置未找到", "CONFIG_NOT_FOUND"));
            }
            var dto = config.ToResponseDto();
            return Ok(ApiResponse<DwsConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS配置 {ConfigId} 时发生错误", id);
            return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult("获取DWS配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 创建新的DWS配置
    /// Create new DWS configuration
    /// </summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "创建DWS配置",
        Description = "创建新的DWS配置",
        OperationId = "CreateDwsConfig",
        Tags = new[] { "DwsConfig" }
    )]
    public async Task<ActionResult> Create([FromBody] DwsConfig config)
    {
        try
        {
            // Set timestamps at persistence layer
            var now = DateTime.Now;
            var configWithTimestamps = config with 
            { 
                CreatedAt = now, 
                UpdatedAt = now 
            };
            
            var success = await _repository.AddAsync(configWithTimestamps);
            if (success)
            {
                _logger.LogInformation("成功创建DWS配置: {ConfigId}", config.ConfigId);
                return CreatedAtAction(nameof(GetById), new { id = config.ConfigId }, configWithTimestamps);
            }
            return BadRequest(new { message = "创建DWS配置失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建DWS配置时发生错误");
            return StatusCode(500, new { message = "创建DWS配置失败" });
        }
    }

    /// <summary>
    /// 更新DWS配置
    /// Update DWS configuration
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "更新DWS配置",
        Description = "更新现有的DWS配置",
        OperationId = "UpdateDwsConfig",
        Tags = new[] { "DwsConfig" }
    )]
    public async Task<ActionResult> Update(string id, [FromBody] DwsConfig config)
    {
        try
        {
            if (id != config.ConfigId)
            {
                return BadRequest(new { message = "配置ID不匹配" });
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"未找到ID为 {id} 的DWS配置" });
            }

            var success = await _repository.UpdateAsync(config);
            if (success)
            {
                _logger.LogInformation("成功更新DWS配置: {ConfigId}", config.ConfigId);
                return Ok(new { message = "DWS配置更新成功" });
            }
            return BadRequest(new { message = "更新DWS配置失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS配置 {ConfigId} 时发生错误", id);
            return StatusCode(500, new { message = "更新DWS配置失败" });
        }
    }

    /// <summary>
    /// 删除DWS配置
    /// Delete DWS configuration
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "删除DWS配置",
        Description = "删除指定的DWS配置",
        OperationId = "DeleteDwsConfig",
        Tags = new[] { "DwsConfig" }
    )]
    public async Task<ActionResult> Delete(string id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"未找到ID为 {id} 的DWS配置" });
            }

            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _logger.LogInformation("成功删除DWS配置: {ConfigId}", id);
                return Ok(new { message = "DWS配置删除成功" });
            }
            return BadRequest(new { message = "删除DWS配置失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除DWS配置 {ConfigId} 时发生错误", id);
            return StatusCode(500, new { message = "删除DWS配置失败" });
        }
    }
}
