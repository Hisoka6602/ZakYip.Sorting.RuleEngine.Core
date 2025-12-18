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
/// DWS管理控制器
/// DWS Management Controller
/// </summary>
[ApiController]
[Route("api/Dws/Config")]
[Produces("application/json")]
[SwaggerTag("DWS管理 / DWS Management")]
public class DwsConfigController : ControllerBase
{
    private readonly IDwsConfigRepository _configRepository;
    private readonly IConfigurationAuditLogRepository _auditLogRepository;
    private readonly ILogger<DwsConfigController> _logger;
    private readonly ISystemClock _clock;
    private readonly IPublisher _publisher;

    public DwsConfigController(
        IDwsConfigRepository configRepository,
        IConfigurationAuditLogRepository auditLogRepository,
        ILogger<DwsConfigController> logger,
        ISystemClock clock,
        IPublisher publisher)
    {
        _configRepository = configRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
        _clock = clock;
        _publisher = publisher;
    }

    /// <summary>
    /// 获取DWS TCP配置
    /// Get DWS TCP configuration
    /// </summary>
    /// <returns>当前DWS配置</returns>
    /// <response code="200">成功返回配置</response>
    /// <response code="404">配置不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取DWS TCP配置",
        Description = "获取当前DWS通信配置，包括模式(Server/Client)、主机地址、端口等信息",
        OperationId = "GetDwsConfig",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsConfigResponseDto>>> GetConfig()
    {
        try
        {
            var config = await _configRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<DwsConfigResponseDto>.FailureResult(
                    "DWS配置不存在，请先创建配置", "CONFIG_NOT_FOUND"));
            }

