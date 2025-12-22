using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using System.Text;
using Microsoft.Extensions.Options;
using ZakYip.Sorting.RuleEngine.Service.Configuration;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 日志查询控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("日志查询接口，提供各类日志的查询和导出功能")]
public class LogController : ControllerBase
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly ILogger<LogController> _logger;
    private readonly bool _useMySql;

    public LogController(
        ILogger<LogController> logger,
        IOptions<AppSettings> appSettings,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock,
        MySqlLogDbContext? mysqlContext = null,
        SqliteLogDbContext? sqliteContext = null)
    {
        _logger = logger;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
        _useMySql = appSettings.Value.MySql.Enabled;
        _clock = clock;
    }

    /// <summary>
    /// 获取匹配日志
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="parcelId">包裹ID（可选）</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认50）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配日志列表</returns>
    /// <response code="200">成功返回匹配日志列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("matching")]
    [SwaggerOperation(
        Summary = "获取匹配日志",
        Description = "查询包裹匹配规则的日志记录，支持时间范围和包裹ID筛选",
        OperationId = "GetMatchingLogs",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回匹配日志列表", typeof(PagedResponse<MatchingLogResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(PagedResponse<MatchingLogResponseDto>))]
    [ProducesResponseType(typeof(PagedResponse<MatchingLogResponseDto>), 200)]
    [ProducesResponseType(typeof(PagedResponse<MatchingLogResponseDto>), 500)]
    public async Task<ActionResult<PagedResponse<MatchingLogResponseDto>>> GetMatchingLogs(
        [FromQuery, SwaggerParameter("开始时间，格式：yyyy-MM-dd HH:mm:ss")] DateTime? startTime,
        [FromQuery, SwaggerParameter("结束时间，格式：yyyy-MM-dd HH:mm:ss")] DateTime? endTime,
        [FromQuery, SwaggerParameter("包裹ID，精确匹配")] string? parcelId,
        [FromQuery, SwaggerParameter("页码，从1开始")] int page = 1,
        [FromQuery, SwaggerParameter("每页数量，最大100")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DbContext? context = _useMySql ? _mysqlContext : _sqliteContext;
            if (context == null)
            {
                return StatusCode(500, PagedResponse<MatchingLogResponseDto>.FailureResult("数据库未配置", "DB_NOT_CONFIGURED"));
            }

            var logs = _useMySql 
                ? _mysqlContext!.MatchingLogs.AsQueryable()
                : _sqliteContext!.MatchingLogs.AsQueryable();

            if (startTime.HasValue)
                logs = logs.Where(x => x.MatchingTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.MatchingTime <= endTime.Value);
            
            if (!string.IsNullOrWhiteSpace(parcelId))
                logs = logs.Where(x => x.ParcelId == parcelId);

            var total = await logs.CountAsync(cancellationToken).ConfigureAwait(false);
            var entities = await logs
                .OrderByDescending(x => x.MatchingTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var dtos = entities.Select(e => new MatchingLogResponseDto
            {
                Id = e.Id,
                ParcelId = e.ParcelId,
                DwsContent = e.DwsContent,
                ApiContent = e.ApiContent,
                MatchedRuleId = e.MatchedRuleId,
                MatchingReason = e.MatchingReason,
                ChuteId = e.ChuteId,
                CartOccupancy = e.CartOccupancy,
                MatchingTime = e.MatchingTime,
                IsSuccess = e.IsSuccess,
                ErrorMessage = e.ErrorMessage
            }).ToList();

            return Ok(PagedResponse<MatchingLogResponseDto>.SuccessResult(dtos, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询匹配日志失败");
            return StatusCode(500, PagedResponse<MatchingLogResponseDto>.FailureResult($"查询匹配日志失败: {ex.Message}", "QUERY_FAILED"));
        }
    }

    /// <summary>
    /// 获取DWS通信日志
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="barcode">条码（可选）</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认50）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>DWS通信日志列表</returns>
    /// <response code="200">成功返回DWS通信日志列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("dws-communication")]
    [SwaggerOperation(
        Summary = "获取DWS通信日志",
        Description = "查询DWS设备通信日志，支持时间范围和条码筛选",
        OperationId = "GetDwsCommunicationLogs",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回DWS通信日志列表", typeof(PagedResponse<DwsCommunicationLog>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(PagedResponse<DwsCommunicationLog>))]
    [ProducesResponseType(typeof(PagedResponse<DwsCommunicationLog>), 200)]
    [ProducesResponseType(typeof(PagedResponse<DwsCommunicationLog>), 500)]
    public async Task<ActionResult<PagedResponse<DwsCommunicationLog>>> GetDwsCommunicationLogs(
        [FromQuery, SwaggerParameter("开始时间")] DateTime? startTime,
        [FromQuery, SwaggerParameter("结束时间")] DateTime? endTime,
        [FromQuery, SwaggerParameter("条码")] string? barcode,
        [FromQuery, SwaggerParameter("页码")] int page = 1,
        [FromQuery, SwaggerParameter("每页数量")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DbContext? context = _useMySql ? _mysqlContext : _sqliteContext;
            if (context == null)
            {
                return StatusCode(500, PagedResponse<DwsCommunicationLog>.FailureResult("数据库未配置", "DB_NOT_CONFIGURED"));
            }

            var logs = _useMySql 
                ? _mysqlContext!.DwsCommunicationLogs.AsQueryable()
                : _sqliteContext!.DwsCommunicationLogs.AsQueryable();

            // 优先使用Barcode索引进行过滤
            if (!string.IsNullOrWhiteSpace(barcode))
                logs = logs.Where(x => x.Barcode == barcode);
            
            if (startTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime <= endTime.Value);

            var total = await logs.CountAsync(cancellationToken).ConfigureAwait(false);
            var data = await logs
                .OrderByDescending(x => x.CommunicationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            return Ok(PagedResponse<DwsCommunicationLog>.SuccessResult(data, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询DWS通信日志失败");
            return StatusCode(500, PagedResponse<DwsCommunicationLog>.FailureResult($"查询DWS通信日志失败: {ex.Message}", "QUERY_FAILED"));
        }
    }

    /// <summary>
    /// 获取WCS的API请求日志
    /// Get WCS API request logs
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="requestPath">请求路径（可选，支持模糊匹配）</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认50）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>WCS API请求日志列表</returns>
    /// <response code="200">成功返回API请求日志列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("api-communication")]
    [SwaggerOperation(
        Summary = "获取WCS的API请求日志",
        Description = "查询对WCS的API请求日志，支持时间范围和请求路径筛选",
        OperationId = "GetApiCommunicationLogs",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回API请求日志列表", typeof(PagedResponse<ApiRequestLog>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(PagedResponse<ApiRequestLog>))]
    [ProducesResponseType(typeof(PagedResponse<ApiRequestLog>), 200)]
    [ProducesResponseType(typeof(PagedResponse<ApiRequestLog>), 500)]
    public async Task<ActionResult<PagedResponse<ApiRequestLog>>> GetApiCommunicationLogs(
        [FromQuery, SwaggerParameter("开始时间")] DateTime? startTime,
        [FromQuery, SwaggerParameter("结束时间")] DateTime? endTime,
        [FromQuery, SwaggerParameter("请求路径（支持模糊匹配）")] string? requestPath,
        [FromQuery, SwaggerParameter("页码")] int page = 1,
        [FromQuery, SwaggerParameter("每页数量")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DbContext? context = _useMySql ? _mysqlContext : _sqliteContext;
            if (context == null)
            {
                return StatusCode(500, PagedResponse<ApiRequestLog>.FailureResult("数据库未配置", "DB_NOT_CONFIGURED"));
            }

            var logs = _useMySql 
                ? _mysqlContext!.ApiRequestLogs.AsQueryable()
                : _sqliteContext!.ApiRequestLogs.AsQueryable();

            // 优先使用RequestPath索引进行过滤
            if (!string.IsNullOrWhiteSpace(requestPath))
                logs = logs.Where(x => x.RequestPath.Contains(requestPath));
            
            if (startTime.HasValue)
                logs = logs.Where(x => x.RequestTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.RequestTime <= endTime.Value);

            var total = await logs.CountAsync(cancellationToken).ConfigureAwait(false);
            var data = await logs
                .AsNoTracking()
                .OrderByDescending(x => x.RequestTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            return Ok(PagedResponse<ApiRequestLog>.SuccessResult(data, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询WCS API请求日志失败");
            return StatusCode(500, PagedResponse<ApiRequestLog>.FailureResult($"查询WCS API请求日志失败: {ex.Message}", "QUERY_FAILED"));
        }
    }

    /// <summary>
    /// 获取分拣机通信日志
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="parcelId">包裹ID（可选）</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认50）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分拣机通信日志列表</returns>
    /// <response code="200">成功返回分拣机通信日志列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("sorter-communication")]
    [SwaggerOperation(
        Summary = "获取分拣机通信日志",
        Description = "查询分拣机通信日志，支持时间范围和包裹ID筛选",
        OperationId = "GetSorterCommunicationLogs",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回分拣机通信日志列表", typeof(PagedResponse<SorterCommunicationLog>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(PagedResponse<SorterCommunicationLog>))]
    [ProducesResponseType(typeof(PagedResponse<SorterCommunicationLog>), 200)]
    [ProducesResponseType(typeof(PagedResponse<SorterCommunicationLog>), 500)]
    public async Task<ActionResult<PagedResponse<SorterCommunicationLog>>> GetSorterCommunicationLogs(
        [FromQuery, SwaggerParameter("开始时间")] DateTime? startTime,
        [FromQuery, SwaggerParameter("结束时间")] DateTime? endTime,
        [FromQuery, SwaggerParameter("包裹ID")] string? parcelId,
        [FromQuery, SwaggerParameter("页码")] int page = 1,
        [FromQuery, SwaggerParameter("每页数量")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DbContext? context = _useMySql ? _mysqlContext : _sqliteContext;
            if (context == null)
            {
                return StatusCode(500, PagedResponse<SorterCommunicationLog>.FailureResult("数据库未配置", "DB_NOT_CONFIGURED"));
            }

            var logs = _useMySql 
                ? _mysqlContext!.SorterCommunicationLogs.AsQueryable()
                : _sqliteContext!.SorterCommunicationLogs.AsQueryable();

            // 优先使用ExtractedParcelId索引进行过滤
            if (!string.IsNullOrWhiteSpace(parcelId))
                logs = logs.Where(x => x.ExtractedParcelId == parcelId);
            
            if (startTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime <= endTime.Value);

            var total = await logs.CountAsync(cancellationToken).ConfigureAwait(false);
            var data = await logs
                .OrderByDescending(x => x.CommunicationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            return Ok(PagedResponse<SorterCommunicationLog>.SuccessResult(data, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询分拣机通信日志失败");
            return StatusCode(500, PagedResponse<SorterCommunicationLog>.FailureResult($"查询分拣机通信日志失败: {ex.Message}", "QUERY_FAILED"));
        }
    }

    /// <summary>
    /// 导出匹配日志为CSV
    /// </summary>
    [HttpGet("matching/export")]
    public async Task<IActionResult> ExportMatchingLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? parcelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = _useMySql 
                ? _mysqlContext!.MatchingLogs.AsQueryable()
                : _sqliteContext!.MatchingLogs.AsQueryable();

            if (startTime.HasValue)
                logs = logs.Where(x => x.MatchingTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.MatchingTime <= endTime.Value);
            
            if (!string.IsNullOrWhiteSpace(parcelId))
                logs = logs.Where(x => x.ParcelId == parcelId);

            var data = await logs
                .OrderByDescending(x => x.MatchingTime)
                .Take(10000) // 限制最多导出10000条
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var csv = new StringBuilder();
            csv.AppendLine("Id,ParcelId,MatchedRuleId,ChuteId,MatchingTime,IsSuccess,ErrorMessage");
            
            foreach (var log in data)
            {
                csv.AppendLine($"{log.Id},{log.ParcelId},{log.MatchedRuleId},{log.ChuteId},{log.MatchingTime:yyyy-MM-dd HH:mm:ss},{log.IsSuccess},\"{log.ErrorMessage?.Replace("\"", "\"\"")}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"matching_logs_{_clock.LocalNow:yyyyMMddHHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出匹配日志失败");
            return StatusCode(500, new { error = "导出匹配日志失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取API请求日志列表（分页）
    /// Get API request logs (paginated)
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
    [HttpGet("api-request")]
    [SwaggerOperation(
        Summary = "获取API请求日志列表",
        Description = "分页获取API请求日志，支持按时间范围、路径、方法、状态码等条件过滤",
        OperationId = "GetApiRequestLogs",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回日志列表", typeof(ApiRequestLogPagedResult))]
    [SwaggerResponse(400, "请求参数错误")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetApiRequestLogs(
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
            if (pageIndex < 1)
            {
                return BadRequest(new { error = "页码必须大于等于1" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "每页数量必须在1到100之间" });
            }

            IQueryable<ApiRequestLog>? query = null;
            if (_useMySql && _mysqlContext != null)
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

            query = query.OrderByDescending(log => log.RequestTime);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var logs = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

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
    /// Get single API request log details by ID
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API请求日志详情</returns>
    /// <response code="200">成功返回日志详情</response>
    /// <response code="404">日志不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("api-request/{id}")]
    [SwaggerOperation(
        Summary = "获取API请求日志详情",
        Description = "根据日志ID获取单个API请求日志的详细信息",
        OperationId = "GetApiRequestLogById",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回日志详情", typeof(ApiRequestLog))]
    [SwaggerResponse(404, "日志不存在")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetApiRequestLogById(
        [SwaggerParameter("日志ID", Required = true)] long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApiRequestLog? log = null;

            if (_useMySql && _mysqlContext != null)
            {
                log = await _mysqlContext.ApiRequestLogs
                    .FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
            }
            else if (_sqliteContext != null)
            {
                log = await _sqliteContext.ApiRequestLogs
                    .FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
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
    /// Get API request statistics
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    /// <response code="200">成功返回统计信息</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("api-request/statistics")]
    [SwaggerOperation(
        Summary = "获取API请求统计信息",
        Description = "获取指定时间范围内的API请求统计信息，包括总请求数、成功率、平均耗时等",
        OperationId = "GetApiRequestStatistics",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回统计信息", typeof(ApiRequestStatistics))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetApiRequestStatistics(
        [FromQuery, SwaggerParameter("开始时间")] DateTime? startTime = null,
        [FromQuery, SwaggerParameter("结束时间")] DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<ApiRequestLog>? query = null;
            if (_useMySql && _mysqlContext != null)
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

            if (startTime.HasValue)
            {
                query = query.Where(log => log.RequestTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(log => log.RequestTime <= endTime.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var successCount = await query.CountAsync(log => log.IsSuccess, cancellationToken).ConfigureAwait(false);
            var avgDuration = await query.AverageAsync(log => (double?)log.DurationMs, cancellationToken).ConfigureAwait(false) ?? 0;

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
/// API request log paged result
/// </summary>
file class ApiRequestLogPagedResult
{
    /// <summary>
    /// 日志数据列表
    /// Log data list
    /// </summary>
    public required List<ApiRequestLog> Data { get; init; }

    /// <summary>
    /// 当前页码
    /// Current page index
    /// </summary>
    public required int PageIndex { get; init; }

    /// <summary>
    /// 每页数量
    /// Page size
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// 总记录数
    /// Total count
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// 总页数
    /// Total pages
    /// </summary>
    public required int TotalPages { get; init; }
}

/// <summary>
/// API请求统计信息
/// API request statistics
/// </summary>
file class ApiRequestStatistics
{
    /// <summary>
    /// 总请求数
    /// Total requests
    /// </summary>
    public required int TotalRequests { get; init; }

    /// <summary>
    /// 成功请求数
    /// Success requests
    /// </summary>
    public required int SuccessRequests { get; init; }

    /// <summary>
    /// 失败请求数
    /// Failed requests
    /// </summary>
    public required int FailedRequests { get; init; }

    /// <summary>
    /// 成功率（百分比）
    /// Success rate (percentage)
    /// </summary>
    public required decimal SuccessRate { get; init; }

    /// <summary>
    /// 平均耗时（毫秒）
    /// Average duration (milliseconds)
    /// </summary>
    public required decimal AverageDurationMs { get; init; }
}
