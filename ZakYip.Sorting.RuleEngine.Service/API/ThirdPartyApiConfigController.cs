using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 第三方API配置管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("第三方API配置管理接口")]
public class ThirdPartyApiConfigController : ControllerBase
{
    private readonly IThirdPartyApiConfigRepository _repository;
    private readonly ILogger<ThirdPartyApiConfigController> _logger;

    public ThirdPartyApiConfigController(
        IThirdPartyApiConfigRepository repository,
        ILogger<ThirdPartyApiConfigController> logger)
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
        Description = "获取系统中所有第三方API配置",
        OperationId = "GetAllApiConfigs",
        Tags = new[] { "ThirdPartyApiConfig" }
    )]
    [SwaggerResponse(200, "成功返回API配置列表", typeof(IEnumerable<ThirdPartyApiConfig>))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult<IEnumerable<ThirdPartyApiConfig>>> GetAll()
    {
        try
        {
            var configs = await _repository.GetAllAsync();
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有API配置时发生错误");
            return StatusCode(500, new { message = "获取API配置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有启用的API配置
    /// </summary>
    /// <returns>启用的API配置列表（按优先级排序）</returns>
    [HttpGet("enabled")]
    public async Task<ActionResult<IEnumerable<ThirdPartyApiConfig>>> GetEnabled()
    {
        try
        {
            var configs = await _repository.GetEnabledConfigsAsync();
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用的API配置时发生错误");
            return StatusCode(500, new { message = "获取启用的API配置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取API配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <returns>API配置</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ThirdPartyApiConfig>> GetById(string id)
    {
        try
        {
            var config = await _repository.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(new { message = $"未找到ID为 {id} 的API配置" });
            }
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取API配置 {ConfigId} 时发生错误", id);
            return StatusCode(500, new { message = "获取API配置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 创建新的API配置
    /// </summary>
    /// <param name="config">API配置</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ThirdPartyApiConfig config)
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
    public async Task<ActionResult> Update(string id, [FromBody] ThirdPartyApiConfig config)
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
