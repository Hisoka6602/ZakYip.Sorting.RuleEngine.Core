using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// API请求日志查询控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("API请求日志查询接口，提供API请求日志的查询功能")]
public class ApiRequestLogController : ControllerBase
{
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly ILogger<ApiRequestLogController> _logger;

    public ApiRequestLogController(
        MySqlLogDbContext? mysqlContext,
        SqliteLogDbContext? sqliteContext,
        ILogger<ApiRequestLogController> logger)
    {
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取API请求日志列表（分页）
    /// </summary>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">每页数量（最大100）</param>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="requestPath">请求路径（可选，支持模糊匹配）</param>
    /// <param name="requestMethod">请求方法（可选，如GET、POST）</param>
    /// <param name="statusCode">响应状态码（可选）</param>
    /// <param name="isSuccess">是否成功（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API请求日志列表</returns>
    /// <response code="200">成功返回日志列表</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取API请求日志列表",
        Description = "分页获取API请求日志，支持按时间范围、路径、方法、状态码等条件过滤",
        OperationId = "GetApiRequestLogs",
        Tags = new[] { "ApiRequestLog" }
    )]
    [SwaggerResponse(200, "成功返回日志列表", typeof(ApiRequestLogPagedResult))]
    [SwaggerResponse(400, "请求参数错误")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetLogs(
        [FromQuery, SwaggerParameter("页码（从1开始）")] int pageIndex = 1,
        [FromQuery, SwaggerParameter("每页数量（最大100）")] int pageSize = 20,
        [FromQuery, SwaggerParameter("开始时间")] DateTime? startTime = null,
        [FromQuery, SwaggerParameter("结束时间")] DateTime? endTime = null,
        [FromQuery, SwaggerParameter("请求路径（支持模糊匹配）")] string? requestPath = null,
        [FromQuery, SwaggerParameter("请求方法")] string? requestMethod = null,
        [FromQuery, SwaggerParameter("响应状态码")] int? statusCode = null,
        [FromQuery, SwaggerParameter("是否成功")] bool? isSuccess = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证参数
            if (pageIndex < 1)
            {
                return BadRequest(new { error = "页码必须大于等于1" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "每页数量必须在1到100之间" });
            }

            // 优先使用MySQL
            IQueryable<ApiRequestLog>? query = null;
            if (_mysqlContext != null)
            {
                query = _mysqlContext.ApiRequestLogs.AsQueryable();
            }
            else if (_sqliteContext != null)
            {
                query = _sqliteContext.ApiRequestLogs.AsQueryable();
            }

            if (query == null)
            {
                return StatusCode(500, new { error = "数据库上下文未配置" });
            }

            // 应用过滤条件
            if (startTime.HasValue)
            {
                query = query.Where(log => log.RequestTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(log => log.RequestTime <= endTime.Value);
            }

            if (!string.IsNullOrWhiteSpace(requestPath))
            {
                query = query.Where(log => log.RequestPath.Contains(requestPath));
            }

            if (!string.IsNullOrWhiteSpace(requestMethod))
            {
                query = query.Where(log => log.RequestMethod == requestMethod.ToUpper());
            }

            if (statusCode.HasValue)
            {
                query = query.Where(log => log.ResponseStatusCode == statusCode.Value);
            }

            if (isSuccess.HasValue)
            {
                query = query.Where(log => log.IsSuccess == isSuccess.Value);
            }

            // 按创建时间倒序排序
            query = query.OrderByDescending(log => log.RequestTime);

            // 获取总数
            var totalCount = await query.CountAsync(cancellationToken);

            // 分页
            var logs = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var result = new ApiRequestLogPagedResult
            {
                Data = logs,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取API请求日志失败");
            return StatusCode(500, new { error = "获取日志失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取单个API请求日志详情
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API请求日志详情</returns>
    /// <response code="200">成功返回日志详情</response>
    /// <response code="404">日志不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "获取API请求日志详情",
        Description = "根据日志ID获取单个API请求日志的详细信息",
        OperationId = "GetApiRequestLogById",
        Tags = new[] { "ApiRequestLog" }
    )]
    [SwaggerResponse(200, "成功返回日志详情", typeof(ApiRequestLog))]
    [SwaggerResponse(404, "日志不存在")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetById(
        [SwaggerParameter("日志ID", Required = true)] long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApiRequestLog? log = null;

            // 优先使用MySQL
            if (_mysqlContext != null)
            {
                log = await _mysqlContext.ApiRequestLogs
                    .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            }
            else if (_sqliteContext != null)
            {
                log = await _sqliteContext.ApiRequestLogs
                    .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            }

            if (log == null)
            {
                return NotFound(new { error = "日志不存在", id });
            }

            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取API请求日志详情失败，ID: {Id}", id);
            return StatusCode(500, new { error = "获取日志详情失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取API请求统计信息
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    /// <response code="200">成功返回统计信息</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("statistics")]
    [SwaggerOperation(
        Summary = "获取API请求统计信息",
        Description = "获取指定时间范围内的API请求统计信息，包括总请求数、成功率、平均耗时等",
        OperationId = "GetApiRequestStatistics",
        Tags = new[] { "ApiRequestLog" }
    )]
    [SwaggerResponse(200, "成功返回统计信息", typeof(ApiRequestStatistics))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery, SwaggerParameter("开始时间")] DateTime? startTime = null,
        [FromQuery, SwaggerParameter("结束时间")] DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<ApiRequestLog>? query = null;
            if (_mysqlContext != null)
            {
                query = _mysqlContext.ApiRequestLogs.AsQueryable();
            }
            else if (_sqliteContext != null)
            {
                query = _sqliteContext.ApiRequestLogs.AsQueryable();
            }

            if (query == null)
            {
                return StatusCode(500, new { error = "数据库上下文未配置" });
            }

            // 应用时间过滤
            if (startTime.HasValue)
            {
                query = query.Where(log => log.RequestTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(log => log.RequestTime <= endTime.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var successCount = await query.CountAsync(log => log.IsSuccess, cancellationToken);
            var avgDuration = await query.AverageAsync(log => (double?)log.DurationMs, cancellationToken) ?? 0;

            var statistics = new ApiRequestStatistics
            {
                TotalRequests = totalCount,
                SuccessRequests = successCount,
                FailedRequests = totalCount - successCount,
                SuccessRate = totalCount > 0 ? (decimal)successCount / totalCount * 100 : 0,
                AverageDurationMs = (decimal)avgDuration
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取API请求统计信息失败");
            return StatusCode(500, new { error = "获取统计信息失败", message = ex.Message });
        }
    }
}

/// <summary>
/// API请求日志分页结果
/// </summary>
public class ApiRequestLogPagedResult
{
    /// <summary>
    /// 日志数据列表
    /// </summary>
    public List<ApiRequestLog> Data { get; set; } = new();

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// API请求统计信息
/// </summary>
public class ApiRequestStatistics
{
    /// <summary>
    /// 总请求数
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// 成功请求数
    /// </summary>
    public int SuccessRequests { get; set; }

    /// <summary>
    /// 失败请求数
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// 成功率（百分比）
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// 平均耗时（毫秒）
    /// </summary>
    public decimal AverageDurationMs { get; set; }
}
