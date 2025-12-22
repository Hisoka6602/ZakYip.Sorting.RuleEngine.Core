using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 健康检查控制器 / Health Check Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthCheckController : ControllerBase
{
    private readonly ISystemClock _clock;
    private readonly IDwsAdapterManager _dwsAdapterManager;
    private readonly ISorterAdapterManager _sorterAdapterManager;
    private readonly ILogger<HealthCheckController> _logger;

    public HealthCheckController(
        ISystemClock clock,
        IDwsAdapterManager dwsAdapterManager,
        ISorterAdapterManager sorterAdapterManager,
        ILogger<HealthCheckController> logger)
    {
        _clock = clock;
        _dwsAdapterManager = dwsAdapterManager;
        _sorterAdapterManager = sorterAdapterManager;
        _logger = logger;
    }

    /// <summary>
    /// 基本健康检查 / Basic Health Check
    /// </summary>
    /// <returns>健康状态</returns>
    /// <response code="200">系统健康</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "基本健康检查",
        Description = "检查系统是否正常运行",
        OperationId = "GetHealth",
        Tags = new[] { "健康检查 / Health Check" }
    )]
    [SwaggerResponse(200, "系统健康", typeof(ApiResponse<HealthStatusDto>))]
    public ActionResult<ApiResponse<HealthStatusDto>> GetHealth()
    {
        var healthStatus = new HealthStatusDto
        {
            Status = "healthy",
            Timestamp = _clock.LocalNow
        };

        return Ok(ApiResponse<HealthStatusDto>.SuccessResult(healthStatus));
    }

    /// <summary>
    /// DWS连接健康检查 / DWS Connection Health Check
    /// </summary>
    /// <returns>DWS连接状态</returns>
    /// <response code="200">DWS连接正常</response>
    /// <response code="503">DWS连接异常</response>
    [HttpGet("dws")]
    [SwaggerOperation(
        Summary = "DWS连接健康检查",
        Description = "检查DWS适配器连接状态",
        OperationId = "GetDwsHealth",
        Tags = new[] { "健康检查 / Health Check" }
    )]
    [SwaggerResponse(200, "DWS连接正常", typeof(ApiResponse<ConnectionHealthDto>))]
    [SwaggerResponse(503, "DWS连接异常", typeof(ApiResponse<ConnectionHealthDto>))]
    public ActionResult<ApiResponse<ConnectionHealthDto>> GetDwsHealth()
    {
        try
        {
            var isConnected = _dwsAdapterManager.IsConnected;
            var healthDto = new ConnectionHealthDto
            {
                ComponentName = "DWS",
                IsConnected = isConnected,
                Status = isConnected ? "Connected" : "Disconnected",
                Timestamp = _clock.LocalNow
            };

            if (isConnected)
            {
                return Ok(ApiResponse<ConnectionHealthDto>.SuccessResult(healthDto));
            }
            else
            {
                return StatusCode(503, ApiResponse<ConnectionHealthDto>.FailureResult(
                    "DWS连接未建立", "DWS_DISCONNECTED"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查DWS连接健康状态失败");
            return StatusCode(503, ApiResponse<ConnectionHealthDto>.FailureResult(
                $"DWS健康检查失败: {ex.Message}", "DWS_HEALTH_CHECK_FAILED"));
        }
    }

    /// <summary>
    /// 分拣机连接健康检查 / Sorter Connection Health Check
    /// </summary>
    /// <returns>分拣机连接状态</returns>
    /// <response code="200">分拣机连接正常</response>
    /// <response code="503">分拣机连接异常</response>
    [HttpGet("sorter")]
    [SwaggerOperation(
        Summary = "分拣机连接健康检查",
        Description = "检查分拣机适配器连接状态",
        OperationId = "GetSorterHealth",
        Tags = new[] { "健康检查 / Health Check" }
    )]
    [SwaggerResponse(200, "分拣机连接正常", typeof(ApiResponse<ConnectionHealthDto>))]
    [SwaggerResponse(503, "分拣机连接异常", typeof(ApiResponse<ConnectionHealthDto>))]
    public ActionResult<ApiResponse<ConnectionHealthDto>> GetSorterHealth()
    {
        try
        {
            var isConnected = _sorterAdapterManager.IsConnected;
            var healthDto = new ConnectionHealthDto
            {
                ComponentName = "Sorter",
                IsConnected = isConnected,
                Status = isConnected ? "Connected" : "Disconnected",
                Timestamp = _clock.LocalNow
            };

            if (isConnected)
            {
                return Ok(ApiResponse<ConnectionHealthDto>.SuccessResult(healthDto));
            }
            else
            {
                return StatusCode(503, ApiResponse<ConnectionHealthDto>.FailureResult(
                    "分拣机连接未建立", "SORTER_DISCONNECTED"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查分拣机连接健康状态失败");
            return StatusCode(503, ApiResponse<ConnectionHealthDto>.FailureResult(
                $"分拣机健康检查失败: {ex.Message}", "SORTER_HEALTH_CHECK_FAILED"));
        }
    }
}

/// <summary>
/// 健康状态DTO / Health Status DTO
/// </summary>
public record HealthStatusDto
{
    /// <summary>
    /// 状态 / Status
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 时间戳 / Timestamp
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// 连接健康状态DTO / Connection Health DTO
/// </summary>
public record ConnectionHealthDto
{
    /// <summary>
    /// 组件名称 / Component Name
    /// </summary>
    public required string ComponentName { get; init; }

    /// <summary>
    /// 是否已连接 / Is Connected
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// 状态描述 / Status Description
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 时间戳 / Timestamp
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
