using Microsoft.AspNetCore.Mvc;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 甘特图数据查询API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GanttChartController : ControllerBase
{
    private readonly IGanttChartService _ganttChartService;
    private readonly ILogger<GanttChartController> _logger;

    public GanttChartController(
        IGanttChartService ganttChartService,
        ILogger<GanttChartController> logger)
    {
        _ganttChartService = ganttChartService;
        _logger = logger;
    }

    /// <summary>
    /// 查询指定包裹前后N条数据的甘特图数据
    /// </summary>
    /// <param name="target">目标包裹ID或条码</param>
    /// <param name="beforeCount">查询目标前面N条数据（默认5条，最大100条）</param>
    /// <param name="afterCount">查询目标后面N条数据（默认5条，最大100条）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>甘特图数据查询响应</returns>
    [HttpGet("{target}")]
    public async Task<ActionResult<GanttChartQueryResponse>> GetGanttChartData(
        string target,
        [FromQuery] int beforeCount = 5,
        [FromQuery] int afterCount = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "查询甘特图数据: Target={Target}, BeforeCount={BeforeCount}, AfterCount={AfterCount}",
                target, beforeCount, afterCount);

            var response = await _ganttChartService.QueryGanttChartDataAsync(
                target,
                beforeCount,
                afterCount,
                cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询甘特图数据失败: Target={Target}", target);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST方式查询甘特图数据（支持更复杂的查询参数）
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>甘特图数据查询响应</returns>
    [HttpPost("query")]
    public async Task<ActionResult<GanttChartQueryResponse>> QueryGanttChartData(
        [FromBody] GanttChartQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "查询甘特图数据(POST): Target={Target}, BeforeCount={BeforeCount}, AfterCount={AfterCount}",
                request.Target, request.BeforeCount, request.AfterCount);

            var response = await _ganttChartService.QueryGanttChartDataAsync(
                request.Target,
                request.BeforeCount,
                request.AfterCount,
                cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询甘特图数据失败: Target={Target}", request.Target);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
