using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostProcessingCenter;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using Newtonsoft.Json;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// ApiClient测试控制器
/// ApiClient Test Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("ApiClient测试接口")]
public class ApiClientTestController : ControllerBase
{
    private readonly ILogger<ApiClientTestController> _logger;
    private readonly JushuitanErpApiClient? _jushuitanErpApiClient;
    private readonly WdtWmsApiClient? _wdtWmsApiClient;
    private readonly WdtErpFlagshipApiClient? _wdtErpFlagshipApiClient;
    private readonly PostCollectionApiClient? _postCollectionApiClient;
    private readonly PostProcessingCenterApiClient? _postProcessingCenterApiClient;
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly WcsApiLogBackgroundService _logBackgroundService;

    public ApiClientTestController(
        ILogger<ApiClientTestController> logger,
        IServiceProvider serviceProvider,
        WcsApiLogBackgroundService logBackgroundService)
    {
        _logger = logger;
        _logBackgroundService = logBackgroundService;
        
        // Try to get clients from DI, they may not be registered
        _jushuitanErpApiClient = serviceProvider.GetService<JushuitanErpApiClient>();
        _wdtWmsApiClient = serviceProvider.GetService<WdtWmsApiClient>();
        _wdtErpFlagshipApiClient = serviceProvider.GetService<WdtErpFlagshipApiClient>();
        _postCollectionApiClient = serviceProvider.GetService<PostCollectionApiClient>();
        _postProcessingCenterApiClient = serviceProvider.GetService<PostProcessingCenterApiClient>();
        
        // Get database contexts for logging
        _mysqlContext = serviceProvider.GetService<MySqlLogDbContext>();
        _sqliteContext = serviceProvider.GetService<SqliteLogDbContext>();
    }

