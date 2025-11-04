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
    /// <example>1.11.0</example>
    [Description("系统版本号")]
    public required string Version { get; init; }

    /// <summary>
    /// 构建日期
    /// </summary>
    /// <example>2025-11-04</example>
    [Description("系统构建日期")]
    public required string BuildDate { get; init; }

    /// <summary>
    /// 运行环境
    /// </summary>
    /// <example>Production</example>
    [Description("运行环境：Development、Staging、Production")]
    public required string Environment { get; init; }

    /// <summary>
    /// .NET版本
    /// </summary>
    /// <example>8.0</example>
    [Description(".NET运行时版本")]
    public required string DotNetVersion { get; init; }
}
