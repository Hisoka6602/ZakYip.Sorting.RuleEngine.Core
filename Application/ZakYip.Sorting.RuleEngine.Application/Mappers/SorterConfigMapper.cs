using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Mappers;

/// <summary>
/// 分拣机配置映射器 - 实体与DTO之间的转换
/// Sorter configuration mapper - Conversion between entity and DTO
/// </summary>
public static class SorterConfigMapper
{
    /// <summary>
    /// 转换为响应DTO（单例模式，不包含ID）
    /// Convert to response DTO (Singleton pattern, no ID)
    /// </summary>
    public static SorterConfigResponseDto ToResponseDto(this SorterConfig entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        return new SorterConfigResponseDto
        {
            Protocol = entity.Protocol,
            ConnectionMode = entity.ConnectionMode,
            Host = entity.Host,
            Port = entity.Port,
            IsEnabled = entity.IsEnabled,
            TimeoutSeconds = entity.TimeoutSeconds,
            AutoReconnect = entity.AutoReconnect,
            ReconnectIntervalSeconds = entity.ReconnectIntervalSeconds,
            HeartbeatIntervalSeconds = entity.HeartbeatIntervalSeconds,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 从更新请求创建实体
    /// Create entity from update request
    /// </summary>
    public static SorterConfig ToEntity(this SorterConfigUpdateRequest request, ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.LocalNow;
        return new SorterConfig
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
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
