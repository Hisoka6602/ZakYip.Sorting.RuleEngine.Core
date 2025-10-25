using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 格口统计API控制器
/// Chute statistics API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChuteStatisticsController : ControllerBase
{
    private readonly IChuteStatisticsService _chuteStatisticsService;
    private readonly ILogger<ChuteStatisticsController> _logger;

    public ChuteStatisticsController(
        IChuteStatisticsService chuteStatisticsService,
        ILogger<ChuteStatisticsController> logger)
    {
        _chuteStatisticsService = chuteStatisticsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取格口利用率统计列表
    /// Get chute utilization statistics list
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口统计列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ChuteUtilizationStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ChuteUtilizationStatisticsDto>>> GetChuteUtilizationStatistics(
        [FromQuery] ChuteStatisticsQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("查询格口利用率统计");
            var statistics = await _chuteStatisticsService.GetChuteUtilizationStatisticsAsync(
                query,
                cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询格口利用率统计时发生错误");
            return StatusCode(500, new { error = "查询格口统计时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定格口的统计信息
    /// Get statistics for a specific chute
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口统计信息</returns>
    [HttpGet("{chuteId}")]
    [ProducesResponseType(typeof(ChuteUtilizationStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChuteUtilizationStatisticsDto>> GetChuteStatisticsById(
        long chuteId,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("查询格口统计: ChuteId={ChuteId}", chuteId);
            var statistics = await _chuteStatisticsService.GetChuteStatisticsByIdAsync(
                chuteId,
                startTime,
                endTime,
                cancellationToken);

            if (statistics == null)
            {
                return NotFound(new { error = "格口不存在或无统计数据", chuteId });
            }

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询格口统计时发生错误: ChuteId={ChuteId}", chuteId);
            return StatusCode(500, new { error = "查询格口统计时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取分拣效率概览
    /// Get sorting efficiency overview
    /// </summary>
    /// <param name="startTime">开始时间（可选，默认7天前）</param>
    /// <param name="endTime">结束时间（可选，默认当前时间）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分拣效率概览</returns>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(SortingEfficiencyOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SortingEfficiencyOverviewDto>> GetSortingEfficiencyOverview(
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("查询分拣效率概览");
            var overview = await _chuteStatisticsService.GetSortingEfficiencyOverviewAsync(
                startTime,
                endTime,
                cancellationToken);
            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询分拣效率概览时发生错误");
            return StatusCode(500, new { error = "查询分拣效率概览时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取格口小时级统计（用于趋势分析）
    /// Get hourly chute statistics for trend analysis
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>小时级统计列表</returns>
    [HttpGet("{chuteId}/hourly")]
    [ProducesResponseType(typeof(List<ChuteHourlyStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ChuteHourlyStatisticsDto>>> GetChuteHourlyStatistics(
        long chuteId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (startTime >= endTime)
            {
                return BadRequest(new { error = "开始时间必须早于结束时间" });
            }

            _logger.LogInformation("查询格口小时级统计: ChuteId={ChuteId}, {StartTime} - {EndTime}",
                chuteId, startTime, endTime);

            var statistics = await _chuteStatisticsService.GetChuteHourlyStatisticsAsync(
                chuteId,
                startTime,
                endTime,
                cancellationToken);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询格口小时级统计时发生错误: ChuteId={ChuteId}", chuteId);
            return StatusCode(500, new { error = "查询格口小时级统计时发生内部错误", message = ex.Message });
        }
    }
}