            var dto = new DwsConfigResponseDto
            {
                Name = config.Name,
                Mode = config.Mode,
                Host = config.Host,
                Port = config.Port,
                DataTemplateId = config.DataTemplateId,
                IsEnabled = config.IsEnabled,
                MaxConnections = config.MaxConnections,
                ReceiveBufferSize = config.ReceiveBufferSize,
                SendBufferSize = config.SendBufferSize,
                TimeoutSeconds = config.TimeoutSeconds,
                AutoReconnect = config.AutoReconnect,
                ReconnectIntervalSeconds = config.ReconnectIntervalSeconds,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<DwsConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取DWS配置失败");
            return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult(
                $"获取配置失败: {ex.Message}", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新DWS TCP配置（支持热更新）
    /// Update DWS TCP configuration (with hot reload support)
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    /// <response code="200">更新成功，配置已热更新</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 更新配置后，系统会自动重启DWS TCP连接，无需重启应用程序。
    /// 
    /// 示例请求:
    /// 
    ///     PUT /api/Dws/Config
    ///     {
    ///       "name": "DWS主配置",
    ///       "mode": "Server",
    ///       "host": "0.0.0.0",
    ///       "port": 8001,
    ///       "dataTemplateId": 1,
    ///       "isEnabled": true,
    ///       "maxConnections": 1000,
    ///       "receiveBufferSize": 8192,
    ///       "sendBufferSize": 8192,
    ///       "timeoutSeconds": 30,
    ///       "autoReconnect": true,
    ///       "reconnectIntervalSeconds": 5,
    ///       "description": "DWS服务器模式配置"
    ///     }
    /// </remarks>
    [HttpPut]
    [SwaggerOperation(
        Summary = "更新DWS TCP配置",
        Description = "更新DWS通信配置并触发热更新。配置更新后，系统会自动重启DWS连接，无需重启应用。支持Server和Client两种模式。",
        OperationId = "UpdateDwsConfig",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "更新成功", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(400, "请求参数错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsConfigResponseDto>>> UpdateConfig(
        [FromBody, SwaggerRequestBody("DWS配置更新请求", Required = true)] DwsConfigUpdateRequest request)
    {
        try
        {
            // 验证参数
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(ApiResponse<DwsConfigResponseDto>.FailureResult(
                    "配置名称不能为空", "INVALID_NAME"));
            }

            if (request.Mode != "Server" && request.Mode != "Client")
            {
                return BadRequest(ApiResponse<DwsConfigResponseDto>.FailureResult(
                    "模式必须是 Server 或 Client", "INVALID_MODE"));
            }

            if (string.IsNullOrWhiteSpace(request.Host))
            {
                return BadRequest(ApiResponse<DwsConfigResponseDto>.FailureResult(
                    "主机地址不能为空", "INVALID_HOST"));
            }

            if (request.Port < 1 || request.Port > 65535)
            {
                return BadRequest(ApiResponse<DwsConfigResponseDto>.FailureResult(
                    "端口号必须在 1-65535 之间", "INVALID_PORT"));
            }

            // 检查配置是否存在
            var existingConfig = await _configRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var updatedConfig = new DwsConfig
            {
                ConfigId = DwsConfig.SingletonId,
                Name = request.Name,
                Mode = request.Mode,
                Host = request.Host,
                Port = request.Port,
                DataTemplateId = request.DataTemplateId,
                IsEnabled = request.IsEnabled,
                MaxConnections = request.MaxConnections,
                ReceiveBufferSize = request.ReceiveBufferSize,
                SendBufferSize = request.SendBufferSize,
                TimeoutSeconds = request.TimeoutSeconds,
                AutoReconnect = request.AutoReconnect,
                ReconnectIntervalSeconds = request.ReconnectIntervalSeconds,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _configRepository.AddAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("创建DWS配置成功: {Name}, Mode={Mode}, Host={Host}, Port={Port}", 
                    request.Name, request.Mode, request.Host, request.Port);
            }
            else
            {
                success = await _configRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("更新DWS配置成功: {Name}, Mode={Mode}, Host={Host}, Port={Port}", 
                    request.Name, request.Mode, request.Host, request.Port);
            }

            if (!success)
            {
                return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult(
                    "保存配置失败", "SAVE_FAILED"));
            }

            // 保存审计日志 / Save audit log
            var operationType = existingConfig == null ? "Create" : "Update";
            var contentBefore = existingConfig != null 
                ? JsonSerializer.Serialize(existingConfig, new JsonSerializerOptions { WriteIndented = false })
                : null;
            var contentAfter = JsonSerializer.Serialize(updatedConfig, new JsonSerializerOptions { WriteIndented = false });
            
            var auditLog = new ConfigurationAuditLog
            {
                ConfigurationType = nameof(DwsConfig),
                ConfigurationId = updatedConfig.ConfigId,
                OperationType = operationType,
                ContentBefore = contentBefore,
                ContentAfter = contentAfter,
                ChangeReason = existingConfig == null 
                    ? ConfigChangeReasons.ConfigurationCreated 
                    : ConfigChangeReasons.ConfigurationUpdated,
                OperatorUser = User?.Identity?.Name ?? Environment.MachineName,
                OperatorIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = now,
                Remarks = $"DWS配置{operationType}：{updatedConfig.Name}"
            };
            
            var auditSaved = await _auditLogRepository.AddAsync(auditLog).ConfigureAwait(false);
            if (!auditSaved)
            {
                _logger.LogWarning(
                    "DWS配置审计日志保存失败 / Failed to save DWS config audit log: Operation={Operation}, ConfigId={ConfigId}",
                    operationType, updatedConfig.ConfigId);
            }

            // 发布配置变更事件，触发热更新 / Publish configuration changed event to trigger hot reload
            var configChangedEvent = new DwsConfigChangedEvent
            {
                ConfigId = updatedConfig.ConfigId,
                Mode = updatedConfig.Mode,
                Host = updatedConfig.Host,
                Port = updatedConfig.Port,
                IsEnabled = updatedConfig.IsEnabled,
                UpdatedAt = updatedConfig.UpdatedAt,
                Reason = existingConfig == null 
                    ? ConfigChangeReasons.ConfigurationCreated 
                    : ConfigChangeReasons.ConfigurationUpdated
            };
            
            await _publisher.Publish(configChangedEvent, default).ConfigureAwait(false);
            _logger.LogInformation("DWS配置变更事件已发布，等待热更新生效 / DWS config changed event published, waiting for hot reload");

            var dto = new DwsConfigResponseDto
            {
                Name = updatedConfig.Name,
                Mode = updatedConfig.Mode,
                Host = updatedConfig.Host,
                Port = updatedConfig.Port,
                DataTemplateId = updatedConfig.DataTemplateId,
                IsEnabled = updatedConfig.IsEnabled,
                MaxConnections = updatedConfig.MaxConnections,
                ReceiveBufferSize = updatedConfig.ReceiveBufferSize,
                SendBufferSize = updatedConfig.SendBufferSize,
                TimeoutSeconds = updatedConfig.TimeoutSeconds,
                AutoReconnect = updatedConfig.AutoReconnect,
                ReconnectIntervalSeconds = updatedConfig.ReconnectIntervalSeconds,
                Description = updatedConfig.Description,
                CreatedAt = updatedConfig.CreatedAt,
                UpdatedAt = updatedConfig.UpdatedAt
            };

            return Ok(ApiResponse<DwsConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新DWS配置失败");
            return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult(
                $"更新配置失败: {ex.Message}", "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// 重置DWS配置为默认值
    /// Reset DWS configuration to default values
    /// </summary>
    /// <returns>重置结果</returns>
    /// <response code="200">重置成功</response>
    /// <response code="500">服务器内部错误</response>
    [HttpDelete]
    [SwaggerOperation(
        Summary = "重置DWS配置",
        Description = "将DWS配置重置为默认值。默认配置：Server模式，监听0.0.0.0:8001",
        OperationId = "ResetDwsConfig",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "重置成功", typeof(ApiResponse<DwsConfigResponseDto>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<DwsConfigResponseDto>))]
    public async Task<ActionResult<ApiResponse<DwsConfigResponseDto>>> ResetConfig()
    {
        try
        {
            var now = _clock.LocalNow;
            var defaultConfig = new DwsConfig
            {
                ConfigId = DwsConfig.SingletonId,
                Name = "DWS默认配置",
                Mode = "Server",
                Host = "0.0.0.0",
                Port = 8001,
                DataTemplateId = 1,
                IsEnabled = true,
                MaxConnections = 1000,
                ReceiveBufferSize = 8192,
                SendBufferSize = 8192,
                TimeoutSeconds = 30,
                AutoReconnect = true,
                ReconnectIntervalSeconds = 5,
                Description = "DWS服务器模式默认配置",
                CreatedAt = now,
                UpdatedAt = now
            };

            var existingConfig = await _configRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);
            if (existingConfig != null)
            {
                defaultConfig = defaultConfig with { CreatedAt = existingConfig.CreatedAt };
            }

            var success = existingConfig == null
                ? await _configRepository.AddAsync(defaultConfig).ConfigureAwait(false)
                : await _configRepository.UpdateAsync(defaultConfig).ConfigureAwait(false);

            if (!success)
            {
                return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult(
                    "重置配置失败", "RESET_FAILED"));
            }

            _logger.LogInformation("DWS配置已重置为默认值");

            var dto = new DwsConfigResponseDto
            {
                Name = defaultConfig.Name,
                Mode = defaultConfig.Mode,
                Host = defaultConfig.Host,
                Port = defaultConfig.Port,
                DataTemplateId = defaultConfig.DataTemplateId,
                IsEnabled = defaultConfig.IsEnabled,
                MaxConnections = defaultConfig.MaxConnections,
                ReceiveBufferSize = defaultConfig.ReceiveBufferSize,
                SendBufferSize = defaultConfig.SendBufferSize,
                TimeoutSeconds = defaultConfig.TimeoutSeconds,
                AutoReconnect = defaultConfig.AutoReconnect,
                ReconnectIntervalSeconds = defaultConfig.ReconnectIntervalSeconds,
                Description = defaultConfig.Description,
                CreatedAt = defaultConfig.CreatedAt,
                UpdatedAt = defaultConfig.UpdatedAt
            };

            return Ok(ApiResponse<DwsConfigResponseDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置DWS配置失败");
            return StatusCode(500, ApiResponse<DwsConfigResponseDto>.FailureResult(
                $"重置配置失败: {ex.Message}", "RESET_FAILED"));
        }
    }

    /// <summary>
    /// 手动触发DWS配置重载
    /// Manually trigger DWS configuration reload
    /// </summary>
    /// <returns>重载结果</returns>
    /// <response code="200">重载成功</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("reload")]
    [SwaggerOperation(
        Summary = "手动触发配置重载",
        Description = "手动触发DWS配置重载，重启TCP连接。通常在更新配置后自动触发，此端点用于特殊情况下的手动重载。",
        OperationId = "ReloadDwsConfig",
        Tags = new[] { "DWS管理 / DWS Management" }
    )]
    [SwaggerResponse(200, "重载成功", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> ReloadConfig()
    {
        try
        {
            _logger.LogInformation("手动触发DWS配置重载 / Manually triggering DWS configuration reload");

            // 获取当前配置 / Get current configuration
            var config = await _configRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);
            if (config == null)
            {
                return NotFound(ApiResponse<object>.FailureResult(
                    "DWS配置不存在，无法重载 / DWS configuration not found, cannot reload", 
                    "CONFIG_NOT_FOUND"));
            }

            // 发布配置变更事件，触发重载 / Publish configuration changed event to trigger reload
            var configChangedEvent = new DwsConfigChangedEvent
            {
                ConfigId = config.ConfigId,
                Mode = config.Mode,
                Host = config.Host,
                Port = config.Port,
                IsEnabled = config.IsEnabled,
                UpdatedAt = _clock.LocalNow,
                Reason = ConfigChangeReasons.ManualReloadTriggered
            };
            
            await _publisher.Publish(configChangedEvent, default).ConfigureAwait(false);
            _logger.LogInformation("DWS配置重载事件已发布 / DWS config reload event published");

            // 保存审计日志：记录手动重载操作 / Save audit log: record manual reload operation
            var auditLog = new ConfigurationAuditLog
            {
                ConfigurationType = nameof(DwsConfig),
                ConfigurationId = config.ConfigId,
                OperationType = "Reload",
                ContentBefore = null,
                ContentAfter = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = false }),
                ChangeReason = ConfigChangeReasons.ManualReloadTriggered,
                OperatorUser = User?.Identity?.Name ?? Environment.MachineName,
                OperatorIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = _clock.LocalNow,
                Remarks = $"DWS配置手动重载：{config.Name}"
            };
            
            var auditSaved = await _auditLogRepository.AddAsync(auditLog).ConfigureAwait(false);
            if (!auditSaved)
            {
                _logger.LogWarning(
                    "DWS配置重载审计日志保存失败 / Failed to save DWS config reload audit log: ConfigId={ConfigId}",
                    config.ConfigId);
            }

            return Ok(ApiResponse<object>.SuccessResult(
                new { reloadedAt = _clock.LocalNow, message = "配置重载已触发 / Configuration reload triggered" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发DWS配置重载失败 / Failed to trigger DWS configuration reload");
            return StatusCode(500, ApiResponse<object>.FailureResult(
                $"触发重载失败 / Reload trigger failed: {ex.Message}", "RELOAD_FAILED"));
        }
    }
}
