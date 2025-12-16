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
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly IAutoResponseModeService _autoResponseModeService;
    private readonly ILogger<AutoResponseModeController> _logger;

    public AutoResponseModeController(
        IAutoResponseModeService autoResponseModeService,
        ILogger<AutoResponseModeController> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_autoResponseModeService = autoResponseModeService;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 启用自动应答模式
    /// Enable auto-response mode
    /// </summary>
    /// <param name="request">启用请求，包含可选的格口数组配置 / Enable request with optional chute array configuration</param>
    /// <returns>操作结果</returns>
    /// <response code="200">自动应答模式已启用</response>
    /// <remarks>
    /// 启用后，系统将不请求任何第三方API，而是从配置的格口数组中随机返回格口ID。
    /// 如果未提供格口数组，默认使用 [1,2,3]。
    /// 
    /// When enabled, the system will not request any third-party APIs,
    /// but randomly return a chute ID from the configured array.
    /// Defaults to [1,2,3] if no array is provided.
    /// 
    /// 示例请求:
    /// 
    ///     POST /api/AutoResponseMode/enable
    ///     {
    ///        "chuteNumbers": [1, 2, 3, 4, 5, 6]
    ///     }
    /// 
    /// </remarks>
    [HttpPost("enable")]
    [SwaggerOperation(
        Summary = "启用自动应答模式",
        Description = "启用自动应答模式后，系统将从配置的格口数组中返回随机格口ID，不调用第三方API。默认 [1,2,3]",
        OperationId = "EnableAutoResponseMode",
        Tags = new[] { "AutoResponseMode" }
    )]
    [SwaggerResponse(200, "自动应答模式已启用", typeof(AutoResponseModeStatusDto))]
    public ActionResult<AutoResponseModeStatusDto> Enable([FromBody] EnableAutoResponseModeRequest? request = null)
    {
        _logger.LogInformation("收到启用自动应答模式请求");
        
        var chuteNumbers = request?.ChuteNumbers;
        _autoResponseModeService.Enable(chuteNumbers);
        
        var actualChuteNumbers = _autoResponseModeService.ChuteNumbers;
        
        return Ok(new AutoResponseModeStatusDto
        {
            Enabled = true,
            Message = $"自动应答模式已启用，格口数组: [{string.Join(", ", actualChuteNumbers)}] / Auto-response mode enabled with chute array: [{string.Join(", ", actualChuteNumbers)}]",
            Timestamp = _clock.LocalNow,
            ChuteNumbers = actualChuteNumbers
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
        var chuteNumbers = _autoResponseModeService.ChuteNumbers;
        
        return Ok(new AutoResponseModeStatusDto
        {
            Enabled = false,
            Message = "自动应答模式已禁用 / Auto-response mode disabled",
            Timestamp = _clock.LocalNow,
            ChuteNumbers = chuteNumbers
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
        Description = "查询当前自动应答模式是否启用及配置的格口数组",
        OperationId = "GetAutoResponseModeStatus",
        Tags = new[] { "AutoResponseMode" }
    )]
    [SwaggerResponse(200, "返回当前状态", typeof(AutoResponseModeStatusDto))]
    public ActionResult<AutoResponseModeStatusDto> GetStatus()
    {
        var isEnabled = _autoResponseModeService.IsEnabled;
        var chuteNumbers = _autoResponseModeService.ChuteNumbers;
        
        return Ok(new AutoResponseModeStatusDto
        {
            Enabled = isEnabled,
            Message = isEnabled 
                ? $"自动应答模式已启用，格口数组: [{string.Join(", ", chuteNumbers)}] / Auto-response mode enabled with chute array: [{string.Join(", ", chuteNumbers)}]"
                : "自动应答模式已禁用 / Auto-response mode disabled",
            Timestamp = _clock.LocalNow,
            ChuteNumbers = chuteNumbers
        });
    }
}

/// <summary>
/// 启用自动应答模式请求
/// Enable auto-response mode request
/// </summary>
[SwaggerSchema(Description = "启用自动应答模式请求，可选指定格口数组")]
public record class EnableAutoResponseModeRequest
{
    /// <summary>
    /// 格口号数组，例如 [1,2,3,4,5,6]。如果未指定，默认使用 [1,2,3]
    /// Chute numbers array, e.g. [1,2,3,4,5,6]. Defaults to [1,2,3] if not specified
    /// </summary>
    /// <example>[1, 2, 3, 4, 5, 6]</example>
    [SwaggerSchema(Description = "格口号数组，例如 [1,2,3,4,5,6]")]
    public int[]? ChuteNumbers { get; init; }
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

    /// <summary>
    /// 格口号数组
    /// Chute numbers array
    /// </summary>
    [SwaggerSchema(Description = "当前配置的格口号数组")]
    public required int[] ChuteNumbers { get; init; }
}
