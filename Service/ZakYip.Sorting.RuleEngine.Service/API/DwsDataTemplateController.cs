using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// DWS数据模板管理控制器
/// DWS Data Template Management Controller
/// </summary>
[ApiController]
[Route("api/Dws/Template")]
[Produces("application/json")]
public class DwsDataTemplateController : ControllerBase
{
    private readonly IDwsDataTemplateRepository _templateRepository;
    private readonly IConfigurationAuditLogRepository _auditLogRepository;
    private readonly ILogger<DwsDataTemplateController> _logger;
    private readonly ISystemClock _clock;
    private readonly IPublisher _publisher;

    public DwsDataTemplateController(
        IDwsDataTemplateRepository templateRepository,
        IConfigurationAuditLogRepository auditLogRepository,
        ILogger<DwsDataTemplateController> logger,
        ISystemClock clock,
        IPublisher publisher)
    {
        _templateRepository = templateRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
        _clock = clock;
        _publisher = publisher;
    }

    /// <summary>
    /// 获取DWS数据模板配置
    /// Get DWS data template configuration
    /// </summary>
    /// <returns>当前DWS数据模板</returns>
    /// <response code="200">成功返回模板配置</response>
    /// <response code="404">模板配置不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取DWS数据模板",
        Description = "获取当前DWS数据解析模板配置，包括模板格式、字段分隔符等信息",
        OperationId = "GetDwsDataTemplate",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "成功返回模板配置", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(404, "模板配置不存在", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsDataTemplateResponseDto>>> GetTemplate()
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(DwsDataTemplate.SingletonId).ConfigureAwait(false);
            
            if (template == null)
            {
                return NotFound(ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                    "DWS数据模板不存在，请先创建模板配置", "TEMPLATE_NOT_FOUND"));
            }

            var dto = new DwsDataTemplateResponseDto
            {
                Name = template.Name,
                Template = template.Template,
                Delimiter = template.Delimiter,
                IsJsonFormat = template.IsJsonFormat,
                IsEnabled = template.IsEnabled,
                Description = template.Description,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return Ok(ApiResponse<DwsDataTemplateResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS数据模板失败");
            return StatusCode(500, ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                $"获取模板配置失败: {ex.Message}", "GET_TEMPLATE_FAILED"));
        }
    }

    /// <summary>
    /// 更新DWS数据模板配置
    /// Update DWS data template configuration
    /// </summary>
    /// <param name="request">模板更新请求</param>
    /// <returns>更新后的模板配置</returns>
    /// <response code="200">成功更新模板配置</response>
    /// <response code="400">请求参数无效</response>
    /// <response code="404">模板配置不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新DWS数据模板",
        Description = "更新DWS数据解析模板配置。更新后需要重新连接DWS设备才能生效。",
        OperationId = "UpdateDwsDataTemplate",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "成功更新模板配置", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(400, "请求参数无效", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(404, "模板配置不存在", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsDataTemplateResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsDataTemplateResponseDto>>> UpdateTemplate(
        [FromBody] DwsDataTemplateUpdateRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                    "请求参数不能为空", "INVALID_REQUEST"));
            }

            // 验证模板格式
            if (string.IsNullOrWhiteSpace(request.Template))
            {
                return BadRequest(ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                    "模板格式不能为空", "INVALID_TEMPLATE"));
            }

            // 获取现有模板
            var existingTemplate = await _templateRepository.GetByIdAsync(DwsDataTemplate.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var template = new DwsDataTemplate
            {
                TemplateId = DwsDataTemplate.SingletonId,
                Name = request.Name,
                Template = request.Template,
                Delimiter = request.Delimiter,
                IsJsonFormat = request.IsJsonFormat,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingTemplate?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingTemplate == null)
            {
                // 创建新模板
                success = await _templateRepository.AddAsync(template).ConfigureAwait(false);
                _logger.LogInformation("创建DWS数据模板: {Name}", request.Name);
            }
            else
            {
                // 更新现有模板
                success = await _templateRepository.UpdateAsync(template).ConfigureAwait(false);
                _logger.LogInformation("更新DWS数据模板: {Name}", request.Name);
            }

            if (!success)
            {
                return StatusCode(500, ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                    "更新模板配置失败", "UPDATE_TEMPLATE_FAILED"));
            }

            // 记录审计日志
            await _auditLogRepository.AddAsync(new ConfigurationAuditLog
            {
                ConfigurationType = "DwsDataTemplate",
                ConfigurationId = DwsDataTemplate.SingletonId.ToString(),
                OperationType = existingTemplate == null ? "Create" : "Update",
                ContentBefore = existingTemplate != null ? $"Template: {existingTemplate.Template}" : null,
                ContentAfter = $"Template: {template.Template}",
                ChangeReason = $"通过API{(existingTemplate == null ? "创建" : "更新")}DWS数据模板",
                OperatorUser = "系统管理员 / System Administrator",
                CreatedAt = now
            }).ConfigureAwait(false);

            // 发布配置变更事件（如果是更新）
            // Note: DwsConfigChangedEvent requires specific fields, so we skip it for template updates
            // Template changes would require DWS reconnection anyway
            if (existingTemplate != null)
            {
                _logger.LogInformation("DWS数据模板已更新，建议重新连接DWS设备以应用新模板");
            }

            var responseDto = new DwsDataTemplateResponseDto
            {
                Name = template.Name,
                Template = template.Template,
                Delimiter = template.Delimiter,
                IsJsonFormat = template.IsJsonFormat,
                IsEnabled = template.IsEnabled,
                Description = template.Description,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return Ok(ApiResponse<DwsDataTemplateResponseDto>.SuccessResult(responseDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS数据模板失败");
            return StatusCode(500, ApiResponse<DwsDataTemplateResponseDto>.FailureResult(
                $"更新模板配置失败: {ex.Message}", "UPDATE_TEMPLATE_FAILED"));
        }
    }
}
