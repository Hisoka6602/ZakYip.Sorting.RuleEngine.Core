using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 版本信息响应数据传输对象
/// </summary>
public record class VersionResponseDto
{
    /// <summary>
    /// 系统版本号
    /// </summary>
    /// <example>1.13.0</example>
    [Description("系统版本号")]
    public required string Version { get; init; }

    /// <summary>
    /// 产品版本号
    /// </summary>
    /// <example>1.13.0</example>
    [Description("产品版本号")]
    public required string ProductVersion { get; init; }

    /// <summary>
    /// 文件版本号
    /// </summary>
    /// <example>1.13.0.0</example>
    [Description("文件版本号")]
    public required string FileVersion { get; init; }

    /// <summary>
    /// 产品名称
    /// </summary>
    /// <example>ZakYip 分拣规则引擎</example>
    [Description("产品名称")]
    public required string ProductName { get; init; }

    /// <summary>
    /// 公司名称
    /// </summary>
    /// <example>ZakYip</example>
    [Description("公司名称")]
    public required string CompanyName { get; init; }

    /// <summary>
    /// 系统描述
    /// </summary>
    /// <example>ZakYip分拣规则引擎系统 - 高性能包裹分拣规则引擎</example>
    [Description("系统描述")]
    public required string Description { get; init; }

    /// <summary>
    /// 构建日期
    /// </summary>
    /// <example>2025-11-04 09:00:00</example>
    [Description("系统构建日期")]
    public required string BuildDate { get; init; }

    /// <summary>
    /// .NET运行时框架
    /// </summary>
    /// <example>.NET 8.0</example>
    [Description(".NET运行时框架描述")]
    public required string Framework { get; init; }
}
