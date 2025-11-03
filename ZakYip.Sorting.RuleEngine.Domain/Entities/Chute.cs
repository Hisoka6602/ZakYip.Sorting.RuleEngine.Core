using Swashbuckle.AspNetCore.Annotations;
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
    /// Example: 1
    /// </summary>
    [SwaggerSchema("格口ID")]
    public long ChuteId { get; set; }

    /// <summary>
    /// 格口名称
    /// Example: 深圳格口1号
    /// </summary>
    [Required(ErrorMessage = "格口名称不能为空")]
    [StringLength(200, ErrorMessage = "格口名称长度不能超过200个字符")]
    [SwaggerSchema("格口名称")]
    public string ChuteName { get; set; } = string.Empty;

    /// <summary>
    /// 格口编号（可选）
    /// Example: SZ001
    /// </summary>
    [StringLength(100, ErrorMessage = "格口编号长度不能超过100个字符")]
    [SwaggerSchema("格口编号")]
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 格口描述
    /// Example: 深圳方向专用格口
    /// </summary>
    [StringLength(500, ErrorMessage = "格口描述长度不能超过500个字符")]
    [SwaggerSchema("格口描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// Example: true
    /// </summary>
    [SwaggerSchema("是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// Example: 2023-11-01T08:30:00Z
    /// </summary>
    [SwaggerSchema("创建时间")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// Example: 2023-11-01T10:15:00Z
    /// </summary>
    [SwaggerSchema("更新时间")]
    public DateTime? UpdatedAt { get; set; }
}
