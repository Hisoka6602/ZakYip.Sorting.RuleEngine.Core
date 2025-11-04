using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 格口信息响应数据传输对象
/// </summary>
public record class ChuteResponseDto
{
    /// <summary>
    /// 格口ID（自增主键）
    /// </summary>
    /// <example>1</example>
    [Description("格口ID（自增主键）")]
    public required long ChuteId { get; init; }

    /// <summary>
    /// 格口名称
    /// </summary>
    /// <example>顺丰专用格口A01</example>
    [Description("格口名称")]
    public required string ChuteName { get; init; }

    /// <summary>
    /// 格口编号（可选）
    /// </summary>
    /// <example>CHUTE-SF-A01</example>
    [Description("格口编号（可选）")]
    public string? ChuteCode { get; init; }

    /// <summary>
    /// 格口描述
    /// </summary>
    /// <example>用于分拣顺丰快递的专用格口</example>
    [Description("格口描述")]
    public string? Description { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <example>true</example>
    [Description("是否启用此格口")]
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    /// <example>2025-11-04T06:00:00</example>
    [Description("格口创建时间")]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    /// <example>2025-11-04T08:30:00</example>
    [Description("格口最后更新时间")]
    public DateTime? UpdatedAt { get; init; }
}
