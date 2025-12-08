namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 自动应答模式服务接口
/// Auto-response mode service interface
/// </summary>
public interface IAutoResponseModeService
{
    /// <summary>
    /// 启用自动应答模式
    /// Enable auto-response mode
    /// </summary>
    void Enable();

    /// <summary>
    /// 禁用自动应答模式
    /// Disable auto-response mode
    /// </summary>
    void Disable();

    /// <summary>
    /// 获取自动应答模式状态
    /// Get auto-response mode status
    /// </summary>
    bool IsEnabled { get; }
}
