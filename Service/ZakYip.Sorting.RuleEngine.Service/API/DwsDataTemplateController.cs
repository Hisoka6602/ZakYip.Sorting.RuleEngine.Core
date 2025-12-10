using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS数据模板管理控制器
/// DWS data template management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("DWS数据模板管理接口")]
public class DwsDataTemplateController : ControllerBase
{
    private readonly IDwsDataTemplateRepository _repository;
    private readonly ILogger<DwsDataTemplateController> _logger;

    public DwsDataTemplateController(
        IDwsDataTemplateRepository repository,
        ILogger<DwsDataTemplateController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有数据模板
    /// Get all data templates
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取所有数据模板",
        Description = "获取系统中所有DWS数据模板",
        OperationId = "GetAllDataTemplates",
        Tags = new[] { "DwsDataTemplate" }
    )]
    [SwaggerResponse(200, "成功返回数据模板列表", typeof(ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>>> GetAll()
    {
        try
        {
            var templates = await _repository.GetAllAsync();
            var dtos = templates.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有数据模板时发生错误");
            return StatusCode(500, ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>.FailureResult("获取数据模板失败", "GET_TEMPLATES_FAILED"));
        }
    }

    /// <summary>
    /// 获取所有启用的数据模板
    /// Get all enabled data templates
    /// </summary>
    [HttpGet("enabled")]
    [SwaggerOperation(
        Summary = "获取启用的数据模板",
        Description = "获取系统中所有已启用的数据模板",
        OperationId = "GetEnabledDataTemplates",
        Tags = new[] { "DwsDataTemplate" }
    )]
    [SwaggerResponse(200, "成功返回启用的数据模板列表", typeof(ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>>> GetEnabled()
    {
        try
        {
            var templates = await _repository.GetEnabledTemplatesAsync();
            var dtos = templates.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用的数据模板时发生错误");
            return StatusCode(500, ApiResponse<IEnumerable<DwsDataTemplateResponseDto>>.FailureResult("获取启用的数据模板失败", "GET_ENABLED_TEMPLATES_FAILED"));
        }
    }

    /// <summary>
    /// 根据ID获取数据模板
    /// Get data template by ID
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "根据ID获取数据模板",
        Description = "根据模板ID获取特定数据模板的详细信息",
        OperationId = "GetDataTemplateById",
        Tags = new[] { "DwsDataTemplate" }
    )]
    [SwaggerResponse(200, "成功返回数据模板", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(404, "数据模板未找到", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsDataTemplateResponseDto>>> GetById(
        [SwaggerParameter("模板ID", Required = true)] string id)
    {
        try
        {
            var template = await _repository.GetByIdAsync(id);
            if (template == null)
            {
                return NotFound(ApiResponse<DwsDataTemplateResponseDto>.FailureResult("数据模板未找到", "TEMPLATE_NOT_FOUND"));
            }
            var dto = template.ToResponseDto();
            return Ok(ApiResponse<DwsDataTemplateResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据模板 {TemplateId} 时发生错误", id);
            return StatusCode(500, ApiResponse<DwsDataTemplateResponseDto>.FailureResult("获取数据模板失败", "GET_TEMPLATE_FAILED"));
        }
    }

    /// <summary>
    /// 创建新的数据模板
    /// Create new data template
    /// </summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "创建数据模板",
        Description = "创建新的DWS数据模板",
        OperationId = "CreateDataTemplate",
        Tags = new[] { "DwsDataTemplate" }
    )]
    public async Task<ActionResult> Create([FromBody] DwsDataTemplate template)
    {
        try
        {
            var success = await _repository.AddAsync(template);
            if (success)
            {
                _logger.LogInformation("成功创建数据模板: {TemplateId}", template.TemplateId);
                return CreatedAtAction(nameof(GetById), new { id = template.TemplateId }, template);
            }
            return BadRequest(new { message = "创建数据模板失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建数据模板时发生错误");
            return StatusCode(500, new { message = "创建数据模板失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新数据模板
    /// Update data template
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "更新数据模板",
        Description = "更新现有的数据模板",
        OperationId = "UpdateDataTemplate",
        Tags = new[] { "DwsDataTemplate" }
    )]
    public async Task<ActionResult> Update(string id, [FromBody] DwsDataTemplate template)
    {
        try
        {
            if (id != template.TemplateId)
            {
                return BadRequest(new { message = "模板ID不匹配" });
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"未找到ID为 {id} 的数据模板" });
            }

            var success = await _repository.UpdateAsync(template);
            if (success)
            {
                _logger.LogInformation("成功更新数据模板: {TemplateId}", template.TemplateId);
                return Ok(new { message = "数据模板更新成功" });
            }
            return BadRequest(new { message = "更新数据模板失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新数据模板 {TemplateId} 时发生错误", id);
            return StatusCode(500, new { message = "更新数据模板失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除数据模板
    /// Delete data template
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "删除数据模板",
        Description = "删除指定的数据模板",
        OperationId = "DeleteDataTemplate",
        Tags = new[] { "DwsDataTemplate" }
    )]
    public async Task<ActionResult> Delete(string id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"未找到ID为 {id} 的数据模板" });
            }

            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _logger.LogInformation("成功删除数据模板: {TemplateId}", id);
                return Ok(new { message = "数据模板删除成功" });
            }
            return BadRequest(new { message = "删除数据模板失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除数据模板 {TemplateId} 时发生错误", id);
            return StatusCode(500, new { message = "删除数据模板失败", error = ex.Message });
        }
    }
}
