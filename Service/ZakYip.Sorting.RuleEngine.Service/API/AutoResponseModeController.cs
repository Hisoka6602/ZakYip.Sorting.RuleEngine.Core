using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 自动应答模式控制器
/// Auto-response mode controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("自动应答模式管理接口，用于开启和关闭模拟应答功能")]
public class AutoResponseModeController : ControllerBase
{
    private readonly IAutoResponseModeService _autoResponseModeService;
    private readonly ILogger<AutoResponseModeController> _logger;

    public AutoResponseModeController(
        IAutoResponseModeService autoResponseModeService,
        ILogger<AutoResponseModeController> logger)
    {
        _autoResponseModeService = autoResponseModeService;
        _logger = logger;
    }

    /// <summary>
    /// 启用自动应答模式
    /// Enable auto-response mode
    /// </summary>
    /// <returns>操作结果</returns>
    /// <response code="200">自动应答模式已启用</response>
    /// <remarks>
    /// 启用后，系统将不请求任何第三方API，而是随机返回1-20之间的格口ID。
    /// 用于模拟与下游的通信，方便测试和演示。
    /// 
    /// When enabled, the system will not request any third-party APIs,
    /// but randomly return a chute ID between 1-20.
    /// Used to simulate communication with downstream systems for testing and demonstration.
    /// </remarks>
    [HttpPost("enable")]
    [SwaggerOperation(
        Summary = "启用自动应答模式",
        Description = "启用自动应答模式后，系统将返回随机格口ID (1-20)，不调用第三方API",
        OperationId = "EnableAutoResponseMode",
        Tags = new[] { "AutoResponseMode" }
    )]
    [SwaggerResponse(200, "自动应答模式已启用", typeof(AutoResponseModeStatusDto))]
    public ActionResult<AutoResponseModeStatusDto> Enable()
    {
        _logger.LogInformation("收到启用自动应答模式请求");
        
        _autoResponseModeService.Enable();
        
        return Ok(new AutoResponseModeStatusDto
        {
            Enabled = true,
            Message = "自动应答模式已启用 / Auto-response mode enabled",
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 禁用自动应答模式
    /// Disable auto-response mode
    /// </summary>
    /// <returns>操作结果</returns>
    /// <response code="200">自动应答模式已禁用</response>
    /// <remarks>
    /// 禁用后，系统将恢复正常的第三方API调用流程。
    /// 
    /// When disabled, the system will resume normal third-party API call flow.
    /// </remarks>
    [HttpPost("disable")]
    [SwaggerOperation(
        Summary = "禁用自动应答模式",
        Description = "禁用自动应答模式后，系统将恢复正常的第三方API调用",
        OperationId = "DisableAutoResponseMode",
        Tags = new[] { "AutoResponseMode" }
    )]
    [SwaggerResponse(200, "自动应答模式已禁用", typeof(AutoResponseModeStatusDto))]
    public ActionResult<AutoResponseModeStatusDto> Disable()
    {
        _logger.LogInformation("收到禁用自动应答模式请求");
        
        _autoResponseModeService.Disable();
        
        return Ok(new AutoResponseModeStatusDto
        {
            Enabled = false,
            Message = "自动应答模式已禁用 / Auto-response mode disabled",
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 获取自动应答模式状态
    /// Get auto-response mode status
    /// </summary>
    /// <returns>当前状态</returns>
    /// <response code="200">返回当前状态</response>
    [HttpGet("status")]
    [SwaggerOperation(
        Summary = "获取自动应答模式状态",
        Description = "查询当前自动应答模式是否启用",
        OperationId = "GetAutoResponseModeStatus",
        Tags = new[] { "AutoResponseMode" }
    )]
    [SwaggerResponse(200, "返回当前状态", typeof(AutoResponseModeStatusDto))]
    public ActionResult<AutoResponseModeStatusDto> GetStatus()
    {
        var isEnabled = _autoResponseModeService.IsEnabled;
        
        return Ok(new AutoResponseModeStatusDto
        {
            Enabled = isEnabled,
            Message = isEnabled 
                ? "自动应答模式已启用 / Auto-response mode enabled" 
                : "自动应答模式已禁用 / Auto-response mode disabled",
            Timestamp = DateTime.Now
        });
    }
}

/// <summary>
/// 自动应答模式状态DTO
/// Auto-response mode status DTO
/// </summary>
[SwaggerSchema(Description = "自动应答模式状态数据传输对象")]
public record class AutoResponseModeStatusDto
{
    /// <summary>
    /// 是否启用
    /// Whether enabled
    /// </summary>
    [SwaggerSchema(Description = "自动应答模式是否启用")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// 消息
    /// Message
    /// </summary>
    [SwaggerSchema(Description = "状态消息")]
    public required string Message { get; init; }

    /// <summary>
    /// 时间戳
    /// Timestamp
    /// </summary>
    [SwaggerSchema(Description = "操作时间戳")]
    public required DateTime Timestamp { get; init; }
}
