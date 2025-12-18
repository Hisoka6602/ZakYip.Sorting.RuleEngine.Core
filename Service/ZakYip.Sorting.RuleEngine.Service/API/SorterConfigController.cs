using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 分拣机配置管理控制器 / Sorter Configuration Management Controller
/// </summary>
[ApiController]
[Route("api/Sorter/Config")]
[Produces("application/json")]
[SwaggerTag("分拣机管理 / Sorting Management")]
public class SorterConfigController : ControllerBase
{
    private readonly ISorterConfigRepository _configRepository;
    private readonly ILogger<SorterConfigController> _logger;
    private readonly ISystemClock _clock;
    private readonly IPublisher _publisher;

    public SorterConfigController(
        ISorterConfigRepository configRepository,
        ILogger<SorterConfigController> logger,
        ISystemClock clock,
        IPublisher publisher)
    {
        _configRepository = configRepository;
        _logger = logger;
        _clock = clock;
        _publisher = publisher;
    }

    /// <summary>
    /// 获取分拣机通信配置 / Get Sorter Communication Configuration
    /// </summary>
    /// <returns>当前分拣机配置</returns>
    /// <response code="200">成功返回配置</response>
    /// <response code="404">配置不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
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
                Name = config.Name,
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
    ///     PUT /api/Sorter/Config
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
    [HttpPut]
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
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(ApiResponse<SorterConfigResponseDto>.FailureResult(
                    "配置名称不能为空", "INVALID_NAME"));
            }

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
                Name = request.Name,
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
                _logger.LogInformation("创建分拣机配置成功: {Name}, Protocol={Protocol}, Mode={Mode}, Host={Host}, Port={Port}", 
                    request.Name, request.Protocol, request.ConnectionMode, request.Host, request.Port);
            }
            else
            {
                _logger.LogInformation("更新分拣机配置成功: {Name}, Protocol={Protocol}, Mode={Mode}, Host={Host}, Port={Port}", 
                    request.Name, request.Protocol, request.ConnectionMode, request.Host, request.Port);
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
                Name = updatedConfig.Name,
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
}