    /// <summary>
    /// 测试聚水潭ERP API
    /// Test Jushuituan ERP API
    /// </summary>
    /// <param name="request">测试请求参数 / Test request parameters</param>
    /// <param name="methodName">要测试的WCS API方法，默认为RequestChute / WCS API method to test, defaults to RequestChute</param>
    /// <returns>测试结果</returns>
    [HttpPost("jushuitanerp")]
    [SwaggerOperation(
        Summary = "测试聚水潭ERP API",
        Description = "远程测试聚水潭ERP API客户端，发送测试数据并记录访问信息。支持选择测试方法：ScanParcel(扫描包裹), RequestChute(请求格口), NotifyChuteLanding(落格回调)",
        OperationId = "TestJushuitanErpApi",
        Tags = new[] { "ApiClientTest" }
    )]
    [SwaggerResponse(200, "测试成功", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(500, "测试失败", typeof(ApiResponse<ApiClientTestResponse>))]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> TestJushuitanErpApi(
        [FromBody] ApiClientTestRequest request,
        [FromQuery] WcsApiMethod methodName = WcsApiMethod.RequestChute)
    {
        return await TestApiClientAsync(
            _jushuitanErpApiClient,
            "JushuitanErp",
            "聚水潭ERP",
            methodName,
            request,
            (client, barcode, dwsData, ocrData, ct) => client.RequestChuteAsync(barcode, dwsData, ocrData, ct)).ConfigureAwait(false);
    }

    /// <summary>
    /// 测试旺店通WMS API
    /// Test WDT WMS API
    /// </summary>
    /// <param name="request">测试请求参数 / Test request parameters</param>
    /// <param name="methodName">要测试的WCS API方法，默认为RequestChute / WCS API method to test, defaults to RequestChute</param>
    /// <returns>测试结果</returns>
    [HttpPost("wdtwms")]
    [SwaggerOperation(
        Summary = "测试旺店通WMS API",
        Description = "远程测试旺店通WMS API客户端，发送测试数据并记录访问信息。支持选择测试方法：ScanParcel(扫描包裹), RequestChute(请求格口), NotifyChuteLanding(落格回调)",
        OperationId = "TestWdtWmsApi",
        Tags = new[] { "ApiClientTest" }
    )]
    [SwaggerResponse(200, "测试成功", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(500, "测试失败", typeof(ApiResponse<ApiClientTestResponse>))]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> TestWdtWmsApi(
        [FromBody] ApiClientTestRequest request,
        [FromQuery] WcsApiMethod methodName = WcsApiMethod.RequestChute)
    {
        return await TestApiClientAsync(
            _wdtWmsApiClient,
            "WdtWms",
            "旺店通WMS",
            methodName,
            request,
            (client, barcode, dwsData, ocrData, ct) => client.RequestChuteAsync(barcode, dwsData, ocrData, ct)).ConfigureAwait(false);
    }

    /// <summary>
    /// 测试旺店通ERP旗舰版 API
    /// Test WDT ERP Flagship API
    /// </summary>
    /// <param name="request">测试请求参数 / Test request parameters</param>
    /// <param name="methodName">要测试的WCS API方法，默认为RequestChute / WCS API method to test, defaults to RequestChute</param>
    /// <returns>测试结果</returns>
    [HttpPost("wdterpflagship")]
    [SwaggerOperation(
        Summary = "测试旺店通ERP旗舰版 API",
        Description = "远程测试旺店通ERP旗舰版 API客户端，发送测试数据并记录访问信息。支持选择测试方法：ScanParcel(扫描包裹), RequestChute(请求格口), NotifyChuteLanding(落格回调)",
        OperationId = "TestWdtErpFlagshipApi",
        Tags = new[] { "ApiClientTest" }
    )]
    [SwaggerResponse(200, "测试成功", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(500, "测试失败", typeof(ApiResponse<ApiClientTestResponse>))]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> TestWdtErpFlagshipApi(
        [FromBody] ApiClientTestRequest request,
        [FromQuery] WcsApiMethod methodName = WcsApiMethod.RequestChute)
    {
        return await TestApiClientAsync(
            _wdtErpFlagshipApiClient,
            "WdtErpFlagship",
            "旺店通ERP旗舰版",
            methodName,
            request,
            (client, barcode, dwsData, ocrData, ct) => client.RequestChuteAsync(barcode, dwsData, ocrData, ct)).ConfigureAwait(false);
    }

    /// <summary>
    /// 通用API客户端测试逻辑 / Generic API client test logic
    /// </summary>
    /// <typeparam name="T">API客户端类型 / API client type</typeparam>
    /// <param name="apiClient">API客户端实例 / API client instance</param>
    /// <param name="clientName">客户端名称（用于日志）/ Client name (for logging)</param>
    /// <param name="displayName">显示名称（用于错误消息）/ Display name (for error messages)</param>
    /// <param name="methodName">要测试的WCS API方法 / WCS API method to test</param>
    /// <param name="request">测试请求参数 / Test request parameters</param>
    /// <param name="callApiFunc">调用API的委托函数 / Delegate function to call the API</param>
    /// <returns>测试结果 / Test result</returns>
    private async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> TestApiClientAsync<T>(
        T? apiClient,
        string clientName,
        string displayName,
        WcsApiMethod methodName,
        ApiClientTestRequest request,
        Func<T, string, DwsData, OcrData?, CancellationToken, Task<WcsApiResponse>> callApiFunc)
        where T : class
    {
        try
        {
            if (apiClient == null)
            {
                return NotFound(ApiResponse<ApiClientTestResponse>.FailureResult(
                    $"{displayName} ApiClient未配置", "CLIENT_NOT_CONFIGURED"));
            }

            // Check if the client implements IWcsApiAdapter to support method selection
            var wcsAdapter = apiClient as IWcsApiAdapter;
            var selectedMethod = methodName;

            // Create DWS data for testing
            var dwsData = new DwsData
            {
                Barcode = request.Barcode,
                Weight = request.Weight,
                Length = request.Length ?? 0,
                Width = request.Width ?? 0,
                Height = request.Height ?? 0,
                Volume = ((request.Length ?? 0) * (request.Width ?? 0) * (request.Height ?? 0)) / 1000000
            };

            _logger.LogInformation("开始测试{DisplayName} API，条码: {Barcode}，方法: {Method}", 
                displayName, request.Barcode, selectedMethod);

            // 如果客户端实现了 IWcsApiAdapter，则始终通过适配器按 selectedMethod 调用
            // If the client implements IWcsApiAdapter, always invoke via adapter using selectedMethod
            WcsApiResponse response = wcsAdapter != null
                ? selectedMethod switch
                {
                    WcsApiMethod.ScanParcel => await wcsAdapter.ScanParcelAsync(
                        request.Barcode, 
                        HttpContext.RequestAborted),
                    
                    WcsApiMethod.RequestChute => await wcsAdapter.RequestChuteAsync(
                        request.Barcode, 
                        dwsData, 
                        null, 
                        HttpContext.RequestAborted),
                    
                    WcsApiMethod.NotifyChuteLanding => await wcsAdapter.NotifyChuteLandingAsync(
                        request.ParcelId ?? request.Barcode,
                        request.ChuteId ?? "DEFAULT_CHUTE",
                        request.Barcode,
                        HttpContext.RequestAborted),
                    
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(selectedMethod), 
                        selectedMethod, 
                        $"内部错误：收到未支持的测试方法枚举值 {selectedMethod}，理论上不应该到达此分支")
                }
                : await callApiFunc(apiClient, request.Barcode, dwsData, null, HttpContext.RequestAborted);

            // Create test response - map from WcsApiResponse to ApiClientTestResponse
            var testResponse = new ApiClientTestResponse
            {
                Success = response.RequestStatus == ApiRequestStatus.Success,
                Code = response.ResponseStatusCode?.ToString() ?? "ERROR",
                Message = response.FormattedMessage ?? response.ErrorMessage ?? "无消息",
                Data = response.ResponseBody,
                ParcelId = response.ParcelId,
                RequestUrl = response.RequestUrl,
                RequestBody = response.RequestBody,
                ResponseBody = response.ResponseBody,
                ErrorMessage = response.ErrorMessage,
                RequestTime = response.RequestTime,
                ResponseTime = response.ResponseTime,
                DurationMs = response.DurationMs,
                ResponseStatusCode = response.ResponseStatusCode,
                FormattedCurl = response.FormattedCurl
            };

            // Log the test request (incoming API request to our server) - non-blocking
            LogApiTestRequest(clientName, request, testResponse);
            
            // Log the WCS API communication (outgoing API call to WCS/ERP) - non-blocking via Channel
            LogWcsApiCommunication(response, clientName);

            _logger.LogInformation(
                "{DisplayName} API测试完成，条码: {Barcode}, 方法: {Method}, 结果: {Success}",
                displayName, request.Barcode, selectedMethod, response.RequestStatus == ApiRequestStatus.Success);

            return Ok(ApiResponse<ApiClientTestResponse>.SuccessResult(testResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试{DisplayName} API时发生错误", displayName);
            return StatusCode(500, ApiResponse<ApiClientTestResponse>.FailureResult(
                $"测试失败: {ex.Message}", "TEST_FAILED"));
        }
    }

    /// <summary>
    /// 记录API测试请求日志（incoming request to our server）
    /// Log API test request (incoming request to our server)
    /// 非阻塞方式
    /// Non-blocking method
    /// </summary>
    private void LogApiTestRequest(
        string apiClientName, 
        ApiClientTestRequest request, 
        ApiClientTestResponse response)
    {
        try
        {
            var requestLog = new ApiRequestLog
            {
                RequestTime = response.RequestTime,
                RequestIp = GetClientIp(HttpContext),
                RequestMethod = "POST",
                RequestPath = $"/api/ApiClientTest/{apiClientName.ToLower()}",
                QueryString = null,
                RequestHeaders = JsonConvert.SerializeObject(new
                {
                    ContentType = "application/json",
                    ApiClientName = apiClientName
                }),
                RequestBody = JsonConvert.SerializeObject(request),
                ResponseTime = response.ResponseTime,
                ResponseStatusCode = response.ResponseStatusCode ?? 200,
                ResponseHeaders = "{}",
                ResponseBody = response.ResponseBody,
                DurationMs = response.DurationMs,
                UserId = null,
                IsSuccess = response.Success,
                ErrorMessage = response.ErrorMessage
            };

            // 使用 Task.Run 异步保存，避免阻塞主线程
            // Use Task.Run to save asynchronously, avoiding blocking the main thread
            _ = Task.Run(async () =>
            {
                try
                {
                    // Try to save to MySQL first, then SQLite if MySQL is not available
                    if (_mysqlContext != null)
                    {
                        await _mysqlContext.ApiRequestLogs.AddAsync(requestLog).ConfigureAwait(false);
                        await _mysqlContext.SaveChangesAsync().ConfigureAwait(false);
                        _logger.LogDebug("API测试日志已保存到MySQL，ApiClient: {ApiClientName}", apiClientName);
                    }
                    else if (_sqliteContext != null)
                    {
                        await _sqliteContext.ApiRequestLogs.AddAsync(requestLog).ConfigureAwait(false);
                        await _sqliteContext.SaveChangesAsync().ConfigureAwait(false);
                        _logger.LogDebug("API测试日志已保存到SQLite，ApiClient: {ApiClientName}", apiClientName);
                    }
                    else
                    {
                        _logger.LogWarning("无法保存API测试日志，数据库上下文未配置");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存API测试日志时发生错误");
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建API测试日志时发生错误");
        }
    }

    /// <summary>
    /// 记录WCS API通信日志（outgoing API call to WCS/ERP）
    /// Log WCS API communication (outgoing API call to WCS/ERP)
    /// 使用Channel队列，零阻塞，零线程消耗
    /// Uses Channel queue, zero blocking, zero thread consumption
    /// </summary>
    private void LogWcsApiCommunication(WcsApiResponse response, string apiClientName)
    {
        try
        {
            // 使用共享映射器，避免代码重复（影分身）
            // Use shared mapper to avoid code duplication (shadow clone)
            var apiLog = WcsApiResponseMapper.ToApiCommunicationLog(response);

            // 通过后台服务Channel队列入队，非阻塞
            // Enqueue via background service Channel queue, non-blocking
            _logBackgroundService.EnqueueLog(apiLog);
            
            _logger.LogDebug(
                "WCS API通信日志已入队，ApiClient: {ApiClientName}, ParcelId: {ParcelId}, Success: {Success}",
                apiClientName, response.ParcelId, apiLog.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "入队WCS API通信日志时发生错误，ApiClient: {ApiClientName}", apiClientName);
            // 不抛出异常，避免影响主业务流程
            // Do not throw exception to avoid affecting main business flow
        }
    }

    /// <summary>
    /// 测试邮政分揽投机构 API
    /// Test Postal Collection API
    /// </summary>
    /// <param name="request">测试请求参数 / Test request parameters</param>
    /// <param name="methodName">要测试的WCS API方法，默认为RequestChute / WCS API method to test, defaults to RequestChute</param>
    /// <returns>测试结果</returns>
    [HttpPost("postcollection")]
    [SwaggerOperation(
        Summary = "测试邮政分揽投机构 API",
        Description = "远程测试邮政分揽投机构 API客户端，发送测试数据并记录访问信息。支持选择测试方法：ScanParcel(扫描包裹), RequestChute(请求格口), NotifyChuteLanding(落格回调)",
        OperationId = "TestPostCollectionApi",
        Tags = new[] { "ApiClientTest" }
    )]
    [SwaggerResponse(200, "测试成功", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(500, "测试失败", typeof(ApiResponse<ApiClientTestResponse>))]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> TestPostCollectionApi(
        [FromBody] ApiClientTestRequest request,
        [FromQuery] WcsApiMethod methodName = WcsApiMethod.RequestChute)
    {
        return await TestApiClientAsync(
            _postCollectionApiClient,
            "PostCollection",
            "邮政分揽投机构",
            methodName,
            request,
            (client, barcode, dwsData, ocrData, ct) => client.RequestChuteAsync(barcode, dwsData, ocrData, ct)).ConfigureAwait(false);
    }

    /// <summary>
    /// 测试邮政处理中心 API
    /// Test Postal Processing Center API
    /// </summary>
    /// <param name="request">测试请求参数 / Test request parameters</param>
    /// <param name="methodName">要测试的WCS API方法，默认为RequestChute / WCS API method to test, defaults to RequestChute</param>
    /// <returns>测试结果</returns>
    [HttpPost("postprocessingcenter")]
    [SwaggerOperation(
        Summary = "测试邮政处理中心 API",
        Description = "远程测试邮政处理中心 API客户端，发送测试数据并记录访问信息。支持选择测试方法：ScanParcel(扫描包裹), RequestChute(请求格口), NotifyChuteLanding(落格回调)",
        OperationId = "TestPostProcessingCenterApi",
        Tags = new[] { "ApiClientTest" }
    )]
    [SwaggerResponse(200, "测试成功", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<ApiClientTestResponse>))]
    [SwaggerResponse(500, "测试失败", typeof(ApiResponse<ApiClientTestResponse>))]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ApiClientTestResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> TestPostProcessingCenterApi(
        [FromBody] ApiClientTestRequest request,
        [FromQuery] WcsApiMethod methodName = WcsApiMethod.RequestChute)
    {
        return await TestApiClientAsync(
            _postProcessingCenterApiClient,
            "PostProcessingCenter",
            "邮政处理中心",
            methodName,
            request,
            (client, barcode, dwsData, ocrData, ct) => client.RequestChuteAsync(barcode, dwsData, ocrData, ct)).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取客户端IP地址
    /// Get client IP address
    /// </summary>
    private static string GetClientIp(HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
        {
            ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ip))
        {
            ip = context.Connection.RemoteIpAddress?.ToString();
        }
        return ip ?? "Unknown";
    }
}
