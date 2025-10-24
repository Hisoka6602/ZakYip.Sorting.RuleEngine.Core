using Microsoft.AspNetCore.Mvc;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 规则管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RuleController : ControllerBase
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<RuleController> _logger;
    private readonly RuleValidationService _validationService;

    public RuleController(
        IRuleRepository ruleRepository,
        ILogger<RuleController> logger,
        RuleValidationService validationService)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
        _validationService = validationService;
    }

    /// <summary>
    /// 获取所有规则
    /// Get all rules
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SortingRule>>> GetAllRules(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _ruleRepository.GetAllAsync(cancellationToken);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有规则失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取启用的规则
    /// Get enabled rules
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<IEnumerable<SortingRule>>> GetEnabledRules(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _ruleRepository.GetEnabledRulesAsync(cancellationToken);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用规则失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取规则
    /// Get rule by ID
    /// </summary>
    [HttpGet("{ruleId}")]
    public async Task<ActionResult<SortingRule>> GetRuleById(string ruleId, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken);
            if (rule == null)
            {
                return NotFound(new { message = $"规则未找到: {ruleId}" });
            }
            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取规则失败: {RuleId}", ruleId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 添加规则
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SortingRule>> AddRule(
        [FromBody] SortingRule rule,
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

            var addedRule = await _ruleRepository.AddAsync(rule, cancellationToken);
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
    [HttpPut("{ruleId}")]
    public async Task<ActionResult<SortingRule>> UpdateRule(
        string ruleId,
        [FromBody] SortingRule rule,
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

            var updatedRule = await _ruleRepository.UpdateAsync(rule, cancellationToken);
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
    [HttpDelete("{ruleId}")]
    public async Task<ActionResult> DeleteRule(string ruleId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("删除规则: {RuleId}", ruleId);

            var result = await _ruleRepository.DeleteAsync(ruleId, cancellationToken);
            if (result)
            {
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
