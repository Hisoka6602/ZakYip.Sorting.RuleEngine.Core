using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Mappers;

/// <summary>
/// DWS配置映射器 - 实体与DTO之间的转换
/// DWS configuration mapper - Conversion between entity and DTO
/// </summary>
public static class DwsConfigMapper
{
    /// <summary>
    /// 转换为响应DTO（单例模式，不包含ID）
    /// Convert to response DTO (Singleton pattern, no ID)
    /// </summary>
    public static DwsConfigResponseDto ToResponseDto(this DwsConfig entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        return new DwsConfigResponseDto
        {
            Name = entity.Name,
            Mode = entity.Mode,
            Host = entity.Host,
            Port = entity.Port,
            DataTemplateId = entity.DataTemplateId,
            IsEnabled = entity.IsEnabled,
            MaxConnections = entity.MaxConnections,
            ReceiveBufferSize = entity.ReceiveBufferSize,
            SendBufferSize = entity.SendBufferSize,
            TimeoutSeconds = entity.TimeoutSeconds,
            AutoReconnect = entity.AutoReconnect,
            ReconnectIntervalSeconds = entity.ReconnectIntervalSeconds,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 从更新请求创建实体
    /// Create entity from update request
    /// </summary>
    public static DwsConfig ToEntity(this DwsConfigUpdateRequest request, ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.LocalNow;
        return new DwsConfig
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
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// 批量转换为响应DTO
    /// Batch convert to response DTO
    /// </summary>
    public static IEnumerable<DwsConfigResponseDto> ToResponseDtos(this IEnumerable<DwsConfig> entities)
    {
        return entities.Select(ToResponseDto);
    }
}

/// <summary>
/// DWS数据模板映射器 - 实体与DTO之间的转换
/// DWS data template mapper - Conversion between entity and DTO
/// </summary>
public static class DwsDataTemplateMapper
{
    /// <summary>
    /// 转换为响应DTO（单例模式，不包含ID）
    /// Convert to response DTO (Singleton pattern, no ID)
    /// </summary>
    public static DwsDataTemplateResponseDto ToResponseDto(this DwsDataTemplate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        return new DwsDataTemplateResponseDto
        {
            Name = entity.Name,
            Template = entity.Template,
            Delimiter = entity.Delimiter,
            IsJsonFormat = entity.IsJsonFormat,
            IsEnabled = entity.IsEnabled,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 从更新请求创建实体
    /// Create entity from update request
    /// </summary>
    public static DwsDataTemplate ToEntity(this DwsDataTemplateUpdateRequest request, ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.LocalNow;
        return new DwsDataTemplate
        {
            TemplateId = DwsDataTemplate.SingletonId,
            Name = request.Name,
            Template = request.Template,
            Delimiter = request.Delimiter,
            IsJsonFormat = request.IsJsonFormat,
            IsEnabled = request.IsEnabled,
            Description = request.Description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// 批量转换为响应DTO
    /// Batch convert to response DTO
    /// </summary>
    public static IEnumerable<DwsDataTemplateResponseDto> ToResponseDtos(this IEnumerable<DwsDataTemplate> entities)
    {
        return entities.Select(ToResponseDto);
    }
}
