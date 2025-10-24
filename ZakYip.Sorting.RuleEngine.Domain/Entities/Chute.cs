namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 格口实体
/// </summary>
public class Chute
{
    /// <summary>
    /// 格口ID（自增主键）
    /// </summary>
    public long ChuteId { get; set; }

    /// <summary>
    /// 格口名称
    /// </summary>
    public string ChuteName { get; set; } = string.Empty;

    /// <summary>
    /// 格口编号（可选）
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 格口描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
