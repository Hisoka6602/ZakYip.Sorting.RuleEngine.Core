using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 甘特图数据查询API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("甘特图数据查询接口，用于可视化包裹处理时间线")]
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
    /// <response code="200">成功返回甘特图数据</response>
    /// <response code="404">目标包裹未找到</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("{target}")]
    [SwaggerOperation(
        Summary = "查询甘特图数据",
        Description = "查询指定包裹前后N条数据的甘特图数据，用于可视化包裹处理时间线",
        OperationId = "GetGanttChartData",
        Tags = new[] { "GanttChart" }
    )]
    [SwaggerResponse(200, "成功返回甘特图数据", typeof(GanttChartQueryResponse))]
    [SwaggerResponse(404, "目标包裹未找到", typeof(GanttChartQueryResponse))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult<GanttChartQueryResponse>> GetGanttChartData(
        [SwaggerParameter("目标包裹ID或条码", Required = true)] string target,
        [FromQuery, SwaggerParameter("查询目标前面N条数据(默认5,最大100)")] int beforeCount = 5,
        [FromQuery, SwaggerParameter("查询目标后面N条数据(默认5,最大100)")] int afterCount = 5,
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
    /// <response code="200">成功返回甘特图数据</response>
    /// <response code="404">目标包裹未找到</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/ganttchart/query
    ///     {
    ///        "target": "PKG20231101001",
    ///        "beforeCount": 10,
    ///        "afterCount": 10
    ///     }
    /// </remarks>
    [HttpPost("query")]
    [SwaggerOperation(
        Summary = "POST方式查询甘特图数据",
        Description = "使用POST方式查询甘特图数据，支持更复杂的查询参数",
        OperationId = "QueryGanttChartData",
        Tags = new[] { "GanttChart" }
    )]
    [SwaggerResponse(200, "成功返回甘特图数据", typeof(GanttChartQueryResponse))]
    [SwaggerResponse(404, "目标包裹未找到", typeof(GanttChartQueryResponse))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult<GanttChartQueryResponse>> QueryGanttChartData(
        [FromBody, SwaggerRequestBody("查询请求", Required = true)] GanttChartQueryRequest request,
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
