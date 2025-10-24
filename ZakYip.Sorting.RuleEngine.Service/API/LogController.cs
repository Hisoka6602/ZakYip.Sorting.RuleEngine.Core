using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using System.Text;
using Microsoft.Extensions.Options;
using ZakYip.Sorting.RuleEngine.Service.Configuration;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 日志查询控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    [HttpGet("matching")]
    public async Task<IActionResult> GetMatchingLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? parcelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DbContext? context = _useMySql ? _mysqlContext : _sqliteContext;
            if (context == null)
            {
                return StatusCode(500, new { error = "数据库未配置" });
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
            var data = await logs
                .OrderByDescending(x => x.MatchingTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new { total, page, pageSize, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询匹配日志失败");
            return StatusCode(500, new { error = "查询匹配日志失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取DWS通信日志
    /// </summary>
    [HttpGet("dws-communication")]
    public async Task<IActionResult> GetDwsCommunicationLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? barcode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = _useMySql 
                ? _mysqlContext!.DwsCommunicationLogs.AsQueryable()
                : _sqliteContext!.DwsCommunicationLogs.AsQueryable();

            if (startTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime <= endTime.Value);
            
            if (!string.IsNullOrWhiteSpace(barcode))
                logs = logs.Where(x => x.Barcode == barcode);

            var total = await logs.CountAsync(cancellationToken);
            var data = await logs
                .OrderByDescending(x => x.CommunicationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new { total, page, pageSize, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询DWS通信日志失败");
            return StatusCode(500, new { error = "查询DWS通信日志失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取API通信日志
    /// </summary>
    [HttpGet("api-communication")]
    public async Task<IActionResult> GetApiCommunicationLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? parcelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = _useMySql 
                ? _mysqlContext!.ApiCommunicationLogs.AsQueryable()
                : _sqliteContext!.ApiCommunicationLogs.AsQueryable();

            if (startTime.HasValue)
                logs = logs.Where(x => x.RequestTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.RequestTime <= endTime.Value);
            
            if (!string.IsNullOrWhiteSpace(parcelId))
                logs = logs.Where(x => x.ParcelId == parcelId);

            var total = await logs.CountAsync(cancellationToken);
            var data = await logs
                .OrderByDescending(x => x.RequestTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new { total, page, pageSize, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询API通信日志失败");
            return StatusCode(500, new { error = "查询API通信日志失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取分拣机通信日志
    /// </summary>
    [HttpGet("sorter-communication")]
    public async Task<IActionResult> GetSorterCommunicationLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? parcelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = _useMySql 
                ? _mysqlContext!.SorterCommunicationLogs.AsQueryable()
                : _sqliteContext!.SorterCommunicationLogs.AsQueryable();

            if (startTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime >= startTime.Value);
            
            if (endTime.HasValue)
                logs = logs.Where(x => x.CommunicationTime <= endTime.Value);
            
            if (!string.IsNullOrWhiteSpace(parcelId))
                logs = logs.Where(x => x.ExtractedParcelId == parcelId);

            var total = await logs.CountAsync(cancellationToken);
            var data = await logs
                .OrderByDescending(x => x.CommunicationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new { total, page, pageSize, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询分拣机通信日志失败");
            return StatusCode(500, new { error = "查询分拣机通信日志失败", message = ex.Message });
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
            return File(bytes, "text/csv", $"matching_logs_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出匹配日志失败");
            return StatusCode(500, new { error = "导出匹配日志失败", message = ex.Message });
        }
    }
}
