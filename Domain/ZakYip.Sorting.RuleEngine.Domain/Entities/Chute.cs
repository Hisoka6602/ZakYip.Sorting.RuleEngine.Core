using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.Services;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;
/// <summary>
/// 格口实体
/// </summary>
[SwaggerSchema(Description = "格口实体，表示分拣系统中的物理格口")]
public class Chute
{
    /// <summary>
    /// 格口ID（自增主键）
    /// 示例: 1
    /// </summary>
    [SwaggerSchema(Description = "格口ID")]
    public long ChuteId { get; set; }
    /// 格口名称
    /// 示例: 深圳格口1号
    [Required(ErrorMessage = "格口名称不能为空")]
    [StringLength(200, ErrorMessage = "格口名称长度不能超过200个字符")]
    [SwaggerSchema(Description = "格口名称")]
    public string ChuteName { get; set; } = string.Empty;
    /// 格口编号（可选）
    /// 示例: SZ001
    [StringLength(100, ErrorMessage = "格口编号长度不能超过100个字符")]
    [SwaggerSchema(Description = "格口编号")]
    public string? ChuteCode { get; set; }
    /// 格口描述
    /// 示例: 深圳方向专用格口
    [StringLength(500, ErrorMessage = "格口描述长度不能超过500个字符")]
    [SwaggerSchema(Description = "格口描述")]
    public string? Description { get; set; }
    /// 是否启用
    /// 示例: true
    [SwaggerSchema(Description = "是否启用")]
    public bool IsEnabled { get; set; } = true;
    /// 创建时间
    /// 示例: 2023-11-01T08:30:00Z
    [SwaggerSchema(Description = "创建时间")]
    public DateTime CreatedAt { get; set; } = SystemClockProvider.LocalNow;
    /// 更新时间
    /// 示例: 2023-11-01T10:15:00Z
    [SwaggerSchema(Description = "更新时间")]
    public DateTime? UpdatedAt { get; set; }
}
