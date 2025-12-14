using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using Newtonsoft.Json;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Middleware;

/// <summary>
/// API请求日志中间件
/// 记录所有API请求的详细信息
/// </summary>
public class ApiRequestLoggingMiddleware
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRequestLoggingMiddleware> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志记录器</param>
    public ApiRequestLoggingMiddleware(
        RequestDelegate next, ILogger<ApiRequestLoggingMiddleware> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_next = next;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 执行中间件逻辑
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mysqlContext">MySQL数据库上下文（可选）</param>
    /// <param name="sqliteContext">SQLite数据库上下文（可选）</param>
    public async Task InvokeAsync(
        HttpContext context,
        MySqlLogDbContext? mysqlContext,
        SqliteLogDbContext? sqliteContext)
    {
        // 跳过健康检查、Swagger和SignalR端点的日志记录
        var path = context.Request.Path.Value ?? "";
        if (path.Contains("/health", StringComparison.OrdinalIgnoreCase) || 
            path.Contains("/swagger", StringComparison.OrdinalIgnoreCase) || 
            path.Contains("/hubs/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var requestLog = new ApiRequestLog
        {
            RequestTime = _clock.LocalNow,
            RequestIp = GetClientIp(context),
            RequestMethod = context.Request.Method,
            RequestPath = context.Request.Path,
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null
        };

        // 步骤1：记录请求头
        var requestHeaders = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            // 不记录敏感信息
            if (!header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                requestHeaders[header.Key] = header.Value.ToString();
            }
        }
        requestLog.RequestHeaders = JsonConvert.SerializeObject(requestHeaders);

        // 步骤2：记录请求体（仅对POST/PUT/PATCH）
        string? requestBody = null;
        if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
        {
            requestBody = await ReadRequestBodyAsync(context.Request);
            requestLog.RequestBody = requestBody;
        }

        // 步骤3：保存原始响应流
        var originalBodyStream = context.Response.Body;

        // 步骤4：创建新的内存流来捕获响应
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            // 步骤5：执行下一个中间件
            await _next(context);
            requestLog.IsSuccess = context.Response.StatusCode < 400;
        }
        catch (Exception ex)
        {
            exception = ex;
            requestLog.IsSuccess = false;
            requestLog.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            requestLog.DurationMs = stopwatch.ElapsedMilliseconds;
            requestLog.ResponseTime = _clock.LocalNow;
            requestLog.ResponseStatusCode = context.Response.StatusCode;

            // 步骤6：记录响应头
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in context.Response.Headers)
            {
                responseHeaders[header.Key] = header.Value.ToString();
            }
            requestLog.ResponseHeaders = JsonConvert.SerializeObject(responseHeaders);

            // 步骤7：记录响应体
            requestLog.ResponseBody = await ReadResponseBodyAsync(responseBody);

            // 步骤8：将响应写回原始流
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // 步骤9：保存日志到数据库
            await SaveLogAsync(requestLog, mysqlContext, sqliteContext);

            // 步骤10：记录到日志系统
            if (requestLog.IsSuccess)
            {
                _logger.LogInformation(
                    "API请求: {Method} {Path} - 状态码: {StatusCode} - 耗时: {Duration}ms",
                    requestLog.RequestMethod,
                    requestLog.RequestPath,
                    requestLog.ResponseStatusCode,
                    requestLog.DurationMs);
            }
            else
            {
                _logger.LogWarning(
                    "API请求失败: {Method} {Path} - 状态码: {StatusCode} - 错误: {Error} - 耗时: {Duration}ms",
                    requestLog.RequestMethod,
                    requestLog.RequestPath,
                    requestLog.ResponseStatusCode,
                    requestLog.ErrorMessage,
                    requestLog.DurationMs);
            }
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>客户端IP地址</returns>
    private string GetClientIp(HttpContext context)
    {
        // 检查X-Forwarded-For头（代理/负载均衡器）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // 检查X-Real-IP头
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 使用RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 读取请求体
    /// </summary>
    /// <param name="request">HTTP请求</param>
    /// <returns>请求体内容</returns>
    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);
            
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            
            // 限制请求体大小（最多10KB）
            if (body.Length > 10240)
            {
                return body.Substring(0, 10240) + "... (truncated)";
            }
            
            return body;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 读取响应体
    /// </summary>
    /// <param name="responseBody">响应体流</param>
    /// <returns>响应体内容</returns>
    private async Task<string?> ReadResponseBodyAsync(MemoryStream responseBody)
    {
        try
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            
            // 限制响应体大小（最多10KB）
            if (text.Length > 10240)
            {
                return text.Substring(0, 10240) + "... (truncated)";
            }
            
            return text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 保存日志到数据库
    /// </summary>
    /// <param name="log">API请求日志</param>
    /// <param name="mysqlContext">MySQL数据库上下文</param>
    /// <param name="sqliteContext">SQLite数据库上下文</param>
    private async Task SaveLogAsync(
        ApiRequestLog log,
        MySqlLogDbContext? mysqlContext,
        SqliteLogDbContext? sqliteContext)
    {
        try
        {
            // 优先使用MySQL
            if (mysqlContext != null)
            {
                mysqlContext.ApiRequestLogs.Add(log);
                await mysqlContext.SaveChangesAsync();
                return;
            }

            // 降级使用SQLite
            if (sqliteContext != null)
            {
                sqliteContext.ApiRequestLogs.Add(log);
                await sqliteContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存API请求日志失败");
        }
    }
}
