using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 规则管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("分拣规则管理接口，提供规则的增删改查功能")]
public class RuleController : ControllerBase
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<RuleController> _logger;
    private readonly RuleValidationService _validationService;
    private readonly IPublisher _publisher;
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;

    public RuleController(
        IRuleRepository ruleRepository,
        ILogger<RuleController> logger,
        RuleValidationService validationService,
        IPublisher publisher,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
        _validationService = validationService;
        _publisher = publisher;
        _clock = clock;
    }

    /// <summary>
    /// 获取所有规则
    /// Get all rules
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>规则列表</returns>
    /// <response code="200">返回规则列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取所有规则",
        Description = "获取系统中所有分拣规则，包括启用和禁用的规则。如果没有规则，将自动创建一个默认规则。",
        OperationId = "GetAllRules",
        Tags = new[] { "Rule" }
    )]
    [SwaggerResponse(200, "成功返回规则列表", typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>))]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SortingRuleResponseDto>>>> GetAllRules(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _ruleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            
            // 如果没有规则，创建一个默认规则
            // If no rules exist, create a default rule
            if (!rules.Any())
            {
                var defaultRule = new SortingRule
                {
                    RuleId = "DEFAULT_RULE_001",
                    RuleName = "默认规则",
                    Description = "系统默认创建的规则，匹配所有包裹到默认格口",
                    Priority = 9999,
                    MatchingMethod = Domain.Enums.MatchingMethodType.LegacyExpression,
                    ConditionExpression = "true",
                    TargetChute = "DEFAULT",
                    IsEnabled = true,
                    CreatedAt = _clock.LocalNow
                };
                
                await _ruleRepository.AddAsync(defaultRule, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("创建默认规则: {RuleId}", defaultRule.RuleId);
                
                // 发布规则创建事件
                await _publisher.Publish(new RuleCreatedEvent
                {
                    RuleId = defaultRule.RuleId,
                    RuleName = defaultRule.RuleName,
                    TargetChute = defaultRule.TargetChute,
                    CreatedAt = defaultRule.CreatedAt
                }, cancellationToken);
                
                // 重新获取规则列表
                rules = await _ruleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            }
            
            var responseDtos = rules.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<SortingRuleResponseDto>>.SuccessResult(responseDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有规则失败");
            return StatusCode(500, ApiResponse<IEnumerable<SortingRuleResponseDto>>.FailureResult("获取规则列表失败", "GET_RULES_FAILED"));
        }
    }

    /// <summary>
    /// 获取启用的规则
    /// Get enabled rules
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启用的规则列表</returns>
    /// <response code="200">返回启用的规则列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("enabled")]
    [SwaggerOperation(
        Summary = "获取启用的规则",
        Description = "获取系统中所有已启用的分拣规则，按优先级排序",
        OperationId = "GetEnabledRules",
        Tags = new[] { "Rule" }
    )]
    [SwaggerResponse(200, "成功返回启用的规则列表", typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>))]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SortingRuleResponseDto>>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SortingRuleResponseDto>>>> GetEnabledRules(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _ruleRepository.GetEnabledRulesAsync(cancellationToken).ConfigureAwait(false);
            var responseDtos = rules.ToResponseDtos();
            return Ok(ApiResponse<IEnumerable<SortingRuleResponseDto>>.SuccessResult(responseDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用规则失败");
            return StatusCode(500, ApiResponse<IEnumerable<SortingRuleResponseDto>>.FailureResult("获取启用规则失败", "GET_ENABLED_RULES_FAILED"));
        }
    }

    /// <summary>
    /// 根据ID获取规则
    /// Get rule by ID
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>规则详情</returns>
    /// <response code="200">成功返回规则详情</response>
    /// <response code="404">规则未找到</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("{ruleId}")]
    [SwaggerOperation(
        Summary = "根据ID获取规则",
        Description = "根据规则ID获取特定分拣规则的详细信息",
        OperationId = "GetRuleById",
        Tags = new[] { "Rule" }
    )]
    [SwaggerResponse(200, "成功返回规则详情", typeof(ApiResponse<SortingRuleResponseDto>))]
    [SwaggerResponse(404, "规则未找到", typeof(ApiResponse<SortingRuleResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<SortingRuleResponseDto>))]
    [ProducesResponseType(typeof(ApiResponse<SortingRuleResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SortingRuleResponseDto>), 404)]
    [ProducesResponseType(typeof(ApiResponse<SortingRuleResponseDto>), 500)]
    public async Task<ActionResult<ApiResponse<SortingRuleResponseDto>>> GetRuleById(
        [SwaggerParameter("规则唯一标识", Required = true)] string ruleId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
            if (rule == null)
            {
                return NotFound(ApiResponse<SortingRuleResponseDto>.FailureResult("规则未找到", "RULE_NOT_FOUND"));
            }
            var responseDto = rule.ToResponseDto();
            return Ok(ApiResponse<SortingRuleResponseDto>.SuccessResult(responseDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取规则失败: {RuleId}", ruleId);
            return StatusCode(500, ApiResponse<SortingRuleResponseDto>.FailureResult("获取规则失败", "GET_RULE_FAILED"));
        }
    }

    /// <summary>
    /// 添加规则
    /// </summary>
    /// <param name="rule">规则信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的规则</returns>
    /// <response code="201">规则创建成功</response>
    /// <response code="400">请求参数错误或规则验证失败</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/rule
    ///     {
    ///        "ruleId": "RULE001",
    ///        "ruleName": "深圳规则",
    ///        "description": "所有发往深圳的包裹",
    ///        "priority": 10,
    ///        "matchingMethod": 0,
    ///        "conditionExpression": "destination == '深圳'",
    ///        "targetChute": "CHUTE01",
    ///        "isEnabled": true
    ///     }
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(
        Summary = "添加规则",
        Description = "创建新的分拣规则。规则会经过安全验证，不合规的规则会被拒绝。",
        OperationId = "AddRule",
        Tags = new[] { "Rule" }
    )]
    [SwaggerResponse(201, "规则创建成功", typeof(SortingRule))]
    [SwaggerResponse(400, "请求参数错误或规则验证失败")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult<SortingRule>> AddRule(
        [FromBody, SwaggerRequestBody("规则信息", Required = true)] SortingRule rule,
        CancellationToken cancellationToken)
    {
        try
        {
            // 验证规则安全性
            var validation = _validationService.ValidateRule(rule);
            if (!validation.IsValid)
            {
                _logger.LogWarning("规则验证失败: {RuleId} - {Error}", rule.RuleId, validation.ErrorMessage);
                return BadRequest(new { error = validation.ErrorMessage });
            }

            _logger.LogInformation("添加规则: {RuleId} - {RuleName}", rule.RuleId, rule.RuleName);

            var addedRule = await _ruleRepository.AddAsync(rule, cancellationToken).ConfigureAwait(false);
            
            // 发布规则创建事件
            await _publisher.Publish(new RuleCreatedEvent
            {
                RuleId = addedRule.RuleId,
                RuleName = addedRule.RuleName,
                TargetChute = addedRule.TargetChute,
                Priority = addedRule.Priority,
                IsEnabled = addedRule.IsEnabled,
                CreatedAt = _clock.LocalNow
            }, cancellationToken);
            
            return CreatedAtAction(nameof(GetRuleById), new { ruleId = addedRule.RuleId }, addedRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加规则失败: {RuleId}", rule.RuleId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新规则
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="rule">更新的规则信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的规则</returns>
    /// <response code="200">规则更新成功</response>
    /// <response code="400">请求参数错误或规则ID不匹配</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     PUT /api/rule/RULE001
    ///     {
    ///        "ruleId": "RULE001",
    ///        "ruleName": "深圳规则(已更新)",
    ///        "description": "所有发往深圳的包裹-更新版",
    ///        "priority": 5,
    ///        "matchingMethod": 0,
    ///        "conditionExpression": "destination == '深圳' AND weight > 1000",
    ///        "targetChute": "CHUTE02",
    ///        "isEnabled": true
    ///     }
    /// </remarks>
    [HttpPut("{ruleId}")]
    [SwaggerOperation(
        Summary = "更新规则",
        Description = "更新现有分拣规则的信息。规则ID必须与路径参数一致。",
        OperationId = "UpdateRule",
        Tags = new[] { "Rule" }
    )]
    [SwaggerResponse(200, "规则更新成功", typeof(SortingRule))]
    [SwaggerResponse(400, "请求参数错误或规则ID不匹配")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult<SortingRule>> UpdateRule(
        [SwaggerParameter("规则唯一标识", Required = true)] string ruleId,
        [FromBody, SwaggerRequestBody("更新的规则信息", Required = true)] SortingRule rule,
        CancellationToken cancellationToken)
    {
        try
        {
            if (ruleId != rule.RuleId)
            {
                return BadRequest(new { message = "规则ID不匹配" });
            }

            // 验证规则安全性
            var validation = _validationService.ValidateRule(rule);
            if (!validation.IsValid)
            {
                _logger.LogWarning("规则验证失败: {RuleId} - {Error}", rule.RuleId, validation.ErrorMessage);
                return BadRequest(new { error = validation.ErrorMessage });
            }

            _logger.LogInformation("更新规则: {RuleId} - {RuleName}", rule.RuleId, rule.RuleName);

            var updatedRule = await _ruleRepository.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
            
            // 发布规则更新事件
            await _publisher.Publish(new RuleUpdatedEvent
            {
                RuleId = updatedRule.RuleId,
                RuleName = updatedRule.RuleName,
                TargetChute = updatedRule.TargetChute,
                Priority = updatedRule.Priority,
                IsEnabled = updatedRule.IsEnabled,
                UpdatedAt = _clock.LocalNow
            }, cancellationToken);
            
            return Ok(updatedRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新规则失败: {RuleId}", ruleId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除规则
    /// Delete a rule
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    /// <response code="200">规则删除成功</response>
    /// <response code="404">规则未找到</response>
    /// <response code="500">服务器内部错误</response>
    [HttpDelete("{ruleId}")]
    [SwaggerOperation(
        Summary = "删除规则",
        Description = "根据规则ID删除指定的分拣规则",
        OperationId = "DeleteRule",
        Tags = new[] { "Rule" }
    )]
    [SwaggerResponse(200, "规则删除成功")]
    [SwaggerResponse(404, "规则未找到")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult> DeleteRule(
        [SwaggerParameter("规则唯一标识", Required = true)] string ruleId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 先获取规则信息用于事件发布
            var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation("删除规则: {RuleId}", ruleId);

            var result = await _ruleRepository.DeleteAsync(ruleId, cancellationToken).ConfigureAwait(false);
            if (result)
            {
                // 发布规则删除事件
                if (rule != null)
                {
                    await _publisher.Publish(new RuleDeletedEvent
                    {
                        RuleId = rule.RuleId,
                        RuleName = rule.RuleName,
                        DeletedAt = _clock.LocalNow
                    }, cancellationToken);
                }
                
                return Ok(new { message = "规则删除成功" });
            }
            else
            {
                return NotFound(new { message = $"规则未找到: {ruleId}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除规则失败: {RuleId}", ruleId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
