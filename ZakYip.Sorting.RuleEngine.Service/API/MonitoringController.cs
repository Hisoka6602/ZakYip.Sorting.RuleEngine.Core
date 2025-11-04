using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 监控告警API控制器
/// Monitoring alert API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("监控告警接口，提供实时监控、告警查询和管理功能")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IMonitoringService monitoringService,
        ILogger<MonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
    }

    /// <summary>
    /// 获取实时监控数据
    /// Get real-time monitoring data
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实时监控数据</returns>
    /// <response code="200">成功返回监控数据</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("realtime")]
    [SwaggerOperation(
        Summary = "获取实时监控数据",
        Description = "获取系统实时监控数据，包括包裹处理速率、格口使用率、错误率等",
        OperationId = "GetRealtimeMonitoringData",
        Tags = new[] { "Monitoring" }
    )]
    [ProducesResponseType(typeof(RealtimeMonitoringDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RealtimeMonitoringDto>> GetRealtimeMonitoringData(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取实时监控数据");
            var data = await _monitoringService.GetRealtimeMonitoringDataAsync(cancellationToken);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实时监控数据时发生错误");
            return StatusCode(500, new { error = "获取监控数据时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取活跃告警列表
    /// Get active alerts list
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>活跃告警列表</returns>
    /// <response code="200">成功返回告警列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("alerts/active")]
    [SwaggerOperation(
        Summary = "获取活跃告警列表",
        Description = "获取当前所有未解决的告警",
        OperationId = "GetActiveAlerts",
        Tags = new[] { "Monitoring" }
    )]
    [ProducesResponseType(typeof(List<MonitoringAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<MonitoringAlertDto>>> GetActiveAlerts(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取活跃告警列表");
            var alerts = await _monitoringService.GetActiveAlertsAsync(cancellationToken);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃告警列表时发生错误");
            return StatusCode(500, new { error = "获取告警列表时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取告警历史
    /// Get alert history
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>告警历史列表</returns>
    /// <response code="200">成功返回告警历史</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("alerts/history")]
    [SwaggerOperation(
        Summary = "获取告警历史",
        Description = "获取指定时间范围内的所有告警记录",
        OperationId = "GetAlertHistory",
        Tags = new[] { "Monitoring" }
    )]
    [ProducesResponseType(typeof(List<MonitoringAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<MonitoringAlertDto>>> GetAlertHistory(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (startTime > endTime)
            {
                return BadRequest(new { error = "开始时间不能晚于结束时间" });
            }

            _logger.LogInformation("获取告警历史: {StartTime} - {EndTime}", startTime, endTime);
            var alerts = await _monitoringService.GetAlertHistoryAsync(
                startTime, endTime, cancellationToken);
            
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警历史时发生错误");
            return StatusCode(500, new { error = "获取告警历史时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 解决告警
    /// Resolve alert
    /// </summary>
    /// <param name="alertId">告警ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    /// <response code="200">成功解决告警</response>
    /// <response code="404">告警不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("alerts/{alertId}/resolve")]
    [SwaggerOperation(
        Summary = "解决告警",
        Description = "将指定告警标记为已解决",
        OperationId = "ResolveAlert",
        Tags = new[] { "Monitoring" }
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ResolveAlert(
        string alertId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("解决告警: {AlertId}", alertId);
            await _monitoringService.ResolveAlertAsync(alertId, cancellationToken);
            return Ok(new { message = "告警已解决", alertId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解决告警时发生错误: {AlertId}", alertId);
            return StatusCode(500, new { error = "解决告警时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 手动触发告警检查
    /// Manually trigger alert check
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    /// <response code="200">成功触发检查</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("alerts/check")]
    [SwaggerOperation(
        Summary = "手动触发告警检查",
        Description = "手动触发一次监控告警检查",
        OperationId = "TriggerAlertCheck",
        Tags = new[] { "Monitoring" }
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TriggerAlertCheck(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("手动触发告警检查");
            await _monitoringService.CheckAndGenerateAlertsAsync(cancellationToken);
            return Ok(new { message = "告警检查已完成" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发告警检查时发生错误");
            return StatusCode(500, new { error = "触发告警检查时发生内部错误", message = ex.Message });
        }
    }
}
