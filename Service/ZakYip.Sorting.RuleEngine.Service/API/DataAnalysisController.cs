using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 数据分析API控制器
/// Data analysis API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("数据分析接口，提供格口使用热力图和分拣效率分析报表")]
public class DataAnalysisController : ControllerBase
{
    private readonly IDataAnalysisService _dataAnalysisService;
    private readonly ILogger<DataAnalysisController> _logger;

    public DataAnalysisController(
        IDataAnalysisService dataAnalysisService,
        ILogger<DataAnalysisController> logger)
    {
        _dataAnalysisService = dataAnalysisService;
        _logger = logger;
    }

    /// <summary>
    /// 获取格口使用热力图
    /// Get chute usage heatmap
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口使用热力图数据</returns>
    /// <response code="200">成功返回热力图数据</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("heatmap")]
    [SwaggerOperation(
        Summary = "获取格口使用热力图",
        Description = "获取格口使用热力图数据，显示各格口在不同时段的使用率",
        OperationId = "GetChuteHeatmap",
        Tags = new[] { "DataAnalysis" }
    )]
    [ProducesResponseType(typeof(List<ChuteHeatmapDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ChuteHeatmapDto>>> GetChuteHeatmap(
        [FromQuery, SwaggerParameter("查询参数")] HeatmapQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (query.StartDate > query.EndDate)
            {
                return BadRequest(new { error = "开始日期不能晚于结束日期" });
            }

            _logger.LogInformation("获取格口使用热力图: {StartDate} - {EndDate}", 
                query.StartDate, query.EndDate);

            var heatmapData = await _dataAnalysisService.GetChuteHeatmapAsync(query, cancellationToken).ConfigureAwait(false);
            return Ok(heatmapData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取格口使用热力图时发生错误");
            return StatusCode(500, new { error = "获取热力图数据时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取分拣效率分析报表
    /// Get sorting efficiency analysis report
    /// </summary>
    /// <param name="startTime">开始时间（可选，默认7天前）</param>
    /// <param name="endTime">结束时间（可选，默认当前时间）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分拣效率分析报表</returns>
    /// <response code="200">成功返回分析报表</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("efficiency-report")]
    [SwaggerOperation(
        Summary = "获取分拣效率分析报表",
        Description = "获取分拣系统的效率分析报表，包括处理量、成功率、利用率等关键指标",
        OperationId = "GetSortingEfficiencyReport",
        Tags = new[] { "DataAnalysis" }
    )]
    [ProducesResponseType(typeof(SortingEfficiencyOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SortingEfficiencyOverviewDto>> GetSortingEfficiencyReport(
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startTime ?? DateTime.Now.AddDays(-7);
            var end = endTime ?? DateTime.Now;

            if (start > end)
            {
                return BadRequest(new { error = "开始时间不能晚于结束时间" });
            }

            _logger.LogInformation("获取分拣效率分析报表: {StartTime} - {EndTime}", start, end);

            var report = await _dataAnalysisService.GetSortingEfficiencyReportAsync(
                start, end, cancellationToken).ConfigureAwait(false);
            
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分拣效率分析报表时发生错误");
            return StatusCode(500, new { error = "获取分析报表时发生内部错误", message = ex.Message });
        }
    }
}
