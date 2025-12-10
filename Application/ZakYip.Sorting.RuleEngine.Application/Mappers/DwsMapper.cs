using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Mappers;

/// <summary>
/// DWS配置映射器 - 实体与DTO之间的转换
/// DWS configuration mapper - Conversion between entity and DTO
/// </summary>
public static class DwsConfigMapper
{
    /// <summary>
    /// 转换为响应DTO
    /// Convert to response DTO
    /// </summary>
    public static DwsConfigResponseDto ToResponseDto(this DwsConfig entity)
    {
        return new DwsConfigResponseDto
        {
            ConfigId = entity.ConfigId,
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
    /// 转换为响应DTO
    /// Convert to response DTO
    /// </summary>
    public static DwsDataTemplateResponseDto ToResponseDto(this DwsDataTemplate entity)
    {
        return new DwsDataTemplateResponseDto
        {
            TemplateId = entity.TemplateId,
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
    /// 批量转换为响应DTO
    /// Batch convert to response DTO
    /// </summary>
    public static IEnumerable<DwsDataTemplateResponseDto> ToResponseDtos(this IEnumerable<DwsDataTemplate> entities)
    {
        return entities.Select(ToResponseDto);
    }
}
