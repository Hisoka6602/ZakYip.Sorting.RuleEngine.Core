using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS数据模板管理控制器（单例模式）
/// DWS data template management controller (Singleton pattern)
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
    /// 获取DWS数据模板（单例）
    /// Get DWS data template (singleton)
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取DWS数据模板",
        Description = "获取系统中唯一的DWS数据解析模板（单例模式）",
        OperationId = "GetDwsDataTemplate",
        Tags = new[] { "DwsDataTemplate" }
    )]
    [SwaggerResponse(200, "成功返回DWS数据模板", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsDataTemplateResponseDto>>> Get()
    {
        try
        {
            var template = await _repository.GetByIdAsync(DwsDataTemplate.SINGLETON_ID);
            
            if (template == null)
            {
                // 返回默认模板
                var defaultTemplate = GetDefaultTemplate();
                return Ok(ApiResponse<DwsDataTemplateResponseDto>.SuccessResult(defaultTemplate));
            }
            
            var dto = template.ToResponseDto();
            return Ok(ApiResponse<DwsDataTemplateResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS数据模板时发生错误");
            return StatusCode(500, ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                "获取DWS数据模板失败", "GET_TEMPLATE_FAILED"));
        }
    }

    /// <summary>
    /// 更新DWS数据模板（Upsert）
    /// Update DWS data template (Upsert)
    /// </summary>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新DWS数据模板",
        Description = "更新DWS数据模板，如果不存在则创建（单例模式，全量更新）",
        OperationId = "UpdateDwsDataTemplate",
        Tags = new[] { "DwsDataTemplate" }
    )]
    [SwaggerResponse(200, "模板更新成功", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsDataTemplateResponseDto>>> Update(
        [FromBody, SwaggerRequestBody("DWS数据模板更新请求", Required = true)] DwsDataTemplateUpdateRequest request)
    {
        try
        {
            // 从请求创建实体（自动设置单例ID）
            var template = request.ToEntity();
            
            // 检查现有模板
            var existing = await _repository.GetByIdAsync(DwsDataTemplate.SINGLETON_ID);
            bool success;
            
            if (existing == null)
            {
                success = await _repository.AddAsync(template);
            }
            else
            {
                // 保留原创建时间
                template = template with { CreatedAt = existing.CreatedAt };
                success = await _repository.UpdateAsync(template);
            }
            
            if (success)
            {
                _logger.LogInformation("DWS数据模板已更新");
                
                var dto = template.ToResponseDto();
                return Ok(ApiResponse<DwsDataTemplateResponseDto>.SuccessResult(dto, "模板已更新"));
            }
            
            return BadRequest(ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                "更新DWS数据模板失败", "UPDATE_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS数据模板时发生错误");
            return StatusCode(500, ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                "更新DWS数据模板失败", "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// 获取默认模板
    /// Get default template
    /// </summary>
    private static DwsDataTemplateResponseDto GetDefaultTemplate()
    {
        return new DwsDataTemplateResponseDto
        {
            Name = "默认数据模板",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = false,
            Description = "默认的DWS数据解析模板",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}
