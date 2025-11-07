using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// WCS API配置管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("WCS API配置管理接口")]
public class WcsApiConfigController : ControllerBase
{
    private readonly IWcsApiConfigRepository _repository;
    private readonly ILogger<WcsApiConfigController> _logger;

    public WcsApiConfigController(
        IWcsApiConfigRepository repository,
        ILogger<WcsApiConfigController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有API配置
    /// </summary>
    /// <returns>所有API配置列表</returns>
    /// <response code="200">成功返回API配置列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取所有API配置",
        Description = "获取系统中所有WCS API配置（API密钥已脱敏）",
        OperationId = "GetAllApiConfigs",
        Tags = new[] { "WcsApiConfig" }
    )]
    [SwaggerResponse(200, "成功返回API配置列表", typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>))]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<WcsApiConfigResponseDto>>>> GetAll()
    {
        try
        {
            var configs = await _repository.GetAllAsync();
            var dtos = configs.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有API配置时发生错误");
            return StatusCode(500, ApiResponse<IEnumerable<WcsApiConfigResponseDto>>.FailureResult("获取API配置失败", "GET_CONFIGS_FAILED"));
        }
    }

    /// <summary>
    /// 获取所有启用的API配置
    /// </summary>
    /// <returns>启用的API配置列表（按优先级排序）</returns>
    /// <response code="200">成功返回启用的API配置列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("enabled")]
    [SwaggerOperation(
        Summary = "获取启用的API配置",
        Description = "获取系统中所有已启用的WCS API配置，按优先级排序（API密钥已脱敏）",
        OperationId = "GetEnabledApiConfigs",
        Tags = new[] { "WcsApiConfig" }
    )]
    [SwaggerResponse(200, "成功返回启用的API配置列表", typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>))]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<WcsApiConfigResponseDto>>>> GetEnabled()
    {
        try
        {
            var configs = await _repository.GetEnabledConfigsAsync();
            var dtos = configs.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<WcsApiConfigResponseDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用的API配置时发生错误");
            return StatusCode(500, ApiResponse<IEnumerable<WcsApiConfigResponseDto>>.FailureResult("获取启用的API配置失败", "GET_ENABLED_CONFIGS_FAILED"));
        }
    }

    /// <summary>
    /// 根据ID获取API配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <returns>API配置</returns>
    /// <response code="200">成功返回API配置</response>
    /// <response code="404">API配置未找到</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "根据ID获取API配置",
        Description = "根据配置ID获取特定WCS API配置的详细信息（API密钥已脱敏）",
        OperationId = "GetApiConfigById",
        Tags = new[] { "WcsApiConfig" }
    )]
    [SwaggerResponse(200, "成功返回API配置", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(404, "API配置未找到", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WcsApiConfigResponseDto>))]
    [ProducesResponseType(typeof(ApiResponse<WcsApiConfigResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<WcsApiConfigResponseDto>), 404)]
    [ProducesResponseType(typeof(ApiResponse<WcsApiConfigResponseDto>), 500)]
    public async Task<ActionResult<ApiResponse<WcsApiConfigResponseDto>>> GetById(
        [SwaggerParameter("配置ID", Required = true)] string id)
    {
        try
        {
            var config = await _repository.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(ApiResponse<WcsApiConfigResponseDto>.FailureResult("API配置未找到", "CONFIG_NOT_FOUND"));
            }
            var dto = config.ToResponseDto();
            return Ok(ApiResponse<WcsApiConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取API配置 {ConfigId} 时发生错误", id);
            return StatusCode(500, ApiResponse<WcsApiConfigResponseDto>.FailureResult("获取API配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 创建新的API配置
    /// </summary>
    /// <param name="config">API配置</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] WcsApiConfig config)
    {
        try
        {
            var success = await _repository.AddAsync(config);
            if (success)
            {
                _logger.LogInformation("成功创建API配置: {ConfigId}", config.ConfigId);
                return CreatedAtAction(nameof(GetById), new { id = config.ConfigId }, config);
            }
            return BadRequest(new { message = "创建API配置失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建API配置时发生错误");
            return StatusCode(500, new { message = "创建API配置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新API配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <param name="config">API配置</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] WcsApiConfig config)
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
                return NotFound(new { message = $"未找到ID为 {id} 的API配置" });
            }

            var success = await _repository.UpdateAsync(config);
            if (success)
            {
                _logger.LogInformation("成功更新API配置: {ConfigId}", config.ConfigId);
                return Ok(new { message = "API配置更新成功" });
            }
            return BadRequest(new { message = "更新API配置失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新API配置 {ConfigId} 时发生错误", id);
            return StatusCode(500, new { message = "更新API配置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除API配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"未找到ID为 {id} 的API配置" });
            }

            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _logger.LogInformation("成功删除API配置: {ConfigId}", id);
                return Ok(new { message = "API配置删除成功" });
            }
            return BadRequest(new { message = "删除API配置失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除API配置 {ConfigId} 时发生错误", id);
            return StatusCode(500, new { message = "删除API配置失败", error = ex.Message });
        }
    }
}
