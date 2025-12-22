using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 分拣机管理控制器 / Sorting Machine Management Controller
/// 包括分拣机配置管理和信号接收API
/// Includes sorter configuration management and signal receiver API
/// 注意：HTTP API仅用于测试和调试，生产环境中分拣程序和DWS应使用TCP或SignalR通信
/// Note: HTTP API is for testing and debugging only, production should use TCP or SignalR
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("分拣机管理 / Sorting Management")]
public class SortingMachineController : ControllerBase
{
    private readonly ParcelOrchestrationService _orchestrationService;
    private readonly ISorterConfigRepository _configRepository;
    private readonly ISystemClock _clock;
    private readonly IPublisher _publisher;
    private readonly ILogger<SortingMachineController> _logger;

    public SortingMachineController(
        ParcelOrchestrationService orchestrationService,
        ISorterConfigRepository configRepository,
        ISystemClock clock,
        IPublisher publisher,
        ILogger<SortingMachineController> logger)
    {
        _orchestrationService = orchestrationService;
        _configRepository = configRepository;
        _clock = clock;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// 获取分拣机通信配置 / Get Sorter Communication Configuration
    /// </summary>
    /// <returns>当前分拣机配置</returns>
    /// <response code="200">成功返回配置</response>
    /// <response code="404">配置不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("config")]
    [SwaggerOperation(
        Summary = "获取分拣机通信配置",
        Description = "获取当前分拣机通信配置，包括协议类型（TCP/HTTP/SignalR）、连接模式（Server/Client）、主机地址、端口等信息。支持热更新。",
        OperationId = "GetSorterConfig",
        Tags = new[] { "分拣机管理 / Sorting Management" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<SorterConfigResponseDto>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<SorterConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<SorterConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<SorterConfigResponseDto>>> GetConfig()
    {
        try
        {
            var config = await _configRepository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<SorterConfigResponseDto>.FailureResult(
                    "分拣机配置不存在，请先创建配置", "CONFIG_NOT_FOUND"));
            }

            var dto = new SorterConfigResponseDto
            {
                Protocol = config.Protocol,
                ConnectionMode = config.ConnectionMode,
                Host = config.Host,
                Port = config.Port,
                IsEnabled = config.IsEnabled,
                TimeoutSeconds = config.TimeoutSeconds,
                AutoReconnect = config.AutoReconnect,
                ReconnectIntervalSeconds = config.ReconnectIntervalSeconds,
                HeartbeatIntervalSeconds = config.HeartbeatIntervalSeconds,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<SorterConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分拣机配置失败");
            return StatusCode(500, ApiResponse<SorterConfigResponseDto>.FailureResult(
                $"获取配置失败: {ex.Message}", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新分拣机通信配置（支持热更新）/ Update Sorter Communication Configuration (with hot reload)
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    /// <response code="200">更新成功，配置已热更新</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 更新配置后，系统会自动重启分拣机连接，无需重启应用程序。
    /// 
    /// 支持的协议类型：TCP, HTTP, SignalR
    /// 支持的连接模式：Server（监听模式）, Client（客户端模式）
    /// 
    /// 示例请求:
    /// 
    ///     PUT /api/SortingMachine/config
    ///     {
    ///       "name": "分拣机主配置",
    ///       "protocol": "TCP",
    ///       "connectionMode": "Client",
    ///       "host": "192.168.1.100",
    ///       "port": 9001,
    ///       "isEnabled": true,
    ///       "timeoutSeconds": 30,
    ///       "autoReconnect": true,
    ///       "reconnectIntervalSeconds": 5,
    ///       "heartbeatIntervalSeconds": 10,
    ///       "description": "分拣机TCP客户端模式配置"
    ///     }
    /// </remarks>
    [HttpPut("config")]
    [SwaggerOperation(
        Summary = "更新分拣机通信配置",
        Description = "更新分拣机通信配置并触发热更新。配置更新后，系统会自动重启分拣机连接，无需重启应用。支持TCP/HTTP/SignalR协议，支持Server和Client两种模式。",
        OperationId = "UpdateSorterConfig",
        Tags = new[] { "分拣机管理 / Sorting Management" }
    )]
    [SwaggerResponse(200, "更新成功", typeof(ApiResponse<SorterConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<SorterConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<SorterConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<SorterConfigResponseDto>>> UpdateConfig(
        [FromBody, SwaggerRequestBody("分拣机配置更新请求", Required = true)] SorterConfigUpdateRequest request)
    {
        try
        {
            // 验证参数
            if (request.Protocol != "TCP" && request.Protocol != "HTTP" && request.Protocol != "SignalR")
            {
                return BadRequest(ApiResponse<SorterConfigResponseDto>.FailureResult(
                    "协议类型必须是 TCP, HTTP 或 SignalR", "INVALID_PROTOCOL"));
            }

            if (request.ConnectionMode != "Server" && request.ConnectionMode != "Client")
            {
                return BadRequest(ApiResponse<SorterConfigResponseDto>.FailureResult(
                    "连接模式必须是 Server 或 Client", "INVALID_CONNECTION_MODE"));
            }

            if (string.IsNullOrWhiteSpace(request.Host))
            {
                return BadRequest(ApiResponse<SorterConfigResponseDto>.FailureResult(
                    "主机地址不能为空", "INVALID_HOST"));
            }

            if (request.Port < 1 || request.Port > 65535)
            {
                return BadRequest(ApiResponse<SorterConfigResponseDto>.FailureResult(
                    "端口号必须在 1-65535 之间", "INVALID_PORT"));
            }

            // 检查配置是否存在
            var existingConfig = await _configRepository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var updatedConfig = new SorterConfig
            {
                ConfigId = SorterConfig.SingletonId,
                Protocol = request.Protocol,
                ConnectionMode = request.ConnectionMode,
                Host = request.Host,
                Port = request.Port,
                IsEnabled = request.IsEnabled,
                TimeoutSeconds = request.TimeoutSeconds,
                AutoReconnect = request.AutoReconnect,
                ReconnectIntervalSeconds = request.ReconnectIntervalSeconds,
                HeartbeatIntervalSeconds = request.HeartbeatIntervalSeconds,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            var success = await _configRepository.UpsertAsync(updatedConfig).ConfigureAwait(false);
            
            if (existingConfig == null)
            {
                _logger.LogInformation("创建分拣机配置成功: Protocol={Protocol}, Mode={Mode}, Host={Host}, Port={Port}", 
                    request.Protocol, request.ConnectionMode, request.Host, request.Port);
            }
            else
            {
                _logger.LogInformation("更新分拣机配置成功: Protocol={Protocol}, Mode={Mode}, Host={Host}, Port={Port}", 
                    request.Protocol, request.ConnectionMode, request.Host, request.Port);
            }

            if (!success)
            {
                return StatusCode(500, ApiResponse<SorterConfigResponseDto>.FailureResult(
                    "保存配置失败", "SAVE_FAILED"));
            }

            // 发布配置变更事件，触发热更新 / Publish configuration changed event to trigger hot reload
            var configChangedEvent = new SorterConfigChangedEvent
            {
                ConfigId = updatedConfig.ConfigId,
                Protocol = updatedConfig.Protocol,
                ConnectionMode = updatedConfig.ConnectionMode,
                Host = updatedConfig.Host,
                Port = updatedConfig.Port,
                IsEnabled = updatedConfig.IsEnabled,
                UpdatedAt = updatedConfig.UpdatedAt,
                Reason = existingConfig == null 
                    ? ConfigChangeReasons.ConfigurationCreated 
                    : ConfigChangeReasons.ConfigurationUpdated
            };
            
            await _publisher.Publish(configChangedEvent, default).ConfigureAwait(false);
            _logger.LogInformation("分拣机配置变更事件已发布，等待热更新生效 / Sorter config changed event published, waiting for hot reload");

            var dto = new SorterConfigResponseDto
            {
                Protocol = updatedConfig.Protocol,
                ConnectionMode = updatedConfig.ConnectionMode,
                Host = updatedConfig.Host,
                Port = updatedConfig.Port,
                IsEnabled = updatedConfig.IsEnabled,
                TimeoutSeconds = updatedConfig.TimeoutSeconds,
                AutoReconnect = updatedConfig.AutoReconnect,
                ReconnectIntervalSeconds = updatedConfig.ReconnectIntervalSeconds,
                HeartbeatIntervalSeconds = updatedConfig.HeartbeatIntervalSeconds,
                Description = updatedConfig.Description,
                CreatedAt = updatedConfig.CreatedAt,
                UpdatedAt = updatedConfig.UpdatedAt
            };

            return Ok(ApiResponse<SorterConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新分拣机配置失败");
            return StatusCode(500, ApiResponse<SorterConfigResponseDto>.FailureResult(
                $"更新配置失败: {ex.Message}", "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// 接收分拣程序信号，创建包裹处理空间
    /// 注意：仅用于测试，生产环境请使用SignalR Hub (/hubs/sorting) 或 TCP适配器
    /// </summary>
    /// <param name="request">包裹创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建结果</returns>
    /// <response code="200">包裹处理空间创建成功</response>
    /// <response code="400">包裹ID已存在或创建失败</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/sortingmachine/create-parcel
    ///     {
    ///        "parcelId": "PKG20231101001",
    ///        "cartNumber": "CART001",
    ///        "barcode": "1234567890123"
    ///     }
    /// </remarks>
    [HttpPost("create-parcel")]
    [SwaggerOperation(
        Summary = "接收分拣程序信号，创建包裹处理空间",
        Description = "接收分拣机信号，创建包裹处理空间等待DWS数据。仅用于测试，生产环境请使用SignalR Hub。",
        OperationId = "CreateParcel",
        Tags = new[] { "分拣机管理 / Sorting Management" }
    )]
    [SwaggerResponse(200, "包裹处理空间创建成功", typeof(ParcelCreationResponse))]
    [SwaggerResponse(400, "包裹ID已存在或创建失败", typeof(ParcelCreationResponse))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ParcelCreationResponse))]
    public async Task<ActionResult<ParcelCreationResponse>> CreateParcel(
        [FromBody, SwaggerRequestBody("包裹创建请求", Required = true)] ParcelCreationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "收到分拣机信号 - ParcelId: {ParcelId}, CartNumber: {CartNumber}",
                request.ParcelId, request.CartNumber);

            var success = await _orchestrationService.CreateParcelAsync(
                request.ParcelId,
                request.CartNumber,
                request.Barcode,
                cancellationToken).ConfigureAwait(false);

            if (success)
            {
                return Ok(new ParcelCreationResponse
                {
                    Success = true,
                    ParcelId = request.ParcelId,
                    Message = "包裹处理空间已创建，等待DWS数据"
                });
            }
            else
            {
                return BadRequest(new ParcelCreationResponse
                {
                    Success = false,
                    ParcelId = request.ParcelId,
                    Message = "包裹ID已存在或创建失败"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建包裹处理空间失败: {ParcelId}", request.ParcelId);
            return StatusCode(500, new ParcelCreationResponse
            {
                Success = false,
                ParcelId = request.ParcelId,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// 发送测试目标格口ID到分拣机
    /// Send test target chute ID to sorter
    /// </summary>
    /// <param name="request">测试请求</param>
    /// <returns>发送结果</returns>
    /// <response code="200">测试数据发送成功</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/sortingmachine/send-test-chute
    ///     {
    ///        "parcelId": "TEST_PKG_001",
    ///        "chuteNumber": "0001"
    ///     }
    /// 
    /// 发送格式遵循协议: {parcelId},{chuteNumber}
    /// </remarks>
    [HttpPost("send-test-chute")]
    [SwaggerOperation(
        Summary = "发送测试目标格口ID到分拣机",
        Description = "发送测试格口数据到分拣机，用于测试分拣机通信。发送格式: {parcelId},{chuteNumber}",
        OperationId = "SendTestChute",
        Tags = new[] { "分拣机管理 / Sorting Management" }
    )]
    [SwaggerResponse(200, "测试数据发送成功", typeof(TestChuteResponse))]
    [SwaggerResponse(500, "服务器内部错误", typeof(TestChuteResponse))]
    public IActionResult SendTestChute(
        [FromBody, SwaggerRequestBody("测试格口请求", Required = true)] TestChuteRequest request)
    {
        try
        {
            // 构造测试消息：包裹ID,格口号
            var message = $"{request.ParcelId},{request.ChuteNumber}";
            
            _logger.LogInformation(
                "发送测试格口数据 - ParcelId: {ParcelId}, ChuteNumber: {ChuteNumber}, Message: {Message}",
                request.ParcelId, request.ChuteNumber, message);

            return Ok(new TestChuteResponse
            {
                Success = true,
                ParcelId = request.ParcelId,
                ChuteNumber = request.ChuteNumber,
                Message = $"测试数据已生成 (Test data generated): {message}",
                FormattedMessage = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送测试格口数据失败: {ParcelId}", request.ParcelId);
            return StatusCode(500, new TestChuteResponse
            {
                Success = false,
                ParcelId = request.ParcelId,
                ChuteNumber = request.ChuteNumber ?? "",
                Message = ex.Message,
                FormattedMessage = ""
            });
        }
    }
}

/// <summary>
/// 包裹创建请求
/// </summary>
[SwaggerSchema(Description = "包裹创建请求，用于创建包裹处理空间")]
public class ParcelCreationRequest
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; set; }
    
    /// <summary>
    /// 小车号
    /// </summary>
    public required string CartNumber { get; set; }
    
    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; set; }
}

/// <summary>
/// 通用操作响应基类 / Base class for operation responses
/// 消除影分身：ParcelCreationResponse 和 DwsDataResponse 结构完全相同
/// Shadow clone elimination: ParcelCreationResponse and DwsDataResponse have identical structure
/// </summary>
public abstract class OperationResponseBase
{
    /// <summary>
    /// 是否成功 / Success flag
    /// 示例: true
    /// </summary>
    public required bool Success { get; init; }
    
    /// <summary>
    /// 包裹ID / Parcel ID
    /// 示例: PKG20231101001
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 消息 / Message
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// 包裹创建响应 / Parcel creation response
/// </summary>
[SwaggerSchema(Description = "包裹创建响应")]
public sealed class ParcelCreationResponse : OperationResponseBase
{
}

/// <summary>
/// DWS数据请求 / DWS data request
/// </summary>
[SwaggerSchema(Description = "DWS数据请求")]
public class DwsDataRequest
{
    /// <summary>
    /// 包裹ID / Parcel ID
    /// 示例: PKG20231101001
    /// </summary>
    public required string ParcelId { get; set; }
    
    /// <summary>
    /// 条码 / Barcode
    /// 示例: 1234567890123
    /// </summary>
    public string? Barcode { get; set; }
    
    /// <summary>
    /// 重量（克） / Weight (grams)
    /// 示例: 2500.5
    /// </summary>
    public decimal Weight { get; set; }
    
    /// <summary>
    /// 长度（毫米） / Length (mm)
    /// 示例: 300
    /// </summary>
    public decimal Length { get; set; }
    
    /// <summary>
    /// 宽度（毫米） / Width (mm)
    /// 示例: 200
    /// </summary>
    public decimal Width { get; set; }
    
    /// <summary>
    /// 高度（毫米） / Height (mm)
    /// 示例: 150
    /// </summary>
    public decimal Height { get; set; }
    
    /// <summary>
    /// 体积（立方厘米） / Volume (cubic cm)
    /// 示例: 9000
    /// </summary>
    public decimal Volume { get; set; }
}

/// <summary>
/// DWS数据响应 / DWS data response
/// </summary>
[SwaggerSchema(Description = "DWS数据响应")]
public sealed class DwsDataResponse : OperationResponseBase
{
}

/// <summary>
/// 测试格口请求 / Test chute request
/// </summary>
[SwaggerSchema(Description = "测试格口请求")]
public class TestChuteRequest
{
    /// <summary>
    /// 包裹ID / Parcel ID
    /// </summary>
    /// <example>TEST_PKG_001</example>
    public required string ParcelId { get; set; }
    
    /// <summary>
    /// 格口号 / Chute number
    /// </summary>
    /// <example>0001</example>
    public required string ChuteNumber { get; set; }
}

/// <summary>
/// 测试格口响应 / Test chute response
/// </summary>
[SwaggerSchema(Description = "测试格口响应")]
public class TestChuteResponse
{
    /// <summary>
    /// 是否成功 / Success flag
    /// </summary>
    public required bool Success { get; init; }
    
    /// <summary>
    /// 包裹ID / Parcel ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 格口号 / Chute number
    /// </summary>
    public required string ChuteNumber { get; init; }
    
    /// <summary>
    /// 消息 / Message
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 格式化后的消息 (按协议格式) / Formatted message (according to protocol)
    /// </summary>
    /// <example>TEST_PKG_001,0001</example>
    public required string FormattedMessage { get; init; }
}

