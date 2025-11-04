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
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly ILogger<LogController> _logger;
    private readonly bool _useMySql;

    public LogController(
        ILogger<LogController> logger,
        IOptions<AppSettings> appSettings,
        MySqlLogDbContext? mysqlContext = null,
        SqliteLogDbContext? sqliteContext = null)
    {
        _logger = logger;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
        _useMySql = appSettings.Value.MySql.Enabled;
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

            var total = await logs.CountAsync(cancellationToken);
            var entities = await logs
                .OrderByDescending(x => x.MatchingTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

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

            var total = await logs.CountAsync(cancellationToken);
            var data = await logs
                .OrderByDescending(x => x.CommunicationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(PagedResponse<DwsCommunicationLog>.SuccessResult(data, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询DWS通信日志失败");
            return StatusCode(500, PagedResponse<DwsCommunicationLog>.FailureResult($"查询DWS通信日志失败: {ex.Message}", "QUERY_FAILED"));
        }
    }

    /// <summary>
    /// 获取API通信日志
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <param name="parcelId">包裹ID（可选）</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认50）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API通信日志列表</returns>
    /// <response code="200">成功返回API通信日志列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("api-communication")]
    [SwaggerOperation(
        Summary = "获取API通信日志",
        Description = "查询第三方API通信日志，支持时间范围和包裹ID筛选",
        OperationId = "GetApiCommunicationLogs",
        Tags = new[] { "Log" }
    )]
    [SwaggerResponse(200, "成功返回API通信日志列表", typeof(PagedResponse<ApiCommunicationLog>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(PagedResponse<ApiCommunicationLog>))]
    [ProducesResponseType(typeof(PagedResponse<ApiCommunicationLog>), 200)]
    [ProducesResponseType(typeof(PagedResponse<ApiCommunicationLog>), 500)]
    public async Task<ActionResult<PagedResponse<ApiCommunicationLog>>> GetApiCommunicationLogs(
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
                return StatusCode(500, PagedResponse<ApiCommunicationLog>.FailureResult("数据库未配置", "DB_NOT_CONFIGURED"));
            }

            var logs = _useMySql 
                ? _mysqlContext!.ApiCommunicationLogs.AsQueryable()
                : _sqliteContext!.ApiCommunicationLogs.AsQueryable();

            // 优先使用ParcelId索引进行过滤
            if (!string.IsNullOrWhiteSpace(parcelId))
                logs = logs.Where(x => x.ParcelId == parcelId);
            
            if (startTime.HasValue)
                logs = logs.Where(x => x.RequestTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.RequestTime <= endTime.Value);

            var total = await logs.CountAsync(cancellationToken);
            var data = await logs
                .OrderByDescending(x => x.RequestTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(PagedResponse<ApiCommunicationLog>.SuccessResult(data, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询API通信日志失败");
            return StatusCode(500, PagedResponse<ApiCommunicationLog>.FailureResult($"查询API通信日志失败: {ex.Message}", "QUERY_FAILED"));
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

            var total = await logs.CountAsync(cancellationToken);
            var data = await logs
                .OrderByDescending(x => x.CommunicationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

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
                .ToListAsync(cancellationToken);

            var csv = new StringBuilder();
            csv.AppendLine("Id,ParcelId,MatchedRuleId,ChuteId,MatchingTime,IsSuccess,ErrorMessage");
            
            foreach (var log in data)
            {
                csv.AppendLine($"{log.Id},{log.ParcelId},{log.MatchedRuleId},{log.ChuteId},{log.MatchingTime:yyyy-MM-dd HH:mm:ss},{log.IsSuccess},\"{log.ErrorMessage?.Replace("\"", "\"\"")}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"matching_logs_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出匹配日志失败");
            return StatusCode(500, new { error = "导出匹配日志失败", message = ex.Message });
        }
    }
}
